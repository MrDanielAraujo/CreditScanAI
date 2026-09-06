using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class DocumentsControllerTests : IClassFixture<DocumentsApiWebApplicationFactory>
{
    private readonly DocumentsApiWebApplicationFactory _factory;

    public DocumentsControllerTests(DocumentsApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.FixturePdfBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Balanco2Trim2020.pdf"));
    }

    [Fact]
    public async Task UploadStatusResult_FullFlow_ExtractsRealData()
    {
        // Arrange: seed a tenant + company (there's no Company CRUD API yet).
        Guid companyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenantId = Guid.NewGuid();
            companyId = Guid.NewGuid();
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
            db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = "C1", Name = "Company 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent
        {
            { new StringContent(companyId.ToString()), "companyId" },
            { new StringContent("BalanceSheet"), "documentType" }
        };
        var fileContent = new ByteArrayContent(_factory.FixturePdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "Balanco2Trim2020.pdf");

        // Act: upload
        var uploadResponse = await client.PostAsync("/api/documents/upload", form);

        // Assert: upload accepted
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var uploadBody = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<UploadDocumentResponse>>();
        uploadBody!.Success.Should().BeTrue();
        var documentId = uploadBody.Data!.DocumentId;

        // Act: poll status until the background service finishes (fast: no I/O, InMemory DB).
        DocumentStatusResponse? status = null;
        for (var i = 0; i < 50; i++)
        {
            var statusResponse = await client.GetAsync($"/api/documents/{documentId}/status");
            var statusBody = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<DocumentStatusResponse>>();
            status = statusBody!.Data;
            if (status!.Status is "Completed" or "Failed")
            {
                break;
            }
            await Task.Delay(100);
        }

        status!.Status.Should().Be("Completed", status.ExtractionError);

        // Act: fetch result
        var resultResponse = await client.GetAsync($"/api/documents/{documentId}/result");
        var resultBody = await resultResponse.Content.ReadFromJsonAsync<ApiResponse<DocumentResultResponse>>();

        // Assert: the real extracted data made it all the way through HTTP.
        resultBody!.Success.Should().BeTrue();
        var result = resultBody.Data!;
        result.Periods.Should().Contain(p => p.Year == 2020 && p.Quarter == 2);

        var caixa = result.Accounts.Should().ContainSingle(a => a.OriginalName == "Caixa e bancos").Subject;
        var period2020 = result.Periods.Single(p => p.Year == 2020 && p.Quarter == 2);
        caixa.Values.Should().Contain(v => v.PeriodId == period2020.Id && v.RawValue == 1_067_737.38m);
    }

    private async Task<(Guid CompanyId, Guid DocumentId)> SeedDocumentAsync(
        string fileName, string companyName, ExtractionStatus extractionStatus = ExtractionStatus.Completed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{documentId:N}"[..10], Name = companyName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document
        {
            Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow, FileName = fileName, FilePath = "doc.pdf",
            ExtractionStatus = extractionStatus,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return (companyId, documentId);
    }

    [Fact]
    public async Task List_FiltersByCompanyId_ReturnsOnlyThatCompanysDocuments()
    {
        var (companyA, documentA) = await SeedDocumentAsync("balanco_a.pdf", "Empresa A List Test");
        var (_, documentB) = await SeedDocumentAsync("balanco_b.pdf", "Empresa B List Test");

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<ApiResponse<DocumentListResponse>>($"/api/documents?companyId={companyA}");

        response!.Data!.Items.Should().Contain(i => i.Id == documentA);
        response.Data!.Items.Should().NotContain(i => i.Id == documentB);
    }

    [Fact]
    public async Task List_SearchByFileName_ReturnsOnlyMatchingDocuments()
    {
        var (_, matchingId) = await SeedDocumentAsync("relatorio_especial_2020.pdf", "Empresa Search Test A");
        var (_, otherId) = await SeedDocumentAsync("outro_arquivo.pdf", "Empresa Search Test B");

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<ApiResponse<DocumentListResponse>>("/api/documents?search=especial");

        response!.Data!.Items.Should().Contain(i => i.Id == matchingId);
        response.Data!.Items.Should().NotContain(i => i.Id == otherId);
    }

    [Fact]
    public async Task Reprocess_ExtractedDocument_ReturnsAccepted()
    {
        var (_, documentId) = await SeedDocumentAsync("reprocessar.pdf", "Empresa Reprocess Test");

        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/documents/{documentId}/reprocess", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReprocessDocumentResponse>>();
        body!.Data!.DocumentId.Should().Be(documentId);
    }

    [Fact]
    public async Task Reprocess_NotYetExtractedDocument_ReturnsConflict()
    {
        var (_, documentId) = await SeedDocumentAsync("nao_extraido.pdf", "Empresa Reprocess Conflict Test", ExtractionStatus.Pending);

        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/documents/{documentId}/reprocess", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reprocess_UnknownDocument_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/documents/{Guid.NewGuid()}/reprocess", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

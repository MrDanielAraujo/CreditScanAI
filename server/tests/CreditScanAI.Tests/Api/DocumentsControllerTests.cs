using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Domain.Entities;
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
}

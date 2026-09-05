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
public class CompaniesControllerTests : IClassFixture<DocumentsApiWebApplicationFactory>
{
    private readonly DocumentsApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CompaniesControllerTests(DocumentsApiWebApplicationFactory factory)
    {
        _factory = factory;

        // CurrentTenantProvider needs at least one Tenant row - the Testing
        // environment's fresh InMemory DB never runs DbSeeder (that only
        // runs in Development), so every test in this class needs one.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Tenants.Any())
        {
            db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Companies Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListPeriods_OnlyReturnsPeriodsWithDataForThatCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var periodWithDataId = Guid.NewGuid();
        var periodWithoutDataId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var otherDocumentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Periods Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{companyId:N}", Name = "Empresa A", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = otherCompanyId, TenantId = tenantId, Code = $"C_{otherCompanyId:N}", Name = "Empresa B", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodWithDataId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2021, Quarter = 1, StartDate = new DateOnly(2021, 1, 1), EndDate = new DateOnly(2021, 3, 31), CreatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodWithoutDataId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2021, Quarter = 2, StartDate = new DateOnly(2021, 4, 1), EndDate = new DateOnly(2021, 6, 30), CreatedAt = DateTime.UtcNow });

        db.Documents.Add(new Document { Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet, UploadDate = DateTime.UtcNow, FileName = "a.pdf", FilePath = "a.pdf", ExtractionStatus = ExtractionStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document { Id = otherDocumentId, TenantId = tenantId, CompanyId = otherCompanyId, DocumentType = DocumentType.BalanceSheet, UploadDate = DateTime.UtcNow, FileName = "b.pdf", FilePath = "b.pdf", ExtractionStatus = ExtractionStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var sourceAccountId = Guid.NewGuid();
        db.SourceAccounts.Add(new SourceAccount { Id = sourceAccountId, TenantId = tenantId, DocumentId = documentId, OriginalName = "Caixa", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow });
        db.AccountValues.Add(new AccountValue { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, DocumentId = documentId, PeriodId = periodWithDataId, RawValue = 100m, ScaleFactor = 1, CreatedAt = DateTime.UtcNow });

        // periodWithoutDataId has no AccountValue for ANY company - should never appear.
        // otherDocumentId belongs to a different company - its data must not leak into companyId's period list.
        var otherSourceAccountId = Guid.NewGuid();
        db.SourceAccounts.Add(new SourceAccount { Id = otherSourceAccountId, TenantId = tenantId, DocumentId = otherDocumentId, OriginalName = "Caixa", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow });
        db.AccountValues.Add(new AccountValue { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = otherSourceAccountId, DocumentId = otherDocumentId, PeriodId = periodWithoutDataId, RawValue = 50m, ScaleFactor = 1, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var response = await _client.GetFromJsonAsync<ApiResponse<List<PeriodDto>>>($"/api/companies/{companyId}/periods");

        response!.Data!.Should().ContainSingle(p => p.Id == periodWithDataId);
        response.Data!.Should().NotContain(p => p.Id == periodWithoutDataId);
    }

    [Fact]
    public async Task ListCalculatedPeriods_OnlyReturnsPeriodsWithAFase4Calculation()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var periodWithCalculationId = Guid.NewGuid();
        var periodWithOnlyRawDataId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Calc Periods Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{companyId:N}", Name = "Empresa Calc", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodWithCalculationId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2022, Quarter = 1, StartDate = new DateOnly(2022, 1, 1), EndDate = new DateOnly(2022, 3, 31), CreatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodWithOnlyRawDataId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2022, Quarter = 2, StartDate = new DateOnly(2022, 4, 1), EndDate = new DateOnly(2022, 6, 30), CreatedAt = DateTime.UtcNow });

        // periodWithOnlyRawDataId has raw AccountValues but was never
        // calculated (Fase 4) - it must not appear here, even though it
        // WOULD appear in the /periods (raw-data) endpoint.
        var documentId = Guid.NewGuid();
        db.Documents.Add(new Document { Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet, UploadDate = DateTime.UtcNow, FileName = "a.pdf", FilePath = "a.pdf", ExtractionStatus = ExtractionStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        var sourceAccountId = Guid.NewGuid();
        db.SourceAccounts.Add(new SourceAccount { Id = sourceAccountId, TenantId = tenantId, DocumentId = documentId, OriginalName = "Caixa", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow });
        db.AccountValues.Add(new AccountValue { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, DocumentId = documentId, PeriodId = periodWithOnlyRawDataId, RawValue = 100m, ScaleFactor = 1, CreatedAt = DateTime.UtcNow });

        db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = companyId, PeriodId = periodWithCalculationId,
            Key = "ATIVO_TOTAL", Value = 100m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var response = await _client.GetFromJsonAsync<ApiResponse<List<PeriodDto>>>($"/api/companies/{companyId}/calculated-periods");

        response!.Data!.Should().ContainSingle(p => p.Id == periodWithCalculationId);
        response.Data!.Should().NotContain(p => p.Id == periodWithOnlyRawDataId);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithAllFields()
    {
        var request = new UpsertCompanyRequest("CONSOL_A", "Empresa Consolidação A", "Empresa Consolidação A Ltda", "12.345.678/0001-90", "Educação", null, "BRL");

        var response = await _client.PostAsJsonAsync("/api/companies", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        body!.Data!.Code.Should().Be("CONSOL_A");
        body.Data!.LegalName.Should().Be("Empresa Consolidação A Ltda");
        body.Data!.Cnpj.Should().Be("12.345.678/0001-90");
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var request = new UpsertCompanyRequest("DUPCOMP", "Empresa Dup", null, null, null, null, null);
        var first = await _client.PostAsJsonAsync("/api/companies", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/companies", request with { Name = "Outra" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_CompanyWithDocuments_ReturnsConflict()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var createResponse = await _client.PostAsJsonAsync("/api/companies", new UpsertCompanyRequest("INUSE_COMP", "Empresa Em Uso", null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        var companyId = created!.Data!.Id;

        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(), TenantId = db.Companies.First(c => c.Id == companyId).TenantId, CompanyId = companyId,
            DocumentType = DocumentType.BalanceSheet, UploadDate = DateTime.UtcNow, FileName = "x.pdf", FilePath = "x.pdf",
            ExtractionStatus = ExtractionStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/companies/{companyId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ChangesNameAndKeepsId()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/companies", new UpsertCompanyRequest("UPD_COMP", "Nome Original", null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/companies/{created!.Data!.Id}",
            new UpsertCompanyRequest("UPD_COMP", "Nome Atualizado", null, null, null, null, null));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        updated!.Data!.Id.Should().Be(created.Data!.Id);
        updated.Data!.Name.Should().Be("Nome Atualizado");
    }
}

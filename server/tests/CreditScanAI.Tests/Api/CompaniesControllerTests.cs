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
}

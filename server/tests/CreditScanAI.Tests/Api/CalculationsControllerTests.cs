using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Calculations;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class CalculationsControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CalculationsControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid CompanyId, Guid PeriodId)> SeedCompanyPeriodAndOneClassifiedValueAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Calc Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{companyId:N}", Name = "Empresa Cálculo", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2020, Quarter = 2, StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2020, 6, 30), CreatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document
        {
            Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow, FileName = "d.pdf", FilePath = "d.pdf", ExtractionStatus = ExtractionStatus.Completed,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var ativo = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "ATIVO", Name = "Ativo", CreatedAt = DateTime.UtcNow };
        db.AccountTypes.Add(ativo);
        var circulante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = ativo.Id, Code = "CIRCULANTE", Name = "Circulante", CreatedAt = DateTime.UtcNow };
        db.AccountSubtypes.Add(circulante);

        var caixa = new StandardAccount { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId, AccountTypeId = ativo.Id, AccountSubtypeId = circulante.Id, Code = "CAIXA", Name = "Caixa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.StandardAccounts.Add(caixa);

        var sourceAccountId = Guid.NewGuid();
        db.SourceAccounts.Add(new SourceAccount { Id = sourceAccountId, TenantId = tenantId, DocumentId = documentId, OriginalName = "Caixa", NormalizedName = "CAIXA", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow });
        db.AccountClassifications.Add(new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, StandardAccountId = caixa.Id, ChartOfAccountsId = chartId,
            ConfidenceScore = 0.99m, ClassificationMethod = "EXACT_MATCH", ReviewStatus = ClassificationReviewStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.AccountValues.Add(new AccountValue { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, DocumentId = documentId, PeriodId = periodId, RawValue = 1000m, ScaleFactor = 1, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (companyId, periodId);
    }

    [Fact]
    public async Task Calculate_UnknownCompany_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/calculations/companies/{Guid.NewGuid()}/periods/{Guid.NewGuid()}/calculate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetResults_BeforeAnyCalculation_ReturnsNotFound()
    {
        var (companyId, periodId) = await SeedCompanyPeriodAndOneClassifiedValueAsync();

        var response = await _client.GetAsync($"/api/calculations/companies/{companyId}/periods/{periodId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Calculate_ThenGetResults_ReturnsThePersistedValues()
    {
        var (companyId, periodId) = await SeedCompanyPeriodAndOneClassifiedValueAsync();

        var calculateResponse = await _client.PostAsync($"/api/calculations/companies/{companyId}/periods/{periodId}/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var calculateBody = await calculateResponse.Content.ReadFromJsonAsync<ApiResponse<CalculationResultResponse>>();
        calculateBody!.Data!.Values[CalculationKeys.AtivoCirculante].Should().Be(1000m);
        calculateBody.Data!.Values[CalculationKeys.AtivoTotal].Should().Be(1000m);

        var getResponse = await _client.GetFromJsonAsync<ApiResponse<CalculationResultResponse>>($"/api/calculations/companies/{companyId}/periods/{periodId}");
        getResponse!.Data!.Values[CalculationKeys.AtivoCirculante].Should().Be(1000m);
    }
}

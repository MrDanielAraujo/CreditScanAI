using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Consolidation;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class ConsolidationControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConsolidationControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_FewerThanTwoCompanies_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/consolidation/calculate", new ConsolidateRequest(Guid.NewGuid(), [Guid.NewGuid()]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculate_CompanyMissingCalculation_ReturnsBadRequestNamingIt()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var calculatedCompanyId = Guid.NewGuid();
        var uncalculatedCompanyId = Guid.NewGuid();

        db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = calculatedCompanyId, PeriodId = periodId,
            Key = CalculationKeys.AtivoTotal, Value = 100m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/consolidation/calculate", new ConsolidateRequest(periodId, [calculatedCompanyId, uncalculatedCompanyId]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Error!.Message.Should().Contain(uncalculatedCompanyId.ToString());
    }

    [Fact]
    public async Task Calculate_TwoCalculatedCompanies_ReturnsConsolidatedTotals()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        void Add(Guid companyId, string key, decimal value) => db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = companyId, PeriodId = periodId,
            Key = key, Value = value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        foreach (var companyId in new[] { companyAId, companyBId })
        {
            Add(companyId, CalculationKeys.AtivoCirculante, 100m);
            Add(companyId, CalculationKeys.AtivoNaoCirculante, 0m);
            Add(companyId, CalculationKeys.AtivoTotal, 100m);
            Add(companyId, CalculationKeys.PassivoCirculante, 40m);
            Add(companyId, CalculationKeys.PassivoNaoCirculante, 0m);
            Add(companyId, CalculationKeys.PassivoTotal, 40m);
            Add(companyId, CalculationKeys.PatrimonioLiquido, 60m);
            Add(companyId, CalculationKeys.ReceitaTotal, 0m);
            Add(companyId, CalculationKeys.CustoTotal, 0m);
            Add(companyId, CalculationKeys.DespesaTotal, 0m);
            Add(companyId, CalculationKeys.DepreciacaoAmortizacaoTotal, 0m);
            Add(companyId, CalculationKeys.ResultadoPeriodo, 0m);
            Add(companyId, CalculationKeys.ResultadoAntesDepreciacaoAmortizacao, 0m);
        }
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/consolidation/calculate", new ConsolidateRequest(periodId, [companyAId, companyBId]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ConsolidationResultResponse>>();
        body!.Data!.Values[CalculationKeys.AtivoTotal].Should().Be(200m);
        body.Data!.EquationBalanced.Should().BeTrue();
        body.Data!.IsValid.Should().BeTrue();
    }
}

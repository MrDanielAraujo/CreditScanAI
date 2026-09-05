using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Tests.Services;

public class ConsolidationServiceTests
{
    private static AppDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void AddCalculatedValue(AppDbContext db, Guid tenantId, Guid companyId, Guid periodId, string key, decimal value) =>
        db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = companyId, PeriodId = periodId,
            Key = key, Value = value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task ConsolidateAsync_TwoCompanies_SumsAdditiveKeysAndRecomputesRatios()
    {
        await using var db = CreateInMemoryContext();
        var tenantId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        // Empresa A: Ativo Circ 1000, Passivo Circ 300, PL 700 (balanceada: 1000 = 300+700)
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.AtivoCirculante, 1000m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.AtivoNaoCirculante, 0m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.AtivoTotal, 1000m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.PassivoCirculante, 300m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.PassivoNaoCirculante, 0m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.PassivoTotal, 300m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.PatrimonioLiquido, 700m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.ReceitaTotal, 2000m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.CustoTotal, -500m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.DespesaTotal, -300m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.DepreciacaoAmortizacaoTotal, 0m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.ResultadoPeriodo, 1200m);
        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.ResultadoAntesDepreciacaoAmortizacao, 1200m);

        // Empresa B: Ativo Circ 500, Passivo Circ 200, PL 300 (balanceada: 500 = 200+300)
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.AtivoCirculante, 500m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.AtivoNaoCirculante, 0m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.AtivoTotal, 500m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.PassivoCirculante, 200m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.PassivoNaoCirculante, 0m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.PassivoTotal, 200m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.PatrimonioLiquido, 300m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.ReceitaTotal, 1000m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.CustoTotal, -200m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.DespesaTotal, -100m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.DepreciacaoAmortizacaoTotal, 0m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.ResultadoPeriodo, 700m);
        AddCalculatedValue(db, tenantId, companyBId, periodId, CalculationKeys.ResultadoAntesDepreciacaoAmortizacao, 700m);

        await db.SaveChangesAsync();

        var service = new ConsolidationService(db);
        var outcome = await service.ConsolidateAsync(periodId, [companyAId, companyBId], CancellationToken.None);

        outcome.Success.Should().BeTrue();
        outcome.Values[CalculationKeys.AtivoCirculante].Should().Be(1500m);
        outcome.Values[CalculationKeys.AtivoTotal].Should().Be(1500m);
        outcome.Values[CalculationKeys.PassivoTotal].Should().Be(500m);
        outcome.Values[CalculationKeys.PatrimonioLiquido].Should().Be(1000m);
        outcome.Values[CalculationKeys.ReceitaTotal].Should().Be(3000m);
        outcome.Values[CalculationKeys.ResultadoPeriodo].Should().Be(1900m);

        // Índices recalculados a partir dos totais consolidados, não somados
        // diretamente (1500/500=3.0, e NÃO a soma/média de 1000/300 e 500/200).
        outcome.Values[CalculationKeys.LiquidezCorrente].Should().Be(3.0m);
        outcome.Values[CalculationKeys.EquacaoVariancia].Should().Be(0m);

        outcome.ReconciliationChecks.Should().OnlyContain(c => c.Passed);
    }

    [Fact]
    public async Task ConsolidateAsync_OneCompanyNeverCalculated_ReturnsMissing()
    {
        await using var db = CreateInMemoryContext();
        var tenantId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid(); // never calculated

        AddCalculatedValue(db, tenantId, companyAId, periodId, CalculationKeys.AtivoTotal, 1000m);
        await db.SaveChangesAsync();

        var service = new ConsolidationService(db);
        var outcome = await service.ConsolidateAsync(periodId, [companyAId, companyBId], CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.MissingCompanyIds.Should().ContainSingle(id => id == companyBId);
    }

    [Fact]
    public async Task ConsolidateAsync_UnbalancedConsolidation_FailsTheEquationCheck()
    {
        await using var db = CreateInMemoryContext();
        var tenantId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Ativo 1000 mas Passivo+PL só 400 -> desbalanceado de propósito.
        AddCalculatedValue(db, tenantId, companyId, periodId, CalculationKeys.AtivoTotal, 1000m);
        AddCalculatedValue(db, tenantId, companyId, periodId, CalculationKeys.PassivoTotal, 100m);
        AddCalculatedValue(db, tenantId, companyId, periodId, CalculationKeys.PatrimonioLiquido, 300m);
        var otherCompanyId = Guid.NewGuid();
        AddCalculatedValue(db, tenantId, otherCompanyId, periodId, CalculationKeys.AtivoTotal, 0m);
        await db.SaveChangesAsync();

        var service = new ConsolidationService(db);
        var outcome = await service.ConsolidateAsync(periodId, [companyId, otherCompanyId], CancellationToken.None);

        outcome.Success.Should().BeTrue();
        var equationCheck = outcome.ReconciliationChecks.Single(c => c.CheckId == "BASIC_EQUATION");
        equationCheck.Passed.Should().BeFalse();
        equationCheck.Variance.Should().Be(600m);
    }
}

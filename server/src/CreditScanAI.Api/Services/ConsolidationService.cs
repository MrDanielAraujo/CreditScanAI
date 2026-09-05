using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

public sealed record ReconciliationCheck(
    string CheckId,
    string Description,
    bool Passed,
    decimal ExpectedValue,
    decimal ActualValue,
    decimal Variance,
    string? ErrorMessage);

public sealed class ConsolidationOutcome
{
    public bool Success { get; private init; }
    public List<Guid> MissingCompanyIds { get; private init; } = [];
    public Dictionary<string, decimal> Values { get; private init; } = new();
    public List<ReconciliationCheck> ReconciliationChecks { get; private init; } = [];

    public static ConsolidationOutcome Missing(List<Guid> missingCompanyIds) => new() { Success = false, MissingCompanyIds = missingCompanyIds };

    public static ConsolidationOutcome Ok(Dictionary<string, decimal> values, List<ReconciliationCheck> checks) =>
        new() { Success = true, Values = values, ReconciliationChecks = checks };
}

/// <summary>
/// Fase 5 (Motor de Consolidação), primeira versão: Consolidação Simples
/// (soma) reaproveitando os totais já calculados por empresa/período na
/// Fase 4 (CalculatedFinancialValue) - sem motor de fórmulas configurável.
/// Eliminação de transações inter-empresariais e Consolidação Ponderada
/// ficam de fora (sem conta de partes relacionadas nem participação
/// societária no modelo hoje - ver decisão da Fase 5).
/// </summary>
public class ConsolidationService
{
    private readonly AppDbContext _db;

    public ConsolidationService(AppDbContext db) => _db = db;

    public async Task<ConsolidationOutcome> ConsolidateAsync(Guid periodId, List<Guid> companyIds, CancellationToken cancellationToken)
    {
        var rows = await _db.CalculatedFinancialValues
            .Where(v => v.PeriodId == periodId && companyIds.Contains(v.CompanyId))
            .ToListAsync(cancellationToken);

        var calculatedCompanyIds = rows.Select(r => r.CompanyId).Distinct().ToHashSet();
        var missing = companyIds.Where(id => !calculatedCompanyIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            return ConsolidationOutcome.Missing(missing);
        }

        // Só as chaves aditivas são somadas diretamente entre empresas -
        // margens/índices/variância são recalculados a partir delas.
        var consolidated = new Dictionary<string, decimal>();
        foreach (var key in FinancialCalculationService.AdditiveKeys)
        {
            consolidated[key] = rows.Where(r => r.Key == key).Sum(r => r.Value);
        }

        FinancialCalculationService.ApplyDerivedValues(consolidated);

        var checks = BuildReconciliationChecks(consolidated, rows);

        return ConsolidationOutcome.Ok(consolidated, checks);
    }

    private static List<ReconciliationCheck> BuildReconciliationChecks(
        Dictionary<string, decimal> consolidated, List<CalculatedFinancialValue> rows)
    {
        var checks = new List<ReconciliationCheck>();

        // Check 1: cada total consolidado deve ser exatamente a soma dos
        // originais por empresa (garantido por construção nesta implementação
        // - funciona como uma rede de segurança contra regressão futura).
        var maxVariance = 0m;
        foreach (var key in FinancialCalculationService.AdditiveKeys)
        {
            var sourceSum = rows.Where(r => r.Key == key).Sum(r => r.Value);
            maxVariance = Math.Max(maxVariance, Math.Abs(consolidated.GetValueOrDefault(key) - sourceSum));
        }
        var sourceTotalsPassed = maxVariance < 0.01m;
        checks.Add(new ReconciliationCheck(
            "SOURCE_TOTALS",
            "Totais consolidados devem ser a soma exata dos originais por empresa",
            sourceTotalsPassed, 0m, maxVariance, maxVariance,
            sourceTotalsPassed ? null : $"Variância de {maxVariance} entre totais consolidados e a soma dos originais"));

        // Check 2: equação fundamental no consolidado.
        var ativoTotal = consolidated.GetValueOrDefault(CalculationKeys.AtivoTotal);
        var passivoMaisPl = consolidated.GetValueOrDefault(CalculationKeys.PassivoTotal) + consolidated.GetValueOrDefault(CalculationKeys.PatrimonioLiquido);
        var equationVariance = Math.Abs(ativoTotal - passivoMaisPl);
        var equationPassed = equationVariance <= FinancialCalculationService.EquationTolerance;
        checks.Add(new ReconciliationCheck(
            "BASIC_EQUATION",
            "Ativo = Passivo + Patrimônio Líquido (consolidado)",
            equationPassed, ativoTotal, passivoMaisPl, equationVariance,
            equationPassed ? null : $"Equação desbalanceada: variância de {equationVariance}"));

        return checks;
    }
}

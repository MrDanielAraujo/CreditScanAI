using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Fase 4 (Motor de Cálculos), primeira versão: totais do plano de contas por
/// Tipo/Subtipo já classificado (sem motor de fórmulas configurável - ver
/// decisão da Fase 4) e um pequeno conjunto de indicadores financeiros
/// adaptados de 04_MOTOR_CALCULOS.md para uma entidade sem fins lucrativos
/// (o DRE real de exemplo é uma "Demonstração de Superávit/Déficit", sem
/// Lucro Operacional/Lucro Líquido). Sinal (+/-) já vem correto dos valores
/// extraídos - o normalizador já converte "(1.234,56)" em negativo - então
/// os totais são somas diretas, sem precisar de um motor de regras de sinal.
/// </summary>
public class FinancialCalculationService
{
    // Tolerância de arredondamento para considerar a equação Ativo = Passivo
    // + PL balanceada, igual ao critério de 04_MOTOR_CALCULOS.md.
    public const decimal EquationTolerance = 1m;

    // Chaves que são somas diretas de valores classificados - podem ser
    // somadas entre empresas na consolidação (Fase 5). As chaves restantes
    // (margens, índices, variância da equação) são razões/derivadas e
    // precisam ser recalculadas a partir dos totais já consolidados, nunca
    // somadas diretamente entre empresas - ver ApplyDerivedValues.
    public static readonly IReadOnlyList<string> AdditiveKeys =
    [
        CalculationKeys.AtivoCirculante, CalculationKeys.AtivoNaoCirculante, CalculationKeys.AtivoTotal,
        CalculationKeys.PassivoCirculante, CalculationKeys.PassivoNaoCirculante, CalculationKeys.PassivoTotal, CalculationKeys.PatrimonioLiquido,
        CalculationKeys.ReceitaTotal, CalculationKeys.CustoTotal, CalculationKeys.DespesaTotal, CalculationKeys.DepreciacaoAmortizacaoTotal,
        CalculationKeys.ResultadoPeriodo, CalculationKeys.ResultadoAntesDepreciacaoAmortizacao
    ];

    private readonly AppDbContext _db;

    public FinancialCalculationService(AppDbContext db) => _db = db;

    public async Task<Dictionary<string, decimal>> CalculateAsync(Guid companyId, Guid periodId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Empresa não encontrada.");

        var classifiedValues = await (
            from accountValue in _db.AccountValues
            join sourceAccount in _db.SourceAccounts on accountValue.SourceAccountId equals sourceAccount.Id
            join document in _db.Documents on sourceAccount.DocumentId equals document.Id
            join classification in _db.AccountClassifications on sourceAccount.Id equals classification.SourceAccountId
            join standardAccount in _db.StandardAccounts on classification.StandardAccountId equals standardAccount.Id
            join accountType in _db.AccountTypes on standardAccount.AccountTypeId equals accountType.Id
            join accountSubtype in _db.AccountSubtypes on standardAccount.AccountSubtypeId equals accountSubtype.Id
            where document.CompanyId == companyId && accountValue.PeriodId == periodId
            select new
            {
                accountValue.RawValue,
                TypeCode = accountType.Code,
                SubtypeCode = accountSubtype.Code,
                StandardAccountCode = standardAccount.Code
            }
        ).ToListAsync(cancellationToken);

        decimal SumWhere(Func<string, string, string, bool> predicate) =>
            classifiedValues.Where(v => predicate(v.TypeCode, v.SubtypeCode, v.StandardAccountCode)).Sum(v => v.RawValue ?? 0m);

        var ativoCirculante = SumWhere((type, subtype, _) => type == "ATIVO" && subtype == "CIRCULANTE");
        var ativoNaoCirculante = SumWhere((type, subtype, _) => type == "ATIVO" && subtype == "NAO_CIRCULANTE");
        var ativoTotal = SumWhere((type, _, _) => type == "ATIVO");

        var passivoCirculante = SumWhere((type, subtype, _) => type == "PASSIVO" && subtype == "CIRCULANTE");
        var passivoNaoCirculante = SumWhere((type, subtype, _) => type == "PASSIVO" && subtype == "NAO_CIRCULANTE");
        var patrimonioLiquido = SumWhere((type, subtype, _) => type == "PASSIVO" && subtype == "PL");
        var passivoTotal = SumWhere((type, subtype, _) => type == "PASSIVO" && subtype != "PL");

        var receitaTotal = SumWhere((type, subtype, _) => type == "DRE" && subtype == "RECEITA");
        var custoTotal = SumWhere((type, subtype, _) => type == "DRE" && subtype == "CUSTO");
        var despesaTotal = SumWhere((type, subtype, _) => type == "DRE" && subtype == "DESPESA");
        var depreciacaoAmortizacaoTotal = SumWhere((_, _, code) =>
            code is "DRE_DESPESA_DEPRECIACAO" or "DRE_DESPESA_AMORTIZACAO");

        var resultadoPeriodo = receitaTotal + custoTotal + despesaTotal;
        // Deprec./Amort. já entram negativas no resultado - "tirá-las" do
        // resultado é somar de volta a magnitude (subtrair um valor negativo).
        var resultadoAntesDepreciacaoAmortizacao = resultadoPeriodo - depreciacaoAmortizacaoTotal;

        var values = new Dictionary<string, decimal>
        {
            [CalculationKeys.AtivoCirculante] = ativoCirculante,
            [CalculationKeys.AtivoNaoCirculante] = ativoNaoCirculante,
            [CalculationKeys.AtivoTotal] = ativoTotal,
            [CalculationKeys.PassivoCirculante] = passivoCirculante,
            [CalculationKeys.PassivoNaoCirculante] = passivoNaoCirculante,
            [CalculationKeys.PassivoTotal] = passivoTotal,
            [CalculationKeys.PatrimonioLiquido] = patrimonioLiquido,
            [CalculationKeys.ReceitaTotal] = receitaTotal,
            [CalculationKeys.CustoTotal] = custoTotal,
            [CalculationKeys.DespesaTotal] = despesaTotal,
            [CalculationKeys.DepreciacaoAmortizacaoTotal] = depreciacaoAmortizacaoTotal,
            [CalculationKeys.ResultadoPeriodo] = resultadoPeriodo,
            [CalculationKeys.ResultadoAntesDepreciacaoAmortizacao] = resultadoAntesDepreciacaoAmortizacao
        };

        ApplyDerivedValues(values);

        var existing = await _db.CalculatedFinancialValues
            .Where(v => v.CompanyId == companyId && v.PeriodId == periodId)
            .ToListAsync(cancellationToken);
        var existingByKey = existing.ToDictionary(v => v.Key);

        foreach (var (key, value) in values)
        {
            if (existingByKey.TryGetValue(key, out var row))
            {
                row.Value = value;
                row.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
                {
                    Id = Guid.NewGuid(),
                    TenantId = company.TenantId,
                    CompanyId = companyId,
                    PeriodId = periodId,
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return values;
    }

    /// <summary>
    /// Recalcula as chaves derivadas (margens, índices, variância da
    /// equação) a partir das chaves aditivas já presentes em <paramref
    /// name="values"/> - reaproveitado tanto por CalculateAsync (totais de
    /// uma empresa) quanto por ConsolidationService (totais já consolidados
    /// entre empresas), já que essas razões nunca podem ser somadas
    /// diretamente entre empresas.
    /// </summary>
    public static void ApplyDerivedValues(Dictionary<string, decimal> values)
    {
        var ativoCirculante = values.GetValueOrDefault(CalculationKeys.AtivoCirculante);
        var ativoTotal = values.GetValueOrDefault(CalculationKeys.AtivoTotal);
        var passivoCirculante = values.GetValueOrDefault(CalculationKeys.PassivoCirculante);
        var passivoTotal = values.GetValueOrDefault(CalculationKeys.PassivoTotal);
        var patrimonioLiquido = values.GetValueOrDefault(CalculationKeys.PatrimonioLiquido);
        var receitaTotal = values.GetValueOrDefault(CalculationKeys.ReceitaTotal);
        var resultadoPeriodo = values.GetValueOrDefault(CalculationKeys.ResultadoPeriodo);

        values[CalculationKeys.MargemResultado] = receitaTotal != 0 ? Math.Round(resultadoPeriodo / receitaTotal * 100, 4) : 0m;
        values[CalculationKeys.LiquidezCorrente] = passivoCirculante != 0 ? Math.Round(ativoCirculante / passivoCirculante, 4) : 0m;
        values[CalculationKeys.IndiceEndividamento] = patrimonioLiquido != 0 ? Math.Round(passivoTotal / patrimonioLiquido, 4) : 0m;
        values[CalculationKeys.EquacaoVariancia] = ativoTotal - (passivoTotal + patrimonioLiquido);
    }
}

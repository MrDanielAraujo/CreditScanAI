using System.Globalization;

namespace CreditScanAI.Api.Services;

public enum FinancialValueFormat
{
    Currency,
    Percent,
    Ratio
}

public sealed record FinancialValueDefinition(string Key, string Label, FinancialValueFormat Format);

/// <summary>
/// Server-side mirror of client/src/components/financial/financialValueDefinitions.ts -
/// used by the PDF/Excel export (Fase 10) so the exported file uses the same
/// labels/grouping/formatting as the Dashboard and Consolidação pages. Keep
/// both in sync if either changes.
/// </summary>
public static class FinancialValueDefinitions
{
    public static readonly IReadOnlyList<FinancialValueDefinition> Balanco =
    [
        new(CalculationKeys.AtivoCirculante, "Ativo Circulante", FinancialValueFormat.Currency),
        new(CalculationKeys.AtivoNaoCirculante, "Ativo Não Circulante", FinancialValueFormat.Currency),
        new(CalculationKeys.AtivoTotal, "Ativo Total", FinancialValueFormat.Currency),
        new(CalculationKeys.PassivoCirculante, "Passivo Circulante", FinancialValueFormat.Currency),
        new(CalculationKeys.PassivoNaoCirculante, "Passivo Não Circulante", FinancialValueFormat.Currency),
        new(CalculationKeys.PassivoTotal, "Passivo Total", FinancialValueFormat.Currency),
        new(CalculationKeys.PatrimonioLiquido, "Patrimônio Líquido", FinancialValueFormat.Currency)
    ];

    public static readonly IReadOnlyList<FinancialValueDefinition> Dre =
    [
        new(CalculationKeys.ReceitaTotal, "Receita Total", FinancialValueFormat.Currency),
        new(CalculationKeys.CustoTotal, "Custo Total", FinancialValueFormat.Currency),
        new(CalculationKeys.DespesaTotal, "Despesa Total", FinancialValueFormat.Currency),
        new(CalculationKeys.DepreciacaoAmortizacaoTotal, "Depreciação e Amortização", FinancialValueFormat.Currency)
    ];

    public static readonly IReadOnlyList<FinancialValueDefinition> Indicadores =
    [
        new(CalculationKeys.ResultadoPeriodo, "Resultado do Período", FinancialValueFormat.Currency),
        new(CalculationKeys.ResultadoAntesDepreciacaoAmortizacao, "Resultado Antes de Depreciação e Amortização", FinancialValueFormat.Currency),
        new(CalculationKeys.MargemResultado, "Margem de Resultado", FinancialValueFormat.Percent),
        new(CalculationKeys.LiquidezCorrente, "Liquidez Corrente", FinancialValueFormat.Ratio),
        new(CalculationKeys.IndiceEndividamento, "Índice de Endividamento", FinancialValueFormat.Ratio)
    ];

    public static string Format(decimal? value, FinancialValueFormat format)
    {
        if (value is null)
        {
            return "—";
        }

        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        return format switch
        {
            FinancialValueFormat.Currency => value.Value.ToString("C", ptBr),
            FinancialValueFormat.Percent => $"{value.Value.ToString("N2", ptBr)}%",
            _ => value.Value.ToString("N4", ptBr)
        };
    }
}

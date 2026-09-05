namespace CreditScanAI.Api.Services;

/// <summary>Names of the values produced by FinancialCalculationService, stored in CalculatedFinancialValue.Key.</summary>
public static class CalculationKeys
{
    // Totais do balanço, por agregação de Tipo/Subtipo já classificado.
    public const string AtivoCirculante = "ATIVO_CIRCULANTE";
    public const string AtivoNaoCirculante = "ATIVO_NAO_CIRCULANTE";
    public const string AtivoTotal = "ATIVO_TOTAL";
    public const string PassivoCirculante = "PASSIVO_CIRCULANTE";
    public const string PassivoNaoCirculante = "PASSIVO_NAO_CIRCULANTE";
    public const string PassivoTotal = "PASSIVO_TOTAL";
    public const string PatrimonioLiquido = "PATRIMONIO_LIQUIDO";

    // Totais do DRE.
    public const string ReceitaTotal = "RECEITA_TOTAL";
    public const string CustoTotal = "CUSTO_TOTAL";
    public const string DespesaTotal = "DESPESA_TOTAL";
    public const string DepreciacaoAmortizacaoTotal = "DEPRECIACAO_AMORTIZACAO_TOTAL";

    // Indicadores.
    public const string ResultadoPeriodo = "RESULTADO_PERIODO";
    public const string MargemResultado = "MARGEM_RESULTADO";
    public const string ResultadoAntesDepreciacaoAmortizacao = "RESULTADO_ANTES_DEPRECIACAO_AMORTIZACAO";
    public const string LiquidezCorrente = "LIQUIDEZ_CORRENTE";
    public const string IndiceEndividamento = "INDICE_ENDIVIDAMENTO";

    // Validação da equação contábil (Ativo = Passivo + PL): a diferença
    // (idealmente 0). "Balanceada" é uma decisão do chamador (tolerância).
    public const string EquacaoVariancia = "EQUACAO_VARIANCIA";

    public static readonly IReadOnlyList<string> All =
    [
        AtivoCirculante, AtivoNaoCirculante, AtivoTotal,
        PassivoCirculante, PassivoNaoCirculante, PassivoTotal, PatrimonioLiquido,
        ReceitaTotal, CustoTotal, DespesaTotal, DepreciacaoAmortizacaoTotal,
        ResultadoPeriodo, MargemResultado, ResultadoAntesDepreciacaoAmortizacao,
        LiquidezCorrente, IndiceEndividamento,
        EquacaoVariancia
    ];
}

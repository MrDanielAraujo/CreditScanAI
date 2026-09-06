namespace CreditScanAI.Api.Contracts.Reports;

public sealed record PeriodEquationStatusDto(
    Guid PeriodId,
    string PeriodLabel,
    bool? EquationBalanced,
    decimal? EquationVariance);

/// <summary>
/// Relatório de qualidade por documento (Fase 7) - só sinais reais
/// derivados do banco (contagens de revisão, confiança média, equação do(s)
/// período(s) que este documento alimenta). Sem um "quality_score" 0-1
/// combinando tudo numa nota única sem uma base real que justifique os
/// pesos, ao contrário do 07_ESPECIFICACAO_APIS.md original.
/// </summary>
public sealed record QualityReportResponse(
    Guid DocumentId,
    string FileName,
    int TotalClassifiedAccounts,
    int PendingCount,
    int NeedsReviewCount,
    int ApprovedCount,
    int OverriddenCount,
    int RejectedCount,
    float? AverageConfidence,
    List<PeriodEquationStatusDto> PeriodEquationStatus);

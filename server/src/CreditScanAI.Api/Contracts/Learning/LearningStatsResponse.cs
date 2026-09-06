namespace CreditScanAI.Api.Contracts.Learning;

public sealed record TopCorrectedAccountDto(string SourceAccountName, int Count);

/// <summary>
/// Estatísticas reais de aprendizado (Fase 6 Parte 3) - só contagens
/// derivadas de decisões que realmente aconteceram no banco. Sem métrica de
/// "acurácia" inventada: ApprovalRate só existe quando há pelo menos uma
/// classificação revisada por humano (Approved/Overridden/Rejected).
/// </summary>
public sealed record LearningStatsResponse(
    int TotalClassifications,
    Dictionary<string, int> ByReviewStatus,
    Dictionary<string, int> ByMethod,
    int ReviewedCount,
    float? ApprovalRate,
    List<TopCorrectedAccountDto> TopOverriddenAccounts,
    List<TopCorrectedAccountDto> TopRejectedAccounts);

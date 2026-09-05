using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Models;
using Microsoft.Extensions.Logging;

namespace CreditScanAI.Classification;

public interface IAccountClassifier
{
    /// <summary>
    /// Classifica uma conta de origem combinando, em ordem: histórico de
    /// decisões humanas da mesma empresa, regras determinísticas, e IA local
    /// como camada de reforço. Nunca lança por falha da IA - se o Ollama
    /// estiver indisponível, cai de volta na sugestão da regra (se houver)
    /// com o método "AI_UNAVAILABLE".
    /// </summary>
    Task<ClassificationResult> ClassifyAsync(
        ClassificationContext context,
        IReadOnlyList<StandardAccountCandidate> candidates,
        CancellationToken cancellationToken);
}

public sealed class AccountClassifier : IAccountClassifier
{
    // Regra com confiança >= a isso é aceita sem consultar a IA (hoje só o
    // EXACT_MATCH, 0.99, atinge esse patamar - o PATTERN_MATCH, no máximo
    // 0.85, sempre passa pela IA como segunda opinião).
    private const float RuleHighConfidenceThreshold = 0.90f;

    private readonly IRuleOrchestrator _ruleOrchestrator;
    private readonly IAiClassificationService _aiService;
    private readonly IClassificationHistoryProvider _historyProvider;
    private readonly ILogger<AccountClassifier> _logger;

    public AccountClassifier(
        IRuleOrchestrator ruleOrchestrator,
        IAiClassificationService aiService,
        IClassificationHistoryProvider historyProvider,
        ILogger<AccountClassifier> logger)
    {
        _ruleOrchestrator = ruleOrchestrator;
        _aiService = aiService;
        _historyProvider = historyProvider;
        _logger = logger;
    }

    public async Task<ClassificationResult> ClassifyAsync(
        ClassificationContext context,
        IReadOnlyList<StandardAccountCandidate> candidates,
        CancellationToken cancellationToken)
    {
        // Camada 4 (Histórico) roda primeiro: se esta empresa já teve uma
        // classificação aprovada/corrigida por humano para este mesmo nome
        // de conta antes, essa é a decisão mais confiável que existe -
        // reaproveita direto, sem gastar tempo com regra/IA de novo.
        var historical = await _historyProvider.FindPreviousDecisionAsync(
            context.CompanyId, context.DocumentId, context.NormalizedName, cancellationToken);

        if (historical is not null)
        {
            return new ClassificationResult(
                historical.StandardAccountId,
                0.98f,
                "HISTORICAL_DECISION",
                $"Empresa já classificou '{context.SourceAccountName}' como '{historical.StandardAccountName}' anteriormente ({historical.DecidedAt:d})");
        }

        var ruleResult = _ruleOrchestrator.Classify(context, candidates);

        if (ruleResult.StandardAccountId is not null && ruleResult.Confidence >= RuleHighConfidenceThreshold)
        {
            return ruleResult;
        }

        if (candidates.Count == 0)
        {
            return ruleResult;
        }

        try
        {
            var aiResult = await _aiService.ClassifyAsync(context, candidates, cancellationToken);

            var evidence = ruleResult.StandardAccountId is null
                ? aiResult.Reasoning
                : $"{aiResult.Reasoning} (regra {ruleResult.Method} sugeriu confiança {ruleResult.Confidence:P0})";

            return new ClassificationResult(aiResult.StandardAccountId, aiResult.Confidence, "AI", evidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IA local indisponível ao classificar '{SourceAccount}'", context.SourceAccountName);

            return new ClassificationResult(
                ruleResult.StandardAccountId,
                ruleResult.Confidence,
                "AI_UNAVAILABLE",
                $"IA local indisponível ({ex.Message}); mantida sugestão da regra: {ruleResult.Evidence ?? "nenhuma"}");
        }
    }
}

using CreditScanAI.Classification.Models;

namespace CreditScanAI.Classification.Ai;

/// <summary>
/// Semantic classification fallback used when the deterministic rules
/// (see Rules/) can't confidently pick a StandardAccount. Implementations
/// must throw on failure (unreachable service, malformed response, etc.) -
/// the caller (AccountClassifier) treats any exception as "IA indisponível"
/// and falls back to whatever the rule layer suggested.
/// </summary>
public interface IAiClassificationService
{
    Task<AiClassificationResult> ClassifyAsync(
        ClassificationContext context,
        IReadOnlyList<StandardAccountCandidate> candidates,
        CancellationToken cancellationToken);
}

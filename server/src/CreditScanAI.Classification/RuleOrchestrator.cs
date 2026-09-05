using CreditScanAI.Classification.Models;
using CreditScanAI.Classification.Rules;

namespace CreditScanAI.Classification;

public interface IRuleOrchestrator
{
    /// <summary>
    /// Runs every rule in priority order and returns the first confident
    /// match. If none matches, returns a result with no StandardAccountId
    /// and method "UNKNOWN" - the caller should flag that for review.
    /// </summary>
    ClassificationResult Classify(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates);
}

public sealed class RuleOrchestrator : IRuleOrchestrator
{
    private readonly IReadOnlyList<IClassificationRule> _rules;

    public RuleOrchestrator(IEnumerable<IClassificationRule> rules)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
    }

    public ClassificationResult Classify(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return new ClassificationResult(null, 0f, "UNKNOWN", "Nenhuma Conta compatível com o Tipo/Subtipo desta conta no plano de contas.");
        }

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(context, candidates);
            if (result.Matches)
            {
                return new ClassificationResult(result.StandardAccountId, result.Confidence, rule.RuleId, result.Reason);
            }
        }

        return new ClassificationResult(null, 0f, "UNKNOWN", "Nenhuma regra encontrou correspondência com confiança suficiente.");
    }
}

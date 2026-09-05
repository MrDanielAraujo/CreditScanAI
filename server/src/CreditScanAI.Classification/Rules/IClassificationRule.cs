using CreditScanAI.Classification.Models;

namespace CreditScanAI.Classification.Rules;

public interface IClassificationRule
{
    string RuleId { get; }

    /// <summary>Higher runs first.</summary>
    int Priority { get; }

    ClassificationRuleResult Evaluate(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates);
}

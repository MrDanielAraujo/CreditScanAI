using CreditScanAI.Classification.Models;
using CreditScanAI.PdfPipeline.Normalization;

namespace CreditScanAI.Classification.Rules;

/// <summary>Matches when the normalized source name equals a candidate's normalized name exactly.</summary>
public sealed class ExactMatchRule : IClassificationRule
{
    private readonly IAccountNameNormalizer _normalizer;

    public ExactMatchRule(IAccountNameNormalizer normalizer) => _normalizer = normalizer;

    public string RuleId => "EXACT_MATCH";
    public int Priority => 90;

    public ClassificationRuleResult Evaluate(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates)
    {
        var match = candidates.FirstOrDefault(c => _normalizer.Normalize(c.Name) == context.NormalizedName);

        return match is null
            ? new ClassificationRuleResult(false, null, 0f, null)
            : new ClassificationRuleResult(true, match.Id, 0.99f, $"Nome normalizado igual a '{match.Name}'");
    }
}

using CreditScanAI.Classification.Models;
using CreditScanAI.PdfPipeline.Normalization;

namespace CreditScanAI.Classification.Rules;

/// <summary>
/// Fuzzy word-overlap match: normalizes both names, tokenizes into words
/// (dropping Portuguese stopwords), and scores each candidate by the overlap
/// coefficient (shared tokens / smaller token set size). Handles cases like
/// "Fornecedores" matching "Fornecedores e Contas a Pagar" that an exact
/// match would miss.
/// </summary>
public sealed class PatternMatchRule : IClassificationRule
{
    private const float MinimumOverlap = 0.5f;

    private static readonly HashSet<string> Stopwords =
        ["E", "DE", "DA", "DO", "DAS", "DOS", "A", "O", "AS", "OS", "PARA", "EM", "COM", "POR", "NO", "NA"];

    private readonly IAccountNameNormalizer _normalizer;

    public PatternMatchRule(IAccountNameNormalizer normalizer) => _normalizer = normalizer;

    public string RuleId => "PATTERN_MATCH";
    public int Priority => 80;

    public ClassificationRuleResult Evaluate(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates)
    {
        var sourceTokens = Tokenize(context.NormalizedName);
        if (sourceTokens.Count == 0)
        {
            return new ClassificationRuleResult(false, null, 0f, null);
        }

        StandardAccountCandidate? best = null;
        var bestOverlap = 0f;

        foreach (var candidate in candidates)
        {
            var candidateTokens = Tokenize(_normalizer.Normalize(candidate.Name));
            if (candidateTokens.Count == 0)
            {
                continue;
            }

            var intersection = sourceTokens.Intersect(candidateTokens).Count();
            var overlap = (float)intersection / Math.Min(sourceTokens.Count, candidateTokens.Count);

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = candidate;
            }
        }

        if (best is null || bestOverlap < MinimumOverlap)
        {
            return new ClassificationRuleResult(false, null, 0f, null);
        }

        var confidence = 0.6f + 0.25f * bestOverlap;
        return new ClassificationRuleResult(true, best.Id, confidence, $"Sobreposição de palavras com '{best.Name}' ({bestOverlap:P0})");
    }

    private static HashSet<string> Tokenize(string normalizedName) =>
        normalizedName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !Stopwords.Contains(token))
            .ToHashSet();
}

namespace CreditScanAI.Classification.Models;

public sealed record ClassificationRuleResult(bool Matches, Guid? StandardAccountId, float Confidence, string? Reason);

public sealed record ClassificationResult(Guid? StandardAccountId, float Confidence, string Method, string? Evidence);

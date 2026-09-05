namespace CreditScanAI.Classification.Models;

/// <summary>
/// Everything a rule needs to know about the source account being
/// classified. Framework/DB-agnostic on purpose - the caller (Api layer)
/// fetches this from the database and the candidate list separately.
/// </summary>
public sealed record ClassificationContext(
    string SourceAccountName,
    string NormalizedName,
    string? InferredType,
    string? InferredSubtype);

/// <summary>One StandardAccount eligible to be the classification target.</summary>
public sealed record StandardAccountCandidate(Guid Id, string Code, string Name, string? Description);

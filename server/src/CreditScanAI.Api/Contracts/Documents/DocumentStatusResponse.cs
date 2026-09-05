namespace CreditScanAI.Api.Contracts.Documents;

public sealed record DocumentStatusResponse(
    Guid DocumentId,
    string Status,
    DateTime? ExtractionStartedAt,
    DateTime? ExtractionCompletedAt,
    string? ExtractionError);

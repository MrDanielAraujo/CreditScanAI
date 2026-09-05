namespace CreditScanAI.Api.Contracts.Classifications;

public sealed record PendingClassificationDto(
    Guid ClassificationId,
    Guid DocumentId,
    string SourceAccountName,
    string? SuggestedStandardAccountName,
    decimal ConfidenceScore,
    string ClassificationMethod,
    string? Evidence);

public sealed record PendingClassificationsResponse(List<PendingClassificationDto> Items, int TotalCount, bool HasMore);

public sealed record SourceAccountSummaryDto(
    Guid Id,
    string OriginalName,
    string? NormalizedName,
    int HierarchyLevel,
    string? InferredType,
    string? InferredSubtype);

public sealed record StandardAccountSummaryDto(Guid Id, string Code, string Name, string? Description);

public sealed record ClassificationDetailResponse(
    Guid ClassificationId,
    SourceAccountSummaryDto SourceAccount,
    StandardAccountSummaryDto? SuggestedStandardAccount,
    decimal ConfidenceScore,
    string ClassificationMethod,
    string? Evidence,
    string ReviewStatus,
    DateTime CreatedAt);

public sealed record ApproveClassificationRequest(string? Notes);

public sealed record ApproveClassificationResponse(Guid ClassificationId, string ReviewStatus, DateTime ReviewedAt);

public sealed record OverrideClassificationRequest(Guid NewStandardAccountId, string? Reason);

public sealed record OverrideClassificationResponse(
    Guid ClassificationId,
    string ReviewStatus,
    StandardAccountSummaryDto NewStandardAccount,
    DateTime ReviewedAt);

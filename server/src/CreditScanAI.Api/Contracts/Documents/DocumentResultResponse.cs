namespace CreditScanAI.Api.Contracts.Documents;

public sealed record DocumentResultResponse(
    Guid DocumentId,
    string Status,
    IReadOnlyList<PeriodDto> Periods,
    IReadOnlyList<SourceAccountDto> Accounts);

public sealed record PeriodDto(Guid Id, string PeriodType, int Year, int? Quarter, DateOnly EndDate);

public sealed record SourceAccountDto(
    Guid Id,
    Guid? ParentId,
    string OriginalName,
    int HierarchyLevel,
    string? InferredType,
    string? InferredSubtype,
    IReadOnlyList<AccountValueDto> Values);

public sealed record AccountValueDto(Guid? PeriodId, string? RawColumnLabel, decimal? RawValue, int ScaleFactor);

public sealed record DocumentListItemDto(
    Guid Id,
    string FileName,
    Guid CompanyId,
    string CompanyName,
    string DocumentType,
    DateTime UploadDate,
    string ExtractionStatus,
    string ClassificationStatus);

public sealed record DocumentListResponse(List<DocumentListItemDto> Items, int TotalCount, bool HasMore);

public sealed record ReprocessDocumentResponse(Guid DocumentId, string ClassificationStatus);

namespace CreditScanAI.PdfPipeline.Models;

public sealed record ExtractedAccountValue(
    string SourceAccountName,
    string NormalizedAccountName,
    int HierarchyLevel,
    string? InferredType,
    string? InferredSubtype,
    float TypeConfidence,
    float SubtypeConfidence,
    int ColumnIndex,
    DateOnly? Period,
    string? RawColumnLabel,
    decimal? Value,
    int ScaleFactor);

namespace CreditScanAI.PdfPipeline.Models;

public sealed record DetectedPeriod(int ColumnIndex, DateOnly Date, string RawLabel);

/// <summary>
/// A value column whose header did not parse as a period - kept as raw
/// metadata (e.g. a fund/project name) rather than mapped to a Company, per
/// the Fase 2 modeling decision: Company stays a legal entity chosen
/// manually at upload time.
/// </summary>
public sealed record DetectedColumn(int ColumnIndex, string RawLabel);

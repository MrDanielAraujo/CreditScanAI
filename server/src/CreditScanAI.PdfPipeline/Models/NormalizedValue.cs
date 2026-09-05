namespace CreditScanAI.PdfPipeline.Models;

/// <summary>
/// A parsed cell value. <see cref="Value"/> is null for an empty-cell
/// placeholder ("-"), which means "no value reported", not zero.
/// </summary>
public sealed record NormalizedValue(decimal? Value, float Confidence);

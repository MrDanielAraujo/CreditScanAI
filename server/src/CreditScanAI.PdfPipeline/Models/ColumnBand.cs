namespace CreditScanAI.PdfPipeline.Models;

/// <summary>
/// A detected value column, identified by clustering the right-aligned edges
/// of numeric-looking tokens across the page. Financial statements align
/// values by their right edge, so this is a reliable column signature.
/// </summary>
public sealed record ColumnBand(int Index, double CentroidRight, string HeaderText);

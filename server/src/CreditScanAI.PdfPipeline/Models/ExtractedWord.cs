namespace CreditScanAI.PdfPipeline.Models;

/// <summary>
/// A single word extracted from a PDF page, with its position and formatting.
/// Coordinates are in PDF points, origin at the bottom-left of the page.
/// </summary>
public sealed record ExtractedWord(
    string Text,
    int PageNumber,
    double Left,
    double Right,
    double Top,
    double Bottom,
    double FontSize,
    bool IsBold);

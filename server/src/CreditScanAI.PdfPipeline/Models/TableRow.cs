namespace CreditScanAI.PdfPipeline.Models;

/// <summary>
/// One reconstructed row of the statement: a label (leftmost text) plus one
/// cell per detected column band, aligned by <see cref="ColumnBand.Index"/>.
/// </summary>
public sealed class TableRow
{
    public int PageNumber { get; init; }
    public double Top { get; init; }
    public string Label { get; init; } = string.Empty;
    public double LabelIndentation { get; init; }
    public bool LabelIsBold { get; init; }
    public double LabelFontSize { get; init; }
    public IReadOnlyDictionary<int, string> Cells { get; init; } = new Dictionary<int, string>();
}

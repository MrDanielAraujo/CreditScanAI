namespace CreditScanAI.PdfPipeline.Models;

public sealed class ReconstructedTable
{
    public IReadOnlyList<ColumnBand> Columns { get; init; } = Array.Empty<ColumnBand>();
    public IReadOnlyList<TableRow> HeaderRows { get; init; } = Array.Empty<TableRow>();
    public IReadOnlyList<TableRow> DataRows { get; init; } = Array.Empty<TableRow>();
}

using CreditScanAI.PdfPipeline.Extraction;
using CreditScanAI.PdfPipeline.TableReconstruction;
using FluentAssertions;
using Xunit.Abstractions;

namespace CreditScanAI.Tests.PdfPipeline;

public class TableReconstructorTests
{
    // "Patrimônio Líquido" is deliberately excluded here: it's a Passivo
    // *subsection* later in the statement, and "Patrim" alone false-positives
    // on the document title "Balanço Patrimonial".
    private static readonly string[] TypeKeywords = ["ATIVO", "PASSIVO", "RECEITA", "DESPESA"];

    private readonly ITestOutputHelper _output;
    private readonly IPdfWordExtractor _extractor = new PdfWordExtractor();
    private readonly ITableReconstructor _reconstructor = new TableReconstructor();

    public TableReconstructorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Reconstruct_BalanceSheetPage1_DetectsTenColumnsAndKeyRows()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Balanco2Trim2020.pdf"));
        var words = _extractor.ExtractWords(bytes).Where(w => w.PageNumber == 1).ToList();

        var table = _reconstructor.Reconstruct(words, TypeKeywords);

        _output.WriteLine($"Columns: {table.Columns.Count}");
        foreach (var col in table.Columns)
        {
            _output.WriteLine($"  [{col.Index}] centroid={col.CentroidRight:F1} header='{col.HeaderText}'");
        }

        _output.WriteLine($"Header rows: {table.HeaderRows.Count}, Data rows: {table.DataRows.Count}");
        foreach (var row in table.DataRows.Take(15))
        {
            var cells = string.Join(" | ", row.Cells.OrderBy(c => c.Key).Select(c => $"{c.Key}:{c.Value}"));
            _output.WriteLine($"indent={row.LabelIndentation:F0} bold={row.LabelIsBold} '{row.Label}' -> {cells}");
        }

        table.Columns.Should().HaveCount(10);
        table.DataRows.Should().Contain(r => r.Label == "Ativo" && r.LabelIsBold);
        table.DataRows.Should().Contain(r => r.Label == "Caixa e bancos" && r.Cells[8] == "1.067.737,38");
        table.Columns[8].HeaderText.Should().Contain("30/06/2020");
        table.Columns[9].HeaderText.Should().Contain("31/12/2019");
        table.Columns[0].HeaderText.Should().NotContain("APAC");
    }
}

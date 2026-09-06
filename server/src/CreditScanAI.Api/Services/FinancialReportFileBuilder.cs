using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Builds a downloadable PDF or Excel version of a financial statement
/// (Fase 10) - the same (Balanço/DRE/Indicadores) values already shown on
/// the Dashboard (per company) and Consolidação (between companies) pages,
/// packaged as a file a user can save/share/archive. Shared by both,
/// since they already produce the exact same Dictionary<string, decimal>
/// shape.
/// </summary>
public static class FinancialReportFileBuilder
{
    public sealed record ReportData(
        string Title,
        string Subtitle,
        Dictionary<string, decimal> Values,
        bool EquationBalanced,
        decimal EquationVariance);

    public static byte[] BuildPdf(ReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.Title).FontSize(16).Bold();
                    column.Item().Text(data.Subtitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Text(text =>
                    {
                        text.Span("Equação Ativo = Passivo + PL: ");
                        if (data.EquationBalanced)
                        {
                            text.Span("balanceada").FontColor(Colors.Green.Darken2).Bold();
                        }
                        else
                        {
                            text.Span($"desbalanceada (diferença de {FinancialValueDefinitions.Format(data.EquationVariance, FinancialValueFormat.Currency)})")
                                .FontColor(Colors.Red.Darken2).Bold();
                        }
                    });

                    AddSection(column, "Balanço", FinancialValueDefinitions.Balanco, data.Values);
                    AddSection(column, "DRE", FinancialValueDefinitions.Dre, data.Values);
                    AddSection(column, "Indicadores", FinancialValueDefinitions.Indicadores, data.Values);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado por CreditScanAI em ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddSection(
        ColumnDescriptor column, string title, IReadOnlyList<FinancialValueDefinition> definitions, Dictionary<string, decimal> values)
    {
        column.Item().Column(section =>
        {
            section.Item().Text(title).FontSize(13).Bold();

            section.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                foreach (var definition in definitions)
                {
                    var value = values.GetValueOrDefault(definition.Key);
                    table.Cell().Text(definition.Label);
                    table.Cell().AlignRight().Text(FinancialValueDefinitions.Format(value, definition.Format));
                }
            });
        });
    }

    public static byte[] BuildExcel(ReportData data)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Demonstrativo");

        sheet.Cell(1, 1).Value = data.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = data.Subtitle;
        sheet.Cell(3, 1).Value = "Equação Ativo = Passivo + PL:";
        sheet.Cell(3, 2).Value = data.EquationBalanced
            ? "balanceada"
            : $"desbalanceada (diferença de {FinancialValueDefinitions.Format(data.EquationVariance, FinancialValueFormat.Currency)})";

        var row = 5;
        row = AddExcelSection(sheet, row, "Balanço", FinancialValueDefinitions.Balanco, data.Values);
        row = AddExcelSection(sheet, row, "DRE", FinancialValueDefinitions.Dre, data.Values);
        AddExcelSection(sheet, row, "Indicadores", FinancialValueDefinitions.Indicadores, data.Values);

        sheet.Columns(1, 2).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static int AddExcelSection(
        IXLWorksheet sheet, int startRow, string title, IReadOnlyList<FinancialValueDefinition> definitions, Dictionary<string, decimal> values)
    {
        var row = startRow;
        sheet.Cell(row, 1).Value = title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        foreach (var definition in definitions)
        {
            var value = values.GetValueOrDefault(definition.Key);
            sheet.Cell(row, 1).Value = definition.Label;
            sheet.Cell(row, 2).Value = FinancialValueDefinitions.Format(value, definition.Format);
            row++;
        }

        return row + 1;
    }
}

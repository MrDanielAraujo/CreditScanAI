using System.Text.RegularExpressions;
using CreditScanAI.PdfPipeline.Extraction;
using CreditScanAI.PdfPipeline.Hierarchy;
using CreditScanAI.PdfPipeline.Models;
using CreditScanAI.PdfPipeline.Normalization;
using CreditScanAI.PdfPipeline.Periods;
using CreditScanAI.PdfPipeline.TableReconstruction;
using CreditScanAI.PdfPipeline.Validation;

namespace CreditScanAI.PdfPipeline;

public interface IPdfExtractionPipeline
{
    ExtractedFinancialData Process(byte[] pdfBytes);
}

/// <summary>
/// Orchestrates the full Fase 2 pipeline: extract -> reconstruct table -> build
/// hierarchy -> classify Tipo/Subtipo -> detect periods -> normalize -> validate.
/// Native PDFs only - scanned documents (no embedded text layer) are out of
/// scope, see 02_PIPELINE_PDF.md.
/// </summary>
public sealed class PdfExtractionPipeline : IPdfExtractionPipeline
{
    // Narrower than TypeSubtypeDetector's classification keywords: this only
    // has to recognize the FIRST row of the account table, so it deliberately
    // excludes "PATRIMONIO LIQUIDO" (a later Passivo subsection, and a
    // substring of the document title "Balanço Patrimonial").
    private static readonly string[] BodyStartKeywords = ["ATIVO", "PASSIVO", "RECEITA", "DESPESA", "CUSTO"];

    private static readonly Regex ScaleFactorPhrase = new(
        @"EM\s+(MILHARES|MILHOES|MILHÕES|REAIS)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IPdfWordExtractor _wordExtractor;
    private readonly ITableReconstructor _tableReconstructor;
    private readonly IHierarchyBuilder _hierarchyBuilder;
    private readonly ITypeSubtypeDetector _typeSubtypeDetector;
    private readonly IPeriodDetector _periodDetector;
    private readonly IAccountNameNormalizer _nameNormalizer;
    private readonly INumericValueNormalizer _valueNormalizer;
    private readonly IPipelineValidator _validator;

    public PdfExtractionPipeline(
        IPdfWordExtractor wordExtractor,
        ITableReconstructor tableReconstructor,
        IHierarchyBuilder hierarchyBuilder,
        ITypeSubtypeDetector typeSubtypeDetector,
        IPeriodDetector periodDetector,
        IAccountNameNormalizer nameNormalizer,
        INumericValueNormalizer valueNormalizer,
        IPipelineValidator validator)
    {
        _wordExtractor = wordExtractor;
        _tableReconstructor = tableReconstructor;
        _hierarchyBuilder = hierarchyBuilder;
        _typeSubtypeDetector = typeSubtypeDetector;
        _periodDetector = periodDetector;
        _nameNormalizer = nameNormalizer;
        _valueNormalizer = valueNormalizer;
        _validator = validator;
    }

    public ExtractedFinancialData Process(byte[] pdfBytes)
    {
        var words = _wordExtractor.ExtractWords(pdfBytes);
        var table = _tableReconstructor.Reconstruct(words, BodyStartKeywords);

        var hierarchy = _hierarchyBuilder.Build(table.DataRows);
        _typeSubtypeDetector.Classify(hierarchy);

        var (periods, otherColumns) = _periodDetector.Detect(table.Columns);
        var periodByColumn = periods.ToDictionary(p => p.ColumnIndex, p => p.Date);
        var labelByColumn = otherColumns.ToDictionary(c => c.ColumnIndex, c => c.RawLabel);

        var scaleFactor = DetectScaleFactor(words);

        var accountValues = FlattenAccountValues(hierarchy, periodByColumn, labelByColumn, scaleFactor);

        var data = new ExtractedFinancialData
        {
            HierarchicalAccounts = hierarchy,
            DetectedPeriods = periods,
            DetectedColumns = otherColumns,
            AccountValues = accountValues,
            ScaleFactor = scaleFactor
        };

        var validation = _validator.Validate(data);

        return new ExtractedFinancialData
        {
            HierarchicalAccounts = hierarchy,
            DetectedPeriods = periods,
            DetectedColumns = otherColumns,
            AccountValues = accountValues,
            ScaleFactor = scaleFactor,
            ValidationResult = validation,
            OverallConfidence = validation.OverallQualityScore
        };
    }

    private List<ExtractedAccountValue> FlattenAccountValues(
        IReadOnlyList<HierarchicalAccount> roots,
        IReadOnlyDictionary<int, DateOnly> periodByColumn,
        IReadOnlyDictionary<int, string> labelByColumn,
        int scaleFactor)
    {
        var results = new List<ExtractedAccountValue>();
        FlattenRecursive(roots, periodByColumn, labelByColumn, scaleFactor, results);
        return results;
    }

    private void FlattenRecursive(
        IReadOnlyList<HierarchicalAccount> nodes,
        IReadOnlyDictionary<int, DateOnly> periodByColumn,
        IReadOnlyDictionary<int, string> labelByColumn,
        int scaleFactor,
        List<ExtractedAccountValue> results)
    {
        foreach (var node in nodes)
        {
            var normalizedName = _nameNormalizer.Normalize(node.OriginalName);

            foreach (var (columnIndex, cellText) in node.Cells)
            {
                var normalizedValue = _valueNormalizer.Normalize(cellText);
                periodByColumn.TryGetValue(columnIndex, out var period);
                labelByColumn.TryGetValue(columnIndex, out var rawLabel);

                results.Add(new ExtractedAccountValue(
                    SourceAccountName: node.OriginalName,
                    NormalizedAccountName: normalizedName,
                    HierarchyLevel: node.Level,
                    InferredType: node.InferredType,
                    InferredSubtype: node.InferredSubtype,
                    TypeConfidence: node.TypeConfidence,
                    SubtypeConfidence: node.SubtypeConfidence,
                    ColumnIndex: columnIndex,
                    Period: periodByColumn.ContainsKey(columnIndex) ? period : null,
                    RawColumnLabel: rawLabel,
                    Value: normalizedValue.Value,
                    ScaleFactor: scaleFactor));
            }

            FlattenRecursive(node.Children, periodByColumn, labelByColumn, scaleFactor, results);
        }
    }

    private static int DetectScaleFactor(IReadOnlyList<ExtractedWord> words)
    {
        var text = string.Join(" ", words.OrderBy(w => w.PageNumber).ThenByDescending(w => w.Top).ThenBy(w => w.Left).Select(w => w.Text));
        var match = ScaleFactorPhrase.Match(text);

        if (!match.Success)
        {
            return 1;
        }

        return match.Groups[1].Value.ToUpperInvariant() switch
        {
            "MILHARES" => 1_000,
            "MILHOES" or "MILHÕES" => 1_000_000,
            _ => 1
        };
    }
}

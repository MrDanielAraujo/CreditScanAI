using System.Text.RegularExpressions;
using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.TableReconstruction;

public interface ITableReconstructor
{
    /// <summary>
    /// Groups words into rows and columns using their position on the page.
    /// Financial statements right-align values, so columns are detected by
    /// clustering the right edge of numeric-looking tokens; everything left
    /// of the first column is treated as the row's label.
    /// </summary>
    /// <param name="typeHeaderKeywords">
    /// Keywords (e.g. "ATIVO", "PASSIVO", "RECEITA") that mark the first row
    /// of the account table - everything above it is document/column header.
    /// </param>
    ReconstructedTable Reconstruct(IReadOnlyList<ExtractedWord> words, IReadOnlyCollection<string> typeHeaderKeywords);
}

public sealed class TableReconstructor : ITableReconstructor
{
    // Tuned against the real APAC balance sheet/DRE samples: consecutive
    // value columns there sit ~35-58pt apart, so this comfortably separates
    // distinct columns without splitting digits of the same value.
    private const double ColumnGapThreshold = 16.0;
    private const double RowYTolerance = 1.5;
    private const double LabelBoundaryBuffer = 5.0;

    // Some source PDFs render a single number as two adjacent glyph runs with
    // an essentially-zero gap (e.g. "1" then ".274.086,43" for "1.274.086,43").
    // Real word-to-word gaps in this document are ~1.4-1.6pt, so this is safe.
    private const double FragmentMergeGapThreshold = 0.5;

    // Used to decide whether a cell VALUE looks numeric once columns are known.
    private static readonly Regex NumericToken = new(
        @"^\(?-?[0-9]{1,3}(\.[0-9]{3})*(,[0-9]+)?\)?$|^-$",
        RegexOptions.Compiled);

    // Used only to DISCOVER column bands. Stricter than NumericToken: requires
    // a decimal part, so bare numbers/dashes incidental to title text (e.g.
    // "30"/"31" in "Em 30 de Junho... 31 de dezembro...", or a stray "-" in
    // "Cultura - APAC") aren't mistaken for a value column. BRL monetary
    // values in these statements always carry cents; the empty-cell "-"
    // placeholder is still handled later, once bands are known.
    private static readonly Regex BandDiscoveryToken = new(
        @"^\(?-?[0-9]{1,3}(\.[0-9]{3})*,[0-9]+\)?$",
        RegexOptions.Compiled);

    public ReconstructedTable Reconstruct(IReadOnlyList<ExtractedWord> words, IReadOnlyCollection<string> typeHeaderKeywords)
    {
        var rawRows = GroupIntoRows(words);
        if (rawRows.Count == 0)
        {
            return new ReconstructedTable();
        }

        var bands = DetectColumnBands(rawRows);
        if (bands.Count == 0)
        {
            return new ReconstructedTable();
        }

        var bandCentroids = bands.Select(b => b.Centroid).ToList();
        var labelBoundary = bands[0].MinLeft - LabelBoundaryBuffer;
        var rows = rawRows
            .Select(row => BuildTableRow(row, bandCentroids, labelBoundary))
            .Where(row => row.Cells.Count > 0 || !string.IsNullOrWhiteSpace(row.Label))
            .ToList();

        var firstBodyRowIndex = rows.FindIndex(r => MatchesAnyKeyword(r.Label, typeHeaderKeywords));
        var headerRows = firstBodyRowIndex > 0 ? rows.Take(firstBodyRowIndex).ToList() : new List<TableRow>();
        var dataRows = firstBodyRowIndex >= 0 ? rows.Skip(firstBodyRowIndex).ToList() : rows;

        var columns = bandCentroids
            .Select((centroid, index) => new ColumnBand(index, centroid, BuildHeaderText(headerRows, index)))
            .ToList();

        return new ReconstructedTable
        {
            Columns = columns,
            HeaderRows = headerRows,
            DataRows = dataRows
        };
    }

    private static List<List<ExtractedWord>> GroupIntoRows(IReadOnlyList<ExtractedWord> words)
    {
        var rows = new List<List<ExtractedWord>>();

        foreach (var pageWords in words.GroupBy(w => w.PageNumber).OrderBy(g => g.Key))
        {
            var ordered = pageWords.OrderByDescending(w => w.Top).ToList();
            List<ExtractedWord>? currentRow = null;
            double currentBottom = double.NaN;

            foreach (var word in ordered)
            {
                if (currentRow is null || Math.Abs(word.Bottom - currentBottom) > RowYTolerance)
                {
                    currentRow = new List<ExtractedWord>();
                    rows.Add(currentRow);
                    currentBottom = word.Bottom;
                }

                currentRow.Add(word);
            }
        }

        foreach (var row in rows)
        {
            row.Sort((a, b) => a.Left.CompareTo(b.Left));
        }

        return rows.Select(MergeAdjacentFragments).ToList();
    }

    /// <summary>
    /// Rejoins words that are really a single number split by the PDF's text
    /// layout into two touching glyph runs (near-zero gap between them).
    /// </summary>
    private static List<ExtractedWord> MergeAdjacentFragments(List<ExtractedWord> row)
    {
        var merged = new List<ExtractedWord>();

        foreach (var word in row)
        {
            if (merged.Count > 0 && word.Left - merged[^1].Right < FragmentMergeGapThreshold)
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    Text = previous.Text + word.Text,
                    Right = word.Right,
                    Top = Math.Max(previous.Top, word.Top),
                    Bottom = Math.Min(previous.Bottom, word.Bottom)
                };
            }
            else
            {
                merged.Add(word);
            }
        }

        return merged;
    }

    private List<(double Centroid, double MinLeft)> DetectColumnBands(List<List<ExtractedWord>> rows)
    {
        var candidates = rows
            .SelectMany(r => r)
            .Where(w => BandDiscoveryToken.IsMatch(w.Text))
            .OrderBy(w => w.Right)
            .ToList();

        if (candidates.Count == 0)
        {
            return new List<(double, double)>();
        }

        var bands = new List<List<ExtractedWord>> { new() { candidates[0] } };

        for (var i = 1; i < candidates.Count; i++)
        {
            var lastBand = bands[^1];
            if (candidates[i].Right - lastBand[^1].Right <= ColumnGapThreshold)
            {
                lastBand.Add(candidates[i]);
            }
            else
            {
                bands.Add(new List<ExtractedWord> { candidates[i] });
            }
        }

        return bands
            .Select(b => (Centroid: b.Average(w => w.Right), MinLeft: b.Min(w => w.Left)))
            .ToList();
    }

    private static double[] BuildBoundaries(List<double> bandCentroids)
    {
        // boundaries[i] = the upper edge of band i (midpoint to the next centroid).
        var boundaries = new double[bandCentroids.Count];
        for (var i = 0; i < bandCentroids.Count - 1; i++)
        {
            boundaries[i] = (bandCentroids[i] + bandCentroids[i + 1]) / 2.0;
        }
        boundaries[^1] = double.PositiveInfinity;
        return boundaries;
    }

    private static int AssignBand(double right, double[] boundaries)
    {
        for (var i = 0; i < boundaries.Length; i++)
        {
            if (right <= boundaries[i])
            {
                return i;
            }
        }
        return boundaries.Length - 1;
    }

    private static TableRow BuildTableRow(List<ExtractedWord> row, List<double> bandCentroids, double labelBoundary)
    {
        var boundaries = BuildBoundaries(bandCentroids);
        var labelWords = new List<ExtractedWord>();
        var cellWords = new Dictionary<int, List<ExtractedWord>>();

        foreach (var word in row)
        {
            if (word.Right < labelBoundary)
            {
                labelWords.Add(word);
                continue;
            }

            var band = AssignBand(word.Right, boundaries);
            if (!cellWords.TryGetValue(band, out var list))
            {
                list = new List<ExtractedWord>();
                cellWords[band] = list;
            }
            list.Add(word);
        }

        var cells = cellWords.ToDictionary(
            kv => kv.Key,
            kv => string.Join(" ", kv.Value.OrderBy(w => w.Left).Select(w => w.Text)));

        return new TableRow
        {
            PageNumber = row[0].PageNumber,
            Top = row[0].Top,
            Label = string.Join(" ", labelWords.OrderBy(w => w.Left).Select(w => w.Text)),
            LabelIndentation = labelWords.Count > 0 ? labelWords.Min(w => w.Left) : row.Min(w => w.Left),
            LabelIsBold = labelWords.Count > 0 && labelWords.All(w => w.IsBold),
            LabelFontSize = labelWords.Count > 0 ? labelWords.Max(w => w.FontSize) : 0,
            Cells = cells
        };
    }

    private static bool MatchesAnyKeyword(string label, IReadOnlyCollection<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return false;
        }

        return keywords.Any(k => label.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildHeaderText(List<TableRow> headerRows, int bandIndex)
    {
        // Real column-header rows (codes, fund names, period dates) populate
        // several bands at once; a document title line that happens to spill
        // into one band's X-range only ever populates that single cell, so
        // requiring multiple populated cells filters title noise out.
        var parts = headerRows
            .Where(r => r.Cells.Count >= 3 && r.Cells.ContainsKey(bandIndex))
            .Select(r => r.Cells[bandIndex]);

        return string.Join(" ", parts).Trim();
    }
}

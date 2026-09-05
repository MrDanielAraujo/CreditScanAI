using System.Text.RegularExpressions;
using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Periods;

public interface IPeriodDetector
{
    /// <summary>
    /// Splits the reconstructed columns into periods (a parseable date in the
    /// header) and everything else (kept as raw <see cref="DetectedColumn"/>
    /// metadata - see the Fase 2 entity-modeling decision).
    /// </summary>
    (IReadOnlyList<DetectedPeriod> Periods, IReadOnlyList<DetectedColumn> OtherColumns) Detect(IReadOnlyList<ColumnBand> columns);
}

public sealed class PeriodDetector : IPeriodDetector
{
    private static readonly Regex NumericDate = new(
        @"(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, int> MonthNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JAN"] = 1, ["JANEIRO"] = 1,
        ["FEV"] = 2, ["FEVEREIRO"] = 2,
        ["MAR"] = 3, ["MARÇO"] = 3, ["MARCO"] = 3,
        ["ABR"] = 4, ["ABRIL"] = 4,
        ["MAI"] = 5, ["MAIO"] = 5,
        ["JUN"] = 6, ["JUNHO"] = 6,
        ["JUL"] = 7, ["JULHO"] = 7,
        ["AGO"] = 8, ["AGOSTO"] = 8,
        ["SET"] = 9, ["SETEMBRO"] = 9,
        ["OUT"] = 10, ["OUTUBRO"] = 10,
        ["NOV"] = 11, ["NOVEMBRO"] = 11,
        ["DEZ"] = 12, ["DEZEMBRO"] = 12,
    };

    private static readonly Regex MonthYear = new(
        @"([A-Za-zÀ-ÿ]{3,})[/\-\s](\d{2,4})",
        RegexOptions.Compiled);

    public (IReadOnlyList<DetectedPeriod> Periods, IReadOnlyList<DetectedColumn> OtherColumns) Detect(IReadOnlyList<ColumnBand> columns)
    {
        var periods = new List<DetectedPeriod>();
        var others = new List<DetectedColumn>();

        foreach (var column in columns)
        {
            var date = TryParseDate(column.HeaderText);
            if (date.HasValue)
            {
                periods.Add(new DetectedPeriod(column.Index, date.Value, column.HeaderText));
            }
            else
            {
                others.Add(new DetectedColumn(column.Index, column.HeaderText));
            }
        }

        return (periods, others);
    }

    private static DateOnly? TryParseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var numericMatch = NumericDate.Match(text);
        if (numericMatch.Success)
        {
            var day = int.Parse(numericMatch.Groups[1].Value);
            var month = int.Parse(numericMatch.Groups[2].Value);
            var year = int.Parse(numericMatch.Groups[3].Value);

            if (IsValidDate(year, month, day))
            {
                return new DateOnly(year, month, day);
            }
        }

        var monthYearMatch = MonthYear.Match(text);
        if (monthYearMatch.Success && MonthNames.TryGetValue(monthYearMatch.Groups[1].Value, out var monthNumber))
        {
            var yearText = monthYearMatch.Groups[2].Value;
            // 2-digit years ("jun/20") are common in these statements; this
            // system only deals with modern (2000s) financial documents.
            var year = yearText.Length <= 2 ? 2000 + int.Parse(yearText) : int.Parse(yearText);
            var lastDay = DateTime.DaysInMonth(year, monthNumber);
            return new DateOnly(year, monthNumber, lastDay);
        }

        return null;
    }

    private static bool IsValidDate(int year, int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);
}

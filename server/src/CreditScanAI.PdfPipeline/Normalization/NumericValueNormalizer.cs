using System.Text.RegularExpressions;
using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Normalization;

public interface INumericValueNormalizer
{
    /// <summary>
    /// Parses a Brazilian-formatted currency cell ("1.234.567,89", "(1.234,56)"
    /// for negative, or "-" for an empty/not-reported cell).
    /// </summary>
    NormalizedValue Normalize(string cellText);
}

public sealed class NumericValueNormalizer : INumericValueNormalizer
{
    private static readonly Regex ValuePattern = new(
        @"^\((?<inner>[0-9.,]+)\)$|^(?<sign>-)?(?<inner>[0-9.,]+)$",
        RegexOptions.Compiled);

    public NormalizedValue Normalize(string cellText)
    {
        var trimmed = cellText.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed == "-")
        {
            return new NormalizedValue(null, 1.0f);
        }

        var match = ValuePattern.Match(trimmed);
        if (!match.Success)
        {
            return new NormalizedValue(null, 0f);
        }

        var isNegative = trimmed.StartsWith('(') || match.Groups["sign"].Success;
        var inner = match.Groups["inner"].Value;

        // Brazilian format: '.' groups thousands, ',' is the decimal separator.
        var withoutThousands = inner.Replace(".", "");
        var invariant = withoutThousands.Replace(",", ".");

        if (!decimal.TryParse(invariant, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return new NormalizedValue(null, 0f);
        }

        return new NormalizedValue(isNegative ? -parsed : parsed, 0.95f);
    }
}

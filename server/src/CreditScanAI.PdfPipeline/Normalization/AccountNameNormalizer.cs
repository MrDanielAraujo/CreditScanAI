using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CreditScanAI.PdfPipeline.Normalization;

public interface IAccountNameNormalizer
{
    string Normalize(string originalName);
}

public sealed class AccountNameNormalizer : IAccountNameNormalizer
{
    private static readonly Regex MultipleSpaces = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex DisallowedChars = new(@"[^\w\s\-/]", RegexOptions.Compiled);

    public string Normalize(string originalName)
    {
        if (string.IsNullOrWhiteSpace(originalName))
        {
            return string.Empty;
        }

        var withoutDiacritics = RemoveDiacritics(originalName.Trim().ToUpperInvariant());
        var withoutSpecialChars = DisallowedChars.Replace(withoutDiacritics, "");
        return MultipleSpaces.Replace(withoutSpecialChars, " ").Trim();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

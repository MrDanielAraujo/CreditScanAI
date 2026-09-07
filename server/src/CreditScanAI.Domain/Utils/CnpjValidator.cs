namespace CreditScanAI.Domain.Utils;

/// <summary>
/// Validates and formats Brazilian CNPJ numbers (14 digits, 2 check digits computed
/// with the standard modulo-11 weighted-sum algorithm).
/// </summary>
public static class CnpjValidator
{
    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Strips any non-digit characters (mask, spaces) from the input.</summary>
    public static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    /// <summary>True if the input, once stripped of any mask, is a 14-digit CNPJ with valid check digits.</summary>
    public static bool IsValid(string value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 14)
        {
            return false;
        }

        // A run of the same digit repeated 14 times (e.g. "00000000000000") passes the
        // checksum below by construction, but is never a real CNPJ.
        if (digits.Distinct().Count() == 1)
        {
            return false;
        }

        var firstCheck = ComputeCheckDigit(digits[..12], FirstWeights);
        var secondCheck = ComputeCheckDigit(digits[..12] + firstCheck, SecondWeights);

        return digits[12] - '0' == firstCheck && digits[13] - '0' == secondCheck;
    }

    /// <summary>Formats 14 digits as 00.000.000/0000-00. Assumes the input already passed <see cref="IsValid"/>.</summary>
    public static string Format(string value)
    {
        var digits = OnlyDigits(value);
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..14]}";
    }

    /// <summary>
    /// Placeholder Name/Code used when a Company is auto-created from a CNPJ found at
    /// upload time with no matching record and no name extracted from the document.
    /// Kept in one place so the upload flow (that sets it) and the background
    /// extraction job (that later checks for it, to know whether it's still safe to
    /// overwrite with a name found in the PDF) always agree on the exact string.
    /// </summary>
    public static string PlaceholderCompanyName(string value) => $"Empresa {Format(value)}";

    private static int ComputeCheckDigit(string digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}

using CreditScanAI.PdfPipeline.Models;
using UglyToad.PdfPig;

namespace CreditScanAI.PdfPipeline.Extraction;

public interface IPdfWordExtractor
{
    IReadOnlyList<ExtractedWord> ExtractWords(byte[] pdfBytes);
}

/// <summary>
/// Extracts words with position/formatting from a native (non-scanned) PDF using PdfPig.
/// Scanned PDFs (no embedded text layer) are out of scope - see 02_PIPELINE_PDF.md.
/// </summary>
public sealed class PdfWordExtractor : IPdfWordExtractor
{
    public IReadOnlyList<ExtractedWord> ExtractWords(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var words = new List<ExtractedWord>();

        foreach (var page in document.GetPages())
        {
            foreach (var word in page.GetWords())
            {
                var firstLetter = word.Letters.Count > 0 ? word.Letters[0] : null;

                words.Add(new ExtractedWord(
                    Text: word.Text,
                    PageNumber: page.Number,
                    Left: word.BoundingBox.Left,
                    Right: word.BoundingBox.Right,
                    Top: word.BoundingBox.Top,
                    Bottom: word.BoundingBox.Bottom,
                    FontSize: firstLetter?.PointSize ?? 0,
                    IsBold: firstLetter?.FontDetails.IsBold ?? false));
            }
        }

        return words;
    }
}

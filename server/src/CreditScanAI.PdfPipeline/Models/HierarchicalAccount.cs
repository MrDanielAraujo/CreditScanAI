namespace CreditScanAI.PdfPipeline.Models;

public sealed class HierarchicalAccount
{
    public required string OriginalName { get; init; }
    public required int Level { get; init; }
    public required double Indentation { get; init; }
    public bool IsBold { get; init; }
    public HierarchicalAccount? Parent { get; set; }
    public List<HierarchicalAccount> Children { get; } = new();
    public IReadOnlyDictionary<int, string> Cells { get; init; } = new Dictionary<int, string>();

    public string? InferredType { get; set; }
    public string? InferredSubtype { get; set; }
    public float TypeConfidence { get; set; }
    public float SubtypeConfidence { get; set; }

    public IEnumerable<string> AncestorNames()
    {
        var current = Parent;
        while (current is not null)
        {
            yield return current.OriginalName;
            current = current.Parent;
        }
    }
}

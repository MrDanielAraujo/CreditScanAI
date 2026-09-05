using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Hierarchy;

public interface ITypeSubtypeDetector
{
    /// <summary>
    /// Assigns InferredType/InferredSubtype (with a confidence score) to every
    /// node in the tree, walking it top-down so a node with no keyword match
    /// of its own inherits its parent's classification.
    /// </summary>
    void Classify(IReadOnlyList<HierarchicalAccount> roots);
}

public sealed class TypeSubtypeDetector : ITypeSubtypeDetector
{
    // Ordered so a more specific phrase (e.g. "PATRIMONIO LIQUIDO") is tried
    // before a more generic one that could be a substring of something else.
    private static readonly (string Keyword, string Type)[] TypeKeywords =
    [
        ("PATRIMONIO LIQUIDO", "PASSIVO"),
        ("ATIVO", "ATIVO"),
        ("PASSIVO", "PASSIVO"),
        ("RECEITA", "DRE"),
        ("DESPESA", "DRE"),
        ("CUSTO", "DRE"),
        ("SUPERAVIT", "DRE"),
        ("DEFICIT", "DRE")
    ];

    private static readonly (string Keyword, string Subtype)[] SubtypeKeywords =
    [
        ("NAO CIRCULANTE", "NAO_CIRCULANTE"),
        ("NÃO CIRCULANTE", "NAO_CIRCULANTE"),
        ("LONGO PRAZO", "NAO_CIRCULANTE"),
        ("CIRCULANTE", "CIRCULANTE"),
        ("CURTO PRAZO", "CIRCULANTE"),
        ("PERMANENTE", "PERMANENTE"),
        ("PATRIMONIO LIQUIDO", "PL"),
        ("PATRIMÔNIO LÍQUIDO", "PL")
    ];

    public void Classify(IReadOnlyList<HierarchicalAccount> roots)
    {
        foreach (var root in roots)
        {
            ClassifyNode(root, parentType: null, parentSubtype: null);
        }
    }

    private static void ClassifyNode(HierarchicalAccount node, string? parentType, string? parentSubtype)
    {
        var normalizedName = Normalize(node.OriginalName);

        var typeMatch = TypeKeywords.FirstOrDefault(kv => normalizedName.Contains(kv.Keyword));
        var subtypeMatch = SubtypeKeywords.FirstOrDefault(kv => normalizedName.Contains(kv.Keyword));

        if (typeMatch.Type is not null)
        {
            node.InferredType = typeMatch.Type;
            node.TypeConfidence = 0.95f;
        }
        else if (parentType is not null)
        {
            node.InferredType = parentType;
            node.TypeConfidence = 0.7f;
        }

        if (subtypeMatch.Subtype is not null)
        {
            node.InferredSubtype = subtypeMatch.Subtype;
            node.SubtypeConfidence = 0.95f;
        }
        else if (parentSubtype is not null)
        {
            node.InferredSubtype = parentSubtype;
            node.SubtypeConfidence = 0.7f;
        }

        foreach (var child in node.Children)
        {
            ClassifyNode(child, node.InferredType, node.InferredSubtype);
        }
    }

    private static string Normalize(string text) => text.ToUpperInvariant();
}

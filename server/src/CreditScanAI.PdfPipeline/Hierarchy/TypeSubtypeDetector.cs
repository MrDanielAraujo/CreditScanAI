using System.Text.RegularExpressions;
using CreditScanAI.Domain.Enums;
using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Hierarchy;

public interface ITypeSubtypeDetector
{
    /// <summary>
    /// Assigns InferredType/InferredSubtype (with a confidence score) to every
    /// node in the tree, walking it top-down so a node with no keyword match
    /// of its own inherits its parent's classification. documentType seeds
    /// the root nodes: an income-statement document's own top-level sections
    /// are DRE by definition, even when their header text uses a plural form
    /// ("Despesas...") that the keyword list otherwise avoids matching (to
    /// keep balance-sheet lines like "Despesas Antecipadas" safe).
    /// </summary>
    void Classify(IReadOnlyList<HierarchicalAccount> roots, DocumentType documentType);
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

    // Only tried once a node is already known to be DRE (see ClassifyNode) -
    // the plural forms ("RECEITAS", "DESPESAS", "CUSTOS") are how real DRE
    // section headers are actually worded, but matching them unconditionally
    // would misfire on unrelated ATIVO lines like "Despesas Antecipadas".
    private static readonly (string Keyword, string Subtype)[] DreSubtypeKeywords =
    [
        ("RECEITA", "RECEITA"),
        ("RECEITAS", "RECEITA"),
        ("DESPESA", "DESPESA"),
        ("DESPESAS", "DESPESA"),
        ("CUSTO", "CUSTO"),
        ("CUSTOS", "CUSTO"),
        // Depreciação/Amortização são, em si, um tipo de despesa - mas só
        // conta como palavra-chave de Subtipo quando o nó já é DRE (o mesmo
        // termo aparece em contas de ATIVO como "Depreciações acumuladas",
        // que não deve virar DESPESA). Normalize() não remove acentos, então
        // as variantes acentuadas precisam estar listadas explicitamente.
        ("DEPRECIACAO", "DESPESA"),
        ("DEPRECIAÇÃO", "DESPESA"),
        ("DEPRECIACOES", "DESPESA"),
        ("DEPRECIAÇÕES", "DESPESA"),
        ("AMORTIZACAO", "DESPESA"),
        ("AMORTIZAÇÃO", "DESPESA"),
        ("AMORTIZACOES", "DESPESA"),
        ("AMORTIZAÇÕES", "DESPESA")
    ];

    public void Classify(IReadOnlyList<HierarchicalAccount> roots, DocumentType documentType)
    {
        var rootDefaultType = documentType == DocumentType.IncomeStatement ? "DRE" : null;

        foreach (var root in roots)
        {
            ClassifyNode(root, parentType: rootDefaultType, parentSubtype: null);
        }
    }

    private static void ClassifyNode(HierarchicalAccount node, string? parentType, string? parentSubtype)
    {
        var normalizedName = Normalize(node.OriginalName);

        // Whole-word match: a plain substring check would misfire on e.g.
        // "Despesas antecipadas" (an ATIVO/prepaid-expense line, not a DRE
        // entry) just because it contains "DESPESA".
        var typeMatch = TypeKeywords.FirstOrDefault(kv => MatchesWholeWords(normalizedName, kv.Keyword));

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

        var subtypeMatch = SubtypeKeywords.FirstOrDefault(kv => MatchesWholeWords(normalizedName, kv.Keyword));
        if (subtypeMatch.Subtype is null && node.InferredType == "DRE")
        {
            subtypeMatch = DreSubtypeKeywords.FirstOrDefault(kv => MatchesWholeWords(normalizedName, kv.Keyword));
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

    private static bool MatchesWholeWords(string text, string keyword) =>
        Regex.IsMatch(text, $@"\b{Regex.Escape(keyword)}\b");

    private static string Normalize(string text) => text.ToUpperInvariant();
}

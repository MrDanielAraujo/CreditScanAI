using CreditScanAI.Domain.Enums;
using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Hierarchy;

/// <summary>
/// Derives the overall document type (Balanço/DRE/Misto) from an already-classified
/// hierarchy, instead of requiring it as an upload-time input. Walks every node (not
/// just roots, since a branch can be classified without its own root matching a
/// keyword) collecting which of the two keyword groups - ATIVO/PASSIVO vs DRE -
/// appear anywhere in the tree.
/// </summary>
public static class DocumentTypeResolver
{
    public static DocumentType? Resolve(IReadOnlyList<HierarchicalAccount> roots)
    {
        var hasBalanceSheet = false;
        var hasIncomeStatement = false;

        void Walk(HierarchicalAccount node)
        {
            if (node.InferredType is "ATIVO" or "PASSIVO")
            {
                hasBalanceSheet = true;
            }
            else if (node.InferredType is "DRE")
            {
                hasIncomeStatement = true;
            }

            foreach (var child in node.Children)
            {
                Walk(child);
            }
        }

        foreach (var root in roots)
        {
            Walk(root);
        }

        if (hasBalanceSheet && hasIncomeStatement) return DocumentType.Mixed;
        if (hasBalanceSheet) return DocumentType.BalanceSheet;
        if (hasIncomeStatement) return DocumentType.IncomeStatement;
        return null;
    }
}

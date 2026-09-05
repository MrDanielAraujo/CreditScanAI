using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Hierarchy;

public interface IHierarchyBuilder
{
    /// <summary>
    /// Reconstructs the Tipo -> Subtipo -> Conta tree from row indentation:
    /// a row becomes the child of the closest preceding row with a smaller
    /// indentation (the classic stack-based approach for indentation-based
    /// outlines).
    /// </summary>
    IReadOnlyList<HierarchicalAccount> Build(IReadOnlyList<TableRow> dataRows);
}

public sealed class HierarchyBuilder : IHierarchyBuilder
{
    public IReadOnlyList<HierarchicalAccount> Build(IReadOnlyList<TableRow> dataRows)
    {
        var roots = new List<HierarchicalAccount>();
        var stack = new List<HierarchicalAccount>();

        foreach (var row in dataRows)
        {
            if (string.IsNullOrWhiteSpace(row.Label))
            {
                // A row with no label (e.g. a bare subtotal line) attaches to
                // whatever section is currently open, contributing values but
                // not a new hierarchy node of its own.
                continue;
            }

            while (stack.Count > 0 && stack[^1].Indentation >= row.LabelIndentation)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            var parent = stack.Count > 0 ? stack[^1] : null;

            var node = new HierarchicalAccount
            {
                OriginalName = row.Label,
                Level = parent is null ? 0 : parent.Level + 1,
                Indentation = row.LabelIndentation,
                IsBold = row.LabelIsBold,
                Cells = row.Cells,
                Parent = parent
            };

            if (parent is null)
            {
                roots.Add(node);
            }
            else
            {
                parent.Children.Add(node);
            }

            stack.Add(node);
        }

        return roots;
    }
}

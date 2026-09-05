using CreditScanAI.PdfPipeline.Models;

namespace CreditScanAI.PdfPipeline.Validation;

public interface IPipelineValidator
{
    PipelineValidationResult Validate(ExtractedFinancialData data);
}

public sealed class PipelineValidator : IPipelineValidator
{
    public PipelineValidationResult Validate(ExtractedFinancialData data)
    {
        var result = new PipelineValidationResult();

        if (data.DetectedPeriods.Count == 0)
        {
            result.Errors.Add(new ValidationError("NO_PERIODS", "Nenhum período foi detectado no documento."));
        }

        if (data.HierarchicalAccounts.Count == 0)
        {
            result.Errors.Add(new ValidationError("NO_ACCOUNTS", "Nenhuma conta foi detectada no documento."));
        }

        var valuesWithoutPeriodMapping = data.AccountValues.Count(v => v.Period is null && v.RawColumnLabel is null);
        if (valuesWithoutPeriodMapping > 0)
        {
            result.Warnings.Add(new ValidationWarning(
                "MISSING_COLUMN_LABEL",
                $"{valuesWithoutPeriodMapping} valores sem período ou rótulo de coluna associado."));
        }

        var lowConfidenceTypes = CountLeaves(data.HierarchicalAccounts, a => a.InferredType is null);
        if (lowConfidenceTypes > 0)
        {
            result.Warnings.Add(new ValidationWarning(
                "UNCLASSIFIED_TYPE",
                $"{lowConfidenceTypes} contas sem Tipo inferido."));
        }

        result.OverallQualityScore = CalculateQualityScore(data);
        return result;
    }

    private static int CountLeaves(IReadOnlyList<HierarchicalAccount> nodes, Func<HierarchicalAccount, bool> predicate)
    {
        var count = 0;
        foreach (var node in nodes)
        {
            if (node.Children.Count == 0 && predicate(node))
            {
                count++;
            }
            count += CountLeaves(node.Children, predicate);
        }
        return count;
    }

    private static float CalculateQualityScore(ExtractedFinancialData data)
    {
        var score = 0.5f;

        if (data.DetectedPeriods.Count > 0)
        {
            score += 0.2f;
        }

        if (data.HierarchicalAccounts.Count > 0)
        {
            score += 0.2f;
        }

        if (data.AccountValues.Any(v => v.Value.HasValue))
        {
            score += 0.1f;
        }

        return Math.Min(score, 1.0f);
    }
}

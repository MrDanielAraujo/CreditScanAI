namespace CreditScanAI.PdfPipeline.Validation;

public sealed record ValidationError(string Code, string Message);

public sealed record ValidationWarning(string Code, string Message);

public sealed class PipelineValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; } = new();
    public List<ValidationWarning> Warnings { get; } = new();
    public float OverallQualityScore { get; set; }
}

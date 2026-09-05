namespace CreditScanAI.Classification.Models;

/// <summary>Result of asking the local AI model to classify one source account.</summary>
public sealed record AiClassificationResult(Guid StandardAccountId, float Confidence, string Reasoning);

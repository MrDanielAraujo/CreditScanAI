namespace CreditScanAI.Classification.Ai;

/// <summary>Bound from the "Ollama" configuration section.</summary>
public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:7b-instruct";
    public int TimeoutSeconds { get; set; } = 90;
}

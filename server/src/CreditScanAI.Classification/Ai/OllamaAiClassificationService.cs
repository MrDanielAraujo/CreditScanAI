using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreditScanAI.Classification.Models;
using Microsoft.Extensions.Options;

namespace CreditScanAI.Classification.Ai;

/// <summary>
/// Semantic classification fallback backed by a local Ollama model
/// (qwen2.5:7b-instruct por padrão) - nenhuma chamada a API externa, pois
/// rodar um modelo local é premissa do projeto (não usamos Claude/qualquer
/// LLM na nuvem para classificar dados financeiros do cliente).
/// </summary>
public sealed class OllamaAiClassificationService : IAiClassificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Força o Ollama a retornar um JSON com exatamente estes campos, em vez
    // de confiar apenas em instrução textual no prompt.
    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            selected_index = new { type = "integer" },
            confidence = new { type = "number" },
            reasoning = new { type = "string" }
        },
        required = new[] { "selected_index", "confidence", "reasoning" }
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaAiClassificationService(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AiClassificationResult> ClassifyAsync(
        ClassificationContext context,
        IReadOnlyList<StandardAccountCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Não é possível classificar com IA sem nenhuma conta candidata.");
        }

        var request = new
        {
            model = _options.Model,
            stream = false,
            messages = new[] { new { role = "user", content = BuildPrompt(context, candidates) } },
            format = ResponseSchema
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia do Ollama.");

        var content = payload.Message?.Content
            ?? throw new InvalidOperationException("Resposta do Ollama sem conteúdo de mensagem.");

        var parsed = JsonSerializer.Deserialize<AiClassificationPayload>(content, JsonOptions)
            ?? throw new InvalidOperationException("Falha ao interpretar o JSON retornado pelo Ollama.");

        var index = parsed.SelectedIndex - 1;
        if (index < 0 || index >= candidates.Count)
        {
            throw new InvalidOperationException(
                $"Índice selecionado pela IA ({parsed.SelectedIndex}) fora do intervalo de candidatos (1-{candidates.Count}).");
        }

        return new AiClassificationResult(candidates[index].Id, parsed.Confidence, parsed.Reasoning);
    }

    private static string BuildPrompt(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates)
    {
        var candidatesList = string.Join('\n', candidates.Select((c, i) =>
            $"{i + 1}. {c.Code}: {c.Name}" + (string.IsNullOrWhiteSpace(c.Description) ? "" : $" ({c.Description})")));

        return $"""
            Você é um especialista em contabilidade brasileira. Classifique a conta de
            origem abaixo, escolhendo a conta padrão candidata que melhor corresponde a
            ela semanticamente.

            CONTA DE ORIGEM:
            - Nome: {context.SourceAccountName}
            - Tipo: {context.InferredType}
            - Subtipo: {context.InferredSubtype}

            CONTAS PADRÃO CANDIDATAS:
            {candidatesList}

            Escolha o índice (1-{candidates.Count}) da melhor candidata, indique sua
            confiança de 0.0 a 1.0 e justifique brevemente.
            """;
    }

    private sealed record OllamaChatResponse([property: JsonPropertyName("message")] OllamaChatMessage? Message);

    private sealed record OllamaChatMessage([property: JsonPropertyName("content")] string? Content);

    private sealed record AiClassificationPayload(
        [property: JsonPropertyName("selected_index")] int SelectedIndex,
        [property: JsonPropertyName("confidence")] float Confidence,
        [property: JsonPropertyName("reasoning")] string Reasoning);
}

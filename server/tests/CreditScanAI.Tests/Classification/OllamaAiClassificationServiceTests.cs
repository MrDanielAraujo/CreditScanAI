using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace CreditScanAI.Tests.Classification;

/// <summary>
/// Integration tests against a real, locally running Ollama instance
/// (qwen2.5:7b-instruct). Every other classification test uses a fake
/// IAiClassificationService for determinism and speed - these are the
/// only ones that talk to the actual model.
///
/// xUnit 2.x has no runtime "Skip" (unlike v3's Assert.Skip), so instead
/// of failing the build on a machine where Ollama isn't running (CI, a
/// fresh dev machine), each test probes reachability first and simply
/// returns - logging why - when it's unavailable.
/// </summary>
public class OllamaAiClassificationServiceTests
{
    private const string BaseUrl = "http://localhost:11434";

    private readonly ITestOutputHelper _output;
    public OllamaAiClassificationServiceTests(ITestOutputHelper output) => _output = output;

    private async Task<bool> IsOllamaAvailableAsync()
    {
        try
        {
            using var probe = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(2) };
            var response = await probe.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static OllamaAiClassificationService BuildService() => new(
        new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(90) },
        Options.Create(new OllamaOptions()));

    [Fact]
    public async Task ClassifyAsync_RealAmbiguousAccountPair_PicksTheSemanticCorrectCandidate()
    {
        if (!await IsOllamaAvailableAsync())
        {
            _output.WriteLine($"Ollama não está rodando em {BaseUrl} - teste ignorado.");
            return;
        }

        var service = BuildService();
        var context = new ClassificationContext("Salários a pagar", "SALARIOS A PAGAR", "PASSIVO", "CIRCULANTE", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        List<StandardAccountCandidate> candidates =
        [
            new(Guid.NewGuid(), "IMPOSTOS", "Impostos a Pagar", null),
            new(Guid.NewGuid(), "SALENC", "Salários e Encargos a Pagar", null),
            new(Guid.NewGuid(), "FORN", "Fornecedores", null),
        ];

        var result = await service.ClassifyAsync(context, candidates, CancellationToken.None);
        _output.WriteLine($"IA respondeu: standardAccountId={result.StandardAccountId}, confidence={result.Confidence}, reasoning={result.Reasoning}");

        result.StandardAccountId.Should().Be(candidates[1].Id);
        result.Confidence.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public async Task ClassifyAsync_ClearMatch_ReturnsTheObviousCandidate()
    {
        if (!await IsOllamaAvailableAsync())
        {
            _output.WriteLine($"Ollama não está rodando em {BaseUrl} - teste ignorado.");
            return;
        }

        var service = BuildService();
        var context = new ClassificationContext("Fornecedores nacionais", "FORNECEDORES NACIONAIS", "PASSIVO", "CIRCULANTE", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        List<StandardAccountCandidate> candidates =
        [
            new(Guid.NewGuid(), "FORN", "Fornecedores", null),
            new(Guid.NewGuid(), "IMPOSTOS", "Impostos a Pagar", null),
        ];

        var result = await service.ClassifyAsync(context, candidates, CancellationToken.None);
        _output.WriteLine($"IA respondeu: standardAccountId={result.StandardAccountId}, confidence={result.Confidence}, reasoning={result.Reasoning}");

        result.StandardAccountId.Should().Be(candidates[0].Id);
    }
}

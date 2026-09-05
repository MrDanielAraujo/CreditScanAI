using CreditScanAI.Classification;
using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Models;
using CreditScanAI.Classification.Rules;
using CreditScanAI.PdfPipeline.Normalization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditScanAI.Tests.Classification;

/// <summary>
/// Unit tests for AccountClassifier's orchestration logic (rules first,
/// AI as fallback/second opinion, graceful degradation on AI failure) in
/// isolation - no database, no real Ollama.
/// </summary>
public class AccountClassifierTests
{
    private static readonly StandardAccountCandidate CaixaCandidate =
        new(Guid.NewGuid(), "CAIXA", "Caixa e Equivalentes de Caixa", null);

    private sealed class StubAiClassificationService : IAiClassificationService
    {
        private readonly Func<AiClassificationResult> _respond;
        public StubAiClassificationService(Func<AiClassificationResult> respond) => _respond = respond;

        public Task<AiClassificationResult> ClassifyAsync(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates, CancellationToken cancellationToken)
            => Task.FromResult(_respond());
    }

    private sealed class ThrowingAiClassificationService : IAiClassificationService
    {
        public Task<AiClassificationResult> ClassifyAsync(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates, CancellationToken cancellationToken)
            => throw new HttpRequestException("Ollama indisponível (stub de teste)");
    }

    private static AccountClassifier BuildClassifier(IAiClassificationService aiService) => new(
        new RuleOrchestrator([new ExactMatchRule(new AccountNameNormalizer()), new PatternMatchRule(new AccountNameNormalizer())]),
        aiService,
        NullLogger<AccountClassifier>.Instance);

    [Fact]
    public async Task ClassifyAsync_ExactMatch_ReturnsRuleResultWithoutCallingAi()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = new ClassificationContext("Caixa e Equivalentes de Caixa", "CAIXA E EQUIVALENTES DE CAIXA", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("EXACT_MATCH");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
        result.Confidence.Should().Be(0.99f);
    }

    [Fact]
    public async Task ClassifyAsync_LowConfidenceRuleMatch_ConsultsAiAndUsesItsAnswer()
    {
        var aiResult = new AiClassificationResult(CaixaCandidate.Id, 0.92f, "IA concorda");
        var classifier = BuildClassifier(new StubAiClassificationService(() => aiResult));
        var context = new ClassificationContext("Caixa e bancos", "CAIXA E BANCOS", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
        result.Confidence.Should().Be(0.92f);
    }

    [Fact]
    public async Task ClassifyAsync_NoRuleMatchAtAll_AlsoConsultsAi()
    {
        var aiResult = new AiClassificationResult(CaixaCandidate.Id, 0.8f, "IA escolheu por semântica");
        var classifier = BuildClassifier(new StubAiClassificationService(() => aiResult));
        var context = new ClassificationContext("Conta totalmente diferente", "CONTA TOTALMENTE DIFERENTE", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
    }

    [Fact]
    public async Task ClassifyAsync_NoCandidates_ReturnsUnknownWithoutCallingAi()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = new ClassificationContext("Qualquer conta", "QUALQUER CONTA", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [], CancellationToken.None);

        result.Method.Should().Be("UNKNOWN");
        result.StandardAccountId.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_AiThrows_FallsBackToRuleSuggestionAsAiUnavailable()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = new ClassificationContext("Caixa e bancos", "CAIXA E BANCOS", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI_UNAVAILABLE");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
    }

    [Fact]
    public async Task ClassifyAsync_AiThrowsAndNoRuleMatched_ReturnsAiUnavailableWithNoSuggestion()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = new ClassificationContext("Conta totalmente diferente", "CONTA TOTALMENTE DIFERENTE", "ATIVO", "CIRCULANTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI_UNAVAILABLE");
        result.StandardAccountId.Should().BeNull();
    }
}

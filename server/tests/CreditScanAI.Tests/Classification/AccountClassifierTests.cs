using CreditScanAI.Classification;
using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Models;
using CreditScanAI.Classification.Rules;
using CreditScanAI.PdfPipeline.Normalization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditScanAI.Tests.Classification;

/// <summary>
/// Unit tests for AccountClassifier's orchestration logic (histórico
/// primeiro, depois regras, depois IA como fallback/segunda opinião,
/// degradação graciosa em falha da IA) em isolamento - sem banco, sem
/// Ollama real.
/// </summary>
public class AccountClassifierTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid DocumentId = Guid.NewGuid();

    private static readonly StandardAccountCandidate CaixaCandidate =
        new(Guid.NewGuid(), "CAIXA", "Caixa e Equivalentes de Caixa", null);

    private static ClassificationContext ContextFor(string sourceName, string normalizedName, string? type = "ATIVO", string? subtype = "CIRCULANTE") =>
        new(sourceName, normalizedName, type, subtype, CompanyId, DocumentId);

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

    /// <summary>Nunca encontra decisão histórica - usado pela maioria dos testes, que não são sobre a Camada 4.</summary>
    private sealed class NoHistoryProvider : IClassificationHistoryProvider
    {
        public Task<HistoricalClassification?> FindPreviousDecisionAsync(Guid companyId, Guid excludeDocumentId, string normalizedSourceAccountName, CancellationToken cancellationToken)
            => Task.FromResult<HistoricalClassification?>(null);
    }

    private sealed class StubHistoryProvider : IClassificationHistoryProvider
    {
        private readonly HistoricalClassification _result;
        public StubHistoryProvider(HistoricalClassification result) => _result = result;

        public Task<HistoricalClassification?> FindPreviousDecisionAsync(Guid companyId, Guid excludeDocumentId, string normalizedSourceAccountName, CancellationToken cancellationToken)
            => Task.FromResult<HistoricalClassification?>(_result);
    }

    private static AccountClassifier BuildClassifier(IAiClassificationService aiService, IClassificationHistoryProvider? historyProvider = null) => new(
        new RuleOrchestrator([new ExactMatchRule(new AccountNameNormalizer()), new PatternMatchRule(new AccountNameNormalizer())]),
        aiService,
        historyProvider ?? new NoHistoryProvider(),
        NullLogger<AccountClassifier>.Instance);

    [Fact]
    public async Task ClassifyAsync_PreviousApprovedDecisionForSameCompany_UsesItWithoutTouchingRulesOrAi()
    {
        var historical = new HistoricalClassification(CaixaCandidate.Id, CaixaCandidate.Name, DateTime.UtcNow.AddDays(-30));
        var classifier = BuildClassifier(new ThrowingAiClassificationService(), new StubHistoryProvider(historical));
        var context = ContextFor("Caixa e bancos", "CAIXA E BANCOS");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("HISTORICAL_DECISION");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
        result.Confidence.Should().Be(0.98f);
    }

    [Fact]
    public async Task ClassifyAsync_ExactMatch_ReturnsRuleResultWithoutCallingAi()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = ContextFor("Caixa e Equivalentes de Caixa", "CAIXA E EQUIVALENTES DE CAIXA");

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
        var context = ContextFor("Caixa e bancos", "CAIXA E BANCOS");

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
        var context = ContextFor("Conta totalmente diferente", "CONTA TOTALMENTE DIFERENTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
    }

    [Fact]
    public async Task ClassifyAsync_NoCandidates_ReturnsUnknownWithoutCallingAi()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = ContextFor("Qualquer conta", "QUALQUER CONTA");

        var result = await classifier.ClassifyAsync(context, [], CancellationToken.None);

        result.Method.Should().Be("UNKNOWN");
        result.StandardAccountId.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_AiThrows_FallsBackToRuleSuggestionAsAiUnavailable()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = ContextFor("Caixa e bancos", "CAIXA E BANCOS");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI_UNAVAILABLE");
        result.StandardAccountId.Should().Be(CaixaCandidate.Id);
    }

    [Fact]
    public async Task ClassifyAsync_AiThrowsAndNoRuleMatched_ReturnsAiUnavailableWithNoSuggestion()
    {
        var classifier = BuildClassifier(new ThrowingAiClassificationService());
        var context = ContextFor("Conta totalmente diferente", "CONTA TOTALMENTE DIFERENTE");

        var result = await classifier.ClassifyAsync(context, [CaixaCandidate], CancellationToken.None);

        result.Method.Should().Be("AI_UNAVAILABLE");
        result.StandardAccountId.Should().BeNull();
    }
}

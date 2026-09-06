using CreditScanAI.Classification;
using CreditScanAI.Classification.Models;
using CreditScanAI.Classification.Rules;
using CreditScanAI.PdfPipeline.Normalization;
using FluentAssertions;

namespace CreditScanAI.Tests.Classification;

public class RuleOrchestratorTests
{
    private readonly IAccountNameNormalizer _normalizer = new AccountNameNormalizer();
    private readonly IRuleOrchestrator _orchestrator;

    private readonly Guid _caixaId = Guid.NewGuid();
    private readonly Guid _fornecedoresId = Guid.NewGuid();
    private readonly Guid _aplicacoesId = Guid.NewGuid();

    public RuleOrchestratorTests()
    {
        IClassificationRule[] rules = [new ExactMatchRule(_normalizer), new PatternMatchRule(_normalizer)];
        _orchestrator = new RuleOrchestrator(rules);
    }

    private List<StandardAccountCandidate> RealSeedCandidates() =>
    [
        new(_caixaId, "ATIVO_CIRC_CAIXA", "Caixa e Equivalentes de Caixa", null),
        new(_fornecedoresId, "PASSIVO_CIRC_FORNECEDORES", "Fornecedores", null),
        new(_aplicacoesId, "ATIVO_CIRC_APLIC_FIN", "Aplicações Financeiras", null),
    ];

    // Regras não usam CompanyId/DocumentId (só a camada de Histórico usa) -
    // valores arbitrários bastam aqui.
    private ClassificationContext ContextFor(string sourceName) =>
        new(sourceName, _normalizer.Normalize(sourceName), "ATIVO", "CIRCULANTE", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Classify_NoCandidates_ReturnsUnknown()
    {
        var result = _orchestrator.Classify(ContextFor("Caixa e bancos"), []);

        result.StandardAccountId.Should().BeNull();
        result.Method.Should().Be("UNKNOWN");
    }

    [Fact]
    public void Classify_ExactNormalizedMatch_UsesExactMatchRule()
    {
        var result = _orchestrator.Classify(ContextFor("Aplicações Financeiras"), RealSeedCandidates());

        result.StandardAccountId.Should().Be(_aplicacoesId);
        result.Method.Should().Be("EXACT_MATCH");
        result.Confidence.Should().BeGreaterThan(0.95f);
    }

    [Fact]
    public void Classify_PartialNameFromRealDocument_FallsBackToPatternMatch()
    {
        // "Caixa e bancos" is exactly how it appears in the real balance
        // sheet PDF - not identical to the standard account's fuller name.
        var result = _orchestrator.Classify(ContextFor("Caixa e bancos"), RealSeedCandidates());

        result.StandardAccountId.Should().Be(_caixaId);
        result.Method.Should().Be("PATTERN_MATCH");
        result.Confidence.Should().BeInRange(0.6f, 0.9f);
    }

    [Fact]
    public void Classify_FornecedoresEContasAPagar_MatchesFornecedoresViaPattern()
    {
        var result = _orchestrator.Classify(ContextFor("Fornecedores e contas a pagar"), RealSeedCandidates());

        result.StandardAccountId.Should().Be(_fornecedoresId);
        result.Method.Should().Be("PATTERN_MATCH");
    }

    [Fact]
    public void Classify_SalariosAPagar_PrefersSalariosOverImpostos()
    {
        // Regressão da Fase 3 Parte 2: "Salários a pagar" normalizado sem
        // remover acentos ("SALÁRIOS...") empatava com "Impostos a Pagar"
        // por sobreposição de tokens, em vez de escolher o candidato
        // correto. Este teste isola a regra em si (sem passar pela IA),
        // usando o normalizador real - se o bug reaparecer aqui, é porque a
        // normalização voltou a divergir.
        var impostosId = Guid.NewGuid();
        var salariosId = Guid.NewGuid();
        List<StandardAccountCandidate> candidates =
        [
            new(impostosId, "IMPOSTOS", "Impostos a Pagar", null),
            new(salariosId, "SALENC", "Salários e Encargos a Pagar", null),
            new(_fornecedoresId, "FORN", "Fornecedores", null),
        ];

        var context = ContextFor("Salários a pagar") with { InferredType = "PASSIVO", InferredSubtype = "CIRCULANTE" };
        var result = _orchestrator.Classify(context, candidates);

        result.StandardAccountId.Should().Be(salariosId);
        result.Method.Should().Be("PATTERN_MATCH");
    }

    [Fact]
    public void Classify_CompletelyUnrelatedName_ReturnsUnknown()
    {
        var result = _orchestrator.Classify(ContextFor("Xyzabc Totally Unrelated"), RealSeedCandidates());

        result.StandardAccountId.Should().BeNull();
        result.Method.Should().Be("UNKNOWN");
    }
}

using CreditScanAI.Api.Services;
using CreditScanAI.Classification;
using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Models;
using CreditScanAI.Classification.Rules;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.PdfPipeline;
using CreditScanAI.PdfPipeline.Extraction;
using CreditScanAI.PdfPipeline.Hierarchy;
using CreditScanAI.PdfPipeline.Normalization;
using CreditScanAI.PdfPipeline.Periods;
using CreditScanAI.PdfPipeline.TableReconstruction;
using CreditScanAI.PdfPipeline.Validation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditScanAI.Tests.Services;

public class ClassificationProcessingServiceTests
{
    private static async Task<(AppDbContext Db, Guid TenantId, Guid DocumentId)> ExtractRealBalanceSheetAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = "C1", Name = "Company 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var documentId = Guid.NewGuid();
        db.Documents.Add(new Document
        {
            Id = documentId,
            TenantId = tenantId,
            CompanyId = companyId,
            DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow,
            FileName = "Balanco2Trim2020.pdf",
            FilePath = "irrelevant.pdf",
            ExtractionStatus = ExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var pdfBytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "Balanco2Trim2020.pdf"));
        var pipeline = new PdfExtractionPipeline(
            new PdfWordExtractor(),
            new TableReconstructor(),
            new HierarchyBuilder(),
            new TypeSubtypeDetector(),
            new PeriodDetector(),
            new AccountNameNormalizer(),
            new NumericValueNormalizer(),
            new PipelineValidator());

        var extractionService = new DocumentProcessingService(
            db,
            new FakeDocumentStorage(pdfBytes),
            pipeline,
            new NumericValueNormalizer(),
            new AccountNameNormalizer(),
            new ClassificationProcessingQueue(),
            NullLogger<DocumentProcessingService>.Instance);

        await extractionService.ProcessAsync(documentId, CancellationToken.None);

        return (db, tenantId, documentId);
    }

    private static async Task<(AccountType Ativo, AccountSubtype Circulante, ChartOfAccounts Chart, StandardAccount CaixaAccount)> SeedChartAsync(AppDbContext db, Guid tenantId)
    {
        var ativo = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "ATIVO", Name = "Ativo", CreatedAt = DateTime.UtcNow };
        var circulante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = ativo.Id, Code = "CIRCULANTE", Name = "Circulante", CreatedAt = DateTime.UtcNow };
        db.AccountTypes.Add(ativo);
        db.AccountSubtypes.Add(circulante);
        db.TypeSubtypeCompatibilities.Add(new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = ativo.Id, AccountSubtypeId = circulante.Id, IsAllowed = true });

        var chart = new ChartOfAccounts { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Plano Teste", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChartOfAccounts.Add(chart);

        var caixaAccount = new StandardAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChartOfAccountsId = chart.Id,
            AccountTypeId = ativo.Id,
            AccountSubtypeId = circulante.Id,
            Code = "CAIXA",
            Name = "Caixa e Equivalentes de Caixa",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(caixaAccount);

        await db.SaveChangesAsync();
        return (ativo, circulante, chart, caixaAccount);
    }

    private static async Task<(ChartOfAccounts Chart, StandardAccount SalariosAccount)> SeedPassivoChartWithSalariosAsync(AppDbContext db, Guid tenantId)
    {
        var passivo = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "PASSIVO", Name = "Passivo", CreatedAt = DateTime.UtcNow };
        var circulante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = passivo.Id, Code = "CIRCULANTE", Name = "Circulante", CreatedAt = DateTime.UtcNow };
        db.AccountTypes.Add(passivo);
        db.AccountSubtypes.Add(circulante);
        db.TypeSubtypeCompatibilities.Add(new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = passivo.Id, AccountSubtypeId = circulante.Id, IsAllowed = true });

        var chart = new ChartOfAccounts { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Plano Teste Passivo", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChartOfAccounts.Add(chart);

        // Both compatible so the rule has to actually pick the better match,
        // not just the only option - this is the exact real-world pair that
        // exposed the accent-normalization bug (see the test below).
        var impostosAccount = new StandardAccount { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chart.Id, AccountTypeId = passivo.Id, AccountSubtypeId = circulante.Id, Code = "IMPOSTOS", Name = "Impostos a Pagar", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var salariosAccount = new StandardAccount { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chart.Id, AccountTypeId = passivo.Id, AccountSubtypeId = circulante.Id, Code = "SALARIOS", Name = "Salários e Encargos a Pagar", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.StandardAccounts.AddRange(impostosAccount, salariosAccount);

        await db.SaveChangesAsync();
        return (chart, salariosAccount);
    }

    /// <summary>
    /// Always agrees with whatever candidate is passed in, so tests that
    /// don't care about the AI layer's answer (only that the pipeline
    /// completes and persists something) can stay simple.
    /// </summary>
    private sealed class StubAiClassificationService : IAiClassificationService
    {
        private readonly Guid _standardAccountId;
        private readonly float _confidence;
        private readonly string _reasoning;

        public StubAiClassificationService(Guid standardAccountId, float confidence, string reasoning = "IA concorda (stub de teste)")
        {
            _standardAccountId = standardAccountId;
            _confidence = confidence;
            _reasoning = reasoning;
        }

        public Task<AiClassificationResult> ClassifyAsync(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates, CancellationToken cancellationToken)
            => Task.FromResult(new AiClassificationResult(_standardAccountId, _confidence, _reasoning));
    }

    /// <summary>Simulates the Ollama service being unreachable.</summary>
    private sealed class UnreachableAiClassificationService : IAiClassificationService
    {
        public Task<AiClassificationResult> ClassifyAsync(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates, CancellationToken cancellationToken)
            => throw new HttpRequestException("Ollama indisponível (stub de teste)");
    }

    /// <summary>Fails the test loudly if the AI layer is invoked when it shouldn't be.</summary>
    private sealed class NeverCalledAiClassificationService : IAiClassificationService
    {
        public Task<AiClassificationResult> ClassifyAsync(ClassificationContext context, IReadOnlyList<StandardAccountCandidate> candidates, CancellationToken cancellationToken)
            => throw new InvalidOperationException("A IA não deveria ter sido chamada neste cenário.");
    }

    private static ClassificationProcessingService BuildService(AppDbContext db, IAiClassificationService aiService) => new(
        db,
        new AccountClassifier(
            new RuleOrchestrator([new ExactMatchRule(new AccountNameNormalizer()), new PatternMatchRule(new AccountNameNormalizer())]),
            aiService,
            NullLogger<AccountClassifier>.Instance),
        new AccountNameNormalizer(),
        NullLogger<ClassificationProcessingService>.Instance);

    [Fact]
    public async Task ProcessAsync_WithNoDefaultChart_ParksDocumentAwaitingOne()
    {
        var (db, _, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;

        var service = BuildService(db, new NeverCalledAiClassificationService());
        await service.ProcessAsync(documentId, CancellationToken.None);

        var document = await db.Documents.FirstAsync(d => d.Id == documentId);
        document.ClassificationStatus.Should().Be(ClassificationStatus.AwaitingDefaultChartOfAccounts);

        (await db.AccountClassifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_WithDefaultChart_ClassifiesRealCaixaEBancosLine()
    {
        // PATTERN_MATCH sozinho nunca ultrapassa 0.85 de confiança (ver
        // AccountClassifier.RuleHighConfidenceThreshold = 0.90), então a IA
        // sempre é consultada como segunda opinião aqui - o teste usa um
        // stub que concorda com a sugestão da regra.
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, chart, caixaAccount) = await SeedChartAsync(db, tenantId);

        var service = BuildService(db, new StubAiClassificationService(caixaAccount.Id, 0.95f));
        await service.ProcessAsync(documentId, CancellationToken.None);

        var document = await db.Documents.FirstAsync(d => d.Id == documentId);
        document.ClassificationStatus.Should().Be(ClassificationStatus.Completed);
        document.ChartOfAccountsId.Should().Be(chart.Id);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        classification.StandardAccountId.Should().Be(caixaAccount.Id);
        classification.ClassificationMethod.Should().Be("AI");
        classification.ConfidenceScore.Should().Be(0.95m);
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.Pending);
    }

    [Fact]
    public async Task ProcessAsync_ExactMatch_SkipsAiEntirely()
    {
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, _, caixaAccount) = await SeedChartAsync(db, tenantId);
        // Renomeia a conta padrão para bater exatamente com o nome extraído,
        // forçando EXACT_MATCH (confiança 0.99) - acima do limiar de 0.90 que
        // dispensa a IA.
        caixaAccount.Name = "Caixa e bancos";
        await db.SaveChangesAsync();

        var service = BuildService(db, new NeverCalledAiClassificationService());
        await service.ProcessAsync(documentId, CancellationToken.None);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        classification.StandardAccountId.Should().Be(caixaAccount.Id);
        classification.ClassificationMethod.Should().Be("EXACT_MATCH");
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.Pending);
    }

    [Fact]
    public async Task ProcessAsync_WhenAiIsUnavailable_FallsBackToRuleSuggestionAndFlagsForReview()
    {
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, _, caixaAccount) = await SeedChartAsync(db, tenantId);

        var service = BuildService(db, new UnreachableAiClassificationService());
        await service.ProcessAsync(documentId, CancellationToken.None);

        var document = await db.Documents.FirstAsync(d => d.Id == documentId);
        document.ClassificationStatus.Should().Be(ClassificationStatus.Completed);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        // A regra (PATTERN_MATCH) ainda encontrou "Caixa e Equivalentes de
        // Caixa"; como a IA está indisponível, mantemos essa sugestão mas
        // marcamos para revisão em vez de confiar cegamente nela.
        classification.StandardAccountId.Should().Be(caixaAccount.Id);
        classification.ClassificationMethod.Should().Be("AI_UNAVAILABLE");
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.NeedsReview);
    }

    [Fact]
    public async Task ProcessAsync_WhenAiConfidenceIsBelowItsOwnThreshold_IsFlaggedForReview()
    {
        // 0.72 fica acima do limiar de regra (0.70) mas abaixo do limiar
        // específico de IA (0.75) - prova que usamos o segundo, não o primeiro.
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, _, caixaAccount) = await SeedChartAsync(db, tenantId);

        var service = BuildService(db, new StubAiClassificationService(caixaAccount.Id, 0.72f));
        await service.ProcessAsync(documentId, CancellationToken.None);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        classification.ClassificationMethod.Should().Be("AI");
        classification.ConfidenceScore.Should().Be(0.72m);
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.NeedsReview);
    }

    [Fact]
    public async Task ProcessAsync_RealAccentedSourceName_MatchesTheBetterCandidateNotJustAnyOverlap()
    {
        // Regression: SourceAccount.NormalizedName used to be computed with a
        // plain ToUpperInvariant() (keeping accents, e.g. "SALÁRIOS"), while
        // the classification engine strips accents from candidate names
        // ("SALARIOS"). "SALÁRIOS" != "SALARIOS" as strings, so the real
        // best match ("Salários e Encargos a Pagar") lost a tokenoverlap tie
        // to an unrelated candidate ("Impostos a Pagar") that happened to be
        // enumerated first. Both source and candidate names now go through
        // the same IAccountNameNormalizer.
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (chart, salariosAccount) = await SeedPassivoChartWithSalariosAsync(db, tenantId);

        // A asserção que realmente prende o bug original é a do
        // NormalizedName logo abaixo - ela não depende da IA. O stub aqui só
        // permite que o pipeline completo (regra de baixa confiança -> IA)
        // seja exercitado de ponta a ponta; a proteção específica da regra
        // (PatternMatchRule escolhendo o candidato certo) tem um teste
        // dedicado em RuleOrchestratorTests.
        var service = BuildService(db, new StubAiClassificationService(salariosAccount.Id, 0.9f));
        await service.ProcessAsync(documentId, CancellationToken.None);

        var salariosSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Salários a pagar");
        salariosSourceAccount.NormalizedName.Should().NotContain("Á");
        salariosSourceAccount.NormalizedName.Should().Be("SALARIOS A PAGAR");

        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == salariosSourceAccount.Id);
        classification.StandardAccountId.Should().Be(salariosAccount.Id);
        classification.ChartOfAccountsId.Should().Be(chart.Id);
    }

    [Fact]
    public async Task ProcessAsync_AccountWithNoCandidates_IsFlaggedForReview()
    {
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        // Seed a chart with zero StandardAccounts - nothing can match anything.
        var chart = new ChartOfAccounts { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Plano Vazio", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChartOfAccounts.Add(chart);
        await db.SaveChangesAsync();

        // Sem candidatos, a IA nem deveria ser chamada - ver
        // AccountClassifier: candidates.Count == 0 retorna direto.
        var service = BuildService(db, new NeverCalledAiClassificationService());
        await service.ProcessAsync(documentId, CancellationToken.None);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        classification.StandardAccountId.Should().BeNull();
        classification.ClassificationMethod.Should().Be("UNKNOWN");
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.NeedsReview);
    }

    [Fact]
    public async Task ProcessAsync_OnlyClassifiesSourceAccountsThatHaveValues()
    {
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, _, caixaAccount) = await SeedChartAsync(db, tenantId);

        var service = BuildService(db, new StubAiClassificationService(caixaAccount.Id, 0.9f));
        await service.ProcessAsync(documentId, CancellationToken.None);

        // "Ativo" and "Circulante:" are structural headers with no values of
        // their own - they must not end up with a classification row.
        var ativoHeader = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Ativo");
        var hasClassification = await db.AccountClassifications.AnyAsync(c => c.SourceAccountId == ativoHeader.Id);

        hasClassification.Should().BeFalse();
    }
}

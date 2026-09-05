using CreditScanAI.Api.Services;
using CreditScanAI.Classification;
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

    private static ClassificationProcessingService BuildService(AppDbContext db) => new(
        db,
        new RuleOrchestrator([new ExactMatchRule(new AccountNameNormalizer()), new PatternMatchRule(new AccountNameNormalizer())]),
        new AccountNameNormalizer(),
        NullLogger<ClassificationProcessingService>.Instance);

    [Fact]
    public async Task ProcessAsync_WithNoDefaultChart_ParksDocumentAwaitingOne()
    {
        var (db, _, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;

        var service = BuildService(db);
        await service.ProcessAsync(documentId, CancellationToken.None);

        var document = await db.Documents.FirstAsync(d => d.Id == documentId);
        document.ClassificationStatus.Should().Be(ClassificationStatus.AwaitingDefaultChartOfAccounts);

        (await db.AccountClassifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_WithDefaultChart_ClassifiesRealCaixaEBancosLine()
    {
        var (db, tenantId, documentId) = await ExtractRealBalanceSheetAsync();
        await using var _ = db;
        var (_, _, chart, caixaAccount) = await SeedChartAsync(db, tenantId);

        var service = BuildService(db);
        await service.ProcessAsync(documentId, CancellationToken.None);

        var document = await db.Documents.FirstAsync(d => d.Id == documentId);
        document.ClassificationStatus.Should().Be(ClassificationStatus.Completed);
        document.ChartOfAccountsId.Should().Be(chart.Id);

        var caixaSourceAccount = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Caixa e bancos");
        var classification = await db.AccountClassifications.FirstAsync(c => c.SourceAccountId == caixaSourceAccount.Id);

        classification.StandardAccountId.Should().Be(caixaAccount.Id);
        classification.ClassificationMethod.Should().Be("PATTERN_MATCH");
        classification.ConfidenceScore.Should().BeGreaterThan(0.7m);
        classification.ReviewStatus.Should().Be(ClassificationReviewStatus.Pending);
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

        var service = BuildService(db);
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

        var service = BuildService(db);
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
        await SeedChartAsync(db, tenantId);

        var service = BuildService(db);
        await service.ProcessAsync(documentId, CancellationToken.None);

        // "Ativo" and "Circulante:" are structural headers with no values of
        // their own - they must not end up with a classification row.
        var ativoHeader = await db.SourceAccounts.FirstAsync(a => a.OriginalName == "Ativo");
        var hasClassification = await db.AccountClassifications.AnyAsync(c => c.SourceAccountId == ativoHeader.Id);

        hasClassification.Should().BeFalse();
    }
}

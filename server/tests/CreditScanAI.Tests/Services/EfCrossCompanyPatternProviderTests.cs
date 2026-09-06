using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Tests.Services;

public class EfCrossCompanyPatternProviderTests
{
    private static async Task<(AppDbContext Db, Guid TenantId, Guid ChartId)> SeedBaseAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Active = true, CreatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return (db, tenantId, chartId);
    }

    private static async Task<Guid> SeedCompanyAsync(AppDbContext db, Guid tenantId, string code)
    {
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = code, Name = $"Empresa {code}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return companyId;
    }

    private static async Task SeedApprovedDecisionAsync(
        AppDbContext db, Guid tenantId, Guid companyId, Guid chartId, string normalizedName, Guid standardAccountId,
        ClassificationReviewStatus status = ClassificationReviewStatus.Approved)
    {
        var documentId = Guid.NewGuid();
        db.Documents.Add(new Document
        {
            Id = documentId,
            TenantId = tenantId,
            CompanyId = companyId,
            DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow,
            FileName = "doc.pdf",
            FilePath = "doc.pdf",
            ExtractionStatus = ExtractionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var sourceAccountId = Guid.NewGuid();
        db.SourceAccounts.Add(new SourceAccount
        {
            Id = sourceAccountId,
            TenantId = tenantId,
            DocumentId = documentId,
            OriginalName = "Conta qualquer",
            NormalizedName = normalizedName,
            HierarchyLevel = 1,
            CreatedAt = DateTime.UtcNow
        });

        db.AccountClassifications.Add(new AccountClassification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceAccountId = sourceAccountId,
            StandardAccountId = standardAccountId,
            ChartOfAccountsId = chartId,
            ConfidenceScore = 1.0m,
            ClassificationMethod = "AI",
            ReviewStatus = status,
            ReviewedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static StandardAccount NewStandardAccount(Guid tenantId, Guid chartId, string code, string name) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
        AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
        Code = code, Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task FindPatternAsync_TwoOtherCompaniesAgree_ReturnsPattern()
    {
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var caixa = NewStandardAccount(tenantId, chartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        db.StandardAccounts.Add(caixa);
        await db.SaveChangesAsync();

        var companyA = await SeedCompanyAsync(db, tenantId, "A");
        var companyB = await SeedCompanyAsync(db, tenantId, "B");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);
        await SeedApprovedDecisionAsync(db, tenantId, companyB, chartId, "CAIXA E BANCOS", caixa.Id);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().NotBeNull();
        result!.StandardAccountId.Should().Be(caixa.Id);
        result.CompanyCount.Should().Be(2);
        result.Consistency.Should().Be(1.0f);
    }

    [Fact]
    public async Task FindPatternAsync_OnlyOneOtherCompanyHasDecided_ReturnsNull()
    {
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var caixa = NewStandardAccount(tenantId, chartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        db.StandardAccounts.Add(caixa);
        await db.SaveChangesAsync();

        var companyA = await SeedCompanyAsync(db, tenantId, "A");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPatternAsync_RepeatedUploadsFromSameCompanyCountAsOneVote()
    {
        // Uma única empresa que reenviou o mesmo documento várias vezes não
        // deve, sozinha, parecer um "padrão entre empresas".
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var caixa = NewStandardAccount(tenantId, chartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        db.StandardAccounts.Add(caixa);
        await db.SaveChangesAsync();

        var companyA = await SeedCompanyAsync(db, tenantId, "A");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);
        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);
        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPatternAsync_LowConsistencyAcrossCompanies_ReturnsNull()
    {
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var caixa = NewStandardAccount(tenantId, chartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        var outra = NewStandardAccount(tenantId, chartId, "OUTRA", "Outra Conta");
        db.StandardAccounts.AddRange(caixa, outra);
        await db.SaveChangesAsync();

        var companyA = await SeedCompanyAsync(db, tenantId, "A");
        var companyB = await SeedCompanyAsync(db, tenantId, "B");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        // Metade concorda, metade não - abaixo do limiar de 80% de consistência.
        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id);
        await SeedApprovedDecisionAsync(db, tenantId, companyB, chartId, "CAIXA E BANCOS", outra.Id);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPatternAsync_OnlyPendingDecisionsExist_ReturnsNull()
    {
        // Sem revisão humana (Pending), não é uma decisão confiável o
        // suficiente para virar um padrão aprendido.
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var caixa = NewStandardAccount(tenantId, chartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        db.StandardAccounts.Add(caixa);
        await db.SaveChangesAsync();

        var companyA = await SeedCompanyAsync(db, tenantId, "A");
        var companyB = await SeedCompanyAsync(db, tenantId, "B");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        await SeedApprovedDecisionAsync(db, tenantId, companyA, chartId, "CAIXA E BANCOS", caixa.Id, ClassificationReviewStatus.Pending);
        await SeedApprovedDecisionAsync(db, tenantId, companyB, chartId, "CAIXA E BANCOS", caixa.Id, ClassificationReviewStatus.Pending);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPatternAsync_DecisionsBelongToDifferentTenant_ReturnsNull()
    {
        var (db, tenantId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var otherTenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = otherTenantId, Name = "Other Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        var otherChartId = Guid.NewGuid();
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = otherChartId, TenantId = otherTenantId, Name = "Plano", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var caixa = NewStandardAccount(otherTenantId, otherChartId, "CAIXA", "Caixa e Equivalentes de Caixa");
        db.StandardAccounts.Add(caixa);
        await db.SaveChangesAsync();

        var otherTenantCompanyA = await SeedCompanyAsync(db, otherTenantId, "A");
        var otherTenantCompanyB = await SeedCompanyAsync(db, otherTenantId, "B");
        var newCompany = await SeedCompanyAsync(db, tenantId, "NEW");

        await SeedApprovedDecisionAsync(db, otherTenantId, otherTenantCompanyA, otherChartId, "CAIXA E BANCOS", caixa.Id);
        await SeedApprovedDecisionAsync(db, otherTenantId, otherTenantCompanyB, otherChartId, "CAIXA E BANCOS", caixa.Id);

        var provider = new EfCrossCompanyPatternProvider(db);
        var result = await provider.FindPatternAsync(tenantId, newCompany, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }
}

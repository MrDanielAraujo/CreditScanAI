using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Tests.Services;

public class EfClassificationHistoryProviderTests
{
    private static async Task<(AppDbContext Db, Guid TenantId, Guid CompanyId, Guid ChartId)> SeedBaseAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = "C1", Name = "Company 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return (db, tenantId, companyId, chartId);
    }

    private static async Task<(Guid DocumentId, Guid SourceAccountId)> SeedDocumentWithAccountAsync(
        AppDbContext db, Guid tenantId, Guid companyId, string normalizedName)
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

        await db.SaveChangesAsync();
        return (documentId, sourceAccountId);
    }

    [Fact]
    public async Task FindPreviousDecisionAsync_ApprovedClassificationForSameCompanyAndName_IsFound()
    {
        var (db, tenantId, companyId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var standardAccount = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = "CAIXA", Name = "Caixa e Equivalentes de Caixa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(standardAccount);

        var (previousDocumentId, previousSourceAccountId) = await SeedDocumentWithAccountAsync(db, tenantId, companyId, "CAIXA E BANCOS");
        db.AccountClassifications.Add(new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = previousSourceAccountId,
            StandardAccountId = standardAccount.Id, ChartOfAccountsId = chartId,
            ConfidenceScore = 1.0m, ClassificationMethod = "AI",
            ReviewStatus = ClassificationReviewStatus.Approved, ReviewedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var (currentDocumentId, _) = await SeedDocumentWithAccountAsync(db, tenantId, companyId, "CAIXA E BANCOS");

        var provider = new EfClassificationHistoryProvider(db);
        var result = await provider.FindPreviousDecisionAsync(companyId, currentDocumentId, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().NotBeNull();
        result!.StandardAccountId.Should().Be(standardAccount.Id);
        result.StandardAccountName.Should().Be("Caixa e Equivalentes de Caixa");
    }

    [Fact]
    public async Task FindPreviousDecisionAsync_OnlyPendingClassificationExists_ReturnsNull()
    {
        // Uma classificação nunca revisada por humano (Pending) não é uma
        // decisão confiável o suficiente para reaproveitar automaticamente.
        var (db, tenantId, companyId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var standardAccount = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = "CAIXA", Name = "Caixa e Equivalentes de Caixa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(standardAccount);

        var (_, previousSourceAccountId) = await SeedDocumentWithAccountAsync(db, tenantId, companyId, "CAIXA E BANCOS");
        db.AccountClassifications.Add(new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = previousSourceAccountId,
            StandardAccountId = standardAccount.Id, ChartOfAccountsId = chartId,
            ConfidenceScore = 0.8m, ClassificationMethod = "AI",
            ReviewStatus = ClassificationReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (currentDocumentId, _) = await SeedDocumentWithAccountAsync(db, tenantId, companyId, "CAIXA E BANCOS");

        var provider = new EfClassificationHistoryProvider(db);
        var result = await provider.FindPreviousDecisionAsync(companyId, currentDocumentId, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPreviousDecisionAsync_ApprovedClassificationBelongsToDifferentCompany_ReturnsNull()
    {
        var (db, tenantId, companyId, chartId) = await SeedBaseAsync();
        await using var _ = db;

        var otherCompanyId = Guid.NewGuid();
        db.Companies.Add(new Company { Id = otherCompanyId, TenantId = tenantId, Code = "C2", Name = "Company 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var standardAccount = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = "CAIXA", Name = "Caixa e Equivalentes de Caixa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(standardAccount);

        // A decisão histórica pertence à OUTRA empresa.
        var (_, otherCompanySourceAccountId) = await SeedDocumentWithAccountAsync(db, tenantId, otherCompanyId, "CAIXA E BANCOS");
        db.AccountClassifications.Add(new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = otherCompanySourceAccountId,
            StandardAccountId = standardAccount.Id, ChartOfAccountsId = chartId,
            ConfidenceScore = 1.0m, ClassificationMethod = "AI",
            ReviewStatus = ClassificationReviewStatus.Approved, ReviewedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (currentDocumentId, _) = await SeedDocumentWithAccountAsync(db, tenantId, companyId, "CAIXA E BANCOS");

        var provider = new EfClassificationHistoryProvider(db);
        var result = await provider.FindPreviousDecisionAsync(companyId, currentDocumentId, "CAIXA E BANCOS", CancellationToken.None);

        result.Should().BeNull();
    }
}

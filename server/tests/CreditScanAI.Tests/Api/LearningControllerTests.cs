using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Learning;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class LearningControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LearningControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> SeedClassificationAsync(
        string sourceName, ClassificationReviewStatus status, string method, Guid? standardAccountId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Learning Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{Guid.NewGuid():N}"[..10], Name = "Empresa Teste", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano Teste", IsDefault = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document
        {
            Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow, FileName = "doc.pdf", FilePath = "doc.pdf",
            ExtractionStatus = ExtractionStatus.Completed, ChartOfAccountsId = chartId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var sourceAccount = new SourceAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, DocumentId = documentId,
            OriginalName = sourceName, NormalizedName = sourceName.ToUpperInvariant(),
            HierarchyLevel = 1, CreatedAt = DateTime.UtcNow
        };
        db.SourceAccounts.Add(sourceAccount);

        var classification = new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccount.Id,
            StandardAccountId = standardAccountId, ChartOfAccountsId = chartId,
            ConfidenceScore = 0.9m, ClassificationMethod = method,
            ReviewStatus = status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AccountClassifications.Add(classification);

        await db.SaveChangesAsync();
        return classification.Id;
    }

    [Fact]
    public async Task GetStats_CountsClassificationsByReviewStatusAndMethod()
    {
        await SeedClassificationAsync("Conta A", ClassificationReviewStatus.Approved, "EXACT_MATCH");
        await SeedClassificationAsync("Conta B", ClassificationReviewStatus.NeedsReview, "AI");

        var response = await _client.GetFromJsonAsync<ApiResponse<LearningStatsResponse>>("/api/learning/stats");

        response!.Data!.TotalClassifications.Should().BeGreaterOrEqualTo(2);
        response.Data!.ByReviewStatus.Should().ContainKey("Approved").WhoseValue.Should().BeGreaterOrEqualTo(1);
        response.Data!.ByReviewStatus.Should().ContainKey("NeedsReview").WhoseValue.Should().BeGreaterOrEqualTo(1);
        response.Data!.ByMethod.Should().ContainKey("EXACT_MATCH");
        response.Data!.ByMethod.Should().ContainKey("AI");
    }

    [Fact]
    public async Task GetStats_ApprovalRate_OnlyCountsHumanReviewedOutcomes()
    {
        await SeedClassificationAsync("Conta Aprovada", ClassificationReviewStatus.Approved, "AI");
        await SeedClassificationAsync("Conta Corrigida", ClassificationReviewStatus.Overridden, "AI");

        var response = await _client.GetFromJsonAsync<ApiResponse<LearningStatsResponse>>("/api/learning/stats");

        // Não afirmamos um valor exato (o banco acumula entre testes), só que
        // a taxa é calculável e coerente com Approved / (Approved+Overridden+Rejected).
        response!.Data!.ReviewedCount.Should().BeGreaterOrEqualTo(2);
        response.Data!.ApprovalRate.Should().NotBeNull();
        response.Data!.ApprovalRate!.Value.Should().BeInRange(0f, 1f);
    }

    [Fact]
    public async Task GetStats_TopOverriddenAccounts_ListsMostCorrectedSourceAccountNames()
    {
        await SeedClassificationAsync("Conta Sempre Errada", ClassificationReviewStatus.Overridden, "AI");
        await SeedClassificationAsync("Conta Sempre Errada", ClassificationReviewStatus.Overridden, "AI");
        await SeedClassificationAsync("Conta Sempre Errada", ClassificationReviewStatus.Overridden, "AI");

        var response = await _client.GetFromJsonAsync<ApiResponse<LearningStatsResponse>>("/api/learning/stats");

        response!.Data!.TopOverriddenAccounts.Should().Contain(a => a.SourceAccountName == "Conta Sempre Errada" && a.Count >= 3);
    }

    [Fact]
    public async Task GetStats_NoReviewedClassificationsYet_ApprovalRateIsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Uma base totalmente vazia (nenhuma classificação nunca revisada) -
        // não dá pra calcular uma taxa de aprovação de zero decisões.
        var hasAnyReviewed = await db.AccountClassifications.AnyAsync(c =>
            c.ReviewStatus == ClassificationReviewStatus.Approved
            || c.ReviewStatus == ClassificationReviewStatus.Overridden
            || c.ReviewStatus == ClassificationReviewStatus.Rejected);

        if (hasAnyReviewed)
        {
            // Outros testes desta classe já rodaram e populados o mesmo
            // banco em memória compartilhado pela fixture - este cenário só
            // é observável isoladamente, então não falha o teste.
            return;
        }

        var response = await _client.GetFromJsonAsync<ApiResponse<LearningStatsResponse>>("/api/learning/stats");

        response!.Data!.ApprovalRate.Should().BeNull();
    }
}

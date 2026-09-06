using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Classifications;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class ClassificationsControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClassificationsControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid TenantId, Guid CompanyId, Guid ChartId, Guid DocumentId)> SeedTenantCompanyDocumentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Classifications Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{documentId:N}", Name = "Empresa Teste", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano Teste", IsDefault = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
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
            ChartOfAccountsId = chartId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return (tenantId, companyId, chartId, documentId);
    }

    private async Task<(Guid SourceAccountId, Guid StandardAccountId, Guid ClassificationId)> SeedNeedsReviewClassificationAsync(
        Guid tenantId, Guid documentId, Guid chartId, string sourceName = "Conta X", float confidence = 0.5f)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var standardAccount = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = $"COD_{Guid.NewGuid():N}"[..12], Name = "Conta Padrão Sugerida",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(standardAccount);

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
            StandardAccountId = standardAccount.Id, ChartOfAccountsId = chartId,
            ConfidenceScore = (decimal)confidence, ClassificationMethod = "AI", Evidence = "IA sugeriu isso",
            ReviewStatus = ClassificationReviewStatus.NeedsReview,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AccountClassifications.Add(classification);

        await db.SaveChangesAsync();
        return (sourceAccount.Id, standardAccount.Id, classification.Id);
    }

    [Fact]
    public async Task GetPending_ListsOnlyNeedsReviewItemsForTheGivenDocument()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Pendente");

        var response = await _client.GetFromJsonAsync<ApiResponse<PendingClassificationsResponse>>(
            $"/api/classifications/pending?documentId={documentId}");

        response!.Data!.Items.Should().Contain(i => i.ClassificationId == classificationId);
        response.Data!.Items.Should().OnlyContain(i => i.DocumentId == documentId);
    }

    [Fact]
    public async Task GetPending_ExposesReviewStatusOnEachItem()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Com Status");

        var response = await _client.GetFromJsonAsync<ApiResponse<PendingClassificationsResponse>>(
            $"/api/classifications/pending?documentId={documentId}");

        response!.Data!.Items.Should().ContainSingle(i => i.ClassificationId == classificationId)
            .Which.ReviewStatus.Should().Be("NeedsReview");
    }

    [Fact]
    public async Task GetPending_DefaultStatus_ExcludesAlreadyPendingClassification()
    {
        // Fase 6 Parte 2: por padrão a fila continua só mostrando NeedsReview -
        // uma classificação Pending (auto-aprovada com confiança alta) não
        // deve poluir a fila de revisão clássica.
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sourceAccount = new SourceAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, DocumentId = documentId,
            OriginalName = "Conta Já Classificada", NormalizedName = "CONTA JA CLASSIFICADA",
            HierarchyLevel = 1, CreatedAt = DateTime.UtcNow
        };
        db.SourceAccounts.Add(sourceAccount);
        var classification = new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccount.Id,
            StandardAccountId = null, ChartOfAccountsId = chartId,
            ConfidenceScore = 0.95m, ClassificationMethod = "AI",
            ReviewStatus = ClassificationReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AccountClassifications.Add(classification);
        await db.SaveChangesAsync();

        var defaultResponse = await _client.GetFromJsonAsync<ApiResponse<PendingClassificationsResponse>>(
            $"/api/classifications/pending?documentId={documentId}");
        defaultResponse!.Data!.Items.Should().NotContain(i => i.ClassificationId == classification.Id);

        var allResponse = await _client.GetFromJsonAsync<ApiResponse<PendingClassificationsResponse>>(
            $"/api/classifications/pending?documentId={documentId}&status=all");
        allResponse!.Data!.Items.Should().Contain(i => i.ClassificationId == classification.Id && i.ReviewStatus == "Pending");
    }

    [Fact]
    public async Task GetById_ReturnsFullDetailWithSuggestedStandardAccount()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, standardAccountId, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Detalhe");

        var response = await _client.GetFromJsonAsync<ApiResponse<ClassificationDetailResponse>>($"/api/classifications/{classificationId}");

        response!.Data!.ClassificationId.Should().Be(classificationId);
        response.Data!.ChartOfAccountsId.Should().Be(chartId);
        response.Data!.SourceAccount.OriginalName.Should().Be("Conta Detalhe");
        response.Data!.SuggestedStandardAccount.Should().NotBeNull();
        response.Data!.SuggestedStandardAccount!.Id.Should().Be(standardAccountId);
        response.Data!.ReviewStatus.Should().Be("NeedsReview");
    }

    [Fact]
    public async Task Reject_ClearsSuggestionAndMarksAsRejected()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Rejeitar");

        var response = await _client.PostAsJsonAsync(
            $"/api/classifications/{classificationId}/reject",
            new RejectClassificationRequest("Não corresponde a nenhuma conta padrão"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RejectClassificationResponse>>();
        body!.Data!.ReviewStatus.Should().Be("Rejected");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.AccountClassifications.FirstAsync(c => c.Id == classificationId);
        persisted.ReviewStatus.Should().Be(ClassificationReviewStatus.Rejected);
        persisted.StandardAccountId.Should().BeNull();
        persisted.ConfidenceScore.Should().Be(0m);
        persisted.ReviewNotes.Should().Be("Não corresponde a nenhuma conta padrão");
    }

    [Fact]
    public async Task Reject_DoesNotAppearInPendingListAfterwards()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Rejeitar Fila");

        await _client.PostAsJsonAsync($"/api/classifications/{classificationId}/reject", new RejectClassificationRequest(null));

        var response = await _client.GetFromJsonAsync<ApiResponse<PendingClassificationsResponse>>(
            $"/api/classifications/pending?documentId={documentId}");

        response!.Data!.Items.Should().NotContain(i => i.ClassificationId == classificationId);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/classifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_MarksAsApprovedAndStoresNotes()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Aprovar");

        var response = await _client.PostAsJsonAsync(
            $"/api/classifications/{classificationId}/approve",
            new ApproveClassificationRequest("Confirmado, está correto"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApproveClassificationResponse>>();
        body!.Data!.ReviewStatus.Should().Be("Approved");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.AccountClassifications.FirstAsync(c => c.Id == classificationId);
        persisted.ReviewStatus.Should().Be(ClassificationReviewStatus.Approved);
        persisted.ReviewNotes.Should().Be("Confirmado, está correto");
        persisted.ReviewedAt.Should().NotBeNull();
        persisted.ReviewedBy.Should().BeNull(); // sem login funcional ainda
    }

    [Fact]
    public async Task Approve_ClassificationWithNoSuggestion_ReturnsConflict()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sourceAccount = new SourceAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, DocumentId = documentId,
            OriginalName = "Conta sem sugestão", NormalizedName = "CONTA SEM SUGESTAO",
            HierarchyLevel = 1, CreatedAt = DateTime.UtcNow
        };
        db.SourceAccounts.Add(sourceAccount);
        var classification = new AccountClassification
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccount.Id,
            StandardAccountId = null, ChartOfAccountsId = chartId,
            ConfidenceScore = 0m, ClassificationMethod = "UNKNOWN",
            ReviewStatus = ClassificationReviewStatus.NeedsReview,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AccountClassifications.Add(classification);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync($"/api/classifications/{classification.Id}/approve", new ApproveClassificationRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Override_ChangesStandardAccountAndSetsFullConfidence()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Override");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var correctAccount = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = "CORRETA", Name = "Conta Correta", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(correctAccount);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/classifications/{classificationId}/override",
            new OverrideClassificationRequest(correctAccount.Id, "A empresa sempre classifica assim"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OverrideClassificationResponse>>();
        body!.Data!.NewStandardAccount.Id.Should().Be(correctAccount.Id);
        body.Data!.ReviewStatus.Should().Be("Overridden");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await verifyDb.AccountClassifications.FirstAsync(c => c.Id == classificationId);
        persisted.StandardAccountId.Should().Be(correctAccount.Id);
        persisted.ConfidenceScore.Should().Be(1.0m);
        persisted.ReviewStatus.Should().Be(ClassificationReviewStatus.Overridden);
        persisted.ReviewNotes.Should().Be("A empresa sempre classifica assim");
    }

    [Fact]
    public async Task Override_WithStandardAccountFromAnotherChart_ReturnsBadRequest()
    {
        var (tenantId, _, chartId, documentId) = await SeedTenantCompanyDocumentAsync();
        var (_, _, classificationId) = await SeedNeedsReviewClassificationAsync(tenantId, documentId, chartId, "Conta Override Inválido");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var otherChartId = Guid.NewGuid();
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = otherChartId, TenantId = tenantId, Name = "Outro Plano", IsDefault = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        var accountFromOtherChart = new StandardAccount
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = otherChartId,
            AccountTypeId = Guid.NewGuid(), AccountSubtypeId = Guid.NewGuid(),
            Code = "OUTRO", Name = "Conta de Outro Plano", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.StandardAccounts.Add(accountFromOtherChart);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/classifications/{classificationId}/override",
            new OverrideClassificationRequest(accountFromOtherChart.Id, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

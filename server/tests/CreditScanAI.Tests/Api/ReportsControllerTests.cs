using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Reports;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class ReportsControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid CompanyId, Guid DocumentId, Guid PeriodId)> SeedDocumentWithClassificationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Reports Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{documentId:N}"[..10], Name = "Empresa Reports Test", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document
        {
            Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow, FileName = "quality_test.pdf", FilePath = "doc.pdf",
            ExtractionStatus = ExtractionStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.Periods.Add(new Period { Id = periodId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2020, Quarter = 2, EndDate = new DateOnly(2020, 6, 30), CreatedAt = DateTime.UtcNow });

        var approvedAccount = new SourceAccount { Id = Guid.NewGuid(), TenantId = tenantId, DocumentId = documentId, OriginalName = "Conta OK", NormalizedName = "CONTA OK", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow };
        var needsReviewAccount = new SourceAccount { Id = Guid.NewGuid(), TenantId = tenantId, DocumentId = documentId, OriginalName = "Conta Duvidosa", NormalizedName = "CONTA DUVIDOSA", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow };
        db.SourceAccounts.AddRange(approvedAccount, needsReviewAccount);

        db.AccountValues.Add(new AccountValue { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = approvedAccount.Id, DocumentId = documentId, PeriodId = periodId, RawValue = 100m, CreatedAt = DateTime.UtcNow });

        db.AccountClassifications.AddRange(
            new AccountClassification { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = approvedAccount.Id, ConfidenceScore = 0.99m, ClassificationMethod = "EXACT_MATCH", ReviewStatus = ClassificationReviewStatus.Approved, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AccountClassification { Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = needsReviewAccount.Id, ConfidenceScore = 0.4m, ClassificationMethod = "AI", ReviewStatus = ClassificationReviewStatus.NeedsReview, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        return (companyId, documentId, periodId);
    }

    [Fact]
    public async Task GetQualityReport_CountsClassificationsByStatus()
    {
        var (_, documentId, _) = await SeedDocumentWithClassificationsAsync();

        var response = await _client.GetFromJsonAsync<ApiResponse<QualityReportResponse>>($"/api/reports/quality/{documentId}");

        response!.Data!.TotalClassifiedAccounts.Should().Be(2);
        response.Data!.ApprovedCount.Should().Be(1);
        response.Data!.NeedsReviewCount.Should().Be(1);
        response.Data!.AverageConfidence.Should().BeApproximately((0.99f + 0.4f) / 2f, 0.001f);
    }

    [Fact]
    public async Task GetQualityReport_PeriodWithNoCalculationYet_EquationBalancedIsNull()
    {
        var (_, documentId, periodId) = await SeedDocumentWithClassificationsAsync();

        var response = await _client.GetFromJsonAsync<ApiResponse<QualityReportResponse>>($"/api/reports/quality/{documentId}");

        var periodStatus = response!.Data!.PeriodEquationStatus.Should().ContainSingle(p => p.PeriodId == periodId).Subject;
        periodStatus.EquationBalanced.Should().BeNull();
    }

    [Fact]
    public async Task GetQualityReport_PeriodWithCalculation_ReportsBalancedEquation()
    {
        var (companyId, documentId, periodId) = await SeedDocumentWithClassificationsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
            {
                Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), CompanyId = companyId, PeriodId = periodId,
                Key = CalculationKeys.EquacaoVariancia, Value = 0m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetFromJsonAsync<ApiResponse<QualityReportResponse>>($"/api/reports/quality/{documentId}");

        var periodStatus = response!.Data!.PeriodEquationStatus.Should().ContainSingle(p => p.PeriodId == periodId).Subject;
        periodStatus.EquationBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetQualityReport_UnknownDocument_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/reports/quality/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

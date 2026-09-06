using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Reports;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

    private async Task<(Guid TenantId, Guid CompanyId, Guid PeriodId)> SeedCalculatedCompanyAsync(string companyName, decimal ativoTotal = 1000m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync();
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{Guid.NewGuid():N}"[..10], Name = companyName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2020, Quarter = 2, EndDate = new DateOnly(2020, 6, 30), CreatedAt = DateTime.UtcNow });

        void AddValue(string key, decimal value) => db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = companyId, PeriodId = periodId,
            Key = key, Value = value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        AddValue(CalculationKeys.AtivoTotal, ativoTotal);
        AddValue(CalculationKeys.PassivoTotal, ativoTotal * 0.6m);
        AddValue(CalculationKeys.PatrimonioLiquido, ativoTotal * 0.4m);
        AddValue(CalculationKeys.EquacaoVariancia, 0m);

        await db.SaveChangesAsync();

        return (tenantId, companyId, periodId);
    }

    [Fact]
    public async Task ExportCompanyStatement_Pdf_ReturnsPdfFile()
    {
        var (_, companyId, periodId) = await SeedCalculatedCompanyAsync("Empresa Export PDF Test");

        var response = await _client.GetAsync($"/api/reports/financial-statement/company/{companyId}/periods/{periodId}?format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        // Assinatura padrão de arquivo PDF.
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task ExportCompanyStatement_Excel_ReturnsXlsxFile()
    {
        var (_, companyId, periodId) = await SeedCalculatedCompanyAsync("Empresa Export Excel Test");

        var response = await _client.GetAsync($"/api/reports/financial-statement/company/{companyId}/periods/{periodId}?format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportCompanyStatement_InvalidFormat_ReturnsBadRequest()
    {
        var (_, companyId, periodId) = await SeedCalculatedCompanyAsync("Empresa Export Invalid Format Test");

        var response = await _client.GetAsync($"/api/reports/financial-statement/company/{companyId}/periods/{periodId}?format=docx");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportCompanyStatement_NoCalculationYet_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantId = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync();
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = $"C_{Guid.NewGuid():N}"[..10], Name = "Empresa Sem Cálculo", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2021, Quarter = 1, EndDate = new DateOnly(2021, 3, 31), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/reports/financial-statement/company/{companyId}/periods/{periodId}?format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportConsolidatedStatement_Pdf_ReturnsPdfFile()
    {
        var (_, companyAId, periodId) = await SeedCalculatedCompanyAsync("Empresa Consolidada A");
        Guid companyBId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenantId = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync();
            companyBId = Guid.NewGuid();
            db.Companies.Add(new Company { Id = companyBId, TenantId = tenantId, Code = $"C_{Guid.NewGuid():N}"[..10], Name = "Empresa Consolidada B", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            void AddValue(string key, decimal value) => db.CalculatedFinancialValues.Add(new CalculatedFinancialValue
            {
                Id = Guid.NewGuid(), TenantId = tenantId, CompanyId = companyBId, PeriodId = periodId,
                Key = key, Value = value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            AddValue(CalculationKeys.AtivoTotal, 500m);
            AddValue(CalculationKeys.PassivoTotal, 300m);
            AddValue(CalculationKeys.PatrimonioLiquido, 200m);
            AddValue(CalculationKeys.EquacaoVariancia, 0m);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            $"/api/reports/financial-statement/consolidated?periodId={periodId}&companyIds={companyAId}&companyIds={companyBId}&format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ExportConsolidatedStatement_OneCompanyMissingCalculation_ReturnsBadRequest()
    {
        var (tenantId, companyAId, periodId) = await SeedCalculatedCompanyAsync("Empresa Consolidada Faltando A");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var companyBId = Guid.NewGuid();
        db.Companies.Add(new Company { Id = companyBId, TenantId = tenantId, Code = $"C_{Guid.NewGuid():N}"[..10], Name = "Empresa Sem Cálculo Consolidado", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync(
            $"/api/reports/financial-statement/consolidated?periodId={periodId}&companyIds={companyAId}&companyIds={companyBId}&format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

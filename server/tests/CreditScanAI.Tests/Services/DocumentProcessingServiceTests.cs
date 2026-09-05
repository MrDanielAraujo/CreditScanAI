using CreditScanAI.Api.Services;
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
using CreditScanAI.Tests.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditScanAI.Tests.Services;

public class DocumentProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_BalanceSheet_PersistsAccountsValuesAndPeriods()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = "C1", Name = "Company 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var documentId = Guid.NewGuid();
        var document = new Document
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
        };
        db.Documents.Add(document);
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

        var service = new DocumentProcessingService(
            db,
            new FakeDocumentStorage(pdfBytes),
            pipeline,
            new NumericValueNormalizer(),
            new AccountNameNormalizer(),
            new ClassificationProcessingQueue(),
            NullLogger<DocumentProcessingService>.Instance);

        // Act
        await service.ProcessAsync(documentId, CancellationToken.None);

        // Assert
        var updatedDocument = await db.Documents.FirstAsync(d => d.Id == documentId);
        updatedDocument.ExtractionStatus.Should().Be(ExtractionStatus.Completed);
        updatedDocument.ExtractionStartedAt.Should().NotBeNull();
        updatedDocument.ExtractionCompletedAt.Should().NotBeNull();

        var accounts = await db.SourceAccounts.Where(a => a.DocumentId == documentId).ToListAsync();
        accounts.Should().Contain(a => a.OriginalName == "Ativo" && a.InferredType == "ATIVO");
        accounts.Should().Contain(a => a.OriginalName == "Caixa e bancos");

        var periods = await db.Periods.Where(p => p.TenantId == tenantId).ToListAsync();
        periods.Should().Contain(p => p.Year == 2020 && p.Quarter == 2);
        periods.Should().Contain(p => p.Year == 2019 && p.PeriodType == PeriodType.Annual);

        var caixaAccount = accounts.Single(a => a.OriginalName == "Caixa e bancos");
        var period2020 = periods.Single(p => p.Year == 2020 && p.Quarter == 2);

        var caixaValue = await db.AccountValues.SingleAsync(v =>
            v.SourceAccountId == caixaAccount.Id && v.PeriodId == period2020.Id);
        caixaValue.RawValue.Should().Be(1_067_737.38m);

        var fundColumnValues = await db.AccountValues
            .Where(v => v.SourceAccountId == caixaAccount.Id && v.RawColumnLabel != null)
            .ToListAsync();
        fundColumnValues.Should().Contain(v => v.RawColumnLabel!.Contains("ADM"));
    }
}

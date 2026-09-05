using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.PdfPipeline;
using CreditScanAI.PdfPipeline.Models;
using CreditScanAI.PdfPipeline.Normalization;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Runs the PDF extraction pipeline for one document and persists the
/// result as SourceAccount/AccountValue rows. A detected period column maps
/// to a real Period (created on first use); every other column is kept as
/// raw label metadata on AccountValue - see the Fase 2 entity-modeling
/// decision (Company stays a legal entity chosen manually at upload, not
/// inferred from document columns).
/// </summary>
public class DocumentProcessingService
{
    private readonly AppDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IPdfExtractionPipeline _pipeline;
    private readonly INumericValueNormalizer _valueNormalizer;
    private readonly IAccountNameNormalizer _nameNormalizer;
    private readonly IClassificationProcessingQueue _classificationQueue;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        AppDbContext db,
        IDocumentStorage storage,
        IPdfExtractionPipeline pipeline,
        INumericValueNormalizer valueNormalizer,
        IAccountNameNormalizer nameNormalizer,
        IClassificationProcessingQueue classificationQueue,
        ILogger<DocumentProcessingService> logger)
    {
        _db = db;
        _storage = storage;
        _pipeline = pipeline;
        _valueNormalizer = valueNormalizer;
        _nameNormalizer = nameNormalizer;
        _classificationQueue = classificationQueue;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Documento {DocumentId} não encontrado para processamento", documentId);
            return;
        }

        document.ExtractionStatus = ExtractionStatus.Processing;
        document.ExtractionStartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var bytes = await _storage.ReadAsync(document.FilePath, cancellationToken);
            var result = _pipeline.Process(bytes, document.DocumentType);

            var periodIdByColumn = await ResolvePeriodsAsync(document.TenantId, result.DetectedPeriods, cancellationToken);
            var labelByColumn = result.DetectedColumns.ToDictionary(c => c.ColumnIndex, c => c.RawLabel);
            PersistAccounts(document, result.HierarchicalAccounts, parentId: null, periodIdByColumn, labelByColumn, result.ScaleFactor);

            document.ExtractionStatus = ExtractionStatus.Completed;
            document.ExtractionCompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar documento {DocumentId}", documentId);
            document.ExtractionStatus = ExtractionStatus.Failed;
            document.ExtractionCompletedAt = DateTime.UtcNow;
            document.ExtractionError = ex.Message;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Chain straight into classification (Fase 3) - it parks itself with
        // AwaitingDefaultChartOfAccounts if there's no default plan yet, so
        // nothing is lost either way.
        if (document.ExtractionStatus == ExtractionStatus.Completed)
        {
            _classificationQueue.Enqueue(documentId);
        }
    }

    private async Task<Dictionary<int, Guid>> ResolvePeriodsAsync(
        Guid tenantId,
        IReadOnlyList<DetectedPeriod> detectedPeriods,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, Guid>();

        foreach (var detected in detectedPeriods)
        {
            var isYearEnd = detected.Date is { Month: 12, Day: 31 };
            var periodType = isYearEnd ? PeriodType.Annual : PeriodType.Quarterly;
            int? quarter = isYearEnd ? null : (detected.Date.Month - 1) / 3 + 1;
            var year = detected.Date.Year;

            var existing = await _db.Periods.FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.PeriodType == periodType &&
                p.Year == year &&
                p.Quarter == quarter,
                cancellationToken);

            if (existing is null)
            {
                existing = new Period
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PeriodType = periodType,
                    Year = year,
                    Quarter = quarter,
                    StartDate = new DateOnly(year, 1, 1),
                    EndDate = detected.Date,
                    IsClosed = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Periods.Add(existing);
                await _db.SaveChangesAsync(cancellationToken);
            }

            map[detected.ColumnIndex] = existing.Id;
        }

        return map;
    }

    private void PersistAccounts(
        Document document,
        IReadOnlyList<HierarchicalAccount> nodes,
        Guid? parentId,
        IReadOnlyDictionary<int, Guid> periodIdByColumn,
        IReadOnlyDictionary<int, string> labelByColumn,
        int scaleFactor)
    {
        foreach (var node in nodes)
        {
            var sourceAccount = new SourceAccount
            {
                Id = Guid.NewGuid(),
                TenantId = document.TenantId,
                DocumentId = document.Id,
                OriginalName = node.OriginalName,
                // Must match IAccountNameNormalizer exactly - it's also what
                // the classification engine (Fase 3) normalizes StandardAccount
                // names with, and ExactMatchRule/PatternMatchRule compare the
                // two directly.
                NormalizedName = _nameNormalizer.Normalize(node.OriginalName),
                HierarchyLevel = node.Level,
                ParentSourceAccountId = parentId,
                InferredType = node.InferredType,
                InferredSubtype = node.InferredSubtype,
                CreatedAt = DateTime.UtcNow
            };
            _db.SourceAccounts.Add(sourceAccount);

            foreach (var (columnIndex, cellText) in node.Cells)
            {
                var hasPeriod = periodIdByColumn.TryGetValue(columnIndex, out var periodId);
                labelByColumn.TryGetValue(columnIndex, out var rawLabel);

                _db.AccountValues.Add(new AccountValue
                {
                    Id = Guid.NewGuid(),
                    TenantId = document.TenantId,
                    SourceAccountId = sourceAccount.Id,
                    DocumentId = document.Id,
                    PeriodId = hasPeriod ? periodId : null,
                    RawColumnLabel = hasPeriod ? null : rawLabel,
                    RawValue = _valueNormalizer.Normalize(cellText).Value,
                    ScaleFactor = scaleFactor,
                    CreatedAt = DateTime.UtcNow
                });
            }

            PersistAccounts(document, node.Children, sourceAccount.Id, periodIdByColumn, labelByColumn, scaleFactor);
        }
    }
}

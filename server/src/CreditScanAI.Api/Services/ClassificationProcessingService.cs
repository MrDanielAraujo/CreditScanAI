using CreditScanAI.Classification;
using CreditScanAI.Classification.Models;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.PdfPipeline.Normalization;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Classifies every SourceAccount of a document that has at least one
/// AccountValue (i.e. an actual line item, not a structural header like
/// "Ativo"/"Circulante:") against the tenant's default ChartOfAccounts.
/// If there's no default chart yet, the document is parked with
/// AwaitingDefaultChartOfAccounts - nothing is lost, and
/// ChartOfAccountsController.SetDefault re-enqueues parked documents once
/// one is set.
/// </summary>
public class ClassificationProcessingService
{
    // Classificações por regra (EXACT_MATCH/PATTERN_MATCH) usam este limiar;
    // classificações por IA usam um limiar próprio, um pouco mais rígido -
    // ver AiReviewConfidenceThreshold.
    private const float RuleReviewConfidenceThreshold = 0.7f;
    private const float AiReviewConfidenceThreshold = 0.75f;

    private readonly AppDbContext _db;
    private readonly IAccountClassifier _classifier;
    private readonly IAccountNameNormalizer _normalizer;
    private readonly ILogger<ClassificationProcessingService> _logger;

    public ClassificationProcessingService(
        AppDbContext db,
        IAccountClassifier classifier,
        IAccountNameNormalizer normalizer,
        ILogger<ClassificationProcessingService> logger)
    {
        _db = db;
        _classifier = classifier;
        _normalizer = normalizer;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Documento {DocumentId} não encontrado para classificação", documentId);
            return;
        }

        var defaultChart = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(c => c.TenantId == document.TenantId && c.IsDefault, cancellationToken);

        if (defaultChart is null)
        {
            document.ClassificationStatus = ClassificationStatus.AwaitingDefaultChartOfAccounts;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        document.ClassificationStatus = ClassificationStatus.Processing;
        document.ClassificationStartedAt = DateTime.UtcNow;
        document.ChartOfAccountsId = defaultChart.Id;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var sourceAccountIds = await _db.AccountValues
                .Where(v => v.DocumentId == documentId)
                .Select(v => v.SourceAccountId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var sourceAccounts = await _db.SourceAccounts
                .Where(a => sourceAccountIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            var standardAccounts = await _db.StandardAccounts
                .Where(a => a.ChartOfAccountsId == defaultChart.Id)
                .ToListAsync(cancellationToken);

            var typesByCode = await _db.AccountTypes
                .Where(t => t.TenantId == document.TenantId)
                .ToDictionaryAsync(t => t.Code, cancellationToken);

            var subtypesByCode = await _db.AccountSubtypes
                .Where(s => s.TenantId == document.TenantId)
                .ToDictionaryAsync(s => s.Code, cancellationToken);

            foreach (var sourceAccount in sourceAccounts)
            {
                var candidates = FilterCandidates(standardAccounts, sourceAccount.InferredType, sourceAccount.InferredSubtype, typesByCode, subtypesByCode);

                var context = new ClassificationContext(
                    sourceAccount.OriginalName,
                    sourceAccount.NormalizedName ?? _normalizer.Normalize(sourceAccount.OriginalName),
                    sourceAccount.InferredType,
                    sourceAccount.InferredSubtype,
                    document.CompanyId,
                    document.Id);

                var result = await _classifier.ClassifyAsync(context, candidates, cancellationToken);
                var reviewStatus = DetermineReviewStatus(result);

                var existing = await _db.AccountClassifications
                    .FirstOrDefaultAsync(c => c.SourceAccountId == sourceAccount.Id, cancellationToken);

                if (existing is null)
                {
                    _db.AccountClassifications.Add(new AccountClassification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = document.TenantId,
                        SourceAccountId = sourceAccount.Id,
                        StandardAccountId = result.StandardAccountId,
                        ChartOfAccountsId = defaultChart.Id,
                        ConfidenceScore = (decimal)result.Confidence,
                        ClassificationMethod = result.Method,
                        Evidence = result.Evidence,
                        ReviewStatus = reviewStatus,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.StandardAccountId = result.StandardAccountId;
                    existing.ChartOfAccountsId = defaultChart.Id;
                    existing.ConfidenceScore = (decimal)result.Confidence;
                    existing.ClassificationMethod = result.Method;
                    existing.Evidence = result.Evidence;
                    existing.ReviewStatus = reviewStatus;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            document.ClassificationStatus = ClassificationStatus.Completed;
            document.ClassificationCompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao classificar documento {DocumentId}", documentId);
            document.ClassificationStatus = ClassificationStatus.Failed;
            document.ClassificationCompletedAt = DateTime.UtcNow;
            document.ClassificationError = ex.Message;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ClassificationReviewStatus DetermineReviewStatus(ClassificationResult result)
    {
        if (result.StandardAccountId is null || result.Method == "AI_UNAVAILABLE")
        {
            return ClassificationReviewStatus.NeedsReview;
        }

        var threshold = result.Method == "AI" ? AiReviewConfidenceThreshold : RuleReviewConfidenceThreshold;
        return result.Confidence < threshold ? ClassificationReviewStatus.NeedsReview : ClassificationReviewStatus.Pending;
    }

    private static List<StandardAccountCandidate> FilterCandidates(
        List<StandardAccount> standardAccounts,
        string? inferredType,
        string? inferredSubtype,
        Dictionary<string, AccountType> typesByCode,
        Dictionary<string, AccountSubtype> subtypesByCode)
    {
        if (inferredType is null || inferredSubtype is null)
        {
            return [];
        }

        if (!typesByCode.TryGetValue(inferredType, out var type) || !subtypesByCode.TryGetValue(inferredSubtype, out var subtype))
        {
            return [];
        }

        return standardAccounts
            .Where(a => a.AccountTypeId == type.Id && a.AccountSubtypeId == subtype.Id)
            .Select(a => new StandardAccountCandidate(a.Id, a.Code, a.Name, a.Description))
            .ToList();
    }
}

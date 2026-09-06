using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Learning;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Dashboard de aprendizado (Fase 6 Parte 3) - só contagens reais derivadas
/// de account_classifications, sem inventar uma métrica de "acurácia" sem
/// base (o doc original tinha valores de exemplo hardcoded para isso).
/// </summary>
[ApiController]
[Route("api/learning")]
public class LearningController : ControllerBase
{
    private const int TopAccountsLimit = 5;

    private readonly AppDbContext _db;

    public LearningController(AppDbContext db) => _db = db;

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<LearningStatsResponse>>> GetStats(CancellationToken cancellationToken)
    {
        var totalClassifications = await _db.AccountClassifications.CountAsync(cancellationToken);

        var byReviewStatus = await _db.AccountClassifications
            .GroupBy(c => c.ReviewStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status.ToString(), g => g.Count, cancellationToken);

        var byMethod = await _db.AccountClassifications
            .GroupBy(c => c.ClassificationMethod)
            .Select(g => new { Method = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Method, g => g.Count, cancellationToken);

        var approvedCount = byReviewStatus.GetValueOrDefault(ClassificationReviewStatus.Approved.ToString());
        var overriddenCount = byReviewStatus.GetValueOrDefault(ClassificationReviewStatus.Overridden.ToString());
        var rejectedCount = byReviewStatus.GetValueOrDefault(ClassificationReviewStatus.Rejected.ToString());
        var reviewedCount = approvedCount + overriddenCount + rejectedCount;
        var approvalRate = reviewedCount > 0 ? (float)approvedCount / reviewedCount : (float?)null;

        var topOverridden = await (
            from classification in _db.AccountClassifications
            join sourceAccount in _db.SourceAccounts on classification.SourceAccountId equals sourceAccount.Id
            where classification.ReviewStatus == ClassificationReviewStatus.Overridden
            group sourceAccount by sourceAccount.OriginalName into g
            orderby g.Count() descending
            select new TopCorrectedAccountDto(g.Key, g.Count())
        ).Take(TopAccountsLimit).ToListAsync(cancellationToken);

        var topRejected = await (
            from classification in _db.AccountClassifications
            join sourceAccount in _db.SourceAccounts on classification.SourceAccountId equals sourceAccount.Id
            where classification.ReviewStatus == ClassificationReviewStatus.Rejected
            group sourceAccount by sourceAccount.OriginalName into g
            orderby g.Count() descending
            select new TopCorrectedAccountDto(g.Key, g.Count())
        ).Take(TopAccountsLimit).ToListAsync(cancellationToken);

        return Ok(ApiResponse<LearningStatsResponse>.Ok(new LearningStatsResponse(
            totalClassifications, byReviewStatus, byMethod, reviewedCount, approvalRate, topOverridden, topRejected)));
    }
}

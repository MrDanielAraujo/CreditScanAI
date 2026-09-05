using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Classifications;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Fila de revisão humana das classificações (Fase 3 Parte 4). "ReviewedBy"
/// fica sempre nulo por enquanto - ainda não há login funcional no sistema.
/// </summary>
[ApiController]
[Route("api/classifications")]
[AllowAnonymous] // Fase 1 scope: JWT is scaffolded but there's no functional login yet.
public class ClassificationsController : ControllerBase
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    private readonly AppDbContext _db;

    public ClassificationsController(AppDbContext db) => _db = db;

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<PendingClassificationsResponse>>> GetPending(
        [FromQuery] Guid? documentId,
        [FromQuery] Guid? companyId,
        [FromQuery] int limit,
        [FromQuery] int offset,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        offset = Math.Max(offset, 0);

        var query = _db.AccountClassifications
            .Where(c => c.ReviewStatus == ClassificationReviewStatus.NeedsReview);

        if (documentId is not null)
        {
            var sourceAccountIdsForDocument = _db.SourceAccounts
                .Where(a => a.DocumentId == documentId)
                .Select(a => a.Id);
            query = query.Where(c => sourceAccountIdsForDocument.Contains(c.SourceAccountId));
        }

        if (companyId is not null)
        {
            var sourceAccountIdsForCompany =
                from sourceAccount in _db.SourceAccounts
                join document in _db.Documents on sourceAccount.DocumentId equals document.Id
                where document.CompanyId == companyId
                select sourceAccount.Id;
            query = query.Where(c => sourceAccountIdsForCompany.Contains(c.SourceAccountId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderBy(c => c.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.ConfidenceScore,
                c.ClassificationMethod,
                c.Evidence,
                DocumentId = c.SourceAccount!.DocumentId,
                SourceAccountName = c.SourceAccount!.OriginalName,
                SuggestedStandardAccountName = c.StandardAccount != null ? c.StandardAccount.Name : null
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(p => new PendingClassificationDto(
                p.Id, p.DocumentId, p.SourceAccountName, p.SuggestedStandardAccountName, p.ConfidenceScore, p.ClassificationMethod, p.Evidence))
            .ToList();

        return Ok(ApiResponse<PendingClassificationsResponse>.Ok(
            new PendingClassificationsResponse(items, totalCount, offset + items.Count < totalCount)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassificationDetailResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var classification = await _db.AccountClassifications
            .Include(c => c.SourceAccount)
            .Include(c => c.StandardAccount)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (classification is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Classificação não encontrada."));
        }

        var sourceAccount = classification.SourceAccount!;
        var standardAccount = classification.StandardAccount;

        return Ok(ApiResponse<ClassificationDetailResponse>.Ok(new ClassificationDetailResponse(
            classification.Id,
            new SourceAccountSummaryDto(
                sourceAccount.Id, sourceAccount.OriginalName, sourceAccount.NormalizedName,
                sourceAccount.HierarchyLevel, sourceAccount.InferredType, sourceAccount.InferredSubtype),
            standardAccount is null
                ? null
                : new StandardAccountSummaryDto(standardAccount.Id, standardAccount.Code, standardAccount.Name, standardAccount.Description),
            classification.ConfidenceScore,
            classification.ClassificationMethod,
            classification.Evidence,
            classification.ReviewStatus.ToString(),
            classification.CreatedAt)));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ApproveClassificationResponse>>> Approve(
        Guid id, [FromBody] ApproveClassificationRequest? request, CancellationToken cancellationToken)
    {
        var classification = await _db.AccountClassifications.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (classification is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Classificação não encontrada."));
        }

        if (classification.StandardAccountId is null)
        {
            return Conflict(ApiResponse<object>.Fail(
                "NO_SUGGESTION",
                "Esta classificação não tem uma conta padrão sugerida para aprovar - use override para escolher uma manualmente."));
        }

        classification.ReviewStatus = ClassificationReviewStatus.Approved;
        classification.ReviewedAt = DateTime.UtcNow;
        classification.ReviewNotes = request?.Notes;
        classification.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ApproveClassificationResponse>.Ok(
            new ApproveClassificationResponse(classification.Id, classification.ReviewStatus.ToString(), classification.ReviewedAt!.Value)));
    }

    [HttpPost("{id:guid}/override")]
    public async Task<ActionResult<ApiResponse<OverrideClassificationResponse>>> Override(
        Guid id, [FromBody] OverrideClassificationRequest request, CancellationToken cancellationToken)
    {
        var classification = await _db.AccountClassifications.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (classification is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Classificação não encontrada."));
        }

        var newStandardAccount = await _db.StandardAccounts
            .FirstOrDefaultAsync(a => a.Id == request.NewStandardAccountId, cancellationToken);

        if (newStandardAccount is null || newStandardAccount.ChartOfAccountsId != classification.ChartOfAccountsId)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "INVALID_REQUEST",
                "new_standard_account_id inválido ou não pertence ao plano de contas desta classificação."));
        }

        classification.StandardAccountId = newStandardAccount.Id;
        classification.ConfidenceScore = 1.0m;
        classification.ReviewStatus = ClassificationReviewStatus.Overridden;
        classification.ReviewedAt = DateTime.UtcNow;
        classification.ReviewNotes = request.Reason;
        classification.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<OverrideClassificationResponse>.Ok(new OverrideClassificationResponse(
            classification.Id,
            classification.ReviewStatus.ToString(),
            new StandardAccountSummaryDto(newStandardAccount.Id, newStandardAccount.Code, newStandardAccount.Name, newStandardAccount.Description),
            classification.ReviewedAt!.Value)));
    }
}

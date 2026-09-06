using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Reports;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Relatórios (Fase 7). Por enquanto só o relatório de qualidade por
/// documento - o "relatório financeiro" do doc original (demonstrativo
/// exportável) já existe em espírito via GET /api/calculations/... (Fase 4)
/// e GET /api/consolidation/... (Fase 5); duplicar esse dado aqui não
/// agregaria nada.
/// </summary>
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    private static string PeriodLabel(PeriodType periodType, int year, int? quarter) =>
        periodType == PeriodType.Annual ? $"{year} (Anual)" : $"{year} T{quarter}";

    [HttpGet("quality/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<QualityReportResponse>>> GetQualityReport(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Documento não encontrado."));
        }

        var classifications = await (
            from classification in _db.AccountClassifications
            join sourceAccount in _db.SourceAccounts on classification.SourceAccountId equals sourceAccount.Id
            where sourceAccount.DocumentId == documentId
            select new { classification.ReviewStatus, classification.ConfidenceScore }
        ).ToListAsync(cancellationToken);

        var byStatus = classifications.ToLookup(c => c.ReviewStatus);
        var averageConfidence = classifications.Count > 0 ? (float)classifications.Average(c => c.ConfidenceScore) : (float?)null;

        var periodIds = await _db.AccountValues
            .Where(v => v.DocumentId == documentId && v.PeriodId != null)
            .Select(v => v.PeriodId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var periods = await _db.Periods.Where(p => periodIds.Contains(p.Id)).ToListAsync(cancellationToken);

        var calculatedByPeriod = await _db.CalculatedFinancialValues
            .Where(v => v.CompanyId == document.CompanyId && periodIds.Contains(v.PeriodId) && v.Key == CalculationKeys.EquacaoVariancia)
            .ToDictionaryAsync(v => v.PeriodId, v => v.Value, cancellationToken);

        var periodStatuses = periods
            .Select(p =>
            {
                var hasCalculation = calculatedByPeriod.TryGetValue(p.Id, out var variance);
                return new PeriodEquationStatusDto(
                    p.Id,
                    PeriodLabel(p.PeriodType, p.Year, p.Quarter),
                    hasCalculation ? Math.Abs(variance) <= FinancialCalculationService.EquationTolerance : null,
                    hasCalculation ? variance : null);
            })
            .OrderBy(p => p.PeriodLabel)
            .ToList();

        return Ok(ApiResponse<QualityReportResponse>.Ok(new QualityReportResponse(
            document.Id,
            document.FileName,
            classifications.Count,
            byStatus[ClassificationReviewStatus.Pending].Count(),
            byStatus[ClassificationReviewStatus.NeedsReview].Count(),
            byStatus[ClassificationReviewStatus.Approved].Count(),
            byStatus[ClassificationReviewStatus.Overridden].Count(),
            byStatus[ClassificationReviewStatus.Rejected].Count(),
            averageConfidence,
            periodStatuses)));
    }
}

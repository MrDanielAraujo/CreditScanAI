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
/// Relatórios (Fase 7: qualidade por documento; Fase 10: exportar o
/// demonstrativo em PDF/Excel). O demonstrativo em si (Balanço/DRE/
/// Indicadores) já existe em espírito via GET /api/calculations/... (Fase 4)
/// e GET /api/consolidation/... (Fase 5) - aqui só empacota os mesmos
/// valores num arquivo baixável, reaproveitando FinancialReportFileBuilder.
/// </summary>
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ConsolidationService _consolidationService;

    public ReportsController(AppDbContext db, ConsolidationService consolidationService)
    {
        _db = db;
        _consolidationService = consolidationService;
    }

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

    [HttpGet("financial-statement/company/{companyId:guid}/periods/{periodId:guid}")]
    public async Task<IActionResult> ExportCompanyStatement(
        Guid companyId, Guid periodId, [FromQuery] string format, CancellationToken cancellationToken)
    {
        if (!TryParseFormat(format, out var isExcel))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "format deve ser 'pdf' ou 'xlsx'."));
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Empresa não encontrada."));
        }

        var period = await _db.Periods.FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        if (period is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Período não encontrado."));
        }

        var rows = await _db.CalculatedFinancialValues
            .Where(v => v.CompanyId == companyId && v.PeriodId == periodId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_CALCULATED", "Ainda não há cálculo para esta empresa/período."));
        }

        var values = rows.ToDictionary(r => r.Key, r => r.Value);
        var variance = values.GetValueOrDefault(CalculationKeys.EquacaoVariancia);
        var data = new FinancialReportFileBuilder.ReportData(
            company.Name,
            PeriodLabel(period.PeriodType, period.Year, period.Quarter),
            values,
            Math.Abs(variance) <= FinancialCalculationService.EquationTolerance,
            variance);

        return BuildFileResult(data, isExcel, company.Name);
    }

    [HttpGet("financial-statement/consolidated")]
    public async Task<IActionResult> ExportConsolidatedStatement(
        [FromQuery] Guid periodId, [FromQuery] List<Guid> companyIds, [FromQuery] string format, CancellationToken cancellationToken)
    {
        if (!TryParseFormat(format, out var isExcel))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "format deve ser 'pdf' ou 'xlsx'."));
        }

        if (companyIds is null || companyIds.Count < 2)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Informe ao menos 2 companyIds."));
        }

        var period = await _db.Periods.FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        if (period is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Período não encontrado."));
        }

        var outcome = await _consolidationService.ConsolidateAsync(periodId, companyIds, cancellationToken);
        if (!outcome.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "MISSING_CALCULATION", $"Empresa(s) sem cálculo para este período: {string.Join(", ", outcome.MissingCompanyIds)}."));
        }

        var companyNames = await _db.Companies
            .Where(c => companyIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        var variance = outcome.Values.GetValueOrDefault(CalculationKeys.EquacaoVariancia);
        var data = new FinancialReportFileBuilder.ReportData(
            $"Consolidado: {string.Join(", ", companyNames)}",
            PeriodLabel(period.PeriodType, period.Year, period.Quarter),
            outcome.Values,
            Math.Abs(variance) <= FinancialCalculationService.EquationTolerance,
            variance);

        return BuildFileResult(data, isExcel, "consolidado");
    }

    private static bool TryParseFormat(string? format, out bool isExcel)
    {
        switch (format?.ToLowerInvariant())
        {
            case "pdf":
                isExcel = false;
                return true;
            case "xlsx":
                isExcel = true;
                return true;
            default:
                isExcel = false;
                return false;
        }
    }

    private FileContentResult BuildFileResult(FinancialReportFileBuilder.ReportData data, bool isExcel, string fileNameHint)
    {
        var safeName = string.Join("_", fileNameHint.Split(Path.GetInvalidFileNameChars()));

        if (isExcel)
        {
            var bytes = FinancialReportFileBuilder.BuildExcel(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safeName}.xlsx");
        }

        var pdfBytes = FinancialReportFileBuilder.BuildPdf(data);
        return File(pdfBytes, "application/pdf", $"{safeName}.pdf");
    }
}

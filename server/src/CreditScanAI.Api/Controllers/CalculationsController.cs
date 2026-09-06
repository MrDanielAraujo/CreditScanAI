using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Calculations;
using CreditScanAI.Api.Services;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Fase 4 (Motor de Cálculos): calcula totais/indicadores sob demanda para
/// uma empresa+período (não é encadeado automaticamente após a classificação
/// - o cálculo completo geralmente precisa de mais de um documento do mesmo
/// período, ex: balanço + DRE).
/// </summary>
[ApiController]
[Route("api/calculations")]
public class CalculationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly FinancialCalculationService _calculationService;

    public CalculationsController(AppDbContext db, FinancialCalculationService calculationService)
    {
        _db = db;
        _calculationService = calculationService;
    }

    [Authorize(Policy = AuthorizationPolicies.CanConsolidate)]
    [HttpPost("companies/{companyId:guid}/periods/{periodId:guid}/calculate")]
    public async Task<ActionResult<ApiResponse<CalculationResultResponse>>> Calculate(
        Guid companyId, Guid periodId, CancellationToken cancellationToken)
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Empresa não encontrada."));
        }

        var periodExists = await _db.Periods.AnyAsync(p => p.Id == periodId, cancellationToken);
        if (!periodExists)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Período não encontrado."));
        }

        var values = await _calculationService.CalculateAsync(companyId, periodId, cancellationToken);
        return Ok(BuildResponse(companyId, periodId, values));
    }

    [HttpGet("companies/{companyId:guid}/periods/{periodId:guid}")]
    public async Task<ActionResult<ApiResponse<CalculationResultResponse>>> GetResults(
        Guid companyId, Guid periodId, CancellationToken cancellationToken)
    {
        var rows = await _db.CalculatedFinancialValues
            .Where(v => v.CompanyId == companyId && v.PeriodId == periodId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_CALCULATED", "Ainda não há cálculo para esta empresa/período - use o endpoint de calcular primeiro."));
        }

        var values = rows.ToDictionary(r => r.Key, r => r.Value);
        return Ok(BuildResponse(companyId, periodId, values, rows.Max(r => r.UpdatedAt)));
    }

    private static ApiResponse<CalculationResultResponse> BuildResponse(
        Guid companyId, Guid periodId, Dictionary<string, decimal> values, DateTime? calculatedAt = null)
    {
        var variance = values.GetValueOrDefault(CalculationKeys.EquacaoVariancia);
        var balanced = Math.Abs(variance) <= FinancialCalculationService.EquationTolerance;

        return ApiResponse<CalculationResultResponse>.Ok(new CalculationResultResponse(
            companyId, periodId, values, balanced, variance, calculatedAt ?? DateTime.UtcNow));
    }
}

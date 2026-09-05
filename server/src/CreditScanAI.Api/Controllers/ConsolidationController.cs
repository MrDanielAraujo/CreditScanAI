using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Consolidation;
using CreditScanAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditScanAI.Api.Controllers;

/// <summary>Fase 5 (Motor de Consolidação): Consolidação Simples (soma) sob demanda.</summary>
[ApiController]
[Route("api/consolidation")]
[AllowAnonymous] // Fase 1 scope: JWT is scaffolded but there's no functional login yet.
public class ConsolidationController : ControllerBase
{
    private readonly ConsolidationService _consolidationService;

    public ConsolidationController(ConsolidationService consolidationService) => _consolidationService = consolidationService;

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<ConsolidationResultResponse>>> Calculate(
        ConsolidateRequest request, CancellationToken cancellationToken)
    {
        if (request.CompanyIds is null || request.CompanyIds.Count < 2)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Informe ao menos 2 empresas para consolidar."));
        }

        var outcome = await _consolidationService.ConsolidateAsync(request.PeriodId, request.CompanyIds, cancellationToken);

        if (!outcome.Success)
        {
            var missing = string.Join(", ", outcome.MissingCompanyIds);
            return BadRequest(ApiResponse<object>.Fail(
                "MISSING_CALCULATION",
                $"As seguintes empresas ainda não têm cálculo (Fase 4) para este período: {missing}. Calcule cada uma antes de consolidar."));
        }

        var variance = outcome.Values.GetValueOrDefault(CalculationKeys.EquacaoVariancia);
        var balanced = Math.Abs(variance) <= FinancialCalculationService.EquationTolerance;

        var checks = outcome.ReconciliationChecks
            .Select(c => new ReconciliationCheckDto(c.CheckId, c.Description, c.Passed, c.ExpectedValue, c.ActualValue, c.Variance, c.ErrorMessage))
            .ToList();

        return Ok(ApiResponse<ConsolidationResultResponse>.Ok(new ConsolidationResultResponse(
            request.PeriodId, request.CompanyIds, outcome.Values, balanced, variance, checks, checks.All(c => c.Passed))));
    }
}

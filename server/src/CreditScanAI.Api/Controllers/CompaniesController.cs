using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Read-only for now: there's no Company CRUD API yet (not a Fase 1/2
/// deliverable). This exists only so the upload form has something to
/// populate its company dropdown with.
/// </summary>
[ApiController]
[Route("api/companies")]
[AllowAnonymous]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CompaniesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CompanyDto>>>> List(CancellationToken cancellationToken)
    {
        var companies = await _db.Companies
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Code, c.Name))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<CompanyDto>>.Ok(companies));
    }

    /// <summary>
    /// Períodos (Fase 4) mostra só os que essa empresa já tem algum valor
    /// extraído - a lista de opções para o seletor de período do Dashboard.
    /// </summary>
    [HttpGet("{companyId:guid}/periods")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PeriodDto>>>> ListPeriods(Guid companyId, CancellationToken cancellationToken)
    {
        var periodIds = await (
            from accountValue in _db.AccountValues
            join sourceAccount in _db.SourceAccounts on accountValue.SourceAccountId equals sourceAccount.Id
            join document in _db.Documents on sourceAccount.DocumentId equals document.Id
            where document.CompanyId == companyId && accountValue.PeriodId != null
            select accountValue.PeriodId!.Value
        ).Distinct().ToListAsync(cancellationToken);

        var periods = await _db.Periods
            .Where(p => periodIds.Contains(p.Id))
            .OrderByDescending(p => p.EndDate)
            .Select(p => new PeriodDto(p.Id, p.PeriodType.ToString(), p.Year, p.Quarter, p.EndDate))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PeriodDto>>.Ok(periods));
    }
}

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
}

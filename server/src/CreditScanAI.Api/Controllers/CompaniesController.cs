using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// CRUD de empresas (Fase 5 - necessário para sequer ter uma segunda
/// empresa real com a qual testar a consolidação).
/// </summary>
[ApiController]
[Route("api/companies")]
[AllowAnonymous] // Fase 1 scope: JWT is scaffolded but there's no functional login yet.
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public CompaniesController(AppDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    private static CompanyDto ToDto(Company c) =>
        new(c.Id, c.Code, c.Name, c.LegalName, c.Cnpj, c.Industry, c.FiscalYearEnd, c.ReportingCurrency);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CompanyDto>>>> List(CancellationToken cancellationToken)
    {
        var companies = await _db.Companies
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<CompanyDto>>.Ok(companies.Select(ToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Empresa não encontrada."));
        }

        return Ok(ApiResponse<CompanyDto>.Ok(ToDto(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Create(UpsertCompanyRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var codeExists = await _db.Companies.AnyAsync(c => c.TenantId == tenantId && c.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe uma Empresa com o código '{request.Code}'."));
        }

        var entity = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = request.Code,
            Name = request.Name,
            LegalName = request.LegalName,
            Cnpj = request.Cnpj,
            Industry = request.Industry,
            FiscalYearEnd = request.FiscalYearEnd,
            ReportingCurrency = string.IsNullOrWhiteSpace(request.ReportingCurrency) ? "BRL" : request.ReportingCurrency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Companies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ApiResponse<CompanyDto>.Ok(ToDto(entity)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Update(Guid id, UpsertCompanyRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Empresa não encontrada."));
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var codeTaken = await _db.Companies.AnyAsync(c => c.TenantId == entity.TenantId && c.Code == request.Code && c.Id != id, cancellationToken);
        if (codeTaken)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe uma Empresa com o código '{request.Code}'."));
        }

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.LegalName = request.LegalName;
        entity.Cnpj = request.Cnpj;
        entity.Industry = request.Industry;
        entity.FiscalYearEnd = request.FiscalYearEnd;
        entity.ReportingCurrency = string.IsNullOrWhiteSpace(request.ReportingCurrency) ? entity.ReportingCurrency : request.ReportingCurrency;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CompanyDto>.Ok(ToDto(entity)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Empresa não encontrada."));
        }

        var inUse = await _db.Documents.AnyAsync(d => d.CompanyId == id, cancellationToken);
        if (inUse)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Esta Empresa possui documentos e não pode ser excluída."));
        }

        _db.Companies.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
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

    /// <summary>
    /// Períodos (Fase 5) mostra só os que essa empresa já tem um cálculo
    /// (Fase 4) feito - é isso que a Consolidação exige de cada empresa
    /// selecionada, diferente de ListPeriods (que só olha dado bruto extraído).
    /// </summary>
    [HttpGet("{companyId:guid}/calculated-periods")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PeriodDto>>>> ListCalculatedPeriods(Guid companyId, CancellationToken cancellationToken)
    {
        var periodIds = await _db.CalculatedFinancialValues
            .Where(v => v.CompanyId == companyId)
            .Select(v => v.PeriodId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var periods = await _db.Periods
            .Where(p => periodIds.Contains(p.Id))
            .OrderByDescending(p => p.EndDate)
            .Select(p => new PeriodDto(p.Id, p.PeriodType.ToString(), p.Year, p.Quarter, p.EndDate))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PeriodDto>>.Ok(periods));
    }
}

using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Registrations;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

[ApiController]
[Route("api/standard-accounts")]
[AllowAnonymous]
public class StandardAccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public StandardAccountsController(AppDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    private static StandardAccountDto ToDto(StandardAccount a) =>
        new(a.Id, a.ChartOfAccountsId, a.AccountTypeId, a.AccountSubtypeId, a.Code, a.Name, a.Description);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StandardAccountDto>>>> List(
        [FromQuery] Guid? chartOfAccountsId,
        CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var query = _db.StandardAccounts.Where(a => a.TenantId == tenantId);
        if (chartOfAccountsId.HasValue)
        {
            query = query.Where(a => a.ChartOfAccountsId == chartOfAccountsId.Value);
        }

        var items = await query
            .OrderBy(a => a.Code)
            .Select(a => ToDto(a))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<StandardAccountDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StandardAccountDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.StandardAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Conta não encontrada."));
        }

        return Ok(ApiResponse<StandardAccountDto>.Ok(ToDto(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StandardAccountDto>>> Create(UpsertStandardAccountRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var validation = await ValidateAsync(tenantId, request, existingId: null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var entity = new StandardAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChartOfAccountsId = request.ChartOfAccountsId,
            AccountTypeId = request.AccountTypeId,
            AccountSubtypeId = request.AccountSubtypeId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.StandardAccounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ApiResponse<StandardAccountDto>.Ok(ToDto(entity)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StandardAccountDto>>> Update(Guid id, UpsertStandardAccountRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.StandardAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Conta não encontrada."));
        }

        var validation = await ValidateAsync(entity.TenantId, request, existingId: id, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        entity.ChartOfAccountsId = request.ChartOfAccountsId;
        entity.AccountTypeId = request.AccountTypeId;
        entity.AccountSubtypeId = request.AccountSubtypeId;
        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<StandardAccountDto>.Ok(ToDto(entity)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.StandardAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Conta não encontrada."));
        }

        _db.StandardAccounts.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }

    /// <summary>Returns null when valid, or the error ActionResult to return.</summary>
    private async Task<ActionResult<ApiResponse<StandardAccountDto>>?> ValidateAsync(
        Guid tenantId,
        UpsertStandardAccountRequest request,
        Guid? existingId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var chartExists = await _db.ChartOfAccounts.AnyAsync(c => c.Id == request.ChartOfAccountsId && c.TenantId == tenantId, cancellationToken);
        if (!chartExists)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "chart_of_accounts_id não corresponde a nenhum Plano de Contas cadastrado."));
        }

        var isCompatible = await _db.TypeSubtypeCompatibilities.AnyAsync(c =>
            c.TenantId == tenantId &&
            c.AccountTypeId == request.AccountTypeId &&
            c.AccountSubtypeId == request.AccountSubtypeId &&
            c.IsAllowed,
            cancellationToken);
        if (!isCompatible)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "INVALID_REQUEST",
                "A combinação de Tipo e Subtipo informada não é compatível. Cadastre essa compatibilidade antes (ela é criada automaticamente ao criar um Subtipo para um Tipo)."));
        }

        var codeTaken = await _db.StandardAccounts.AnyAsync(a =>
            a.ChartOfAccountsId == request.ChartOfAccountsId &&
            a.Code == request.Code &&
            a.Id != existingId,
            cancellationToken);
        if (codeTaken)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe uma Conta com o código '{request.Code}' neste Plano de Contas."));
        }

        return null;
    }
}

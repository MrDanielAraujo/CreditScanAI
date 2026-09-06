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
[Route("api/account-subtypes")]
public class AccountSubtypesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public AccountSubtypesController(AppDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    private static AccountSubtypeDto ToDto(AccountSubtype a) =>
        new(a.Id, a.AccountTypeId, a.Code, a.Name, a.Description, a.SequenceOrder);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountSubtypeDto>>>> List(CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var items = await _db.AccountSubtypes
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.SequenceOrder)
            .Select(a => ToDto(a))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AccountSubtypeDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountSubtypeDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountSubtypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Subtipo não encontrado."));
        }

        return Ok(ApiResponse<AccountSubtypeDto>.Ok(ToDto(entity)));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountSubtypeDto>>> Create(UpsertAccountSubtypeRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var accountType = await _db.AccountTypes.FirstOrDefaultAsync(t => t.Id == request.AccountTypeId && t.TenantId == tenantId, cancellationToken);
        if (accountType is null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "account_type_id não corresponde a nenhum Tipo cadastrado."));
        }

        var codeExists = await _db.AccountSubtypes.AnyAsync(a => a.TenantId == tenantId && a.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Subtipo com o código '{request.Code}'."));
        }

        var entity = new AccountSubtype
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountTypeId = request.AccountTypeId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SequenceOrder = request.SequenceOrder,
            CreatedAt = DateTime.UtcNow
        };
        _db.AccountSubtypes.Add(entity);

        // A subtype is always compatible with the type it was created under -
        // additional (type, subtype) combinations can still be granted later
        // via a separate compatibility entry, but this one is implicit.
        _db.TypeSubtypeCompatibilities.Add(new TypeSubtypeCompatibility
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountTypeId = request.AccountTypeId,
            AccountSubtypeId = entity.Id,
            IsAllowed = true
        });

        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ApiResponse<AccountSubtypeDto>.Ok(ToDto(entity)));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountSubtypeDto>>> Update(Guid id, UpsertAccountSubtypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountSubtypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Subtipo não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var accountType = await _db.AccountTypes.FirstOrDefaultAsync(t => t.Id == request.AccountTypeId && t.TenantId == entity.TenantId, cancellationToken);
        if (accountType is null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "account_type_id não corresponde a nenhum Tipo cadastrado."));
        }

        var codeTaken = await _db.AccountSubtypes.AnyAsync(a => a.TenantId == entity.TenantId && a.Code == request.Code && a.Id != id, cancellationToken);
        if (codeTaken)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Subtipo com o código '{request.Code}'."));
        }

        entity.AccountTypeId = request.AccountTypeId;
        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SequenceOrder = request.SequenceOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AccountSubtypeDto>.Ok(ToDto(entity)));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountSubtypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Subtipo não encontrado."));
        }

        var inUse = await _db.StandardAccounts.AnyAsync(s => s.AccountSubtypeId == id, cancellationToken);
        if (inUse)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Este Subtipo está em uso por Contas e não pode ser excluído."));
        }

        var compatibilities = _db.TypeSubtypeCompatibilities.Where(c => c.AccountSubtypeId == id);
        _db.TypeSubtypeCompatibilities.RemoveRange(compatibilities);
        _db.AccountSubtypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }
}

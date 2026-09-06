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
[Route("api/account-types")]
public class AccountTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public AccountTypesController(AppDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountTypeDto>>>> List(CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var items = await _db.AccountTypes
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.SequenceOrder)
            .Select(a => new AccountTypeDto(a.Id, a.Code, a.Name, a.Description, a.SequenceOrder))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AccountTypeDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountTypeDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountTypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Tipo não encontrado."));
        }

        return Ok(ApiResponse<AccountTypeDto>.Ok(new AccountTypeDto(entity.Id, entity.Code, entity.Name, entity.Description, entity.SequenceOrder)));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountTypeDto>>> Create(UpsertAccountTypeRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var codeExists = await _db.AccountTypes.AnyAsync(a => a.TenantId == tenantId && a.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Tipo com o código '{request.Code}'."));
        }

        var entity = new AccountType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SequenceOrder = request.SequenceOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.AccountTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new AccountTypeDto(entity.Id, entity.Code, entity.Name, entity.Description, entity.SequenceOrder);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ApiResponse<AccountTypeDto>.Ok(dto));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountTypeDto>>> Update(Guid id, UpsertAccountTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountTypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Tipo não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Código e nome são obrigatórios."));
        }

        var codeTaken = await _db.AccountTypes.AnyAsync(a => a.TenantId == entity.TenantId && a.Code == request.Code && a.Id != id, cancellationToken);
        if (codeTaken)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Tipo com o código '{request.Code}'."));
        }

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SequenceOrder = request.SequenceOrder;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AccountTypeDto>.Ok(new AccountTypeDto(entity.Id, entity.Code, entity.Name, entity.Description, entity.SequenceOrder)));
    }

    [Authorize(Policy = AuthorizationPolicies.CanAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.AccountTypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Tipo não encontrado."));
        }

        var inUse = await _db.AccountSubtypes.AnyAsync(s => s.AccountTypeId == id, cancellationToken)
            || await _db.StandardAccounts.AnyAsync(s => s.AccountTypeId == id, cancellationToken);
        if (inUse)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Este Tipo está em uso por Subtipos ou Contas e não pode ser excluído."));
        }

        _db.AccountTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }
}

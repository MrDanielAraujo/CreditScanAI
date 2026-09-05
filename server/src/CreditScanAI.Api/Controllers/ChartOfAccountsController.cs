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
[Route("api/chart-of-accounts")]
[AllowAnonymous]
public class ChartOfAccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public ChartOfAccountsController(AppDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    private static ChartOfAccountsDto ToDto(ChartOfAccounts c) => new(c.Id, c.Name, c.Description, c.IsDefault);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChartOfAccountsDto>>>> List(CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var items = await _db.ChartOfAccounts
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ChartOfAccountsDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ChartOfAccountsDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.ChartOfAccounts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Plano de Contas não encontrado."));
        }

        return Ok(ApiResponse<ChartOfAccountsDto>.Ok(ToDto(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ChartOfAccountsDto>>> Create(UpsertChartOfAccountsRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Nome é obrigatório."));
        }

        var nameExists = await _db.ChartOfAccounts.AnyAsync(c => c.TenantId == tenantId && c.Name == request.Name, cancellationToken);
        if (nameExists)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Plano de Contas chamado '{request.Name}'."));
        }

        var isFirstChart = !await _db.ChartOfAccounts.AnyAsync(c => c.TenantId == tenantId, cancellationToken);

        var entity = new ChartOfAccounts
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            // The very first chart a tenant creates becomes the default
            // automatically - otherwise classification would have nothing to
            // use and nothing would ever prompt the user to pick one.
            IsDefault = isFirstChart,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ChartOfAccounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ApiResponse<ChartOfAccountsDto>.Ok(ToDto(entity)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ChartOfAccountsDto>>> Update(Guid id, UpsertChartOfAccountsRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.ChartOfAccounts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Plano de Contas não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Nome é obrigatório."));
        }

        var nameTaken = await _db.ChartOfAccounts.AnyAsync(c => c.TenantId == entity.TenantId && c.Name == request.Name && c.Id != id, cancellationToken);
        if (nameTaken)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", $"Já existe um Plano de Contas chamado '{request.Name}'."));
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ChartOfAccountsDto>.Ok(ToDto(entity)));
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult<ApiResponse<ChartOfAccountsDto>>> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.ChartOfAccounts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Plano de Contas não encontrado."));
        }

        // A single SaveChangesAsync is already atomic for a relational
        // provider (EF Core wraps all pending changes in one transaction) -
        // no need for an explicit BeginTransaction/Commit, which the
        // InMemory provider used in tests doesn't support the same way.
        var currentDefaults = await _db.ChartOfAccounts
            .Where(c => c.TenantId == entity.TenantId && c.IsDefault && c.Id != id)
            .ToListAsync(cancellationToken);

        foreach (var current in currentDefaults)
        {
            current.IsDefault = false;
        }

        entity.IsDefault = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ChartOfAccountsDto>.Ok(ToDto(entity)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.ChartOfAccounts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Plano de Contas não encontrado."));
        }

        if (entity.IsDefault)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Não é possível excluir o Plano de Contas padrão. Defina outro como padrão primeiro."));
        }

        var inUse = await _db.Documents.AnyAsync(d => d.ChartOfAccountsId == id, cancellationToken);
        if (inUse)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Este Plano de Contas já foi usado para classificar documentos e não pode ser excluído."));
        }

        // StandardAccounts cascade with the chart itself (configured on the FK).
        _db.ChartOfAccounts.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }));
    }
}

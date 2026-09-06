using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Resolves "the" tenant for requests. Fase 8 Parte 1: now reads the
/// tenant_id claim from the authenticated user's JWT when one is present.
/// Falls back to the single seeded tenant (oldest by CreatedAt) when there's
/// no authenticated user yet - existing endpoints stay [AllowAnonymous]
/// until Fase 8 Parte 2 protects them, so this fallback is what keeps them
/// working in the meantime. Once every endpoint requires auth, this
/// fallback becomes unreachable and can be removed.
/// </summary>
public interface ICurrentTenantProvider
{
    Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken);
}

public class CurrentTenantProvider : ICurrentTenantProvider
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantProvider(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken)
    {
        var claimValue = _httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimNames.TenantId)?.Value;
        if (claimValue is not null && Guid.TryParse(claimValue, out var claimTenantId))
        {
            return claimTenantId;
        }

        var tenantId = await _db.Tenants
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Nenhum tenant cadastrado.");
        }

        return tenantId;
    }
}

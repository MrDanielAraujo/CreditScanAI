using CreditScanAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Services;

/// <summary>
/// Resolves "the" tenant for requests. There's no functional login yet (Fase
/// 1 scope), so every request works against the single seeded tenant -
/// mirrors DbSeeder's own resolution (oldest tenant by CreatedAt). Replace
/// with a claims-based lookup once real auth exists.
/// </summary>
public interface ICurrentTenantProvider
{
    Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken);
}

public class CurrentTenantProvider : ICurrentTenantProvider
{
    private readonly AppDbContext _db;

    public CurrentTenantProvider(AppDbContext db) => _db = db;

    public async Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken)
    {
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

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a default tenant and the base chart-of-account taxonomy (tipos/subtipos)
/// described in 01_MODELO_DADOS_BANCO_DADOS.md. Intended for local/dev environments.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Default",
                Active = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasCompany = await db.Companies.AnyAsync(c => c.TenantId == tenant.Id, cancellationToken);
        if (!hasCompany)
        {
            // There's no Company CRUD API yet (not a Fase 1/2 deliverable), and
            // document upload requires a company_id - seed one so the upload
            // flow is testable end-to-end in dev.
            db.Companies.Add(new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Code = "DEFAULT",
                Name = "Empresa Padrão",
                ReportingCurrency = "BRL",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAccountTypes = await db.AccountTypes.AnyAsync(a => a.TenantId == tenant.Id, cancellationToken);
        if (hasAccountTypes)
        {
            return;
        }

        var ativo = new AccountType { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "ATIVO", Name = "Ativo", SequenceOrder = 1, CreatedAt = DateTime.UtcNow };
        var passivo = new AccountType { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "PASSIVO", Name = "Passivo", SequenceOrder = 2, CreatedAt = DateTime.UtcNow };
        var dre = new AccountType { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "DRE", Name = "Demonstração do Resultado", SequenceOrder = 3, CreatedAt = DateTime.UtcNow };
        db.AccountTypes.AddRange(ativo, passivo, dre);

        var circulante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, Code = "CIRCULANTE", Name = "Circulante", SequenceOrder = 1, CreatedAt = DateTime.UtcNow };
        var naoCirculante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, Code = "NAO_CIRCULANTE", Name = "Não Circulante", SequenceOrder = 2, CreatedAt = DateTime.UtcNow };
        var permanente = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, Code = "PERMANENTE", Name = "Permanente", SequenceOrder = 3, CreatedAt = DateTime.UtcNow };
        var patrimonioLiquido = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = passivo.Id, Code = "PL", Name = "Patrimônio Líquido", SequenceOrder = 4, CreatedAt = DateTime.UtcNow };
        db.AccountSubtypes.AddRange(circulante, naoCirculante, permanente, patrimonioLiquido);

        db.TypeSubtypeCompatibilities.AddRange(
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, AccountSubtypeId = circulante.Id, IsAllowed = true, SequenceOrder = 1 },
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, AccountSubtypeId = naoCirculante.Id, IsAllowed = true, SequenceOrder = 2 },
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = ativo.Id, AccountSubtypeId = permanente.Id, IsAllowed = true, SequenceOrder = 3 },
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = passivo.Id, AccountSubtypeId = circulante.Id, IsAllowed = true, SequenceOrder = 1 },
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = passivo.Id, AccountSubtypeId = naoCirculante.Id, IsAllowed = true, SequenceOrder = 2 },
            new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = passivo.Id, AccountSubtypeId = patrimonioLiquido.Id, IsAllowed = true, SequenceOrder = 3 }
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}

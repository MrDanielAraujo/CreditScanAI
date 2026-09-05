using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a default tenant and the base chart-of-account taxonomy (tipos/subtipos)
/// described in 01_MODELO_DADOS_BANCO_DADOS.md. Intended for local/dev environments.
/// Each section checks its own existence, so re-running after only some of
/// it exists (e.g. types/subtypes from an earlier run) still fills in the rest.
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

        var ativo = await GetOrCreateAccountTypeAsync(db, tenant.Id, "ATIVO", "Ativo", 1, cancellationToken);
        var passivo = await GetOrCreateAccountTypeAsync(db, tenant.Id, "PASSIVO", "Passivo", 2, cancellationToken);
        var dre = await GetOrCreateAccountTypeAsync(db, tenant.Id, "DRE", "Demonstração do Resultado", 3, cancellationToken);

        var circulante = await GetOrCreateAccountSubtypeAsync(db, tenant.Id, ativo.Id, "CIRCULANTE", "Circulante", 1, cancellationToken);
        var naoCirculante = await GetOrCreateAccountSubtypeAsync(db, tenant.Id, ativo.Id, "NAO_CIRCULANTE", "Não Circulante", 2, cancellationToken);
        var permanente = await GetOrCreateAccountSubtypeAsync(db, tenant.Id, ativo.Id, "PERMANENTE", "Permanente", 3, cancellationToken);
        var patrimonioLiquido = await GetOrCreateAccountSubtypeAsync(db, tenant.Id, passivo.Id, "PL", "Patrimônio Líquido", 4, cancellationToken);
        // DRE line items (receita/despesa/custo) don't have a circulante/não-circulante
        // notion - this generic subtype exists so DRE standard accounts have
        // something valid to point AccountSubtypeId at.
        var geral = await GetOrCreateAccountSubtypeAsync(db, tenant.Id, dre.Id, "GERAL", "Geral", 5, cancellationToken);

        await EnsureCompatibilityAsync(db, tenant.Id, ativo.Id, circulante.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, ativo.Id, naoCirculante.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, ativo.Id, permanente.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, passivo.Id, circulante.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, passivo.Id, naoCirculante.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, passivo.Id, patrimonioLiquido.Id, cancellationToken);
        await EnsureCompatibilityAsync(db, tenant.Id, dre.Id, geral.Id, cancellationToken);

        var hasChart = await db.ChartOfAccounts.AnyAsync(c => c.TenantId == tenant.Id, cancellationToken);
        if (hasChart)
        {
            return;
        }

        var chart = new ChartOfAccounts
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Plano Padrão",
            Description = "Plano de contas inicial, com base nas contas observadas nos documentos de exemplo.",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChartOfAccounts.Add(chart);

        StandardAccount Account(string code, string name, AccountType type, AccountSubtype subtype) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ChartOfAccountsId = chart.Id,
            AccountTypeId = type.Id,
            AccountSubtypeId = subtype.Id,
            Code = code,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.StandardAccounts.AddRange(
            Account("ATIVO_CIRC_CAIXA", "Caixa e Equivalentes de Caixa", ativo, circulante),
            Account("ATIVO_CIRC_APLIC_FIN", "Aplicações Financeiras", ativo, circulante),
            Account("ATIVO_CIRC_CONTAS_RECEBER", "Contas a Receber", ativo, circulante),
            Account("ATIVO_CIRC_ESTOQUES", "Estoques", ativo, circulante),
            Account("ATIVO_CIRC_DESP_ANTECIPADAS", "Despesas Antecipadas", ativo, circulante),
            Account("ATIVO_NCIRC_APLIC_FIN_LP", "Aplicações Financeiras de Longo Prazo", ativo, naoCirculante),
            Account("ATIVO_NCIRC_IMOBILIZADO", "Imobilizado", ativo, naoCirculante),
            Account("ATIVO_NCIRC_INTANGIVEL", "Intangível", ativo, naoCirculante),
            Account("PASSIVO_CIRC_FORNECEDORES", "Fornecedores", passivo, circulante),
            Account("PASSIVO_CIRC_IMPOSTOS", "Impostos a Pagar", passivo, circulante),
            Account("PASSIVO_CIRC_SALARIOS", "Salários e Encargos a Pagar", passivo, circulante),
            Account("PASSIVO_CIRC_EMPRESTIMOS", "Empréstimos e Financiamentos", passivo, circulante),
            Account("PASSIVO_NCIRC_PROVISOES", "Provisões", passivo, naoCirculante),
            Account("PASSIVO_PL_CAPITAL", "Patrimônio Social", passivo, patrimonioLiquido),
            Account("PASSIVO_PL_SUPERAVIT", "Superávit (Déficit) Acumulado", passivo, patrimonioLiquido),
            Account("DRE_RECEITA", "Receitas", dre, geral),
            Account("DRE_CUSTO", "Custos", dre, geral),
            Account("DRE_DESPESA", "Despesas", dre, geral));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<AccountType> GetOrCreateAccountTypeAsync(
        AppDbContext db, Guid tenantId, string code, string name, int sequenceOrder, CancellationToken cancellationToken)
    {
        var existing = await db.AccountTypes.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == code, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var entity = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Name = name, SequenceOrder = sequenceOrder, CreatedAt = DateTime.UtcNow };
        db.AccountTypes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<AccountSubtype> GetOrCreateAccountSubtypeAsync(
        AppDbContext db, Guid tenantId, Guid accountTypeId, string code, string name, int sequenceOrder, CancellationToken cancellationToken)
    {
        var existing = await db.AccountSubtypes.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == code, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var entity = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = accountTypeId, Code = code, Name = name, SequenceOrder = sequenceOrder, CreatedAt = DateTime.UtcNow };
        db.AccountSubtypes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task EnsureCompatibilityAsync(
        AppDbContext db, Guid tenantId, Guid accountTypeId, Guid accountSubtypeId, CancellationToken cancellationToken)
    {
        var exists = await db.TypeSubtypeCompatibilities.AnyAsync(c =>
            c.TenantId == tenantId && c.AccountTypeId == accountTypeId && c.AccountSubtypeId == accountSubtypeId, cancellationToken);
        if (exists)
        {
            return;
        }

        db.TypeSubtypeCompatibilities.Add(new TypeSubtypeCompatibility
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountTypeId = accountTypeId,
            AccountSubtypeId = accountSubtypeId,
            IsAllowed = true
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

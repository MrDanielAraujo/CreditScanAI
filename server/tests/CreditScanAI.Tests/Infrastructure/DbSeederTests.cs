using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using CreditScanAI.Infrastructure.Persistence.Seed;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Tests.Infrastructure;

public class DbSeederTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesDefaultTenantAndTaxonomy()
    {
        // Arrange
        await using var db = CreateInMemoryContext();

        // Act
        await DbSeeder.SeedAsync(db);

        // Assert
        var tenant = db.Tenants.Should().ContainSingle().Subject;

        db.AccountTypes.Where(a => a.TenantId == tenant.Id)
            .Select(a => a.Code)
            .Should().BeEquivalentTo("ATIVO", "PASSIVO", "DRE");

        db.AccountSubtypes.Where(a => a.TenantId == tenant.Id)
            .Select(a => a.Code)
            .Should().BeEquivalentTo("CIRCULANTE", "NAO_CIRCULANTE", "PERMANENTE", "PL", "RECEITA", "CUSTO", "DESPESA");

        db.TypeSubtypeCompatibilities.Count(c => c.TenantId == tenant.Id).Should().Be(9);

        var chart = db.ChartOfAccounts.Should().ContainSingle(c => c.TenantId == tenant.Id && c.IsDefault).Subject;
        db.StandardAccounts.Count(a => a.ChartOfAccountsId == chart.Id).Should().Be(20);
    }

    [Fact]
    public async Task SeedAsync_OnInstallStillUsingTheOldGeralSubtype_MigratesToReceitaDespesaCusto()
    {
        // Simulates an install seeded before Fase 4 introduced RECEITA/CUSTO/
        // DESPESA: a "GERAL" subtype with the 3 old DRE StandardAccounts
        // still pointing at it, and no Depreciação/Amortização yet.
        await using var db = CreateInMemoryContext();

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Default", Active = true, CreatedAt = DateTime.UtcNow };
        db.Tenants.Add(tenant);
        db.Companies.Add(new Company { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "DEFAULT", Name = "Empresa Padrão", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        var dre = new AccountType { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "DRE", Name = "Demonstração do Resultado", SequenceOrder = 3, CreatedAt = DateTime.UtcNow };
        db.AccountTypes.Add(dre);
        var geral = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = dre.Id, Code = "GERAL", Name = "Geral", SequenceOrder = 5, CreatedAt = DateTime.UtcNow };
        db.AccountSubtypes.Add(geral);
        db.TypeSubtypeCompatibilities.Add(new TypeSubtypeCompatibility { Id = Guid.NewGuid(), TenantId = tenant.Id, AccountTypeId = dre.Id, AccountSubtypeId = geral.Id, IsAllowed = true });

        var chart = new ChartOfAccounts { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Plano Padrão", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChartOfAccounts.Add(chart);
        db.StandardAccounts.AddRange(
            new StandardAccount { Id = Guid.NewGuid(), TenantId = tenant.Id, ChartOfAccountsId = chart.Id, AccountTypeId = dre.Id, AccountSubtypeId = geral.Id, Code = "DRE_RECEITA", Name = "Receitas", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new StandardAccount { Id = Guid.NewGuid(), TenantId = tenant.Id, ChartOfAccountsId = chart.Id, AccountTypeId = dre.Id, AccountSubtypeId = geral.Id, Code = "DRE_CUSTO", Name = "Custos", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new StandardAccount { Id = Guid.NewGuid(), TenantId = tenant.Id, ChartOfAccountsId = chart.Id, AccountTypeId = dre.Id, AccountSubtypeId = geral.Id, Code = "DRE_DESPESA", Name = "Despesas", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await DbSeeder.SeedAsync(db);

        (await db.AccountSubtypes.AnyAsync(s => s.TenantId == tenant.Id && s.Code == "GERAL")).Should().BeFalse();
        (await db.TypeSubtypeCompatibilities.AnyAsync(c => c.AccountSubtypeId == geral.Id)).Should().BeFalse();

        var receita = await db.AccountSubtypes.SingleAsync(s => s.TenantId == tenant.Id && s.Code == "RECEITA");
        var custo = await db.AccountSubtypes.SingleAsync(s => s.TenantId == tenant.Id && s.Code == "CUSTO");
        var despesa = await db.AccountSubtypes.SingleAsync(s => s.TenantId == tenant.Id && s.Code == "DESPESA");

        (await db.StandardAccounts.SingleAsync(a => a.Code == "DRE_RECEITA")).AccountSubtypeId.Should().Be(receita.Id);
        (await db.StandardAccounts.SingleAsync(a => a.Code == "DRE_CUSTO")).AccountSubtypeId.Should().Be(custo.Id);
        (await db.StandardAccounts.SingleAsync(a => a.Code == "DRE_DESPESA")).AccountSubtypeId.Should().Be(despesa.Id);

        // Fase 4's new Depreciação/Amortização accounts get backfilled too.
        (await db.StandardAccounts.AnyAsync(a => a.Code == "DRE_DESPESA_DEPRECIACAO" && a.AccountSubtypeId == despesa.Id)).Should().BeTrue();
        (await db.StandardAccounts.AnyAsync(a => a.Code == "DRE_DESPESA_AMORTIZACAO" && a.AccountSubtypeId == despesa.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenTaxonomyAlreadyExists_DoesNotDuplicateData()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        await DbSeeder.SeedAsync(db);
        var accountTypeCountAfterFirstSeed = await db.AccountTypes.CountAsync();
        var standardAccountCountAfterFirstSeed = await db.StandardAccounts.CountAsync();
        var chartCountAfterFirstSeed = await db.ChartOfAccounts.CountAsync();

        // Act
        await DbSeeder.SeedAsync(db);

        // Assert
        (await db.AccountTypes.CountAsync()).Should().Be(accountTypeCountAfterFirstSeed);
        (await db.StandardAccounts.CountAsync()).Should().Be(standardAccountCountAfterFirstSeed);
        (await db.ChartOfAccounts.CountAsync()).Should().Be(chartCountAfterFirstSeed);
    }
}

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
            .Should().BeEquivalentTo("CIRCULANTE", "NAO_CIRCULANTE", "PERMANENTE", "PL", "GERAL");

        db.TypeSubtypeCompatibilities.Count(c => c.TenantId == tenant.Id).Should().Be(7);

        var chart = db.ChartOfAccounts.Should().ContainSingle(c => c.TenantId == tenant.Id && c.IsDefault).Subject;
        db.StandardAccounts.Count(a => a.ChartOfAccountsId == chart.Id).Should().Be(18);
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

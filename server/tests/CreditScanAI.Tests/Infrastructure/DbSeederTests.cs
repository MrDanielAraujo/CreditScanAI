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
            .Should().BeEquivalentTo("CIRCULANTE", "NAO_CIRCULANTE", "PERMANENTE", "PL");

        db.TypeSubtypeCompatibilities.Count(c => c.TenantId == tenant.Id).Should().Be(6);
    }

    [Fact]
    public async Task SeedAsync_WhenTaxonomyAlreadyExists_DoesNotDuplicateData()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        await DbSeeder.SeedAsync(db);
        var accountTypeCountAfterFirstSeed = await db.AccountTypes.CountAsync();

        // Act
        await DbSeeder.SeedAsync(db);

        // Assert
        var accountTypeCountAfterSecondSeed = await db.AccountTypes.CountAsync();
        accountTypeCountAfterSecondSeed.Should().Be(accountTypeCountAfterFirstSeed);
    }
}

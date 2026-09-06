using CreditScanAI.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Infrastructure.Persistence;

/// <summary>
/// IdentityUserContext (not the plain DbContext this used to be) brings in
/// ASP.NET Core Identity's own user/claims/logins/tokens tables (Fase 8) -
/// just the user store, not the role store, since this app's roles are a
/// single simple enum per user (see UserRole), not Identity's own
/// many-to-many Role/UserRole tables.
/// </summary>
public class AppDbContext : IdentityUserContext<User, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<AccountSubtype> AccountSubtypes => Set<AccountSubtype>();
    public DbSet<TypeSubtypeCompatibility> TypeSubtypeCompatibilities => Set<TypeSubtypeCompatibility>();
    public DbSet<SourceAccount> SourceAccounts => Set<SourceAccount>();
    public DbSet<AccountValue> AccountValues => Set<AccountValue>();
    public DbSet<ChartOfAccounts> ChartOfAccounts => Set<ChartOfAccounts>();
    public DbSet<StandardAccount> StandardAccounts => Set<StandardAccount>();
    public DbSet<AccountClassification> AccountClassifications => Set<AccountClassification>();
    public DbSet<CalculatedFinancialValue> CalculatedFinancialValues => Set<CalculatedFinancialValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

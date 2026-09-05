using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class StandardAccountConfiguration : IEntityTypeConfiguration<StandardAccount>
{
    public void Configure(EntityTypeBuilder<StandardAccount> builder)
    {
        builder.ToTable("standard_accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.ChartOfAccountsId).HasColumnName("chart_of_accounts_id");
        builder.Property(a => a.AccountTypeId).HasColumnName("account_type_id");
        builder.Property(a => a.AccountSubtypeId).HasColumnName("account_subtype_id");
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ChartOfAccounts)
            .WithMany()
            .HasForeignKey(a => a.ChartOfAccountsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.AccountType)
            .WithMany()
            .HasForeignKey(a => a.AccountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AccountSubtype)
            .WithMany()
            .HasForeignKey(a => a.AccountSubtypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ChartOfAccountsId, a.Code }).IsUnique();
    }
}

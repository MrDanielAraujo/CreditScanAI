using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class ChartOfAccountsConfiguration : IEntityTypeConfiguration<ChartOfAccounts>
{
    public void Configure(EntityTypeBuilder<ChartOfAccounts> builder)
    {
        builder.ToTable("chart_of_accounts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();

        // Partial unique index: at most one default chart per tenant. EF Core
        // maps this via a filtered index (Npgsql provider).
        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("idx_chart_of_accounts_one_default_per_tenant")
            .IsUnique()
            .HasFilter("is_default = true");
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class CalculatedFinancialValueConfiguration : IEntityTypeConfiguration<CalculatedFinancialValue>
{
    public void Configure(EntityTypeBuilder<CalculatedFinancialValue> builder)
    {
        builder.ToTable("calculated_financial_values");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.TenantId).HasColumnName("tenant_id");
        builder.Property(v => v.CompanyId).HasColumnName("company_id");
        builder.Property(v => v.PeriodId).HasColumnName("period_id");
        builder.Property(v => v.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.Property(v => v.Value).HasColumnName("value").HasColumnType("decimal(18,4)");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(v => v.Tenant)
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Company)
            .WithMany()
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Period)
            .WithMany()
            .HasForeignKey(v => v.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.CompanyId, v.PeriodId, v.Key })
            .IsUnique()
            .HasDatabaseName("idx_calculated_financial_values_company_period_key");
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class AccountValueConfiguration : IEntityTypeConfiguration<AccountValue>
{
    public void Configure(EntityTypeBuilder<AccountValue> builder)
    {
        builder.ToTable("account_values");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.TenantId).HasColumnName("tenant_id");
        builder.Property(v => v.SourceAccountId).HasColumnName("source_account_id");
        builder.Property(v => v.DocumentId).HasColumnName("document_id");
        builder.Property(v => v.PeriodId).HasColumnName("period_id");
        builder.Property(v => v.RawColumnLabel).HasColumnName("raw_column_label").HasMaxLength(255);
        builder.Property(v => v.RawValue).HasColumnName("raw_value").HasColumnType("decimal(19,4)");
        builder.Property(v => v.ScaleFactor).HasColumnName("scale_factor").HasDefaultValue(1);
        builder.Property(v => v.ExtractionConfidence).HasColumnName("extraction_confidence").HasColumnType("decimal(3,2)");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");

        builder.HasOne(v => v.Tenant)
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.SourceAccount)
            .WithMany()
            .HasForeignKey(v => v.SourceAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Document)
            .WithMany()
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Period)
            .WithMany()
            .HasForeignKey(v => v.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.PeriodId, v.SourceAccountId }).HasDatabaseName("idx_account_values_period_source");
    }
}

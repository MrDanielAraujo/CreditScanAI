using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> builder)
    {
        builder.ToTable("periods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.PeriodType).HasColumnName("period_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Year).HasColumnName("year");
        builder.Property(p => p.Quarter).HasColumnName("quarter");
        builder.Property(p => p.StartDate).HasColumnName("start_date");
        builder.Property(p => p.EndDate).HasColumnName("end_date");
        builder.Property(p => p.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.PeriodType, p.Year, p.Quarter }).IsUnique();
    }
}

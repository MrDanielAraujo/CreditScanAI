using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.LegalName).HasColumnName("legal_name").HasMaxLength(255);
        builder.Property(c => c.Cnpj).HasColumnName("cnpj").HasMaxLength(20);
        builder.Property(c => c.Industry).HasColumnName("industry").HasMaxLength(100);
        builder.Property(c => c.FiscalYearEnd).HasColumnName("fiscal_year_end");
        builder.Property(c => c.ReportingCurrency).HasColumnName("reporting_currency").HasMaxLength(3).HasDefaultValue("BRL");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
    }
}

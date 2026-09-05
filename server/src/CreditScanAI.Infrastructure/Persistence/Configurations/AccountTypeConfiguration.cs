using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class AccountTypeConfiguration : IEntityTypeConfiguration<AccountType>
{
    public void Configure(EntityTypeBuilder<AccountType> builder)
    {
        builder.ToTable("account_types");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.SequenceOrder).HasColumnName("sequence_order");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
    }
}

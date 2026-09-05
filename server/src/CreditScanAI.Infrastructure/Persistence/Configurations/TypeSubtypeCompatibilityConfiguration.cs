using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class TypeSubtypeCompatibilityConfiguration : IEntityTypeConfiguration<TypeSubtypeCompatibility>
{
    public void Configure(EntityTypeBuilder<TypeSubtypeCompatibility> builder)
    {
        builder.ToTable("type_subtype_compatibility");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.AccountTypeId).HasColumnName("account_type_id");
        builder.Property(c => c.AccountSubtypeId).HasColumnName("account_subtype_id");
        builder.Property(c => c.IsAllowed).HasColumnName("is_allowed").HasDefaultValue(true);
        builder.Property(c => c.SequenceOrder).HasColumnName("sequence_order");

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AccountType)
            .WithMany()
            .HasForeignKey(c => c.AccountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AccountSubtype)
            .WithMany()
            .HasForeignKey(c => c.AccountSubtypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.AccountTypeId, c.AccountSubtypeId }).IsUnique();
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class AccountSubtypeConfiguration : IEntityTypeConfiguration<AccountSubtype>
{
    public void Configure(EntityTypeBuilder<AccountSubtype> builder)
    {
        builder.ToTable("account_subtypes");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.AccountTypeId).HasColumnName("account_type_id");
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.SequenceOrder).HasColumnName("sequence_order");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AccountType)
            .WithMany(t => t.Subtypes)
            .HasForeignKey(a => a.AccountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// IdentityUserContext.OnModelCreating already configures the base
/// IdentityUser columns/keys/indexes (Email, PasswordHash, UserName
/// uniqueness, etc.) - this only adds this app's own extra columns
/// (TenantId/Name/Role/CreatedAt) and renames the table away from Identity's
/// default "AspNetUsers".
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        builder.HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class LoginAuditEntryConfiguration : IEntityTypeConfiguration<LoginAuditEntry>
{
    public void Configure(EntityTypeBuilder<LoginAuditEntry> builder)
    {
        builder.ToTable("login_audit_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Success).HasColumnName("success");
        builder.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(255);
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        builder.Property(e => e.AttemptedAt).HasColumnName("attempted_at");

        builder.HasIndex(e => e.AttemptedAt);
    }
}

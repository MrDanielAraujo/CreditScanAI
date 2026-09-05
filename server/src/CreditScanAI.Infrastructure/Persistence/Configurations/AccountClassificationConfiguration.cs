using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class AccountClassificationConfiguration : IEntityTypeConfiguration<AccountClassification>
{
    public void Configure(EntityTypeBuilder<AccountClassification> builder)
    {
        builder.ToTable("account_classifications");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.SourceAccountId).HasColumnName("source_account_id");
        builder.Property(c => c.StandardAccountId).HasColumnName("standard_account_id");
        builder.Property(c => c.ChartOfAccountsId).HasColumnName("chart_of_accounts_id");
        builder.Property(c => c.ConfidenceScore).HasColumnName("confidence_score").HasColumnType("decimal(4,3)");
        builder.Property(c => c.ClassificationMethod).HasColumnName("classification_method").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Evidence).HasColumnName("evidence");
        builder.Property(c => c.ReviewStatus).HasColumnName("review_status").HasConversion<string>().HasMaxLength(50)
            .HasDefaultValue(ClassificationReviewStatus.Pending);
        builder.Property(c => c.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(c => c.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(c => c.ReviewNotes).HasColumnName("review_notes");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.SourceAccount)
            .WithMany()
            .HasForeignKey(c => c.SourceAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.StandardAccount)
            .WithMany()
            .HasForeignKey(c => c.StandardAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ChartOfAccounts)
            .WithMany()
            .HasForeignKey(c => c.ChartOfAccountsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.SourceAccountId).IsUnique();
        builder.HasIndex(c => c.ReviewStatus).HasDatabaseName("idx_account_classifications_review_status");
    }
}

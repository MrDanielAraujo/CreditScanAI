using CreditScanAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class SourceAccountConfiguration : IEntityTypeConfiguration<SourceAccount>
{
    public void Configure(EntityTypeBuilder<SourceAccount> builder)
    {
        builder.ToTable("source_accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.DocumentId).HasColumnName("document_id");
        builder.Property(a => a.OriginalName).HasColumnName("original_name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.NormalizedName).HasColumnName("normalized_name").HasMaxLength(255);
        builder.Property(a => a.HierarchyLevel).HasColumnName("hierarchy_level");
        builder.Property(a => a.ParentSourceAccountId).HasColumnName("parent_source_account_id");
        builder.Property(a => a.InferredType).HasColumnName("inferred_type").HasMaxLength(50);
        builder.Property(a => a.InferredSubtype).HasColumnName("inferred_subtype").HasMaxLength(50);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Document)
            .WithMany()
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ParentSourceAccount)
            .WithMany()
            .HasForeignKey(a => a.ParentSourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.DocumentId).HasDatabaseName("idx_source_accounts_document");
    }
}

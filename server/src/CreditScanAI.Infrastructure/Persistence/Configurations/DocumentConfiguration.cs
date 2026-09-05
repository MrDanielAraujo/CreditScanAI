using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditScanAI.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.CompanyId).HasColumnName("company_id");
        builder.Property(d => d.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.UploadDate).HasColumnName("upload_date");
        builder.Property(d => d.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(d => d.FilePath).HasColumnName("file_path").IsRequired();
        builder.Property(d => d.FileSize).HasColumnName("file_size");
        builder.Property(d => d.FileHash).HasColumnName("file_hash").HasMaxLength(255);
        builder.Property(d => d.ExtractionStatus).HasColumnName("extraction_status").HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.ExtractionStartedAt).HasColumnName("extraction_started_at");
        builder.Property(d => d.ExtractionCompletedAt).HasColumnName("extraction_completed_at");
        builder.Property(d => d.ExtractionError).HasColumnName("extraction_error");
        builder.Property(d => d.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.ChartOfAccountsId).HasColumnName("chart_of_accounts_id");
        builder.Property(d => d.ClassificationStatus).HasColumnName("classification_status").HasConversion<string>().HasMaxLength(50)
            .HasDefaultValue(ClassificationStatus.NotStarted);
        builder.Property(d => d.ClassificationStartedAt).HasColumnName("classification_started_at");
        builder.Property(d => d.ClassificationCompletedAt).HasColumnName("classification_completed_at");
        builder.Property(d => d.ClassificationError).HasColumnName("classification_error");

        builder.HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Company)
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ChartOfAccounts)
            .WithMany()
            .HasForeignKey(d => d.ChartOfAccountsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.TenantId, d.CompanyId }).HasDatabaseName("idx_documents_tenant_company");
        builder.HasIndex(d => d.ExtractionStatus).HasDatabaseName("idx_documents_status");
        builder.HasIndex(d => d.DocumentType).HasDatabaseName("idx_documents_type");
        builder.HasIndex(d => d.ClassificationStatus).HasDatabaseName("idx_documents_classification_status");
    }
}

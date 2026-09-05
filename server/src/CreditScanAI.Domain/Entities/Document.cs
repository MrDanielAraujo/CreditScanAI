using CreditScanAI.Domain.Enums;

namespace CreditScanAI.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? FileHash { get; set; }
    public ExtractionStatus ExtractionStatus { get; set; } = ExtractionStatus.Pending;
    public DateTime? ExtractionStartedAt { get; set; }
    public DateTime? ExtractionCompletedAt { get; set; }
    public string? ExtractionError { get; set; }
    public string? Metadata { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Company? Company { get; set; }
}

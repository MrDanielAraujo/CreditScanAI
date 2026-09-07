using CreditScanAI.Domain.Enums;

namespace CreditScanAI.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>
    /// Balanço/DRE/Misto - detectado a partir da classificação por palavra-chave
    /// das contas extraídas (Fase 2), não mais informado no upload. Null até a
    /// extração terminar (ou se nada foi classificado com confiança suficiente).
    /// </summary>
    public DocumentType? DocumentType { get; set; }
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

    /// <summary>
    /// Which ChartOfAccounts was used to classify this document (Fase 3) -
    /// set once classification runs, for traceability. Null until then.
    /// </summary>
    public Guid? ChartOfAccountsId { get; set; }

    public ClassificationStatus ClassificationStatus { get; set; } = ClassificationStatus.NotStarted;
    public DateTime? ClassificationStartedAt { get; set; }
    public DateTime? ClassificationCompletedAt { get; set; }
    public string? ClassificationError { get; set; }

    public Tenant? Tenant { get; set; }
    public Company? Company { get; set; }
    public ChartOfAccounts? ChartOfAccounts { get; set; }
}

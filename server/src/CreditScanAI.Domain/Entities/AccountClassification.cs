using CreditScanAI.Domain.Enums;

namespace CreditScanAI.Domain.Entities;

/// <summary>
/// One SourceAccount's suggested (or confirmed) mapping to a StandardAccount
/// within the ChartOfAccounts used for the document. StandardAccountId is
/// null when no rule/AI layer could suggest anything with confidence.
/// </summary>
public class AccountClassification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid? StandardAccountId { get; set; }
    public Guid ChartOfAccountsId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ClassificationMethod { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public ClassificationReviewStatus ReviewStatus { get; set; } = ClassificationReviewStatus.Pending;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public SourceAccount? SourceAccount { get; set; }
    public StandardAccount? StandardAccount { get; set; }
    public ChartOfAccounts? ChartOfAccounts { get; set; }
}

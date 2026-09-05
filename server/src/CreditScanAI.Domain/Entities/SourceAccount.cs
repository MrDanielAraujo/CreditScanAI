namespace CreditScanAI.Domain.Entities;

/// <summary>
/// An account as it literally appears in the source document, before
/// classification against the standard chart of accounts (Fase 3).
/// </summary>
public class SourceAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string? NormalizedName { get; set; }
    public int HierarchyLevel { get; set; }
    public Guid? ParentSourceAccountId { get; set; }
    public string? InferredType { get; set; }
    public string? InferredSubtype { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Document? Document { get; set; }
    public SourceAccount? ParentSourceAccount { get; set; }
}

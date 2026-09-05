namespace CreditScanAI.Domain.Entities;

/// <summary>
/// A named set of StandardAccounts a tenant can classify documents against.
/// Exactly one per tenant should have IsDefault = true; that's the plan used
/// automatically when classifying (Fase 3) - if none is default, classification
/// must not proceed silently.
/// </summary>
public class ChartOfAccounts
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}

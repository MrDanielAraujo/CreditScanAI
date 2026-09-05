namespace CreditScanAI.Domain.Entities;

public class TypeSubtypeCompatibility
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AccountTypeId { get; set; }
    public Guid AccountSubtypeId { get; set; }
    public bool IsAllowed { get; set; } = true;
    public int? SequenceOrder { get; set; }

    public Tenant? Tenant { get; set; }
    public AccountType? AccountType { get; set; }
    public AccountSubtype? AccountSubtype { get; set; }
}

namespace CreditScanAI.Domain.Entities;

public class AccountSubtype
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AccountTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SequenceOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public AccountType? AccountType { get; set; }
}

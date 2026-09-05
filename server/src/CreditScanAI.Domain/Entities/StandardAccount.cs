namespace CreditScanAI.Domain.Entities;

/// <summary>
/// One account of a ChartOfAccounts - the classification target for
/// SourceAccounts (Fase 3). (AccountTypeId, AccountSubtypeId) must be a pair
/// allowed by TypeSubtypeCompatibility.
/// </summary>
public class StandardAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChartOfAccountsId { get; set; }
    public Guid AccountTypeId { get; set; }
    public Guid AccountSubtypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public ChartOfAccounts? ChartOfAccounts { get; set; }
    public AccountType? AccountType { get; set; }
    public AccountSubtype? AccountSubtype { get; set; }
}

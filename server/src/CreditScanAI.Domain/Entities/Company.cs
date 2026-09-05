namespace CreditScanAI.Domain.Entities;

public class Company
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Cnpj { get; set; }
    public string? Industry { get; set; }
    public DateTime? FiscalYearEnd { get; set; }
    public string ReportingCurrency { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}

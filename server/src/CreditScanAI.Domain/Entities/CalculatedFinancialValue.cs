namespace CreditScanAI.Domain.Entities;

/// <summary>
/// One named calculated value (a chart-of-accounts total or a financial
/// indicator - see CalculationKeys) for a company/period, produced by the
/// Fase 4 calculation engine. Recalculating overwrites the existing row for
/// the same (CompanyId, PeriodId, Key).
/// </summary>
public class CalculatedFinancialValue
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public string Key { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Company? Company { get; set; }
    public Period? Period { get; set; }
}

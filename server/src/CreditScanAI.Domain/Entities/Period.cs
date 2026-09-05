using CreditScanAI.Domain.Enums;

namespace CreditScanAI.Domain.Entities;

public class Period
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public PeriodType PeriodType { get; set; }
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}

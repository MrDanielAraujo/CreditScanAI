namespace CreditScanAI.Domain.Entities;

/// <summary>
/// One value cell extracted for a <see cref="SourceAccount"/>. Exactly one of
/// <see cref="PeriodId"/> or <see cref="RawColumnLabel"/> is set: a column
/// that parsed as a date maps to a real Period, anything else (e.g. a
/// fund/project column) is kept as raw label metadata - see the Fase 2
/// entity-modeling decision (Company stays a legal entity, chosen manually
/// at upload time, not inferred from document columns).
/// </summary>
public class AccountValue
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? PeriodId { get; set; }
    public string? RawColumnLabel { get; set; }
    public decimal? RawValue { get; set; }
    public int ScaleFactor { get; set; } = 1;
    public decimal? ExtractionConfidence { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public SourceAccount? SourceAccount { get; set; }
    public Document? Document { get; set; }
    public Period? Period { get; set; }
}

namespace CreditScanAI.Api.Contracts.Consolidation;

public sealed record ConsolidateRequest(Guid PeriodId, List<Guid> CompanyIds);

public sealed record ReconciliationCheckDto(
    string CheckId,
    string Description,
    bool Passed,
    decimal ExpectedValue,
    decimal ActualValue,
    decimal Variance,
    string? ErrorMessage);

public sealed record ConsolidationResultResponse(
    Guid PeriodId,
    List<Guid> CompanyIds,
    Dictionary<string, decimal> Values,
    bool EquationBalanced,
    decimal EquationVariance,
    List<ReconciliationCheckDto> ReconciliationChecks,
    bool IsValid);

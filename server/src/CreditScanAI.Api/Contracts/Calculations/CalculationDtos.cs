namespace CreditScanAI.Api.Contracts.Calculations;

public sealed record CalculationResultResponse(
    Guid CompanyId,
    Guid PeriodId,
    Dictionary<string, decimal> Values,
    bool EquationBalanced,
    decimal EquationVariance,
    DateTime CalculatedAt);

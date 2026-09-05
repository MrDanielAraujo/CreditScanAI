namespace CreditScanAI.Api.Contracts.Documents;

public sealed record CompanyDto(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? Cnpj,
    string? Industry,
    DateTime? FiscalYearEnd,
    string ReportingCurrency);

public sealed record UpsertCompanyRequest(
    string Code,
    string Name,
    string? LegalName,
    string? Cnpj,
    string? Industry,
    DateTime? FiscalYearEnd,
    string? ReportingCurrency);

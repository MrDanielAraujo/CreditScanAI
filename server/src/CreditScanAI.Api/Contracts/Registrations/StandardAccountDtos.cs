namespace CreditScanAI.Api.Contracts.Registrations;

public sealed record StandardAccountDto(
    Guid Id,
    Guid ChartOfAccountsId,
    Guid AccountTypeId,
    Guid AccountSubtypeId,
    string Code,
    string Name,
    string? Description);

public sealed record UpsertStandardAccountRequest(
    Guid ChartOfAccountsId,
    Guid AccountTypeId,
    Guid AccountSubtypeId,
    string Code,
    string Name,
    string? Description);

namespace CreditScanAI.Api.Contracts.Registrations;

public sealed record ChartOfAccountsDto(Guid Id, string Name, string? Description, bool IsDefault);

public sealed record UpsertChartOfAccountsRequest(string Name, string? Description);

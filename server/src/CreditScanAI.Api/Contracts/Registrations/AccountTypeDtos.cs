namespace CreditScanAI.Api.Contracts.Registrations;

public sealed record AccountTypeDto(Guid Id, string Code, string Name, string? Description, int? SequenceOrder);

public sealed record UpsertAccountTypeRequest(string Code, string Name, string? Description, int? SequenceOrder);

namespace CreditScanAI.Api.Contracts.Registrations;

public sealed record AccountSubtypeDto(Guid Id, Guid AccountTypeId, string Code, string Name, string? Description, int? SequenceOrder);

public sealed record UpsertAccountSubtypeRequest(Guid AccountTypeId, string Code, string Name, string? Description, int? SequenceOrder);

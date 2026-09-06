namespace CreditScanAI.Api.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, string? Name);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserSummaryDto(Guid Id, string Email, string? Name, string Role);

public sealed record AuthResponse(string Token, UserSummaryDto User);

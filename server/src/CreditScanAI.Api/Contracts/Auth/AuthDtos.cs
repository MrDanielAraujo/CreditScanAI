namespace CreditScanAI.Api.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, string? Name);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserSummaryDto(Guid Id, string Email, string? Name, string Role);

public sealed record AuthResponse(string Token, UserSummaryDto User);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record LoginAuditEntryDto(Guid Id, string Email, bool Success, string? FailureReason, string? IpAddress, DateTime AttemptedAt);

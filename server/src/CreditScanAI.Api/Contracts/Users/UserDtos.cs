namespace CreditScanAI.Api.Contracts.Users;

public sealed record UserListItemDto(Guid Id, string Email, string? Name, string Role, bool IsLockedOut);

public sealed record CreateUserRequest(string Email, string Password, string? Name, string Role);

public sealed record UpdateUserRoleRequest(string Role);

public sealed record ResetUserPasswordRequest(string NewPassword);

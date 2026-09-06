namespace CreditScanAI.Domain.Entities;

/// <summary>
/// One row per login attempt (Fase 8 Parte 3, UC-01's "auditoria: log de
/// todos os logins"), success or failure. Email is stored as typed, not
/// resolved to a UserId, so a login attempt against a non-existent email is
/// still auditable - exactly the kind of attempt UC-10 (Compliance auditing
/// the system) cares about.
/// </summary>
public class LoginAuditEntry
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime AttemptedAt { get; set; }
}

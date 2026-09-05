using CreditScanAI.Domain.Enums;

namespace CreditScanAI.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}

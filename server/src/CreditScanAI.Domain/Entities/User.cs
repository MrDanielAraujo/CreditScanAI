using CreditScanAI.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CreditScanAI.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's own IdentityUser (Email, PasswordHash,
/// LockoutEnd/LockoutEnabled/AccessFailedCount, etc. all come for free) with
/// this system's own concepts: which Tenant the user belongs to, and their
/// UserRole (a single simple role per user - not Identity's own many-to-many
/// Role store, which this app has no use for).
/// </summary>
public class User : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string? Name { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}

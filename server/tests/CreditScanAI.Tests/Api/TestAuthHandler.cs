using System.Security.Claims;
using System.Text.Encodings.Web;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditScanAI.Tests.Api;

/// <summary>
/// Replaces real JWT validation in integration tests: every request is
/// "authenticated" as an Admin (every policy passes, matching the old
/// [AllowAnonymous]-everywhere behavior most tests were written against) in
/// whichever tenant is oldest in the DB - unless a test explicitly overrides
/// the role or tenant via headers, to exercise Fase 8 Parte 2's
/// authorization matrix itself (see AuthorizationPolicyTests).
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";
    public const string TenantHeader = "X-Test-Tenant-Id";
    public const string UserIdHeader = "X-Test-User-Id";

    private readonly AppDbContext _db;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers[RoleHeader].FirstOrDefault() ?? nameof(UserRole.Admin);

        var tenantIdHeader = Request.Headers[TenantHeader].FirstOrDefault();
        Guid tenantId;
        if (tenantIdHeader is not null && Guid.TryParse(tenantIdHeader, out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }
        else
        {
            tenantId = await _db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync();
        }

        var userId = Request.Headers[UserIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtClaimNames.TenantId, tenantId.ToString()),
            new(ClaimTypes.Role, role)
        ];

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}

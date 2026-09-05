using CreditScanAI.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CreditScanAI.Tests.Api;

/// <summary>
/// Runs the API host under a dedicated "Testing" environment so startup does not
/// try to reach a real PostgreSQL instance (the dev-only seeding path only runs
/// under "Development").
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

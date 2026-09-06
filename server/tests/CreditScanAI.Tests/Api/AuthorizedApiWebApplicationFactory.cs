using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

/// <summary>
/// Same as DocumentsApiWebApplicationFactory (InMemory DB, fake file store),
/// but also swaps the default authentication scheme for TestAuthHandler -
/// for every controller test EXCEPT AuthControllerTests, which needs the
/// real JWT bearer validation to actually test login/register/lockout.
/// </summary>
public class AuthorizedApiWebApplicationFactory : DocumentsApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

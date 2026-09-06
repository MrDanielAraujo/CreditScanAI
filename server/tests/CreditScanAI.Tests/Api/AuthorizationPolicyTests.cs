using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Documents;
using CreditScanAI.Domain.Enums;
using FluentAssertions;

namespace CreditScanAI.Tests.Api;

/// <summary>
/// Verifies the actual permission matrix from 10_CASOS_DE_USO.md section 5
/// (Fase 8 Parte 2) - not just that endpoints still work for Admin (every
/// other test class in this project runs as Admin by default via
/// TestAuthHandler), but that the OTHER roles are actually blocked where the
/// matrix says they should be, and allowed where it says they should be.
/// "No token at all" (401, not 403) is covered separately by
/// AuthControllerTests.Me_WithoutToken_ReturnsUnauthorized, which runs
/// against the real JWT bearer scheme rather than this factory's
/// TestAuthHandler (which always authenticates, by design - see its own
/// doc comment).
/// </summary>
[Collection(ApiHostTestCollection.Name)]
public class AuthorizationPolicyTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;

    public AuthorizationPolicyTests(AuthorizedApiWebApplicationFactory factory) => _factory = factory;

    private HttpClient ClientAs(UserRole role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        return client;
    }

    [Theory]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Compliance)]
    public async Task Upload_NonUploadRole_ReturnsForbidden(UserRole role)
    {
        var client = ClientAs(role);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Guid.NewGuid().ToString()), "companyId" },
            { new StringContent("BalanceSheet"), "documentType" }
        };
        form.Add(new ByteArrayContent([1, 2, 3]), "file", "doc.pdf");

        var response = await client.PostAsync("/api/documents/upload", form);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRole.Analyst)]
    [InlineData(UserRole.Admin)]
    public async Task Upload_UploadRole_IsNotForbidden(UserRole role)
    {
        var client = ClientAs(role);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Guid.NewGuid().ToString()), "companyId" },
            { new StringContent("BalanceSheet"), "documentType" }
        };
        form.Add(new ByteArrayContent([1, 2, 3]), "file", "doc.pdf");

        var response = await client.PostAsync("/api/documents/upload", form);

        // Chega a validar o request (ex: company_id inexistente => 400) em
        // vez de barrar por papel (403) - o que importa aqui é não ser 403.
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Compliance)]
    public async Task Review_NonReviewRole_ReturnsForbidden(UserRole role)
    {
        var client = ClientAs(role);

        var response = await client.GetAsync("/api/classifications/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRole.Analyst)]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.Admin)]
    public async Task Review_ReviewRole_IsAllowed(UserRole role)
    {
        var client = ClientAs(role);

        var response = await client.GetAsync("/api/classifications/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Compliance)]
    public async Task Consolidate_NonConsolidateRole_ReturnsForbidden(UserRole role)
    {
        var client = ClientAs(role);

        var response = await client.PostAsJsonAsync(
            $"/api/calculations/companies/{Guid.NewGuid()}/periods/{Guid.NewGuid()}/calculate", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(UserRole.Analyst)]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Compliance)]
    public async Task Admin_NonAdminRole_CannotCreateCompany_ReturnsForbidden(UserRole role)
    {
        var client = ClientAs(role);

        var response = await client.PostAsJsonAsync(
            "/api/companies", new UpsertCompanyRequest("COD", "Nome", null, null, null, null, "BRL"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_AdminRole_CanCreateCompany()
    {
        var client = ClientAs(UserRole.Admin);

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new UpsertCompanyRequest($"COD_{Guid.NewGuid():N}"[..10], "Empresa Admin Test", null, null, null, null, "BRL"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(UserRole.Analyst)]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Compliance)]
    public async Task Export_AnyAuthenticatedRole_CanListCompanies(UserRole role)
    {
        var client = ClientAs(role);

        var response = await client.GetAsync("/api/companies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

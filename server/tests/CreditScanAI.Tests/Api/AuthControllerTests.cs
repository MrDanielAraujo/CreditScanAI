using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Auth;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class AuthControllerTests : IClassFixture<DocumentsApiWebApplicationFactory>
{
    private readonly DocumentsApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(DocumentsApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Tenants.Any())
        {
            db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Auth Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Register_ValidRequest_CreatesUserAndReturnsToken()
    {
        await SeedTenantAsync();
        var email = $"user_{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Fulano de Tal"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body!.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data!.User.Email.Should().Be(email);
        body.Data!.User.Role.Should().Be("Analyst");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        await SeedTenantAsync();
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Original"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "OutraSenha123", "Duplicado"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await SeedTenantAsync();
        var email = $"login_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Login Test"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaForte123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body!.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await SeedTenantAsync();
        var email = $"wrongpw_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Wrong Password Test"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaErrada999"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ninguem@example.com", "QualquerSenha123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccountEvenWithCorrectPassword()
    {
        await SeedTenantAsync();
        var email = $"lockout_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Lockout Test"));

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaErrada999"));
        }

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaForte123"));

        response.StatusCode.Should().Be((HttpStatusCode)423);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        await SeedTenantAsync();
        var email = $"me_{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SenhaForte123", "Me Test"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registerBody!.Data!.Token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserSummaryDto>>();
        body!.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

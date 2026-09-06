using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Users;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class UsersControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly AuthorizedApiWebApplicationFactory _factory;

    public UsersControllerTests(AuthorizedApiWebApplicationFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, nameof(UserRole.Admin));
        return client;
    }

    private HttpClient NonAdminClient(UserRole role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        return client;
    }

    private async Task<Guid> CreateUserDirectlyAsync(string email, string password = "SenhaForte123")
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync();
        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserName = email, Email = email,
            Name = "Seed User", Role = UserRole.Analyst, CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(user, password);
        return user.Id;
    }

    [Fact]
    public async Task List_ReturnsUsersInTheSameTenant()
    {
        var email = $"list_{Guid.NewGuid():N}@example.com";
        await CreateUserDirectlyAsync(email);

        var response = await AdminClient().GetFromJsonAsync<ApiResponse<List<UserListItemDto>>>("/api/users");

        response!.Data!.Should().Contain(u => u.Email == email);
    }

    [Theory]
    [InlineData(UserRole.Analyst)]
    [InlineData(UserRole.Reviewer)]
    [InlineData(UserRole.CFO)]
    [InlineData(UserRole.Compliance)]
    public async Task List_NonAdminRole_ReturnsForbidden(UserRole role)
    {
        var response = await NonAdminClient(role).GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ValidRequest_CreatesUserWithChosenRole()
    {
        var email = $"create_{Guid.NewGuid():N}@example.com";

        var response = await AdminClient().PostAsJsonAsync(
            "/api/users", new CreateUserRequest(email, "SenhaForte123", "Novo Reviewer", nameof(UserRole.Reviewer)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserListItemDto>>();
        body!.Data!.Role.Should().Be(nameof(UserRole.Reviewer));
    }

    [Fact]
    public async Task Create_InvalidRole_ReturnsBadRequest()
    {
        var email = $"badrole_{Guid.NewGuid():N}@example.com";

        var response = await AdminClient().PostAsJsonAsync(
            "/api/users", new CreateUserRequest(email, "SenhaForte123", null, "PapelInexistente"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRole_ChangesTargetUsersRole()
    {
        var email = $"rolechange_{Guid.NewGuid():N}@example.com";
        var userId = await CreateUserDirectlyAsync(email);

        var response = await AdminClient().PutAsJsonAsync($"/api/users/{userId}/role", new UpdateUserRoleRequest(nameof(UserRole.CFO)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserListItemDto>>();
        body!.Data!.Role.Should().Be(nameof(UserRole.CFO));
    }

    [Fact]
    public async Task UpdateRole_OwnAccount_ReturnsConflict()
    {
        var ownId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, nameof(UserRole.Admin));
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, ownId.ToString());

        var response = await client.PutAsJsonAsync($"/api/users/{ownId}/role", new UpdateUserRoleRequest(nameof(UserRole.Analyst)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResetPassword_AllowsLoginWithNewPassword()
    {
        var email = $"adminreset_{Guid.NewGuid():N}@example.com";
        var userId = await CreateUserDirectlyAsync(email, "SenhaAntiga123");

        var resetResponse = await AdminClient().PostAsJsonAsync($"/api/users/{userId}/reset-password", new ResetUserPasswordRequest("SenhaNovaImposta456"));
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new CreditScanAI.Api.Contracts.Auth.LoginRequest(email, "SenhaNovaImposta456"));

        // A fábrica usada aqui substitui a autenticação real, mas login
        // ainda passa pelo UserManager real - confirma que a senha foi
        // realmente trocada no banco.
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lock_OwnAccount_ReturnsConflict()
    {
        var ownId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, nameof(UserRole.Admin));
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, ownId.ToString());

        var response = await client.PostAsync($"/api/users/{ownId}/lock", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Lock_ThenLogin_IsRejectedAsLockedOut()
    {
        var email = $"lockflow_{Guid.NewGuid():N}@example.com";
        var userId = await CreateUserDirectlyAsync(email);

        var lockResponse = await AdminClient().PostAsync($"/api/users/{userId}/lock", null);
        lockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new CreditScanAI.Api.Contracts.Auth.LoginRequest(email, "SenhaForte123"));

        loginResponse.StatusCode.Should().Be((HttpStatusCode)423);
    }

    [Fact]
    public async Task Lock_ThenUnlock_AllowsLoginAgain()
    {
        var email = $"unlockflow_{Guid.NewGuid():N}@example.com";
        var userId = await CreateUserDirectlyAsync(email);

        await AdminClient().PostAsync($"/api/users/{userId}/lock", null);
        var unlockResponse = await AdminClient().PostAsync($"/api/users/{userId}/unlock", null);
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new CreditScanAI.Api.Contracts.Auth.LoginRequest(email, "SenhaForte123"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

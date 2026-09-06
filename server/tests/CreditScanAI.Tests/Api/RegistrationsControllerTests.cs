using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Registrations;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Tests.Api;

[Collection(ApiHostTestCollection.Name)]
public class RegistrationsControllerTests : IClassFixture<AuthorizedApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegistrationsControllerTests(AuthorizedApiWebApplicationFactory factory)
    {
        // Reuses the InMemory-DB factory from the documents tests; the file
        // storage override it also sets up is simply unused here.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Registrations Test Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AccountType_CreateWithDuplicateCode_ReturnsConflict()
    {
        var request = new UpsertAccountTypeRequest("DUPTYPE", "Duplicado", null, null);

        var first = await _client.PostAsJsonAsync("/api/account-types", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/account-types", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AccountType_DeleteWhileInUseBySubtype_ReturnsConflict()
    {
        var type = await CreateAccountTypeAsync("INUSE_TYPE");
        await CreateAccountSubtypeAsync(type.Id, "INUSE_SUB");

        var response = await _client.DeleteAsync($"/api/account-types/{type.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AccountSubtype_Create_AutomaticallyGrantsCompatibilityWithItsType()
    {
        var type = await CreateAccountTypeAsync("AUTOCOMPAT_TYPE");
        var subtype = await CreateAccountSubtypeAsync(type.Id, "AUTOCOMPAT_SUB");

        var chart = await CreateChartOfAccountsAsync("Plano AutoCompat");

        // If the compatibility wasn't auto-created, this would fail with 400.
        var response = await _client.PostAsJsonAsync("/api/standard-accounts", new UpsertStandardAccountRequest(
            chart.Id, type.Id, subtype.Id, "AUTOCOMPAT_ACC", "Conta AutoCompat", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ChartOfAccounts_FirstOneCreated_BecomesDefaultAutomatically()
    {
        // A fresh tenant-per-test-class factory instance means the very
        // first chart created here has no pre-existing default to compete with.
        using var isolatedFactory = new AuthorizedApiWebApplicationFactory();
        using var scope = isolatedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Isolated Tenant", Active = true, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var client = isolatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chart-of-accounts", new UpsertChartOfAccountsRequest("Primeiro Plano", null));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ChartOfAccountsDto>>();

        body!.Data!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task ChartOfAccounts_SetDefault_UnsetsThePreviousDefault()
    {
        var chartA = await CreateChartOfAccountsAsync("Plano SetDefault A");
        var chartB = await CreateChartOfAccountsAsync("Plano SetDefault B");

        await _client.PostAsync($"/api/chart-of-accounts/{chartB.Id}/set-default", null);

        var listResponse = await _client.GetFromJsonAsync<ApiResponse<List<ChartOfAccountsDto>>>("/api/chart-of-accounts");
        var refreshedA = listResponse!.Data!.Single(c => c.Id == chartA.Id);
        var refreshedB = listResponse.Data!.Single(c => c.Id == chartB.Id);

        refreshedA.IsDefault.Should().BeFalse();
        refreshedB.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task ChartOfAccounts_DeleteWhileDefault_ReturnsConflict()
    {
        var chart = await CreateChartOfAccountsAsync("Plano ProtegidoDelete");
        await _client.PostAsync($"/api/chart-of-accounts/{chart.Id}/set-default", null);

        var response = await _client.DeleteAsync($"/api/chart-of-accounts/{chart.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task StandardAccount_IncompatibleTypeAndSubtype_ReturnsBadRequest()
    {
        var typeA = await CreateAccountTypeAsync("INCOMPAT_TYPE_A");
        var typeB = await CreateAccountTypeAsync("INCOMPAT_TYPE_B");
        var subtypeOfB = await CreateAccountSubtypeAsync(typeB.Id, "INCOMPAT_SUB_B");
        var chart = await CreateChartOfAccountsAsync("Plano Incompat");

        // subtypeOfB is only compatible with typeB, not typeA.
        var response = await _client.PostAsJsonAsync("/api/standard-accounts", new UpsertStandardAccountRequest(
            chart.Id, typeA.Id, subtypeOfB.Id, "INCOMPAT_ACC", "Conta Incompatível", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StandardAccount_DuplicateCodeWithinSameChart_ReturnsConflict()
    {
        var type = await CreateAccountTypeAsync("DUPCODE_TYPE");
        var subtype = await CreateAccountSubtypeAsync(type.Id, "DUPCODE_SUB");
        var chart = await CreateChartOfAccountsAsync("Plano DupCode");

        var request = new UpsertStandardAccountRequest(chart.Id, type.Id, subtype.Id, "DUP_ACC_CODE", "Conta 1", null);
        var first = await _client.PostAsJsonAsync("/api/standard-accounts", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/standard-accounts",
            request with { Name = "Conta 2" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<AccountTypeDto> CreateAccountTypeAsync(string code)
    {
        var response = await _client.PostAsJsonAsync("/api/account-types", new UpsertAccountTypeRequest(code, $"Tipo {code}", null, null));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AccountTypeDto>>();
        return body!.Data!;
    }

    private async Task<AccountSubtypeDto> CreateAccountSubtypeAsync(Guid accountTypeId, string code)
    {
        var response = await _client.PostAsJsonAsync("/api/account-subtypes", new UpsertAccountSubtypeRequest(accountTypeId, code, $"Subtipo {code}", null, null));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AccountSubtypeDto>>();
        return body!.Data!;
    }

    private async Task<ChartOfAccountsDto> CreateChartOfAccountsAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/chart-of-accounts", new UpsertChartOfAccountsRequest(name, null));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ChartOfAccountsDto>>();
        return body!.Data!;
    }
}

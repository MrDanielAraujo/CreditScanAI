using System.Net;
using System.Net.Http.Json;
using CreditScanAI.Api.Contracts;
using FluentAssertions;

namespace CreditScanAI.Tests.Api;

// Serilog's Program.cs bootstrap logger is process-static; running two real
// hosts (WebApplicationFactory<Program>) at once races on freezing it. Share
// one collection with every other such test class so xUnit runs them
// sequentially instead of in parallel.
[Collection(ApiHostTestCollection.Name)]
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithSuccessEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElementWrapper>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // Minimal shape used only to deserialize the anonymous "data" payload without
    // coupling the test to HealthController's internal implementation details.
    public class JsonElementWrapper
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

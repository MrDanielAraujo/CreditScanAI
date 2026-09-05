namespace CreditScanAI.Tests.Api;

/// <summary>
/// Groups every test class that spins up a real WebApplicationFactory
/// (Program.cs's real host) so xUnit runs them sequentially - see the
/// comment on HealthEndpointTests for why running them in parallel breaks.
/// </summary>
[CollectionDefinition(Name)]
public class ApiHostTestCollection
{
    public const string Name = "ApiHost";
}

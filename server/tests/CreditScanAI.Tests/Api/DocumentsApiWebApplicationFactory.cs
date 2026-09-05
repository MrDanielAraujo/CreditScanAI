using CreditScanAI.Api;
using CreditScanAI.Api.Services;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreditScanAI.Tests.Api;

/// <summary>
/// Runs the real API host (controllers, routing, the actual background
/// processing queue) against an in-memory database and a fake file store, so
/// the whole upload -> background processing -> status -> result flow can be
/// exercised over real HTTP without needing Postgres or disk I/O.
/// </summary>
public class DocumentsApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    public byte[] FixturePdfBytes { get; set; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Removing only DbContextOptions<AppDbContext> isn't enough - EF
            // Core also registers internal options-configuration descriptors
            // for Program.cs's UseNpgsql call, and leaving any of them in
            // place makes EF see two providers configured at once. Strip
            // every descriptor generic over AppDbContext before re-adding it
            // with InMemory.
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext)))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IDocumentStorage>();
            services.AddSingleton<IDocumentStorage>(_ => new FakeDocumentStorageThatIgnoresUploadedBytes(() => FixturePdfBytes));
        });
    }

    private sealed class FakeDocumentStorageThatIgnoresUploadedBytes : IDocumentStorage
    {
        private readonly Func<byte[]> _bytesProvider;

        public FakeDocumentStorageThatIgnoresUploadedBytes(Func<byte[]> bytesProvider) => _bytesProvider = bytesProvider;

        public Task<string> SaveAsync(Guid tenantId, Guid documentId, Stream content, CancellationToken cancellationToken) =>
            Task.FromResult($"{tenantId}/{documentId}.pdf");

        public Task<byte[]> ReadAsync(string filePath, CancellationToken cancellationToken) => Task.FromResult(_bytesProvider());
    }
}

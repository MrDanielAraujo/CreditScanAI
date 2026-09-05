using CreditScanAI.Api.Services;

namespace CreditScanAI.Tests.Services;

/// <summary>Always returns the same pre-loaded bytes, ignoring the path.</summary>
public sealed class FakeDocumentStorage : IDocumentStorage
{
    private readonly byte[] _bytes;

    public FakeDocumentStorage(byte[] bytes) => _bytes = bytes;

    public Task<string> SaveAsync(Guid tenantId, Guid documentId, Stream content, CancellationToken cancellationToken) =>
        Task.FromResult($"{tenantId}/{documentId}.pdf");

    public Task<byte[]> ReadAsync(string filePath, CancellationToken cancellationToken) => Task.FromResult(_bytes);
}

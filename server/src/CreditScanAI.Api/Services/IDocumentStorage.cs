namespace CreditScanAI.Api.Services;

public interface IDocumentStorage
{
    Task<string> SaveAsync(Guid tenantId, Guid documentId, Stream content, CancellationToken cancellationToken);
    Task<byte[]> ReadAsync(string filePath, CancellationToken cancellationToken);
}

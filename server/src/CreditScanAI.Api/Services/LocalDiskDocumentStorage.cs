using Microsoft.Extensions.Options;

namespace CreditScanAI.Api.Services;

public class DocumentStorageOptions
{
    public string RootPath { get; set; } = "storage/documents";
}

/// <summary>
/// Stores uploaded PDFs on local disk. Fine for Fase 2 / local dev; an
/// S3-compatible store (MinIO/AWS) is the documented target for production
/// hardening later, not needed to make the upload flow work end-to-end now.
/// </summary>
public class LocalDiskDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public LocalDiskDocumentStorage(IOptions<DocumentStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
    }

    public async Task<string> SaveAsync(Guid tenantId, Guid documentId, Stream content, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, tenantId.ToString());
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{documentId}.pdf");

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return filePath;
    }

    public async Task<byte[]> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }
}

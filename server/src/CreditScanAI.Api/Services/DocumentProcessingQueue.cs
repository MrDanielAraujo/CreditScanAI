using System.Threading.Channels;

namespace CreditScanAI.Api.Services;

public interface IDocumentProcessingQueue
{
    void Enqueue(Guid documentId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// In-process background queue (no message broker). Fine for Fase 2's
/// expected volume; revisit with RabbitMQ/similar only if real throughput
/// demands it later.
/// </summary>
public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid documentId) => _channel.Writer.TryWrite(documentId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

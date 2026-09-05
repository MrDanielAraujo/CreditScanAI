using System.Threading.Channels;

namespace CreditScanAI.Api.Services;

public interface IClassificationProcessingQueue
{
    void Enqueue(Guid documentId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}

public class ClassificationProcessingQueue : IClassificationProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid documentId) => _channel.Writer.TryWrite(documentId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

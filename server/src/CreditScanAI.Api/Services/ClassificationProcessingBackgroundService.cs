namespace CreditScanAI.Api.Services;

public class ClassificationProcessingBackgroundService : BackgroundService
{
    private readonly IClassificationProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClassificationProcessingBackgroundService> _logger;

    public ClassificationProcessingBackgroundService(
        IClassificationProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ClassificationProcessingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var documentId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ClassificationProcessingService>();
                await processor.ProcessAsync(documentId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado classificando documento {DocumentId}", documentId);
            }
        }
    }
}

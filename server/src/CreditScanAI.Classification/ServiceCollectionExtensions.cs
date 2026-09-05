using CreditScanAI.Classification.Ai;
using CreditScanAI.Classification.Rules;
using CreditScanAI.PdfPipeline.Normalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CreditScanAI.Classification;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassificationEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAccountNameNormalizer, AccountNameNormalizer>();
        services.AddScoped<IClassificationRule, ExactMatchRule>();
        services.AddScoped<IClassificationRule, PatternMatchRule>();
        services.AddScoped<IRuleOrchestrator, RuleOrchestrator>();

        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
        services.AddHttpClient<IAiClassificationService, OllamaAiClassificationService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IAccountClassifier, AccountClassifier>();

        return services;
    }
}

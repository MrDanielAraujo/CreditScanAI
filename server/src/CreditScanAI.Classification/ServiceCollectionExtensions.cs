using CreditScanAI.Classification.Rules;
using CreditScanAI.PdfPipeline.Normalization;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.Classification;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassificationEngine(this IServiceCollection services)
    {
        services.AddScoped<IAccountNameNormalizer, AccountNameNormalizer>();
        services.AddScoped<IClassificationRule, ExactMatchRule>();
        services.AddScoped<IClassificationRule, PatternMatchRule>();
        services.AddScoped<IRuleOrchestrator, RuleOrchestrator>();

        return services;
    }
}

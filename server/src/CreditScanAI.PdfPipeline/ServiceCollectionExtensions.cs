using CreditScanAI.PdfPipeline.Extraction;
using CreditScanAI.PdfPipeline.Hierarchy;
using CreditScanAI.PdfPipeline.Normalization;
using CreditScanAI.PdfPipeline.Periods;
using CreditScanAI.PdfPipeline.TableReconstruction;
using CreditScanAI.PdfPipeline.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace CreditScanAI.PdfPipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfPipeline(this IServiceCollection services)
    {
        services.AddScoped<IPdfWordExtractor, PdfWordExtractor>();
        services.AddScoped<ITableReconstructor, TableReconstructor>();
        services.AddScoped<IHierarchyBuilder, HierarchyBuilder>();
        services.AddScoped<ITypeSubtypeDetector, TypeSubtypeDetector>();
        services.AddScoped<IPeriodDetector, PeriodDetector>();
        services.AddScoped<IAccountNameNormalizer, AccountNameNormalizer>();
        services.AddScoped<INumericValueNormalizer, NumericValueNormalizer>();
        services.AddScoped<IPipelineValidator, PipelineValidator>();
        services.AddScoped<IPdfExtractionPipeline, PdfExtractionPipeline>();

        return services;
    }
}

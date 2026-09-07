using CreditScanAI.Domain.Enums;
using CreditScanAI.PdfPipeline.Validation;

namespace CreditScanAI.PdfPipeline.Models;

public sealed class ExtractedFinancialData
{
    public IReadOnlyList<HierarchicalAccount> HierarchicalAccounts { get; init; } = Array.Empty<HierarchicalAccount>();
    public IReadOnlyList<DetectedPeriod> DetectedPeriods { get; init; } = Array.Empty<DetectedPeriod>();
    public IReadOnlyList<DetectedColumn> DetectedColumns { get; init; } = Array.Empty<DetectedColumn>();
    public IReadOnlyList<ExtractedAccountValue> AccountValues { get; init; } = Array.Empty<ExtractedAccountValue>();
    public int ScaleFactor { get; init; } = 1;
    public PipelineValidationResult ValidationResult { get; init; } = new();
    public float OverallConfidence { get; init; }
    /// <summary>Balanço/DRE/Misto, derivado da classificação das contas - null se nada foi classificado.</summary>
    public DocumentType? DetectedDocumentType { get; init; }
    /// <summary>Melhor palpite do nome da empresa, achado perto de um CNPJ no texto do PDF. Null se não achou.</summary>
    public string? DetectedCompanyName { get; init; }
}

using CreditScanAI.PdfPipeline;
using CreditScanAI.PdfPipeline.Extraction;
using CreditScanAI.PdfPipeline.Hierarchy;
using CreditScanAI.PdfPipeline.Normalization;
using CreditScanAI.PdfPipeline.Periods;
using CreditScanAI.PdfPipeline.TableReconstruction;
using CreditScanAI.PdfPipeline.Validation;
using FluentAssertions;
using Xunit.Abstractions;

namespace CreditScanAI.Tests.PdfPipeline;

public class PdfExtractionPipelineTests
{
    private readonly ITestOutputHelper _output;
    private readonly IPdfExtractionPipeline _pipeline;

    public PdfExtractionPipelineTests(ITestOutputHelper output)
    {
        _output = output;
        _pipeline = new PdfExtractionPipeline(
            new PdfWordExtractor(),
            new TableReconstructor(),
            new HierarchyBuilder(),
            new TypeSubtypeDetector(),
            new PeriodDetector(),
            new AccountNameNormalizer(),
            new NumericValueNormalizer(),
            new PipelineValidator());
    }

    [Fact]
    public void Process_BalanceSheet_ExtractsPeriodsHierarchyAndKeyValues()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Balanco2Trim2020.pdf"));

        var result = _pipeline.Process(bytes);

        _output.WriteLine($"Periods: {string.Join(", ", result.DetectedPeriods.Select(p => p.Date))}");
        _output.WriteLine($"Other columns: {string.Join(", ", result.DetectedColumns.Select(c => c.RawLabel))}");
        _output.WriteLine($"Roots: {string.Join(", ", result.HierarchicalAccounts.Select(a => $"{a.OriginalName}[{a.InferredType}]"))}");
        _output.WriteLine($"Scale factor: {result.ScaleFactor}");
        _output.WriteLine($"Overall confidence: {result.OverallConfidence}");
        _output.WriteLine($"Errors: {string.Join(", ", result.ValidationResult.Errors.Select(e => e.Message))}");
        _output.WriteLine($"Warnings: {string.Join(", ", result.ValidationResult.Warnings.Select(w => w.Message))}");

        result.ValidationResult.IsValid.Should().BeTrue();
        result.ScaleFactor.Should().Be(1); // document says "Em reais"

        result.DetectedPeriods.Should().Contain(p => p.Date == new DateOnly(2020, 6, 30));
        result.DetectedPeriods.Should().Contain(p => p.Date == new DateOnly(2019, 12, 31));
        result.DetectedColumns.Should().Contain(c => c.RawLabel.Contains("ADM"));

        var ativo = result.HierarchicalAccounts.Should().ContainSingle(a => a.OriginalName == "Ativo").Subject;
        ativo.InferredType.Should().Be("ATIVO");

        var circulante = ativo.Children.Should().ContainSingle(a => a.OriginalName == "Circulante:").Subject;
        circulante.InferredType.Should().Be("ATIVO");
        circulante.InferredSubtype.Should().Be("CIRCULANTE");

        var caixaEBancos = circulante.Children.Should().ContainSingle(a => a.OriginalName == "Caixa e bancos").Subject;
        caixaEBancos.InferredSubtype.Should().Be("CIRCULANTE");

        var consolidadoJun2020 = result.DetectedPeriods.Single(p => p.Date == new DateOnly(2020, 6, 30)).ColumnIndex;
        var caixaValue = result.AccountValues.Should().ContainSingle(v =>
            v.SourceAccountName == "Caixa e bancos" && v.ColumnIndex == consolidadoJun2020).Subject;
        caixaValue.Value.Should().Be(1_067_737.38m);
    }

    [Fact]
    public void Process_IncomeStatement_DoesNotCrashAndExtractsSomeStructure()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Dre2Trim2020.pdf"));

        var result = _pipeline.Process(bytes);

        _output.WriteLine($"Periods: {result.DetectedPeriods.Count}, Accounts: {result.HierarchicalAccounts.Count}, Values: {result.AccountValues.Count}");
        _output.WriteLine($"Errors: {string.Join(", ", result.ValidationResult.Errors.Select(e => e.Message))}");

        result.DetectedPeriods.Should().NotBeEmpty();
        result.HierarchicalAccounts.Should().NotBeEmpty();
        result.AccountValues.Should().NotBeEmpty();
    }
}

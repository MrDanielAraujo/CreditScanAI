using CreditScanAI.Domain.Enums;
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

        var result = _pipeline.Process(bytes, DocumentType.BalanceSheet);

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

        // Regression: "Despesas antecipadas" (prepaid expenses, an ATIVO line)
        // was misclassified as DRE by a naive substring match on "DESPESA".
        var despesasAntecipadas = circulante.Children.Should().ContainSingle(a => a.OriginalName == "Despesas antecipadas").Subject;
        despesasAntecipadas.InferredType.Should().Be("ATIVO");
        despesasAntecipadas.InferredSubtype.Should().Be("CIRCULANTE");

        var consolidadoJun2020 = result.DetectedPeriods.Single(p => p.Date == new DateOnly(2020, 6, 30)).ColumnIndex;
        var caixaValue = result.AccountValues.Should().ContainSingle(v =>
            v.SourceAccountName == "Caixa e bancos" && v.ColumnIndex == consolidadoJun2020).Subject;
        caixaValue.Value.Should().Be(1_067_737.38m);

        // Regression: this nonprofit's equity section is headed "Patrimônio
        // social:" - the corporate synonym "Patrimônio Líquido" was the only
        // one recognized, so every line under here (and the Patrimônio
        // Líquido total itself) came back with no Subtipo=PL, and the
        // Fase 4 calculation engine always summed PatrimonioLiquido as zero.
        var patrimonioSocial = result.HierarchicalAccounts
            .SelectMany(a => a.Children)
            .Should().ContainSingle(a => a.OriginalName == "Patrimônio social:").Subject;
        patrimonioSocial.InferredType.Should().Be("PASSIVO");
        patrimonioSocial.InferredSubtype.Should().Be("PL");

        var superavitAcumulado = patrimonioSocial.Children.Should().ContainSingle(a => a.OriginalName == "Superávit (déficit) dos exercícios").Subject;
        superavitAcumulado.InferredSubtype.Should().Be("PL");
    }

    [Fact]
    public void Process_IncomeStatement_DoesNotCrashAndExtractsSomeStructure()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Dre2Trim2020.pdf"));

        var result = _pipeline.Process(bytes, DocumentType.IncomeStatement);

        _output.WriteLine($"Periods: {result.DetectedPeriods.Count}, Accounts: {result.HierarchicalAccounts.Count}, Values: {result.AccountValues.Count}");
        _output.WriteLine($"Errors: {string.Join(", ", result.ValidationResult.Errors.Select(e => e.Message))}");

        result.DetectedPeriods.Should().NotBeEmpty();
        result.HierarchicalAccounts.Should().NotBeEmpty();
        result.AccountValues.Should().NotBeEmpty();

        // Regression: this real DRE's section headers use plural forms
        // ("Receitas", "Custos", "Despesas") that the singular-only keyword
        // list doesn't match on their own text - before DocumentType was
        // threaded through, every one of these came back with no Subtipo
        // (and, for a document with no shared DRE ancestor above them, no
        // Tipo either), so the whole DRE branch was unclassifiable.
        var allNodes = Flatten(result.HierarchicalAccounts).ToList();

        allNodes.Where(a => a.OriginalName == "Receitas das atividades")
            .Should().NotBeEmpty().And.OnlyContain(a => a.InferredType == "DRE" && a.InferredSubtype == "RECEITA");

        allNodes.Where(a => a.OriginalName == "Custos operacionais dos programas:")
            .Should().NotBeEmpty().And.OnlyContain(a => a.InferredType == "DRE" && a.InferredSubtype == "CUSTO");

        allNodes.Where(a => a.OriginalName == "Despesas gerais e administrativas dos programas:")
            .Should().NotBeEmpty().And.OnlyContain(a => a.InferredType == "DRE" && a.InferredSubtype == "DESPESA");

        // "Depreciação" needs its own Subtipo=DESPESA specifically (not just
        // inherited Tipo=DRE) since Fase 4 isolates it for the "Resultado
        // Antes de Depreciação e Amortização" indicator.
        allNodes.Where(a => a.OriginalName == "Depreciação")
            .Should().NotBeEmpty().And.OnlyContain(a => a.InferredType == "DRE" && a.InferredSubtype == "DESPESA");
    }

    private static IEnumerable<CreditScanAI.PdfPipeline.Models.HierarchicalAccount> Flatten(
        IReadOnlyList<CreditScanAI.PdfPipeline.Models.HierarchicalAccount> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var descendant in Flatten(node.Children))
            {
                yield return descendant;
            }
        }
    }
}

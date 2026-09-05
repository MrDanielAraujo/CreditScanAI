using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Tests.Services;

public class FinancialCalculationServiceTests
{
    private static async Task<(AppDbContext Db, Guid CompanyId, Guid PeriodId)> SeedScenarioAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var chartId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Active = true, CreatedAt = DateTime.UtcNow });
        db.Companies.Add(new Company { Id = companyId, TenantId = tenantId, Code = "C1", Name = "Empresa Teste", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Periods.Add(new Period { Id = periodId, TenantId = tenantId, PeriodType = PeriodType.Quarterly, Year = 2020, Quarter = 2, StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2020, 6, 30), CreatedAt = DateTime.UtcNow });
        db.ChartOfAccounts.Add(new ChartOfAccounts { Id = chartId, TenantId = tenantId, Name = "Plano", IsDefault = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Documents.Add(new Document
        {
            Id = documentId, TenantId = tenantId, CompanyId = companyId, DocumentType = DocumentType.BalanceSheet,
            UploadDate = DateTime.UtcNow, FileName = "d.pdf", FilePath = "d.pdf", ExtractionStatus = ExtractionStatus.Completed,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var ativo = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "ATIVO", Name = "Ativo", CreatedAt = DateTime.UtcNow };
        var passivo = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "PASSIVO", Name = "Passivo", CreatedAt = DateTime.UtcNow };
        var dre = new AccountType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "DRE", Name = "DRE", CreatedAt = DateTime.UtcNow };
        db.AccountTypes.AddRange(ativo, passivo, dre);

        var circulante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = ativo.Id, Code = "CIRCULANTE", Name = "Circulante", CreatedAt = DateTime.UtcNow };
        var naoCirculante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = ativo.Id, Code = "NAO_CIRCULANTE", Name = "Não Circulante", CreatedAt = DateTime.UtcNow };
        var passivoCirculante = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = passivo.Id, Code = "CIRCULANTE", Name = "Circulante", CreatedAt = DateTime.UtcNow };
        var pl = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = passivo.Id, Code = "PL", Name = "Patrimônio Líquido", CreatedAt = DateTime.UtcNow };
        var receita = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = dre.Id, Code = "RECEITA", Name = "Receita", CreatedAt = DateTime.UtcNow };
        var custo = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = dre.Id, Code = "CUSTO", Name = "Custo", CreatedAt = DateTime.UtcNow };
        var despesa = new AccountSubtype { Id = Guid.NewGuid(), TenantId = tenantId, AccountTypeId = dre.Id, Code = "DESPESA", Name = "Despesa", CreatedAt = DateTime.UtcNow };
        db.AccountSubtypes.AddRange(circulante, naoCirculante, passivoCirculante, pl, receita, custo, despesa);

        StandardAccount Std(string code, string name, AccountType type, AccountSubtype subtype) => new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountsId = chartId, AccountTypeId = type.Id, AccountSubtypeId = subtype.Id,
            Code = code, Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        var caixa = Std("CAIXA", "Caixa", ativo, circulante);
        var imobilizado = Std("IMOB", "Imobilizado", ativo, naoCirculante);
        var fornecedores = Std("FORN", "Fornecedores", passivo, passivoCirculante);
        var patrimonio = Std("PL", "Patrimônio Social", passivo, pl);
        var receitas = Std("REC", "Receitas", dre, receita);
        var custos = Std("CUS", "Custos", dre, custo);
        var despesas = Std("DESP", "Despesas", dre, despesa);
        var depreciacao = Std("DRE_DESPESA_DEPRECIACAO", "Depreciação", dre, despesa);
        db.StandardAccounts.AddRange(caixa, imobilizado, fornecedores, patrimonio, receitas, custos, despesas, depreciacao);

        void AddClassifiedValue(StandardAccount standardAccount, decimal rawValue)
        {
            var sourceAccountId = Guid.NewGuid();
            db.SourceAccounts.Add(new SourceAccount
            {
                Id = sourceAccountId, TenantId = tenantId, DocumentId = documentId, OriginalName = standardAccount.Name,
                NormalizedName = standardAccount.Name.ToUpperInvariant(), HierarchyLevel = 1, CreatedAt = DateTime.UtcNow
            });
            db.AccountClassifications.Add(new AccountClassification
            {
                Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, StandardAccountId = standardAccount.Id,
                ChartOfAccountsId = chartId, ConfidenceScore = 0.99m, ClassificationMethod = "EXACT_MATCH",
                ReviewStatus = ClassificationReviewStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            db.AccountValues.Add(new AccountValue
            {
                Id = Guid.NewGuid(), TenantId = tenantId, SourceAccountId = sourceAccountId, DocumentId = documentId,
                PeriodId = periodId, RawValue = rawValue, ScaleFactor = 1, CreatedAt = DateTime.UtcNow
            });
        }

        AddClassifiedValue(caixa, 1000m);
        AddClassifiedValue(imobilizado, 500m);
        AddClassifiedValue(fornecedores, 300m);
        AddClassifiedValue(patrimonio, 1200m);
        AddClassifiedValue(receitas, 2000m);
        AddClassifiedValue(custos, -800m);
        AddClassifiedValue(despesas, -500m);
        AddClassifiedValue(depreciacao, -100m);

        await db.SaveChangesAsync();
        return (db, companyId, periodId);
    }

    [Fact]
    public async Task CalculateAsync_ComputesTotaisEIndicadoresCorretamente()
    {
        var (db, companyId, periodId) = await SeedScenarioAsync();
        await using var _ = db;

        var service = new FinancialCalculationService(db);
        var values = await service.CalculateAsync(companyId, periodId, CancellationToken.None);

        values[CalculationKeys.AtivoCirculante].Should().Be(1000m);
        values[CalculationKeys.AtivoNaoCirculante].Should().Be(500m);
        values[CalculationKeys.AtivoTotal].Should().Be(1500m);

        values[CalculationKeys.PassivoCirculante].Should().Be(300m);
        values[CalculationKeys.PassivoTotal].Should().Be(300m);
        values[CalculationKeys.PatrimonioLiquido].Should().Be(1200m);

        values[CalculationKeys.ReceitaTotal].Should().Be(2000m);
        values[CalculationKeys.CustoTotal].Should().Be(-800m);
        // Despesas inclui Depreciação, já que ambas são Subtipo=DESPESA.
        values[CalculationKeys.DespesaTotal].Should().Be(-600m);
        values[CalculationKeys.DepreciacaoAmortizacaoTotal].Should().Be(-100m);

        values[CalculationKeys.ResultadoPeriodo].Should().Be(600m); // 2000 - 800 - 600
        values[CalculationKeys.ResultadoAntesDepreciacaoAmortizacao].Should().Be(700m); // 600 - (-100)
        values[CalculationKeys.MargemResultado].Should().Be(30m); // 600/2000*100
        values[CalculationKeys.LiquidezCorrente].Should().Be(Math.Round(1000m / 300m, 4)); // Ativo Circ / Passivo Circ
        values[CalculationKeys.IndiceEndividamento].Should().Be(0.25m); // 300/1200

        // Ativo (1500) = Passivo (300) + PL (1200) -> balanceada.
        values[CalculationKeys.EquacaoVariancia].Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_CalledTwice_OverwritesInsteadOfDuplicating()
    {
        var (db, companyId, periodId) = await SeedScenarioAsync();
        await using var _ = db;

        var service = new FinancialCalculationService(db);
        await service.CalculateAsync(companyId, periodId, CancellationToken.None);
        await service.CalculateAsync(companyId, periodId, CancellationToken.None);

        var rows = await db.CalculatedFinancialValues.Where(v => v.CompanyId == companyId && v.PeriodId == periodId).ToListAsync();
        rows.Should().HaveCount(CalculationKeys.All.Count);
        rows.Select(r => r.Key).Should().BeEquivalentTo(CalculationKeys.All);
    }
}

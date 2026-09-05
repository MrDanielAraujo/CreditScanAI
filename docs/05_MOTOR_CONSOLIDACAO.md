# 5. Especificação do Motor de Consolidação

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026

---

## 1. Visão Geral

O Motor de Consolidação integra demonstrações financeiras de múltiplas empresas/entidades em um único resultado consolidado, aplicando:

1. **Consolidação por Período** - Agregar múltiplos períodos
2. **Consolidação por Entidade** - Agregar múltiplas empresas
3. **Eliminação de Transações** - Remove transações inter-empresariais
4. **Reconciliação** - Valida integridade dos dados consolidados

---

## 2. Estratégias de Consolidação

### 2.1 Consolidação Simples (Soma)

```csharp
public class SimpleConsolidationStrategy : IConsolidationStrategy
{
    public ConsolidationResult Consolidate(
        List<CompanyStatement> statements,
        ConsolidationSettings settings)
    {
        var result = new ConsolidationResult();

        // Agrupar por conta padrão
        var consolidatedAccounts = new Dictionary<Guid, decimal>();

        foreach (var statement in statements)
        {
            foreach (var account in statement.Accounts)
            {
                if (!consolidatedAccounts.ContainsKey(account.StandardAccountId))
                    consolidatedAccounts[account.StandardAccountId] = 0m;

                consolidatedAccounts[account.StandardAccountId] += account.Value;
            }
        }

        result.ConsolidatedAccounts = consolidatedAccounts;
        return result;
    }
}
```

### 2.2 Consolidação Ponderada

```csharp
public class WeightedConsolidationStrategy : IConsolidationStrategy
{
    public ConsolidationResult Consolidate(
        List<CompanyStatement> statements,
        ConsolidationSettings settings)
    {
        var result = new ConsolidationResult();
        var consolidatedAccounts = new Dictionary<Guid, decimal>();

        var totalWeight = statements.Sum(s => s.Weight ?? 1m);

        foreach (var statement in statements)
        {
            var weight = (statement.Weight ?? 1m) / totalWeight;

            foreach (var account in statement.Accounts)
            {
                if (!consolidatedAccounts.ContainsKey(account.StandardAccountId))
                    consolidatedAccounts[account.StandardAccountId] = 0m;

                consolidatedAccounts[account.StandardAccountId] += account.Value * weight;
            }
        }

        result.ConsolidatedAccounts = consolidatedAccounts;
        result.UsedWeights = statements.ToDictionary(s => s.CompanyId, s => s.Weight ?? 1m);

        return result;
    }
}
```

---

## 3. Motor de Eliminações

```csharp
public class IntercompanyEliminationEngine
{
    public class EliminationRule
    {
        public Guid RuleId { get; set; }
        public string Description { get; set; }
        public List<Guid> InvolvedCompanies { get; set; }
        public Guid AccountToEliminate { get; set; }
        public string EliminationMethod { get; set; } // FULL, PROPORTIONAL
        public bool IsActive { get; set; }
    }

    public class EliminationEntry
        {
        public Guid RuleId { get; set; }
        public Guid FromCompany { get; set; }
        public Guid ToCompany { get; set; }
        public Guid StandardAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task<List<EliminationEntry>> IdentifyIntercompanyTransactionsAsync(
        Guid periodId,
        List<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var eliminations = new List<EliminationEntry>();

        // Estratégia 1: Contas Mútuas
        // Se Empresa A tem "Contas a Receber de B" e Empresa B tem "Contas a Pagar para A"
        var receivables = await GetAccountValuesByCompanyAsync(
            periodId, companyIds, "CONTAS_RECEBER_PARTES_RELACIONADAS");
        var payables = await GetAccountValuesByCompanyAsync(
            periodId, companyIds, "CONTAS_PAGAR_PARTES_RELACIONADAS");

        foreach (var receivable in receivables)
        {
            foreach (var payable in payables)
            {
                if (receivable.FromCompany == payable.ToCompany &&
                    receivable.ToCompany == payable.FromCompany &&
                    Math.Abs(receivable.Amount - payable.Amount) < 0.01m) // Tolerância de arredondamento
                {
                    // Encontrou transação bidirecional
                    eliminations.Add(new EliminationEntry
                    {
                        FromCompany = receivable.FromCompany,
                        ToCompany = receivable.ToCompany,
                        StandardAccountId = receivable.StandardAccountId,
                        Amount = receivable.Amount,
                        Description = $"Eliminação de transação entre {receivable.FromCompany} e {receivable.ToCompany}"
                    });
                }
            }
        }

        return eliminations;
    }

    public async Task<ConsolidatedStatement> ApplyEliminationsAsync(
        ConsolidatedStatement statement,
        List<EliminationEntry> eliminations,
        CancellationToken cancellationToken)
    {
        foreach (var elimination in eliminations)
        {
            if (!statement.ConsolidatedValues.ContainsKey(elimination.StandardAccountId))
                continue;

            switch (elimination.EliminationMethod ?? "FULL")
            {
                case "FULL":
                    statement.ConsolidatedValues[elimination.StandardAccountId] -= elimination.Amount;
                    break;

                case "PROPORTIONAL":
                    // Eliminação proporcional baseada em participação
                    statement.ConsolidatedValues[elimination.StandardAccountId] -= 
                        elimination.Amount * 0.5m; // Exemplo
                    break;
            }

            statement.Eliminations.Add(elimination);
        }

        return statement;
    }

    private Task<List<IntercompanyTransaction>> GetAccountValuesByCompanyAsync(
        Guid periodId,
        List<Guid> companyIds,
        string accountType)
    {
        // Implementação
        return Task.FromResult(new List<IntercompanyTransaction>());
    }
}
```

---

## 4. Motor de Reconciliação

```csharp
public class ReconciliationEngine
{
    public class ReconciliationCheck
    {
        public string CheckId { get; set; }
        public string Description { get; set; }
        public bool Passed { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal Variance { get; set; }
        public string ErrorMessage { get; set; }
    }

    public async Task<List<ReconciliationCheck>> RunReconciliationAsync(
        ConsolidatedStatement statement,
        CancellationToken cancellationToken)
    {
        var checks = new List<ReconciliationCheck>();

        // Check 1: Somas batem com originais
        checks.Add(await CheckSourceTotalsAsync(statement, cancellationToken));

        // Check 2: Equação fundamental válida
        checks.Add(CheckBasicEquation(statement));

        // Check 3: Sem valores negativos indevidos
        checks.Add(CheckSignConsistency(statement));

        // Check 4: Sem valores anómalos
        checks.Add(await CheckAnomalousValuesAsync(statement, cancellationToken));

        return checks;
    }

    private async Task<ReconciliationCheck> CheckSourceTotalsAsync(
        ConsolidatedStatement statement,
        CancellationToken cancellationToken)
    {
        var check = new ReconciliationCheck
        {
            CheckId = "SOURCE_TOTALS",
            Description = "Totais consolidados devem ser soma dos originais"
        };

        // Para cada conta consolidada, verificar se é a soma correta
        var consolidatedTotal = statement.ConsolidatedValues.Values.Sum();
        var sourceTotal = statement.SourceStatements
            .SelectMany(s => s.Accounts)
            .Sum(a => a.Value);

        check.ExpectedValue = sourceTotal;
        check.ActualValue = consolidatedTotal;
        check.Variance = Math.Abs(consolidatedTotal - sourceTotal);
        check.Passed = check.Variance < 1m; // Tolerância de 1 real

        if (!check.Passed)
        {
            check.ErrorMessage = $"Variância de {check.Variance} entre totais consolidados e fonte";
        }

        return check;
    }

    private ReconciliationCheck CheckBasicEquation(ConsolidatedStatement statement)
    {
        var check = new ReconciliationCheck
        {
            CheckId = "BASIC_EQUATION",
            Description = "Ativo = Passivo + PL"
        };

        var assets = statement.ConsolidatedValues
            .Where(kv => kv.Key.ToString().StartsWith("ATIVO"))
            .Sum(kv => kv.Value);

        var liabilities = statement.ConsolidatedValues
            .Where(kv => kv.Key.ToString().StartsWith("PASSIVO"))
            .Sum(kv => kv.Value);

        var equity = statement.ConsolidatedValues
            .Where(kv => kv.Key.ToString().StartsWith("PL"))
            .Sum(kv => kv.Value);

        check.ExpectedValue = assets;
        check.ActualValue = liabilities + equity;
        check.Variance = Math.Abs(check.ExpectedValue - check.ActualValue);
        check.Passed = check.Variance < 1m;

        return check;
    }

    private ReconciliationCheck CheckSignConsistency(ConsolidatedStatement statement)
    {
        var check = new ReconciliationCheck
        {
            CheckId = "SIGN_CONSISTENCY",
            Description = "Verificar consistência de sinais"
        };

        var inconsistencies = new List<string>();

        foreach (var account in statement.ConsolidatedValues)
        {
            // Ativo deve ser sempre positivo
            if (account.Key.ToString().Contains("ATIVO") && account.Value < 0)
                inconsistencies.Add($"Ativo negativo: {account.Key}");

            // Despesa deve ser sempre negativa (ou positiva em DRE)
            if (account.Key.ToString().Contains("DESPESA") && account.Value > 0)
                inconsistencies.Add($"Despesa positiva: {account.Key}");
        }

        check.Passed = !inconsistencies.Any();
        if (!check.Passed)
            check.ErrorMessage = string.Join("; ", inconsistencies);

        return check;
    }

    private async Task<ReconciliationCheck> CheckAnomalousValuesAsync(
        ConsolidatedStatement statement,
        CancellationToken cancellationToken)
    {
        var check = new ReconciliationCheck
        {
            CheckId = "ANOMALOUS_VALUES",
            Description = "Detectar valores fora do padrão histórico"
        };

        var anomalies = new List<string>();

        // Comparar com período anterior
        // Se uma conta mudou mais de X%, marcar

        check.Passed = !anomalies.Any();
        if (!check.Passed)
            check.ErrorMessage = string.Join("; ", anomalies);

        return check;
    }
}
```

---

## 5. Orquestrador de Consolidação

```csharp
public interface IConsolidationEngine
{
    Task<ConsolidatedStatement> ConsolidateAsync(
        Guid periodId,
        List<Guid> companyIds,
        ConsolidationSettings settings,
        CancellationToken cancellationToken);
}

public class ConsolidationEngine : IConsolidationEngine
{
    private readonly IConsolidationStrategy _strategy;
    private readonly IntercompanyEliminationEngine _eliminationEngine;
    private readonly ReconciliationEngine _reconciliationEngine;
    private readonly ILogger<ConsolidationEngine> _logger;

    public async Task<ConsolidatedStatement> ConsolidateAsync(
        Guid periodId,
        List<Guid> companyIds,
        ConsolidationSettings settings,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando consolidação: período {PeriodId}, {CompanyCount} empresas",
            periodId, companyIds.Count);

        var result = new ConsolidatedStatement
        {
            PeriodId = periodId,
            ConsolidatedDate = DateTime.UtcNow,
            ConsolidationMethod = settings.ConsolidationMethod
        };

        try
        {
            // Etapa 1: Buscar demonstrações individuais
            result.SourceStatements = await GetCompanyStatementsAsync(periodId, companyIds);

            // Etapa 2: Aplicar estratégia de consolidação
            var consolidationResult = _strategy.Consolidate(result.SourceStatements, settings);
            result.ConsolidatedValues = consolidationResult.ConsolidatedValues;

            // Etapa 3: Identificar e eliminar transações inter-empresariais
            var eliminations = await _eliminationEngine
                .IdentifyIntercompanyTransactionsAsync(periodId, companyIds, cancellationToken);

            result = await _eliminationEngine.ApplyEliminationsAsync(
                result, eliminations, cancellationToken);

            // Etapa 4: Reconciliar
            result.ReconciliationChecks = await _reconciliationEngine
                .RunReconciliationAsync(result, cancellationToken);

            result.IsValid = result.ReconciliationChecks.All(c => c.Passed);

            _logger.LogInformation(
                "Consolidação concluída: {AccountCount} contas, Válida: {IsValid}",
                result.ConsolidatedValues.Count,
                result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na consolidação");
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private Task<List<CompanyStatement>> GetCompanyStatementsAsync(
        Guid periodId,
        List<Guid> companyIds)
    {
        // Implementação
        return Task.FromResult(new List<CompanyStatement>());
    }
}

public class ConsolidatedStatement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PeriodId { get; set; }
    public DateTime ConsolidatedDate { get; set; }
    public string ConsolidationMethod { get; set; }
    
    public List<CompanyStatement> SourceStatements { get; set; } = new();
    public Dictionary<Guid, decimal> ConsolidatedValues { get; set; } = new();
    public List<IntercompanyEliminationEngine.EliminationEntry> Eliminations { get; set; } = new();
    
    public List<ReconciliationEngine.ReconciliationCheck> ReconciliationChecks { get; set; } = new();
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}

public class ConsolidationSettings
{
    public string ConsolidationMethod { get; set; } = "SIMPLE"; // SIMPLE, WEIGHTED, PROPORTIONAL
    public bool EliminateIntercompanyTransactions { get; set; } = true;
    public bool PerformReconciliation { get; set; } = true;
    public float ReconciliationTolerancePercentage { get; set; } = 0.01f;
}
```

---

## 6. Critérios de Aceite

- ✅ Consolidação simples (soma) funciona
- ✅ Consolidação ponderada funciona
- ✅ Identifica transações inter-empresariais
- ✅ Elimina valores corretamente
- ✅ Valida somas consolidadas
- ✅ Verifica equação fundamental pós-consolidação
- ✅ Detecta valores anômalos
- ✅ Retorna trail de reconciliação

---

## 7. Próximos Passos

1. Implementar estratégias de consolidação reais
2. Criar regras de eliminação específicas por setor
3. Prosseguir com **06 - Human-in-the-loop e Aprendizado**


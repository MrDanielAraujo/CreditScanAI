# 4. Especificação do Motor de Cálculos

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** C#/ASP.NET Core, Expression Trees, Formula Parser

---

## 1. Visão Geral

O Motor de Cálculos é responsável por:

1. **Calcular contas derivadas** (totalizações, subtotais)
2. **Aplicar regras de sinal** (débito/crédito)
3. **Gerar KPIs financeiros** (EBITDA, Margens, Índices)
4. **Validar equações contábeis** (Ativo = Passivo + PL)
5. **Rastrear dependências** (árvore de cálculos)

---

## 2. Arquitetura de Cálculos

```
┌──────────────────────────────────────────┐
│      VALORES CLASSIFICADOS               │
│  (contas padrão + valores brutos)        │
└───────────────┬──────────────────────────┘
                │
        ┌───────▼────────┐
        │ Regras de Sinal│
        │ Normalizar (+/-│
        └───────┬────────┘
                │
        ┌───────▼────────┐
        │ Calcular       │
        │ Subtotais      │
        │ Totais         │
        └───────┬────────┘
                │
        ┌───────▼────────┐
        │ Gerar KPIs     │
        │ Indicadores    │
        └───────┬────────┘
                │
        ┌───────▼────────┐
        │ Validar        │
        │ Equações       │
        └───────┬────────┘
                │
        ┌───────▼────────────────┐
        │ Resultado com Auditoria│
        │ (trail completo)       │
        └────────────────────────┘
```

---

## 3. Engine de Fórmulas

### 3.1 Definição de Fórmulas

```csharp
public class FormulaDefinition
{
    public Guid StandardAccountId { get; set; }
    public string FormulaExpression { get; set; } // Ex: "SUM(CIRCULANTE_*)" ou "a.1 + a.2 + a.3"
    public FormulaType Type { get; set; } // SUM, CUSTOM, CONDITIONAL
    public List<Guid> DependentAccountIds { get; set; } // Contas que precisam ser calculadas primeiro
    public int CalculationOrder { get; set; } // Ordem de execução
    public string Description { get; set; }
    public bool IsRequired { get; set; }
}

public enum FormulaType
{
    DirectValue,      // Valor direto (não calculado)
    Sum,              // SUM de contas filhas
    Difference,       // Diferença entre contas
    Conditional,      // IF-THEN-ELSE
    Custom            // Fórmula customizada
}
```

### 3.2 Avaliador de Fórmulas

```csharp
public interface IFormulaEvaluator
{
    Task<FormulaResult> EvaluateAsync(
        FormulaDefinition formula,
        Dictionary<Guid, decimal> accountValues,
        CancellationToken cancellationToken);
}

public class FormulaResult
{
    public Guid StandardAccountId { get; set; }
    public decimal CalculatedValue { get; set; }
    public List<string> CalculationSteps { get; set; } = new();
    public List<Guid> UsedAccounts { get; set; } = new();
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}

public class ExpressionTreeFormulaEvaluator : IFormulaEvaluator
{
    private readonly ILogger<ExpressionTreeFormulaEvaluator> _logger;

    public async Task<FormulaResult> EvaluateAsync(
        FormulaDefinition formula,
        Dictionary<Guid, decimal> accountValues,
        CancellationToken cancellationToken)
    {
        var result = new FormulaResult
        {
            StandardAccountId = formula.StandardAccountId
        };

        try
        {
            switch (formula.Type)
            {
                case FormulaType.DirectValue:
                    // Sem cálculo, usar valor direto
                    result.CalculatedValue = accountValues.GetValueOrDefault(formula.StandardAccountId, 0m);
                    result.IsValid = true;
                    break;

                case FormulaType.Sum:
                    result = EvaluateSumFormula(formula, accountValues);
                    break;

                case FormulaType.Conditional:
                    result = await EvaluateConditionalFormulaAsync(
                        formula, accountValues, cancellationToken);
                    break;

                case FormulaType.Custom:
                    result = EvaluateCustomFormula(formula, accountValues);
                    break;

                default:
                    result.IsValid = false;
                    result.ErrorMessage = "Tipo de fórmula desconhecido";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao avaliar fórmula para {AccountId}", formula.StandardAccountId);
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private FormulaResult EvaluateSumFormula(
        FormulaDefinition formula,
        Dictionary<Guid, decimal> accountValues)
    {
        var result = new FormulaResult
        {
            StandardAccountId = formula.StandardAccountId
        };

        decimal sum = 0m;

        // Fórmula SUM pode ter wildcard: "SUM(CIRCULANTE_*)"
        // Ou lista: "SUM(a.1.1, a.1.2, a.1.3)"

        if (formula.FormulaExpression.Contains("*"))
        {
            // Wildcard matching
            var pattern = formula.FormulaExpression
                .Replace("SUM(", "")
                .Replace(")", "")
                .Replace("*", "");

            var matchingAccounts = accountValues
                .Where(kv => kv.Key.ToString().StartsWith(pattern))
                .ToList();

            foreach (var account in matchingAccounts)
            {
                sum += account.Value;
                result.UsedAccounts.Add(account.Key);
                result.CalculationSteps.Add($"+ {account.Value}");
            }
        }
        else
        {
            // Lista explícita de contas
            // Extrair IDs de contas da expressão
            var accountIds = ParseAccountIdsFromExpression(formula.FormulaExpression);

            foreach (var accountId in accountIds)
            {
                if (accountValues.TryGetValue(accountId, out var value))
                {
                    sum += value;
                    result.UsedAccounts.Add(accountId);
                    result.CalculationSteps.Add($"+ {value}");
                }
            }
        }

        result.CalculatedValue = sum;
        result.IsValid = true;

        return result;
    }

    private FormulaResult EvaluateCustomFormula(
        FormulaDefinition formula,
        Dictionary<Guid, decimal> accountValues)
    {
        // Exemplo: "a.1 + a.2 - a.3" onde a.1, a.2, a.3 são substitutos de contas
        
        var result = new FormulaResult
        {
            StandardAccountId = formula.StandardAccountId
        };

        try
        {
            // Substituir placeholders por valores reais
            var expression = formula.FormulaExpression;
            var accountMappings = new Dictionary<string, decimal>();

            var placeholders = System.Text.RegularExpressions.Regex
                .Matches(expression, @"\b[a-z]\.\d+\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            foreach (var placeholder in placeholders)
            {
                // Mapear "a.1" para seu valor (precisa de tabela de mapeamento)
                accountMappings[placeholder] = 0m; // Buscar do accountValues
                expression = expression.Replace(placeholder, accountMappings[placeholder].ToString());
            }

            // Avaliar expressão matemática
            var dataTable = new System.Data.DataTable();
            var calculatedValue = dataTable.Compute(expression, null);

            result.CalculatedValue = Convert.ToDecimal(calculatedValue);
            result.IsValid = true;
            result.CalculationSteps.Add($"Expressão: {formula.FormulaExpression} = {result.CalculatedValue}");
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Erro ao avaliar expressão: {ex.Message}";
        }

        return result;
    }

    private async Task<FormulaResult> EvaluateConditionalFormulaAsync(
        FormulaDefinition formula,
        Dictionary<Guid, decimal> accountValues,
        CancellationToken cancellationToken)
    {
        // Exemplo: IF(SUM(RECEITA) > 100000, EBITDA_CALCULATION, 0)
        // Implementação futura
        return new FormulaResult { IsValid = false };
    }

    private List<Guid> ParseAccountIdsFromExpression(string expression)
    {
        // Parser para extrair IDs de contas
        // Exemplo: "(acc-1, acc-2, acc-3)" → [Guid, Guid, Guid]
        return new List<Guid>();
    }
}
```

---

## 4. Resolvedor de Dependências e Ordem de Cálculo

```csharp
public class CalculationDependencyResolver
{
    public class DependencyGraph
    {
        public Dictionary<Guid, List<Guid>> Dependencies { get; set; } // account → contas que precisa
        public List<Guid> CalculationOrder { get; set; } // Ordem topológica
        public bool HasCircularDependency { get; set; }
    }

    public DependencyGraph ResolveDependencies(
        List<FormulaDefinition> formulas)
    {
        var graph = new DependencyGraph
        {
            Dependencies = formulas.ToDictionary(f => f.StandardAccountId, f => f.DependentAccountIds),
            CalculationOrder = new List<Guid>()
        };

        // Topological sort (Kahn's algorithm)
        var inDegree = new Dictionary<Guid, int>();
        var accountsToProcess = new Queue<Guid>();

        // Inicializar
        foreach (var formula in formulas)
        {
            if (!inDegree.ContainsKey(formula.StandardAccountId))
                inDegree[formula.StandardAccountId] = 0;

            foreach (var dep in formula.DependentAccountIds)
            {
                if (!inDegree.ContainsKey(dep))
                    inDegree[dep] = 0;

                inDegree[formula.StandardAccountId]++;
            }
        }

        // Começa com contas sem dependências
        foreach (var account in inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key))
        {
            accountsToProcess.Enqueue(account);
        }

        while (accountsToProcess.Count > 0)
        {
            var account = accountsToProcess.Dequeue();
            graph.CalculationOrder.Add(account);

            // Procurar contas que dependem desta
            var dependents = graph.Dependencies
                .Where(kv => kv.Value.Contains(account))
                .Select(kv => kv.Key);

            foreach (var dependent in dependents)
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    accountsToProcess.Enqueue(dependent);
            }
        }

        // Verificar dependência circular
        graph.HasCircularDependency = graph.CalculationOrder.Count != formulas.Count;

        return graph;
    }
}
```

---

## 5. Motor de Regras de Sinal

```csharp
public class SignRuleEngine
{
    public class SignRule
    {
        public Guid StandardAccountId { get; set; }
        public string ExpectedSign { get; set; } // '+' ou '-'
        public string Description { get; set; }
        public List<string> ReasonsForThisSign { get; set; }
    }

    private readonly Dictionary<string, string> _signRulesByType = new()
    {
        // ATIVO (sempre positivo)
        { "ATIVO_CIRCULANTE_*", "+" },
        { "ATIVO_NAO_CIRCULANTE_*", "+" },
        { "ATIVO_PERMANENTE_*", "+" },

        // PASSIVO (sempre positivo - é uma obrigação)
        { "PASSIVO_CIRCULANTE_*", "+" },
        { "PASSIVO_NAO_CIRCULANTE_*", "+" },

        // PATRIMÔNIO LÍQUIDO
        { "PASSIVO_PL_*", "+" },

        // DRE (Receita positiva, Despesa negativa)
        { "DRE_RECEITA_*", "+" },
        { "DRE_DESPESA_*", "-" },
        { "DRE_CUSTO_*", "-" },
    };

    public SignRule GetSignRule(StandardAccount account)
    {
        var key = $"{account.Type}_{account.Subtype}_*";
        var expectedSign = _signRulesByType.GetValueOrDefault(key, "+");

        return new SignRule
        {
            StandardAccountId = account.Id,
            ExpectedSign = expectedSign,
            Description = $"Conta de {account.Type} deve ser {(expectedSign == "+" ? "positiva" : "negativa")}",
            ReasonsForThisSign = GetReasonsForSign(account)
        };
    }

    public decimal ApplySign(decimal value, string expectedSign)
    {
        // Se valor já vem negativo no documento mas deveria ser positivo
        return expectedSign == "+" ? Math.Abs(value) : -Math.Abs(value);
    }

    private List<string> GetReasonsForSign(StandardAccount account)
    {
        var reasons = new List<string>();

        if (account.Type == "ATIVO")
            reasons.Add("Ativo representa valores que a empresa possui");

        if (account.Type.Contains("RECEITA"))
            reasons.Add("Receita é entrada de valor");

        if (account.Type.Contains("DESPESA"))
            reasons.Add("Despesa é saída de valor");

        return reasons;
    }
}
```

---

## 6. Calculadora de KPIs Financeiros

```csharp
public interface IKpiCalculator
{
    Task<KpiCalculationResult> CalculateKpisAsync(
        Guid periodId,
        Guid companyId,
        Dictionary<string, decimal> standardAccountValues,
        CancellationToken cancellationToken);
}

public class KpiCalculationResult
{
    public Dictionary<string, decimal> Kpis { get; set; } = new();
    public Dictionary<string, string> KpiDescriptions { get; set; } = new();
    public List<string> CalculationAudit { get; set; } = new();
}

public class KpiCalculator : IKpiCalculator
{
    public async Task<KpiCalculationResult> CalculateKpisAsync(
        Guid periodId,
        Guid companyId,
        Dictionary<string, decimal> standardAccountValues,
        CancellationToken cancellationToken)
    {
        var result = new KpiCalculationResult();

        // 1. EBITDA = Lucro Operacional + Depreciação + Amortização
        try
        {
            var operatingProfit = standardAccountValues.GetValueOrDefault("LUCRO_OPERACIONAL", 0m);
            var depreciation = standardAccountValues.GetValueOrDefault("DEPRECIACAO", 0m);
            var amortization = standardAccountValues.GetValueOrDefault("AMORTIZACAO", 0m);

            var ebitda = operatingProfit + depreciation + amortization;
            result.Kpis["EBITDA"] = ebitda;
            result.KpiDescriptions["EBITDA"] = "Lucro antes de juros, impostos, depreciação e amortização";
            result.CalculationAudit.Add($"EBITDA = {operatingProfit} + {depreciation} + {amortization} = {ebitda}");
        }
        catch (Exception ex)
        {
            result.CalculationAudit.Add($"Erro ao calcular EBITDA: {ex.Message}");
        }

        // 2. Margens
        try
        {
            var revenue = standardAccountValues.GetValueOrDefault("RECEITA_BRUTA", 0m);
            var netIncome = standardAccountValues.GetValueOrDefault("LUCRO_LIQUIDO", 0m);
            var operatingIncome = standardAccountValues.GetValueOrDefault("LUCRO_OPERACIONAL", 0m);

            if (revenue != 0)
            {
                result.Kpis["MARGEM_LIQUIDA"] = (netIncome / revenue) * 100;
                result.Kpis["MARGEM_OPERACIONAL"] = (operatingIncome / revenue) * 100;
                result.CalculationAudit.Add($"Margem Líquida = ({netIncome} / {revenue}) * 100 = {result.Kpis["MARGEM_LIQUIDA"]}%");
            }
        }
        catch (Exception ex)
        {
            result.CalculationAudit.Add($"Erro ao calcular margens: {ex.Message}");
        }

        // 3. Índices de Liquidez
        try
        {
            var currentAssets = standardAccountValues.GetValueOrDefault("ATIVO_CIRCULANTE", 0m);
            var currentLiabilities = standardAccountValues.GetValueOrDefault("PASSIVO_CIRCULANTE", 0m);

            if (currentLiabilities != 0)
            {
                result.Kpis["LIQUIDEZ_CORRENTE"] = currentAssets / currentLiabilities;
                result.CalculationAudit.Add($"Liquidez Corrente = {currentAssets} / {currentLiabilities} = {result.Kpis["LIQUIDEZ_CORRENTE"]}");
            }
        }
        catch (Exception ex)
        {
            result.CalculationAudit.Add($"Erro ao calcular índices de liquidez: {ex.Message}");
        }

        // 4. Índices de Endividamento
        try
        {
            var totalDebt = standardAccountValues.GetValueOrDefault("PASSIVO_TOTAL", 0m);
            var equity = standardAccountValues.GetValueOrDefault("PATRIMONIO_LIQUIDO", 0m);

            if (equity != 0)
            {
                result.Kpis["INDICE_ENDIVIDAMENTO"] = totalDebt / equity;
                result.CalculationAudit.Add($"Índice Endividamento = {totalDebt} / {equity} = {result.Kpis["INDICE_ENDIVIDAMENTO"]}");
            }
        }
        catch (Exception ex)
        {
            result.CalculationAudit.Add($"Erro ao calcular índices de endividamento: {ex.Message}");
        }

        return result;
    }
}
```

---

## 7. Validação de Equações Contábeis

```csharp
public class AccountingEquationValidator
{
    public class EquationValidationResult
    {
        public bool IsValid { get; set; }
        public decimal LeftSide { get; set; }
        public decimal RightSide { get; set; }
        public decimal Variance { get; set; }
        public float VariancePercentage { get; set; }
        public List<string> ValidationSteps { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public EquationValidationResult ValidateBasicEquation(
        Dictionary<string, decimal> standardAccountValues)
    {
        var result = new EquationValidationResult();

        try
        {
            // Equação: ATIVO = PASSIVO + PATRIMÔNIO LÍQUIDO
            var totalAssets = standardAccountValues.GetValueOrDefault("ATIVO_TOTAL", 0m);
            var totalLiabilities = standardAccountValues.GetValueOrDefault("PASSIVO_TOTAL", 0m);
            var equity = standardAccountValues.GetValueOrDefault("PATRIMONIO_LIQUIDO", 0m);

            result.LeftSide = totalAssets;
            result.RightSide = totalLiabilities + equity;
            result.Variance = Math.Abs(result.LeftSide - result.RightSide);

            result.ValidationSteps.Add($"Ativo Total (LHS) = {totalAssets}");
            result.ValidationSteps.Add($"Passivo Total + PL (RHS) = {totalLiabilities} + {equity} = {result.RightSide}");
            result.ValidationSteps.Add($"Variância = {result.Variance}");

            // Permite variância de até 1 real (devido a arredondamentos)
            var tolerance = 1m;
            result.IsValid = result.Variance <= tolerance;

            if (result.Variance > tolerance)
            {
                result.VariancePercentage = (result.Variance / result.LeftSide) * 100;
                result.ErrorMessage = $"Equação desbalanceada: Variância de {result.Variance} ({result.VariancePercentage}%)";
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
```

---

## 8. Orquestrador de Cálculos

```csharp
public interface ICalculationEngine
{
    Task<CalculationResult> CalculateAllAsync(
        Guid periodId,
        Guid companyId,
        Dictionary<Guid, decimal> rawValues,
        CancellationToken cancellationToken);
}

public class CalculationResult
{
    public Guid PeriodId { get; set; }
    public Guid CompanyId { get; set; }
    public Dictionary<Guid, decimal> CalculatedValues { get; set; }
    public Dictionary<string, decimal> Kpis { get; set; }
    public List<ValidationError> ValidationErrors { get; set; }
    public List<string> CalculationAuditTrail { get; set; }
    public bool IsValid { get; set; }
}

public class CalculationEngine : ICalculationEngine
{
    private readonly IFormulaEvaluator _formulaEvaluator;
    private readonly CalculationDependencyResolver _dependencyResolver;
    private readonly SignRuleEngine _signRuleEngine;
    private readonly IKpiCalculator _kpiCalculator;
    private readonly AccountingEquationValidator _equationValidator;
    private readonly ILogger<CalculationEngine> _logger;

    public async Task<CalculationResult> CalculateAllAsync(
        Guid periodId,
        Guid companyId,
        Dictionary<Guid, decimal> rawValues,
        CancellationToken cancellationToken)
    {
        var result = new CalculationResult
        {
            PeriodId = periodId,
            CompanyId = companyId,
            CalculatedValues = new(),
            ValidationErrors = new(),
            CalculationAuditTrail = new()
        };

        try
        {
            _logger.LogInformation("Iniciando cálculos para período {PeriodId}, empresa {CompanyId}", periodId, companyId);

            // Etapa 1: Aplicar regras de sinal
            result.CalculationAuditTrail.Add("=== ETAPA 1: APLICAR REGRAS DE SINAL ===");
            var signedValues = ApplySignRules(rawValues);
            result.CalculatedValues = new Dictionary<Guid, decimal>(signedValues);

            // Etapa 2: Resolver dependências e ordem de cálculo
            result.CalculationAuditTrail.Add("=== ETAPA 2: RESOLVER ORDEM DE CÁLCULO ===");
            var formulas = await GetFormulasForAccountsAsync(signedValues.Keys.ToList());
            var dependencyGraph = _dependencyResolver.ResolveDependencies(formulas);

            if (dependencyGraph.HasCircularDependency)
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    Code = "CIRCULAR_DEPENDENCY",
                    Message = "Dependência circular detectada nas fórmulas"
                });
                result.IsValid = false;
                return result;
            }

            // Etapa 3: Calcular valores derivados
            result.CalculationAuditTrail.Add("=== ETAPA 3: CALCULAR VALORES DERIVADOS ===");
            foreach (var accountId in dependencyGraph.CalculationOrder)
            {
                var formula = formulas.FirstOrDefault(f => f.StandardAccountId == accountId);
                if (formula == null) continue;

                var formulaResult = await _formulaEvaluator.EvaluateAsync(
                    formula, result.CalculatedValues, cancellationToken);

                if (formulaResult.IsValid)
                {
                    result.CalculatedValues[accountId] = formulaResult.CalculatedValue;
                    result.CalculationAuditTrail.AddRange(formulaResult.CalculationSteps);
                }
                else
                {
                    result.ValidationErrors.Add(new ValidationError
                    {
                        Code = "FORMULA_ERROR",
                        Message = formulaResult.ErrorMessage,
                        AffectedElement = accountId.ToString()
                    });
                }
            }

            // Etapa 4: Calcular KPIs
            result.CalculationAuditTrail.Add("=== ETAPA 4: CALCULAR KPIs ===");
            var accountNameValues = ConvertGuidKeysToNames(result.CalculatedValues);
            var kpiResult = await _kpiCalculator.CalculateKpisAsync(
                periodId, companyId, accountNameValues, cancellationToken);

            result.Kpis = kpiResult.Kpis;
            result.CalculationAuditTrail.AddRange(kpiResult.CalculationAudit);

            // Etapa 5: Validar equações
            result.CalculationAuditTrail.Add("=== ETAPA 5: VALIDAR EQUAÇÕES CONTÁBEIS ===");
            var equationValidation = _equationValidator.ValidateBasicEquation(accountNameValues);
            result.CalculationAuditTrail.AddRange(equationValidation.ValidationSteps);

            if (!equationValidation.IsValid)
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    Code = "EQUATION_UNBALANCED",
                    Message = equationValidation.ErrorMessage
                });
            }

            result.IsValid = !result.ValidationErrors.Any();

            _logger.LogInformation(
                "Cálculos concluídos: {AccountCount} contas, {KpiCount} KPIs, Válido: {IsValid}",
                result.CalculatedValues.Count,
                result.Kpis.Count,
                result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro nos cálculos");
            result.IsValid = false;
            result.ValidationErrors.Add(new ValidationError
            {
                Code = "CALCULATION_ERROR",
                Message = ex.Message
            });
            return result;
        }
    }

    private Dictionary<Guid, decimal> ApplySignRules(Dictionary<Guid, decimal> rawValues)
    {
        // Implementar aplicação de regras de sinal
        return rawValues;
    }

    private async Task<List<FormulaDefinition>> GetFormulasForAccountsAsync(List<Guid> accountIds)
    {
        // Buscar fórmulas do banco de dados
        return new List<FormulaDefinition>();
    }

    private Dictionary<string, decimal> ConvertGuidKeysToNames(Dictionary<Guid, decimal> values)
    {
        // Converter Guids para nomes de contas
        return new Dictionary<string, decimal>();
    }
}
```

---

## 9. Critérios de Aceite

- ✅ Evalua fórmulas em ordem correta (resolvendo dependências)
- ✅ Aplica regras de sinal (Ativo +, Despesa -)
- ✅ Calcula subtotais e totais automaticamente
- ✅ Gera KPIs (EBITDA, Margens, Índices de Liquidez)
- ✅ Valida equação fundamental (Ativo = Passivo + PL)
- ✅ Detecta dependências circulares
- ✅ Retorna trail completo de auditoria
- ✅ Trata erros com graciosidade

---

## 10. Próximos Passos

1. Implementar parsers de fórmula
2. Criar biblioteca de KPIs reais
3. Validar com dados financeiros reais
4. Prosseguir com **05 - Motor de Consolidação**


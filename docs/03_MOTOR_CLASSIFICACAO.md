# 3. Especificação do Motor de Classificação

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** C#/ASP.NET Core, Claude API (LLM), Rule Engine

---

## 1. Visão Geral

O Motor de Classificação é o **núcleo inteligente** do sistema. Seu objetivo é mapear:

```
Conta de Origem → Tipo + Subtipo → Conta Padrão
```

Combinando **4 camadas de inteligência:**

1. **Regras Determinísticas** - Casos claros e conhecidos
2. **Contexto Hierárquico** - Posição na hierarquia do documento
3. **IA Generativa** - Análise semântica e contextual
4. **Decisões Históricas** - Aprendizado de escolhas anteriores

---

## 2. Arquitetura do Motor

```
┌─────────────────────────────────────────────────────┐
│         CONTA DE ORIGEM + CONTEXTO                 │
│  (nome, hierarquia, valores, período, empresa)     │
└────────────────┬────────────────────────────────────┘
                 │
        ┌────────▼────────┐
        │  Pré-filtros    │
        │  (quick reject) │
        └────────┬────────┘
                 │
    ┌────────────┼────────────┐
    │            │            │
    ▼            ▼            ▼
 Regras      Contexto      IA
Determis.   Hierárquico   Generativa
    │            │            │
    └────────────┼────────────┘
                 │
         ┌───────▼────────┐
         │ Candidatos     │
         │ Ranqueados     │
         └───────┬────────┘
                 │
         ┌───────▼────────────────┐
         │  Validação e Score     │
         │  (confiança, regras)   │
         └───────┬────────────────┘
                 │
    ┌────────────┴────────────────┐
    │                             │
    ▼                             ▼
 Confiança >         Confiança
 threshold?          baixa?
    │                 │
  SIM                NÃO
    │                 │
    ▼                 ▼
 Salva Auto      Human Review
 (Auditar)       (Fila)
```

---

## 3. Camada 1: Regras Determinísticas

### 3.1 Motor de Regras

```csharp
public interface IClassificationRule
{
    string RuleId { get; }
    int Priority { get; } // 1-100, maior = executado primeiro
    Task<ClassificationRuleResult> EvaluateAsync(
        ClassificationContext context,
        CancellationToken cancellationToken);
}

public class ClassificationRuleResult
{
    public bool Matches { get; set; }
    public Guid StandardAccountId { get; set; }
    public float Confidence { get; set; } // 0.0 - 1.0
    public string Reason { get; set; }
}

public class ClassificationContext
{
    // Conta de origem
    public string SourceAccountName { get; set; }
    public string NormalizedName { get; set; }
    
    // Hierarquia
    public string InferredType { get; set; }
    public string InferredSubtype { get; set; }
    public int HierarchyLevel { get; set; }
    public List<string> AncestorAccounts { get; set; }
    
    // Documento
    public Guid DocumentId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    
    // Valores
    public decimal? Value { get; set; }
    public int? ScaleFactor { get; set; }
    
    // Histórico
    public List<PreviousClassification> PreviousClassifications { get; set; }
}
```

### 3.2 Exemplos de Regras

```csharp
// Regra 1: Correspondência exata (normalizando nome)
public class ExactMatchRule : IClassificationRule
{
    private readonly IStandardAccountRepository _accountRepo;

    public string RuleId => "EXACT_MATCH";
    public int Priority => 90;

    public async Task<ClassificationRuleResult> EvaluateAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        var standardAccounts = await _accountRepo
            .GetByTypeAndSubtypeAsync(context.InferredType, context.InferredSubtype);

        var exactMatch = standardAccounts
            .FirstOrDefault(sa => 
                sa.Name.Equals(context.NormalizedName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            return new ClassificationRuleResult
            {
                Matches = true,
                StandardAccountId = exactMatch.Id,
                Confidence = 0.99f,
                Reason = "Correspondência exata com conta padrão"
            };
        }

        return new ClassificationRuleResult { Matches = false };
    }
}

// Regra 2: Correspondência por padrões conhecidos
public class PatternMatchRule : IClassificationRule
{
    private static readonly Dictionary<string, Guid> AccountPatterns = new()
    {
        { "CAIXA", Guid.Parse("...") }, // Caixa e Equivalentes
        { "BANCO", Guid.Parse("...") }, // Caixa e Equivalentes
        { "APLICAÇÃO", Guid.Parse("...") }, // Aplicações Financeiras
        { "FORNECEDOR", Guid.Parse("...") }, // Fornecedores
    };

    public string RuleId => "PATTERN_MATCH";
    public int Priority => 80;

    public async Task<ClassificationRuleResult> EvaluateAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        foreach (var pattern in AccountPatterns)
        {
            if (context.NormalizedName.Contains(pattern.Key))
            {
                return new ClassificationRuleResult
                {
                    Matches = true,
                    StandardAccountId = pattern.Value,
                    Confidence = 0.85f,
                    Reason = $"Correspondência com padrão: {pattern.Key}"
                };
            }
        }

        return new ClassificationRuleResult { Matches = false };
    }
}

// Regra 3: Histórico de empresa
public class CompanyHistoryRule : IClassificationRule
{
    private readonly IClassificationHistoryRepository _historyRepo;

    public string RuleId => "COMPANY_HISTORY";
    public int Priority => 75;

    public async Task<ClassificationRuleResult> EvaluateAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        // Se essa empresa já classificou essa conta antes, use a mesma classificação
        var previousClassification = await _historyRepo
            .GetMostRecentAsync(
                context.CompanyId,
                context.SourceAccountName);

        if (previousClassification != null &&
            previousClassification.Confidence >= 0.8f)
        {
            return new ClassificationRuleResult
            {
                Matches = true,
                StandardAccountId = previousClassification.StandardAccountId,
                Confidence = previousClassification.Confidence * 0.95f, // Reduz um pouco
                Reason = "Classificação anterior da empresa"
            };
        }

        return new ClassificationRuleResult { Matches = false };
    }
}

// Regra 4: Validação de Tipo/Subtipo
public class TypeSubtypeCompatibilityRule : IClassificationRule
{
    private readonly ICompatibilityRepository _compatibilityRepo;

    public string RuleId => "TYPE_SUBTYPE_COMPATIBILITY";
    public int Priority => 95; // Executar cedo

    public async Task<ClassificationRuleResult> EvaluateAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        // Rejeitar se Tipo/Subtipo incompatível
        var isCompatible = await _compatibilityRepo
            .IsCompatibleAsync(context.InferredType, context.InferredSubtype);

        if (!isCompatible)
        {
            return new ClassificationRuleResult
            {
                Matches = false, // Este é um pré-filtro
                Reason = "Tipo/Subtipo incompatível"
            };
        }

        // Se compatível, retorna null para continuar com outras regras
        return new ClassificationRuleResult { Matches = false };
    }
}
```

### 3.3 Orquestração de Regras

```csharp
public interface IRuleOrchestrator
{
    Task<List<ClassificationRuleResult>> EvaluateAllRulesAsync(
        ClassificationContext context,
        CancellationToken cancellationToken);
}

public class RuleOrchestrator : IRuleOrchestrator
{
    private readonly IEnumerable<IClassificationRule> _rules;
    private readonly ILogger<RuleOrchestrator> _logger;

    public RuleOrchestrator(
        IEnumerable<IClassificationRule> rules,
        ILogger<RuleOrchestrator> logger)
    {
        _rules = rules;
        _logger = logger;
    }

    public async Task<List<ClassificationRuleResult>> EvaluateAllRulesAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<ClassificationRuleResult>();

        // Executar regras em ordem de prioridade
        var orderedRules = _rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        foreach (var rule in orderedRules)
        {
            _logger.LogDebug("Avaliando regra: {RuleId}", rule.RuleId);

            var result = await rule.EvaluateAsync(context, cancellationToken);

            if (result.Matches)
            {
                _logger.LogInformation(
                    "Regra {RuleId} passou com confiança {Confidence}",
                    rule.RuleId,
                    result.Confidence);

                results.Add(result);
            }
        }

        return results;
    }
}
```

---

## 4. Camada 2: Análise de Contexto Hierárquico

```csharp
public class HierarchicContextAnalyzer
{
    public class HierarchicContextScore
    {
        public Guid StandardAccountId { get; set; }
        public float Score { get; set; }
        public string Reason { get; set; }
    }

    public async Task<HierarchicContextScore> AnalyzeContextAsync(
        ClassificationContext context,
        List<Guid> candidateStandardAccountIds)
    {
        float bestScore = 0f;
        Guid bestAccountId = Guid.Empty;
        string bestReason = "";

        // Estratégia 1: Parent/Child relationships
        var hierarchyScore = await EvaluateHierarchyAsync(
            context, candidateStandardAccountIds);

        if (hierarchyScore.Score > bestScore)
        {
            bestScore = hierarchyScore.Score;
            bestAccountId = hierarchyScore.StandardAccountId;
            bestReason = hierarchyScore.Reason;
        }

        return new HierarchicContextScore
        {
            StandardAccountId = bestAccountId,
            Score = bestScore,
            Reason = bestReason
        };
    }

    private async Task<HierarchicContextScore> EvaluateHierarchyAsync(
        ClassificationContext context,
        List<Guid> candidates)
    {
        // Se conta está dentro de "Caixa e Equivalentes"
        // E todas as contas irmãs foram classificadas como "Caixa e Equivalentes"
        // Aumenta confiança

        // Implementação: análise de árvore hierárquica
        return new HierarchicContextScore();
    }
}
```

---

## 5. Camada 3: Integração com IA (Claude API)

### 5.1 Análise Semântica com LLM

```csharp
public interface IAiClassificationService
{
    Task<AiClassificationResult> ClassifyWithAiAsync(
        ClassificationContext context,
        List<StandardAccount> standardAccountCandidates,
        CancellationToken cancellationToken);
}

public class AiClassificationService : IAiClassificationService
{
    private readonly IAnthropicClient _client;
    private readonly ILogger<AiClassificationService> _logger;

    public AiClassificationService(
        IAnthropicClient client,
        ILogger<AiClassificationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<AiClassificationResult> ClassifyWithAiAsync(
        ClassificationContext context,
        List<StandardAccount> standardAccountCandidates,
        CancellationToken cancellationToken)
    {
        var prompt = BuildClassificationPrompt(context, standardAccountCandidates);

        _logger.LogDebug("Enviando para análise de IA: {SourceAccount}", context.SourceAccountName);

        var response = await _client.Messages.CreateAsync(
            new CreateMessageRequest
            {
                Model = "claude-opus-4-1",
                MaxTokens = 500,
                Messages = new[]
                {
                    new Message
                    {
                        Role = "user",
                        Content = prompt
                    }
                }
            },
            cancellationToken);

        return ParseAiResponse(response, standardAccountCandidates);
    }

    private string BuildClassificationPrompt(
        ClassificationContext context,
        List<StandardAccount> candidates)
    {
        var candidatesList = string.Join("\n", candidates.Select((c, i) =>
            $"{i + 1}. {c.Code}: {c.Name} ({c.Description})"));

        return $"""
            Você é um especialista em contabilidade. Classifique a seguinte conta contábil.

            INFORMAÇÕES DA CONTA:
            - Nome Original: {context.SourceAccountName}
            - Tipo Inferido: {context.InferredType}
            - Subtipo Inferido: {context.InferredSubtype}
            - Nível Hierárquico: {context.HierarchyLevel}
            - Ancestrais: {string.Join(", ", context.AncestorAccounts)}
            - Valor: {context.Value} (Escala: {context.ScaleFactor}x)

            CONTAS PADRÃO CANDIDATAS:
            {candidatesList}

            INSTRUÇÕES:
            1. Analise semântica da conta
            2. Considere o contexto e a hierarquia
            3. Escolha o melhor candidato
            4. Justifique sua escolha
            5. Indique seu nível de confiança (0.0 - 1.0)

            RESPONDA EM JSON:
            {{
                "selected_index": <número 1-N>,
                "confidence": <0.0 - 1.0>,
                "reasoning": "<explicação>",
                "alternative_options": [<índices alternativos>]
            }}
            """;
    }

    private AiClassificationResult ParseAiResponse(
        CreateMessageResponse response,
        List<StandardAccount> candidates)
    {
        var content = response.Content.FirstOrDefault()?
            .Text ?? "";

        // Extrair JSON da resposta
        var jsonMatch = System.Text.RegularExpressions.Regex
            .Match(content, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);

        if (!jsonMatch.Success)
        {
            throw new InvalidOperationException("Falha ao extrair JSON da resposta de IA");
        }

        var parsed = System.Text.Json.JsonDocument.Parse(jsonMatch.Value);
        var root = parsed.RootElement;

        var selectedIndex = root.GetProperty("selected_index").GetInt32() - 1;
        var confidence = root.GetProperty("confidence").GetSingle();
        var reasoning = root.GetProperty("reasoning").GetString();

        return new AiClassificationResult
        {
            StandardAccountId = candidates[selectedIndex].Id,
            Confidence = confidence,
            Reasoning = reasoning,
            IsAiGenerated = true
        };
    }
}

public class AiClassificationResult
{
    public Guid StandardAccountId { get; set; }
    public float Confidence { get; set; }
    public string Reasoning { get; set; }
    public bool IsAiGenerated { get; set; }
}
```

---

## 6. Camada 4: Aprendizado de Decisões Anteriores

```csharp
public class PreviousDecisionAnalyzer
{
    private readonly IAnalystDecisionRepository _decisionRepo;

    public async Task<PreviousDecisionScore> AnalyzePreviousDecisionsAsync(
        ClassificationContext context,
        List<Guid> candidateIds)
    {
        // Buscar decisões anteriores similares
        var similarDecisions = await _decisionRepo
            .FindSimilarAsync(
                context.SourceAccountName,
                context.CompanyId,
                context.InferredType,
                context.InferredSubtype);

        var scores = new Dictionary<Guid, float>();

        foreach (var decision in similarDecisions)
        {
            var similarity = CalculateSimilarity(context.SourceAccountName, decision.SourceAccountName);

            if (!scores.ContainsKey(decision.StandardAccountId))
            {
                scores[decision.StandardAccountId] = 0f;
            }

            scores[decision.StandardAccountId] += similarity * decision.ConfidenceFeedback / 5f;
        }

        if (scores.Any())
        {
            var bestChoice = scores.OrderByDescending(s => s.Value).First();
            return new PreviousDecisionScore
            {
                StandardAccountId = bestChoice.Key,
                Score = bestChoice.Value,
                DecisionsUsed = similarDecisions.Count
            };
        }

        return new PreviousDecisionScore { Score = 0f };
    }

    private float CalculateSimilarity(string s1, string s2)
    {
        // Jaro-Winkler ou similar
        return 0f;
    }
}

public class PreviousDecisionScore
{
    public Guid StandardAccountId { get; set; }
    public float Score { get; set; }
    public int DecisionsUsed { get; set; }
}
```

---

## 7. Orquestração Final: Classificador Principal

```csharp
public interface IAccountClassifier
{
    Task<ClassificationResult> ClassifyAsync(
        ClassificationContext context,
        CancellationToken cancellationToken);
}

public class AccountClassifier : IAccountClassifier
{
    private readonly IRuleOrchestrator _ruleOrchestrator;
    private readonly HierarchicContextAnalyzer _hierarchicAnalyzer;
    private readonly IAiClassificationService _aiService;
    private readonly PreviousDecisionAnalyzer _decisionAnalyzer;
    private readonly IStandardAccountRepository _accountRepo;
    private readonly ILogger<AccountClassifier> _logger;
    private readonly ClassificationSettings _settings;

    public AccountClassifier(
        IRuleOrchestrator ruleOrchestrator,
        HierarchicContextAnalyzer hierarchicAnalyzer,
        IAiClassificationService aiService,
        PreviousDecisionAnalyzer decisionAnalyzer,
        IStandardAccountRepository accountRepo,
        ILogger<AccountClassifier> logger,
        ClassificationSettings settings)
    {
        _ruleOrchestrator = ruleOrchestrator;
        _hierarchicAnalyzer = hierarchicAnalyzer;
        _aiService = aiService;
        _decisionAnalyzer = decisionAnalyzer;
        _accountRepo = accountRepo;
        _logger = logger;
        _settings = settings;
    }

    public async Task<ClassificationResult> ClassifyAsync(
        ClassificationContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando classificação: {SourceAccount}", context.SourceAccountName);

        var result = new ClassificationResult();

        try
        {
            // CAMADA 1: Regras determinísticas
            var ruleResults = await _ruleOrchestrator
                .EvaluateAllRulesAsync(context, cancellationToken);

            if (ruleResults.Any(r => r.Confidence >= _settings.HighConfidenceThreshold))
            {
                var bestRule = ruleResults
                    .OrderByDescending(r => r.Confidence)
                    .First();

                result.StandardAccountId = bestRule.StandardAccountId;
                result.Confidence = bestRule.Confidence;
                result.ClassificationMethod = "RULE_BASED";
                result.Evidence.Add($"Regra: {bestRule.Reason}");

                _logger.LogInformation(
                    "Classificada por regra com confiança {Confidence}",
                    result.Confidence);

                return result;
            }

            // CAMADA 2: Contexto hierárquico
            var candidates = await _accountRepo
                .GetByTypeAndSubtypeAsync(context.InferredType, context.InferredSubtype);

            var hierarchicScore = await _hierarchicAnalyzer
                .AnalyzeContextAsync(context, candidates.Select(c => c.Id).ToList());

            if (hierarchicScore.Score >= _settings.MediumConfidenceThreshold)
            {
                result.StandardAccountId = hierarchicScore.StandardAccountId;
                result.Confidence = hierarchicScore.Score;
                result.ClassificationMethod = "HIERARCHIC_CONTEXT";
                result.Evidence.Add($"Contexto hierárquico: {hierarchicScore.Reason}");

                return result;
            }

            // CAMADA 3: Análise com IA
            var aiResult = await _aiService.ClassifyWithAiAsync(
                context, candidates, cancellationToken);

            if (aiResult.Confidence >= _settings.MediumConfidenceThreshold)
            {
                result.StandardAccountId = aiResult.StandardAccountId;
                result.Confidence = aiResult.Confidence;
                result.ClassificationMethod = "AI";
                result.Evidence.Add($"IA: {aiResult.Reasoning}");

                _logger.LogInformation(
                    "Classificada por IA com confiança {Confidence}",
                    result.Confidence);

                // Se confiança baixa (0.5-0.75), marcar para revisão
                if (aiResult.Confidence < 0.75f)
                {
                    result.NeedsHumanReview = true;
                    result.ReviewReason = "Confiança de IA moderada";
                }

                return result;
            }

            // CAMADA 4: Aprendizado histórico
            var decisionScore = await _decisionAnalyzer
                .AnalyzePreviousDecisionsAsync(context, candidates.Select(c => c.Id).ToList());

            if (decisionScore.Score >= _settings.MediumConfidenceThreshold)
            {
                result.StandardAccountId = decisionScore.StandardAccountId;
                result.Confidence = decisionScore.Score;
                result.ClassificationMethod = "HISTORICAL_DECISION";
                result.Evidence.Add($"Decisões anteriores: {decisionScore.DecisionsUsed} encontradas");

                return result;
            }

            // Nenhuma camada conseguiu classificar com confiança
            result.StandardAccountId = candidates.FirstOrDefault()?.Id ?? Guid.Empty;
            result.Confidence = 0.3f;
            result.ClassificationMethod = "UNKNOWN";
            result.NeedsHumanReview = true;
            result.ReviewReason = "Nenhuma camada classificou com confiança";

            _logger.LogWarning(
                "Classificação com baixa confiança para: {SourceAccount}",
                context.SourceAccountName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na classificação de {SourceAccount}", context.SourceAccountName);
            throw;
        }
    }
}

public class ClassificationResult
{
    public Guid StandardAccountId { get; set; }
    public float Confidence { get; set; } // 0.0 - 1.0
    public string ClassificationMethod { get; set; } // RULE_BASED, HIERARCHIC_CONTEXT, AI, HISTORICAL_DECISION
    public List<string> Evidence { get; set; } = new();
    public bool NeedsHumanReview { get; set; }
    public string ReviewReason { get; set; }
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}

public class ClassificationSettings
{
    public float HighConfidenceThreshold { get; set; } = 0.90f;
    public float MediumConfidenceThreshold { get; set; } = 0.70f;
    public float LowConfidenceThreshold { get; set; } = 0.50f;
    public bool UseAiClassification { get; set; } = true;
    public bool UseHistoricalDecisions { get; set; } = true;
}
```

---

## 8. Fila de Revisão Humana

```csharp
public class ReviewQueue
{
    public class ReviewQueueItem
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid SourceAccountId { get; set; }
        public Guid SuggestedStandardAccountId { get; set; }
        public string SourceAccountName { get; set; }
        public string SuggestedAccountName { get; set; }
        public float ConfidenceScore { get; set; }
        public List<string> Evidence { get; set; }
        public ReviewStatus Status { get; set; }
        public Guid ReviewedBy { get; set; }
        public string ReviewFeedback { get; set; }
        public Guid FinalStandardAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ReviewedAt { get; set; }
    }

    public enum ReviewStatus
    {
        Pending,
        Approved,
        Rejected,
        OverrideAccepted
    }
}
```

---

## 9. Testes Esperados

```csharp
public class AccountClassifierTests
{
    [Fact]
    public async Task ShouldClassifyExactMatch()
    {
        var context = new ClassificationContext
        {
            SourceAccountName = "Caixa e bancos",
            NormalizedName = "CAIXA E BANCOS",
            InferredType = "ATIVO",
            InferredSubtype = "CIRCULANTE"
        };

        var result = await _classifier.ClassifyAsync(context, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.StandardAccountId);
        Assert.True(result.Confidence >= 0.90f);
        Assert.Equal("RULE_BASED", result.ClassificationMethod);
    }

    [Fact]
    public async Task ShouldFlagLowConfidenceForReview()
    {
        var context = new ClassificationContext
        {
            SourceAccountName = "Conta misteriosa X",
            InferredType = "ATIVO",
            InferredSubtype = "CIRCULANTE"
        };

        var result = await _classifier.ClassifyAsync(context, CancellationToken.None);

        Assert.True(result.NeedsHumanReview);
        Assert.True(result.Confidence < 0.75f);
    }

    [Fact]
    public async Task ShouldUseHistoricalDecision()
    {
        // Se empresa já classificou "Caixa" como "Caixa e Equivalentes" antes
        // Deve usar a mesma classificação

        var result = await _classifier.ClassifyAsync(context, CancellationToken.None);

        Assert.Equal("HISTORICAL_DECISION", result.ClassificationMethod);
    }
}
```

---

## 10. Critérios de Aceite

- ✅ Classifica com regras determinísticas (confiança >90%)
- ✅ Utiliza contexto hierárquico para aumentar confiança
- ✅ Integra com Claude API para análise semântica
- ✅ Aprende com decisões anteriores
- ✅ Retorna score de confiança para cada classificação
- ✅ Marca para revisão humana quando confiança <70%
- ✅ Suporta override manual com feedback
- ✅ Combina múltiplas camadas de evidência

---

## 11. Próximos Passos

1. Implementar regras determinísticas específicas
2. Criar prompts de IA testados e validados
3. Integrar com banco de dados de decisões
4. Prosseguir com **04 - Motor de Cálculos**


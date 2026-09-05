# 6. Especificação do Human-in-the-loop e Aprendizado

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026

---

## 1. Visão Geral

O sistema **aprende continuamente** através de:

1. **Fila de Revisão** - Casos que precisam validação humana
2. **Decisões Humanas** - Análistas confirmam ou corrigem
3. **Aprendizado** - Sistema melhora com o tempo
4. **Feedback** - Usuário valida qualidade das sugestões

---

## 2. Fila de Revisão

### 2.1 Critérios para Envio à Fila

```csharp
public class ReviewQueueManager
{
    public class ReviewCriteria
    {
        public bool IsConfianceLow { get; set; } // < 70%
        public bool IsNewPattern { get; set; } // Conta nunca vista antes
        public bool IsAnomalous { get; set; } // Valor muito diferente do histórico
        public bool IsHighValue { get; set; } // Valor acima de threshold
        public bool IsHighRisk { get; set; } // Conta crítica (receita, lucro, etc)
        public bool ManuallyFlagged { get; set; } // Marcada manualmente
    }

    public async Task<List<ReviewQueueItem>> IdentifyItemsForReviewAsync(
        ClassificationResult classification,
        StandardAccount account,
        CancellationToken cancellationToken)
    {
        var criteria = new ReviewCriteria();
        var items = new List<ReviewQueueItem>();

        // Verificar cada critério
        criteria.IsConfianceLow = classification.Confidence < 0.70f;
        criteria.IsNewPattern = await IsNewPatternAsync(classification);
        criteria.IsAnomalous = await IsAnomalousAsync(classification);
        criteria.IsHighValue = classification.Value > 1_000_000;
        criteria.IsHighRisk = IsHighRiskAccount(account);

        // Se qualquer critério é verdadeiro, enviar para revisão
        if (criteria.IsConfianceLow || criteria.IsNewPattern || 
            criteria.IsAnomalous || criteria.IsHighValue || criteria.IsHighRisk)
        {
            items.Add(new ReviewQueueItem
            {
                ClassificationId = classification.Id,
                Priority = CalculatePriority(criteria),
                Reason = GenerateReviewReason(criteria)
            });
        }

        return items;
    }

    private int CalculatePriority(ReviewCriteria criteria)
    {
        int priority = 0;
        if (criteria.IsHighValue) priority += 40;
        if (criteria.IsHighRisk) priority += 30;
        if (criteria.IsConfianceLow) priority += 20;
        if (criteria.IsNewPattern) priority += 10;
        return priority;
    }

    private string GenerateReviewReason(ReviewCriteria criteria)
    {
        var reasons = new List<string>();
        if (criteria.IsConfianceLow) reasons.Add("Confiança baixa");
        if (criteria.IsNewPattern) reasons.Add("Padrão novo");
        if (criteria.IsAnomalous) reasons.Add("Valor anômalo");
        if (criteria.IsHighValue) reasons.Add("Valor alto");
        if (criteria.IsHighRisk) reasons.Add("Conta crítica");
        return string.Join(" | ", reasons);
    }
}

public class ReviewQueueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClassificationId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid SuggestedStandardAccountId { get; set; }
    
    public string SourceAccountName { get; set; }
    public string SuggestedAccountName { get; set; }
    public float ConfidenceScore { get; set; }
    public List<string> Evidence { get; set; }
    
    public int Priority { get; set; } // 1-100, maior = mais urgente
    public string Reason { get; set; }
    
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime AssignedAt { get; set; }
    public DateTime ReviewedAt { get; set; }
    
    public Guid AssignedTo { get; set; } // Usuário responsável
    public Guid ReviewedBy { get; set; } // Usuário que revisou
}

public enum ReviewStatus
{
    Pending,           // Aguardando revisão
    Assigned,          // Atribuído a um analista
    InProgress,        // Sendo revisado
    Approved,          // Aprovado (classificação está correta)
    OverrideAccepted,  // Override aceito (classificação foi corrigida)
    OverrideRejected,  // Override rejeitado (classificação original estava correta)
    Skipped            // Pulado/ignorado
}
```

---

## 3. Registro de Decisões

```csharp
public class AnalystDecisionRecorder
{
    public class AnalystDecision
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReviewQueueItemId { get; set; }
        public Guid AnalystId { get; set; }
        
        public DecisionType DecisionType { get; set; } // APPROVE, OVERRIDE, REJECT
        public Guid ChosenStandardAccountId { get; set; }
        public string DecisionReason { get; set; }
        public int ConfidenceFeedback { get; set; } // 1-5
        
        // Contexto
        public Guid SourceAccountId { get; set; }
        public string SourceAccountName { get; set; }
        public Guid CompanyId { get; set; }
        public Guid PeriodId { get; set; }
        public decimal Value { get; set; }
        
        // Metadados
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ReviewDuration { get; set; }
        
        // Rastreabilidade
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public enum DecisionType
    {
        Approve,              // Classificação original estava correta
        Override,             // Analista escolheu conta diferente
        PartialOverride,      // Analista ajustou mas mantém mesma categoria
        Reject                // Rejeitou completamente
    }

    public async Task<AnalystDecision> RecordDecisionAsync(
        ReviewQueueItem item,
        Guid analystId,
        DecisionType decisionType,
        Guid chosenAccountId,
        string reason,
        int confidence,
        CancellationToken cancellationToken)
    {
        var decision = new AnalystDecision
        {
            ReviewQueueItemId = item.Id,
            AnalystId = analystId,
            DecisionType = decisionType,
            ChosenStandardAccountId = chosenAccountId,
            DecisionReason = reason,
            ConfidenceFeedback = confidence,
            
            SourceAccountId = item.SourceAccountId,
            SourceAccountName = item.SourceAccountName,
            CompanyId = item.DocumentId, // Referência da empresa
            Value = item.ConfidenceScore
        };

        // Persistir no banco
        await SaveDecisionAsync(decision, cancellationToken);

        return decision;
    }

    private Task SaveDecisionAsync(AnalystDecision decision, CancellationToken cancellationToken)
    {
        // Implementação
        return Task.CompletedTask;
    }
}
```

---

## 4. Sistema de Aprendizado

### 4.1 Aprendizado de Padrões

```csharp
public class PatternLearningEngine
{
    public class LearnedPattern
    {
        public Guid PatternId { get; set; } = Guid.NewGuid();
        public string PatternName { get; set; }
        public PatternType Type { get; set; }
        
        // Definição
        public string Condition { get; set; } // "Nome contém 'CAIXA' E Tipo = 'ATIVO'"
        public Guid SuggestedStandardAccountId { get; set; }
        
        // Validação
        public float Confidence { get; set; }
        public int TimesUsed { get; set; }
        public int TimesValidated { get; set; }
        public int TimesRejected { get; set; }
        
        // Controle
        public bool IsActive { get; set; }
        public bool RequiresMaintenance { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
    }

    public enum PatternType
    {
        TextualMatch,          // Baseado em similaridade de texto
        TypeSubtypeMatch,      // Baseado em tipo/subtipo
        HistoricalMatch,       // Baseado em histórico da empresa
        HierarchicMatch,       // Baseado em hierarquia
        ValueRangeMatch        // Baseado em faixa de valores
    }

    public async Task<LearnedPattern> DerivePatternFromDecisionsAsync(
        List<AnalystDecisionRecorder.AnalystDecision> decisions,
        CancellationToken cancellationToken)
    {
        // Agrupar decisões similares
        var similarDecisions = decisions
            .GroupBy(d => new { d.SourceAccountName, d.ChosenStandardAccountId })
            .FirstOrDefault();

        if (similarDecisions == null || similarDecisions.Count() < 3)
            return null; // Padrão precisa de pelo menos 3 ocorrências

        // Análise: 100% de consistência?
        var totalDecisions = similarDecisions.Count();
        var approvals = similarDecisions.Count(d => d.DecisionType == AnalystDecisionRecorder.DecisionType.Approve);
        var consistency = (float)approvals / totalDecisions;

        if (consistency < 0.8f)
            return null; // Padrão não é consistente o suficiente

        var pattern = new LearnedPattern
        {
            PatternName = $"Pattern_{similarDecisions.Key.SourceAccountName}_{DateTime.UtcNow:yyyyMMdd}",
            Type = PatternType.TextualMatch,
            Condition = $"Nome = '{similarDecisions.Key.SourceAccountName}'",
            SuggestedStandardAccountId = similarDecisions.Key.ChosenStandardAccountId,
            Confidence = consistency,
            IsActive = true
        };

        await SavePatternAsync(pattern, cancellationToken);

        return pattern;
    }

    private Task SavePatternAsync(LearnedPattern pattern, CancellationToken cancellationToken)
    {
        // Implementação
        return Task.CompletedTask;
    }
}
```

### 4.2 Aprendizado de Regras

```csharp
public class RuleLearningEngine
{
    public class LearnedRule
    {
        public Guid RuleId { get; set; } = Guid.NewGuid();
        public string RuleName { get; set; }
        
        // Definição
        public string RuleCondition { get; set; } // JSON com lógica
        public string RuleAction { get; set; } // JSON com ação
        
        // Origem
        public int DerivedFromDecisionsCount { get; set; }
        public Guid SuggestedByAiModel { get; set; }
        public Guid ApprovedBy { get; set; }
        
        // Validação
        public float Accuracy { get; set; }
        public int ApplicationCount { get; set; }
        
        // Controle
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }

    public async Task<LearnedRule> DeriveRuleFromAnalystDecisionsAsync(
        List<AnalystDecisionRecorder.AnalystDecision> decisions,
        CancellationToken cancellationToken)
    {
        // Exemplo: Se todas as decisões de "Conta X" para tipo "ATIVO" → "Caixa"
        // Criar regra: "IF (SourceAccount = 'Conta X' AND Type = 'ATIVO') THEN StandardAccount = 'Caixa'"

        var distinctAccounts = decisions
            .Select(d => d.SourceAccountName)
            .Distinct()
            .ToList();

        if (distinctAccounts.Count > 1)
            return null; // Regra seria muito específica

        var targetAccounts = decisions
            .Select(d => d.ChosenStandardAccountId)
            .Distinct()
            .ToList();

        if (targetAccounts.Count > 1)
            return null; // Decisões não convergem

        var rule = new LearnedRule
        {
            RuleName = $"Rule_{distinctAccounts[0]}_{DateTime.UtcNow:yyyyMMdd}",
            RuleCondition = System.Text.Json.JsonSerializer.Serialize(new
            {
                SourceAccountName = distinctAccounts[0]
            }),
            RuleAction = System.Text.Json.JsonSerializer.Serialize(new
            {
                StandardAccountId = targetAccounts[0]
            }),
            DerivedFromDecisionsCount = decisions.Count,
            Accuracy = 0.95f, // 100% das decisões foram para mesma conta
            IsActive = true
        };

        await SaveRuleAsync(rule, cancellationToken);

        return rule;
    }

    private Task SaveRuleAsync(LearnedRule rule, CancellationToken cancellationToken)
    {
        // Implementação
        return Task.CompletedTask;
    }
}
```

---

## 5. Sistema de Feedback e Qualidade

```csharp
public class FeedbackCollector
{
    public class ClassificationFeedback
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClassificationId { get; set; }
        public Guid UserId { get; set; }
        
        public FeedbackType Type { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public string Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FeedbackType
    {
        CorrectClassification,       // Classificação estava correta
        IncorrectClassification,     // Estava errada
        NeedsReview,                 // Precisa de revisão humana
        VeryConfident,               // Muito confiante
        Confusing                    // Confuso/ambíguo
    }

    public async Task<QualityMetrics> CalculateQualityMetricsAsync(
        Guid periodId,
        CancellationToken cancellationToken)
    {
        var classifications = await GetClassificationsForPeriodAsync(periodId);
        var feedback = await GetFeedbackForClassificationsAsync(classifications);

        var metrics = new QualityMetrics
        {
            TotalClassifications = classifications.Count,
            CorrectClassifications = feedback.Count(f => f.Type == FeedbackType.CorrectClassification),
            IncorrectClassifications = feedback.Count(f => f.Type == FeedbackType.IncorrectClassification),
            AverageConfidence = classifications.Average(c => c.ConfidenceScore),
            AccuracyRate = (float)feedback.Count(f => f.Type == FeedbackType.CorrectClassification) / 
                          feedback.Count
        };

        return metrics;
    }

    private Task<List<Classification>> GetClassificationsForPeriodAsync(Guid periodId)
    {
        return Task.FromResult(new List<Classification>());
    }

    private Task<List<ClassificationFeedback>> GetFeedbackForClassificationsAsync(
        List<Classification> classifications)
    {
        return Task.FromResult(new List<ClassificationFeedback>());
    }
}

public class QualityMetrics
{
    public int TotalClassifications { get; set; }
    public int CorrectClassifications { get; set; }
    public int IncorrectClassifications { get; set; }
    public float AverageConfidence { get; set; }
    public float AccuracyRate { get; set; }
}
```

---

## 6. Dashboard de Aprendizado

```csharp
public class LearningDashboard
{
    public class LearningStats
    {
        public int TotalDecisionsMade { get; set; }
        public int PatternsLearned { get; set; }
        public int RulesCreated { get; set; }
        public float SystemAccuracyImprovement { get; set; }
        
        public List<TopMisclassifications> TopMisclassifications { get; set; }
        public List<MostCommonPatterns> MostCommonPatterns { get; set; }
        
        public float BaselineAccuracy { get; set; }
        public float CurrentAccuracy { get; set; }
    }

    public class TopMisclassifications
    {
        public string AccountName { get; set; }
        public int MisclassificationCount { get; set; }
        public string MostCommonIncorrectClassification { get; set; }
    }

    public class MostCommonPatterns
    {
        public string PatternName { get; set; }
        public int UsageCount { get; set; }
        public float AccuracyRate { get; set; }
    }

    public async Task<LearningStats> GetLearningStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Compilar estatísticas de aprendizado
        var stats = new LearningStats();

        // Buscar decisões
        var decisions = await GetAnalystDecisionsAsync(tenantId);
        stats.TotalDecisionsMade = decisions.Count;

        // Buscar padrões aprendidos
        var patterns = await GetLearnedPatternsAsync(tenantId);
        stats.PatternsLearned = patterns.Count;

        // Buscar regras
        var rules = await GetLearnedRulesAsync(tenantId);
        stats.RulesCreated = rules.Count;

        // Calcular melhoria
        stats.BaselineAccuracy = await GetBaselineAccuracyAsync(tenantId);
        stats.CurrentAccuracy = await GetCurrentAccuracyAsync(tenantId);
        stats.SystemAccuracyImprovement = stats.CurrentAccuracy - stats.BaselineAccuracy;

        return stats;
    }

    private Task<List<AnalystDecisionRecorder.AnalystDecision>> GetAnalystDecisionsAsync(Guid tenantId)
    {
        return Task.FromResult(new List<AnalystDecisionRecorder.AnalystDecision>());
    }

    private Task<List<PatternLearningEngine.LearnedPattern>> GetLearnedPatternsAsync(Guid tenantId)
    {
        return Task.FromResult(new List<PatternLearningEngine.LearnedPattern>());
    }

    private Task<List<RuleLearningEngine.LearnedRule>> GetLearnedRulesAsync(Guid tenantId)
    {
        return Task.FromResult(new List<RuleLearningEngine.LearnedRule>());
    }

    private Task<float> GetBaselineAccuracyAsync(Guid tenantId)
    {
        return Task.FromResult(0.65f); // Exemplo
    }

    private Task<float> GetCurrentAccuracyAsync(Guid tenantId)
    {
        return Task.FromResult(0.88f); // Exemplo
    }
}
```

---

## 7. Critérios de Aceite

- ✅ Sistema identifica itens para revisão corretamente
- ✅ Interface de revisão intuitiva e eficiente
- ✅ Decisões são registradas com auditoria completa
- ✅ Padrões são extraídos de decisões repetidas
- ✅ Regras são derivadas automaticamente
- ✅ Feedback melhora a qualidade do sistema
- ✅ Dashboard mostra progresso de aprendizado
- ✅ Sistema melhora com o tempo (accuracy aumenta)

---

## 8. Próximos Passos

1. Implementar UI de revisão
2. Criar algoritmos de derivação de padrões
3. Implementar feedback loop
4. Prosseguir com **07 - Especificação das APIs**


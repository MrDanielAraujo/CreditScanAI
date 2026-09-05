# 2. Especificação do Pipeline de PDF

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** C#/ASP.NET Core, PdfSharp/iTextSharp, NLP libraries

---

## 1. Visão Geral

O pipeline transforma um PDF financeiro bruto em uma estrutura interpretada, pronta para classificação. Não é simplesmente extração de texto — é **interpretação inteligente** de layout, hierarquia, períodos e entidades.

```
PDF Nativo 
    ↓ [Extração de Texto e Tabelas]
Texto Estruturado + Tabelas
    ↓ [Análise de Layout]
Estrutura Hierárquica Identificada
    ↓ [Detecção de Períodos/Entidades]
Períodos e Entidades Detectados
    ↓ [Normalização]
Dados Normalizados Prontos para Classificação
```

---

## 2. Estágio 1: Extração Básica

### 2.1 Extração de Texto

**Tecnologia:** iTextSharp ou PdfSharp

**Entrada:** PDF nativo

**Saída:** Texto completo com metadados de posição

```csharp
public class PdfTextExtraction
{
    public class ExtractedText
    {
        public string FullText { get; set; }
        public List<TextBlock> TextBlocks { get; set; }
        public List<Table> TablesDetected { get; set; }
        public int TotalPages { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }

    public class TextBlock
    {
        public int PageNumber { get; set; }
        public string Text { get; set; }
        public float X { get; set; } // Posição horizontal
        public float Y { get; set; } // Posição vertical
        public float FontSize { get; set; }
        public bool IsBold { get; set; }
        public float Confidence { get; set; } // OCR confidence se necessário
    }

    public ExtractedText ExtractText(byte[] pdfBytes)
    {
        // Implementação com iTextSharp
        // Retorna texto + metadados de posição
    }
}
```

### 2.2 Detecção de Tabelas

**Tecnologia:** Tabula.NET ou equivalente

**Objetivo:** Identificar estruturas de tabela e extrair dados

```csharp
public class TableDetection
{
    public class DetectedTable
    {
        public int PageNumber { get; set; }
        public int TableIndex { get; set; }
        public List<TableRow> Rows { get; set; }
        public List<TableColumn> Columns { get; set; }
        public float Confidence { get; set; }
        public BoundingBox Position { get; set; }
    }

    public class TableRow
    {
        public int RowIndex { get; set; }
        public List<TableCell> Cells { get; set; }
        public float Height { get; set; }
    }

    public class TableCell
    {
        public int ColumnIndex { get; set; }
        public string Text { get; set; }
        public bool IsNumeric { get; set; }
        public decimal? NumericValue { get; set; }
        public int? IndentationLevel { get; set; }
    }

    public List<DetectedTable> DetectTablesInPdf(byte[] pdfBytes)
    {
        // Detecção automática de estruturas de tabela
        // Extração de células com valores
    }
}
```

---

## 3. Estágio 2: Análise de Layout e Hierarquia

### 3.1 Interpretação de Estrutura

O sistema deve **reconstruir a hierarquia** implícita no documento:

```
ATIVO                          → Tipo
  CIRCULANTE                   → Subtipo
    Caixa e bancos            → Conta (nível 1)
    Aplicações financeiras    → Conta (nível 1)
      Curto Prazo             → Conta (nível 2)
      Longo Prazo             → Conta (nível 2)
```

**Técnicas de detecção:**

1. **Indentação:** Espaços em branco à esquerda
2. **Font Size:** Texto maior = nível superior
3. **Bold/Italics:** Negrito = nível superior
4. **Padrões Conhecidos:** Palavras-chave (ATIVO, CIRCULANTE, etc)

```csharp
public class HierarchyDetection
{
    public class HierarchicalAccount
    {
        public int Level { get; set; } // 0 = Tipo, 1 = Subtipo, 2+ = Contas
        public string OriginalName { get; set; }
        public string NormalizedName { get; set; }
        public int IndentationLevel { get; set; }
        public float FontSize { get; set; }
        public bool IsBold { get; set; }
        public int LineNumber { get; set; }
        public List<HierarchicalAccount> Children { get; set; }
        public HierarchicalAccount Parent { get; set; }
        
        // Inferência de tipo/subtipo
        public string InferredType { get; set; } // ATIVO, PASSIVO, DRE
        public string InferredSubtype { get; set; } // CIRCULANTE, NAO_CIRCULANTE, etc
    }

    public List<HierarchicalAccount> BuildHierarchy(
        List<TextBlock> textBlocks,
        List<DetectedTable> tables)
    {
        var accounts = new List<HierarchicalAccount>();
        
        // Algoritmo: conectar elementos based on indentation/formatting
        // Detectar transições de tipo/subtipo
        // Construir árvore hierárquica
        
        return accounts;
    }
}
```

### 3.2 Detectores Especializados de Tipo/Subtipo

```csharp
public class TypeSubtypeDetector
{
    private static readonly Dictionary<string, string> TypeKeywords = new()
    {
        { "ATIVO", "ATIVO" },
        { "PASSIVO", "PASSIVO" },
        { "PATRIMÔNIO LÍQUIDO", "PASSIVO" }, // Subtipo PL
        { "RECEITA", "DRE" },
        { "DESPESA", "DRE" }
    };

    private static readonly Dictionary<string, string> SubtypeKeywords = new()
    {
        { "CIRCULANTE", "CIRCULANTE" },
        { "CURTO PRAZO", "CIRCULANTE" },
        { "NÃO CIRCULANTE", "NAO_CIRCULANTE" },
        { "LONGO PRAZO", "NAO_CIRCULANTE" },
        { "PERMANENTE", "PERMANENTE" },
        { "PATRIMÔNIO LÍQUIDO", "PL" }
    };

    public TypeSubtypeInference InferTypeAndSubtype(
        string accountText,
        int indentationLevel,
        List<HierarchicalAccount> contextAccounts)
    {
        var result = new TypeSubtypeInference();

        // 1. Busca por palavras-chave
        var typeMatch = TypeKeywords
            .FirstOrDefault(kv => accountText.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
            .Value;

        var subtypeMatch = SubtypeKeywords
            .FirstOrDefault(kv => accountText.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
            .Value;

        // 2. Herança do contexto
        if (string.IsNullOrEmpty(typeMatch) && contextAccounts.Count > 0)
        {
            var parent = contextAccounts.LastOrDefault(a => a.Level < indentationLevel);
            typeMatch = parent?.InferredType ?? typeMatch;
        }

        // 3. Validação de compatibilidade
        result.Type = typeMatch;
        result.Subtype = subtypeMatch;
        result.Confidence = CalculateConfidence(typeMatch, subtypeMatch, accountText);

        return result;
    }

    private float CalculateConfidence(string type, string subtype, string text)
    {
        float confidence = 0.5f;
        
        // Aumenta confiança se encontrou palavra-chave exata
        if (TypeKeywords.ContainsKey(text.Trim())) confidence += 0.3f;
        if (SubtypeKeywords.ContainsKey(text.Trim())) confidence += 0.2f;
        
        return Math.Min(confidence, 1.0f);
    }
}
```

---

## 4. Estágio 3: Detecção de Períodos e Entidades

### 4.1 Detecção de Períodos

**Objetivo:** Identificar quais colunas correspondem a quais períodos

```csharp
public class PeriodDetection
{
    public class DetectedPeriod
    {
        public DateTime Date { get; set; }
        public string PeriodLabel { get; set; } // "31/12/2025", "Dez/2025"
        public int ColumnIndex { get; set; } // Em que coluna da tabela
        public float Confidence { get; set; }
    }

    public List<DetectedPeriod> DetectPeriods(List<DetectedTable> tables)
    {
        var periods = new List<DetectedPeriod>();

        foreach (var table in tables)
        {
            // Procurar na primeira linha (headers)
            var headers = table.Rows.FirstOrDefault()?.Cells ?? new List<TableCell>();

            foreach (var (cell, index) in headers.WithIndex())
            {
                var date = ParseDateFromText(cell.Text);
                if (date.HasValue)
                {
                    periods.Add(new DetectedPeriod
                    {
                        Date = date.Value,
                        PeriodLabel = cell.Text,
                        ColumnIndex = index,
                        Confidence = 0.95f
                    });
                }
            }
        }

        return periods.OrderByDescending(p => p.Confidence).ToList();
    }

    private DateTime? ParseDateFromText(string text)
    {
        // Suportar múltiplos formatos:
        // 31/12/2025, 31.12.2025, 31-12-2025, Dec/2025, Dezembro/2025
        
        var patterns = new[]
        {
            @"(\d{1,2})[/-.](\d{1,2})[/-.](\d{4})",  // DD/MM/YYYY
            @"(\w+)/(\d{4})" // Mês/Ano
        };

        // Implementar parsing com DateTime.TryParseExact
        return null;
    }
}
```

### 4.2 Detecção de Entidades/Empresas

```csharp
public class EntityDetection
{
    public class DetectedEntity
    {
        public string EntityName { get; set; }
        public string EntityCode { get; set; }
        public int ColumnIndex { get; set; }
        public float Confidence { get; set; }
        public string MappedToCompanyId { get; set; } // Mapping para banco de dados
    }

    public List<DetectedEntity> DetectEntities(
        List<DetectedTable> tables,
        List<TextBlock> textBlocks,
        List<DetectedPeriod> periods)
    {
        var entities = new List<DetectedEntity>();

        // Estratégia 1: Coluna dedicada para entidade
        foreach (var table in tables)
        {
            var headers = table.Rows.FirstOrDefault()?.Cells ?? new();
            
            // Se temos mais colunas que períodos detectados, pode haver coluna de entidade
            if (headers.Count > periods.Count + 1)
            {
                // Primeira coluna pode ser "Empresa" ou "Entidade"
                entities.Add(new DetectedEntity
                {
                    EntityName = "Padrão",
                    ColumnIndex = 0,
                    Confidence = 0.8f
                });
            }
        }

        // Estratégia 2: Procurar por cabeçalhos em áreas não-tabulares
        var headerCandidates = textBlocks
            .Where(tb => tb.PageNumber == 1 && tb.Y < 200) // Topo da primeira página
            .Select(tb => tb.Text.Trim())
            .ToList();

        // Fuzzy matching contra lista conhecida de empresas do tenant

        return entities;
    }
}
```

---

## 5. Estágio 4: Normalização e Limpeza

### 5.1 Normalização de Nomes

```csharp
public class AccountNameNormalization
{
    public string Normalize(string originalName)
    {
        if (string.IsNullOrEmpty(originalName))
            return string.Empty;

        var normalized = originalName
            .Trim()
            .ToUpper()
            // Remover acentos
            .RemoveDiacritics()
            // Padronizar espaçamento
            .RegexReplace(@"\s+", " ")
            // Remover caracteres especiais perigosos
            .RegexReplace(@"[^\w\s\-/]", "");

        return normalized;
    }

    public Dictionary<string, int> GetSimilarAccounts(
        string accountName,
        List<string> knownAccounts,
        float threshold = 0.85f)
    {
        // Usar Levenshtein distance ou Jaro-Winkler
        var similarities = new Dictionary<string, int>();

        foreach (var known in knownAccounts)
        {
            var similarity = CalculateSimilarity(accountName, known);
            if (similarity >= threshold)
            {
                similarities[known] = (int)(similarity * 100);
            }
        }

        return similarities.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
    }

    private float CalculateSimilarity(string s1, string s2)
    {
        // Implementar Jaro-Winkler ou similar
        return 0.0f;
    }
}
```

### 5.2 Tratamento de Valores Numéricos

```csharp
public class NumericValueNormalization
{
    public class NormalizedValue
    {
        public decimal Value { get; set; }
        public int ScaleFactor { get; set; } // 1, 1000, 1000000
        public bool IsNegative { get; set; }
        public float Confidence { get; set; }
    }

    public NormalizedValue NormalizeNumericValue(string text)
    {
        var result = new NormalizedValue { ScaleFactor = 1 };

        if (string.IsNullOrEmpty(text))
            return result;

        // Padrões: "1.234.567,89" ou "1,234,567.89"
        var cleaned = text
            .Trim()
            .Replace("(", "-")
            .Replace(")", "");

        // Detectar negativo
        result.IsNegative = cleaned.StartsWith("-");

        // Remover símbolos
        var numberOnly = Regex.Replace(cleaned, @"[^0-9\.,\-]", "");

        // Determinar separador decimal (ponto ou vírgula)
        var lastDot = numberOnly.LastIndexOf(".");
        var lastComma = numberOnly.LastIndexOf(",");
        char decimalSeparator = lastDot > lastComma ? '.' : ',';

        // Normalizar para formato System.Decimal
        var normalized = numberOnly
            .Replace(decimalSeparator == '.' ? ',' : '.', ' ')
            .Replace(decimalSeparator, '.')
            .Replace(" ", "");

        if (decimal.TryParse(normalized, out var value))
        {
            result.Value = Math.Abs(value);
            
            // Detectar escala
            result.ScaleFactor = DetectScaleFactor(value);
            
            result.Confidence = 0.95f;
        }

        return result;
    }

    private int DetectScaleFactor(decimal value)
    {
        // Se valor é muito grande, provavelmente está em milhares
        if (value > 1_000_000)
            return 1_000_000; // Milhões?
        if (value > 1_000)
            return 1_000; // Milhares?
        return 1;
    }
}
```

---

## 6. Estágio 5: Validação e Qualidade

### 6.1 Validação Estrutural

```csharp
public class PipelineValidation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public float OverallQualityScore { get; set; }
    }

    public class ValidationError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string AffectedElement { get; set; }
    }

    public ValidationResult ValidateExtractedData(ExtractedFinancialData data)
    {
        var result = new ValidationResult();

        // Validação 1: Tem períodos?
        if (!data.DetectedPeriods.Any())
        {
            result.Errors.Add(new ValidationError
            {
                Code = "NO_PERIODS",
                Message = "Nenhum período foi detectado no documento"
            });
        }

        // Validação 2: Tem contas?
        if (!data.HierarchicalAccounts.Any())
        {
            result.Errors.Add(new ValidationError
            {
                Code = "NO_ACCOUNTS",
                Message = "Nenhuma conta foi detectada no documento"
            });
        }

        // Validação 3: Todos valores numéricos têm período?
        var valuesWithoutPeriod = data.AccountValues
            .Where(v => string.IsNullOrEmpty(v.PeriodLabel))
            .Count();

        if (valuesWithoutPeriod > 0)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Code = "MISSING_PERIOD",
                Message = $"{valuesWithoutPeriod} valores sem período associado"
            });
        }

        result.IsValid = !result.Errors.Any();
        result.OverallQualityScore = CalculateQualityScore(data);

        return result;
    }

    private float CalculateQualityScore(ExtractedFinancialData data)
    {
        float score = 0.5f;

        // Aumenta score baseado em confiança média
        var avgConfidence = data.HierarchicalAccounts
            .Average(a => a.ConfidenceType ?? 0.5f);
        score += avgConfidence * 0.3f;

        // Aumenta se tem valores
        if (data.AccountValues.Any())
            score += 0.2f;

        return Math.Min(score, 1.0f);
    }
}
```

---

## 7. Estrutura de Saída do Pipeline

```csharp
public class ExtractedFinancialData
{
    public string DocumentId { get; set; }
    
    // Entrada original
    public byte[] OriginalPdfBytes { get; set; }
    public Dictionary<string, object> RawMetadata { get; set; }
    
    // Estágio 1: Texto bruto
    public string FullText { get; set; }
    public List<TextBlock> TextBlocks { get; set; }
    
    // Estágio 2: Hierarquia
    public List<HierarchicalAccount> HierarchicalAccounts { get; set; }
    
    // Estágio 3: Períodos e Entidades
    public List<DetectedPeriod> DetectedPeriods { get; set; }
    public List<DetectedEntity> DetectedEntities { get; set; }
    
    // Estágio 4: Valores normalizados
    public List<ExtractedAccountValue> AccountValues { get; set; }
    
    // Estágio 5: Validação
    public ValidationResult ValidationResult { get; set; }
    
    // Metadata
    public DateTime ExtractionTime { get; set; }
    public float OverallConfidence { get; set; }
}

public class ExtractedAccountValue
{
    public string SourceAccountName { get; set; }
    public string NormalizedName { get; set; }
    public string PeriodLabel { get; set; }
    public string EntityName { get; set; }
    public decimal Value { get; set; }
    public int ScaleFactor { get; set; }
    public string InferredType { get; set; }
    public string InferredSubtype { get; set; }
    public int HierarchyLevel { get; set; }
    public float Confidence { get; set; }
}
```

---

## 8. Orquestração do Pipeline

```csharp
public interface IPdfExtractionPipeline
{
    Task<ExtractedFinancialData> ProcessPdfAsync(
        byte[] pdfBytes,
        string documentId,
        CancellationToken cancellationToken);
}

public class PdfExtractionPipeline : IPdfExtractionPipeline
{
    private readonly ILogger<PdfExtractionPipeline> _logger;
    private readonly PdfTextExtraction _textExtraction;
    private readonly TableDetection _tableDetection;
    private readonly HierarchyDetection _hierarchyDetection;
    private readonly TypeSubtypeDetector _typeDetector;
    private readonly PeriodDetection _periodDetection;
    private readonly EntityDetection _entityDetection;
    private readonly AccountNameNormalization _normalization;
    private readonly NumericValueNormalization _valueNormalization;
    private readonly PipelineValidation _validation;

    public async Task<ExtractedFinancialData> ProcessPdfAsync(
        byte[] pdfBytes,
        string documentId,
        CancellationToken cancellationToken)
    {
        var result = new ExtractedFinancialData
        {
            DocumentId = documentId,
            OriginalPdfBytes = pdfBytes,
            ExtractionTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Iniciando extração do PDF {DocumentId}", documentId);

            // Estágio 1: Extração básica
            result.TextBlocks = await _textExtraction.ExtractTextAsync(pdfBytes);
            result.FullText = string.Join("\n", result.TextBlocks.Select(tb => tb.Text));
            
            var tables = await _tableDetection.DetectTablesAsync(pdfBytes);
            _logger.LogInformation("Encontradas {TableCount} tabelas", tables.Count);

            // Estágio 2: Hierarquia
            result.HierarchicalAccounts = _hierarchyDetection.BuildHierarchy(
                result.TextBlocks, tables);
            
            // Estágio 3: Períodos e Entidades
            result.DetectedPeriods = _periodDetection.DetectPeriods(tables);
            result.DetectedEntities = _entityDetection.DetectEntities(
                tables, result.TextBlocks, result.DetectedPeriods);

            // Estágio 4: Normalização
            foreach (var account in result.HierarchicalAccounts)
            {
                account.NormalizedName = _normalization.Normalize(account.OriginalName);
            }

            // Estágio 5: Validação
            result.ValidationResult = _validation.ValidateExtractedData(result);
            result.OverallConfidence = result.ValidationResult.OverallQualityScore;

            _logger.LogInformation(
                "Extração concluída: {AccountCount} contas, {PeriodCount} períodos",
                result.HierarchicalAccounts.Count,
                result.DetectedPeriods.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na extração do PDF {DocumentId}", documentId);
            throw;
        }
    }
}
```

---

## 9. Registração em Dependency Injection

```csharp
public static class PdfExtractionServiceRegistration
{
    public static IServiceCollection AddPdfExtraction(
        this IServiceCollection services)
    {
        services.AddScoped<PdfTextExtraction>();
        services.AddScoped<TableDetection>();
        services.AddScoped<HierarchyDetection>();
        services.AddScoped<TypeSubtypeDetector>();
        services.AddScoped<PeriodDetection>();
        services.AddScoped<EntityDetection>();
        services.AddScoped<AccountNameNormalization>();
        services.AddScoped<NumericValueNormalization>();
        services.AddScoped<PipelineValidation>();
        services.AddScoped<IPdfExtractionPipeline, PdfExtractionPipeline>();

        return services;
    }
}
```

---

## 10. Testes Esperados

```csharp
public class PdfExtractionPipelineTests
{
    [Fact]
    public async Task ShouldExtractTextFromNativePdf()
    {
        // Arrange
        var pdfBytes = File.ReadAllBytes("samples/balance_sheet_2025.pdf");
        
        // Act
        var result = await _pipeline.ProcessPdfAsync(pdfBytes, Guid.NewGuid().ToString(), CancellationToken.None);
        
        // Assert
        Assert.NotEmpty(result.TextBlocks);
        Assert.True(result.OverallConfidence > 0.7f);
    }

    [Fact]
    public void ShouldDetectHierarchyCorrectly()
    {
        // Hierarquia deve estar correta: Tipo > Subtipo > Contas
        Assert.True(result.HierarchicalAccounts[0].Level == 0); // Tipo
        Assert.True(result.HierarchicalAccounts[1].Level == 1); // Subtipo
    }

    [Fact]
    public void ShouldNormalizeNumericValuesCorrectly()
    {
        var normalized = _normalization.NormalizeNumericValue("1.234.567,89");
        Assert.Equal(1234567.89m, normalized.Value);
    }
}
```

---

## 11. Critérios de Aceite

- ✅ Extrai texto de PDFs nativos com >90% confiança
- ✅ Detecta tabelas e células automaticamente
- ✅ Reconstrói hierarquia (Tipo > Subtipo > Contas)
- ✅ Identifica períodos em múltiplos formatos
- ✅ Detecta entidades/empresas
- ✅ Normaliza nomes e valores numéricos
- ✅ Valida completude dos dados extraídos
- ✅ Retorna score de confiança para cada elemento

---

## 12. Próximos Passos

1. Implementar módulos de extração (iTextSharp, Tabula.NET)
2. Criar testes com PDFs reais
3. Implementar detecção de períodos com padrões reais
4. Prosseguir com **03 - Motor de Classificação**


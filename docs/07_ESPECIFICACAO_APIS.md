# 7. Especificação das APIs

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** ASP.NET Core, REST

---

## 1. Estrutura Base

### 1.1 Padrão de Response

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public ApiError Error { get; set; }
    public ApiMetadata Metadata { get; set; }
}

public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Details { get; set; }
}

public class ApiMetadata
{
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ApiVersion { get; set; }
}
```

### 1.2 Autenticação

Todas as APIs requerem **Bearer Token** (JWT):

```
Authorization: Bearer {token}
```

---

## 2. Endpoints de Documentos

### 2.1 Upload de Documento

```
POST /api/documents/upload
Content-Type: multipart/form-data

Request:
{
  "file": <arquivo PDF>,
  "document_type": "BALANCE_SHEET",
  "company_id": "uuid",
  "period_id": "uuid",
  "metadata": {
    "fiscal_year": 2025,
    "external_reference": "ref-123"
  }
}

Response 202 Accepted:
{
  "success": true,
  "data": {
    "document_id": "uuid",
    "upload_id": "uuid",
    "status": "QUEUED",
    "estimated_processing_time_seconds": 120
  }
}
```

### 2.2 Obter Status de Processamento

```
GET /api/documents/{document_id}/status

Response 200:
{
  "success": true,
  "data": {
    "document_id": "uuid",
    "status": "PROCESSING", // QUEUED, PROCESSING, COMPLETED, FAILED
    "progress_percentage": 45,
    "current_step": "CLASSIFICATION",
    "estimated_completion_time": "2025-01-15T14:30:00Z"
  }
}
```

### 2.3 Obter Resultado de Processamento

```
GET /api/documents/{document_id}/result

Response 200:
{
  "success": true,
  "data": {
    "document_id": "uuid",
    "extraction_status": "COMPLETED",
    "extracted_data": {
      "periods": ["2025-12-31", "2024-12-31"],
      "companies": ["Company A"],
      "account_count": 156,
      "table_count": 3,
      "confidence_score": 0.87
    },
    "classification_summary": {
      "classified_accounts": 156,
      "high_confidence": 142,
      "medium_confidence": 10,
      "low_confidence": 4,
      "pending_review": 4
    },
    "calculation_summary": {
      "total_assets": 1500000000,
      "total_liabilities": 800000000,
      "total_equity": 700000000,
      "equation_valid": true,
      "kpis_generated": 15
    }
  }
}
```

---

## 3. Endpoints de Classificação

### 3.1 Listar Contas para Classificação

```
GET /api/classifications/pending

Query Parameters:
- document_id (optional)
- company_id (optional)
- confidence_below (optional, default: 0.7)
- limit (default: 50)
- offset (default: 0)

Response 200:
{
  "success": true,
  "data": {
    "items": [
      {
        "classification_id": "uuid",
        "document_id": "uuid",
        "source_account": "Caixa e Bancos",
        "suggested_standard_account": "Caixa e Equivalentes de Caixa",
        "confidence": 0.65,
        "classification_method": "AI",
        "evidence": [
          "IA: Similaridade semântica com conta padrão",
          "Tipo: ATIVO",
          "Subtipo: CIRCULANTE"
        ]
      }
    ],
    "total_count": 4,
    "has_more": false
  }
}
```

### 3.2 Obter Detalhes de Classificação

```
GET /api/classifications/{classification_id}

Response 200:
{
  "success": true,
  "data": {
    "classification_id": "uuid",
    "source_account": {
      "id": "uuid",
      "original_name": "Caixa e bancos",
      "normalized_name": "CAIXA E BANCOS",
      "hierarchy_level": 2,
      "inferred_type": "ATIVO",
      "inferred_subtype": "CIRCULANTE"
    },
    "suggested_standard_account": {
      "id": "uuid",
      "code": "ATIVO_CIRCULANTE_CAIXA",
      "name": "Caixa e Equivalentes de Caixa",
      "description": "..."
    },
    "alternative_suggestions": [
      {
        "standard_account_id": "uuid",
        "name": "Outros Ativos Circulantes",
        "confidence": 0.35
      }
    ],
    "confidence_score": 0.65,
    "classification_method": "AI",
    "evidence": [...],
    "historical_decisions": [
      {
        "company": "Company A",
        "account": "Caixa e bancos",
        "classified_as": "Caixa e Equivalentes de Caixa",
        "decision_date": "2025-01-10"
      }
    ],
    "review_status": "PENDING",
    "created_at": "2025-01-15T10:00:00Z"
  }
}
```

### 3.3 Aprovar Classificação

```
POST /api/classifications/{classification_id}/approve

Request Body:
{
  "feedback": "Classificação correta",
  "confidence_rating": 5,
  "notes": "Padrão muito claro"
}

Response 200:
{
  "success": true,
  "data": {
    "classification_id": "uuid",
    "review_status": "APPROVED",
    "reviewed_at": "2025-01-15T14:30:00Z"
  }
}
```

### 3.4 Fazer Override na Classificação

```
POST /api/classifications/{classification_id}/override

Request Body:
{
  "new_standard_account_id": "uuid",
  "override_reason": "A empresa costuma classificar assim",
  "confidence_feedback": 5,
  "should_create_rule": true
}

Response 200:
{
  "success": true,
  "data": {
    "classification_id": "uuid",
    "review_status": "OVERRIDE_ACCEPTED",
    "new_standard_account": {
      "id": "uuid",
      "name": "..."
    },
    "rule_created": {
      "rule_id": "uuid",
      "rule_name": "Pattern_Caixa_...",
      "status": "PENDING_APPROVAL"
    }
  }
}
```

---

## 4. Endpoints de Cálculos

### 4.1 Obter Resultado de Cálculos

```
GET /api/calculations/{document_id}/{period_id}/{company_id}

Response 200:
{
  "success": true,
  "data": {
    "calculated_values": {
      "ATIVO_TOTAL": 1500000000,
      "ATIVO_CIRCULANTE": 800000000,
      "ATIVO_CIRCULANTE_CAIXA": 150000000,
      ...
    },
    "kpis": {
      "EBITDA": 250000000,
      "MARGEM_LIQUIDA": 0.15,
      "MARGEM_OPERACIONAL": 0.25,
      "LIQUIDEZ_CORRENTE": 2.5,
      "INDICE_ENDIVIDAMENTO": 1.14
    },
    "validation_results": [
      {
        "check_id": "BASIC_EQUATION",
        "passed": true,
        "message": "Ativo = Passivo + PL"
      }
    ],
    "calculation_audit_trail": [
      "=== ETAPA 1: APLICAR REGRAS DE SINAL ===",
      "Conta ATIVO_CIRCULANTE_CAIXA: +150000000"
    ],
    "is_valid": true
  }
}
```

### 4.2 Recalcular com Ajustes

```
POST /api/calculations/{document_id}/{period_id}/{company_id}/recalculate

Request Body:
{
  "adjustments": [
    {
      "standard_account_id": "uuid",
      "adjusted_value": 160000000,
      "reason": "Correção manual"
    }
  ],
  "formulas_to_update": [
    {
      "account_id": "uuid",
      "formula": "SUM(account_1, account_2, account_3)",
      "reason": "Fórmula corrigida"
    }
  ]
}

Response 200:
{
  "success": true,
  "data": {
    "recalculation_id": "uuid",
    "status": "COMPLETED",
    "new_calculated_values": {...},
    "variance_from_previous": {
      "ATIVO_TOTAL": -10000000,
      "ATIVO_CIRCULANTE": -10000000
    }
  }
}
```

---

## 5. Endpoints de Consolidação

### 5.1 Criar Consolidação

```
POST /api/consolidations

Request Body:
{
  "period_id": "uuid",
  "company_ids": ["uuid1", "uuid2", "uuid3"],
  "consolidation_method": "SIMPLE", // SIMPLE, WEIGHTED, PROPORTIONAL
  "eliminate_intercompany": true,
  "consolidation_name": "Consolidado Q4 2025"
}

Response 201:
{
  "success": true,
  "data": {
    "consolidation_id": "uuid",
    "status": "PROCESSING",
    "estimated_completion_time": "2025-01-15T14:50:00Z"
  }
}
```

### 5.2 Obter Resultado de Consolidação

```
GET /api/consolidations/{consolidation_id}

Response 200:
{
  "success": true,
  "data": {
    "consolidation_id": "uuid",
    "period": "2025-12-31",
    "companies_included": ["Company A", "Company B", "Company C"],
    "consolidation_method": "SIMPLE",
    "consolidated_statement": {
      "ATIVO_TOTAL": 4500000000,
      "ATIVO_CIRCULANTE": 2400000000,
      ...
    },
    "eliminations_applied": [
      {
        "description": "Eliminação transação entre A e B",
        "amount": 50000000,
        "accounts_affected": ["CONTAS_RECEBER", "CONTAS_PAGAR"]
      }
    ],
    "reconciliation_results": [
      {
        "check_id": "BASIC_EQUATION",
        "passed": true,
        "variance": 0
      },
      {
        "check_id": "SOURCE_TOTALS",
        "passed": true,
        "variance": 1000 // 1 real de tolerância
      }
    ],
    "is_valid": true,
    "consolidated_date": "2025-01-15T14:45:00Z"
  }
}
```

---

## 6. Endpoints de Human-in-the-loop

### 6.1 Listar Fila de Revisão

```
GET /api/review-queue

Query Parameters:
- priority_min (optional, 1-100)
- status (optional: PENDING, ASSIGNED, IN_PROGRESS)
- assigned_to_me (optional: true)
- limit (default: 20)
- offset (default: 0)

Response 200:
{
  "success": true,
  "data": {
    "items": [
      {
        "review_id": "uuid",
        "source_account": "Conta Misteriosa X",
        "suggested_account": "Outros Ativos Circulantes",
        "confidence": 0.35,
        "priority": 85,
        "reason": "Valor alto | Padrão novo",
        "created_at": "2025-01-15T10:00:00Z",
        "status": "PENDING"
      }
    ],
    "total_pending": 45,
    "total_high_priority": 12
  }
}
```

### 6.2 Buscar e Atribuir Item de Revisão

```
POST /api/review-queue/{review_id}/assign

Request Body:
{
  "assigned_to_id": "uuid",
  "priority": 90
}

Response 200:
{
  "success": true,
  "data": {
    "review_id": "uuid",
    "assigned_to": "analyst@example.com",
    "assigned_at": "2025-01-15T14:30:00Z"
  }
}
```

### 6.3 Submeter Decisão de Revisão

```
POST /api/review-queue/{review_id}/decision

Request Body:
{
  "decision_type": "APPROVE", // APPROVE, OVERRIDE, REJECT
  "chosen_account_id": "uuid",
  "reason": "Classificação correta, padrão claro",
  "confidence_rating": 5,
  "should_create_rule": true
}

Response 200:
{
  "success": true,
  "data": {
    "review_id": "uuid",
    "decision_recorded": {
      "decision_id": "uuid",
      "recorded_at": "2025-01-15T14:30:00Z"
    },
    "rule_created": {
      "rule_id": "uuid",
      "status": "ACTIVE"
    },
    "impact": {
      "similar_items_affected": 12,
      "estimated_accuracy_improvement": 0.02
    }
  }
}
```

---

## 7. Endpoints de Inteligência e Aprendizado

### 7.1 Obter Estatísticas de Aprendizado

```
GET /api/learning/stats

Query Parameters:
- period_days (optional, default: 30)

Response 200:
{
  "success": true,
  "data": {
    "total_decisions_made": 523,
    "patterns_learned": 28,
    "rules_created": 15,
    "baseline_accuracy": 0.65,
    "current_accuracy": 0.88,
    "accuracy_improvement": 0.23,
    "top_misclassifications": [
      {
        "account_name": "Outros Ativos",
        "misclassification_count": 5,
        "most_common_incorrect": "Outros Ativos Não Circulantes"
      }
    ],
    "trending_patterns": [
      {
        "pattern": "Nomes com 'Caixa'",
        "usage_count": 45,
        "accuracy": 0.96
      }
    ]
  }
}
```

### 7.2 Obter Regras Aprendidas

```
GET /api/learning/rules

Query Parameters:
- is_active (optional: true)
- min_accuracy (optional, default: 0.0)
- limit (default: 50)

Response 200:
{
  "success": true,
  "data": {
    "items": [
      {
        "rule_id": "uuid",
        "rule_name": "Rule_Caixa_Pattern",
        "accuracy": 0.98,
        "applications": 156,
        "created_at": "2025-01-10",
        "is_active": true,
        "conditions_summary": "SourceAccount contém 'CAIXA'"
      }
    ],
    "total_active_rules": 15,
    "total_rule_applications": 1240
  }
}
```

---

## 8. Endpoints de Relatórios

### 8.1 Gerar Demonstrativo Financeiro

```
POST /api/reports/financial-statement

Request Body:
{
  "period_id": "uuid",
  "company_ids": ["uuid1", "uuid2"], // Se vazio, usa todas
  "statement_type": "BALANCE_SHEET", // BALANCE_SHEET, INCOME_STATEMENT, BOTH
  "format": "JSON", // JSON, PDF, EXCEL
  "include_calculations_audit": true,
  "include_confidence_scores": true
}

Response 202:
{
  "success": true,
  "data": {
    "report_id": "uuid",
    "status": "GENERATING",
    "download_url": "https://api.example.com/reports/uuid/download",
    "estimated_completion_time": "2025-01-15T14:35:00Z"
  }
}
```

### 8.2 Obter Relatório de Qualidade

```
GET /api/reports/quality/{document_id}

Response 200:
{
  "success": true,
  "data": {
    "document_id": "uuid",
    "quality_score": 0.87,
    "extraction_quality": 0.95,
    "classification_quality": 0.82,
    "calculation_quality": 0.85,
    "validation_quality": 0.91,
    "recommendations": [
      "4 classificações com baixa confiança precisam revisão",
      "1 equação desbalanceada detectada",
      "2 padrões anomalosos identificados"
    ]
  }
}
```

---

## 9. Tratamento de Erros

### 9.1 Padrão de Erro

```json
{
  "success": false,
  "error": {
    "code": "INVALID_REQUEST",
    "message": "O documento não pôde ser processado",
    "details": {
      "file_format": ["Formato PDF inválido"],
      "file_size": ["Tamanho máximo 100MB excedido"]
    }
  }
}
```

### 9.2 Códigos de Erro Comuns

| Código | Status HTTP | Significado |
|--------|-------------|------------|
| `INVALID_REQUEST` | 400 | Requisição inválida |
| `UNAUTHORIZED` | 401 | Token ausente/inválido |
| `FORBIDDEN` | 403 | Sem permissão |
| `NOT_FOUND` | 404 | Recurso não existe |
| `CONFLICT` | 409 | Conflito (ex: duplicado) |
| `UNPROCESSABLE_ENTITY` | 422 | Validação falhou |
| `INTERNAL_ERROR` | 500 | Erro interno do servidor |
| `SERVICE_UNAVAILABLE` | 503 | Serviço temporariamente indisponível |

---

## 10. Rate Limiting

- **Limite**: 1000 requisições por hora por token
- **Header**: `X-RateLimit-Remaining: 999`

---

## 11. Paginação

Endpoints que retornam listas usam:

```
- limit: Quantidade de itens (padrão: 50, máximo: 500)
- offset: Deslocamento (padrão: 0)
- total_count: Total de itens disponíveis
- has_more: Indica se há mais itens
```

---

## 12. Versioning

API usa versioning na URL: `/api/v1/...`

Versão atual: **v1**

---

## 13. Critérios de Aceite

- ✅ Todos os endpoints implementados e testados
- ✅ Autenticação JWT funciona
- ✅ Rate limiting em vigor
- ✅ Tratamento de erros consistente
- ✅ Documentação Swagger/OpenAPI disponível
- ✅ Responses padronizadas
- ✅ Paginação funcionando

---

## 14. Próximos Passos

1. Gerar Swagger/OpenAPI
2. Implementar SDK de cliente (C# e JavaScript)
3. Prosseguir com **08 - Especificação do Frontend**


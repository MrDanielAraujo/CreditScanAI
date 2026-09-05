# 1. Modelo de Dados e Banco de Dados

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** PostgreSQL 15+, ASP.NET Core

---

## 1. Visão Geral do Modelo

O modelo de dados é construído em torno de três entidades centrais:

1. **Documentos e Metadados** - Origem e rastreabilidade
2. **Contas Contábeis** - Estrutura hierárquica e classificação
3. **Valores e Resultados** - Dados financeiros processados

A arquitetura permite **auditoria completa**, com histórico de todas as transformações desde o PDF até o resultado final.

---

## 2. Entidades Principais

### 2.1 Documentos e Upload

```sql
-- Tabela de documentos (PDFs carregados)
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    company_id UUID NOT NULL,
    document_type VARCHAR(50) NOT NULL, -- BALANCE_SHEET, INCOME_STATEMENT
    upload_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    file_name VARCHAR(255) NOT NULL,
    file_path TEXT NOT NULL, -- Caminho no Object Storage
    file_size BIGINT,
    file_hash VARCHAR(255), -- Para dedupliacação
    extraction_status VARCHAR(50), -- PENDING, PROCESSING, COMPLETED, FAILED
    extraction_started_at TIMESTAMP,
    extraction_completed_at TIMESTAMP,
    extraction_error TEXT,
    metadata JSONB, -- Dados brutos extraídos do PDF
    created_by UUID NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (company_id) REFERENCES companies(id),
    FOREIGN KEY (created_by) REFERENCES users(id)
);

CREATE INDEX idx_documents_tenant_company ON documents(tenant_id, company_id);
CREATE INDEX idx_documents_status ON documents(extraction_status);
CREATE INDEX idx_documents_type ON documents(document_type);
```

### 2.2 Entidades Organizacionais

```sql
-- Multitenancy
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    active BOOLEAN DEFAULT true
);

-- Empresas/Entidades
CREATE TABLE companies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    legal_name VARCHAR(255),
    cnpj VARCHAR(20),
    industry VARCHAR(100),
    fiscal_year_end TIMESTAMP,
    reporting_currency VARCHAR(3) DEFAULT 'BRL',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE(tenant_id, code)
);

-- Períodos (anos/trimestres)
CREATE TABLE periods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    period_type VARCHAR(50) NOT NULL, -- ANNUAL, QUARTERLY
    year INT NOT NULL,
    quarter INT, -- NULL para annual, 1-4 para quarterly
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_closed BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE(tenant_id, period_type, year, quarter)
);

-- Usuários
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    email VARCHAR(255) NOT NULL,
    name VARCHAR(255),
    role VARCHAR(50), -- ADMIN, ANALYST, VIEWER
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE(tenant_id, email)
);
```

### 2.3 Estrutura Contábil (Tipos e Subtipos)

```sql
-- Tipos de contas (ATIVO, PASSIVO, DRE)
CREATE TABLE account_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL, -- ATIVO, PASSIVO, DRE
    name VARCHAR(255) NOT NULL,
    description TEXT,
    sequence_order INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE(tenant_id, code)
);

-- Subtipos de contas (CIRCULANTE, NAO_CIRCULANTE, etc)
CREATE TABLE account_subtypes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    account_type_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL, -- CIRCULANTE, NAO_CIRCULANTE, etc
    name VARCHAR(255) NOT NULL,
    description TEXT,
    sequence_order INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (account_type_id) REFERENCES account_types(id),
    UNIQUE(tenant_id, code)
);

-- Compatibilidade entre Tipo e Subtipo
CREATE TABLE type_subtype_compatibility (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    account_type_id UUID NOT NULL,
    account_subtype_id UUID NOT NULL,
    is_allowed BOOLEAN DEFAULT true,
    sequence_order INT,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (account_type_id) REFERENCES account_types(id),
    FOREIGN KEY (account_subtype_id) REFERENCES account_subtypes(id),
    UNIQUE(tenant_id, account_type_id, account_subtype_id)
);
```

### 2.4 Contas Padrão (Layout Padronizado)

```sql
-- Contas padrão no layout unificado
CREATE TABLE standard_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    account_type_id UUID NOT NULL,
    account_subtype_id UUID NOT NULL,
    code VARCHAR(100) NOT NULL, -- Código único (ex: ATIVO_CIRCULANTE_CAIXA)
    name VARCHAR(255) NOT NULL,
    description TEXT,
    sequence_order INT,
    parent_id UUID, -- Para hierarquia
    is_summary BOOLEAN DEFAULT false, -- TRUE = conta de subtotal
    calculation_method VARCHAR(50), -- SUM, CUSTOM, etc
    calculation_formula TEXT, -- Fórmula para cálculos derivados
    expected_sign VARCHAR(1), -- '+' ou '-'
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (account_type_id) REFERENCES account_types(id),
    FOREIGN KEY (account_subtype_id) REFERENCES account_subtypes(id),
    FOREIGN KEY (parent_id) REFERENCES standard_accounts(id),
    UNIQUE(tenant_id, code)
);

CREATE INDEX idx_standard_accounts_type_subtype 
ON standard_accounts(tenant_id, account_type_id, account_subtype_id);
CREATE INDEX idx_standard_accounts_parent 
ON standard_accounts(parent_id);
```

### 2.5 Contas de Origem (Do Documento)

```sql
-- Contas como aparecem no documento original
CREATE TABLE source_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    document_id UUID NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    normalized_name VARCHAR(255),
    description TEXT,
    hierarchy_level INT, -- Nível de indentação no documento
    parent_source_account_id UUID, -- Relacionamento hierárquico
    inferred_type VARCHAR(50), -- ATIVO, PASSIVO, DRE (inferência)
    inferred_subtype VARCHAR(50), -- CIRCULANTE, NAO_CIRCULANTE, etc
    position_in_document INT, -- Linha/posição no PDF
    document_metadata JSONB, -- Metadados do PDF (fonte, página, etc)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (document_id) REFERENCES documents(id) ON DELETE CASCADE,
    FOREIGN KEY (parent_source_account_id) REFERENCES source_accounts(id),
    UNIQUE(document_id, original_name)
);

CREATE INDEX idx_source_accounts_document 
ON source_accounts(document_id);
```

### 2.6 Classificações (Mapeamento Source → Standard)

```sql
-- Mapeamento entre contas de origem e contas padrão
CREATE TABLE account_classifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    source_account_id UUID NOT NULL,
    standard_account_id UUID NOT NULL,
    document_id UUID NOT NULL,
    period_id UUID NOT NULL,
    company_id UUID NOT NULL,
    
    -- Pontuação e confiança
    confidence_score DECIMAL(3, 2), -- 0.00 a 1.00
    classification_method VARCHAR(50), -- RULE_BASED, AI, MANUAL, HYBRID
    
    -- Rastreamento de decisão
    created_by_type VARCHAR(50), -- SYSTEM, AI, ANALYST
    created_by_user_id UUID, -- Se ANALYST
    override_reason TEXT, -- Se foi override
    previous_classification_id UUID, -- Referência à classificação anterior
    
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reviewed_at TIMESTAMP,
    reviewed_by UUID,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (source_account_id) REFERENCES source_accounts(id),
    FOREIGN KEY (standard_account_id) REFERENCES standard_accounts(id),
    FOREIGN KEY (document_id) REFERENCES documents(id),
    FOREIGN KEY (period_id) REFERENCES periods(id),
    FOREIGN KEY (company_id) REFERENCES companies(id),
    FOREIGN KEY (created_by_user_id) REFERENCES users(id),
    FOREIGN KEY (reviewed_by) REFERENCES users(id),
    UNIQUE(document_id, source_account_id, period_id, company_id)
);

CREATE INDEX idx_classifications_confidence 
ON account_classifications(confidence_score DESC);
CREATE INDEX idx_classifications_review_needed 
ON account_classifications(reviewed_at) 
WHERE reviewed_at IS NULL;
```

### 2.7 Valores (Dados Financeiros)

```sql
-- Valores extraídos do documento
CREATE TABLE account_values (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    source_account_id UUID NOT NULL,
    standard_account_id UUID,
    document_id UUID NOT NULL,
    period_id UUID NOT NULL,
    company_id UUID NOT NULL,
    
    -- Valores em diferentes níveis de transformação
    raw_value DECIMAL(19, 4), -- Valor original do documento
    adjusted_value DECIMAL(19, 4), -- Após ajustes e normalizações
    final_value DECIMAL(19, 4), -- Após cálculos e consolidação
    
    -- Metadados
    currency VARCHAR(3) DEFAULT 'BRL',
    scale_factor INT DEFAULT 1000, -- Valores em milhares? milhões?
    value_type VARCHAR(50), -- BALANCE, FLOW, etc
    
    -- Rastreabilidade
    extraction_confidence DECIMAL(3, 2),
    calculation_applied TEXT, -- JSON com cálculos aplicados
    audit_trail JSONB, -- Histórico de transformações
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (source_account_id) REFERENCES source_accounts(id),
    FOREIGN KEY (standard_account_id) REFERENCES standard_accounts(id),
    FOREIGN KEY (document_id) REFERENCES documents(id),
    FOREIGN KEY (period_id) REFERENCES periods(id),
    FOREIGN KEY (company_id) REFERENCES companies(id),
    UNIQUE(document_id, source_account_id, period_id, company_id)
);

CREATE INDEX idx_account_values_period_company 
ON account_values(period_id, company_id);
CREATE INDEX idx_account_values_standard_account 
ON account_values(standard_account_id, period_id);
```

### 2.8 Validações e Reconciliação

```sql
-- Regras de validação (ex: Ativo = Passivo + PL)
CREATE TABLE validation_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    rule_name VARCHAR(255) NOT NULL,
    rule_type VARCHAR(50), -- EQUATION, COMPARISON, CUSTOM
    rule_expression TEXT, -- JSON com estrutura da regra
    description TEXT,
    severity VARCHAR(50), -- ERROR, WARNING, INFO
    is_active BOOLEAN DEFAULT true,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    UNIQUE(tenant_id, rule_name)
);

-- Resultados de validação
CREATE TABLE validation_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    document_id UUID NOT NULL,
    period_id UUID NOT NULL,
    company_id UUID NOT NULL,
    validation_rule_id UUID NOT NULL,
    
    is_valid BOOLEAN,
    error_message TEXT,
    expected_value DECIMAL(19, 4),
    actual_value DECIMAL(19, 4),
    variance DECIMAL(19, 4),
    variance_percentage DECIMAL(5, 2),
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (document_id) REFERENCES documents(id),
    FOREIGN KEY (period_id) REFERENCES periods(id),
    FOREIGN KEY (company_id) REFERENCES companies(id),
    FOREIGN KEY (validation_rule_id) REFERENCES validation_rules(id)
);

CREATE INDEX idx_validation_results_invalid 
ON validation_results(is_valid) WHERE NOT is_valid;
```

### 2.9 Consolidação

```sql
-- Dados consolidados por período/entidade
CREATE TABLE consolidated_statements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    period_id UUID NOT NULL,
    statement_type VARCHAR(50), -- BALANCE_SHEET, INCOME_STATEMENT
    
    -- Referências aos documentos originais
    document_ids UUID[], -- Array de UUIDs dos documentos consolidados
    companies_involved UUID[], -- Empresas incluídas
    
    consolidation_method VARCHAR(50), -- SIMPLE_SUM, WEIGHTED_AVERAGE, etc
    consolidation_date TIMESTAMP,
    
    -- Validação
    is_valid BOOLEAN DEFAULT false,
    validation_notes TEXT,
    
    created_by UUID NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (period_id) REFERENCES periods(id),
    FOREIGN KEY (created_by) REFERENCES users(id)
);

-- Valores consolidados
CREATE TABLE consolidated_values (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    consolidated_statement_id UUID NOT NULL,
    standard_account_id UUID NOT NULL,
    
    consolidated_value DECIMAL(19, 4),
    individual_values JSONB, -- Valores de cada empresa
    calculation_details JSONB,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (consolidated_statement_id) REFERENCES consolidated_statements(id),
    FOREIGN KEY (standard_account_id) REFERENCES standard_accounts(id)
);
```

### 2.10 Inteligência e Aprendizado

```sql
-- Histórico de decisões do analista
CREATE TABLE analyst_decisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    analyst_id UUID NOT NULL,
    classification_id UUID NOT NULL,
    
    decision_type VARCHAR(50), -- APPROVE, OVERRIDE, REJECT
    decision_reason TEXT,
    confidence_feedback INT, -- 1-5 escala
    
    -- Contexto
    source_account_name VARCHAR(255),
    standard_account_name VARCHAR(255),
    company_name VARCHAR(255),
    period_name VARCHAR(50),
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (analyst_id) REFERENCES users(id),
    FOREIGN KEY (classification_id) REFERENCES account_classifications(id)
);

-- Regras aprendidas através de decisões
CREATE TABLE learned_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    
    rule_name VARCHAR(255) NOT NULL,
    rule_type VARCHAR(50), -- CLASSIFICATION, CALCULATION, VALIDATION
    rule_condition TEXT, -- JSON com condição
    rule_action TEXT, -- JSON com ação
    
    confidence DECIMAL(3, 2),
    times_applied INT DEFAULT 0,
    times_validated INT DEFAULT 0,
    
    -- Origem
    derived_from_analyst_decisions INT, -- Quantidade de decisões que geraram a regra
    suggested_by_ai BOOLEAN DEFAULT false,
    approved_by UUID,
    
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    FOREIGN KEY (approved_by) REFERENCES users(id)
);
```

---

## 3. Estrutura de Dados JSON

### 3.1 Document Metadata

```json
{
  "document_metadata": {
    "extracted_text": "...",
    "tables_found": 5,
    "detected_columns": [
      {
        "index": 0,
        "header": "Descrição",
        "type": "text"
      },
      {
        "index": 1,
        "header": "31/12/2025",
        "type": "number",
        "period": "2025-12-31"
      }
    ],
    "detected_periods": ["2025-12-31", "2024-12-31"],
    "detected_companies": ["Empresa A", "Empresa B"],
    "structural_analysis": {
      "hierarchy_detected": true,
      "indentation_levels": 5
    },
    "extraction_quality": {
      "text_confidence": 0.95,
      "table_confidence": 0.87
    }
  }
}
```

### 3.2 Calculation Details

```json
{
  "calculation_applied": {
    "steps": [
      {
        "step": 1,
        "description": "Somar Ativo Circulante",
        "formula": "SUM(acc_ids: [...])",
        "result": 1500000.00
      },
      {
        "step": 2,
        "description": "Aplicar escala",
        "formula": "result * 1000",
        "result": 1500000000.00
      }
    ],
    "final_result": 1500000000.00,
    "validated": true
  }
}
```

---

## 4. Views Úteis para Relatórios

```sql
-- View: Dados consolidados por período e tipo de conta
CREATE VIEW v_consolidated_by_type AS
SELECT 
    p.year,
    p.quarter,
    at.code as account_type,
    ast.code as account_subtype,
    sa.name as account_name,
    SUM(av.final_value) as total_value,
    COUNT(DISTINCT av.company_id) as company_count
FROM account_values av
JOIN standard_accounts sa ON av.standard_account_id = sa.id
JOIN account_types at ON sa.account_type_id = at.id
JOIN account_subtypes ast ON sa.account_subtype_id = ast.id
JOIN periods p ON av.period_id = p.id
WHERE av.final_value IS NOT NULL
GROUP BY p.year, p.quarter, at.code, ast.code, sa.name;

-- View: Classificações pendentes de revisão
CREATE VIEW v_pending_reviews AS
SELECT 
    ac.id as classification_id,
    d.file_name,
    sa.original_name as source_account,
    st.name as standard_account,
    ac.confidence_score,
    ac.created_at,
    u.name as created_by
FROM account_classifications ac
JOIN source_accounts sa ON ac.source_account_id = sa.id
JOIN standard_accounts st ON ac.standard_account_id = st.id
JOIN documents d ON ac.document_id = d.id
JOIN users u ON ac.created_by_user_id = u.id
WHERE ac.reviewed_at IS NULL
ORDER BY ac.confidence_score ASC;
```

---

## 5. Migrations Strategy

Use Entity Framework Core ou Flyway para versionamento:

```
migrations/
├── 001_initial_schema.sql
├── 002_add_tenant_isolation.sql
├── 003_add_account_hierarchy.sql
├── 004_add_validation_framework.sql
├── 005_add_learning_system.sql
└── 006_add_indexes_and_optimization.sql
```

---

## 6. Indexing Strategy

**Índices Críticos para Performance:**

```sql
-- Queries de classificação
CREATE INDEX idx_source_accounts_full_search 
ON source_accounts USING GIN(to_tsvector('portuguese', original_name));

-- Queries de relatórios financeiros
CREATE INDEX idx_account_values_compound 
ON account_values(period_id, company_id, standard_account_id);

-- Queries de auditoria
CREATE INDEX idx_all_transactions_date 
ON (created_at DESC);

-- Particionamento por período (para tabelas grandes)
CREATE TABLE account_values_2025 PARTITION OF account_values
FOR VALUES FROM ('2025-01-01') TO ('2025-12-31');
```

---

## 7. Segurança e Compliance

```sql
-- Row-Level Security (RLS)
ALTER TABLE account_values ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_policy ON account_values
USING (tenant_id = current_setting('app.current_tenant')::uuid);

-- Audit Log
CREATE TABLE audit_log (
    id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(255),
    operation VARCHAR(10), -- INSERT, UPDATE, DELETE
    record_id UUID,
    old_values JSONB,
    new_values JSONB,
    changed_by UUID,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 8. Critérios de Aceite

- ✅ Schema criado com todas as 20+ tabelas
- ✅ Todas as Foreign Keys corretamente definidas
- ✅ Índices otimizados para queries principais
- ✅ Views de relatório funcionando
- ✅ RLS implementada para multitenancy
- ✅ Migrations automáticas funcionando
- ✅ Testes de integridade referencial passando

---

## 9. Próximos Passos

1. Criar migrations em EF Core
2. Implementar seed data com tipos/subtipos
3. Criar views de relatório
4. Implementar RLS para multitenancy
5. Prosseguir com **02 - Pipeline de PDF**


# 🎯 PROMPT MASTER - Sistema de Padronização e Consolidação de Demonstrações Financeiras

**Versão:** 1.0  
**Data:** Setembro 2026  
**Modelo de Desenvolvimento:** Agile com Entregas Incrementais + TDD  
**Qualidade:** Enterprise-Grade com Auditoria Completa

---

## 🎬 CONTEXTO E OBJETIVO FINAL

### O Projeto

Desenvolver um **sistema inteligente de padronização, classificação e consolidação de demonstrações financeiras** que:

- ✅ Extrai automaticamente dados de PDFs financeiros
- ✅ Classifica contas contábeis com IA + regras determinísticas
- ✅ Calcula KPIs e valores derivados
- ✅ Consolida múltiplas entidades
- ✅ Aprende continuamente com feedback humano
- ✅ Mantém auditoria completa
- ✅ Garante 100% de rastreabilidade

### Stack Tecnológica

**Backend:** ASP.NET Core 8+, C# 12+  
**Banco de Dados:** PostgreSQL 15+  
**Frontend:** React 18+, TypeScript  
**IA:** Claude API (Anthropic)  
**Testes:** xUnit, Moq, FluentAssertions  
**CI/CD:** GitHub Actions  
**Documentação:** Código + XML + README

### Documentação Base Disponível

Todos esses documentos foram fornecidos e devem ser usados como referência:

1. ✅ `00_INDICE_MESTRE.md` - Visão geral do projeto
2. ✅ `01_MODELO_DADOS_BANCO_DADOS.md` - Schema PostgreSQL completo
3. ✅ `02_PIPELINE_PDF.md` - Extração e interpretação de PDFs
4. ✅ `03_MOTOR_CLASSIFICACAO.md` - Motor inteligente de classificação
5. ✅ `04_MOTOR_CALCULOS.md` - Motor de cálculos e KPIs
6. ✅ `05_MOTOR_CONSOLIDACAO.md` - Motor de consolidação
7. ✅ `06_HUMAN_LOOP_APRENDIZADO.md` - Sistema de revisão e aprendizado
8. ✅ `07_ESPECIFICACAO_APIS.md` - Endpoints REST
9. ✅ `08_ESPECIFICACAO_FRONTEND.md` - Telas e componentes React

---

## 👥 TEAM DE AGENTES ESPECIALIZADOS

### 1️⃣ Backend Specialist (C# Core Expert)

**Papéis:**
- Implementar toda lógica de negócio em C#
- Criar interfaces IClassification, ICalculation, etc
- Implementar Dependency Injection
- Aplicar SOLID (SRP, OCP, LSP, ISP, DIP)
- Criar testes unitários com xUnit

**Responsabilidades:**
- Código limpo e bem documentado
- Zero warnings de compilação
- Testes com coverage >90%
- Code review de implementações

**Questões obrigatórias:**
- ❓ Esta interface segue SRP?
- ❓ Posso testar este serviço isoladamente?
- ❓ Está hardcodado? Deveria ser injetado?
- ❓ Faz parte do escopo definido?
- ❓ Preciso de testes de edge case?

---

### 2️⃣ Database Specialist (PostgreSQL Expert)

**Papéis:**
- Desenhar e manter schema PostgreSQL
- Criar migrations com versionamento
- Otimizar indexes para queries
- Garantir integridade referencial
- Implementar RLS para multitenancy

**Responsabilidades:**
- Schema documentado (comentários SQL)
- Migrations reversíveis
- Performance queries <200ms
- Backup/recovery strategy

**Questões obrigatórias:**
- ❓ Esta tabela está normalizada?
- ❓ Faltam indexes para performance?
- ❓ Como fazer rollback desta migration?
- ❓ Preciso de particionamento?
- ❓ RLS está corretamente configurada?

---

### 3️⃣ Software Architecture Expert

**Papéis:**
- Garantir arquitetura coerente
- Documentar decisões arquiteturais
- Revisar design patterns
- Coordenar entre backend/frontend
- Manter diagrama de componentes

**Responsabilidades:**
- ADR (Architecture Decision Records)
- Diagrama C4 atualizado
- Documentação de fluxos
- Garantir low coupling, high cohesion

**Questões obrigatórias:**
- ❓ Este componente respeita a arquitetura?
- ❓ Está acoplado a detalhes de implementação?
- ❓ Precisa documentar em ADR?
- ❓ Como isto conversa com outras camadas?
- ❓ Isto cria débito técnico?

---

### 4️⃣ AI/LLM Integration Specialist

**Papéis:**
- Implementar integração Claude API
- Criar e testar prompts
- Gerenciar tokens e custo
- Implementar fallback/retry logic
- Validar qualidade de respostas

**Responsabilidades:**
- Prompts testados e versionados
- Rate limiting e retry strategies
- Cost tracking
- Quality metrics por prompt
- Documentação de prompt engineering

**Questões obrigatórias:**
- ❓ Este prompt é determinístico?
- ❓ Como validar qualidade da resposta?
- ❓ Qual é o custo estimado?
- ❓ E se a API falhar?
- ❓ Faz parte do escopo usar IA aqui?

---

### 5️⃣ QA/Testing Specialist

**Papéis:**
- Escrever testes unitários
- Criar testes de integração
- Testar casos edge
- Validar cobertura de testes
- Manter matriz de testes

**Responsabilidades:**
- Tests coverage >90%
- Testes executam em <5 segundos
- Testes são determinísticos
- Testes documentam comportamento
- Test double strategy clara

**Questões obrigatórias:**
- ❓ Como testar isto sem conectar ao BD?
- ❓ Quais são os edge cases?
- ❓ Preciso de fixture/factory?
- ❓ Este teste é frágil?
- ❓ Cobertura está acima de 90%?

---

### 6️⃣ Accounting/Financial Expert

**Papéis:**
- Validar regras contábeis
- Revisar equações financeiras
- Garantir compliance fiscal
- Testar cenários reais
- Documentar normas contábeis

**Responsabilidades:**
- Regras contábeis corretas
- Equações fundamentais validadas
- KPIs calculados corretamente
- Testes com dados reais
- Matriz de testes do domínio

**Questões obrigatórias:**
- ❓ Esta equação está contabilmente correta?
- ❓ Este KPI segue qual norma?
- ❓ Qual é o caso de teste contábil?
- ❓ Faz sentido financeiro?
- ❓ Outras empresas fazem assim?

---

## 📋 ESTRUTURA DE ENTREGAS (Roadmap)

### 🔹 FASE 1: Foundation & Infrastructure (Semanas 1-2)

**Objetivo:** Base sólida para todo o desenvolvimento

**Entregas:**

1. **Backend Setup**
   - ✅ Projeto ASP.NET Core 8 estruturado
   - ✅ DI Container configurado
   - ✅ Logging centralizado
   - ✅ Exception handling
   - ✅ Config via appsettings.json
   - 📋 Critério de aceite: Projeto compila, zero warnings

2. **Database Setup**
   - ✅ Schema PostgreSQL (01_MODELO_DADOS_BANCO_DADOS.md)
   - ✅ Migrations com EF Core
   - ✅ Seeds para tipos/subtipos
   - ✅ RLS implementada
   - 📋 Critério de aceite: Schema criado, migrations testadas

3. **API Base Structure**
   - ✅ Controllers base com error handling
   - ✅ Autenticação JWT
   - ✅ Rate limiting
   - ✅ Response formatter
   - 📋 Critério de aceite: Endpoints base funcionando

4. **Frontend Setup**
   - ✅ Projeto React com TypeScript
   - ✅ Componentes base (Button, Input, etc)
   - ✅ Layout básico
   - ✅ Routing estruturado
   - 📋 Critério de aceite: App compila e renderiza

5. **CI/CD Pipeline**
   - ✅ GitHub Actions
   - ✅ Build automático
   - ✅ Testes automáticos
   - ✅ Deploy para staging
   - 📋 Critério de aceite: Pipeline executa com sucesso

**Agentes Responsáveis:**
- 🔵 Backend Specialist + Database Specialist + Arch Expert
- 🟢 Frontend (pode ser contratar depois)
- 🟡 QA Specialist

**Validação de Escopo:**
- ❌ NÃO fazer login funcional ainda
- ❌ NÃO implementar lógica de negócio
- ❌ NÃO fazer testes de integração com IA

---

### 🔹 FASE 2: PDF Pipeline (Semanas 3-4)

**Objetivo:** Extrair e interpretar PDFs corretamente

**Entregas:**

1. **Text Extraction Service**
   - ✅ iTextSharp/PdfSharp integrado
   - ✅ Extração de texto com posição
   - ✅ Detecção de tabelas (Tabula.NET)
   - ✅ Testes com PDFs reais
   - 📋 Critério: Extrai 95%+ do texto

2. **Hierarchy Detection**
   - ✅ Algoritmo de reconhecimento de hierarquia
   - ✅ Detecção de indentação
   - ✅ Font-based detection
   - ✅ Testes unitários
   - 📋 Critério: Identifica Tipo→Subtipo→Contas

3. **Type/Subtype Inference**
   - ✅ Motor de inferência (02_PIPELINE_PDF.md)
   - ✅ Keyword matching
   - ✅ Context inheritance
   - ✅ Testes com casos reais
   - 📋 Critério: Confidence >80%

4. **Period & Entity Detection**
   - ✅ Parser de datas múltiplos formatos
   - ✅ Detecção de colunas
   - ✅ Entity mapping
   - ✅ Testes
   - 📋 Critério: Detecta 100% dos períodos

5. **Normalization Engine**
   - ✅ Nome normalization
   - ✅ Numeric value parsing
   - ✅ Scale factor detection
   - ✅ Testes
   - 📋 Critério: Trata todos formatos BR

6. **API: Upload Endpoint**
   - ✅ POST /api/documents/upload
   - ✅ File validation
   - ✅ Queue processing
   - ✅ Status polling
   - 📋 Critério: Funciona end-to-end

7. **Frontend: Upload Screen**
   - ✅ File uploader component
   - ✅ Metadata form
   - ✅ Processing status
   - ✅ Result display
   - 📋 Critério: Upload → resultado visível

**Agentes Responsáveis:**
- 🔵 Backend Specialist (Pipeline logic)
- 🟠 Database Specialist (Persist extracted data)
- 🟡 QA Specialist (PDF test samples)
- 🔴 Accounting Expert (Validate extracted data)

**Validação de Escopo:**
- ❌ NÃO fazer classificação ainda
- ❌ NÃO fazer cálculos
- ❌ PDFs com OCR (escanear) = FORA do escopo

---

### 🔹 FASE 3: Classification Engine (Semanas 5-7)

**Objetivo:** Classificar contas com 4 camadas de inteligência

**Entregas:**

1. **Rule Engine Implementation**
   - ✅ IClassificationRule interface
   - ✅ ExactMatchRule
   - ✅ PatternMatchRule
   - ✅ CompanyHistoryRule
   - ✅ TypeSubtypeCompatibilityRule
   - ✅ Testes unitários
   - 📋 Critério: 90%+ accuracy em regras

2. **AI Integration (Claude API)**
   - ✅ IAnthropicClient wrapper
   - ✅ Prompt engineering (03_MOTOR_CLASSIFICACAO.md)
   - ✅ Response parsing
   - ✅ Error handling & retry
   - ✅ Cost tracking
   - ✅ Testes (mock responses)
   - 📋 Critério: AI confidence >70%

3. **Hierarchic Context Analyzer**
   - ✅ Parent/child analysis
   - ✅ Context scoring
   - ✅ Testes
   - 📋 Critério: Melhora confiança em 5-10%

4. **Previous Decision Analyzer**
   - ✅ Historical decision lookup
   - ✅ Similarity matching
   - ✅ Weighted scoring
   - ✅ Testes
   - 📋 Critério: Usa histórico corretamente

5. **Orchestrator (Main Classifier)**
   - ✅ AccountClassifier com 4 camadas
   - ✅ Confidence thresholds
   - ✅ Review flagging
   - ✅ Testes integration
   - 📋 Critério: Fluxo completo funciona

6. **API Endpoints**
   - ✅ GET /api/classifications/pending
   - ✅ GET /api/classifications/{id}
   - ✅ POST /api/classifications/{id}/approve
   - ✅ POST /api/classifications/{id}/override
   - 📋 Critério: Endpoints CRUD completos

7. **Frontend: Review Queue**
   - ✅ Pending items list
   - ✅ Detail view
   - ✅ Approve/Override actions
   - ✅ Evidence display
   - 📋 Critério: Revisor consegue usar

**Agentes Responsáveis:**
- 🔵 Backend Specialist (Core logic)
- 🟢 AI Specialist (Claude integration)
- 🟠 Database Specialist (Persistence)
- 🟡 QA Specialist (Test cases)
- 🔴 Accounting Expert (Validation rules)
- 🟠 Arch Expert (Design review)

**Validação de Escopo:**
- ❌ NÃO fazer fine-tuning de modelos
- ❌ NÃO usar modelos além de Claude
- ❌ Escopo de regras: apenas 10-15 iniciais

**Questões Críticas:**
- ❓ Cada regra tem teste?
- ❓ IA response é válida sempre?
- ❓ Confidence score faz sentido?
- ❓ Qual é o custo estimado mensal com IA?

---

### 🔹 FASE 4: Calculation Engine (Semanas 8-9)

**Objetivo:** Calcular valores derivados com auditoria

**Entregas:**

1. **Formula Engine**
   - ✅ FormulaEvaluator (04_MOTOR_CALCULOS.md)
   - ✅ SUM formulas
   - ✅ Custom expressions
   - ✅ Conditional formulas
   - ✅ Testes
   - 📋 Critério: Avalia fórmulas corretamente

2. **Dependency Resolver**
   - ✅ Topological sort (Kahn's algorithm)
   - ✅ Circular dependency detection
   - ✅ Calculation order
   - ✅ Testes
   - 📋 Critério: Sem deadlocks

3. **Sign Rule Engine**
   - ✅ SignRule definitions
   - ✅ Apply signs corretamente
   - ✅ Testes
   - 📋 Critério: Ativo sempre +, Despesa sempre -

4. **KPI Calculator**
   - ✅ EBITDA
   - ✅ Margens
   - ✅ Índices de Liquidez
   - ✅ Índices de Endividamento
   - ✅ Testes com dados reais
   - 📋 Critério: KPIs batem com cálculo manual

5. **Equation Validator**
   - ✅ Ativo = Passivo + PL
   - ✅ DRE validation
   - ✅ Testes
   - 📋 Critério: Detecta desbalanços

6. **Calculation Orchestrator**
   - ✅ CalculationEngine completo
   - ✅ Audit trail gerado
   - ✅ Error handling
   - ✅ Testes
   - 📋 Critério: End-to-end funciona

7. **API Endpoints**
   - ✅ GET /api/calculations/{doc}/{period}/{company}
   - ✅ POST /api/calculations/recalculate
   - 📋 Critério: Endpoints funcionam

**Agentes Responsáveis:**
- 🔵 Backend Specialist (Core formulas)
- 🟡 QA Specialist (Test cases)
- 🔴 Accounting Expert (KPI validation)
- 🟠 Arch Expert (Design review)

**Validação de Escopo:**
- ❌ NÃO criar todas as fórmulas possíveis (max 20)
- ❌ NÃO implementar custom formulas do usuário ainda

**Questões Críticas:**
- ❓ Cada fórmula tem teste com dados reais?
- ❓ KPIs estão corretos contabilmente?
- ❓ Qual é a precisão de decimais?

---

### 🔹 FASE 5: Consolidation & Validation (Semanas 10-11)

**Objetivo:** Consolidar múltiplas entidades com validação

**Entregas:**

1. **Consolidation Strategies**
   - ✅ SimpleConsolidationStrategy
   - ✅ WeightedConsolidationStrategy
   - ✅ Testes
   - 📋 Critério: Consolida corretamente

2. **Intercompany Elimination**
   - ✅ IntercompanyEliminationEngine (05_MOTOR_CONSOLIDACAO.md)
   - ✅ Detect mutual accounts
   - ✅ Apply eliminations
   - ✅ Testes
   - 📋 Critério: Remove transações duplicadas

3. **Reconciliation Engine**
   - ✅ CheckSourceTotals
   - ✅ CheckBasicEquation
   - ✅ CheckSignConsistency
   - ✅ CheckAnomalousValues
   - ✅ Testes
   - 📋 Critério: Todas as validações passam

4. **Consolidation Orchestrator**
   - ✅ ConsolidationEngine completo
   - ✅ End-to-end flow
   - ✅ Testes
   - 📋 Critério: Consolida + valida

5. **API Endpoints**
   - ✅ POST /api/consolidations
   - ✅ GET /api/consolidations/{id}
   - 📋 Critério: Endpoints funcionam

6. **Frontend: Consolidation UI**
   - ✅ New consolidation form
   - ✅ Result view
   - ✅ Reconciliation display
   - 📋 Critério: UX intuitiva

**Agentes Responsáveis:**
- 🔵 Backend Specialist (Core logic)
- 🟠 Database Specialist (Query optimization)
- 🟡 QA Specialist (Test scenarios)
- 🔴 Accounting Expert (Validation rules)

---

### 🔹 FASE 6: Human-in-the-loop & Learning (Semanas 12-13)

**Objetivo:** Sistema aprende com feedback humano

**Entregas:**

1. **Review Queue Management**
   - ✅ ReviewQueueManager (06_HUMAN_LOOP_APRENDIZADO.md)
   - ✅ Criteria for sending to review
   - ✅ Priority calculation
   - ✅ Testes
   - 📋 Critério: Items enfileirados corretamente

2. **Decision Recording**
   - ✅ AnalystDecisionRecorder
   - ✅ Persistence + audit
   - ✅ Testes
   - 📋 Critério: Todas as decisões registradas

3. **Pattern Learning**
   - ✅ PatternLearningEngine
   - ✅ Derive patterns from decisions
   - ✅ Testes
   - 📋 Critério: Padrões extraídos corretamente

4. **Rule Learning**
   - ✅ RuleLearningEngine
   - ✅ Derive rules from patterns
   - ✅ Testes
   - 📋 Critério: Regras aplicáveis

5. **Feedback Collector**
   - ✅ Quality metrics
   - ✅ Accuracy tracking
   - ✅ Testes
   - 📋 Critério: Metrics precisas

6. **Learning Dashboard**
   - ✅ Stats compilation
   - ✅ Progress tracking
   - ✅ Testes
   - 📋 Critério: Dashboard informativo

7. **API Endpoints**
   - ✅ GET /api/review-queue
   - ✅ POST /api/review-queue/{id}/assign
   - ✅ POST /api/review-queue/{id}/decision
   - ✅ GET /api/learning/stats
   - ✅ GET /api/learning/rules
   - 📋 Critério: Endpoints CRUD

8. **Frontend: Review & Learning UI**
   - ✅ Review queue view
   - ✅ Decision UI
   - ✅ Learning dashboard
   - 📋 Critério: UX completa

**Agentes Responsáveis:**
- 🔵 Backend Specialist (Core logic)
- 🟢 AI Specialist (Pattern/rule derivation)
- 🟠 Database Specialist (Queries)
- 🟡 QA Specialist (Test scenarios)

---

### 🔹 FASE 7: Reporting & Analytics (Semana 14)

**Objetivo:** Gerar relatórios e dashboards

**Entregas:**

1. **Financial Statement Reports**
   - ✅ Statement generation
   - ✅ PDF export
   - ✅ Excel export
   - ✅ Testes

2. **Quality Reports**
   - ✅ Extraction quality
   - ✅ Classification quality
   - ✅ Calculation quality

3. **Learning Analytics**
   - ✅ Accuracy over time
   - ✅ Pattern effectiveness
   - ✅ Rule performance

4. **Frontend: Dashboards**
   - ✅ Analytics views
   - ✅ Export functionality
   - ✅ Charts & tables

**Agentes Responsáveis:**
- 🔵 Backend Specialist
- 🟠 Frontend Developer
- 🟡 QA Specialist

---

### 🔹 FASE 8: Production Hardening (Semana 15)

**Objetivo:** Production-ready

**Entregas:**

1. **Performance Tuning**
   - ✅ Query optimization
   - ✅ Caching strategy
   - ✅ API response times <200ms

2. **Security Hardening**
   - ✅ SQL injection prevention
   - ✅ XSS prevention
   - ✅ CSRF tokens
   - ✅ Rate limiting

3. **Monitoring & Alerting**
   - ✅ Error tracking
   - ✅ Performance monitoring
   - ✅ Alert strategy

4. **Documentation**
   - ✅ API documentation (Swagger)
   - ✅ Deployment guide
   - ✅ Troubleshooting guide

5. **Testing**
   - ✅ Load testing
   - ✅ Penetration testing
   - ✅ End-to-end testing

**Agentes Responsáveis:**
- 🔵 Backend Specialist
- 🟠 Database Specialist
- 🟠 DevOps (se houver)
- 🟡 QA Specialist

---

## 🚀 COMO USAR ESTE PROMPT MASTER

### Cenário 1: Iniciar uma Nova Fase

**Você diz:**
```
Iniciando FASE 2: PDF Pipeline

Tarefas principais:
1. Text Extraction Service
2. Hierarchy Detection
3. Type/Subtype Inference

Reúna os agentes: 
- @Backend_Specialist
- @Database_Specialist
- @QA_Specialist
- @Accounting_Expert

Vocês têm perguntas antes de começar?
```

**Agentes respondem:**
```
@Backend_Specialist:
❓ Qual é a máxima complexidade esperada de um PDF?
❓ Preciso validar o texto extraído contra o PDF visual?
❓ Qual é o timeout aceitável para extração?

@Database_Specialist:
❓ Preciso persistir o texto extraído completamente?
❓ Como particiono dados de PDFs grandes?

@QA_Specialist:
❓ Preciso testar com PDFs de múltiplas páginas?
❓ Qual é a taxa de sucesso esperada?

@Accounting_Expert:
❓ Como validar se a extração está contabilmente correta?
```

**Você responde com clareza** e trabalho começa.

---

### Cenário 2: Code Review Entre Fases

**Você diz:**
```
@Backend_Specialist: Pronto para review da TextExtractionService?
```

**Backend responde com:**
```
Sim. Aqui está:
- Interface: ITextExtractionService
- Implementação: PdfSharpTextExtractor
- Tests: 12 unit tests, 95% coverage

❓ Isto segue corretamente o IClassificationRule pattern?
❓ Preciso testar com PDFs scaneados (OCR)?

@QA_Specialist: Seus testes cobrem edge cases?
@Database_Specialist: Isto vai persistir no BD?
@Arch_Expert: Design está alinhado com camadas?
```

---

### Cenário 3: Mudança de Escopo

**Você diz:**
```
Mudança proposta: Adicionar suporte a EXCEL além de PDF

Precisamos discutir:
1. Impacto no escopo
2. Novo timeline
3. Risco técnico
```

**Arch Expert responde:**
```
❓ Isto adiciona 1-2 semanas?
❓ Componentes afetados: Pipeline PDF inteiro
❓ Recomendação: Postergar para Phase 2.5
```

---

### Cenário 4: Bug Encontrado em Produção

**QA diz:**
```
Bug: Classificação retornando Confidence = NaN

@Backend: Qual pode ser a causa?
@Database: Há dados corrompidos?
@Arch: Faz parte do escopo investigar?
```

**Workflow:**
1. Todos questionam até encontrar raiz
2. Documenta no código (comentário com explicação)
3. Adiciona teste para não repetir
4. Valida escopo

---

## 📐 REGRAS DE OURO

### ✅ Obrigatório

1. **Cada delivery tem testes unitários (>90% coverage)**
2. **Zero warnings de compilação**
3. **Documentação no código (XML comments + README)**
4. **Arquitetura documentada (ADR)**
5. **Escopo validado antes de escrever código**
6. **Nenhum hardcoding - tudo é configurável**
7. **Migrations reversíveis**
8. **Logs estruturados em todas as operações críticas**
9. **Auditoria completa (quem, o quê, quando)**
10. **Validação de negócio com Accounting Expert**

### ❌ Proibido

1. ❌ Desenvolver sem testes
2. ❌ Copiar código sem entender
3. ❌ Mudar escopo sem aprovação
4. ❌ Usar padrões não documentados
5. ❌ Deixar TODO/HACK sem issue
6. ❌ Ignorar warnings de compilação
7. ❌ Código sem comments complexos
8. ❌ Supor em vez de questionar
9. ❌ Persistir dados sem auditoria
10. ❌ Release sem validação de negócio

---

## 🎯 CHECKLIST DE SINCRONIZAÇÃO ENTRE AGENTES

Toda vez que uma fase termina:

- ✅ Backend avisa: "TextExtractionService pronto"
- ✅ Database avisa: "Tabelas criadas e migradas"
- ✅ QA avisa: "100 testes verdes, 95% coverage"
- ✅ Arch avisa: "Documentação ADR atualizada"
- ✅ Accounting avisa: "Validação contábil OK"

Só depois a fase é considerada **DONE**.

---

## 📊 MÉTRICAS DE QUALIDADE

Rastreamos continuamente:

| Métrica | Target | Frequency |
|---------|--------|-----------|
| Test Coverage | >90% | Cada commit |
| Build Time | <5min | Cada commit |
| Test Time | <5min | Cada commit |
| Code Quality (SonarQube) | A | Semanal |
| Performance (API <200ms) | 95% percentile | Semanal |
| Accuracy (Accounting) | >95% | Mensal |
| Documentation | 100% | Cada delivery |

---

## 📝 TEMPLATE: Estrutura de Entrega

Cada fase usa este template:

```
# PHASE X: [Nome]

## Overview
[Descrição executiva]

## Deliverables
- [ ] Deliverable 1
  - [ ] Sub-task 1.1
  - [ ] Sub-task 1.2
  - [ ] Tests
  - [ ] Documentation
- [ ] Deliverable 2
  ...

## Acceptance Criteria
- Code compiles: ✅
- Tests pass: ✅
- Coverage >90%: ✅
- Docs complete: ✅
- Accounting validated: ✅
- No scope creep: ✅

## Risks & Mitigations
| Risk | Mitigation |
|------|-----------|
| ... | ... |

## Sign-off
- Backend Specialist: ___
- Database Specialist: ___
- QA Specialist: ___
- Accounting Expert: ___
- Arch Expert: ___
```

---

## 🔄 Próximos Passos

1. **Copie este prompt** para usar em cada fase
2. **Customize os sub-prompts** para cada agente
3. **Execute FASE 1** com todos os agentes
4. **Valide escopo** antes de cada phase
5. **Rastreie progresso** em spreadsheet ou Jira

---

## 🎓 Prompts Específicos por Agente

Veja os prompts a seguir para instruções detalhadas de cada especialista:

- **PROMPT-BACKEND.md** → C# Core Expert
- **PROMPT-DATABASE.md** → PostgreSQL Expert
- **PROMPT-QA.md** → Testing Specialist
- **PROMPT-ACCOUNTING.md** → Financial Expert
- **PROMPT-ARCHITECTURE.md** → Software Architect
- **PROMPT-AI.md** → AI Integration Specialist

---

## 📞 Quando Algo Sai do Escopo

**Você percebe:** "Este requisito não estava planejado"

**Ação:**
1. Pause trabalho atual
2. Questione: "Qual é o impacto no timeline?"
3. Convoque Arch Expert
4. Decida: **Adicionar à fase atual, atrasar, ou postergar?**
5. Documente a decisão em ADR

**Nunca continue** se escopo mudou.

---

**Versão Final: Setembro 2026**  
**Atualizado: Antes de cada fase**


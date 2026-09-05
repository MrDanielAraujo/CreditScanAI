# 📋 CASOS DE USO - Sistema de Demonstrações Financeiras

**Especificação Funcional - Casos de Uso Detalhados**

**Data:** Setembro 2026  
**Status:** Completo

---

## 1. VISÃO GERAL DOS ATORES

```
┌─────────────────────────────────────────────┐
│           ATORES DO SISTEMA                 │
├─────────────────────────────────────────────┤
│ 1. Analyst (Contador/Analista)             │
│    - Carrega PDFs de demonstrações         │
│    - Revisão cotidiana de classificações   │
│    - Cria consolidações                    │
│    - 8+ horas/dia no sistema               │
│                                             │
│ 2. Reviewer (Gerente/Controller)           │
│    - Aprova/rejeita classificações críticas│
│    - Valida consolidações                  │
│    - 2-3 horas/dia                         │
│                                             │
│ 3. CFO (Executivo Financeiro)              │
│    - Visualiza dashboards e relatórios    │
│    - Valida resultados finais              │
│    - 30min/dia                             │
│                                             │
│ 4. System Admin (Técnico)                  │
│    - Gerencia usuários e permissões        │
│    - Monitora sistema                      │
│    - Configura regras e padrões            │
└─────────────────────────────────────────────┘
```

---

## 2. CASOS DE USO PRINCIPAIS

### UC-01: Fazer Login

**Ator Primário:** Analyst, Reviewer, CFO  
**Pré-condições:** Usuário tem credenciais válidas  
**Pós-condições:** Usuário autenticado, vê dashboard  

**Fluxo Principal:**

```
1. Usuário acessa login.html
2. Insere email e senha
3. Clica "Login"
4. Sistema valida credenciais contra BD
5. Se válido:
   a. Gera JWT token
   b. Armazena em localStorage
   c. Redireciona para /dashboard
6. Se inválido:
   a. Exibe erro "Email ou senha inválidos"
   b. Limpa campo de senha
   c. Mantém email para reintentativa
```

**Fluxo Alternativo (Esqueceu Senha):**

```
1. Usuário clica "Esqueceu sua senha?"
2. Insere email
3. Sistema envia email com link de reset
4. Usuário clica link (24h de validade)
5. Insere nova senha
6. Sistema atualiza BD
7. Redireciona para login
```

**Requisitos Funcionais:**

- ✅ Email/senha validados
- ✅ Senhas hasheadas (bcrypt)
- ✅ JWT token com 24h de validade
- ✅ Limite de tentativas (5 tentativas = 15min lockout)
- ✅ Auditoria: log de todos os logins

---

### UC-02: Fazer Upload de PDF

**Ator Primário:** Analyst  
**Pré-condições:** Usuário autenticado, tem permissão de upload  
**Pós-condições:** Documento enfileirado para processamento  

**Fluxo Principal:**

```
1. Analyst acessa /documents/upload
2. Arrasta PDF para dropzone OU clica para selecionar
3. Sistema valida:
   a. Arquivo é PDF?
   b. Tamanho ≤100MB?
   c. Não é duplicado (hash)?
4. Se válido:
   a. Mostra form de metadados:
      - Empresa (dropdown)
      - Período (date picker)
      - Tipo de demonstração (BALANCE_SHEET / INCOME_STATEMENT)
   b. Analyst preenche e clica "Confirmar Upload"
5. Sistema:
   a. Salva arquivo em S3
   b. Cria registro em documents table
   c. Enfileira em fila de processamento
   d. Mostra UUID do documento
   e. Redireciona para /documents/{id}/processing
```

**Fluxo Alternativo (Arquivo Duplicado):**

```
1. Sistema detecta hash_id já existe
2. Oferece opções:
   a. "Usar documento anterior"
   b. "Upload como novo de qualquer forma"
3. Se "usar anterior": redireciona para resultado anterior
4. Se "novo de qualquer forma": continua upload
```

**Fluxo de Erro:**

```
1. PDF corrompido:
   → Erro: "Arquivo PDF inválido"
2. Arquivo muito grande:
   → Erro: "Máximo 100MB"
3. Falha na validação de tipo:
   → Aviso: "Tipo não detectado automaticamente, 
      escolha manualmente"
```

**Requisitos Funcionais:**

- ✅ Validação client-side (tipo + tamanho)
- ✅ Validação server-side (PDF corrompido?)
- ✅ Progress bar durante upload
- ✅ Detecção de duplicatas (hash MD5)
- ✅ Armazenamento seguro (S3 + encryption)

---

### UC-03: Processar PDF (Pipeline Automático)

**Ator Primário:** Sistema (automático)  
**Pré-condições:** Documento enfileirado em fila  
**Pós-condições:** Dados extraídos armazenados, pronto para classificação  

**Fluxo Principal (Automático):**

```
FASE 1: Extração
1. Busca documento da fila
2. Faz download de S3
3. Extrai texto com iTextSharp
4. Extrai tabelas com Tabula.NET
5. Salva texto bruto em documents.metadata

FASE 2: Análise de Layout
1. Reconstrói hierarquia (Tipo → Subtipo → Contas)
2. Identifica indentação
3. Detecta font sizes
4. Cria estrutura hierárquica

FASE 3: Detecção de Períodos
1. Busca datas em headers
2. Normaliza formatos (31/12/2025, etc)
3. Mapeia colunas para períodos
4. Detecta moeda

FASE 4: Normalização
1. Normaliza nomes de contas
2. Processa valores numéricos
3. Detecta escala (milhões, milhares)
4. Remove acentos e caracteres especiais

FASE 5: Validação
1. Verifica qualidade de extração >80%
2. Valida se tem períodos detectados
3. Valida se tem contas detectadas
4. Se qualidade OK: marca READY_FOR_CLASSIFICATION
5. Se qualidade ruim: marca REVIEW_REQUIRED

STATUS: COMPLETED
CONFIDENCE: 87%
TEMPO: 45 segundos
```

**Requisitos Funcionais:**

- ✅ Processamento assíncrono (não bloqueia UI)
- ✅ Retry automático (3 tentativas)
- ✅ Timeout: 5 minutos máximo
- ✅ Trail completo de auditoria
- ✅ Notificação ao analista quando pronto

---

### UC-04: Revisar Classificações

**Ator Primário:** Analyst / Reviewer  
**Pré-condições:** Documento processado, classificações geradas  
**Pós-condições:** Classificações aprovadas/alteradas/rejeitadas  

**Fluxo Principal:**

```
1. Usuário acessa /review (Fila de Revisão)
2. Sistema lista contas pendentes de revisão:
   - Classificadas com confiança <70%
   - Padrões novos
   - Valores anômalos
   - Contas críticas (receita, lucro)

3. Usuário clica em um item da lista

4. Sistema mostra detalhe com:
   ┌─────────────────────────────┐
   │ CONTA ORIGINAL              │
   │ "Caixa e bancos"           │
   │ Valor: R$ 150M              │
   │ Tipo: ATIVO                 │
   ├─────────────────────────────┤
   │ SUGESTÃO (Confiança: 92%)   │
   │ "Caixa e Equivalentes"     │
   │ Método: IA                  │
   │ Evidência:                  │
   │ - Similaridade: 0.95        │
   │ - Histórico: 3/3 OK         │
   ├─────────────────────────────┤
   │ ALTERNATIVAS                │
   │ 1. Outros Ativos (35%)      │
   │ 2. Investimentos (12%)      │
   ├─────────────────────────────┤
   │ HISTÓRICO EMPRESA           │
   │ 5 decisões similares        │
   │ 100% classificadas como     │
   │ "Caixa e Equivalentes"      │
   └─────────────────────────────┘

5. Usuário escolhe ação:
   a. ✅ APROVAR
   b. 🔄 ALTERAR (escolhe alternativa)
   c. ❌ REJEITAR
   d. 💾 CRIAR REGRA (se padrão claro)

6. Sistema registra decisão com:
   - Qual ação (APPROVE/OVERRIDE/REJECT)
   - Timestamp
   - Usuário
   - Motivo (se override)
   - Confidence feedback (1-5 stars)

7. Sistema avança para próximo item

8. Fim: Status = REVIEWED
```

**Fluxo de OVERRIDE:**

```
1. Usuário clica "Alterar" em Alternativa
2. Escolhe "Outros Ativos"
3. Clica "Confirmar Override"
4. Abre form de feedback:
   - "Por que escolheu 'Outros Ativos'?"
   - Confidence (1-5 stars)
   - Checkbox: "Criar regra a partir desta decisão"
5. Salva override
6. Se "criar regra": sistema cria padrão:
   "Se conta contém 'bancos' E tipo = ATIVO
    → Classificar como 'Caixa e Equivalentes'"
```

**Requisitos Funcionais:**

- ✅ Busca + filtros (confiança, tipo, prioridade)
- ✅ Lista mostra 20 itens, paginada
- ✅ Detalhe abre em painel lateral (não modal)
- ✅ Histórico de decisões dessa conta
- ✅ Teclado: Enter aprova, R rejeita, etc
- ✅ Auditar cada decisão

---

### UC-05: Ver Dashboard de Analytics

**Ator Primário:** CFO / Reviewer  
**Pré-condições:** Sistema processou documentos  
**Pós-condições:** Visualiza métricas e trends  

**Fluxo Principal:**

```
1. Usuário acessa /analytics

2. Dashboard mostra:

   ┌─── OVERVIEW ─────────────────┐
   │ Total documentos processados  │
   │ 24 (↑5 vs semana anterior)   │
   │                              │
   │ Taxa de acurácia do sistema  │
   │ 88% (↑3% vs mês anterior)    │
   │                              │
   │ Itens pendentes de revisão   │
   │ 4 críticos, 12 médios       │
   │                              │
   │ Sistema em aprendizado       │
   │ 15 regras criadas (ativas)   │
   └──────────────────────────────┘

   ┌─── ACCURACY TREND ────────────┐
   │ Gráfico de linha: 65% → 88%  │
   │ Últimos 30 dias               │
   └──────────────────────────────┘

   ┌─── PATTERNS LEARNED ──────────┐
   │ 1. "Caixa" → Caixa OK (96%)   │
   │ 2. "Fornecedores" → Pass OK   │
   │    (92%)                      │
   │ 3. "Receita" → DRE OK (87%)   │
   └──────────────────────────────┘

   ┌─── TOP ISSUES ────────────────┐
   │ 1. Contas com "Outros" (5x)   │
   │ 2. Valores anômalos (3x)      │
   │ 3. Padrões novos (2x)         │
   └──────────────────────────────┘

3. Usuário pode:
   - Clicar em métrica para detalhes
   - Filtrar por período (último mês/trimestre/ano)
   - Exportar dados como CSV/Excel
```

**Requisitos Funcionais:**

- ✅ Gráficos interativos (Chart.js / Recharts)
- ✅ Atualização automática a cada 5 minutos
- ✅ Alertas se accuracy < 80%
- ✅ Comparação com períodos anteriores
- ✅ Filtros por empresa/período/tipo

---

### UC-06: Criar Consolidação

**Ator Primário:** Analyst  
**Pré-condições:** Múltiplos documentos processados  
**Pós-condições:** Consolidação criada, validada  

**Fluxo Principal:**

```
1. Analyst acessa /consolidations/new

2. Form pede:
   - Período (date picker)
   - Empresas (multi-select)
   - Método (Simple/Weighted/Proportional)
   - Eliminar inter-company transactions? (checkbox)

3. Analyst preenche:
   - Período: 31/12/2025
   - Empresas: Co. A, Co. B, Co. C
   - Método: Simple
   - Eliminar: SIM

4. Clica "Consolidar"

5. Sistema:
   a. Busca demonstrações de cada empresa
   b. Valida completude (todas têm ativo=passivo+pl?)
   c. Se falta: oferece opções (skip, awaiting payment, fake data)
   d. Aplica método (soma valores)
   e. Identifica transações inter-company
   f. Elimina valores duplicados
   g. Valida nova equação fundamental
   h. Gera resultado consolidado

6. Mostra resultado:
   ┌─────────────────────────────────┐
   │ CONSOLIDAÇÃO CONCLUÍDA          │
   ├─────────────────────────────────┤
   │ Período: 31/12/2025             │
   │ Empresas: 3 (A, B, C)           │
   │ Método: Simple Sum              │
   │                                 │
   │ DEMONSTRAÇÃO CONSOLIDADA        │
   │ Ativo Total:     R$ 4.500M      │
   │ Passivo Total:   R$ 2.400M      │
   │ PL Total:        R$ 2.100M      │
   │                                 │
   │ VALIDAÇÕES                      │
   │ ✅ Equação fundamental OK       │
   │ ✅ Sem valores negativos        │
   │ ✅ Dentro de materialidade      │
   │                                 │
   │ ELIMINAÇÕES APLICADAS           │
   │ - Contas Receber A→B: R$ 50M   │
   │ - Contas Pagar B→A: R$ 50M     │
   │                                 │
   │ [Baixar Excel] [Baixar PDF]     │
   │ [Ver Detalhes]  [Compartilhar]  │
   └─────────────────────────────────┘
```

**Requisitos Funcionais:**

- ✅ Validação de completude antes de consolidar
- ✅ Múltiplas estratégias de consolidação
- ✅ Eliminação automática de transações
- ✅ Validação pós-consolidação
- ✅ Export em Excel/PDF
- ✅ Auditoria de eliminações

---

### UC-07: Gerar Relatório Financeiro

**Ator Primário:** CFO / Analyst  
**Pré-condições:** Consolidação validada  
**Pós-condições:** Relatório gerado em formato solicitado  

**Fluxo Principal:**

```
1. Usuário acessa /reports/new

2. Form pede:
   - Tipo de demonstração (BS, IS, ou Ambas)
   - Período
   - Empresas (ou consolidado)
   - Formato (PDF, Excel, JSON)
   - Incluir auditoria? (checkbox)
   - Incluir confidence scores? (checkbox)

3. Usuário preenche e clica "Gerar"

4. Sistema:
   a. Busca dados consolidados
   b. Formata segundo layout padrão
   c. Inclui notas explicativas
   d. Se auditoria: add trail completo
   e. Se confidence: add scores em cada conta
   f. Gera documento (PDF/Excel)

5. Oferece download:
   - Filename: "DemFin_2025_12_CO3.pdf"
   - Tamanho: 2.3 MB
   - Tempo de processamento: 12 segundos
```

**Requisitos Funcionais:**

- ✅ Templates de layout profissional
- ✅ Notas explicativas automáticas
- ✅ Auditoria rastreável
- ✅ Múltiplos formatos
- ✅ Assinatura digital (futuro)

---

## 3. CASOS DE USO SECUNDÁRIOS

### UC-08: Gerenciar Usuários (Admin)

```
Ator: System Admin
Pré: Admin autenticado
Pós: Usuário criado/editado/deletado

Fluxo:
1. Admin acessa /admin/users
2. Lista usuários com filtros
3. Clica em usuário:
   a. Vê email, nome, role
   b. Pode resetar senha
   c. Pode revogar acesso
   d. Pode auditar atividades
```

---

### UC-09: Configurar Regras de Classificação (Admin)

```
Ator: System Admin ou Analyst Senior
Pré: Admin autenticado
Pós: Regra nova criada/modificada

Fluxo:
1. Admin acessa /admin/rules
2. Lista regras existentes
3. Pode:
   a. Criar nova regra (expressão + resultado)
   b. Editar regra existente
   c. Ativar/desativar regra
   d. Ver estatísticas da regra (usada 156x, 92% acurácia)
```

---

### UC-10: Auditar Sistema (Compliance)

```
Ator: Auditor / Compliance Officer
Pré: Acesso de auditoria
Pós: Relatório de auditoria gerado

Fluxo:
1. Auditor acessa /audit
2. Pode filtrar por:
   - Data range
   - Tipo de operação (upload, classify, approve, etc)
   - Usuário
   - Empresa/período
3. Vê log completo com:
   - Timestamp
   - Usuário
   - Ação
   - Dados antes/depois
   - IP e user agent
4. Pode exportar relatório de auditoria
```

---

## 4. TABELA RESUMIDA DE CASOS DE USO

| UC | Nome | Ator | Frequência | Complexidade |
|----|------|------|-----------|-------------|
| 01 | Login | All | Diária | Baixa |
| 02 | Upload PDF | Analyst | 5-10x/dia | Média |
| 03 | Processar PDF | System | Auto | Alta |
| 04 | Revisar Classificações | Analyst/Reviewer | 50-100x/dia | Alta |
| 05 | Ver Analytics | CFO/Reviewer | 1-2x/dia | Média |
| 06 | Consolidar | Analyst | 2-5x/semana | Alta |
| 07 | Gerar Relatório | CFO/Analyst | 1x/semana | Média |
| 08 | Gerenciar Usuários | Admin | 1-2x/semana | Baixa |
| 09 | Configurar Regras | Admin/Senior | 1-3x/semana | Média |
| 10 | Auditar | Compliance | Mensal | Média |

---

## 5. MATRIZ DE PERMISSÕES

```
              | Login | Upload | Review | Consolidate | Export | Admin |
--------------|-------|--------|--------|-------------|--------|-------|
Analyst       |  ✅   |   ✅   |   ✅   |     ✅      |   ✅   |  ❌   |
Reviewer      |  ✅   |   ❌   |   ✅   |     ❌      |   ✅   |  ❌   |
CFO           |  ✅   |   ❌   |   ❌   |     ❌      |   ✅   |  ❌   |
Admin         |  ✅   |   ✅   |   ✅   |     ✅      |   ✅   |  ✅   |
Compliance    |  ✅   |   ❌   |   ❌   |     ❌      |   ✅   |  ✅*  |
              |       |        |        |             |        | (audit|
              |       |        |        |             |        |  only)|
```

---

**Fim da Especificação de Casos de Uso**


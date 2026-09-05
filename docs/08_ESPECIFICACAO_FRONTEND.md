# 8. Especificação do Frontend

**Especificação Técnica - Sistema de Classificação de Demonstrações Financeiras**

**Versão:** 1.0  
**Data:** Setembro 2026  
**Stack:** React 18+, TypeScript, Tailwind CSS

---

## 1. Visão Geral

Frontend é uma **aplicação React moderna** com foco em:
- Upload e processamento de PDFs
- Revisão inteligente de classificações
- Dashboard de analytics
- Gerenciamento de dados consolidados

---

## 2. Estrutura de Páginas

### 2.1 Autenticação

**`/login`**
- Form: Email + Senha
- Checkbox: "Lembrar-me"
- Link: "Esqueci minha senha"
- Social login: (opcional)

**`/register`**
- Form: Email, Nome, Senha, Confirmação de Senha
- Termos de serviço checkbox
- Verificação de email após registro

**`/forgot-password`**
- Input: Email
- Confirmação: "Email de recuperação enviado"

---

### 2.2 Dashboard Principal

**`/dashboard`**

Layout:
```
┌─────────────────────────────────────────┐
│  Logo          Menu           Usuário   │
├─────────────────────────────────────────┤
│            WELCOME DASHBOARD            │
├─────────────────────────────────────────┤
│                                         │
│  ┌────────────┐  ┌────────────┐       │
│  │ 24 docs    │  │ 156 contas │       │
│  │ processados│  │ classificadas       │
│  └────────────┘  └────────────┘       │
│                                         │
│  ┌────────────────────────────────┐   │
│  │ Atividade Recente              │   │
│  │ - Doc XYZ processado em 45s    │   │
│  │ - 12 classificações aprovadas  │   │
│  └────────────────────────────────┘   │
│                                         │
└─────────────────────────────────────────┘
```

**Widgets:**
- Total de documentos processados
- Taxa de acurácia do sistema
- Itens pendentes de revisão
- Atividade recente
- Quick actions (Upload, Novo projeto)

---

### 2.3 Upload de Documento

**`/documents/upload`**

Componente: File Uploader
```
┌─────────────────────────────────┐
│  Selecione arquivo PDF          │
│  ┌────────────────────────────┐ │
│  │  Ou arraste aqui           │ │
│  │  (máx. 100MB)              │ │
│  └────────────────────────────┘ │
│  ──────────────────────────────  │
│  Metadados:                      │
│  - Company: [dropdown]           │
│  - Period: [date picker]         │
│  - Type: BALANCE_SHEET / DRE    │
│  ─────────────────────────────── │
│  [Cancelar]        [Fazer Upload]│
└─────────────────────────────────┘
```

**Fluxo:**
1. Selecionar arquivo
2. Informar metadados
3. Confirmar upload
4. Redirecionar para status de processamento

---

### 2.4 Status de Processamento

**`/documents/{id}/processing`**

Live Status:
```
┌─────────────────────────────────┐
│  Processando documento...       │
│  ┌─────────────────────────┐   │
│  │ 1. Extração      ✓      │   │
│  │ 2. Interpretação  ⏳    │   │
│  │ 3. Classificação  ○     │   │
│  │ 4. Cálculos       ○     │   │
│  │ 5. Consolidação   ○     │   │
│  │ 6. Validação      ○     │   │
│  └─────────────────────────┘   │
│  Progresso: ███░░░░░  45%       │
│  Tempo estimado: 1 min 30s      │
└─────────────────────────────────┘
```

**Componente:** Progress Stepper com status em tempo real

---

### 2.5 Revisão de Classificações

**`/review`** (Fila de Revisão)

Tabela com:
| Conta Original | Sugerida | Confiança | Prioridade | Ação |
|---|---|---|---|---|
| Caixa e bancos | Caixa... | 0.92 (↑) | Baixa | > |
| Conta X | Outros... | 0.35 (↓) | Alta | > |

Filtros:
- Por confiança (baixa, média, alta)
- Por prioridade
- Por status (pendente, em revisão)
- Por tipo de conta

**`/review/{id}`** (Detalhe de Classificação)

Layout:
```
┌──────────────────────────────────────┐
│  REVISÃO DE CLASSIFICAÇÃO            │
├──────────────────────────────────────┤
│  Conta Original: Caixa e bancos      │
│  Documento: Balance_2025.pdf         │
│  Empresa: Empresa A | Período: 2025  │
│  Valor: R$ 150.000.000               │
├──────────────────────────────────────┤
│  SUGESTÃO:                           │
│  ┌────────────────────────────────┐  │
│  │ Caixa e Equivalentes de Caixa  │  │
│  │ Confiança: 92%                 │  │
│  │ Método: IA                     │  │
│  │                                │  │
│  │ Evidência:                     │  │
│  │ • Similaridade semântica: 0.95 │  │
│  │ • Tipo: ATIVO ✓                │  │
│  │ • Subtipo: CIRCULANTE ✓        │  │
│  │ • Decisões históricas: 3/3     │  │
│  │   classificadas como Caixa     │  │
│  └────────────────────────────────┘  │
├──────────────────────────────────────┤
│  ALTERNATIVAS:                       │
│  1. Outros Ativos Circulantes (35%)  │
│  2. Depósitos Judiciais (12%)        │
├──────────────────────────────────────┤
│  HISTÓRICO:                          │
│  • Empresa A: sempre classifica      │
│    como "Caixa e Equivalentes"       │
├──────────────────────────────────────┤
│  [Rejeitar] [Alterar ▼] [Aprovar]   │
└──────────────────────────────────────┘
```

**Ações:**
- ✅ Aprovar (com feedback opcional)
- 🔄 Alterar (escolher alternativa)
- ❌ Rejeitar (com motivo)
- 💾 Criar regra a partir desta decisão

---

### 2.6 Gerenciamento de Documentos

**`/documents`** (Lista)

Features:
- Filtrar por empresa, período, status
- Busca por nome de arquivo
- Ordenar por data, status, confiança
- Ações em batch: reprocessar, validar
- Download de resultados

Colunas:
| Arquivo | Empresa | Período | Status | Confiança | Ações |
|---------|---------|---------|--------|-----------|-------|
| file.pdf| Co. A | 2025 | ✓ | 87% | ⋯ |

---

### 2.7 Consolidação

**`/consolidations/new`**

Form:
```
Período: [date picker]
Empresas: [multi-select]
  ☑ Company A
  ☑ Company B
  ☑ Company C
Método: 
  ○ Simples (soma)
  ○ Ponderada
  ○ Proporcional
Eliminações: ☑ Aplicar
[Cancelar] [Consolidar]
```

**`/consolidations/{id}`** (Resultado)

Mostra:
- Demonstrativo consolidado (tabela)
- Reconciliação (checklist)
- Eliminações aplicadas
- Validações
- Opções de download/export

---

### 2.8 Relatórios e Analytics

**`/analytics`**

Dashboards:
1. **Overview**
   - Total documentos processados
   - Taxa de acurácia
   - Contas classificadas
   - KPIs do sistema

2. **Learning Progress**
   - Acurácia ao longo do tempo (gráfico)
   - Regras criadas (tabela)
   - Padrões aprendidos (cloud tag)
   - Decisões por tipo

3. **Quality Metrics**
   - Distribuição de confiança (histograma)
   - Contas com problemas (tabela)
   - Validações falhando

4. **Data Explorer**
   - Tabela interativa com todos os dados
   - Filtros avançados
   - Exportar para Excel/CSV

---

## 3. Componentes Reutilizáveis

### 3.1 Tabelas

```tsx
<DataTable
  columns={[
    { key: 'name', label: 'Nome' },
    { key: 'confidence', label: 'Confiança', render: renderConfidenceBar }
  ]}
  data={items}
  pagination={{ page, pageSize }}
  onPageChange={setPage}
  filters={filterConfig}
  onFilterChange={setFilters}
/>
```

### 3.2 Abas/Tabs

```tsx
<Tabs>
  <Tab label="Extraction" icon={extractIcon}>
    <ExtractionResults />
  </Tab>
  <Tab label="Classification" icon={classifyIcon}>
    <ClassificationResults />
  </Tab>
  <Tab label="Calculations" icon={calcIcon}>
    <CalculationResults />
  </Tab>
</Tabs>
```

### 3.3 Alertas e Validações

```tsx
<Alert severity="error" title="Equação Desbalanceada">
  Ativo (R$ 1.5B) ≠ Passivo + PL (R$ 1.49B)
  <Button label="Ver Detalhes" />
</Alert>
```

### 3.4 Histórico e Auditoria

```tsx
<AuditTrail
  events={[
    { type: 'CREATED', timestamp: '14:30', user: 'analyst@...' },
    { type: 'CLASSIFIED', timestamp: '14:35', method: 'AI' },
    { type: 'APPROVED', timestamp: '14:40', user: 'reviewer@...' }
  ]}
/>
```

---

## 4. Estados de Componentes

### 4.1 Loading States

Skeleton Screens para cada tipo de conteúdo:
- Tabelas: shimmer effect em linhas
- Cards: cinza claro com animação
- Gráficos: placeholder com animação

### 4.2 Empty States

```
┌─────────────────────────────────┐
│                                 │
│          Nenhum item            │
│        (ícone amigável)          │
│                                 │
│   Você ainda não fez upload      │
│   de nenhum documento.           │
│                                 │
│      [Fazer Upload →]           │
│                                 │
└─────────────────────────────────┘
```

### 4.3 Error States

```
┌─────────────────────────────────┐
│  ⚠ Erro ao processar            │
│                                 │
│  Ocorreu um erro desconhecido   │
│  ao processar o documento.      │
│                                 │
│  Código: ERR_PDF_INVALID        │
│                                 │
│  [Tentar novamente] [Suporte]  │
│                                 │
└─────────────────────────────────┘
```

---

## 5. Fluxos de Usuário

### 5.1 Fluxo de Upload e Processamento

```
Login → Dashboard → Upload PDF
    ↓
    Processamento automático
    ↓
Classificações geradas
    ↓
Itens para revisão → Revisor aprova/override
    ↓
Cálculos executados
    ↓
Consolidação (opcional)
    ↓
Relatório final gerado
    ↓
Download/Export
```

### 5.2 Fluxo de Revisão

```
Dashboard → Fila de Revisão
    ↓
Selecionar item
    ↓
Ver sugestão + evidência
    ↓
Decidir: Aprovar? Alterar? Criar regra?
    ↓
Registrar decisão
    ↓
Sistema aprende
    ↓
Próximo item
```

---

## 6. Responsive Design

**Breakpoints:**
- Mobile: 320px - 640px
- Tablet: 641px - 1024px
- Desktop: 1025px+

**Considerações:**
- Sidebar colapsável em mobile
- Tabelas com scroll horizontal em mobile
- Botões grandes e touchable (48px mín)

---

## 7. Temas e Cores

**Paleta Principal:**
- Azul primário: #2563EB (confiança/sucesso)
- Laranja: #F97316 (atenção/processamento)
- Vermelho: #EF4444 (erro)
- Verde: #10B981 (aprovado)
- Cinza: #6B7280 (neutro/disabled)

---

## 8. Acessibilidade

- WCAG 2.1 AA compliance
- Suporte a leitores de tela
- Contraste adequado (4.5:1 para texto)
- Navegação por teclado
- ARIA labels em componentes

---

## 9. Critérios de Aceite

- ✅ Todas as páginas principais implementadas
- ✅ Componentes reutilizáveis funcionando
- ✅ Responsivo em mobile/tablet/desktop
- ✅ Acesso integrado com API
- ✅ Loading states apropriados
- ✅ Validações de form funcionando
- ✅ Acessibilidade básica implementada

---

## 10. Próximos Passos

1. Criar wireframes em Figma
2. Implementar componentes base
3. Integrar com APIs
4. Prosseguir com **09 - Plano de Implementação**


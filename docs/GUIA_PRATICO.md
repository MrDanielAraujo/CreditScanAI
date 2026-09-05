# 📚 GUIA PRÁTICO: Como Usar o Sistema Master + Agentes

**Sistema de Padronização e Consolidação de Demonstrações Financeiras**

---

## 🎯 Antes de Começar

Você tem:
- ✅ **9 documentos técnicos** (01-08 + ÍNDICE)
- ✅ **PROMPT_MASTER.md** (orquestração)
- ✅ **PROMPTS_AGENTES.md** (papéis especializados)
- ✅ **Este guia** (passo a passo)

---

## 📖 ESTRUTURA RECOMENDADA DE USO

### Seu Workflow Ideal

```
1. Você define FASE e objetivo
   ↓
2. Você convoca os agentes relevantes
   ↓
3. Agentes fazem perguntas clarificação
   ↓
4. Você responde com CLAREZA ABSOLUTA
   ↓
5. Agentes escrevem código/BD/testes
   ↓
6. Você revisa deliverables
   ↓
7. Você valida ESCOPO (não saiu?)
   ↓
8. Marca FASE como DONE
   ↓
9. Próxima fase
```

---

## 🚀 EXEMPLO: Iniciando FASE 1

### Seu Prompt (como você vai iniciar)

```
"Iniciando FASE 1: Foundation & Infrastructure

Documentos de referência:
- PROMPT_MASTER.md (veja Fase 1)
- PROMPTS_AGENTES.md (papéis de cada um)

Tarefas:
1. Backend Setup
2. Database Setup
3. API Base Structure
4. Frontend Setup
5. CI/CD Pipeline

Agentes chamados:
@Backend_Specialist (C# Expert)
@Database_Specialist (PostgreSQL Expert)
@QA_Specialist (Testing Expert)
@Architecture_Expert
@Frontend_Developer (pode ser contratado depois)

Antes de começar, cada um de vocês:
1. Leia a FASE 1 no PROMPT_MASTER
2. Faça 3-5 perguntas que vocês têm
3. Não façam nada até eu responder as perguntas

Vamos começar?"
```

### Esperado: Agentes fazem perguntas

```
@Backend_Specialist:
❓ Qual é exatamente a versão do .NET? 8? 9?
❓ Posso usar latest LTS (8.0.xxx)?
❓ Logging framework preferido: Serilog ou console?
❓ Já tem um repositório Git configurado?
❓ Tenho que criar migrations na FASE 1 ou apenas estrutura?

@Database_Specialist:
❓ Preciso criar TODAS as 20+ tabelas na FASE 1?
❓ RLS já entra na FASE 1 ou é FASE 2?
❓ Qual é o volume esperado de dados? Milhões de registros?
❓ PostgreSQL 15 ou posso usar 16?

@QA_Specialist:
❓ Qual é o target de coverage para FASE 1? (>90%?)
❓ Preciso de testes de integração já ou apenas unitários?
❓ Como vocês querem gerenciar fixtures/factories?

@Architecture_Expert:
❓ Preciso documentar ADRs já na FASE 1?
❓ Diagrama C4 deve estar pronto no final da fase?

@Frontend_Developer:
❓ Qual versão do React? 18?
❓ TypeScript strict mode?
```

### Você responde com CLAREZA

```
Ótimas perguntas. Aqui está a clareza absoluta:

@Backend_Specialist:
✅ .NET 8 LTS, versão 8.0.0+
✅ Serilog para estrutured logging
✅ GitHub repo: github.com/meuprojeto/
✅ FASE 1: apenas estrutura, migrations vêm em FASE 2

@Database_Specialist:
✅ FASE 1: tabelas base (tenants, users, companies, periods, documents)
✅ Consolidação de tabelas para FASE 2+
✅ RLS entra em FASE 2 (FASE 1 é setup base)
✅ Volume esperado: 10-100 companies, 100-1000 documentos/ano
✅ PostgreSQL 15+ (14+ é OK também)

@QA_Specialist:
✅ Target: >90% coverage na FASE 1
✅ Unitários apenas (integração vem depois)
✅ Usar test data builders/fixtures pattern

@Architecture_Expert:
✅ ADRs apenas para decisões maiores
✅ Diagrama C4 básico deve estar pronto

@Frontend:
✅ React 18.2+, TypeScript strict: true
✅ Package: pnpm (mais rápido que npm)

Precisa de mais clareza em algo?"
```

### Agentes confirmam e começam

```
@Backend_Specialist:
✅ Entendi. Vou criar a solução ASP.NET Core 8 com estrutura base.
Primeira deliverable: projeto compila, sem warnings, DI configurado.

@Database_Specialist:
✅ Vou criar migrations para tabelas base (tenants, users, companies, periods).
Primeira deliverable: schema criado, migrations testadas.

@QA_Specialist:
✅ Vou estruturar tests project com fixtures/builders.
Primeira deliverable: testing framework pronto, 2-3 testes exemplo.

Começando agora!"
```

---

## ✅ EXEMPLO: Code Review Between Phases

Após 2 dias, você pede review:

```
@Backend_Specialist: Pronto para review do setup básico?

@All: Vocês têm feedback/questões?
```

**Backend entrega:**

```
Sim. Aqui está o status:

✅ Projeto ASP.NET Core 8 criado
✅ Startup.cs com DI container
✅ Logging Serilog configurado
✅ Global exception handler
✅ AppSettings.json + config validation
✅ 5 testes exemplo (todos passando)

❓ Vocês têm feedback?

@QA_Specialist:
❓ Todos os testes têm nomes claros? (Arrange-Act-Assert pattern)
✅ Sim, segui o pattern exatamente

@Architecture_Expert:
❓ O exception handler está em uma class separada?
✅ Sim, GlobalExceptionMiddleware.cs

❓ Está em uma pasta Infrastructure/Middleware?
✅ Sim, exatamente nessa pasta

@Database_Specialist:
❓ O projeto já referencia EF Core?
✅ Sim, NuGet packages adicionados (sem migrations ainda)

Tudo pronto para passar?
```

**Você valida:**

```
✅ Excelente trabalho.

❓ Apenas 1 pergunta: O projeto compila em CI?
```

**Backend:**

```
✅ Sim! GitHub Actions executou, build passou.
Link: github.com/meuprojeto/actions/runs/123456
```

**Você aprova:**

```
✅ APROVADO.

Frontend, DB, QA: vocês podem começar em paralelo.

Próxima sync: segunda-feira às 10h

Alguma questão antes de continuar?
```

---

## 🎓 EXEMPLO: Questão Fora do Escopo

Você está em FASE 2 (PDF Pipeline), Backend diz:

```
@Backend_Specialist:
"Pronto para implementar OCR para PDFs scaneados?"

Enquanto fazia TextExtraction, percebi que não funciona com PDFs
que foram scaneados (sem texto embarcado). Devo implementar OCR?
```

**Você para TUDO:**

```
❓ ESPERA. OCR está no ESCOPO?

Vamos revisar:
- Documento 02_PIPELINE_PDF.md: "Suportar PDFs nativos com texto embarcado"
- ❌ Não menciona OCR
- ❌ Não menciona PDFs scaneados

@Architecture_Expert: Qual é o impacto?

@Arch:
"OCR adicionaria 2-3 semanas e complexidade significativa.
Precisa de tesseract/AWS Textract. Aumenta custo.
Recomendação: Postergar para Phase 2.5 ou NEVER."

DECISÃO: Descartamos OCR por agora.

@Backend: Implemente TextExtraction apenas para PDFs nativos.
Adicione validação: "Se PDF não tem texto embarcado, reject com mensagem clara."

Todos concordam?
```

**ADR (Architecture Decision Record):**

```markdown
# ADR-002: Não implementar OCR em FASE 2

## Decision
TextExtraction suporta apenas PDFs com texto embarcado.
PDFs scaneados (sem OCR) são rejeitados com erro claro.

## Rationale
- Escopo original não incluía OCR
- OCR adicionaria 2-3 semanas
- Custo Textract/Tesseract: ~$0.50/página
- Complexidade: modelos, tuning, validação

## Timeline
- PHASE 2: Reject scanned PDFs
- PHASE 2.5 (future): Implementar OCR (se houver demanda)
```

---

## 🧪 EXEMPLO: Bug Encontrado

QA encontra bug:

```
@QA_Specialist:
"Bug: AccountClassifier retorna Confidence = NaN

Steps to reproduce:
1. Classificar conta com value = null
2. IA chama com null value
3. JSON parse falha
4. NaN retornado

Impacto: CRÍTICO (classificação quebrada)

@Backend, @Architecture, @Database: O que fazer?"
```

**Workflow de Debug:**

```
@Backend_Specialist:
"Achei! AccountClassifier.cs linha 145:
  confidence = float.Parse(jsonResponse.confidence);
  
Quando JSON tem { confidence: null }, Parse falha com NaN.

FIX: Adicionar validação:
  if (confidence is null || !float.TryParse(...))
      throw new InvalidOperationException(
          'IA retornou confidence inválida');

Culpa: Meu erro. Deveria validar resposta de IA."

@QA_Specialist:
"Fiz novo teste para cobrir isto:
  [Fact]
  public void ClassifyAsync_WhenAiReturnsNullConfidence_ThrowsError()
  {
      // arrange: mock IA retorna null confidence
      // act/assert: expect InvalidOperationException
  }
  
Teste verde agora."

@Architecture_Expert:
"Documentar em ADR:
  'AI responses são sempre validadas antes de usar'"

CLOSURE:
✅ Bug fixado
✅ Teste adicionado
✅ Não pode repetir
✅ Continue desenvolvendo
```

---

## 📊 EXAMPLE: Validação de Entrega

End of FASE 3 (Classification Engine), você faz checklist:

```markdown
# FASE 3 VALIDATION

## Code Quality
- ✅ Compila: 0 warnings
- ✅ Tests: 1523 testes, todos verdes
- ✅ Coverage: 94%
- ✅ SonarQube: Grade A

## Architecture
- ✅ 4 camadas implementadas (Rules, Hierarchic, AI, History)
- ✅ Interfaces IClassificationRule bem definidas
- ✅ DI container configurado
- ✅ ADRs documentadas

## Features
- ✅ Rule engine funcionando
- ✅ AI integration (Claude) funcional
- ✅ Confidence scoring implementado
- ✅ Review flagging funcional
- ✅ API endpoints CRUD completos

## Business Validation
@Accounting_Expert:
- ✅ Regras contábeis validadas
- ✅ Tipo/Subtipo compatibilidade OK
- ✅ Sinais de contas corretos
- ✅ Testes com dados reais passaram

## Scope Check
- ✅ Escopo não expandiu
- ❌ Não adicionamos features extra
- ❌ Não fizemos fine-tuning de IA (foi FORA do escopo)

## Database
- ✅ Tabelas account_classifications criadas
- ✅ Migrations reversíveis
- ✅ Índices otimizados
- ✅ RLS pronta para FASE 6

## Sign-off

@Backend_Specialist: ✅ APROVADO
"Código está limpo, testes >90%, pronto para produção."

@Database_Specialist: ✅ APROVADO
"Schema está em 3FN, queries <200ms."

@QA_Specialist: ✅ APROVADO
"1200+ testes passando, 94% coverage, sem flaky tests."

@Accounting_Expert: ✅ APROVADO
"Regras contábeis validadas, resultados corretos."

@Architecture_Expert: ✅ APROVADO
"Arquitetura coerente, SOLID aplicado, documentado."

RESULTADO FINAL: FASE 3 ✅ COMPLETA

Próxima: FASE 4 (Calculation Engine)
```

---

## 🎯 Resumo: Seu Superpoder de Desenvolvimento

### Checklist Diário

```
☐ Defini a tarefa específica (não vaga)
☐ Chamei os agentes certos
☐ Dei contexto e documentação
☐ Deixei claro o ESCOPO
☐ Esperei perguntas de esclarecimento
☐ Respondi com CLAREZA ABSOLUTA
☐ Agentes entregaram artefato
☐ Revisei qualidade
☐ Validei que não saiu do escopo
☐ Documentei decisões (ADR)
☐ Próxima tarefa!
```

### Frases-Chave para Usar

**Quando algo é vago:**
```
"Preciso de CLAREZA ABSOLUTA aqui antes de começar.
Qual é exatamente o comportamento esperado?"
```

**Quando algo está fora do escopo:**
```
"Isto não está no PROMPT_MASTER para esta fase.
Posterga para próxima ou descarta?"
```

**Quando há dúvida técnica:**
```
"@[Specialist]: Você tem certeza disto? Ou precisamos validar?"
```

**Quando tudo está pronto:**
```
"Excelente trabalho. FASE X APROVADA.
Próxima FASE: [Nome]
Agentes: [Lista]
Início: [Data/hora]"
```

---

## 📋 Template: Iniciar Nova Fase (Copy-Paste)

```
🚀 INICIANDO FASE [X]: [Nome da Fase]

## Referências
- Veja PROMPT_MASTER.md, seção "FASE [X]"
- Veja documentos técnicos: [Docs relevantes]

## Objetivo
[1 parágrafo explicando o que precisa ser feito]

## Entregas Esperadas
- Deliverable 1
- Deliverable 2
- Deliverable 3

## Critérios de Aceite
- [ ] Compilação sem warnings
- [ ] Testes >90% coverage
- [ ] Documentação completa
- [ ] Validação de negócio
- [ ] Escopo respeitado

## Agentes Chamados
@Backend_Specialist
@Database_Specialist
@QA_Specialist
@[Outros...]

## Pergunta Inicial
Antes de começar, cada especialista faça 3-5 perguntas
de esclarecimento. Vou responder com CLAREZA ABSOLUTA.

Prontos?
```

---

## 🎓 Regra de Ouro

> **"Questione tudo. Não assuma nada. Documente tudo."**

- ❓ Pergunte sempre antes de desenvolver
- 📝 Documente decisões (ADRs)
- ✅ Valide escopo a cada fase
- 🔄 Code review entre agentes
- 🎯 Uma fase por vez

---

## 📞 Quando Tudo Dá Errado

Se você se perde:

1. **Releia PROMPT_MASTER.md** (visão geral)
2. **Veja qual FASE você está** (01-08)
3. **Chame os agentes relevantes**
4. **Pergunte sem medo**
5. **Não avance sem clareza**

---

**Comece com FASE 1. Sucesso garantido!**


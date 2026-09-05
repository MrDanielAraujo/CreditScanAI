import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { calculationsApi } from '../services/calculationsApi'
import { listCompanies } from '../services/documentsApi'
import type { CalculationResult, Period } from '../types/calculations'
import type { Company } from '../types/documents'

type ValueFormat = 'currency' | 'percent' | 'ratio'

interface ValueDefinition {
  key: string
  label: string
  format: ValueFormat
}

const BALANCO_VALUES: ValueDefinition[] = [
  { key: 'ATIVO_CIRCULANTE', label: 'Ativo Circulante', format: 'currency' },
  { key: 'ATIVO_NAO_CIRCULANTE', label: 'Ativo Não Circulante', format: 'currency' },
  { key: 'ATIVO_TOTAL', label: 'Ativo Total', format: 'currency' },
  { key: 'PASSIVO_CIRCULANTE', label: 'Passivo Circulante', format: 'currency' },
  { key: 'PASSIVO_NAO_CIRCULANTE', label: 'Passivo Não Circulante', format: 'currency' },
  { key: 'PASSIVO_TOTAL', label: 'Passivo Total', format: 'currency' },
  { key: 'PATRIMONIO_LIQUIDO', label: 'Patrimônio Líquido', format: 'currency' },
]

const DRE_VALUES: ValueDefinition[] = [
  { key: 'RECEITA_TOTAL', label: 'Receita Total', format: 'currency' },
  { key: 'CUSTO_TOTAL', label: 'Custo Total', format: 'currency' },
  { key: 'DESPESA_TOTAL', label: 'Despesa Total', format: 'currency' },
  { key: 'DEPRECIACAO_AMORTIZACAO_TOTAL', label: 'Depreciação e Amortização', format: 'currency' },
]

const INDICADORES: ValueDefinition[] = [
  { key: 'RESULTADO_PERIODO', label: 'Resultado do Período', format: 'currency' },
  { key: 'RESULTADO_ANTES_DEPRECIACAO_AMORTIZACAO', label: 'Resultado Antes de Depreciação e Amortização', format: 'currency' },
  { key: 'MARGEM_RESULTADO', label: 'Margem de Resultado', format: 'percent' },
  { key: 'LIQUIDEZ_CORRENTE', label: 'Liquidez Corrente', format: 'ratio' },
  { key: 'INDICE_ENDIVIDAMENTO', label: 'Índice de Endividamento', format: 'ratio' },
]

function formatValue(value: number | undefined, format: ValueFormat): string {
  if (value === undefined) return '—'
  if (format === 'currency') return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  if (format === 'percent') return `${value.toLocaleString('pt-BR', { maximumFractionDigits: 2 })}%`
  return value.toLocaleString('pt-BR', { maximumFractionDigits: 4 })
}

function periodLabel(period: Period): string {
  return period.periodType === 'Annual' ? `${period.year} (Anual)` : `${period.year} T${period.quarter}`
}

function ValueGrid({ title, values, result }: { title: string; values: ValueDefinition[]; result: CalculationResult }) {
  return (
    <div className="mt-6">
      <h2 className="text-sm font-semibold uppercase text-neutral">{title}</h2>
      <div className="mt-2 grid grid-cols-2 gap-3 md:grid-cols-4">
        {values.map((v) => (
          <div key={v.key} className="rounded-md border border-neutral/20 p-3">
            <p className="text-xs text-neutral">{v.label}</p>
            <p className="mt-1 text-lg font-semibold">{formatValue(result.values[v.key], v.format)}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

export function DashboardPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [companyId, setCompanyId] = useState('')

  const [periods, setPeriods] = useState<Period[]>([])
  const [periodId, setPeriodId] = useState('')

  const [result, setResult] = useState<CalculationResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [calculating, setCalculating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notCalculatedYet, setNotCalculatedYet] = useState(false)

  useEffect(() => {
    listCompanies()
      .then(setCompanies)
      .catch(() => setCompanies([]))
  }, [])

  useEffect(() => {
    setPeriods([])
    setPeriodId('')
    setResult(null)
    if (!companyId) return

    calculationsApi
      .listPeriods(companyId)
      .then(setPeriods)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar períodos'))
  }, [companyId])

  useEffect(() => {
    setResult(null)
    setNotCalculatedYet(false)
    if (!companyId || !periodId) return

    setLoading(true)
    setError(null)
    calculationsApi
      .getResults(companyId, periodId)
      .then(setResult)
      .catch(() => setNotCalculatedYet(true))
      .finally(() => setLoading(false))
  }, [companyId, periodId])

  const handleCalculate = async () => {
    if (!companyId || !periodId) return
    setCalculating(true)
    setError(null)
    try {
      const calculated = await calculationsApi.calculate(companyId, periodId)
      setResult(calculated)
      setNotCalculatedYet(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao calcular')
    } finally {
      setCalculating(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold">Dashboard</h1>
      <p className="mt-2 text-neutral">Totais do plano de contas e indicadores financeiros por empresa e período.</p>

      <div className="mt-4 flex flex-wrap items-end gap-3">
        <div className="w-64">
          <label className="block text-xs font-semibold uppercase text-neutral">Empresa</label>
          <select
            value={companyId}
            onChange={(e) => setCompanyId(e.target.value)}
            className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            <option value="">Selecione...</option>
            {companies.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>

        <div className="w-48">
          <label className="block text-xs font-semibold uppercase text-neutral">Período</label>
          <select
            value={periodId}
            onChange={(e) => setPeriodId(e.target.value)}
            disabled={periods.length === 0}
            className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            <option value="">{periods.length === 0 ? 'Sem períodos' : 'Selecione...'}</option>
            {periods.map((p) => (
              <option key={p.id} value={p.id}>
                {periodLabel(p)}
              </option>
            ))}
          </select>
        </div>

        <Button onClick={handleCalculate} loading={calculating} disabled={!periodId}>
          Calcular
        </Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}
      {loading && <p className="mt-6 text-sm text-neutral">Carregando...</p>}
      {!loading && notCalculatedYet && (
        <p className="mt-6 text-sm text-neutral">Ainda não há cálculo para esta empresa/período - clique em Calcular.</p>
      )}

      {!loading && result && (
        <div>
          <div className={['mt-6 inline-block rounded-md px-3 py-2 text-sm', result.equationBalanced ? 'bg-success/10 text-success' : 'bg-error/10 text-error'].join(' ')}>
            Equação Ativo = Passivo + PL: {result.equationBalanced ? 'balanceada' : `desbalanceada (diferença de ${formatValue(result.equationVariance, 'currency')})`}
          </div>

          <ValueGrid title="Balanço" values={BALANCO_VALUES} result={result} />
          <ValueGrid title="DRE" values={DRE_VALUES} result={result} />
          <ValueGrid title="Indicadores" values={INDICADORES} result={result} />
        </div>
      )}
    </div>
  )
}

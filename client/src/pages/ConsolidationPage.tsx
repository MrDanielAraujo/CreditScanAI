import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { FinancialValueGrid } from '../components/financial/FinancialValueGrid'
import { BALANCO_VALUES, DRE_VALUES, formatFinancialValue, INDICADORES } from '../components/financial/financialValueDefinitions'
import { companiesApi } from '../services/companiesApi'
import { consolidationApi } from '../services/consolidationApi'
import { listCompanies } from '../services/documentsApi'
import type { Period } from '../types/calculations'
import type { ConsolidationResult } from '../types/consolidation'
import type { Company } from '../types/documents'

function periodLabel(period: Period): string {
  return period.periodType === 'Annual' ? `${period.year} (Anual)` : `${period.year} T${period.quarter}`
}

export function ConsolidationPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [selectedCompanyIds, setSelectedCompanyIds] = useState<string[]>([])

  const [periods, setPeriods] = useState<Period[]>([])
  const [periodId, setPeriodId] = useState('')

  const [result, setResult] = useState<ConsolidationResult | null>(null)
  const [calculating, setCalculating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    listCompanies()
      .then(setCompanies)
      .catch(() => setCompanies([]))
  }, [])

  useEffect(() => {
    setPeriods([])
    setPeriodId('')
    setResult(null)
    if (selectedCompanyIds.length < 2) return

    Promise.all(selectedCompanyIds.map((id) => companiesApi.listCalculatedPeriods(id)))
      .then((periodsByCompany) => {
        // Só períodos que TODAS as empresas selecionadas têm dados.
        const [first, ...rest] = periodsByCompany
        const common = first.filter((p) => rest.every((others) => others.some((o) => o.id === p.id)))
        setPeriods(common)
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar períodos'))
  }, [selectedCompanyIds])

  const toggleCompany = (id: string) => {
    setSelectedCompanyIds((prev) => (prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id]))
  }

  const handleConsolidate = async () => {
    if (selectedCompanyIds.length < 2 || !periodId) return
    setCalculating(true)
    setError(null)
    setResult(null)
    try {
      const consolidated = await consolidationApi.calculate(periodId, selectedCompanyIds)
      setResult(consolidated)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao consolidar')
    } finally {
      setCalculating(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold">Consolidação</h1>
      <p className="mt-2 text-neutral">
        Soma os totais já calculados (Dashboard) de duas ou mais empresas para o mesmo período. Cada empresa precisa ter sido
        calculada antes.
      </p>

      <div className="mt-4 flex flex-wrap gap-8">
        <div>
          <label className="block text-xs font-semibold uppercase text-neutral">Empresas</label>
          <div className="mt-1 flex flex-col gap-1">
            {companies.map((c) => (
              <label key={c.id} className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={selectedCompanyIds.includes(c.id)} onChange={() => toggleCompany(c.id)} />
                {c.name}
              </label>
            ))}
          </div>
        </div>

        <div className="w-48">
          <label className="block text-xs font-semibold uppercase text-neutral">Período</label>
          <select
            value={periodId}
            onChange={(e) => setPeriodId(e.target.value)}
            disabled={periods.length === 0}
            className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            <option value="">{periods.length === 0 ? 'Selecione 2+ empresas' : 'Selecione...'}</option>
            {periods.map((p) => (
              <option key={p.id} value={p.id}>
                {periodLabel(p)}
              </option>
            ))}
          </select>

          <Button className="mt-3 w-full" onClick={handleConsolidate} loading={calculating} disabled={selectedCompanyIds.length < 2 || !periodId}>
            Consolidar
          </Button>
        </div>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      {result && (
        <div>
          <div className={['mt-6 inline-block rounded-md px-3 py-2 text-sm', result.equationBalanced ? 'bg-success/10 text-success' : 'bg-error/10 text-error'].join(' ')}>
            Equação Ativo = Passivo + PL: {result.equationBalanced ? 'balanceada' : `desbalanceada (diferença de ${formatFinancialValue(result.equationVariance, 'currency')})`}
          </div>

          <FinancialValueGrid title="Balanço Consolidado" definitions={BALANCO_VALUES} values={result.values} />
          <FinancialValueGrid title="DRE Consolidado" definitions={DRE_VALUES} values={result.values} />
          <FinancialValueGrid title="Indicadores Consolidados" definitions={INDICADORES} values={result.values} />

          <div className="mt-6">
            <h2 className="text-sm font-semibold uppercase text-neutral">Reconciliação</h2>
            <ul className="mt-2 flex flex-col gap-1 text-sm">
              {result.reconciliationChecks.map((check) => (
                <li key={check.checkId} className={check.passed ? 'text-success' : 'text-error'}>
                  {check.passed ? '✓' : '✗'} {check.description}
                  {!check.passed && check.errorMessage && ` — ${check.errorMessage}`}
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}

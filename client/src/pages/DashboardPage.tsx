import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { ScaleIcon, TrendingUpIcon } from '../components/common/icons'
import { HomeIcon, LayersIcon } from '../components/common/navIcons'
import { EquationBanner } from '../components/financial/EquationBanner'
import { ExportButtons } from '../components/financial/ExportButtons'
import { FinancialValueGrid } from '../components/financial/FinancialValueGrid'
import { BALANCO_VALUES, DRE_VALUES, INDICADORES } from '../components/financial/financialValueDefinitions'
import { calculationsApi } from '../services/calculationsApi'
import { listCompanies } from '../services/documentsApi'
import { reportsApi } from '../services/reportsApi'
import type { CalculationResult, Period } from '../types/calculations'
import type { Company } from '../types/documents'

function periodLabel(period: Period): string {
  return period.periodType === 'Annual' ? `${period.year} (Anual)` : `${period.year} T${period.quarter}`
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
  const [exportError, setExportError] = useState<string | null>(null)

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

  const handleExport = async (format: 'pdf' | 'xlsx') => {
    setExportError(null)
    try {
      await reportsApi.exportCompanyStatement(companyId, periodId, format)
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'Erro ao exportar')
    }
  }

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
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <HomeIcon className="h-6 w-6" />
        Dashboard
      </h1>
      <p className="mt-2 text-neutral">Totais do plano de contas e indicadores financeiros por empresa e período.</p>

      <div className="mt-4 rounded-lg border border-neutral/15 bg-surface p-4 shadow-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-64">
            <label className="block text-xs font-semibold uppercase text-neutral">Empresa</label>
            <select
              value={companyId}
              onChange={(e) => setCompanyId(e.target.value)}
              className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
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
              className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
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
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}
      {loading && <p className="mt-6 text-sm text-neutral">Carregando...</p>}
      {!loading && notCalculatedYet && (
        <p className="mt-6 text-sm text-neutral">Ainda não há cálculo para esta empresa/período - clique em Calcular.</p>
      )}

      {!loading && result && (
        <div className="mt-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <EquationBanner balanced={result.equationBalanced} variance={result.equationVariance} />
            <ExportButtons onExportPdf={() => handleExport('pdf')} onExportExcel={() => handleExport('xlsx')} />
          </div>

          {exportError && <p className="mt-2 text-sm text-error">{exportError}</p>}

          <FinancialValueGrid title="Balanço" definitions={BALANCO_VALUES} values={result.values} icon={ScaleIcon} accent="blue" />
          <FinancialValueGrid title="DRE" definitions={DRE_VALUES} values={result.values} icon={LayersIcon} accent="violet" />
          <FinancialValueGrid title="Indicadores" definitions={INDICADORES} values={result.values} icon={TrendingUpIcon} accent="amber" />
        </div>
      )}
    </div>
  )
}

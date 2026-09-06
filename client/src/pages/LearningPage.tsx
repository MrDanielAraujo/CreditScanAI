import { useEffect, useState } from 'react'
import { learningApi } from '../services/learningApi'
import type { LearningStats } from '../types/learning'

const REVIEW_STATUS_LABELS: Record<string, string> = {
  Pending: 'Auto-aprovada (confiança alta)',
  NeedsReview: 'Precisa revisão',
  Approved: 'Aprovada por humano',
  Overridden: 'Corrigida por humano',
  Rejected: 'Rejeitada',
}

const METHOD_LABELS: Record<string, string> = {
  HISTORICAL_DECISION: 'Histórico da empresa',
  CROSS_COMPANY_PATTERN: 'Padrão entre empresas',
  EXACT_MATCH: 'Regra (correspondência exata)',
  PATTERN_MATCH: 'Regra (padrão)',
  AI: 'IA local',
  AI_UNAVAILABLE: 'IA indisponível',
  UNKNOWN: 'Sem correspondência',
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border border-neutral/20 p-4">
      <p className="text-xs font-semibold uppercase text-neutral">{label}</p>
      <p className="mt-1 text-2xl font-semibold">{value}</p>
    </div>
  )
}

function BreakdownTable({ title, data, labels }: { title: string; data: Record<string, number>; labels: Record<string, string> }) {
  const entries = Object.entries(data).sort(([, a], [, b]) => b - a)
  return (
    <div>
      <h2 className="text-sm font-semibold uppercase text-neutral">{title}</h2>
      <table className="mt-2 w-full max-w-md border-collapse text-sm">
        <tbody>
          {entries.length === 0 && (
            <tr>
              <td className="p-2 text-neutral">Sem dados ainda.</td>
            </tr>
          )}
          {entries.map(([key, count]) => (
            <tr key={key} className="border-b border-neutral/10">
              <td className="p-2">{labels[key] ?? key}</td>
              <td className="p-2 text-right font-medium">{count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function TopAccountsList({ title, items }: { title: string; items: { sourceAccountName: string; count: number }[] }) {
  return (
    <div>
      <h2 className="text-sm font-semibold uppercase text-neutral">{title}</h2>
      {items.length === 0 && <p className="mt-2 text-sm text-neutral">Nenhuma até agora.</p>}
      {items.length > 0 && (
        <ol className="mt-2 flex flex-col gap-1 text-sm">
          {items.map((item) => (
            <li key={item.sourceAccountName} className="flex justify-between border-b border-neutral/10 p-1">
              <span>{item.sourceAccountName}</span>
              <span className="text-neutral">{item.count}x</span>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}

export function LearningPage() {
  const [stats, setStats] = useState<LearningStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    learningApi
      .getStats()
      .then(setStats)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar estatísticas'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <h1 className="text-2xl font-semibold">Dashboard de Aprendizado</h1>
      <p className="mt-2 text-neutral">
        Contagens reais de classificações, decisões humanas e padrões aprendidos entre empresas - sem métricas inventadas.
      </p>

      {loading && <p className="mt-6 text-sm text-neutral">Carregando...</p>}
      {error && <p className="mt-6 text-sm text-error">{error}</p>}

      {!loading && stats && (
        <div>
          <div className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
            <StatCard label="Total de Classificações" value={String(stats.totalClassifications)} />
            <StatCard label="Revisadas por Humano" value={String(stats.reviewedCount)} />
            <StatCard
              label="Taxa de Aprovação"
              value={stats.approvalRate === null ? '—' : `${(stats.approvalRate * 100).toFixed(0)}%`}
            />
            <StatCard
              label="Padrões Entre Empresas Usados"
              value={String(stats.byMethod['CROSS_COMPANY_PATTERN'] ?? 0)}
            />
          </div>

          <div className="mt-8 grid grid-cols-1 gap-8 md:grid-cols-2">
            <BreakdownTable title="Por Status de Revisão" data={stats.byReviewStatus} labels={REVIEW_STATUS_LABELS} />
            <BreakdownTable title="Por Método de Classificação" data={stats.byMethod} labels={METHOD_LABELS} />
          </div>

          <div className="mt-8 grid grid-cols-1 gap-8 md:grid-cols-2">
            <TopAccountsList title="Contas Mais Corrigidas (Override)" items={stats.topOverriddenAccounts} />
            <TopAccountsList title="Contas Mais Rejeitadas" items={stats.topRejectedAccounts} />
          </div>
        </div>
      )}
    </div>
  )
}

import type { ComponentType, SVGProps } from 'react'
import { useEffect, useState } from 'react'
import { accentBadgeClasses, accentTopBorderClasses, type Accent } from '../components/common/accentColors'
import { CheckCircleIcon, TrashIcon, TrendingUpIcon } from '../components/common/icons'
import { BookmarkIcon, CheckSquareIcon, GridIcon, LayersIcon, LightbulbIcon, TagIcon } from '../components/common/navIcons'
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

type IconType = ComponentType<SVGProps<SVGSVGElement>>

function CardShell({ icon: Icon, accent, title, children }: { icon: IconType; accent: Accent; title: string; children: React.ReactNode }) {
  return (
    <div
      className={[
        'min-w-0 rounded-lg border border-t-4 border-neutral/15 bg-surface p-4 shadow-sm transition-shadow hover:shadow-md',
        accentTopBorderClasses[accent],
      ].join(' ')}
    >
      <div className="flex items-center gap-2">
        <span className={['flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', accentBadgeClasses[accent]].join(' ')}>
          <Icon className="h-[18px] w-[18px]" />
        </span>
        <h2 className="truncate text-xs font-semibold uppercase tracking-wide text-neutral" title={title}>
          {title}
        </h2>
      </div>
      {children}
    </div>
  )
}

function StatCard({ label, value, icon, accent }: { label: string; value: string; icon: IconType; accent: Accent }) {
  return (
    <CardShell icon={icon} accent={accent} title={label}>
      <p className="mt-2 break-words text-2xl font-bold tabular-nums text-surface-dark">{value}</p>
    </CardShell>
  )
}

function BreakdownTable({
  title,
  data,
  labels,
  icon,
  accent,
}: {
  title: string
  data: Record<string, number>
  labels: Record<string, string>
  icon: IconType
  accent: Accent
}) {
  const entries = Object.entries(data).sort(([, a], [, b]) => b - a)
  return (
    <CardShell icon={icon} accent={accent} title={title}>
      <table className="mt-2 w-full border-collapse text-sm">
        <tbody>
          {entries.length === 0 && (
            <tr>
              <td className="p-2 text-neutral">Sem dados ainda.</td>
            </tr>
          )}
          {entries.map(([key, count]) => (
            <tr key={key} className="border-b border-neutral/10 last:border-0">
              <td className="p-2">{labels[key] ?? key}</td>
              <td className="p-2 text-right font-medium tabular-nums">{count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </CardShell>
  )
}

function TopAccountsList({
  title,
  items,
  icon,
  accent,
}: {
  title: string
  items: { sourceAccountName: string; count: number }[]
  icon: IconType
  accent: Accent
}) {
  return (
    <CardShell icon={icon} accent={accent} title={title}>
      {items.length === 0 && <p className="mt-2 text-sm text-neutral">Nenhuma até agora.</p>}
      {items.length > 0 && (
        <ol className="mt-2 flex flex-col gap-1 text-sm">
          {items.map((item) => (
            <li key={item.sourceAccountName} className="flex justify-between gap-2 border-b border-neutral/10 p-1 last:border-0">
              <span className="truncate">{item.sourceAccountName}</span>
              <span className="shrink-0 tabular-nums text-neutral">{item.count}x</span>
            </li>
          ))}
        </ol>
      )}
    </CardShell>
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
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <LightbulbIcon className="h-6 w-6" />
        Dashboard de Aprendizado
      </h1>
      <p className="mt-2 text-neutral">
        Contagens reais de classificações, decisões humanas e padrões aprendidos entre empresas - sem métricas inventadas.
      </p>

      {loading && <p className="mt-6 text-sm text-neutral">Carregando...</p>}
      {error && <p className="mt-6 text-sm text-error">{error}</p>}

      {!loading && stats && (
        <div>
          <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Total de Classificações" value={String(stats.totalClassifications)} icon={GridIcon} accent="blue" />
            <StatCard label="Revisadas por Humano" value={String(stats.reviewedCount)} icon={CheckSquareIcon} accent="violet" />
            <StatCard
              label="Taxa de Aprovação"
              value={stats.approvalRate === null ? '—' : `${(stats.approvalRate * 100).toFixed(0)}%`}
              icon={CheckCircleIcon}
              accent="emerald"
            />
            <StatCard
              label="Padrões Entre Empresas Usados"
              value={String(stats.byMethod['CROSS_COMPANY_PATTERN'] ?? 0)}
              icon={TrendingUpIcon}
              accent="amber"
            />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-2">
            <BreakdownTable title="Por Status de Revisão" data={stats.byReviewStatus} labels={REVIEW_STATUS_LABELS} icon={LayersIcon} accent="blue" />
            <BreakdownTable title="Por Método de Classificação" data={stats.byMethod} labels={METHOD_LABELS} icon={TagIcon} accent="violet" />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-2">
            <TopAccountsList title="Contas Mais Corrigidas (Override)" items={stats.topOverriddenAccounts} icon={BookmarkIcon} accent="amber" />
            <TopAccountsList title="Contas Mais Rejeitadas" items={stats.topRejectedAccounts} icon={TrashIcon} accent="emerald" />
          </div>
        </div>
      )}
    </div>
  )
}

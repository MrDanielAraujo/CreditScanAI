import { AlertTriangleIcon, CheckCircleIcon } from '../common/icons'
import { formatFinancialValue } from './financialValueDefinitions'

interface EquationBannerProps {
  balanced: boolean
  variance: number
}

export function EquationBanner({ balanced, variance }: EquationBannerProps) {
  return (
    <div
      className={[
        'inline-flex items-start gap-2 rounded-lg px-4 py-2 text-sm font-medium',
        balanced ? 'bg-success/10 text-success' : 'bg-error/10 text-error',
      ].join(' ')}
    >
      <span className="mt-0.5 shrink-0">{balanced ? <CheckCircleIcon /> : <AlertTriangleIcon />}</span>
      Equação Ativo = Passivo + PL: {balanced ? 'balanceada' : `desbalanceada (diferença de ${formatFinancialValue(variance, 'currency')})`}
    </div>
  )
}

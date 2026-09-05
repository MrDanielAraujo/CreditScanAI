export type ValueFormat = 'currency' | 'percent' | 'ratio'

export interface ValueDefinition {
  key: string
  label: string
  format: ValueFormat
}

export const BALANCO_VALUES: ValueDefinition[] = [
  { key: 'ATIVO_CIRCULANTE', label: 'Ativo Circulante', format: 'currency' },
  { key: 'ATIVO_NAO_CIRCULANTE', label: 'Ativo Não Circulante', format: 'currency' },
  { key: 'ATIVO_TOTAL', label: 'Ativo Total', format: 'currency' },
  { key: 'PASSIVO_CIRCULANTE', label: 'Passivo Circulante', format: 'currency' },
  { key: 'PASSIVO_NAO_CIRCULANTE', label: 'Passivo Não Circulante', format: 'currency' },
  { key: 'PASSIVO_TOTAL', label: 'Passivo Total', format: 'currency' },
  { key: 'PATRIMONIO_LIQUIDO', label: 'Patrimônio Líquido', format: 'currency' },
]

export const DRE_VALUES: ValueDefinition[] = [
  { key: 'RECEITA_TOTAL', label: 'Receita Total', format: 'currency' },
  { key: 'CUSTO_TOTAL', label: 'Custo Total', format: 'currency' },
  { key: 'DESPESA_TOTAL', label: 'Despesa Total', format: 'currency' },
  { key: 'DEPRECIACAO_AMORTIZACAO_TOTAL', label: 'Depreciação e Amortização', format: 'currency' },
]

export const INDICADORES: ValueDefinition[] = [
  { key: 'RESULTADO_PERIODO', label: 'Resultado do Período', format: 'currency' },
  { key: 'RESULTADO_ANTES_DEPRECIACAO_AMORTIZACAO', label: 'Resultado Antes de Depreciação e Amortização', format: 'currency' },
  { key: 'MARGEM_RESULTADO', label: 'Margem de Resultado', format: 'percent' },
  { key: 'LIQUIDEZ_CORRENTE', label: 'Liquidez Corrente', format: 'ratio' },
  { key: 'INDICE_ENDIVIDAMENTO', label: 'Índice de Endividamento', format: 'ratio' },
]

export function formatFinancialValue(value: number | undefined, format: ValueFormat): string {
  if (value === undefined) return '—'
  if (format === 'currency') return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  if (format === 'percent') return `${value.toLocaleString('pt-BR', { maximumFractionDigits: 2 })}%`
  return value.toLocaleString('pt-BR', { maximumFractionDigits: 4 })
}

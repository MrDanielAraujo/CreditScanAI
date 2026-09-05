export interface Period {
  id: string
  periodType: 'Annual' | 'Quarterly'
  year: number
  quarter: number | null
  endDate: string
}

export interface CalculationResult {
  companyId: string
  periodId: string
  values: Record<string, number>
  equationBalanced: boolean
  equationVariance: number
  calculatedAt: string
}

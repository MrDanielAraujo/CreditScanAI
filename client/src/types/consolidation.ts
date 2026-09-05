export interface ReconciliationCheck {
  checkId: string
  description: string
  passed: boolean
  expectedValue: number
  actualValue: number
  variance: number
  errorMessage: string | null
}

export interface ConsolidationResult {
  periodId: string
  companyIds: string[]
  values: Record<string, number>
  equationBalanced: boolean
  equationVariance: number
  reconciliationChecks: ReconciliationCheck[]
  isValid: boolean
}

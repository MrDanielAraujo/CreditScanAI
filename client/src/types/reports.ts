export interface PeriodEquationStatus {
  periodId: string
  periodLabel: string
  equationBalanced: boolean | null
  equationVariance: number | null
}

export interface QualityReport {
  documentId: string
  fileName: string
  totalClassifiedAccounts: number
  pendingCount: number
  needsReviewCount: number
  approvedCount: number
  overriddenCount: number
  rejectedCount: number
  averageConfidence: number | null
  periodEquationStatus: PeriodEquationStatus[]
}

export interface TopCorrectedAccount {
  sourceAccountName: string
  count: number
}

export interface LearningStats {
  totalClassifications: number
  byReviewStatus: Record<string, number>
  byMethod: Record<string, number>
  reviewedCount: number
  approvalRate: number | null
  topOverriddenAccounts: TopCorrectedAccount[]
  topRejectedAccounts: TopCorrectedAccount[]
}

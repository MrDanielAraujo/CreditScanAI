export type ClassificationReviewStatus = 'Pending' | 'NeedsReview' | 'Approved' | 'Rejected' | 'Overridden'

export interface PendingClassification {
  classificationId: string
  documentId: string
  sourceAccountName: string
  suggestedStandardAccountName: string | null
  confidenceScore: number
  classificationMethod: string
  evidence: string | null
}

export interface PendingClassificationsResponse {
  items: PendingClassification[]
  totalCount: number
  hasMore: boolean
}

export interface SourceAccountSummary {
  id: string
  originalName: string
  normalizedName: string | null
  hierarchyLevel: number
  inferredType: string | null
  inferredSubtype: string | null
}

export interface StandardAccountSummary {
  id: string
  code: string
  name: string
  description: string | null
}

export interface ClassificationDetail {
  classificationId: string
  chartOfAccountsId: string
  sourceAccount: SourceAccountSummary
  suggestedStandardAccount: StandardAccountSummary | null
  confidenceScore: number
  classificationMethod: string
  evidence: string | null
  reviewStatus: ClassificationReviewStatus
  createdAt: string
}

export interface ApproveClassificationRequest {
  notes?: string | null
}

export interface ApproveClassificationResponse {
  classificationId: string
  reviewStatus: string
  reviewedAt: string
}

export interface RejectClassificationRequest {
  reason?: string | null
}

export interface RejectClassificationResponse {
  classificationId: string
  reviewStatus: string
  reviewedAt: string
}

export interface OverrideClassificationRequest {
  newStandardAccountId: string
  reason?: string | null
}

export interface OverrideClassificationResponse {
  classificationId: string
  reviewStatus: string
  newStandardAccount: StandardAccountSummary
  reviewedAt: string
}

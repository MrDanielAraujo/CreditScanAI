export type DocumentType = 'BalanceSheet' | 'IncomeStatement'

export type ExtractionStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed'

export type ClassificationStatus = 'NotStarted' | 'AwaitingDefaultChartOfAccounts' | 'Processing' | 'Completed' | 'Failed'

export interface Company {
  id: string
  code: string
  name: string
  legalName: string | null
  cnpj: string | null
  industry: string | null
  fiscalYearEnd: string | null
  reportingCurrency: string
}

export interface UpsertCompanyRequest {
  code: string
  name: string
  legalName?: string | null
  cnpj?: string | null
  industry?: string | null
  fiscalYearEnd?: string | null
  reportingCurrency?: string | null
}

export interface UploadDocumentResponse {
  documentId: string
  status: ExtractionStatus
}

export interface DocumentStatusResponse {
  documentId: string
  status: ExtractionStatus
  extractionStartedAt: string | null
  extractionCompletedAt: string | null
  extractionError: string | null
}

export interface PeriodDto {
  id: string
  periodType: 'Annual' | 'Quarterly'
  year: number
  quarter: number | null
  endDate: string
}

export interface AccountValueDto {
  periodId: string | null
  rawColumnLabel: string | null
  rawValue: number | null
  scaleFactor: number
}

export interface SourceAccountDto {
  id: string
  parentId: string | null
  originalName: string
  hierarchyLevel: number
  inferredType: string | null
  inferredSubtype: string | null
  values: AccountValueDto[]
}

export interface DocumentResultResponse {
  documentId: string
  status: ExtractionStatus
  periods: PeriodDto[]
  accounts: SourceAccountDto[]
}

export interface DocumentListItem {
  id: string
  fileName: string
  companyId: string
  companyName: string
  documentType: DocumentType
  uploadDate: string
  extractionStatus: ExtractionStatus
  classificationStatus: ClassificationStatus
}

export interface DocumentListResponse {
  items: DocumentListItem[]
  totalCount: number
  hasMore: boolean
}

export interface ReprocessDocumentResponse {
  documentId: string
  classificationStatus: ClassificationStatus
}

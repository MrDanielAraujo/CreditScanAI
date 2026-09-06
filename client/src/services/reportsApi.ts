import { apiGet } from './apiClient'
import type { QualityReport } from '../types/reports'

export const reportsApi = {
  getQualityReport: (documentId: string) => apiGet<QualityReport>(`/api/reports/quality/${documentId}`),
}

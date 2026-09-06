import { apiDownload, apiGet } from './apiClient'
import type { QualityReport } from '../types/reports'

export type ExportFormat = 'pdf' | 'xlsx'

export const reportsApi = {
  getQualityReport: (documentId: string) => apiGet<QualityReport>(`/api/reports/quality/${documentId}`),

  exportCompanyStatement: (companyId: string, periodId: string, format: ExportFormat) =>
    apiDownload(
      `/api/reports/financial-statement/company/${companyId}/periods/${periodId}?format=${format}`,
      `demonstrativo.${format}`,
    ),

  exportConsolidatedStatement: (periodId: string, companyIds: string[], format: ExportFormat) => {
    const query = companyIds.map((id) => `companyIds=${id}`).join('&')
    return apiDownload(
      `/api/reports/financial-statement/consolidated?periodId=${periodId}&${query}&format=${format}`,
      `demonstrativo_consolidado.${format}`,
    )
  },
}

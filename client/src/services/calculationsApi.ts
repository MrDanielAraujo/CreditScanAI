import { apiGet, apiPost } from './apiClient'
import type { CalculationResult, Period } from '../types/calculations'

export const calculationsApi = {
  listPeriods: (companyId: string) => apiGet<Period[]>(`/api/companies/${companyId}/periods`),
  calculate: (companyId: string, periodId: string) =>
    apiPost<CalculationResult>(`/api/calculations/companies/${companyId}/periods/${periodId}/calculate`),
  getResults: (companyId: string, periodId: string) =>
    apiGet<CalculationResult>(`/api/calculations/companies/${companyId}/periods/${periodId}`),
}

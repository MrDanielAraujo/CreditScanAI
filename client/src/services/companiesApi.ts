import { apiDelete, apiGet, apiPost, apiPut } from './apiClient'
import type { Period } from '../types/calculations'
import type { Company, UpsertCompanyRequest } from '../types/documents'

export const companiesApi = {
  list: () => apiGet<Company[]>('/api/companies'),
  create: (req: UpsertCompanyRequest) => apiPost<Company>('/api/companies', req),
  update: (id: string, req: UpsertCompanyRequest) => apiPut<Company>(`/api/companies/${id}`, req),
  remove: (id: string) => apiDelete<object>(`/api/companies/${id}`),
  listCalculatedPeriods: (companyId: string) => apiGet<Period[]>(`/api/companies/${companyId}/calculated-periods`),
}

import { apiPost } from './apiClient'
import type { ConsolidationResult } from '../types/consolidation'

export const consolidationApi = {
  calculate: (periodId: string, companyIds: string[]) =>
    apiPost<ConsolidationResult>('/api/consolidation/calculate', { periodId, companyIds }),
}

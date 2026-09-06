import { apiGet, apiPost } from './apiClient'
import type {
  ApproveClassificationRequest,
  ApproveClassificationResponse,
  ClassificationDetail,
  OverrideClassificationRequest,
  OverrideClassificationResponse,
  PendingClassificationsResponse,
  RejectClassificationRequest,
  RejectClassificationResponse,
} from '../types/classifications'

export interface ListPendingParams {
  documentId?: string
  companyId?: string
  /** Omitido (ou 'needs_review') = só fila de revisão clássica. 'all' = qualquer status. */
  status?: 'needs_review' | 'all'
  limit?: number
  offset?: number
}

export const classificationsApi = {
  listPending: (params: ListPendingParams = {}) => {
    const query = new URLSearchParams()
    if (params.documentId) query.set('documentId', params.documentId)
    if (params.companyId) query.set('companyId', params.companyId)
    if (params.status) query.set('status', params.status)
    if (params.limit) query.set('limit', String(params.limit))
    if (params.offset) query.set('offset', String(params.offset))
    const qs = query.toString()
    return apiGet<PendingClassificationsResponse>(`/api/classifications/pending${qs ? `?${qs}` : ''}`)
  },
  getById: (id: string) => apiGet<ClassificationDetail>(`/api/classifications/${id}`),
  approve: (id: string, req: ApproveClassificationRequest) =>
    apiPost<ApproveClassificationResponse>(`/api/classifications/${id}/approve`, req),
  reject: (id: string, req: RejectClassificationRequest) =>
    apiPost<RejectClassificationResponse>(`/api/classifications/${id}/reject`, req),
  override: (id: string, req: OverrideClassificationRequest) =>
    apiPost<OverrideClassificationResponse>(`/api/classifications/${id}/override`, req),
}

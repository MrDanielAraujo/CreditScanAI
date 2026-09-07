import { apiGet, apiPost, API_BASE_URL } from './apiClient'
import { getStoredToken } from './authStorage'
import type {
  Company,
  DocumentListResponse,
  DocumentResultResponse,
  DocumentStatusResponse,
  DocumentType,
  ReprocessDocumentResponse,
  UploadDocumentResponse,
} from '../types/documents'
import type { ApiResponse } from '../types/api'

export async function listCompanies(): Promise<Company[]> {
  return apiGet<Company[]>('/api/companies')
}

export async function uploadDocument(file: File, cnpj: string): Promise<UploadDocumentResponse> {
  const form = new FormData()
  form.append('file', file)
  form.append('cnpj', cnpj)

  const token = getStoredToken()
  const response = await fetch(`${API_BASE_URL}/api/documents/upload`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    body: form,
  })

  const body: ApiResponse<UploadDocumentResponse> = await response.json()
  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `HTTP ${response.status}`)
  }
  return body.data as UploadDocumentResponse
}

export async function getDocumentStatus(documentId: string): Promise<DocumentStatusResponse> {
  return apiGet<DocumentStatusResponse>(`/api/documents/${documentId}/status`)
}

export async function getDocumentResult(documentId: string): Promise<DocumentResultResponse> {
  return apiGet<DocumentResultResponse>(`/api/documents/${documentId}/result`)
}

export interface ListDocumentsParams {
  companyId?: string
  documentType?: DocumentType
  search?: string
  limit?: number
  offset?: number
}

export async function listDocuments(params: ListDocumentsParams = {}): Promise<DocumentListResponse> {
  const query = new URLSearchParams()
  if (params.companyId) query.set('companyId', params.companyId)
  if (params.documentType) query.set('documentType', params.documentType)
  if (params.search) query.set('search', params.search)
  if (params.limit) query.set('limit', String(params.limit))
  if (params.offset) query.set('offset', String(params.offset))
  const qs = query.toString()
  return apiGet<DocumentListResponse>(`/api/documents${qs ? `?${qs}` : ''}`)
}

export async function reprocessDocument(documentId: string): Promise<ReprocessDocumentResponse> {
  return apiPost<ReprocessDocumentResponse>(`/api/documents/${documentId}/reprocess`)
}

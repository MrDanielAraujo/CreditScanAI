import { apiGet, API_BASE_URL } from './apiClient'
import type {
  Company,
  DocumentResultResponse,
  DocumentStatusResponse,
  DocumentType,
  UploadDocumentResponse,
} from '../types/documents'
import type { ApiResponse } from '../types/api'

export async function listCompanies(): Promise<Company[]> {
  return apiGet<Company[]>('/api/companies')
}

export async function uploadDocument(
  file: File,
  companyId: string,
  documentType: DocumentType,
): Promise<UploadDocumentResponse> {
  const form = new FormData()
  form.append('file', file)
  form.append('companyId', companyId)
  form.append('documentType', documentType)

  const response = await fetch(`${API_BASE_URL}/api/documents/upload`, {
    method: 'POST',
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

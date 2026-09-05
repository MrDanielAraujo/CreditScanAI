import type {
  ApiResponse,
} from '../types/api'
import type {
  Company,
  DocumentResultResponse,
  DocumentStatusResponse,
  DocumentType,
  UploadDocumentResponse,
} from '../types/documents'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

async function unwrap<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json()

  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `HTTP ${response.status}`)
  }

  return body.data as T
}

export async function listCompanies(): Promise<Company[]> {
  const response = await fetch(`${API_BASE_URL}/api/companies`)
  return unwrap<Company[]>(response)
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

  return unwrap<UploadDocumentResponse>(response)
}

export async function getDocumentStatus(documentId: string): Promise<DocumentStatusResponse> {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/status`)
  return unwrap<DocumentStatusResponse>(response)
}

export async function getDocumentResult(documentId: string): Promise<DocumentResultResponse> {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/result`)
  return unwrap<DocumentResultResponse>(response)
}

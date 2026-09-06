import type { ApiResponse } from '../types/api'
import { getStoredToken } from './authStorage'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

async function unwrap<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json()

  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `HTTP ${response.status}`)
  }

  return body.data as T
}

function authHeaders(): Record<string, string> {
  const token = getStoredToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function apiGet<T>(path: string): Promise<T> {
  return unwrap<T>(await fetch(`${API_BASE_URL}${path}`, { headers: authHeaders() }))
}

export async function apiPost<T>(path: string, payload?: unknown): Promise<T> {
  return unwrap<T>(
    await fetch(`${API_BASE_URL}${path}`, {
      method: 'POST',
      headers: { ...authHeaders(), ...(payload === undefined ? {} : { 'Content-Type': 'application/json' }) },
      body: payload === undefined ? undefined : JSON.stringify(payload),
    }),
  )
}

export async function apiPut<T>(path: string, payload: unknown): Promise<T> {
  return unwrap<T>(
    await fetch(`${API_BASE_URL}${path}`, {
      method: 'PUT',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    }),
  )
}

export async function apiDelete<T>(path: string): Promise<T> {
  return unwrap<T>(await fetch(`${API_BASE_URL}${path}`, { method: 'DELETE', headers: authHeaders() }))
}

function filenameFromContentDisposition(header: string | null, fallback: string): string {
  const match = header?.match(/filename="?([^"]+)"?/)
  return match?.[1] ?? fallback
}

/** Baixa um arquivo binário (PDF/Excel) autenticado - fetch normal não anexa o token, um <a href> puro também não. */
export async function apiDownload(path: string, fallbackFilename: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${path}`, { headers: authHeaders() })

  if (!response.ok) {
    const body: ApiResponse<unknown> = await response.json()
    throw new Error(body.error?.message ?? `HTTP ${response.status}`)
  }

  const blob = await response.blob()
  const filename = filenameFromContentDisposition(response.headers.get('Content-Disposition'), fallbackFilename)

  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

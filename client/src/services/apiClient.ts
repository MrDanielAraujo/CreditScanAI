import type { ApiResponse } from '../types/api'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

async function unwrap<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json()

  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `HTTP ${response.status}`)
  }

  return body.data as T
}

export async function apiGet<T>(path: string): Promise<T> {
  return unwrap<T>(await fetch(`${API_BASE_URL}${path}`))
}

export async function apiPost<T>(path: string, payload?: unknown): Promise<T> {
  return unwrap<T>(
    await fetch(`${API_BASE_URL}${path}`, {
      method: 'POST',
      headers: payload === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: payload === undefined ? undefined : JSON.stringify(payload),
    }),
  )
}

export async function apiPut<T>(path: string, payload: unknown): Promise<T> {
  return unwrap<T>(
    await fetch(`${API_BASE_URL}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    }),
  )
}

export async function apiDelete<T>(path: string): Promise<T> {
  return unwrap<T>(await fetch(`${API_BASE_URL}${path}`, { method: 'DELETE' }))
}

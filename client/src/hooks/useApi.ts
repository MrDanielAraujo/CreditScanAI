import { useCallback, useEffect, useRef, useState } from 'react'
import type { ApiError } from '../types/api'

interface UseApiState<T> {
  data: T | null
  loading: boolean
  error: ApiError | null
}

/**
 * Generic hook for API calls with loading/error state, following the
 * ApiResponse<T> envelope defined in 07_ESPECIFICACAO_APIS.md.
 */
export function useApi<T>(url: string, options?: RequestInit) {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: true,
    error: null,
  })

  // Kept in a ref (not the useCallback deps) so passing a fresh object
  // literal as `options` on every render doesn't retrigger the fetch.
  const optionsRef = useRef(options)
  useEffect(() => {
    optionsRef.current = options
  }, [options])

  const fetchData = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }))

    try {
      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          ...optionsRef.current?.headers,
        },
        ...optionsRef.current,
      })

      const body = await response.json()

      if (!response.ok || !body.success) {
        setState({ data: null, loading: false, error: body.error ?? { code: 'HTTP_ERROR', message: response.statusText } })
        return
      }

      setState({ data: body.data, loading: false, error: null })
    } catch (err) {
      setState({
        data: null,
        loading: false,
        error: { code: 'FETCH_ERROR', message: err instanceof Error ? err.message : 'Unknown error' },
      })
    }
  }, [url])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  return { ...state, refetch: fetchData }
}

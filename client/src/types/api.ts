export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]>
}

export interface ApiMetadata {
  requestId: string
  timestamp: string
  apiVersion: string
}

export interface ApiResponse<T> {
  success: boolean
  data: T | null
  error: ApiError | null
  metadata: ApiMetadata
}

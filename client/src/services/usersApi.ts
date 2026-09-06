import { apiGet, apiPost, apiPut } from './apiClient'
import type { CreateUserRequest, ResetUserPasswordRequest, UpdateUserRoleRequest, UserListItem } from '../types/users'

export const usersApi = {
  list: () => apiGet<UserListItem[]>('/api/users'),
  create: (req: CreateUserRequest) => apiPost<UserListItem>('/api/users', req),
  updateRole: (id: string, req: UpdateUserRoleRequest) => apiPut<UserListItem>(`/api/users/${id}/role`, req),
  resetPassword: (id: string, req: ResetUserPasswordRequest) => apiPost<{ message: string }>(`/api/users/${id}/reset-password`, req),
  lock: (id: string) => apiPost<{ message: string }>(`/api/users/${id}/lock`),
  unlock: (id: string) => apiPost<{ message: string }>(`/api/users/${id}/unlock`),
}

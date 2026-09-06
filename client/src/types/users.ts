export type UserRole = 'Analyst' | 'Reviewer' | 'CFO' | 'Admin' | 'Compliance'

export interface UserListItem {
  id: string
  email: string
  name: string | null
  role: UserRole
  isLockedOut: boolean
}

export interface CreateUserRequest {
  email: string
  password: string
  name?: string | null
  role: UserRole
}

export interface UpdateUserRoleRequest {
  role: UserRole
}

export interface ResetUserPasswordRequest {
  newPassword: string
}

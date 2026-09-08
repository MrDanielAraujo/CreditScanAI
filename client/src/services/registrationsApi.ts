import { apiDelete, apiGet, apiPost, apiPut } from './apiClient'
import type {
  AccountSubtype,
  AccountType,
  ChartOfAccounts,
  StandardAccount,
  UpsertAccountSubtypeRequest,
  UpsertAccountTypeRequest,
  UpsertChartOfAccountsRequest,
  UpsertStandardAccountRequest,
} from '../types/registrations'

export const accountTypesApi = {
  list: () => apiGet<AccountType[]>('/api/account-types'),
  create: (req: UpsertAccountTypeRequest) => apiPost<AccountType>('/api/account-types', req),
  update: (id: string, req: UpsertAccountTypeRequest) => apiPut<AccountType>(`/api/account-types/${id}`, req),
  remove: (id: string) => apiDelete<object>(`/api/account-types/${id}`),
}

export const accountSubtypesApi = {
  list: () => apiGet<AccountSubtype[]>('/api/account-subtypes'),
  create: (req: UpsertAccountSubtypeRequest) => apiPost<AccountSubtype>('/api/account-subtypes', req),
  update: (id: string, req: UpsertAccountSubtypeRequest) => apiPut<AccountSubtype>(`/api/account-subtypes/${id}`, req),
  remove: (id: string) => apiDelete<object>(`/api/account-subtypes/${id}`),
  getCompatibleTypes: (id: string) => apiGet<string[]>(`/api/account-subtypes/${id}/compatible-types`),
  addCompatibleType: (id: string, accountTypeId: string) =>
    apiPost<object>(`/api/account-subtypes/${id}/compatible-types`, { accountTypeId }),
  removeCompatibleType: (id: string, accountTypeId: string) =>
    apiDelete<object>(`/api/account-subtypes/${id}/compatible-types/${accountTypeId}`),
}

export const chartOfAccountsApi = {
  list: () => apiGet<ChartOfAccounts[]>('/api/chart-of-accounts'),
  create: (req: UpsertChartOfAccountsRequest) => apiPost<ChartOfAccounts>('/api/chart-of-accounts', req),
  update: (id: string, req: UpsertChartOfAccountsRequest) => apiPut<ChartOfAccounts>(`/api/chart-of-accounts/${id}`, req),
  setDefault: (id: string) => apiPost<ChartOfAccounts>(`/api/chart-of-accounts/${id}/set-default`),
  remove: (id: string) => apiDelete<object>(`/api/chart-of-accounts/${id}`),
}

export const standardAccountsApi = {
  list: (chartOfAccountsId?: string) =>
    apiGet<StandardAccount[]>(
      chartOfAccountsId ? `/api/standard-accounts?chartOfAccountsId=${chartOfAccountsId}` : '/api/standard-accounts',
    ),
  create: (req: UpsertStandardAccountRequest) => apiPost<StandardAccount>('/api/standard-accounts', req),
  update: (id: string, req: UpsertStandardAccountRequest) => apiPut<StandardAccount>(`/api/standard-accounts/${id}`, req),
  remove: (id: string) => apiDelete<object>(`/api/standard-accounts/${id}`),
}

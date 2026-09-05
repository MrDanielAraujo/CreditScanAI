export interface AccountType {
  id: string
  code: string
  name: string
  description: string | null
  sequenceOrder: number | null
}

export interface UpsertAccountTypeRequest {
  code: string
  name: string
  description: string | null
  sequenceOrder: number | null
}

export interface AccountSubtype {
  id: string
  accountTypeId: string
  code: string
  name: string
  description: string | null
  sequenceOrder: number | null
}

export interface UpsertAccountSubtypeRequest {
  accountTypeId: string
  code: string
  name: string
  description: string | null
  sequenceOrder: number | null
}

export interface ChartOfAccounts {
  id: string
  name: string
  description: string | null
  isDefault: boolean
}

export interface UpsertChartOfAccountsRequest {
  name: string
  description: string | null
}

export interface StandardAccount {
  id: string
  chartOfAccountsId: string
  accountTypeId: string
  accountSubtypeId: string
  code: string
  name: string
  description: string | null
}

export interface UpsertStandardAccountRequest {
  chartOfAccountsId: string
  accountTypeId: string
  accountSubtypeId: string
  code: string
  name: string
  description: string | null
}

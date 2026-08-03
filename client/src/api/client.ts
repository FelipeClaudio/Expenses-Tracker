import type {
  AuthenticatedUser,
  Balance,
  Expense,
  LogExpenseInput,
  MarkSettlementPaidInput,
  SettledTransfer,
  SettlementTransfer,
  Topic,
} from './types'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    credentials: 'include',
    headers: {
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const body = await response.text()
    throw new ApiError(response.status, body || response.statusText)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  getMe: () => request<AuthenticatedUser>('/api/users/me'),

  getMyTopics: () => request<Topic[]>('/api/topics'),

  createTopic: (input: { name: string; description?: string }) =>
    request<Topic>('/api/topics', { method: 'POST', body: JSON.stringify(input) }),

  getTopic: (topicId: string) => request<Topic>(`/api/topics/${topicId}`),

  getSubtopics: (topicId: string) => request<Topic[]>(`/api/topics/${topicId}/subtopics`),

  createSubtopic: (topicId: string, input: { name: string; description?: string }) =>
    request<Topic>(`/api/topics/${topicId}/subtopics`, { method: 'POST', body: JSON.stringify(input) }),

  getExpenses: (topicId: string) => request<Expense[]>(`/api/topics/${topicId}/expenses`),

  logExpense: (topicId: string, input: LogExpenseInput) =>
    request<Expense>(`/api/topics/${topicId}/expenses`, { method: 'POST', body: JSON.stringify(input) }),

  getBalances: (topicId: string) => request<Balance[]>(`/api/topics/${topicId}/balances`),

  getSettlements: (topicId: string) => request<SettlementTransfer[]>(`/api/topics/${topicId}/settlements`),

  markSettlementPaid: (topicId: string, input: MarkSettlementPaidInput) =>
    request<SettledTransfer>(`/api/topics/${topicId}/settlements/mark-paid`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
}

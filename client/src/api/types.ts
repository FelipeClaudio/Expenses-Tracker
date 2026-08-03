export interface AuthenticatedUser {
  id: string
  email: string
  displayName: string
  avatarUrl: string | null
}

export interface Topic {
  id: string
  parentTopicId: string | null
  rootTopicId: string
  name: string
  description: string | null
  inviteCode: string | null
  createdByUserId: string
  createdAt: string
}

export interface ExpenseParticipant {
  userId: string
  shareAmount: number
}

export interface Expense {
  id: string
  topicId: string
  description: string
  amount: number
  paidByUserId: string
  expenseDate: string
  createdByUserId: string
  createdAt: string
  participants: ExpenseParticipant[]
}

export interface Balance {
  userId: string
  netBalance: number
}

export interface SettlementTransfer {
  fromUserId: string
  toUserId: string
  amount: number
}

export interface SettledTransfer {
  id: string
  fromUserId: string
  toUserId: string
  amount: number
  recordedAt: string
}

export interface LogExpenseInput {
  description: string
  amount: number
  paidByUserId: string
  expenseDate: string
  participantUserIds: string[]
}

export interface MarkSettlementPaidInput {
  fromUserId: string
  toUserId: string
  amount: number
}

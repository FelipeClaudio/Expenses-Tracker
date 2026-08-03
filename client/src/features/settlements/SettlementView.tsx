import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Balance, SettlementTransfer } from '../../api/types'

interface Member {
  id: string
  displayName: string
}

interface SettlementViewProps {
  topicId: string
  members: Member[]
}

export function SettlementView({ topicId, members }: SettlementViewProps) {
  const [balances, setBalances] = useState<Balance[] | null>(null)
  const [transfers, setTransfers] = useState<SettlementTransfer[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  function nameOf(userId: string) {
    return members.find((m) => m.id === userId)?.displayName ?? userId
  }

  function load() {
    Promise.all([api.getBalances(topicId), api.getSettlements(topicId)])
      .then(([balancesResult, transfersResult]) => {
        setBalances(balancesResult)
        setTransfers(transfersResult)
      })
      .catch(() => setError('Could not load balances.'))
  }

  useEffect(load, [topicId])

  async function handleMarkPaid(transfer: SettlementTransfer) {
    try {
      await api.markSettlementPaid(topicId, transfer)
      load()
    } catch {
      setError('Could not record the settlement.')
    }
  }

  if (error) {
    return <p role="alert">{error}</p>
  }

  if (balances === null || transfers === null) {
    return <p>Loading balances…</p>
  }

  return (
    <div>
      <h2>Balances</h2>
      <ul>
        {balances.map((balance) => (
          <li key={balance.userId}>
            {nameOf(balance.userId)}{' '}
            {balance.netBalance >= 0
              ? `is owed ${balance.netBalance.toFixed(2)}`
              : `owes ${Math.abs(balance.netBalance).toFixed(2)}`}
          </li>
        ))}
      </ul>

      <h2>Settle up</h2>
      {transfers.length === 0 ? (
        <p>All settled up!</p>
      ) : (
        <ul>
          {transfers.map((transfer) => (
            <li key={`${transfer.fromUserId}-${transfer.toUserId}`}>
              {nameOf(transfer.fromUserId)} owes {nameOf(transfer.toUserId)} {transfer.amount.toFixed(2)}
              <button type="button" onClick={() => handleMarkPaid(transfer)}>
                Mark as paid
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

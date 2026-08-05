import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Balance, SettlementTransfer } from '../../api/types'
import { Button } from '../../components/ui/button'
import { HeroCard, Card } from '../../components/ui/card'
import { ArrowDownGlyph, ArrowUpGlyph, IconBadge } from '../../components/ui/icon-badge'

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
    <div className="flex flex-col gap-6">
      <HeroCard>
        <h2 className="mb-4 text-lg font-semibold">Balances</h2>
        {balances.length === 0 ? (
          <p className="text-white/70">Nobody owes anything yet.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {balances.map((balance) => {
              const isPositive = balance.netBalance >= 0
              return (
                <li key={balance.userId} className="flex items-center gap-3">
                  <IconBadge variant={isPositive ? 'positive' : 'negative'} aria-hidden="true">
                    {isPositive ? <ArrowUpGlyph /> : <ArrowDownGlyph />}
                  </IconBadge>
                  <span className="flex-1 font-medium">{nameOf(balance.userId)}</span>
                  <span className={isPositive ? 'font-semibold text-mint-400' : 'font-semibold text-rose-300'}>
                    {isPositive
                      ? `is owed ${balance.netBalance.toFixed(2)}`
                      : `owes ${Math.abs(balance.netBalance).toFixed(2)}`}
                  </span>
                </li>
              )
            })}
          </ul>
        )}
      </HeroCard>

      <section>
        <h2 className="mb-2 text-lg font-semibold text-navy-900 dark:text-white">Settle up</h2>
        {transfers.length === 0 ? (
          <p className="text-navy-400 dark:text-navy-300">All settled up!</p>
        ) : (
          <Card className="p-2">
            <ul className="flex flex-col">
              {transfers.map((transfer) => (
                <li
                  key={`${transfer.fromUserId}-${transfer.toUserId}`}
                  className="flex flex-wrap items-center gap-3 rounded-2xl px-2 py-2"
                >
                  <IconBadge variant="neutral" aria-hidden="true">
                    <ArrowDownGlyph />
                  </IconBadge>
                  <span className="flex-1 text-navy-900 dark:text-white">
                    {nameOf(transfer.fromUserId)} owes {nameOf(transfer.toUserId)} {transfer.amount.toFixed(2)}
                  </span>
                  <Button type="button" onClick={() => handleMarkPaid(transfer)}>
                    Mark as paid
                  </Button>
                </li>
              ))}
            </ul>
          </Card>
        )}
      </section>
    </div>
  )
}

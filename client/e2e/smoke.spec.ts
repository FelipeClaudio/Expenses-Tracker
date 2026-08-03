import { expect, test } from '@playwright/test'

test('sign in, create a topic, log an expense, and view the settlement', async ({ page }) => {
  const topicName = `Smoke Test ${Date.now()}`

  await page.goto('/signin')
  await page.getByRole('button', { name: 'Continue as test user' }).click()
  await page.waitForURL('**/topics')

  await page.getByLabel('Topic name').fill(topicName)
  await page.getByRole('button', { name: 'Create topic' }).click()
  await page.getByRole('button', { name: topicName }).click()
  await page.waitForURL(/\/topics\/[^/]+$/)

  await page.getByLabel('Description').fill('Dinner')
  await page.getByLabel('Amount').fill('42.50')
  await page.getByRole('button', { name: 'Log expense' }).click()
  await expect(page.getByText('Dinner')).toBeVisible()

  await page.getByRole('link', { name: 'Settle up' }).click()
  await page.waitForURL(/\/topics\/[^/]+\/settle$/)

  await expect(page.getByRole('heading', { name: 'Balances' })).toBeVisible()
  // A single-member topic always nets to zero: the creator paid their own
  // sole-participant expense, so there's nothing left to transfer.
  await expect(page.getByText('All settled up!')).toBeVisible()
})

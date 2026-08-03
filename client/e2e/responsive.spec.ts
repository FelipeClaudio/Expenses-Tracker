import { expect, test, type Page } from '@playwright/test'

async function signIn(page: Page) {
  await page.goto('/signin')
  await page.getByRole('button', { name: 'Continue as test user' }).click()
  await page.waitForURL('**/topics')
}

async function createTopic(page: Page, name: string) {
  await page.getByLabel('Topic name').fill(name)
  await page.getByRole('button', { name: 'Create topic' }).click()
  await expect(page.getByText(name)).toBeVisible()
}

const viewports = [
  { width: 360, height: 800, desktop: false },
  { width: 390, height: 844, desktop: false },
  { width: 768, height: 1024, desktop: true },
  { width: 1280, height: 800, desktop: true },
]

for (const { width, height, desktop } of viewports) {
  test(`nav and list layout at ${width}px (${desktop ? 'sidebar/table' : 'bottom-nav/cards'})`, async ({ page }) => {
    await signIn(page)
    await createTopic(page, `Responsive Test ${Date.now()}-${width}`)

    await page.setViewportSize({ width, height })

    const bottomNav = page.getByTestId('bottom-nav')
    const sidebarNav = page.getByTestId('sidebar-nav')

    if (desktop) {
      await expect(sidebarNav).toBeVisible()
      await expect(bottomNav).toBeHidden()

      await expect(page.getByTestId('topic-table')).toBeVisible()
      await expect(page.getByTestId('topic-cards')).toHaveCount(0)
    } else {
      await expect(bottomNav).toBeVisible()
      await expect(sidebarNav).toBeHidden()

      await expect(page.getByTestId('topic-cards')).toBeVisible()
      await expect(page.getByTestId('topic-table')).toHaveCount(0)

      const firstNavLink = bottomNav.getByRole('link').first()
      const box = await firstNavLink.boundingBox()
      expect(box).not.toBeNull()
      expect(box!.width).toBeGreaterThanOrEqual(44)
      expect(box!.height).toBeGreaterThanOrEqual(44)
    }
  })
}

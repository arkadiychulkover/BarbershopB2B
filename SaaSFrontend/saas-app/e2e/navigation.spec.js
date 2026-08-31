import { test, expect } from '@playwright/test';

test.describe('Navigation & Protected Route Flow', () => {

  test('should redirect unauthenticated user from protected dashboard to login when API returns 401', async ({ page }) => {
    await page.route('**/api/Statistic/**', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unauthorized' })
      });
    });

    await page.goto('/#/dashboard');

    // apiRequest automatically redirects to /login on 401
    await expect(page).toHaveURL(/#\/login/);
  });

  test('should navigate between dashboard sections using sidebar', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('jwt_token', 'mock.token');
    });

    await page.route('**/api/Settings', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-owner',
          ownerName: 'Алексей',
          barbershopName: 'Мой Барбершоп',
          status: 'Active'
        })
      });
    });

    await page.route('**/api/Statistic/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({})
      });
    });

    await page.route('**/api/ServiceNames', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    await page.route('**/api/Barber/all', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    await page.goto('/#/dashboard');
    await expect(page.locator('h1')).toContainText('Панель управления');

    // Navigate to Services via sidebar link
    const servicesLink = page.locator('a[href="#/dashboard/services"]');
    if (await servicesLink.isVisible()) {
      await servicesLink.click();
      await expect(page).toHaveURL(/#\/dashboard\/services/);
      await expect(page.locator('.page-header h1')).toContainText(/услуг/i);
      await expect(servicesLink).toHaveClass(/active/);
    }

    // Navigate to Masters
    const mastersLink = page.locator('a[href="#/dashboard/masters"]');
    if (await mastersLink.isVisible()) {
      await mastersLink.click();
      await expect(page).toHaveURL(/#\/dashboard\/masters/);
      await expect(page.locator('.page-header h1')).toContainText(/мастеров/i);
      await expect(mastersLink).toHaveClass(/active/);
    }
  });
});

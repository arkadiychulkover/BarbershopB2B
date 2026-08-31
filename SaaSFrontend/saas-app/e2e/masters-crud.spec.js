import { test, expect } from '@playwright/test';

test.describe('Masters Management (CRUD) Flow', () => {

  test.beforeEach(async ({ page }) => {
    // Authenticate
    await page.addInitScript(() => {
      localStorage.setItem('jwt_token', 'mock.jwt.token');
    });

    // Mock initial masters list
    await page.route('**/api/Barber/all', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 'master-uuid-1',
              name: 'Александр Мастер',
              description: 'Барбер с 7-летним стажем',
              telegramId: '123456',
              telegramUsername: 'alex_master',
              isActive: true
            }
          ])
        });
      }
    });

    await page.route('**/api/Settings', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-owner',
          ownerName: 'Владелец',
          status: 'Active'
        })
      });
    });
  });

  test('should display masters list with details', async ({ page }) => {
    await page.goto('/#/dashboard/masters');

    await expect(page.locator('text=Александр Мастер')).toBeVisible();
    await expect(page.locator('text=Барбер с 7-летним стажем')).toBeVisible();
    await expect(page.locator('text=@alex_master')).toBeVisible();
  });

  test('should add a new master and display in the list', async ({ page }) => {
    await page.route('**/api/Barber/add', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            message: 'Barber added successfully',
            barberId: 'master-uuid-2'
          })
        });
      }
    });

    await page.goto('/#/dashboard/masters');

    // Click "Добавить мастера"
    const addBtn = page.locator('button:has-text("Добавить мастера")').first();
    await addBtn.click();

    // Fill form fields
    await page.fill('#masterName', 'Сергей Топовый');
    await page.fill('#masterDesc', 'Мастер фейдов');
    await page.fill('#masterTg', '99887766');
    await page.fill('#masterTgUsername', 'sergey_fade');

    // Submit form
    const submitBtn = page.locator('button[type="submit"]:has-text("Сохранить мастера")');
    await submitBtn.click();

    // Verify newly added master is shown
    await expect(page.locator('text=Сергей Топовый')).toBeVisible();
    await expect(page.locator('text=Мастер фейдов')).toBeVisible();
  });

  test('should delete master when clicking delete action', async ({ page }) => {
    await page.route('**/api/Barber/delete', async (route) => {
      if (route.request().method() === 'DELETE') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Master deleted' })
        });
      }
    });

    page.on('dialog', async (dialog) => {
      expect(dialog.message()).toContain('Вы уверены');
      await dialog.accept();
    });

    await page.goto('/#/dashboard/masters');
    await expect(page.locator('text=Александр Мастер')).toBeVisible();

    const deleteBtn = page.locator('button:has-text("Удалить")').first();
    await deleteBtn.click();

    await expect(page.locator('text=Александр Мастер')).not.toBeVisible();
  });
});

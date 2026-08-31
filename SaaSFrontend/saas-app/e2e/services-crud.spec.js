import { test, expect } from '@playwright/test';

test.describe('Services Management (CRUD) Flow', () => {

  test.beforeEach(async ({ page }) => {
    // Mock user authentication state in localStorage
    await page.addInitScript(() => {
      localStorage.setItem('jwt_token', 'mock.jwt.token');
    });

    // Mock initial services list
    await page.route('**/api/ServiceNames', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: '11111111-2222-3333-4444-555555555555', name: 'Мужская стрижка' },
            { id: '22222222-3333-4444-5555-666666666666', name: 'Моделирование бороды' }
          ])
        });
      } else {
        await route.continue();
      }
    });

    // Mock settings/profile info
    await page.route('**/api/Settings', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-owner-id',
          ownerName: 'Тестовый Владелец',
          status: 'Active'
        })
      });
    });
  });

  test('should display list of existing services', async ({ page }) => {
    await page.goto('/#/dashboard/services');

    await expect(page.locator('text=Мужская стрижка')).toBeVisible();
    await expect(page.locator('text=Моделирование бороды')).toBeVisible();
  });

  test('should create a new service and show it in the list', async ({ page }) => {
    const newService = {
      id: '33333333-4444-5555-6666-777777777777',
      name: 'Королевское бритье'
    };

    await page.route('**/api/ServiceNames', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(newService)
        });
      } else if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: '11111111-2222-3333-4444-555555555555', name: 'Мужская стрижка' },
            { id: '22222222-3333-4444-5555-666666666666', name: 'Моделирование бороды' }
          ])
        });
      }
    });

    await page.goto('/#/dashboard/services');

    // Click "Добавить услугу" button in the header
    const addButton = page.locator('button:has-text("Добавить услугу")');
    await addButton.click();

    // Fill form input #newService
    const input = page.locator('#newService');
    await input.fill('Королевское бритье');

    // Submit save button "Создать услугу"
    const saveButton = page.locator('button[type="submit"]:has-text("Создать услугу")');
    await saveButton.click();

    // Verify newly added service appears in UI
    await expect(page.locator('text=Королевское бритье')).toBeVisible();
  });

  test('should edit existing service name inline', async ({ page }) => {
    await page.route('**/api/ServiceNames/11111111-2222-3333-4444-555555555555', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '11111111-2222-3333-4444-555555555555',
            name: 'Мужская премиум стрижка'
          })
        });
      }
    });

    await page.goto('/#/dashboard/services');
    await expect(page.locator('text=Мужская стрижка')).toBeVisible();

    // Click edit button "Изменить" on the service card
    const editBtn = page.locator('.service-card button:has-text("Изменить")').first();
    await editBtn.click();

    // Edit input in edit-mode
    const editInput = page.locator('.edit-mode input');
    await editInput.fill('Мужская премиум стрижка');

    // Click save button "Сохранить"
    const saveBtn = page.locator('.edit-mode button:has-text("Сохранить")');
    await saveBtn.click();

    // Verify updated title is displayed
    await expect(page.locator('text=Мужская премиум стрижка')).toBeVisible();
  });

  test('should delete service with confirmation dialog', async ({ page }) => {
    await page.route('**/api/ServiceNames/11111111-2222-3333-4444-555555555555', async (route) => {
      if (route.request().method() === 'DELETE') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Service name deleted successfully.' })
        });
      }
    });

    // Accept window confirm dialog
    page.on('dialog', async (dialog) => {
      expect(dialog.message()).toContain('Вы уверены');
      await dialog.accept();
    });

    await page.goto('/#/dashboard/services');
    await expect(page.locator('text=Мужская стрижка')).toBeVisible();

    // Click delete button "Удалить"
    const deleteBtn = page.locator('.service-card button:has-text("Удалить")').first();
    await deleteBtn.click();

    // Service should be removed from view
    await expect(page.locator('text=Мужская стрижка')).not.toBeVisible();
  });
});

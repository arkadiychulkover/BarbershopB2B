import { test, expect } from '@playwright/test';

test.describe('Authentication & Registration Flow', () => {

  test('should display login form with all required elements', async ({ page }) => {
    await page.goto('/#/login');

    await expect(page.locator('text=ARCH SYSTEM')).toBeVisible();
    await expect(page.locator('h2')).toHaveText('Вход в кабинет');
    await expect(page.locator('#email')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toContainText('Войти в систему');
    await expect(page.locator('a[href="#/forgot-password"]')).toBeVisible();
    await expect(page.locator('a[href="#/register"]')).toBeVisible();
  });

  test('should show error alert when login credentials are invalid', async ({ page }) => {
    // Intercept backend login endpoint to simulate invalid credentials (400 or 401 with message)
    await page.route('**/api/Regestration/login', async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid email or password' })
      });
    });

    await page.goto('/#/login');
    await page.fill('#email', 'invalid_user@barbershop.test');
    await page.fill('#password', 'WrongPassword123');
    await page.click('button[type="submit"]');

    const alert = page.locator('.alert-danger');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Invalid email or password');
  });

  test('should successfully authenticate Owner and navigate to Dashboard', async ({ page }) => {
    // Mock successful login response
    await page.route('**/api/Regestration/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock.jwt.token.for.owner.testing',
          role: 'Owner'
        })
      });
    });

    // Mock Settings request that checks subscription status
    await page.route('**/api/Settings', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '11111111-1111-1111-1111-111111111111',
          ownerName: 'Алексей Тестовый',
          barbershopName: 'ARCH Barbershop',
          status: 'Active',
          email: 'owner@arch.store'
        })
      });
    });

    // Mock dashboard statistics
    await page.route('**/api/Statistic/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({})
      });
    });

    await page.goto('/#/login');
    await page.fill('#email', 'owner@arch.store');
    await page.fill('#password', 'SecretPass123!');
    await page.click('button[type="submit"]');

    // Should redirect to dashboard
    await expect(page).toHaveURL(/#\/dashboard/);
    await expect(page.locator('h1')).toContainText('Панель управления');
  });

  test('should validate phone format in Registration form', async ({ page }) => {
    await page.goto('/#/register');

    await page.fill('#ownerName', 'Дмитрий');
    await page.fill('#email', 'dmitry@arch.store');
    await page.fill('#phoneNumber', '123456'); // invalid phone without + and too short
    await page.fill('#telegramId', '123456789');
    await page.fill('#barbershopName', 'Barber Club');
    await page.fill('#barbershopAddress', 'ул. Крещатик 1');
    await page.fill('#barbershopDescription', 'Премиум заведение');
    await page.fill('#botToken', '123456:ABC-DEF');
    await page.fill('#botUsername', 'barber_club_bot');
    await page.fill('#password', 'SecurePass123!');

    await page.click('button[type="submit"]');

    const alert = page.locator('.alert-danger');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Некорректный номер телефона');
  });

  test('should successfully complete registration and show success message', async ({ page }) => {
    await page.route('**/api/Regestration/register', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Registration successful' })
      });
    });

    await page.goto('/#/register');

    await page.fill('#ownerName', 'Михаил');
    await page.fill('#email', 'mikhail@arch.store');
    await page.fill('#phoneNumber', '+380991234567');
    await page.fill('#telegramId', '987654321');
    await page.fill('#barbershopName', 'Gold Scissors');
    await page.fill('#barbershopAddress', 'ул. Центральная, 10');
    await page.fill('#barbershopDescription', 'Отличная атмосфера и профессионалы');
    await page.fill('#botToken', '999999:XYZ-ABC');
    await page.fill('#botUsername', 'gold_scissors_bot');
    await page.fill('#password', 'ValidPass2026!');

    await page.click('button[type="submit"]');

    const alertSuccess = page.locator('.alert-success');
    await expect(alertSuccess).toBeVisible();
    await expect(alertSuccess).toContainText('Регистрация успешна!');
  });

  test('should handle forgot password flow', async ({ page }) => {
    await page.route('**/api/Regestration/forgot-password', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Письмо с инструкциями по смене пароля успешно отправлено' })
      });
    });

    await page.goto('/#/forgot-password');
    await page.fill('#email', 'forgot@arch.store');
    await page.click('button[type="submit"]');

    await expect(page.locator('h2:has-text("Проверьте почту")')).toBeVisible();
    await expect(page.locator('.success-box')).toBeVisible();
  });
});

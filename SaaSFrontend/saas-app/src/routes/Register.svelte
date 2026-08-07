<script>
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';

  let formData = {
    ownerName: '',
    phoneNumber: '',
    email: '',
    telegramId: '',
    barbershopName: '',
    barbershopAddress: '',
    barbershopDescription: '',
    botToken: '',
    botUsername: '',
    timeZone: 'Europe/Kyiv',
    password: ''
  };

  let isLoading = false;
  let errorMsg = '';
  let successMsg = '';

  async function handleRegister() {
    isLoading = true;
    errorMsg = '';
    
    try {
      await apiRequest('/api/Regestration/register', {
        method: 'POST',
        body: JSON.stringify(formData)
      });
      
      successMsg = 'Регистрация успешна! Сейчас вы будете перенаправлены на страницу входа...';
      setTimeout(() => {
        push('/login');
      }, 2000);
    } catch (err) {
      errorMsg = err.message || 'Ошибка регистрации. Проверьте данные.';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card auth-card">
    <h2>Создать аккаунт</h2>
    <p class="subtitle">Присоединяйтесь к BarbershopB2B</p>
    
    {#if errorMsg}
      <div class="alert alert-danger">{errorMsg}</div>
    {/if}
    
    {#if successMsg}
      <div class="alert alert-success">{successMsg}</div>
    {/if}

    <form on:submit|preventDefault={handleRegister}>
      <div class="form-grid">
        <div class="form-group">
          <label for="ownerName">Ваше имя</label>
          <input id="ownerName" type="text" class="input" bind:value={formData.ownerName} required />
        </div>
        
        <div class="form-group">
          <label for="email">Email</label>
          <input id="email" type="email" class="input" bind:value={formData.email} required />
        </div>

        <div class="form-group">
          <label for="phoneNumber">Телефон</label>
          <input id="phoneNumber" type="tel" class="input" bind:value={formData.phoneNumber} required />
        </div>

        <div class="form-group">
          <label for="telegramId">Telegram ID (ваш)</label>
          <input id="telegramId" type="text" class="input" bind:value={formData.telegramId} required />
        </div>

        <div class="form-group">
          <label for="barbershopName">Название барбершопа</label>
          <input id="barbershopName" type="text" class="input" bind:value={formData.barbershopName} required />
        </div>

        <div class="form-group">
          <label for="barbershopAddress">Адрес</label>
          <input id="barbershopAddress" type="text" class="input" bind:value={formData.barbershopAddress} required />
        </div>
      </div>

      <div class="form-group full-width">
        <label for="barbershopDescription">Описание для клиентов</label>
        <textarea id="barbershopDescription" class="input" bind:value={formData.barbershopDescription} rows="2" required></textarea>
      </div>
      
      <div class="form-grid">
        <div class="form-group">
          <label for="botToken">Токен Telegram-бота (от @BotFather)</label>
          <input id="botToken" type="text" class="input" bind:value={formData.botToken} required />
        </div>

        <div class="form-group">
          <label for="botUsername">Username бота (без @)</label>
          <input id="botUsername" type="text" class="input" bind:value={formData.botUsername} required />
        </div>

        <div class="form-group">
          <label for="password">Пароль</label>
          <input id="password" type="password" class="input" bind:value={formData.password} required minlength="6" />
        </div>
        
        <div class="form-group">
          <label for="timeZone">Часовой пояс</label>
          <select id="timeZone" class="input" bind:value={formData.timeZone}>
            <option value="Europe/Kyiv">Киев (UTC+2/3)</option>
            <option value="Europe/Moscow">Москва (UTC+3)</option>
            <option value="Europe/London">Лондон (UTC+0)</option>
          </select>
        </div>
      </div>

      <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading}>
        {isLoading ? 'Загрузка...' : 'Зарегистрироваться'}
      </button>
    </form>
    
    <div class="auth-links">
      Уже есть аккаунт? <a href="#/login">Войти</a>
    </div>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 2rem 1rem;
    background-color: var(--bg-color);
  }
  
  .auth-card {
    width: 100%;
    max-width: 800px;
  }
  
  .auth-card h2 {
    text-align: center;
    margin-bottom: 0.5rem;
  }
  
  .subtitle {
    text-align: center;
    color: var(--text-secondary);
    margin-bottom: 2rem;
  }
  
  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }
  
  @media (max-width: 600px) {
    .form-grid {
      grid-template-columns: 1fr;
    }
  }
  
  .form-group {
    margin-bottom: 1rem;
  }
  
  .full-width {
    margin-bottom: 1rem;
  }
  
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .submit-btn {
    width: 100%;
    margin-top: 1rem;
  }
  
  .auth-links {
    margin-top: 1.5rem;
    text-align: center;
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .alert {
    padding: 1rem;
    border-radius: var(--border-radius);
    margin-bottom: 1.5rem;
    font-size: 0.875rem;
  }
  
  .alert-danger {
    background-color: rgba(239, 68, 68, 0.1);
    color: var(--danger);
    border: 1px solid rgba(239, 68, 68, 0.2);
  }
  
  .alert-success {
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--success);
    border: 1px solid rgba(16, 185, 129, 0.2);
  }
</style>

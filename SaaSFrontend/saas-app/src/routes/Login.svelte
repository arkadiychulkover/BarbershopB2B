<script>
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';
  import { setAuthToken, profileStore } from '../lib/store';

  let email = '';
  let password = '';
  let isLoading = false;
  let errorMsg = '';

  async function handleLogin() {
    isLoading = true;
    errorMsg = '';
    
    try {
      const response = await apiRequest('/api/Regestration/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });
      
      if (response && response.token) {
        setAuthToken(response.token);
        
        // Fetch settings to check Status
        const settings = await apiRequest('/api/Settings');
        profileStore.set({
          ownerId: settings.ownerName, // Using ownerName as placeholder
          status: settings.status,
          email: email
        });
        
        if (settings.status === 'Active') {
          push('/dashboard');
        } else {
          push('/payment');
        }
      }
    } catch (err) {
      errorMsg = err.message || 'Неверный email или пароль';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card auth-card">
    <h2>Вход в кабинет</h2>
    <p class="subtitle">С возвращением в BarbershopB2B</p>
    
    {#if errorMsg}
      <div class="alert alert-danger">{errorMsg}</div>
    {/if}

    <form on:submit|preventDefault={handleLogin}>
      <div class="form-group">
        <label for="email">Email</label>
        <input id="email" type="email" class="input" bind:value={email} required />
      </div>

      <div class="form-group">
        <label for="password">Пароль</label>
        <input id="password" type="password" class="input" bind:value={password} required />
      </div>

      <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading}>
        {isLoading ? 'Вход...' : 'Войти'}
      </button>
    </form>
    
    <div class="auth-links">
      Нет аккаунта? <a href="#/register">Зарегистрироваться</a>
    </div>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 1rem;
    background-color: var(--bg-color);
  }
  
  .auth-card {
    width: 100%;
    max-width: 400px;
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
  
  .form-group {
    margin-bottom: 1.25rem;
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
</style>

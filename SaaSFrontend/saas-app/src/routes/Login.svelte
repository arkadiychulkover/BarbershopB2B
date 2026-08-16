<script>
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';
  import { setAuthToken, profileStore } from '../lib/store';
  import { Mail, Lock, AlertCircle, ArrowRight } from 'lucide-svelte';

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
        setAuthToken(response.token, response.role);

        if (response.role === 'Admin') {
          push('/admin');
          return;
        }

        // Owner flow
        try {
          const settings = await apiRequest('/api/Settings');
          profileStore.set({
            ownerId: settings.ownerName,
            status: settings.status,
            email: email
          });
          
          if (settings.status === 'Active') {
            push('/dashboard');
          } else {
            push('/payment');
          }
        } catch (settingsErr) {
          push('/dashboard');
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
    <div class="brand-header">
      <span class="brand-dot"></span>
      <span class="brand-name">BarbershopB2B</span>
    </div>

    <h2>Вход в кабинет</h2>
    <p class="subtitle">Управляйте барбершопом и онлайн-записями</p>
    
    {#if errorMsg}
      <div class="alert alert-danger">
        <AlertCircle size={18} />
        <span>{errorMsg}</span>
      </div>
    {/if}

    <form on:submit|preventDefault={handleLogin}>
      <div class="form-group">
        <label for="email">Email</label>
        <div class="input-icon-wrap">
          <Mail size={17} class="input-icon" />
          <input 
            id="email" 
            type="email" 
            class="input has-icon" 
            bind:value={email} 
            placeholder="name@barbershop.com" 
            required 
          />
        </div>
      </div>

      <div class="form-group">
        <label for="password">Пароль</label>
        <div class="input-icon-wrap">
          <Lock size={17} class="input-icon" />
          <input 
            id="password" 
            type="password" 
            class="input has-icon" 
            bind:value={password} 
            placeholder="••••••••" 
            required 
          />
        </div>
      </div>

      <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading}>
        <span>{isLoading ? 'Проверка...' : 'Войти в систему'}</span>
        {#if !isLoading}
          <ArrowRight size={17} />
        {/if}
      </button>
    </form>
    
    <div class="auth-links">
      <span>Еще нет аккаунта?</span>
      <a href="#/register">Зарегистрироваться</a>
    </div>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 1.5rem;
    background-color: var(--bg-canvas);
    background-image: 
      radial-gradient(ellipse 60% 50% at 50% 20%, rgba(223, 158, 142, 0.08), transparent 70%),
      radial-gradient(ellipse 40% 40% at 80% 80%, rgba(179, 183, 219, 0.05), transparent 70%);
  }
  
  .auth-card {
    width: 100%;
    max-width: 420px;
    padding: 2.5rem 2.25rem;
    animation: fadeIn 0.35s var(--ease-spring);
  }

  .brand-header {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    margin-bottom: 1.5rem;
  }

  .brand-dot {
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-sage));
    box-shadow: 0 0 10px var(--pastel-rose-glow);
  }

  .brand-name {
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-secondary);
    letter-spacing: -0.01em;
  }
  
  .auth-card h2 {
    text-align: center;
    font-size: 1.75rem;
    margin-bottom: 0.4rem;
  }
  
  .subtitle {
    text-align: center;
    color: var(--text-secondary);
    font-size: 0.92rem;
    margin-bottom: 2rem;
  }
  
  .form-group {
    margin-bottom: 1.35rem;
  }
  
  label {
    display: block;
    margin-bottom: 0.45rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .input-icon-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.input-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .input.has-icon {
    padding-left: 2.75rem;
  }
  
  .submit-btn {
    width: 100%;
    margin-top: 1.25rem;
    padding: 0.85rem 1.5rem;
    font-size: 1rem;
  }
  
  .auth-links {
    margin-top: 1.75rem;
    text-align: center;
    font-size: 0.9rem;
    color: var(--text-secondary);
    display: flex;
    justify-content: center;
    gap: 0.4rem;
  }

  .auth-links a {
    font-weight: 600;
  }
</style>

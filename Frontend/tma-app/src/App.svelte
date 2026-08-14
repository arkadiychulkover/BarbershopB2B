<script lang="ts">
  import { onMount } from 'svelte';
  import { authStore, setAuthStatus } from './lib/stores/auth';
  import { initTelegram, getInitData } from './lib/telegram';
  import { apiFetch } from './lib/api';
  
  import BarberDashboard from './routes/BarberDashboard.svelte';
  import ClientStub from './routes/ClientStub.svelte';

  let tenant = '';
  let initData = '';

  onMount(async () => {
    initTelegram();
    initData = getInitData();
    if (!initData) {
      setAuthStatus('error');
      return;
    }
    const pathSegments = window.location.pathname.split('/').filter(Boolean);
    tenant = pathSegments[0] || '';

    if (!tenant) {
      console.error('Tenant ID not found in URL');
      setAuthStatus('error');
      return;
    }

    await authenticate();
  });

  async function authenticate() {
    setAuthStatus('loading');
    try {
      const query = new URLSearchParams({ initData }).toString();
      const response = await apiFetch(`/api/${tenant}/Telegram?${query}`, {
        method: 'GET',
        skipAuth: true
      });
      
      const token = response.token;
      let role = '';
      try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const payload = JSON.parse(atob(base64));
        // The role claim is stored under the full ClaimTypes.Role URI by ASP.NET Core
        const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
        role = payload[roleClaim] || payload['role'] || payload['Role'] || '';
      } catch (e) {
        console.error('Failed to parse token', e);
      }

      if (role === 'Master') {
        setAuthStatus('barber', token);
      } else if (role === 'Client') {
        setAuthStatus('client', token);
      } else {
        setAuthStatus('error');
      }
    } catch (error) {
      console.error('Auth error:', error);
      setAuthStatus('error');
    }
  }
</script>

<main>
  {#if $authStore.status === 'loading'}
    <div class="loader-container">
      <div class="spinner"></div>
      <p>Загрузка...</p>
    </div>
  {:else if $authStore.status === 'error'}
    <div class="error-container">
      {#if !initData}
        <h2>Откройте приложение через Telegram</h2>
        <p>Это мини-приложение работает только внутри мессенджера Telegram.</p>
      {:else}
        <h2>Произошла ошибка</h2>
        <p>Не удалось подключиться к серверу.</p>
        <button on:click={authenticate}>Повторить</button>
      {/if}
    </div>
  {:else if $authStore.status === 'barber'}
    <BarberDashboard />
  {:else if $authStore.status === 'client'}
    <ClientStub />
  {/if}
</main>

<style>
  main {
    width: 100%;
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    font-family: var(--font-family);
  }

  .loader-container, .error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    padding: 32px 24px;
    text-align: center;
    animation: fadeIn 0.4s var(--ease-spring);
  }

  .loader-container p {
    color: var(--text-secondary);
    font-size: 15px;
    font-weight: 500;
    margin-top: 16px;
    letter-spacing: 0.02em;
  }

  .spinner {
    width: 44px;
    height: 44px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
    box-shadow: 0 0 20px var(--pastel-rose-glow);
  }

  .error-container {
    background: radial-gradient(circle at 50% 30%, rgba(232, 130, 130, 0.08), transparent 65%);
  }

  .error-container h2 {
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 8px;
  }

  .error-container p {
    font-size: 14px;
    color: var(--text-secondary);
    max-width: 320px;
    line-height: 1.5;
    margin: 0 0 20px;
  }

  button {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 12px 28px;
    border-radius: var(--radius-pill);
    font-size: 15px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }

  button:active {
    transform: scale(0.96);
    box-shadow: 0 2px 8px var(--pastel-rose-glow);
  }
</style>

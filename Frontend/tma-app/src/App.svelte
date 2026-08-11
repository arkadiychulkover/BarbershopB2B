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
      let payload = {};
      try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        payload = JSON.parse(atob(base64));
      } catch (e) {
        console.error('Failed to parse token', e);
      }

      if (payload.MasterId) {
        setAuthStatus('barber', token);
      } else if (payload.UserId) {
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
    background-color: var(--tg-theme-bg-color, #ffffff);
    color: var(--tg-theme-text-color, #000000);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
  }

  .loader-container, .error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100vh;
    padding: 20px;
    text-align: center;
  }

  .spinner {
    width: 40px;
    height: 40px;
    border: 4px solid var(--tg-theme-hint-color, #ccc);
    border-top: 4px solid var(--tg-theme-button-color, #3390ec);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 16px;
  }

  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }

  button {
    background-color: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #ffffff);
    border: none;
    padding: 12px 24px;
    border-radius: 8px;
    font-size: 16px;
    font-weight: 500;
    cursor: pointer;
    margin-top: 16px;
  }
</style>

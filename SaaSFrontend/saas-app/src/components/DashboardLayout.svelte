<script>
  import { onMount } from 'svelte';
  import { push, link } from 'svelte-spa-router';
  import { authStore, profileStore, currentLocation } from '../lib/store';
  import { apiRequest } from '../lib/api';
  import { LayoutDashboard, Settings, Users, BarChart3, Bot, LogOut, Scissors, CalendarDays } from 'lucide-svelte';
  
  let isLoading = true;

  onMount(async () => {
    if (!$authStore.isAuthenticated) {
      push('/login');
      return;
    }

    try {
      const settings = await apiRequest('/api/Settings');
      profileStore.set({
        ownerId: settings.ownerName,
        status: settings.status,
        email: $profileStore.email || settings.email || ''
      });
      if (settings.status !== 'Active' && $currentLocation !== '/payment') {
        push('/payment');
      }
    } catch (e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  });

  function handleLogout() {
    import('../lib/store').then(({ setAuthToken }) => {
      setAuthToken(null);
      push('/login');
    });
  }
</script>

{#if isLoading}
  <div class="loader-container">
    <div class="spinner"></div>
    <p>Загрузка...</p>
  </div>
{:else}
  <div class="dashboard-layout">
    <aside class="sidebar">
      <div class="logo">
        <h2>BarbershopB2B</h2>
        <span class="badge">
          {$profileStore.status || 'Unknown'}
        </span>
      </div>

      <nav class="nav-menu">
        <a href="#/dashboard" class="nav-item" class:active={$currentLocation === '/dashboard' || $currentLocation === '/dashboard/'}>
          <LayoutDashboard size={20} />
          <span>Обзор</span>
        </a>
        <a href="#/dashboard/schedule" class="nav-item" class:active={$currentLocation.includes('/schedule')}>
          <CalendarDays size={20} />
          <span>Расписание</span>
        </a>
        <a href="#/dashboard/settings" class="nav-item" class:active={$currentLocation.includes('/settings')}>
          <Settings size={20} />
          <span>Настройки</span>
        </a>
        <a href="#/dashboard/masters" class="nav-item" class:active={$currentLocation.includes('/masters')}>
          <Users size={20} />
          <span>Мастера</span>
        </a>
        <a href="#/dashboard/services" class="nav-item" class:active={$currentLocation.includes('/services')}>
          <Scissors size={20} />
          <span>Услуги</span>
        </a>
        <a href="#/dashboard/statistics" class="nav-item" class:active={$currentLocation.includes('/statistics')}>
          <BarChart3 size={20} />
          <span>Статистика</span>
        </a>
        <a href="#/dashboard/bot-setup" class="nav-item" class:active={$currentLocation.includes('/bot-setup')}>
          <Bot size={20} />
          <span>Настройка бота</span>
        </a>
      </nav>

      <div class="sidebar-footer">
        <button class="nav-item logout" on:click={handleLogout}>
          <LogOut size={20} />
          <span>Выйти</span>
        </button>
      </div>
    </aside>

    <main class="main-content">
      <slot></slot>
    </main>
  </div>
{/if}

<style>
  .loader-container {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    background-color: var(--bg-color);
  }
  
  .spinner {
    border: 4px solid var(--border-color);
    border-top: 4px solid var(--accent);
    border-radius: 50%;
    width: 40px;
    height: 40px;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
  }
  
  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }

  .dashboard-layout {
    display: flex;
    min-height: 100vh;
    background-color: var(--bg-color);
  }
  
  .sidebar {
    width: 260px;
    background-color: var(--bg-secondary);
    border-right: 1px solid var(--border-color);
    display: flex;
    flex-direction: column;
    position: fixed;
    height: 100vh;
    left: 0;
    top: 0;
  }
  
  .logo {
    padding: 1.5rem;
    border-bottom: 1px solid var(--border-color);
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  
  .logo h2 {
    font-size: 1.25rem;
    margin: 0;
    color: var(--accent);
  }
  
  .badge {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
    border-radius: 1rem;
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--success);
    font-weight: 600;
  }

  
  .nav-menu {
    padding: 1rem 0;
    flex: 1;
  }
  
  .nav-item {
    display: flex;
    align-items: center;
    padding: 0.75rem 1.5rem;
    color: var(--text-secondary);
    gap: 0.75rem;
    transition: all 0.2s;
    background: transparent;
    border: none;
    width: 100%;
    text-align: left;
    font-size: 1rem;
  }
  
  .nav-item:hover {
    color: var(--text-primary);
    background-color: rgba(255, 255, 255, 0.05);
  }
  
  .nav-item.active {
    color: var(--accent);
    background-color: var(--accent-muted);
    border-right: 3px solid var(--accent);
  }
  
  .sidebar-footer {
    padding: 1rem 0;
    border-top: 1px solid var(--border-color);
  }
  
  .logout:hover {
    color: var(--danger);
    background-color: rgba(239, 68, 68, 0.05);
  }
  
  .main-content {
    flex: 1;
    margin-left: 260px;
    padding: 2rem;
    overflow-y: auto;
  }
  
  @media (max-width: 768px) {
    .sidebar {
      transform: translateX(-100%);
      transition: transform 0.3s;
      z-index: 100;
    }
    
    .main-content {
      margin-left: 0;
    }
  }
</style>

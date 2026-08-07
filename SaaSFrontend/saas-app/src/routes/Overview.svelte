<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { profileStore } from '../lib/store';
  
  let isLoading = true;
  let summary = { revenue: 0, appointments: 0 };
  
  onMount(async () => {
    try {
      const today = new Date().toISOString().split('T')[0];
      const stats = await apiRequest(`/api/Statistic/owner-statistic-daily?startDate=${today}&endDate=${today}`);
      
      if (stats && stats[today]) {
        summary.revenue = stats[today].ownerProfit;
        summary.appointments = stats[today].clientsCount;
      }
    } catch (e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  });
</script>

<DashboardLayout>
  <div class="overview">
    <header class="page-header">
      <h1>Обзор</h1>
      <p>Добро пожаловать, {$profileStore.ownerId || 'Владелец'}!</p>
    </header>

    <div class="stats-grid">
      <div class="stat-card">
        <h3>Выручка за сегодня</h3>
        <div class="value">{new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.revenue)}</div>
      </div>
      <div class="stat-card">
        <h3>Записей на сегодня</h3>
        <div class="value">{summary.appointments}</div>
      </div>
      <div class="stat-card">
        <h3>Статус подписки</h3>
        <div class="value status-badge" class:active={$profileStore.status === 'Active'}>
          {$profileStore.status}
        </div>
      </div>
    </div>
    
    <div class="quick-links card">
      <h3>Быстрые действия</h3>
      <div class="links-grid">
        <a href="#/dashboard/bot-setup" class="btn btn-secondary">Подключить Telegram-бота</a>
        <a href="#/dashboard/masters" class="btn btn-secondary">Добавить мастера</a>
        <a href="#/dashboard/settings" class="btn btn-secondary">Настроить барбершоп</a>
      </div>
    </div>
  </div>
</DashboardLayout>

<style>
  .page-header {
    margin-bottom: 2rem;
  }
  
  .page-header h1 {
    margin-bottom: 0.5rem;
  }
  
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
  }
  
  .stat-card {
    background-color: var(--bg-secondary);
    padding: 1.5rem;
    border-radius: var(--border-radius);
    border: 1px solid var(--border-color);
  }
  
  .stat-card h3 {
    font-size: 0.875rem;
    color: var(--text-secondary);
    margin-bottom: 0.5rem;
  }
  
  .stat-card .value {
    font-size: 2rem;
    font-weight: 700;
    color: var(--text-primary);
  }
  
  .status-badge {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 1rem;
    font-size: 1.25rem !important;
  }
  
  .status-badge.active {
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--success);
  }

  
  .quick-links h3 {
    margin-bottom: 1.5rem;
  }
  
  .links-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
  }
</style>

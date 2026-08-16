<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { profileStore } from '../lib/store';
  import { 
    Wallet, 
    CalendarCheck, 
    ShieldCheck, 
    Bot, 
    UserPlus, 
    SlidersHorizontal, 
    CalendarDays, 
    ArrowUpRight,
    Sparkles
  } from 'lucide-svelte';
  
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
      <div class="header-left">
        <h1>Панель управления</h1>
        <p class="header-subtitle">
          Добро пожаловать в рабочее пространство, <span class="highlight">{$profileStore.ownerId || 'Владелец'}</span>!
        </p>
      </div>
      <div class="header-badge">
        <Sparkles size={15} />
        <span>Сегодня {new Date().toLocaleDateString('ru-RU', { day: 'numeric', month: 'long' })}</span>
      </div>
    </header>

    <!-- Stats Grid -->
    <div class="stats-grid">
      <div class="stat-card stat-rose">
        <div class="stat-top">
          <span class="stat-title">Денежный оборот за сегодня</span>
          <div class="stat-icon-wrap rose">
            <Wallet size={20} />
          </div>
        </div>
        <div class="stat-value">
          {new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.revenue)}
        </div>
        <div class="stat-footer">
          <span class="stat-hint">Общий объем оказанных услуг за день</span>
        </div>
      </div>

      <div class="stat-card stat-sage">
        <div class="stat-top">
          <span class="stat-title">Записей на сегодня</span>
          <div class="stat-icon-wrap sage">
            <CalendarCheck size={20} />
          </div>
        </div>
        <div class="stat-value">
          {summary.appointments}
        </div>
        <div class="stat-footer">
          <span class="stat-hint">Активных клиентов в графике</span>
        </div>
      </div>

      <div class="stat-card stat-amber">
        <div class="stat-top">
          <span class="stat-title">Статус подписки</span>
          <div class="stat-icon-wrap amber">
            <ShieldCheck size={20} />
          </div>
        </div>
        <div class="stat-badge-wrap">
          <span class="status-pill" class:active={$profileStore.status === 'Active'}>
            {$profileStore.status || 'Active'}
          </span>
        </div>
        <div class="stat-footer">
          <a href="#/payment" class="stat-link">
            <span>Управление тарифом</span>
            <ArrowUpRight size={14} />
          </a>
        </div>
      </div>
    </div>
    
    <!-- Quick Actions -->
    <div class="card quick-actions-card">
      <div class="card-head">
        <h3>Быстрые действия</h3>
        <p>Мгновенный переход к ключевым разделам платформы</p>
      </div>

      <div class="actions-grid">
        <a href="#/dashboard/schedule" class="action-btn">
          <div class="action-icon sage">
            <CalendarDays size={22} />
          </div>
          <div class="action-text">
            <h4>Расписание</h4>
            <p>Просмотр и запись клиентов</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/masters" class="action-btn">
          <div class="action-icon rose">
            <UserPlus size={22} />
          </div>
          <div class="action-text">
            <h4>Мастера</h4>
            <p>Управление персоналом и графиками</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/bot-setup" class="action-btn">
          <div class="action-icon lavender">
            <Bot size={22} />
          </div>
          <div class="action-text">
            <h4>Telegram Бот</h4>
            <p>Интеграция и ссылка на Mini App</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/settings" class="action-btn">
          <div class="action-icon amber">
            <SlidersHorizontal size={22} />
          </div>
          <div class="action-text">
            <h4>Настройки</h4>
            <p>Профиль и параметры барбершопа</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>
      </div>
    </div>
  </div>
</DashboardLayout>

<style>
  .overview {
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 2.25rem;
    flex-wrap: wrap;
    gap: 1rem;
  }
  
  .page-header h1 {
    font-size: 2.2rem;
    margin-bottom: 0.35rem;
    letter-spacing: -0.02em;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 1rem;
  }

  .highlight {
    color: var(--pastel-rose);
    font-weight: 600;
  }

  .header-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.45rem 1rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    color: var(--text-secondary);
    font-size: 0.88rem;
    font-weight: 500;
  }
  
  /* Stats Grid */
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2.25rem;
  }
  
  .stat-card {
    background: var(--bg-surface);
    backdrop-filter: blur(18px);
    -webkit-backdrop-filter: blur(18px);
    padding: 1.75rem;
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    display: flex;
    flex-direction: column;
    transition: all 0.25s var(--ease-spring);
  }

  .stat-card:hover {
    transform: translateY(-3px);
    border-color: var(--border-glass);
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.45);
  }

  .stat-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;
  }
  
  .stat-title {
    font-size: 0.9rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .stat-icon-wrap {
    width: 42px;
    height: 42px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .stat-icon-wrap.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.25);
  }

  .stat-icon-wrap.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
  }

  .stat-icon-wrap.amber {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
  }
  
  .stat-value {
    font-size: 2.2rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    color: var(--text-primary);
    margin-bottom: 0.75rem;
  }

  .stat-badge-wrap {
    margin-bottom: 1rem;
  }
  
  .status-pill {
    display: inline-flex;
    padding: 0.35rem 0.85rem;
    border-radius: var(--radius-pill);
    font-size: 0.95rem;
    font-weight: 700;
    background-color: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
  }
  
  .status-pill.active {
    background-color: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border-color: rgba(152, 193, 169, 0.3);
    box-shadow: 0 0 12px var(--pastel-sage-glow);
  }

  .stat-footer {
    margin-top: auto;
    font-size: 0.85rem;
    color: var(--text-muted);
  }

  .stat-link {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    color: var(--pastel-rose);
    font-weight: 600;
  }

  /* Quick Actions Card */
  .quick-actions-card {
    padding: 2rem;
  }
  
  .card-head {
    margin-bottom: 1.75rem;
  }

  .card-head h3 {
    font-size: 1.3rem;
    margin-bottom: 0.25rem;
  }

  .card-head p {
    font-size: 0.9rem;
    color: var(--text-secondary);
  }
  
  .actions-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    gap: 1.25rem;
  }

  .action-btn {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1.25rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    transition: all 0.25s var(--ease-spring);
    position: relative;
  }

  .action-btn:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
    transform: translateY(-2px);
  }

  .action-icon {
    width: 48px;
    height: 48px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .action-icon.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
  }

  .action-icon.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
  }

  .action-icon.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
  }

  .action-icon.amber {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
  }

  .action-text {
    flex: 1;
    overflow: hidden;
  }

  .action-text h4 {
    font-size: 1rem;
    margin-bottom: 0.2rem;
    color: var(--text-primary);
  }

  .action-text p {
    font-size: 0.82rem;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  :global(.action-arrow) {
    color: var(--text-muted);
    transition: transform 0.2s var(--ease-spring), color 0.2s;
  }

  .action-btn:hover :global(.action-arrow) {
    transform: translate(2px, -2px);
    color: var(--pastel-rose);
  }
</style>

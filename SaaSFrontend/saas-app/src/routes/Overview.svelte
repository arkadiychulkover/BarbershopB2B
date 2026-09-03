<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { profileStore } from '../lib/store';
  import { currentLocale } from '../lib/locale.js';
  import { m } from '../lib/paraglide/messages.js';
  import { 
    Wallet, 
    CalendarCheck, 
    ShieldCheck, 
    Bot, 
    UserPlus, 
    SlidersHorizontal, 
    CalendarDays, 
    ArrowUpRight,
    Sparkles,
    Check
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
        <h1>{m.nav_dashboard()}</h1>
        <p class="header-subtitle">
          {m.overview_welcome()} <span class="highlight">{$profileStore.ownerName || $profileStore.barbershopName || 'Owner'}</span>!
        </p>
      </div>
      <div class="header-badge">
        <Sparkles size={15} />
        <span>{m.overview_today()} {new Date().toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', { day: 'numeric', month: 'long' })}</span>
      </div>
    </header>

    <!-- Stats Grid -->
    <div class="stats-grid">
      <div class="stat-card stat-rose">
        <div class="stat-top">
          <span class="stat-title">{m.overview_revenue_today()}</span>
          <div class="stat-icon-wrap rose">
            <Wallet size={20} />
          </div>
        </div>
        <div class="stat-value">
          {new Intl.NumberFormat($currentLocale === 'ru' ? 'uk-UA' : 'en-US', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.revenue)}
        </div>
        <div class="stat-footer">
          <span class="stat-hint">{m.overview_revenue_hint()}</span>
        </div>
      </div>

      <div class="stat-card stat-sage">
        <div class="stat-top">
          <span class="stat-title">{m.overview_appts_today()}</span>
          <div class="stat-icon-wrap sage">
            <CalendarCheck size={20} />
          </div>
        </div>
        <div class="stat-value">
          {summary.appointments}
        </div>
        <div class="stat-footer">
          <span class="stat-hint">{m.overview_appts_hint()}</span>
        </div>
      </div>

      <div class="stat-card stat-amber subscription-card">
        <div class="stat-top">
          <div>
            <span class="stat-title">{m.overview_subscription()}</span>
            <div class="subscription-plan-tag">{m.overview_sub_pro()}</div>
          </div>
          <div class="stat-icon-wrap amber">
            <ShieldCheck size={20} />
          </div>
        </div>
        
        <div class="stat-badge-wrap subscription-badge-row">
          <span class="status-pill" class:active={$profileStore.status === 'Active'}>
            <span class="status-dot"></span>
            {$profileStore.status === 'Active' ? m.status_active() : ($profileStore.status || m.status_active())}
          </span>
          <span class="subscription-period-hint">{m.overview_sub_unlimited()}</span>
        </div>

        <div class="subscription-perks">
          <div class="perk-item">
            <Check size={13} class="perk-icon" />
            <span>{m.overview_sub_perk1()}</span>
          </div>
          <div class="perk-item">
            <Check size={13} class="perk-icon" />
            <span>{m.overview_sub_perk2()}</span>
          </div>
        </div>

        <div class="stat-footer">
          <a href="#/dashboard/settings" class="stat-link">
            <span>{m.overview_sub_manage()}</span>
            <ArrowUpRight size={14} />
          </a>
        </div>
      </div>
    </div>
    
    <!-- Quick Actions -->
    <div class="card quick-actions-card">
      <div class="card-head">
        <h3>{m.overview_quick_actions()}</h3>
        <p>{m.overview_quick_sub()}</p>
      </div>

      <div class="actions-grid">
        <a href="#/dashboard/schedule" class="action-btn">
          <div class="action-icon sage">
            <CalendarDays size={22} />
          </div>
          <div class="action-text">
            <h4>{m.nav_schedule()}</h4>
            <p>{m.overview_qa_schedule_sub()}</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/masters" class="action-btn">
          <div class="action-icon rose">
            <UserPlus size={22} />
          </div>
          <div class="action-text">
            <h4>{m.nav_masters()}</h4>
            <p>{m.overview_qa_masters_sub()}</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/bot-setup" class="action-btn">
          <div class="action-icon lavender">
            <Bot size={22} />
          </div>
          <div class="action-text">
            <h4>{m.nav_bot_setup()}</h4>
            <p>{m.overview_qa_bot_sub()}</p>
          </div>
          <ArrowUpRight size={18} class="action-arrow" />
        </a>

        <a href="#/dashboard/settings" class="action-btn">
          <div class="action-icon amber">
            <SlidersHorizontal size={22} />
          </div>
          <div class="action-text">
            <h4>{m.nav_settings()}</h4>
            <p>{m.overview_qa_settings_sub()}</p>
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
    align-items: center;
    gap: 0.45rem;
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

  .status-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background-color: currentColor;
    display: inline-block;
    box-shadow: 0 0 6px currentColor;
    animation: pulse-dot 2s infinite ease-in-out;
  }

  @keyframes pulse-dot {
    0%, 100% { opacity: 1; transform: scale(1); }
    50% { opacity: 0.4; transform: scale(0.85); }
  }

  .subscription-card {
    display: flex;
    flex-direction: column;
    justify-content: space-between;
  }

  .subscription-plan-tag {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--pastel-amber);
    margin-top: 0.2rem;
  }

  .subscription-badge-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.75rem;
    flex-wrap: wrap;
  }

  .subscription-period-hint {
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--text-muted);
  }

  .subscription-perks {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    margin-bottom: 1rem;
    padding: 0.55rem 0.65rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
  }

  .perk-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.78rem;
    font-weight: 500;
    color: var(--text-secondary);
  }

  :global(.perk-icon) {
    color: var(--pastel-sage);
    flex-shrink: 0;
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

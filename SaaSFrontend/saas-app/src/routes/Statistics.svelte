<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { 
    Wallet, 
    Coins, 
    TrendingUp, 
    Users, 
    Calendar, 
    Clock, 
    Receipt, 
    ArrowUpRight,
    Sparkles,
    Copy,
    Check,
    ChevronLeft,
    ChevronRight,
    CalendarRange,
    Sun,
    CalendarDays
  } from 'lucide-svelte';
  
  let isLoading = true;
  let selectedPeriod = 'week'; // 'day' | 'week' | 'month' | 'custom'
  
  // Date state
  let singleDate = formatDateInput(new Date());
  let customStartDate = formatDateInput(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  let customEndDate = formatDateInput(new Date());
  
  // Offsets for week / month navigation
  let weekOffset = 0; // 0 = current week
  let monthOffset = 0; // 0 = current month
  
  let statsData = [];
  let hourlyData = [];
  let transactions = [];
  let summary = { totalRevenue: 0, totalClients: 0, avgCheck: 0, activeDays: 0 };
  let copiedHash = null;

  function formatDateInput(d) {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  function getMonday(d) {
    const date = new Date(d);
    const day = date.getDay();
    const diff = date.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(date.setDate(diff));
  }

  function copyTx(hash) {
    if (!hash) return;
    navigator.clipboard.writeText(hash);
    copiedHash = hash;
    setTimeout(() => {
      if (copiedHash === hash) copiedHash = null;
    }, 2000);
  }

  onMount(async () => {
    await fetchStatistics();
    await fetchTransactions();
  });

  async function fetchTransactions() {
    try {
      const txData = await apiRequest('/api/Statistic').catch(() => []);
      if (txData) transactions = txData;
    } catch(e) {
      console.error(e);
    }
  }

  async function setPeriod(period) {
    selectedPeriod = period;
    if (period === 'day') {
      singleDate = formatDateInput(new Date());
    } else if (period === 'week') {
      weekOffset = 0;
    } else if (period === 'month') {
      monthOffset = 0;
    }
    await fetchStatistics();
  }

  // Navigation handlers
  function changeDay(delta) {
    const d = new Date(singleDate);
    d.setDate(d.getDate() + delta);
    singleDate = formatDateInput(d);
    fetchStatistics();
  }

  function changeWeek(delta) {
    weekOffset += delta;
    fetchStatistics();
  }

  function changeMonth(delta) {
    monthOffset += delta;
    fetchStatistics();
  }

  async function fetchStatistics() {
    isLoading = true;
    statsData = [];
    hourlyData = [];
    summary = { totalRevenue: 0, totalClients: 0, avgCheck: 0, activeDays: 0 };

    try {
      if (selectedPeriod === 'day') {
        const dateStr = singleDate;
        const [dailyRes, hourlyRes] = await Promise.all([
          apiRequest(`/api/Statistic/owner-statistic-daily?startDate=${dateStr}&endDate=${dateStr}`).catch(() => ({})),
          apiRequest(`/api/Statistic/owner-statistic-hourly?date=${dateStr}`).catch(() => ({}))
        ]);

        if (dailyRes && dailyRes[dateStr]) {
          summary.totalRevenue = dailyRes[dateStr].ownerProfit;
          summary.totalClients = dailyRes[dateStr].clientsCount;
          summary.activeDays = summary.totalClients > 0 ? 1 : 0;
        }

        if (hourlyRes) {
          hourlyData = Object.keys(hourlyRes).map(h => {
            const hourNum = parseInt(h);
            const item = hourlyRes[h];
            return {
              hour: `${String(hourNum).padStart(2, '0')}:00`,
              revenue: item.ownerProfit,
              clients: item.clientsCount
            };
          }).filter(h => {
            // Keep hours between 08:00 and 22:00 or hours with data
            const hourInt = parseInt(h.hour);
            return (hourInt >= 8 && hourInt <= 22) || h.clients > 0;
          });
        }
      } else {
        let startStr = '';
        let endStr = '';

        if (selectedPeriod === 'week') {
          const today = new Date();
          today.setDate(today.getDate() + (weekOffset * 7));
          const monday = getMonday(today);
          const sunday = new Date(monday);
          sunday.setDate(sunday.getDate() + 6);
          startStr = formatDateInput(monday);
          endStr = formatDateInput(sunday);
        } else if (selectedPeriod === 'month') {
          const now = new Date();
          const targetMonth = new Date(now.getFullYear(), now.getMonth() + monthOffset, 1);
          const lastDay = new Date(targetMonth.getFullYear(), targetMonth.getMonth() + 1, 0);
          startStr = formatDateInput(targetMonth);
          endStr = formatDateInput(lastDay);
        } else if (selectedPeriod === 'custom') {
          startStr = customStartDate;
          endStr = customEndDate;
        }

        if (startStr && endStr) {
          const data = await apiRequest(`/api/Statistic/owner-statistic-daily?startDate=${startStr}&endDate=${endStr}`);
          if (data) {
            let activeDaysCount = 0;
            statsData = Object.keys(data).map(key => {
              const item = data[key];
              summary.totalRevenue += item.ownerProfit;
              summary.totalClients += item.clientsCount;
              if (item.clientsCount > 0) activeDaysCount++;
              return {
                rawDate: key,
                date: new Date(key).toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', weekday: 'short' }),
                revenue: item.ownerProfit,
                clients: item.clientsCount
              };
            });
            summary.activeDays = activeDaysCount;
          }
        }
      }

      if (summary.totalClients > 0) {
        summary.avgCheck = summary.totalRevenue / summary.totalClients;
      }
    } catch (e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  }

  // Label helpers
  $: currentRangeLabel = (() => {
    if (selectedPeriod === 'day') {
      const d = new Date(singleDate);
      return d.toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });
    }
    if (selectedPeriod === 'week') {
      const today = new Date();
      today.setDate(today.getDate() + (weekOffset * 7));
      const monday = getMonday(today);
      const sunday = new Date(monday);
      sunday.setDate(sunday.getDate() + 6);
      return `${monday.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' })} — ${sunday.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric' })}`;
    }
    if (selectedPeriod === 'month') {
      const now = new Date();
      const targetMonth = new Date(now.getFullYear(), now.getMonth() + monthOffset, 1);
      return targetMonth.toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' });
    }
    return `${customStartDate} — ${customEndDate}`;
  })();
</script>

<DashboardLayout>
  <div class="statistics-page">
    <!-- Header -->
    <header class="page-header">
      <div class="header-left">
        <h1>Финансовая аналитика</h1>
        <p class="header-subtitle">Анализ доходности заведения, загруженности мастеров и потока клиентов</p>
      </div>

      <!-- Period Tabs -->
      <div class="period-tabs">
        <button 
          class="period-tab" 
          class:active={selectedPeriod === 'day'} 
          on:click={() => setPeriod('day')}
        >
          <Sun size={15} />
          <span>День</span>
        </button>
        <button 
          class="period-tab" 
          class:active={selectedPeriod === 'week'} 
          on:click={() => setPeriod('week')}
        >
          <CalendarDays size={15} />
          <span>Неделя</span>
        </button>
        <button 
          class="period-tab" 
          class:active={selectedPeriod === 'month'} 
          on:click={() => setPeriod('month')}
        >
          <Calendar size={15} />
          <span>Месяц</span>
        </button>
        <button 
          class="period-tab" 
          class:active={selectedPeriod === 'custom'} 
          on:click={() => setPeriod('custom')}
        >
          <CalendarRange size={15} />
          <span>Свой период</span>
        </button>
      </div>
    </header>

    <!-- Controls Bar -->
    <div class="controls-bar card">
      <div class="nav-controls">
        {#if selectedPeriod === 'day'}
          <button class="nav-btn" on:click={() => changeDay(-1)} title="Предыдущий день">
            <ChevronLeft size={18} />
          </button>
          <div class="date-input-wrap">
            <Calendar size={16} class="input-icon" />
            <input 
              type="date" 
              class="input has-icon date-picker" 
              bind:value={singleDate} 
              on:change={fetchStatistics} 
            />
          </div>
          <button class="nav-btn" on:click={() => changeDay(1)} title="Следующий день">
            <ChevronRight size={18} />
          </button>
          <button class="btn btn-secondary btn-sm" on:click={() => { singleDate = formatDateInput(new Date()); fetchStatistics(); }}>
            Сегодня
          </button>

        {:else if selectedPeriod === 'week'}
          <button class="nav-btn" on:click={() => changeWeek(-1)} title="Предыдущая неделя">
            <ChevronLeft size={18} />
          </button>
          <div class="range-display">
            <CalendarDays size={16} class="range-icon" />
            <span class="range-text">{currentRangeLabel}</span>
          </div>
          <button class="nav-btn" on:click={() => changeWeek(1)} title="Следующая неделя">
            <ChevronRight size={18} />
          </button>
          {#if weekOffset !== 0}
            <button class="btn btn-secondary btn-sm" on:click={() => { weekOffset = 0; fetchStatistics(); }}>
              Текущая неделя
            </button>
          {/if}

        {:else if selectedPeriod === 'month'}
          <button class="nav-btn" on:click={() => changeMonth(-1)} title="Предыдущий месяц">
            <ChevronLeft size={18} />
          </button>
          <div class="range-display">
            <Calendar size={16} class="range-icon" />
            <span class="range-text">{currentRangeLabel}</span>
          </div>
          <button class="nav-btn" on:click={() => changeMonth(1)} title="Следующий месяц">
            <ChevronRight size={18} />
          </button>
          {#if monthOffset !== 0}
            <button class="btn btn-secondary btn-sm" on:click={() => { monthOffset = 0; fetchStatistics(); }}>
              Текущий месяц
            </button>
          {/if}

        {:else if selectedPeriod === 'custom'}
          <div class="custom-range-form">
            <div class="custom-field">
              <label for="customStart">От:</label>
              <input 
                id="customStart" 
                type="date" 
                class="input date-picker" 
                bind:value={customStartDate} 
              />
            </div>
            <div class="custom-field">
              <label for="customEnd">До:</label>
              <input 
                id="customEnd" 
                type="date" 
                class="input date-picker" 
                bind:value={customEndDate} 
              />
            </div>
            <button class="btn btn-primary btn-sm" on:click={fetchStatistics}>
              Показать
            </button>
          </div>
        {/if}
      </div>

      <div class="period-badge">
        <Sparkles size={14} />
        <span>{currentRangeLabel}</span>
      </div>
    </div>

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>Формирование аналитики...</p>
      </div>
    {:else}
      <!-- Stats Grid -->
      <div class="stats-grid">
        <div class="stat-card stat-rose">
          <div class="stat-top">
            <span class="stat-title">
              {selectedPeriod === 'day' ? 'Выручка за день' : 'Суммарная выручка'}
            </span>
            <div class="stat-icon-wrap rose">
              <TrendingUp size={20} />
            </div>
          </div>
          <div class="stat-value">
            {new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.totalRevenue)}
          </div>
          <div class="stat-footer">
            <span>Доход заведения за выбранный период</span>
          </div>
        </div>

        <div class="stat-card stat-sage">
          <div class="stat-top">
            <span class="stat-title">Обслужено клиентов</span>
            <div class="stat-icon-wrap sage">
              <Users size={20} />
            </div>
          </div>
          <div class="stat-value">
            {summary.totalClients}
          </div>
          <div class="stat-footer">
            <span>
              {selectedPeriod === 'day' ? 'Клиентов в этот день' : `За ${summary.activeDays} активных дней`}
            </span>
          </div>
        </div>

        <div class="stat-card stat-lavender">
          <div class="stat-top">
            <span class="stat-title">Средний чек</span>
            <div class="stat-icon-wrap lavender">
              <Receipt size={20} />
            </div>
          </div>
          <div class="stat-value">
            {summary.avgCheck > 0 
              ? new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.avgCheck)
              : '0 ₴'}
          </div>
          <div class="stat-footer">
            <span>Средний расход на клиента</span>
          </div>
        </div>
      </div>
      
      <!-- Day Mode Hourly Breakdown -->
      {#if selectedPeriod === 'day'}
        <div class="card table-card mb-4">
          <div class="card-head">
            <div class="head-title-wrap">
              <Clock size={20} class="title-icon" />
              <div>
                <h3>Почасовая загрузка за день</h3>
                <p>Распределение визитов клиентов и выручки по часам</p>
              </div>
            </div>
          </div>

          <div class="table-responsive">
            <table class="table">
              <thead>
                <tr>
                  <th>Время</th>
                  <th>Клиентов</th>
                  <th>Выручка за час</th>
                  <th>Загруженность</th>
                </tr>
              </thead>
              <tbody>
                {#each hourlyData as row}
                  <tr class:has-data={row.clients > 0}>
                    <td class="cell-hour">
                      <strong>{row.hour}</strong>
                    </td>
                    <td>
                      <span class="clients-pill" class:active={row.clients > 0}>
                        {row.clients} чел.
                      </span>
                    </td>
                    <td class="cell-revenue">
                      {new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(row.revenue)}
                    </td>
                    <td class="cell-progress">
                      <div class="mini-bar-track">
                        <div 
                          class="mini-bar-fill" 
                          style="width: {Math.min(100, row.clients * 35)}%;"
                        ></div>
                      </div>
                    </td>
                  </tr>
                {/each}
                {#if hourlyData.length === 0}
                  <tr>
                    <td colspan="4" class="empty-cell">Нет записей на этот день</td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </div>

      <!-- Week / Month / Custom Mode Daily Breakdown -->
      {:else}
        <div class="card table-card mb-4">
          <div class="card-head">
            <div class="head-title-wrap">
              <CalendarDays size={20} class="title-icon" />
              <div>
                <h3>Динамика по дням</h3>
                <p>Детализация выручки и потока клиентов</p>
              </div>
            </div>
          </div>

          <div class="table-responsive">
            <table class="table">
              <thead>
                <tr>
                  <th>Дата</th>
                  <th>Выручка за день</th>
                  <th>Кол-во клиентов</th>
                  <th>Средний чек дня</th>
                </tr>
              </thead>
              <tbody>
                {#each statsData as row}
                  <tr class:has-data={row.clients > 0}>
                    <td class="cell-date">{row.date}</td>
                    <td class="cell-revenue">
                      {new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(row.revenue)}
                    </td>
                    <td>
                      <span class="clients-pill" class:active={row.clients > 0}>
                        {row.clients} чел.
                      </span>
                    </td>
                    <td class="cell-avg">
                      {row.clients > 0 
                        ? new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(row.revenue / row.clients) 
                        : '—'}
                    </td>
                  </tr>
                {/each}
                {#if statsData.length === 0}
                  <tr>
                    <td colspan="4" class="empty-cell">Нет данных за выбранный диапазон</td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </div>
      {/if}

      <!-- Transactions History Table -->
      <div class="card table-card">
        <div class="card-head">
          <div class="head-title-wrap">
            <Wallet size={20} class="title-icon" />
            <div>
              <h3>История транзакций</h3>
              <p>Поступления выручки и транзакции подписки в сети TON</p>
            </div>
          </div>
        </div>

        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Дата и время</th>
                <th>Назначение</th>
                <th>Сумма</th>
                <th>Хэш транзакции (TON)</th>
              </tr>
            </thead>
            <tbody>
              {#each transactions as tx}
                <tr>
                  <td class="cell-date">{new Date(tx.time).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}</td>
                  <td>
                    {#if tx.txhHash}
                      <span class="tx-badge sub">
                        <Wallet size={13} />
                        <span>Подписка SaaS</span>
                      </span>
                    {:else}
                      <span class="tx-badge rev">
                        <Coins size={13} />
                        <span>Выручка</span>
                      </span>
                    {/if}
                  </td>
                  <td class="cell-amount">
                    {#if tx.txhHash}
                      <span class="ton-amount">{tx.amount} TON</span>
                    {:else}
                      <span class="uah-amount">{new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(tx.amount)}</span>
                    {/if}
                  </td>
                  <td>
                    {#if tx.txhHash}
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <div class="hash-tag" on:click={() => copyTx(tx.txhHash)} title="Скопировать хэш">
                        <code>{tx.txhHash.substring(0, 8)}...{tx.txhHash.substring(tx.txhHash.length - 6)}</code>
                        {#if copiedHash === tx.txhHash}
                          <Check size={12} class="copy-icon check" />
                        {:else}
                          <Copy size={12} class="copy-icon" />
                        {/if}
                      </div>
                    {:else}
                      <span class="no-hash">—</span>
                    {/if}
                  </td>
                </tr>
              {/each}
              {#if transactions.length === 0}
                <tr>
                  <td colspan="4" class="empty-cell">История транзакций пуста</td>
                </tr>
              {/if}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .statistics-page {
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
    gap: 1.25rem;
  }

  .header-left h1 {
    font-size: 2.2rem;
    margin-bottom: 0.35rem;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 1rem;
  }

  /* Period Tabs */
  .period-tabs {
    display: flex;
    gap: 0.4rem;
    background: var(--bg-surface);
    padding: 0.35rem;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
    backdrop-filter: blur(16px);
  }

  .period-tab {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.5rem 1.1rem;
    border-radius: var(--radius-pill);
    background: transparent;
    border: none;
    color: var(--text-secondary);
    font-size: 0.88rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.22s var(--ease-spring);
  }

  .period-tab:hover {
    color: var(--text-primary);
    background: rgba(255, 255, 255, 0.04);
  }

  .period-tab.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    box-shadow: 0 2px 10px var(--pastel-rose-glow);
  }

  /* Controls Bar */
  .controls-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    margin-bottom: 2rem;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .nav-controls {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .nav-btn {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s;
  }

  .nav-btn:hover {
    color: var(--pastel-rose);
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }

  .range-display {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.45rem 1rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
  }

  :global(.range-icon) {
    color: var(--pastel-rose);
  }

  .range-text {
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--text-primary);
    text-transform: capitalize;
  }

  .date-input-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  .date-picker {
    padding: 0.45rem 0.85rem;
    font-size: 0.9rem;
    border-radius: var(--radius-md);
  }

  .date-input-wrap .date-picker {
    padding-left: 2.5rem;
  }

  :global(.date-input-wrap .input-icon) {
    position: absolute;
    left: 0.85rem;
    color: var(--pastel-rose);
    pointer-events: none;
  }

  .custom-range-form {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .custom-field {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
    font-weight: 600;
  }

  .period-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.4rem 0.9rem;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.25);
    border-radius: var(--radius-pill);
    font-size: 0.85rem;
    font-weight: 600;
    text-transform: capitalize;
  }

  /* Loading */
  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 5rem 0;
    color: var(--text-secondary);
    gap: 1rem;
  }

  .spinner-sm {
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    width: 32px;
    height: 32px;
    animation: spinSmooth 0.85s linear infinite;
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

  .stat-icon-wrap.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.25);
  }

  .stat-value {
    font-size: 2.2rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    color: var(--text-primary);
    margin-bottom: 0.75rem;
  }

  .stat-footer {
    margin-top: auto;
    font-size: 0.82rem;
    color: var(--text-muted);
  }

  /* Table Cards */
  .table-card {
    padding: 2rem;
  }

  .card-head {
    margin-bottom: 1.5rem;
  }

  .head-title-wrap {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  :global(.title-icon) {
    color: var(--pastel-rose);
  }

  .head-title-wrap h3 {
    font-size: 1.3rem;
    margin-bottom: 0.2rem;
  }

  .head-title-wrap p {
    color: var(--text-secondary);
    font-size: 0.88rem;
  }

  .table-responsive {
    overflow-x: auto;
  }
  
  .table {
    width: 100%;
    border-collapse: collapse;
  }
  
  .table th {
    padding: 0.85rem 1rem;
    text-align: left;
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    border-bottom: 1px solid var(--border-subtle);
  }

  .table td {
    padding: 1.1rem 1rem;
    text-align: left;
    border-bottom: 1px solid var(--border-subtle);
    font-size: 0.93rem;
  }

  .table tbody tr {
    transition: background-color 0.2s;
  }

  .table tbody tr:hover {
    background-color: rgba(255, 255, 255, 0.02);
  }

  .cell-date, .cell-hour {
    font-weight: 600;
    color: var(--text-primary);
  }

  .cell-revenue {
    font-weight: 700;
    color: var(--pastel-rose);
  }

  .cell-avg {
    color: var(--pastel-lavender);
    font-weight: 600;
  }

  .clients-pill {
    display: inline-block;
    padding: 0.25rem 0.65rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-muted);
  }

  .clients-pill.active {
    color: var(--pastel-sage);
    border-color: rgba(152, 193, 169, 0.3);
    background: var(--pastel-sage-dim);
  }

  .cell-progress {
    width: 160px;
  }

  .mini-bar-track {
    height: 8px;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-pill);
    overflow: hidden;
    width: 100%;
    border: 1px solid var(--border-subtle);
  }

  .mini-bar-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--pastel-rose), var(--pastel-sage));
    border-radius: var(--radius-pill);
    transition: width 0.3s ease;
  }

  .tx-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.25rem 0.7rem;
    border-radius: var(--radius-pill);
    font-size: 0.82rem;
    font-weight: 600;
  }

  .tx-badge.sub {
    background-color: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
  }

  .tx-badge.rev {
    background-color: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
  }

  .ton-amount {
    font-weight: 700;
    color: var(--pastel-amber);
  }

  .uah-amount {
    font-weight: 700;
    color: var(--pastel-sage);
  }

  .hash-tag {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.3rem 0.6rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: all 0.2s;
  }

  .hash-tag:hover {
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }

  .hash-tag code {
    font-family: monospace;
    font-size: 0.82rem;
    color: var(--text-secondary);
  }

  :global(.copy-icon) {
    color: var(--text-muted);
  }

  :global(.copy-icon.check) {
    color: var(--pastel-sage);
  }

  .no-hash {
    color: var(--text-muted);
  }

  .empty-cell {
    text-align: center;
    color: var(--text-muted);
    padding: 3rem 1rem !important;
  }

  .mb-4 {
    margin-bottom: 2rem;
  }
</style>

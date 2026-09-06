<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest, BASE_URL } from '../lib/api';
  import { authStore } from '../lib/store';
  import { m } from '../lib/paraglide/messages.js';
  import { currentLocale } from '../lib/locale.js';
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
    CalendarDays,
    FileSpreadsheet,
    Activity,
    BarChart2,
    Download
  } from 'lucide-svelte';
  
  let isLoading = true;
  let isExporting = false;
  let selectedPeriod = '30d'; // '7d' | '30d' | '90d' | 'year' | 'custom'

  function formatDateInput(d) {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  let customStartDate = formatDateInput(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  let customEndDate = formatDateInput(new Date());
  
  let chartData = {
    points: [],
    totalVisits: 0,
    totalRevenue: 0,
    averageDailyVisits: 0,
    averageDailyRevenue: 0,
    peakDate: '—',
    peakRevenue: 0
  };

  let transactions = [];
  let copiedHash = null;
  let hoveredPoint = null;
  let tooltipX = 0;
  let tooltipY = 0;

  let exportSuccessMsg = '';
  let exportErrorMsg = '';

  onMount(async () => {
    await fetchChartAnalytics();
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
    await fetchChartAnalytics();
  }

  async function fetchChartAnalytics() {
    isLoading = true;
    try {
      let url = `/api/Statistic/owner-chart-analytics?period=${selectedPeriod}`;
      if (selectedPeriod === 'custom') {
        url += `&startDate=${customStartDate}&endDate=${customEndDate}`;
      }
      const res = await apiRequest(url);
      if (res) {
        chartData = res;
      }
    } catch (e) {
      console.error('Failed to load chart analytics:', e);
    } finally {
      isLoading = false;
    }
  }

  async function exportToExcel() {
    isExporting = true;
    exportSuccessMsg = '';
    exportErrorMsg = '';
    try {
      const token = $authStore.token;
      const res = await fetch(`${BASE_URL}/api/Statistic/export-clients`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (!res.ok) throw new Error(m.stats_excel_create_error());

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `clients_report_${new Date().toISOString().slice(0,10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      exportSuccessMsg = m.stats_excel_download_success();
      setTimeout(() => { exportSuccessMsg = ''; }, 4000);
    } catch (e) {
      exportErrorMsg = e.message || m.stats_excel_export_error();
      setTimeout(() => { exportErrorMsg = ''; }, 4000);
    } finally {
      isExporting = false;
    }
  }

  function copyTx(hash) {
    if (!hash) return;
    navigator.clipboard.writeText(hash);
    copiedHash = hash;
    setTimeout(() => {
      if (copiedHash === hash) copiedHash = null;
    }, 2000);
  }

  function formatCurrency(num) {
    return new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(num || 0);
  }

  // SVG Chart Geometry Calculations
  const svgWidth = 860;
  const svgHeight = 260;
  const padding = { top: 25, right: 30, bottom: 40, left: 60 };

  $: chartWidth = svgWidth - padding.left - padding.right;
  $: chartHeight = svgHeight - padding.top - padding.bottom;

  $: maxRevenue = Math.max(...(chartData.points || []).map(p => p.revenue), 100);
  $: maxVisits = Math.max(...(chartData.points || []).map(p => p.visits), 5);

  $: revenuePoints = (chartData.points || []).map((p, i) => {
    const x = padding.left + (i / Math.max(chartData.points.length - 1, 1)) * chartWidth;
    const y = padding.top + chartHeight - (p.revenue / maxRevenue) * chartHeight;
    return { ...p, x, y };
  });

  $: visitsPoints = (chartData.points || []).map((p, i) => {
    const x = padding.left + (i / Math.max(chartData.points.length - 1, 1)) * chartWidth;
    const y = padding.top + chartHeight - (p.visits / maxVisits) * chartHeight;
    return { ...p, x, y };
  });

  function makeLinePath(points) {
    if (!points || points.length === 0) return '';
    return points.reduce((acc, p, i) => {
      if (i === 0) return `M ${p.x} ${p.y}`;
      const prev = points[i - 1];
      const cx1 = prev.x + (p.x - prev.x) / 2;
      const cy1 = prev.y;
      const cx2 = prev.x + (p.x - prev.x) / 2;
      const cy2 = p.y;
      return `${acc} C ${cx1} ${cy1}, ${cx2} ${cy2}, ${p.x} ${p.y}`;
    }, '');
  }

  function makeAreaPath(points) {
    if (!points || points.length === 0) return '';
    const line = makeLinePath(points);
    const lastX = points[points.length - 1].x;
    const firstX = points[0].x;
    const bottomY = padding.top + chartHeight;
    return `${line} L ${lastX} ${bottomY} L ${firstX} ${bottomY} Z`;
  }

  $: revenueLinePath = makeLinePath(revenuePoints);
  $: revenueAreaPath = makeAreaPath(revenuePoints);

  $: visitsLinePath = makeLinePath(visitsPoints);
  $: visitsAreaPath = makeAreaPath(visitsPoints);

  function handlePointHover(pt, type, e) {
    hoveredPoint = { ...pt, type };
    tooltipX = pt.x;
    tooltipY = pt.y - 12;
  }

  function handlePointLeave() {
    hoveredPoint = null;
  }
</script>

<DashboardLayout>
  <div class="statistics-page">
    {#if exportSuccessMsg}
      <div class="toast toast-success">
        <Check size={18} />
        <span>{exportSuccessMsg}</span>
      </div>
    {/if}

    {#if exportErrorMsg}
      <div class="toast toast-danger">
        <span>{exportErrorMsg}</span>
      </div>
    {/if}

    <!-- Header -->
    <header class="page-header">
      <div class="header-left">
        <h1>{m.stats_title()}</h1>
        <p class="header-subtitle">{m.stats_subtitle()}</p>
      </div>

      <div class="header-actions">
        <!-- Excel Export Button -->
        <button class="btn btn-secondary" on:click={exportToExcel} disabled={isExporting} title={m.stats_export_tooltip()}>
          <FileSpreadsheet size={16} />
          <span>{isExporting ? m.common_loading() : m.stats_export_btn()}</span>
        </button>

        <!-- Period Tabs -->
        <div class="period-tabs">
          <button 
            class="period-tab" 
            class:active={selectedPeriod === '7d'} 
            on:click={() => setPeriod('7d')}
          >
            <span>{m.stats_range_7d()}</span>
          </button>
          <button 
            class="period-tab" 
            class:active={selectedPeriod === '30d'} 
            on:click={() => setPeriod('30d')}
          >
            <span>{m.stats_range_30d()}</span>
          </button>
          <button 
            class="period-tab" 
            class:active={selectedPeriod === '90d'} 
            on:click={() => setPeriod('90d')}
          >
            <span>{m.stats_range_3m()}</span>
          </button>
          <button 
            class="period-tab" 
            class:active={selectedPeriod === 'year'} 
            on:click={() => setPeriod('year')}
          >
            <span>{m.stats_range_12m()}</span>
          </button>
          <button 
            class="period-tab" 
            class:active={selectedPeriod === 'custom'} 
            on:click={() => setPeriod('custom')}
          >
            <CalendarRange size={14} />
            <span>{m.stats_range_custom()}</span>
          </button>
        </div>
      </div>
    </header>

    <!-- Custom Date Range Bar -->
    {#if selectedPeriod === 'custom'}
      <div class="custom-range-card card mb-4">
        <div class="custom-range-inner">
          <div class="custom-range-title">
            <CalendarRange size={18} class="text-rose" />
            <span>{m.stats_custom_hint()}</span>
          </div>

          <div class="custom-range-inputs">
            <div class="date-input-group">
              <label for="ownerCustomStart">{m.stats_from()}</label>
              <input 
                id="ownerCustomStart" 
                type="date" 
                class="input date-field" 
                bind:value={customStartDate} 
              />
            </div>

            <div class="date-input-group">
              <label for="ownerCustomEnd">{m.stats_to()}</label>
              <input 
                id="ownerCustomEnd" 
                type="date" 
                class="input date-field" 
                bind:value={customEndDate} 
              />
            </div>

            <button class="btn btn-primary btn-sm" on:click={fetchChartAnalytics} disabled={isLoading}>
              <span>{isLoading ? m.common_loading() : m.stats_apply()}</span>
            </button>
          </div>
        </div>
      </div>
    {/if}

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>{m.stats_building_charts()}</p>
      </div>
    {:else}
      <!-- Key Metric Cards -->
      <div class="stats-grid">
        <div class="stat-card stat-rose">
          <div class="stat-top">
            <span class="stat-title">{m.stats_total_revenue()}</span>
            <div class="stat-icon-wrap rose">
              <TrendingUp size={20} />
            </div>
          </div>
          <div class="stat-value">
            {formatCurrency(chartData.totalRevenue)}
          </div>
          <div class="stat-footer">
            <span>{m.stats_avg_daily_revenue()}: <strong>{formatCurrency(chartData.averageDailyRevenue)}</strong></span>
          </div>
        </div>

        <div class="stat-card stat-sage">
          <div class="stat-top">
            <span class="stat-title">{m.stats_total_visits()}</span>
            <div class="stat-icon-wrap sage">
              <Users size={20} />
            </div>
          </div>
          <div class="stat-value">
            {chartData.totalVisits}
          </div>
          <div class="stat-footer">
            <span>{m.stats_avg_daily_visits_prefix()} <strong>{chartData.averageDailyVisits}</strong> {m.stats_avg_daily_visits_suffix()}</span>
          </div>
        </div>

        <div class="stat-card stat-lavender">
          <div class="stat-top">
            <span class="stat-title">{m.stats_peak_day()}</span>
            <div class="stat-icon-wrap lavender">
              <Sparkles size={20} />
            </div>
          </div>
          <div class="stat-value small">
            {formatCurrency(chartData.peakRevenue)}
          </div>
          <div class="stat-footer">
            <span>{m.stats_peak_date()}: <strong>{chartData.peakDate}</strong></span>
          </div>
        </div>
      </div>

      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <!-- 1. GRAPH 1: TURNOVER / REVENUE OVER TIME                               -->
      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <div class="chart-card card mb-4">
        <div class="chart-card-header">
          <div class="chart-title-wrap">
            <div class="chart-icon-box rose">
              <TrendingUp size={18} />
            </div>
            <div>
              <h3>{m.stats_chart_rev_title()}</h3>
              <p>{m.stats_chart_rev_desc()}</p>
            </div>
          </div>

          <div class="chart-legend">
            <span class="legend-indicator rose"></span>
            <span class="legend-text">{m.stats_legend_rev()}</span>
          </div>
        </div>

        <div class="svg-chart-container">
          <svg viewBox="0 0 {svgWidth} {svgHeight}" class="chart-svg">
            <defs>
              <linearGradient id="roseGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stop-color="#DF9E8E" stop-opacity="0.35" />
                <stop offset="100%" stop-color="#DF9E8E" stop-opacity="0.0" />
              </linearGradient>
            </defs>

            <!-- Y-Axis Grid lines -->
            {#each [0, 0.25, 0.5, 0.75, 1] as fraction}
              {@const yPos = padding.top + chartHeight * (1 - fraction)}
              <line 
                x1={padding.left} 
                y1={yPos} 
                x2={svgWidth - padding.right} 
                y2={yPos} 
                stroke="rgba(255, 255, 255, 0.05)" 
                stroke-dasharray="4 4"
              />
              <text 
                x={padding.left - 10} 
                y={yPos + 4} 
                class="axis-text" 
                text-anchor="end"
              >
                {Math.round(maxRevenue * fraction)} ₴
              </text>
            {/each}

            <!-- Area Path -->
            {#if revenueAreaPath}
              <path d={revenueAreaPath} fill="url(#roseGradient)" />
            {/if}

            <!-- Line Path -->
            {#if revenueLinePath}
              <path 
                d={revenueLinePath} 
                fill="none" 
                stroke="var(--pastel-rose)" 
                stroke-width="3" 
                stroke-linecap="round"
                class="chart-line-anim"
              />
            {/if}

            <!-- Points and X-Axis Labels -->
            {#each revenuePoints as pt, i}
              <!-- X-Axis Labels (show subset if many points) -->
              {#if chartData.points.length <= 15 || i % Math.ceil(chartData.points.length / 10) === 0 || i === chartData.points.length - 1}
                <text 
                  x={pt.x} 
                  y={svgHeight - 12} 
                  class="axis-text" 
                  text-anchor="middle"
                >
                  {pt.label}
                </text>
              {/if}

              <!-- Point Circle -->
              <!-- svelte-ignore a11y-no-static-element-interactions -->
              <circle 
                cx={pt.x} 
                cy={pt.y} 
                r={hoveredPoint?.fullDate === pt.fullDate && hoveredPoint?.type === 'revenue' ? 6 : 4} 
                fill="var(--bg-surface)" 
                stroke="var(--pastel-rose)" 
                stroke-width="2.5"
                class="chart-point"
                on:mouseenter={(e) => handlePointHover(pt, 'revenue', e)}
                on:mouseleave={handlePointLeave}
              />
            {/each}

            <!-- Interactive Tooltip in SVG -->
            {#if hoveredPoint && hoveredPoint.type === 'revenue'}
              <g transform="translate({hoveredPoint.x}, {hoveredPoint.y - 12})">
                <rect 
                  x="-65" 
                  y="-42" 
                  width="130" 
                  height="38" 
                  rx="6" 
                  fill="#1E293B" 
                  stroke="rgba(223, 158, 142, 0.4)" 
                  filter="drop-shadow(0 4px 12px rgba(0,0,0,0.5))"
                />
                <text x="0" y="-24" text-anchor="middle" fill="#94A3B8" font-size="10" font-weight="600">
                  {hoveredPoint.label}
                </text>
                <text x="0" y="-10" text-anchor="middle" fill="#DF9E8E" font-size="12" font-weight="700">
                  {formatCurrency(hoveredPoint.revenue)}
                </text>
              </g>
            {/if}
          </svg>
        </div>
      </div>

      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <!-- 2. GRAPH 2: CLIENT VISITS OVER TIME                                   -->
      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <div class="chart-card card mb-4">
        <div class="chart-card-header">
          <div class="chart-title-wrap">
            <div class="chart-icon-box sage">
              <Users size={18} />
            </div>
            <div>
              <h3>{m.stats_chart_vis_title()}</h3>
              <p>{m.stats_chart_vis_desc()}</p>
            </div>
          </div>

          <div class="chart-legend">
            <span class="legend-indicator sage"></span>
            <span class="legend-text">{m.stats_legend_vis()}</span>
          </div>
        </div>

        <div class="svg-chart-container">
          <svg viewBox="0 0 {svgWidth} {svgHeight}" class="chart-svg">
            <defs>
              <linearGradient id="sageGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stop-color="#A8C69B" stop-opacity="0.35" />
                <stop offset="100%" stop-color="#A8C69B" stop-opacity="0.0" />
              </linearGradient>
            </defs>

            <!-- Y-Axis Grid lines -->
            {#each [0, 0.25, 0.5, 0.75, 1] as fraction}
              {@const yPos = padding.top + chartHeight * (1 - fraction)}
              <line 
                x1={padding.left} 
                y1={yPos} 
                x2={svgWidth - padding.right} 
                y2={yPos} 
                stroke="rgba(255, 255, 255, 0.05)" 
                stroke-dasharray="4 4"
              />
              <text 
                x={padding.left - 10} 
                y={yPos + 4} 
                class="axis-text" 
                text-anchor="end"
              >
                {Math.round(maxVisits * fraction)}
              </text>
            {/each}

            <!-- Area Path -->
            {#if visitsAreaPath}
              <path d={visitsAreaPath} fill="url(#sageGradient)" />
            {/if}

            <!-- Line Path -->
            {#if visitsLinePath}
              <path 
                d={visitsLinePath} 
                fill="none" 
                stroke="var(--pastel-sage)" 
                stroke-width="3" 
                stroke-linecap="round"
                class="chart-line-anim"
              />
            {/if}

            <!-- Points and X-Axis Labels -->
            {#each visitsPoints as pt, i}
              {#if chartData.points.length <= 15 || i % Math.ceil(chartData.points.length / 10) === 0 || i === chartData.points.length - 1}
                <text 
                  x={pt.x} 
                  y={svgHeight - 12} 
                  class="axis-text" 
                  text-anchor="middle"
                >
                  {pt.label}
                </text>
              {/if}

              <!-- svelte-ignore a11y-no-static-element-interactions -->
              <circle 
                cx={pt.x} 
                cy={pt.y} 
                r={hoveredPoint?.fullDate === pt.fullDate && hoveredPoint?.type === 'visits' ? 6 : 4} 
                fill="var(--bg-surface)" 
                stroke="var(--pastel-sage)" 
                stroke-width="2.5"
                class="chart-point"
                on:mouseenter={(e) => handlePointHover(pt, 'visits', e)}
                on:mouseleave={handlePointLeave}
              />
            {/each}

            <!-- Interactive Tooltip in SVG -->
            {#if hoveredPoint && hoveredPoint.type === 'visits'}
              <g transform="translate({hoveredPoint.x}, {hoveredPoint.y - 12})">
                <rect 
                  x="-60" 
                  y="-42" 
                  width="120" 
                  height="38" 
                  rx="6" 
                  fill="#1E293B" 
                  stroke="rgba(168, 198, 155, 0.4)" 
                  filter="drop-shadow(0 4px 12px rgba(0,0,0,0.5))"
                />
                <text x="0" y="-24" text-anchor="middle" fill="#94A3B8" font-size="10" font-weight="600">
                  {hoveredPoint.label}
                </text>
                <text x="0" y="-10" text-anchor="middle" fill="#A8C69B" font-size="12" font-weight="700">
                  {hoveredPoint.visits} {m.stats_visits_unit()}
                </text>
              </g>
            {/if}
          </svg>
        </div>
      </div>

      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <!-- 3. TRANSACTIONS LOG                                                    -->
      <!-- ══════════════════════════════════════════════════════════════════════ -->
      <div class="card table-card">
        <div class="card-head">
          <div class="head-title-wrap">
            <Wallet size={20} class="title-icon" />
            <div>
              <h3>{m.stats_tx_history_title()}</h3>
              <p>{m.stats_tx_history_desc()}</p>
            </div>
          </div>
        </div>

        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>{m.stats_col_datetime()}</th>
                <th>{m.stats_col_purpose()}</th>
                <th>{m.stats_col_amount()}</th>
                <th>{m.stats_col_txhash()}</th>
              </tr>
            </thead>
            <tbody>
              {#each transactions as tx}
                <tr>
                  <td class="cell-date">{new Date(tx.time).toLocaleString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}</td>
                  <td>
                    {#if tx.txhHash}
                      <span class="tx-badge sub">
                        <Wallet size={13} />
                        <span>{m.stats_tx_sub()}</span>
                      </span>
                    {:else}
                      <span class="tx-badge rev">
                        <Coins size={13} />
                        <span>{m.stats_tx_rev()}</span>
                      </span>
                    {/if}
                  </td>
                  <td class="cell-amount">
                    {#if tx.txhHash}
                      <span class="ton-amount">{tx.amount} TON</span>
                    {:else}
                      <span class="uah-amount">{formatCurrency(tx.amount)}</span>
                    {/if}
                  </td>
                  <td>
                    {#if tx.txhHash}
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <div class="hash-tag" on:click={() => copyTx(tx.txhHash)} title={m.stats_copy_hash()}>
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
                  <td colspan="4" class="empty-cell">{m.stats_tx_empty()}</td>
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
    margin-bottom: 2rem;
    flex-wrap: wrap;
    gap: 1.25rem;
  }

  .header-left h1 {
    font-size: 2.1rem;
    font-weight: 700;
    margin-bottom: 0.35rem;
    letter-spacing: -0.02em;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 0.95rem;
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    flex-wrap: wrap;
  }

  /* Toasts */
  .toast {
    position: fixed;
    top: 1.5rem;
    right: 1.5rem;
    z-index: 10000;
    padding: 0.85rem 1.25rem;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 0.92rem;
    font-weight: 600;
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.4);
    animation: fadeIn 0.3s ease;
  }

  .toast-success {
    background: rgba(30, 41, 35, 0.95);
    border: 1px solid rgba(168, 198, 155, 0.4);
    color: var(--pastel-sage);
  }

  .toast-danger {
    background: rgba(45, 25, 25, 0.95);
    border: 1px solid rgba(242, 139, 130, 0.4);
    color: var(--pastel-coral);
  }

  /* Period Tabs */
  .period-tabs {
    display: flex;
    gap: 0.35rem;
    background: var(--bg-surface);
    padding: 0.35rem;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
    backdrop-filter: blur(16px);
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
    max-width: 100%;
    scrollbar-width: none;
  }

  .period-tabs::-webkit-scrollbar {
    display: none;
  }

  .period-tab {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.45rem 0.95rem;
    border-radius: var(--radius-pill);
    background: transparent;
    border: none;
    color: var(--text-secondary);
    font-size: 0.85rem;
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

  /* Custom Range Card */
  .custom-range-card {
    padding: 1.25rem 1.5rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
  }

  .custom-range-inner {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1.5rem;
    flex-wrap: wrap;
  }

  .custom-range-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 600;
    font-size: 0.92rem;
    color: var(--text-primary);
  }

  .custom-range-inputs {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    flex-wrap: wrap;
  }

  .date-input-group {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
    font-weight: 600;
  }

  .date-field {
    padding: 0.45rem 0.85rem;
    font-size: 0.88rem;
    border-radius: var(--radius-md);
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
    width: 34px;
    height: 34px;
    animation: spinSmooth 0.85s linear infinite;
  }

  /* Stats Grid */
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.75rem;
  }
  
  .stat-card {
    background: var(--bg-surface);
    backdrop-filter: blur(18px);
    padding: 1.5rem;
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    display: flex;
    flex-direction: column;
    transition: all 0.25s var(--ease-spring);
  }

  .stat-card:hover {
    transform: translateY(-2px);
    border-color: var(--border-glass);
  }

  .stat-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
  }
  
  .stat-title {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .stat-icon-wrap {
    width: 40px;
    height: 40px;
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
    border: 1px solid rgba(168, 198, 155, 0.25);
  }

  .stat-icon-wrap.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.25);
  }

  .stat-value {
    font-size: 1.95rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
  }

  .stat-value.small {
    font-size: 1.65rem;
  }

  .stat-footer {
    margin-top: auto;
    font-size: 0.8rem;
    color: var(--text-muted);
  }

  .stat-footer strong {
    color: var(--text-primary);
  }

  /* Chart Cards */
  .chart-card {
    padding: 1.75rem;
    margin-bottom: 1.75rem;
  }

  .chart-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .chart-title-wrap {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .chart-icon-box {
    width: 36px;
    height: 36px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .chart-icon-box.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
  }

  .chart-icon-box.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .chart-title-wrap h3 {
    font-size: 1.15rem;
    font-weight: 700;
    margin: 0 0 0.2rem;
  }

  .chart-title-wrap p {
    font-size: 0.82rem;
    color: var(--text-secondary);
    margin: 0;
  }

  .chart-legend {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.82rem;
    color: var(--text-secondary);
    font-weight: 600;
  }

  .legend-indicator {
    width: 10px;
    height: 10px;
    border-radius: 50%;
  }

  .legend-indicator.rose { background: var(--pastel-rose); }
  .legend-indicator.sage { background: var(--pastel-sage); }

  .svg-chart-container {
    width: 100%;
    overflow-x: auto;
  }

  .chart-svg {
    width: 100%;
    height: auto;
    min-width: 600px;
    display: block;
    overflow: visible;
  }

  .axis-text {
    font-size: 10px;
    fill: var(--text-muted);
    font-family: inherit;
  }

  .chart-point {
    cursor: pointer;
    transition: r 0.15s ease;
  }

  .chart-point:hover {
    r: 6.5px;
  }

  /* Table Card */
  .table-card {
    padding: 1.75rem;
  }

  .card-head {
    margin-bottom: 1.25rem;
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
    font-size: 1.2rem;
    font-weight: 700;
    margin: 0 0 0.2rem;
  }

  .head-title-wrap p {
    color: var(--text-secondary);
    font-size: 0.85rem;
    margin: 0;
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
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    border-bottom: 1px solid var(--border-subtle);
  }

  .table td {
    padding: 1rem;
    text-align: left;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    font-size: 0.9rem;
  }

  .table tbody tr:hover {
    background-color: rgba(255, 255, 255, 0.02);
  }

  .cell-date {
    font-weight: 600;
    color: var(--text-primary);
  }

  .tx-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.25rem 0.65rem;
    border-radius: var(--radius-pill);
    font-size: 0.78rem;
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
    border: 1px solid rgba(168, 198, 155, 0.25);
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
    padding: 0.25rem 0.55rem;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: all 0.2s;
  }

  .hash-tag:hover {
    border-color: var(--border-glass);
    background: rgba(255, 255, 255, 0.06);
  }

  .hash-tag code {
    font-family: monospace;
    font-size: 0.8rem;
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

  :global(.text-rose) { color: var(--pastel-rose); }
  .mb-4 { margin-bottom: 1.75rem; }

  @media (max-width: 768px) {
    .page-header {
      flex-direction: column;
      align-items: stretch;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .header-left h1 {
      font-size: 1.65rem;
    }

    .header-actions {
      flex-direction: column;
      align-items: stretch;
      gap: 0.75rem;
      width: 100%;
    }

    .header-actions .btn {
      width: 100%;
    }

    .period-tabs {
      width: 100%;
      box-sizing: border-box;
    }

    .custom-range-inner {
      flex-direction: column;
      align-items: stretch;
      gap: 1rem;
    }

    .custom-range-inputs {
      flex-direction: column;
      align-items: stretch;
      gap: 0.75rem;
      width: 100%;
    }

    .date-input-group {
      justify-content: space-between;
      width: 100%;
    }

    .date-field {
      flex: 1;
    }

    .custom-range-inputs .btn {
      width: 100%;
    }

    .stats-grid {
      grid-template-columns: 1fr;
      gap: 1rem;
    }

    .stat-card {
      padding: 1.25rem 1.15rem;
    }

    .stat-value {
      font-size: 1.7rem;
    }

    .chart-card {
      padding: 1.25rem 1rem;
    }

    .chart-card-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 0.75rem;
    }

    .table-card {
      padding: 1.25rem 1rem;
    }

    .toast {
      left: 1rem;
      right: 1rem;
      max-width: 420px;
      margin: 0 auto;
    }
  }

  @media (max-width: 480px) {
    .header-left h1 {
      font-size: 1.4rem;
    }

    .table th, .table td {
      padding: 0.65rem 0.75rem;
      font-size: 0.82rem;
    }
  }
</style>

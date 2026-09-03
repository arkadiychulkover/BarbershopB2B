<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest, BASE_URL } from '../lib/api';
  import { authStore } from '../lib/store';
  import { m } from '../lib/paraglide/messages.js';
  import { 
    Users, 
    Search, 
    Download, 
    Calendar, 
    DollarSign, 
    RefreshCw, 
    FileSpreadsheet, 
    Phone, 
    Send,
    AlertCircle,
    CheckCircle2,
    UserCheck,
    TrendingUp,
    Sparkles,
    Clock
  } from 'lucide-svelte';

  let clients = [];
  let isLoading = true;
  let isExporting = false;
  let searchQuery = '';
  let errorMsg = '';
  let successMsg = '';

  let searchTimeout = null;

  onMount(async () => {
    await fetchClients();
  });

  async function fetchClients() {
    isLoading = true;
    errorMsg = '';
    try {
      let url = '/api/Statistic/owner-clients';
      if (searchQuery.trim()) {
        url += `?search=${encodeURIComponent(searchQuery.trim())}`;
      }
      const data = await apiRequest(url);
      clients = Array.isArray(data) ? data : [];
    } catch (e) {
      errorMsg = m.clients_load_error() + (e.message || '');
      clients = [];
    } finally {
      isLoading = false;
    }
  }

  function handleSearchInput() {
    if (searchTimeout) clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
      fetchClients();
    }, 300);
  }

  async function exportToExcel() {
    isExporting = true;
    errorMsg = '';
    successMsg = '';
    try {
      const token = $authStore.token;
      const res = await fetch(`${BASE_URL}/api/Statistic/export-clients`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!res.ok) {
        throw new Error(`${m.clients_export_error()} ${res.statusText}`);
      }

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `clients_${new Date().toISOString().slice(0,10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      successMsg = m.clients_export_success();
      setTimeout(() => { successMsg = ''; }, 4000);
    } catch (e) {
      errorMsg = e.message || m.clients_export_error();
    } finally {
      isExporting = false;
    }
  }

  function formatCurrency(num) {
    return (num || 0).toLocaleString('ru-RU') + ' ₴';
  }

  function formatDate(d) {
    if (!d) return '—';
    try {
      const date = new Date(d);
      return date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
    } catch {
      return '—';
    }
  }

  $: totalClientsCount = clients.length;
  $: regularClientsCount = clients.filter(c => (c.totalVisits || 0) >= 2).length;
  $: totalClientsTurnover = clients.reduce((sum, c) => sum + (c.totalSpent || 0), 0);
</script>

<DashboardLayout>
  <!-- Toast alerts -->
  {#if successMsg}
    <div class="toast toast-success">
      <CheckCircle2 size={18} />
      <span>{successMsg}</span>
    </div>
  {/if}

  {#if errorMsg}
    <div class="toast toast-danger">
      <AlertCircle size={18} />
      <span>{errorMsg}</span>
    </div>
  {/if}

  <div class="page-header">
    <div>
      <h1>{m.clients_heading()}</h1>
      <p class="subtitle">{m.clients_sub()}</p>
    </div>

    <div class="header-actions">
      <button class="btn btn-primary" on:click={exportToExcel} disabled={isExporting}>
        <FileSpreadsheet size={17} />
        <span>{isExporting ? m.clients_exporting() : m.clients_export_btn()}</span>
      </button>
      <button class="btn btn-secondary" on:click={fetchClients} title="Refresh">
        <RefreshCw size={16} />
      </button>
    </div>
  </div>

  <!-- KPI Cards -->
  <div class="metrics-grid">
    <div class="metric-card">
      <div class="metric-icon-wrap rose">
        <Users size={20} />
      </div>
      <div class="metric-content">
        <span class="metric-label">{m.clients_total_metric()}</span>
        <div class="metric-value">{totalClientsCount}</div>
        <div class="metric-sub">{m.clients_total_metric_sub()}</div>
      </div>
    </div>

    <div class="metric-card">
      <div class="metric-icon-wrap sage">
        <UserCheck size={20} />
      </div>
      <div class="metric-content">
        <span class="metric-label">{m.clients_regular_metric()}</span>
        <div class="metric-value">{regularClientsCount}</div>
        <div class="metric-sub">{m.clients_regular_metric_sub()}</div>
      </div>
    </div>

    <div class="metric-card">
      <div class="metric-icon-wrap lavender">
        <TrendingUp size={20} />
      </div>
      <div class="metric-content">
        <span class="metric-label">{m.clients_turnover_metric()}</span>
        <div class="metric-value">{formatCurrency(totalClientsTurnover)}</div>
        <div class="metric-sub">{m.clients_turnover_metric_sub()}</div>
      </div>
    </div>
  </div>

  <!-- Search & Filter bar -->
  <div class="filter-bar">
    <div class="search-input-wrap">
      <Search size={17} class="search-icon" />
      <input 
        type="text" 
        placeholder={m.clients_search_placeholder()} 
        bind:value={searchQuery}
        on:input={handleSearchInput}
      />
    </div>
  </div>

  <!-- Clients Data Card -->
  <div class="card p-0">
    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>{m.clients_th_index()}</th>
            <th>{m.clients_th_client()}</th>
            <th>{m.clients_th_phone()}</th>
            <th>{m.clients_th_tg_id()}</th>
            <th>{m.clients_th_visits()}</th>
            <th>{m.clients_th_last_visit()}</th>
            <th class="text-right">{m.clients_th_total_spent()}</th>
          </tr>
        </thead>
        <tbody>
          {#each clients as client, index}
            <tr>
              <td class="text-muted">{index + 1}</td>
              <td>
                <div class="client-name-wrap">
                  <div class="client-avatar">
                    {(client.name || 'C')[0].toUpperCase()}
                  </div>
                  <div>
                    <div class="table-cell-bold">{client.name || 'Guest'}</div>
                    {#if client.notes}
                      <div class="client-notes">{client.notes}</div>
                    {/if}
                  </div>
                </div>
              </td>
              <td>
                {#if client.phone && client.phone !== '\u041d\u0435 \u0443\u043a\u0430\u0437\u0430\u043d' && client.phone !== 'Not specified' && client.phone !== m.clients_not_specified()}
                  <div class="phone-cell">
                    <Phone size={13} class="text-muted" />
                    <span>{client.phone}</span>
                  </div>
                {:else}
                  <span class="text-muted text-xs">—</span>
                {/if}
              </td>
              <td>
                <code class="tg-id-badge">{client.telegramId}</code>
              </td>
              <td>
                <span class="visits-badge" class:highlight={(client.totalVisits || 0) >= 2}>
                  {client.totalVisits || 0}
                </span>
              </td>
              <td>
                <div class="last-visit-cell">
                  <Clock size={13} class="text-muted" />
                  <span>{formatDate(client.lastVisit)}</span>
                </div>
              </td>
              <td class="text-right">
                <span class="table-cell-bold text-sage">{formatCurrency(client.totalSpent || 0)}</span>
              </td>
            </tr>
          {:else}
            <tr>
              <td colspan="7" class="text-center py-5 text-muted">
                {#if isLoading}
                  <div class="loading-state">
                    <div class="spinner-sm"></div>
                    <span>{m.common_loading()}</span>
                  </div>
                {:else}
                  No clients found.
                {/if}
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
</DashboardLayout>

<style>
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1.5rem;
    margin-bottom: 1.75rem;
    flex-wrap: wrap;
  }

  .page-header h1 {
    font-size: 1.85rem;
    font-weight: 700;
    margin: 0 0 0.4rem;
    letter-spacing: -0.02em;
  }

  .subtitle {
    color: var(--text-secondary);
    font-size: 0.95rem;
    margin: 0;
  }

  .header-actions {
    display: flex;
    gap: 0.75rem;
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

  /* Metrics Grid */
  .metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.75rem;
  }

  .metric-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 1.25rem 1.4rem;
    display: flex;
    align-items: center;
    gap: 1rem;
    transition: transform 0.2s ease;
  }

  .metric-card:hover {
    transform: translateY(-2px);
  }

  .metric-icon-wrap {
    width: 44px;
    height: 44px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .metric-icon-wrap.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
  }

  .metric-icon-wrap.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .metric-icon-wrap.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.3);
  }

  .metric-content {
    display: flex;
    flex-direction: column;
  }

  .metric-label {
    font-size: 0.78rem;
    color: var(--text-secondary);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .metric-value {
    font-size: 1.6rem;
    font-weight: 800;
    color: var(--text-primary);
    line-height: 1.2;
    margin: 0.2rem 0;
  }

  .metric-sub {
    font-size: 0.76rem;
    color: var(--text-muted);
  }

  /* Filter */
  .filter-bar {
    display: flex;
    gap: 1rem;
    margin-bottom: 1.25rem;
  }

  .search-input-wrap {
    position: relative;
    flex: 1;
    display: flex;
    align-items: center;
  }

  :global(.search-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .search-input-wrap input {
    width: 100%;
    padding: 0.75rem 1rem 0.75rem 2.75rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-size: 0.92rem;
    transition: border-color 0.2s ease;
  }

  .search-input-wrap input:focus {
    border-color: var(--pastel-rose);
    outline: none;
  }

  /* Table */
  .p-0 { padding: 0 !important; }

  .table-container {
    overflow-x: auto;
  }

  .data-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9rem;
  }

  .data-table th {
    text-align: left;
    padding: 1rem 1.25rem;
    color: var(--text-secondary);
    font-weight: 600;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 1px solid var(--border-subtle);
  }

  .data-table td {
    padding: 1rem 1.25rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    vertical-align: middle;
  }

  .data-table tr:hover td {
    background-color: rgba(255, 255, 255, 0.02);
  }

  .client-name-wrap {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .client-avatar {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: var(--pastel-rose-dim);
    border: 1px solid rgba(223, 158, 142, 0.3);
    color: var(--pastel-rose);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.85rem;
  }

  .table-cell-bold {
    font-weight: 600;
    color: var(--text-primary);
  }

  .client-notes {
    font-size: 0.78rem;
    color: var(--text-muted);
  }

  .phone-cell {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    font-size: 0.88rem;
  }

  .last-visit-cell {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
  }

  .tg-id-badge {
    padding: 3px 8px;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
    font-size: 0.82rem;
    color: var(--pastel-lavender);
  }

  .visits-badge {
    display: inline-flex;
    padding: 3px 10px;
    border-radius: var(--radius-pill);
    background: rgba(255, 255, 255, 0.04);
    color: var(--text-secondary);
    border: 1px solid var(--border-subtle);
    font-size: 0.8rem;
    font-weight: 600;
  }

  .visits-badge.highlight {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border-color: rgba(168, 198, 155, 0.3);
  }

  .loading-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
  }

  .spinner-sm {
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    width: 28px;
    height: 28px;
    animation: spinSmooth 0.85s linear infinite;
  }

  .text-sage { color: var(--pastel-sage); }
  .text-muted { color: var(--text-muted); }
  .text-xs { font-size: 0.78rem; }
  .text-center { text-align: center; }
  .text-right { text-align: right; }
  .py-5 { padding-top: 2.5rem; padding-bottom: 2.5rem; }
</style>

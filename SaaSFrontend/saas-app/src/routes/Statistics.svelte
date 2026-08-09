<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  import { Wallet, Coins } from 'lucide-svelte';
  
  let isLoading = true;
  let statsData = [];
  let transactions = [];
  let summary = { totalRevenue: 0, totalClients: 0 };
  
  onMount(async () => {
    try {
      const end = new Date();
      const start = new Date();
      start.setDate(end.getDate() - 7);
      
      const startStr = start.toISOString().split('T')[0];
      const endStr = end.toISOString().split('T')[0];
      
      const [data, txData] = await Promise.all([
        apiRequest(`/api/Statistic/owner-statistic-daily?startDate=${startStr}&endDate=${endStr}`),
        apiRequest('/api/Statistic').catch(() => [])
      ]);
      
      if (txData) {
        transactions = txData;
      }
      
      if (data) {
        statsData = Object.keys(data).map(key => {
          const item = data[key];
          summary.totalRevenue += item.ownerProfit;
          summary.totalClients += item.clientsCount;
          return {
            date: new Date(key).toLocaleDateString(),
            revenue: item.ownerProfit,
            clients: item.clientsCount
          };
        });
      }
    } catch (e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  });
</script>

<DashboardLayout>
  <div class="statistics">
    <header class="page-header">
      <h1>Статистика</h1>
      <p>Аналитика за последние 7 дней.</p>
    </header>

    {#if isLoading}
      <p>Загрузка...</p>
    {:else}
      <div class="stats-grid">
        <div class="stat-card">
          <h3>Суммарная выручка</h3>
          <div class="value">{new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(summary.totalRevenue)}</div>
        </div>
        <div class="stat-card">
          <h3>Всего клиентов</h3>
          <div class="value">{summary.totalClients}</div>
        </div>
      </div>
      
      <div class="card">
        <h3>По дням</h3>
        <table class="table">
          <thead>
            <tr>
              <th>Дата</th>
              <th>Выручка</th>
              <th>Клиентов</th>
            </tr>
          </thead>
          <tbody>
            {#each statsData as row}
              <tr>
                <td>{row.date}</td>
                <td>{new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(row.revenue)}</td>
                <td>{row.clients}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>

      <div class="card" style="margin-top: 2rem;">
        <h3>История транзакций</h3>
        <table class="table">
          <thead>
            <tr>
              <th>Дата</th>
              <th>Тип</th>
              <th>Сумма</th>
              <th>Хэш (TON)</th>
            </tr>
          </thead>
          <tbody>
            {#each transactions as tx}
              <tr>
                <td>{new Date(tx.time).toLocaleString('ru-RU')}</td>
                <td>
                  {#if tx.txhHash}
                    <span class="badge" style="background-color: rgba(245, 158, 11, 0.1); color: var(--warning);"><Wallet size={14} style="vertical-align: middle; margin-right: 4px;" /> Подписка</span>
                  {:else}
                    <span class="badge active"><Coins size={14} style="vertical-align: middle; margin-right: 4px;" /> Выручка</span>
                  {/if}
                </td>
                <td>
                  {#if tx.txhHash}
                    {tx.amount} TON
                  {:else}
                    {new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH', maximumFractionDigits: 0 }).format(tx.amount)}
                  {/if}
                </td>
                <td>
                  {#if tx.txhHash}
                    <code title={tx.txhHash}>{tx.txhHash.substring(0, 10)}...</code>
                  {:else}
                    -
                  {/if}
                </td>
              </tr>
            {/each}
            {#if transactions.length === 0}
              <tr>
                <td colspan="4" style="text-align: center; color: var(--text-secondary); padding: 2rem;">Нет транзакций</td>
              </tr>
            {/if}
          </tbody>
        </table>
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .page-header {
    margin-bottom: 2rem;
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
  
  .table {
    width: 100%;
    border-collapse: collapse;
    margin-top: 1rem;
  }
  
  .table th, .table td {
    padding: 1rem;
    text-align: left;
    border-bottom: 1px solid var(--border-color);
  }
  
  .table th {
    color: var(--text-secondary);
    font-weight: 500;
  }
</style>

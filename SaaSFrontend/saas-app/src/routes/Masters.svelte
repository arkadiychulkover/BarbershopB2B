<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  let masters = [];
  let isLoading = true;
  let errorMsg = '';
  
  onMount(async () => {
    try {
      masters = await apiRequest('/api/Barber/all');
    } catch (e) {
      errorMsg = 'Не удалось загрузить список мастеров';
    } finally {
      isLoading = false;
    }
  });

  async function deleteMaster(id) {
    if (!confirm('Вы уверены?')) return;
    try {
      await apiRequest('/api/Barber/delete', {
        method: 'DELETE',
        body: JSON.stringify({ barberId: id })
      });
      masters = masters.filter(m => m.id !== id);
    } catch (e) {
      alert('Ошибка удаления: ' + e.message);
    }
  }
</script>

<DashboardLayout>
  <div class="masters">
    <header class="page-header">
      <div class="header-content">
        <div>
          <h1>Команда мастеров</h1>
          <p>Управление списком мастеров вашего барбершопа.</p>
        </div>
        <button class="btn btn-primary">Добавить мастера</button>
      </div>
    </header>

    {#if isLoading}
      <p>Загрузка...</p>
    {:else if errorMsg}
      <div class="alert alert-danger">{errorMsg}</div>
    {:else if masters.length === 0}
      <div class="card empty-state">
        <p>У вас пока нет ни одного мастера.</p>
        <button class="btn btn-primary mt-2">Добавить первого мастера</button>
      </div>
    {:else}
      <div class="grid">
        {#each masters as master}
          <div class="card master-card">
            <div class="master-info">
              <h3>{master.name}</h3>
              <p class="status" class:active={master.isActive}>{master.isActive ? 'Активен' : 'Неактивен'}</p>
            </div>
            <div class="actions">
              <button class="btn btn-danger btn-sm" on:click={() => deleteMaster(master.id)}>Удалить</button>
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .page-header {
    margin-bottom: 2rem;
  }
  
  .header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  
  .empty-state {
    text-align: center;
    padding: 3rem;
  }
  
  .mt-2 { margin-top: 1rem; }
  
  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1.5rem;
  }
  
  .master-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  
  .master-info h3 {
    margin-bottom: 0.25rem;
  }
  
  .status {
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .status.active {
    color: var(--success);
  }
  
  .btn-sm {
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
  }
</style>

<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  let masters = [];
  let isLoading = true;
  let errorMsg = '';
  
  let showAddForm = false;
  let newMaster = { name: '', description: '', telegramId: '' };
  let isSaving = false;
  
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

  async function addMaster() {
    isSaving = true;
    try {
      const res = await apiRequest('/api/Barber/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newMaster)
      });
      masters = [...masters, { id: res.barberId, name: newMaster.name, description: newMaster.description, telegramId: newMaster.telegramId, isActive: true }];
      showAddForm = false;
      newMaster = { name: '', description: '', telegramId: '' };
    } catch (e) {
      alert('Ошибка добавления: ' + (e.message || 'Неизвестная ошибка'));
    } finally {
      isSaving = false;
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
        <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
          {showAddForm ? 'Отмена' : 'Добавить мастера'}
        </button>
      </div>
    </header>

    {#if showAddForm}
      <div class="card mb-4">
        <h3>Добавление нового мастера</h3>
        <div class="form-group" style="margin-top: 1rem;">
          <label>Имя мастера</label>
          <input type="text" class="input" bind:value={newMaster.name} placeholder="Например, Алексей" />
        </div>
        <div class="form-group">
          <label>Описание / Должность</label>
          <input type="text" class="input" bind:value={newMaster.description} placeholder="Например, Старший барбер" />
        </div>
        <div class="form-group">
          <label>Telegram ID</label>
          <input type="text" class="input" bind:value={newMaster.telegramId} placeholder="Например, 123456789" />
        </div>
        <button class="btn btn-primary" on:click={addMaster} disabled={isSaving || !newMaster.name || !newMaster.telegramId}>
          {isSaving ? 'Сохранение...' : 'Сохранить мастера'}
        </button>
      </div>
    {/if}

    {#if isLoading}
      <p>Загрузка...</p>
    {:else if errorMsg}
      <div class="alert alert-danger">{errorMsg}</div>
    {:else if masters.length === 0}
      <div class="card empty-state">
        <p>У вас пока нет ни одного мастера.</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>Добавить первого мастера</button>
      </div>
    {:else}
      <div class="grid">
        {#each masters as master}
          <div class="card master-card">
            <div class="master-info">
              <h3>{master.name}</h3>
              <p class="status" class:active={master.isActive}>{master.isActive ? 'Активен' : 'Неактивен'}</p>
              {#if master.telegramId}
                <p class="status" style="margin-top: 4px;">TG ID: {master.telegramId}</p>
              {/if}
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
  .mb-4 { margin-bottom: 2rem; }
  
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

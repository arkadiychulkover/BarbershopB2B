<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  let services = [];
  let isLoading = true;
  let errorMsg = '';
  
  let showAddForm = false;
  let newServiceName = '';
  let isSaving = false;
  
  let editingServiceId = null;
  let editingServiceName = '';

  onMount(async () => {
    await fetchServices();
  });

  async function fetchServices() {
    isLoading = true;
    try {
      services = await apiRequest('/api/ServiceNames');
    } catch (e) {
      errorMsg = 'Не удалось загрузить список услуг';
    } finally {
      isLoading = false;
    }
  }

  async function addService() {
    if (!newServiceName.trim()) return;
    isSaving = true;
    try {
      const res = await apiRequest('/api/ServiceNames', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: newServiceName.trim() })
      });
      services = [...services, res];
      showAddForm = false;
      newServiceName = '';
    } catch (e) {
      alert('Ошибка добавления: ' + (e.message || 'Неизвестная ошибка'));
    } finally {
      isSaving = false;
    }
  }

  async function deleteService(id) {
    if (!confirm('Вы уверены, что хотите удалить эту услугу? Это также удалит её у всех мастеров!')) return;
    try {
      await apiRequest(`/api/ServiceNames/${id}`, {
        method: 'DELETE'
      });
      services = services.filter(s => s.id !== id);
    } catch (e) {
      alert('Ошибка удаления: ' + e.message);
    }
  }

  function startEdit(service) {
    editingServiceId = service.id;
    editingServiceName = service.name;
  }

  function cancelEdit() {
    editingServiceId = null;
    editingServiceName = '';
  }

  async function saveEdit() {
    if (!editingServiceName.trim()) return;
    try {
      const res = await apiRequest(`/api/ServiceNames/${editingServiceId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: editingServiceName.trim() })
      });
      const index = services.findIndex(s => s.id === editingServiceId);
      if (index !== -1) {
        services[index] = res;
      }
      cancelEdit();
    } catch (e) {
      alert('Ошибка обновления: ' + e.message);
    }
  }
</script>

<DashboardLayout>
  <div class="services">
    <header class="page-header">
      <div class="header-content">
        <div>
          <h1>Справочник услуг</h1>
          <p>Управление базовыми названиями услуг вашего барбершопа. Мастера смогут настроить цену и длительность для каждой из них.</p>
        </div>
        <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
          {showAddForm ? 'Отмена' : 'Добавить услугу'}
        </button>
      </div>
    </header>

    {#if showAddForm}
      <div class="card mb-4">
        <h3>Добавление новой услуги</h3>
        <div class="form-group" style="margin-top: 1rem;">
          <label>Название услуги</label>
          <input type="text" class="input" bind:value={newServiceName} placeholder="Например, Стрижка машинкой" />
        </div>
        <button class="btn btn-primary" on:click={addService} disabled={isSaving || !newServiceName.trim()}>
          {isSaving ? 'Сохранение...' : 'Сохранить услугу'}
        </button>
      </div>
    {/if}

    {#if isLoading}
      <p>Загрузка...</p>
    {:else if errorMsg}
      <div class="alert alert-danger">{errorMsg}</div>
    {:else if services.length === 0}
      <div class="card empty-state">
        <p>У вас пока нет ни одной добавленной услуги.</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>Создать первую услугу</button>
      </div>
    {:else}
      <div class="grid">
        {#each services as service}
          <div class="card service-card">
            {#if editingServiceId === service.id}
              <div class="edit-mode">
                <input type="text" class="input" bind:value={editingServiceName} />
                <div class="actions mt-2">
                  <button class="btn btn-primary" on:click={saveEdit}>Сохранить</button>
                  <button class="btn btn-secondary" on:click={cancelEdit}>Отмена</button>
                </div>
              </div>
            {:else}
              <div class="view-mode">
                <div class="service-info">
                  <h3>{service.name}</h3>
                </div>
                <div class="actions">
                  <button class="btn btn-secondary" on:click={() => startEdit(service)}>Изменить</button>
                  <button class="btn btn-danger" on:click={() => deleteService(service.id)}>Удалить</button>
                </div>
              </div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .services {
    animation: fadeIn 0.3s ease-out;
  }
  
  .page-header {
    margin-bottom: 2rem;
  }
  
  .header-content {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
  }
  
  .header-content h1 {
    font-size: 1.8rem;
    font-weight: 700;
    color: var(--text-color);
    margin: 0 0 0.5rem 0;
  }
  
  .header-content p {
    color: var(--text-muted);
    margin: 0;
  }
  
  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1.5rem;
  }
  
  .service-card {
    display: flex;
    flex-direction: column;
    justify-content: space-between;
  }

  .service-info h3 {
    margin: 0 0 1rem 0;
    font-size: 1.25rem;
  }
  
  .view-mode, .edit-mode {
    display: flex;
    flex-direction: column;
    height: 100%;
  }

  .actions {
    display: flex;
    gap: 0.5rem;
    margin-top: auto;
    padding-top: 1rem;
  }
  
  .btn-danger {
    background: rgba(239, 68, 68, 0.1);
    color: rgb(239, 68, 68);
    border: 1px solid rgba(239, 68, 68, 0.2);
  }
  
  .btn-danger:hover {
    background: rgba(239, 68, 68, 0.2);
  }
  
  .empty-state {
    text-align: center;
    padding: 4rem 2rem;
  }
  
  .empty-state p {
    color: var(--text-muted);
    margin-bottom: 1rem;
  }

  .mt-2 {
    margin-top: 1rem;
  }
</style>

<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { 
    Scissors, 
    Plus, 
    Trash2, 
    Edit2, 
    Check, 
    X, 
    AlertCircle,
    Sparkles
  } from 'lucide-svelte';
  
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
  <div class="services-page">
    <header class="page-header">
      <div class="header-left">
        <h1>Каталог услуг</h1>
        <p class="header-subtitle">Базовые наименования услуг. Каждый мастер может задавать для них свою цену и время выполнения.</p>
      </div>
      <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
        {#if showAddForm}
          <X size={18} />
          <span>Скрыть форму</span>
        {:else}
          <Plus size={18} />
          <span>Добавить услугу</span>
        {/if}
      </button>
    </header>

    {#if showAddForm}
      <div class="card add-card mb-4">
        <div class="card-head">
          <h3>Создание услуги</h3>
          <p>Введите общее название (например: Мужская стрижка, Стрижка бороды, Королевское бритье)</p>
        </div>

        <form on:submit|preventDefault={addService}>
          <div class="form-group">
            <label for="newService">Название услуги</label>
            <div class="input-icon-wrap">
              <Scissors size={16} class="input-icon" />
              <input 
                id="newService" 
                type="text" 
                class="input has-icon" 
                bind:value={newServiceName} 
                placeholder="Стрижка ножницами и машинкой" 
                required 
              />
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" disabled={isSaving || !newServiceName.trim()}>
              {isSaving ? 'Сохранение...' : 'Создать услугу'}
            </button>
            <button type="button" class="btn btn-secondary" on:click={() => showAddForm = false}>
              Отмена
            </button>
          </div>
        </form>
      </div>
    {/if}

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>Загрузка каталога услуг...</p>
      </div>
    {:else if errorMsg}
      <div class="alert alert-danger">
        <AlertCircle size={18} />
        <span>{errorMsg}</span>
      </div>
    {:else if services.length === 0}
      <div class="card empty-state">
        <div class="empty-icon-circle">
          <Scissors size={32} />
        </div>
        <h3>Услуги пока не созданы</h3>
        <p>Добавьте первые базовые услуги барбершопа, чтобы мастера могли прикрепить к ним прайс-лист.</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>
          <Plus size={17} />
          <span>Создать первую услугу</span>
        </button>
      </div>
    {:else}
      <div class="grid">
        {#each services as service}
          <div class="card service-card">
            {#if editingServiceId === service.id}
              <div class="edit-mode">
                <label class="edit-label">Редактирование названия</label>
                <input type="text" class="input" bind:value={editingServiceName} />
                <div class="actions mt-3">
                  <button class="btn btn-primary btn-sm" on:click={saveEdit}>
                    <Check size={15} />
                    <span>Сохранить</span>
                  </button>
                  <button class="btn btn-secondary btn-sm" on:click={cancelEdit}>
                    <span>Отмена</span>
                  </button>
                </div>
              </div>
            {:else}
              <div class="view-mode">
                <div class="service-top">
                  <div class="service-icon-wrap">
                    <Scissors size={20} />
                  </div>
                  <div class="service-info">
                    <h3>{service.name}</h3>
                    <span class="service-status">В каталоге заведения</span>
                  </div>
                </div>

                <div class="actions">
                  <button class="btn btn-secondary btn-sm" on:click={() => startEdit(service)}>
                    <Edit2 size={14} />
                    <span>Изменить</span>
                  </button>
                  <button class="btn btn-danger btn-sm" on:click={() => deleteService(service.id)}>
                    <Trash2 size={14} />
                    <span>Удалить</span>
                  </button>
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
  .services-page {
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

  .header-left h1 {
    font-size: 2.2rem;
    margin-bottom: 0.35rem;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 1rem;
    max-width: 680px;
  }

  .add-card {
    padding: 2rem;
    margin-bottom: 2.25rem;
    animation: fadeIn 0.25s ease;
  }

  .card-head {
    margin-bottom: 1.5rem;
  }

  .card-head h3 {
    font-size: 1.3rem;
    margin-bottom: 0.25rem;
  }

  .card-head p {
    color: var(--text-secondary);
    font-size: 0.9rem;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  label {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .input-icon-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.input-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .input.has-icon {
    padding-left: 2.75rem;
  }

  .form-actions {
    display: flex;
    gap: 1rem;
    margin-top: 1.5rem;
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 0;
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

  .empty-state {
    text-align: center;
    padding: 4rem 2rem;
    max-width: 540px;
    margin: 0 auto;
  }

  .empty-icon-circle {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 0 auto 1.5rem;
    border: 1px solid rgba(223, 158, 142, 0.25);
    box-shadow: 0 0 20px var(--pastel-rose-glow);
  }

  .empty-state h3 {
    font-size: 1.4rem;
    margin-bottom: 0.5rem;
  }

  .empty-state p {
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.55;
    margin-bottom: 1.5rem;
  }

  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(310px, 1fr));
    gap: 1.5rem;
  }

  .service-card {
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    padding: 1.75rem;
    min-height: 160px;
  }

  .view-mode, .edit-mode {
    display: flex;
    flex-direction: column;
    height: 100%;
    justify-content: space-between;
  }

  .service-top {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1.25rem;
  }

  .service-icon-wrap {
    width: 44px;
    height: 44px;
    border-radius: var(--radius-md);
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .service-info h3 {
    font-size: 1.15rem;
    margin-bottom: 0.2rem;
  }

  .service-status {
    font-size: 0.8rem;
    color: var(--text-muted);
  }

  .edit-label {
    font-size: 0.82rem;
    color: var(--pastel-rose);
    margin-bottom: 0.4rem;
  }

  .actions {
    display: flex;
    gap: 0.6rem;
    justify-content: flex-end;
    margin-top: auto;
  }

  .btn-sm {
    padding: 0.45rem 0.9rem;
    font-size: 0.85rem;
  }

  .mt-3 {
    margin-top: 1rem;
  }
</style>

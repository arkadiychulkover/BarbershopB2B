<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { 
    UserPlus, 
    User, 
    Briefcase, 
    Send, 
    Trash2, 
    ShieldCheck, 
    AlertCircle,
    X,
    Users
  } from 'lucide-svelte';
  
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
    if (!confirm('Вы уверены, что хотите удалить этого мастера?')) return;
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
  <div class="masters-page">
    <header class="page-header">
      <div class="header-left">
        <h1>Команда мастеров</h1>
        <p class="header-subtitle">Управляйте барберами, их доступом и индивидуальными графиками</p>
      </div>
      <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
        {#if showAddForm}
          <X size={18} />
          <span>Скрыть форму</span>
        {:else}
          <UserPlus size={18} />
          <span>Добавить мастера</span>
        {/if}
      </button>
    </header>

    {#if showAddForm}
      <div class="card add-card mb-4">
        <div class="card-head">
          <h3>Новый мастер</h3>
          <p>Введите контактные данные и Telegram ID мастера для уведомлений</p>
        </div>

        <form on:submit|preventDefault={addMaster}>
          <div class="form-grid">
            <div class="form-group">
              <label for="masterName">Имя мастера</label>
              <div class="input-icon-wrap">
                <User size={16} class="input-icon" />
                <input id="masterName" type="text" class="input has-icon" bind:value={newMaster.name} placeholder="Алексей" required />
              </div>
            </div>

            <div class="form-group">
              <label for="masterDesc">Квалификация / Должность</label>
              <div class="input-icon-wrap">
                <Briefcase size={16} class="input-icon" />
                <input id="masterDesc" type="text" class="input has-icon" bind:value={newMaster.description} placeholder="Top Barber / Fade Master" />
              </div>
            </div>

            <div class="form-group full-width">
              <label for="masterTg">Telegram ID (цифровой ID или юзернейм)</label>
              <div class="input-icon-wrap">
                <Send size={16} class="input-icon" />
                <input id="masterTg" type="text" class="input has-icon" bind:value={newMaster.telegramId} placeholder="987654321 или master_tg" required />
              </div>
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" disabled={isSaving || !newMaster.name || !newMaster.telegramId}>
              {isSaving ? 'Сохранение...' : 'Сохранить мастера'}
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
        <p>Загрузка мастеров...</p>
      </div>
    {:else if errorMsg}
      <div class="alert alert-danger">
        <AlertCircle size={18} />
        <span>{errorMsg}</span>
      </div>
    {:else if masters.length === 0}
      <div class="card empty-state">
        <div class="empty-icon-circle">
          <Users size={32} />
        </div>
        <h3>У вас пока нет мастеров</h3>
        <p>Добавьте первого специалиста, чтобы начать формировать рабочий график и принимать записи.</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>
          <UserPlus size={17} />
          <span>Добавить первого мастера</span>
        </button>
      </div>
    {:else}
      <div class="grid">
        {#each masters as master}
          <div class="card master-card">
            <div class="master-top">
              <div class="master-avatar">
                {(master.name || 'M')[0].toUpperCase()}
              </div>
              <div class="master-details">
                <h3>{master.name}</h3>
                <span class="master-role">{master.description || 'Барбер'}</span>
              </div>
              <span class="status-badge" class:active={master.isActive}>
                {master.isActive ? 'Активен' : 'Неактивен'}
              </span>
            </div>

            <div class="master-meta">
              {#if master.telegramId}
                <div class="meta-row">
                  <Send size={14} class="meta-icon" />
                  <span>TG ID: <strong>{master.telegramId}</strong></span>
                </div>
              {/if}
            </div>

            <div class="master-footer">
              <button class="btn btn-danger btn-sm" on:click={() => deleteMaster(master.id)}>
                <Trash2 size={14} />
                <span>Удалить</span>
              </button>
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .masters-page {
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

  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
  }

  @media (max-width: 640px) {
    .form-grid {
      grid-template-columns: 1fr;
    }
  }

  .full-width {
    grid-column: 1 / -1;
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
    margin-top: 1.75rem;
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
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 1.5rem;
  }

  .master-card {
    display: flex;
    flex-direction: column;
    padding: 1.75rem;
    gap: 1.25rem;
  }

  .master-top {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .master-avatar {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose-dim), var(--pastel-lavender-dim));
    border: 1px solid rgba(223, 158, 142, 0.35);
    color: var(--pastel-rose);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 1.2rem;
    flex-shrink: 0;
  }

  .master-details {
    flex: 1;
    overflow: hidden;
  }

  .master-details h3 {
    font-size: 1.15rem;
    margin-bottom: 0.2rem;
  }

  .master-role {
    font-size: 0.85rem;
    color: var(--text-secondary);
  }

  .status-badge {
    font-size: 0.75rem;
    padding: 0.25rem 0.65rem;
    border-radius: var(--radius-pill);
    background-color: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.2);
    font-weight: 600;
  }

  .status-badge.active {
    background-color: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border-color: rgba(152, 193, 169, 0.25);
  }

  .master-meta {
    background: var(--bg-surface-elevated);
    padding: 0.75rem 1rem;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
  }

  .meta-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
  }

  :global(.meta-icon) {
    color: var(--pastel-lavender);
  }

  .master-footer {
    display: flex;
    justify-content: flex-end;
    margin-top: auto;
  }

  .btn-sm {
    padding: 0.5rem 1rem;
    font-size: 0.85rem;
  }
</style>

<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  let isLoading = true;
  let isSaving = false;
  let successMsg = '';
  let errorMsg = '';
  
  let settings = {
    barbershopName: '',
    barbershopAddress: '',
    barbershopDescription: '',
    ownerName: '',
    timeZone: '',
    logoUrl: '',
    brandColor: '#FFC107',
    reminderHoursBefore: 24,
    depositEnabled: false,
    depositPercent: 0,
    masterFee: 50,
    walletAddress: ''
  };

  onMount(async () => {
    try {
      const data = await apiRequest('/api/Settings');
      settings = { ...settings, ...data };
    } catch (e) {
      errorMsg = 'Не удалось загрузить настройки';
    } finally {
      isLoading = false;
    }
  });

  async function handleSave() {
    isSaving = true;
    successMsg = '';
    errorMsg = '';
    
    try {
      await apiRequest('/api/Settings', {
        method: 'PUT',
        body: JSON.stringify(settings)
      });
      successMsg = 'Настройки успешно сохранены!';
      setTimeout(() => successMsg = '', 3000);
    } catch (e) {
      errorMsg = e.message || 'Ошибка при сохранении';
    } finally {
      isSaving = false;
    }
  }
</script>

<DashboardLayout>
  <div class="settings">
    <header class="page-header">
      <h1>Настройки</h1>
      <p>Управление профилем барбершопа и финансовыми параметрами.</p>
    </header>

    {#if isLoading}
      <p>Загрузка...</p>
    {:else}
      {#if successMsg}
        <div class="alert alert-success">{successMsg}</div>
      {/if}
      {#if errorMsg}
        <div class="alert alert-danger">{errorMsg}</div>
      {/if}

      <form on:submit|preventDefault={handleSave} class="card">
        <h3>Основная информация</h3>
        <div class="form-grid">
          <div class="form-group">
            <label for="ownerName">Имя владельца</label>
            <input id="ownerName" type="text" class="input" bind:value={settings.ownerName} required />
          </div>
          <div class="form-group">
            <label for="barbershopName">Название барбершопа</label>
            <input id="barbershopName" type="text" class="input" bind:value={settings.barbershopName} required />
          </div>
          <div class="form-group">
            <label for="barbershopAddress">Адрес</label>
            <input id="barbershopAddress" type="text" class="input" bind:value={settings.barbershopAddress} required />
          </div>
          <div class="form-group">
            <label for="timeZone">Часовой пояс</label>
            <input id="timeZone" type="text" class="input" bind:value={settings.timeZone} required />
          </div>
        </div>

        <div class="form-group full-width">
          <label for="description">Описание</label>
          <textarea id="description" class="input" bind:value={settings.barbershopDescription} rows="3" required></textarea>
        </div>
        
        <div class="form-group full-width">
          <label for="walletAddress">Кошелёк для оплаты подписки</label>
          <input id="walletAddress" type="text" class="input" bind:value={settings.walletAddress} placeholder="UQD..." />
        </div>

        <hr />
        
        <h3>Брендинг</h3>
        <div class="form-grid">
          <div class="form-group">
            <label for="logoUrl">URL логотипа</label>
            <input id="logoUrl" type="text" class="input" bind:value={settings.logoUrl} />
          </div>
          <div class="form-group">
            <label for="brandColor">Цвет бренда (HEX)</label>
            <input id="brandColor" type="color" class="input color-picker" bind:value={settings.brandColor} />
          </div>
        </div>

        <hr />
        
        <h3>Финансы и правила</h3>
        <div class="form-grid">
          <div class="form-group toggle-group">
            <label class="toggle-label">
              <input type="checkbox" bind:checked={settings.depositEnabled} />
              <span>Требовать депозит при записи</span>
            </label>
          </div>
          
          {#if settings.depositEnabled}
            <div class="form-group">
              <label for="depositPercent">Размер депозита (%)</label>
              <input id="depositPercent" type="number" class="input" bind:value={settings.depositPercent} min="1" max="100" />
            </div>
          {/if}

          <div class="form-group">
            <label for="masterFee">Комиссия барбершопа (%)</label>
            <input id="masterFee" type="number" class="input" bind:value={settings.masterFee} min="0" max="100" required />
            <small class="hint">Какую долю от оплаты получает барбершоп (владелец).</small>
          </div>
          
          <div class="form-group">
            <label for="reminder">Напоминание (за N часов)</label>
            <input id="reminder" type="number" class="input" bind:value={settings.reminderHoursBefore} min="1" max="72" required />
          </div>
        </div>

        <div class="actions">
          <button type="submit" class="btn btn-primary" disabled={isSaving}>
            {isSaving ? 'Сохранение...' : 'Сохранить изменения'}
          </button>
        </div>
      </form>
    {/if}
  </div>
</DashboardLayout>

<style>
  .page-header {
    margin-bottom: 2rem;
  }
  
  hr {
    border: none;
    border-top: 1px solid var(--border-color);
    margin: 2rem 0;
  }
  
  h3 {
    margin-bottom: 1.5rem;
  }
  
  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem;
  }
  
  .form-group {
    margin-bottom: 1rem;
  }
  
  .full-width {
    margin-top: 1rem;
  }
  
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .color-picker {
    height: 48px;
    padding: 0.25rem;
    cursor: pointer;
  }
  
  .toggle-label {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    cursor: pointer;
    margin-top: 2rem;
  }
  
  .toggle-label input[type="checkbox"] {
    width: 1.25rem;
    height: 1.25rem;
    accent-color: var(--accent);
  }
  
  .hint {
    color: var(--text-muted);
    font-size: 0.75rem;
    margin-top: 0.25rem;
    display: block;
  }
  
  .actions {
    margin-top: 2rem;
    display: flex;
    justify-content: flex-end;
  }
  
  .alert {
    padding: 1rem;
    border-radius: var(--border-radius);
    margin-bottom: 1.5rem;
    font-size: 0.875rem;
  }
  
  .alert-danger {
    background-color: rgba(239, 68, 68, 0.1);
    color: var(--danger);
    border: 1px solid rgba(239, 68, 68, 0.2);
  }
  
  .alert-success {
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--success);
    border: 1px solid rgba(16, 185, 129, 0.2);
  }
</style>

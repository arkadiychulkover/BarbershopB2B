<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../../lib/api';
  import { showAlert, hapticSuccess, hapticError } from '../../lib/telegram';

  let services = [];
  let loading = true;
  let savingId = null;

  onMount(async () => {
    await loadServices();
  });

  async function loadServices() {
    loading = true;
    try {
      services = await apiFetch('/api/Barber/my-services');
      // Services is an array of MasterServiceDto
    } catch (error) {
      console.error('Failed to load services', error);
      showAlert('Ошибка при загрузке услуг');
    } finally {
      loading = false;
    }
  }

  async function saveService(service) {
    savingId = service.serviceNameId;
    try {
      await apiFetch(`/api/Barber/my-services/${service.serviceNameId}`, {
        method: 'PUT',
        body: {
          price: service.price || 0,
          duration: service.duration || 60,
          isActive: service.isActive,
          description: service.description || ''
        }
      });
      hapticSuccess();
    } catch (error) {
      console.error('Failed to save service', error);
      hapticError();
      showAlert('Ошибка при сохранении услуги');
    } finally {
      savingId = null;
    }
  }

  function toggleActive(service) {
    if (!service.isActive) {
      // Если услуга выключается - сохраняем сразу
      saveService(service);
    }
    // Принудительно обновляем массив для Svelte, чтобы отрендерить поля
    services = services;
  }
</script>

<div class="services-container">
  {#if loading}
    <div class="loader">Загрузка...</div>
  {:else if services.length === 0}
    <div class="empty-state">
      <p>В салоне пока нет добавленных услуг. Обратитесь к владельцу.</p>
    </div>
  {:else}
    <div class="services-list">
      {#each services as service (service.serviceNameId)}
        <div class="service-card" class:inactive={!service.isActive}>
          <div class="service-header">
            <h3>{service.name}</h3>
            <label class="toggle">
              <input type="checkbox" bind:checked={service.isActive} on:change={() => toggleActive(service)} />
              <span class="slider round"></span>
            </label>
          </div>
          
          {#if service.isActive}
            <div class="service-details">
              <div class="form-group">
                <label>Цена (₽)</label>
                <input type="number" bind:value={service.price} min="0" />
              </div>
              <div class="form-group">
                <label>Длительность (мин)</label>
                <input type="number" bind:value={service.duration} min="5" step="5" />
              </div>
              <div class="form-group full-width">
                <label>Описание (опционально)</label>
                <input type="text" bind:value={service.description} placeholder="Детали..." />
              </div>
              <button 
                class="save-btn" 
                disabled={savingId === service.serviceNameId || !service.price || service.price <= 0}
                on:click={() => saveService(service)}
              >
                {savingId === service.serviceNameId ? 'Сохранение...' : 'Сохранить изменения'}
              </button>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .services-container {
    padding: 16px;
  }

  .loader, .empty-state {
    text-align: center;
    padding: 32px 16px;
    color: var(--tg-theme-hint-color, #999);
  }

  .services-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .service-card {
    background: var(--tg-theme-bg-color, #fff);
    border-radius: 12px;
    padding: 16px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
    border: 1px solid var(--tg-theme-hint-color, #eee);
    transition: opacity 0.3s ease;
  }

  .service-card.inactive {
    opacity: 0.7;
  }

  .service-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .service-header h3 {
    margin: 0;
    font-size: 16px;
    color: var(--tg-theme-text-color, #000);
  }

  .service-details {
    margin-top: 16px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .form-group.full-width {
    grid-column: 1 / -1;
  }

  label {
    font-size: 12px;
    color: var(--tg-theme-hint-color, #999);
  }

  input {
    background: var(--tg-theme-secondary-bg-color, #f5f5f5);
    border: none;
    padding: 10px 12px;
    border-radius: 8px;
    color: var(--tg-theme-text-color, #000);
    font-size: 14px;
    box-sizing: border-box;
    width: 100%;
  }

  input:focus {
    outline: 2px solid var(--tg-theme-button-color, #3390ec);
  }

  .save-btn {
    grid-column: 1 / -1;
    background: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #fff);
    border: none;
    border-radius: 8px;
    padding: 12px;
    font-weight: 600;
    cursor: pointer;
    margin-top: 8px;
  }

  .save-btn:disabled {
    opacity: 0.7;
  }

  /* Toggle Switch Styles */
  .toggle {
    position: relative;
    display: inline-block;
    width: 44px;
    height: 24px;
  }

  .toggle input {
    opacity: 0;
    width: 0;
    height: 0;
  }

  .slider {
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: var(--tg-theme-hint-color, #ccc);
    transition: .4s;
  }

  .slider:before {
    position: absolute;
    content: "";
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background-color: white;
    transition: .4s;
  }

  input:checked + .slider {
    background-color: var(--tg-theme-button-color, #3390ec);
  }

  input:checked + .slider:before {
    transform: translateX(20px);
  }

  .slider.round {
    border-radius: 24px;
  }

  .slider.round:before {
    border-radius: 50%;
  }
</style>

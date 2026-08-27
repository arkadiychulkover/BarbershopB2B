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
      <p>В заведении пока нет добавленных услуг. Обратитесь к владельцу.</p>
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
                <label>Цена (₴)</label>
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
    padding: 4px 0 30px;
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .loader, .empty-state {
    text-align: center;
    padding: 50px 20px;
    color: var(--text-secondary);
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
  }

  .services-list {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }

  .service-card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border-radius: var(--radius-lg);
    padding: 18px 20px;
    box-shadow: var(--shadow-glass);
    border: 1px solid var(--border-subtle);
    transition: all 0.25s var(--ease-spring);
  }

  .service-card:hover {
    border-color: var(--border-glass);
  }

  .service-card.inactive {
    opacity: 0.6;
    border-style: dashed;
  }

  .service-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .service-header h3 {
    margin: 0;
    font-size: 16px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .service-details {
    margin-top: 18px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
    animation: fadeIn 0.25s var(--ease-spring);
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .form-group.full-width {
    grid-column: 1 / -1;
  }

  label {
    font-size: 12px;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  input {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    padding: 11px 14px;
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-size: 14px;
    font-family: var(--font-family);
    box-sizing: border-box;
    width: 100%;
    transition: border-color 0.2s;
  }

  input:focus {
    outline: none;
    border-color: var(--border-active);
  }

  .save-btn {
    grid-column: 1 / -1;
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    border-radius: var(--radius-pill);
    padding: 12px;
    font-weight: 600;
    font-size: 14px;
    cursor: pointer;
    margin-top: 6px;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }

  .save-btn:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 6px 18px var(--pastel-rose-glow);
  }

  .save-btn:active:not(:disabled) {
    transform: scale(0.97);
  }

  .save-btn:disabled {
    opacity: 0.5;
    cursor: default;
    box-shadow: none;
  }

  /* Toggle Switch Styles */
  .toggle {
    position: relative;
    display: inline-block;
    width: 46px;
    height: 26px;
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
    background-color: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    transition: .3s var(--ease-spring);
  }

  .slider:before {
    position: absolute;
    content: "";
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background-color: var(--text-secondary);
    transition: .3s var(--ease-spring);
  }

  input:checked + .slider {
    background-color: var(--pastel-rose);
    border-color: var(--pastel-rose);
    box-shadow: 0 0 12px var(--pastel-rose-glow);
  }

  input:checked + .slider:before {
    transform: translateX(20px);
    background-color: var(--text-inverse);
  }

  .slider.round {
    border-radius: 26px;
  }

  .slider.round:before {
    border-radius: 50%;
  }
</style>

<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { apiFetch, fetchImageBlob } from '../api';
  import SecureImage from './SecureImage.svelte';

  export let clientId: string;

  const dispatch = createEventDispatcher();

  let loading = true;
  let client: any = null;
  let appointments: any[] = [];
  let error = '';

  onMount(async () => {
    await loadHistory();
  });

  async function loadHistory() {
    loading = true;
    error = '';
    try {
      const data = await apiFetch(`/api/Barber/client-history/${clientId}`);
      client = data.client;
      appointments = data.appointments;
    } catch (e) {
      error = 'Не удалось загрузить историю клиента';
      console.error(e);
    } finally {
      loading = false;
    }
  }

  function statusLabel(status: number): string {
    if (status === 1) return 'Выполнено';
    if (status === 2) return 'Отменено';
    return 'Запланировано';
  }

  function formatDate(iso: string): string {
    return new Date(iso).toLocaleString('ru-RU', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  // split appointments by upcoming vs past
  $: upcoming = appointments.filter(a => new Date(a.appointmentDate) >= new Date() && a.status !== 2);
  $: past = appointments.filter(a => new Date(a.appointmentDate) < new Date() || a.status === 2);

  let lightboxSrc: string | null = null; // server path
</script>

<!-- Lightbox overlay -->
{#if lightboxSrc}
  <!-- svelte-ignore a11y-click-events-have-key-events -->
  <!-- svelte-ignore a11y-no-static-element-interactions -->
  <div class="lightbox" on:click={() => lightboxSrc = null}>
    <SecureImage src={lightboxSrc} alt="Результат" className="lightbox-img" style="max-height:85vh;object-fit:contain;" />
    <button class="lightbox-close" on:click={() => lightboxSrc = null}>✕</button>
  </div>
{/if}

<div class="history-page">
  <div class="history-header">
    <button class="back-btn" on:click={() => dispatch('back')}>
      ← Назад
    </button>
    {#if client}
      <div class="client-header-info">
        <div class="client-avatar">{(client.name || '?')[0].toUpperCase()}</div>
        <div class="client-meta">
          <div class="client-name-title">{client.name || 'Клиент'}</div>
          {#if client.telegramId && client.telegramId !== 'WALKIN'}
            <a
              class="tg-link"
              href="https://t.me/{client.telegramId}"
              target="_blank"
              rel="noopener noreferrer"
            >
              @{client.telegramId}
            </a>
          {:else}
            <span class="tg-guest">Гость (без TG)</span>
          {/if}
          {#if client.phone}
            <div class="client-phone">📞 {client.phone}</div>
          {/if}
        </div>
      </div>
    {/if}
  </div>

  {#if loading}
    <div class="loading-wrap">
      <div class="spinner-lg"></div>
      <p>Загрузка истории...</p>
    </div>
  {:else if error}
    <div class="error-msg">{error}</div>
  {:else}
    <!-- Stats strip -->
    <div class="stats-strip">
      <div class="stat">
        <div class="stat-value">{appointments.length}</div>
        <div class="stat-label">Всего</div>
      </div>
      <div class="stat">
        <div class="stat-value">{appointments.filter(a=>a.status===1).length}</div>
        <div class="stat-label">Выполнено</div>
      </div>
      <div class="stat">
        <div class="stat-value">{upcoming.length}</div>
        <div class="stat-label">Предстоит</div>
      </div>
    </div>

    {#if client?.notes}
      <div class="notes-card">
        <span class="notes-label">Заметки:</span> {client.notes}
      </div>
    {/if}

    {#if upcoming.length > 0}
      <div class="section-title">Предстоящие</div>
      {#each upcoming as appt}
        <div class="appt-row status-{appt.status}">
          <div class="appt-row-left">
            <div class="appt-row-date">{formatDate(appt.appointmentDate)}</div>
            <div class="appt-row-service">{appt.serviceName || 'Услуга'}</div>
            {#if appt.photoResultUrl}
              <!-- svelte-ignore a11y-click-events-have-key-events -->
              <!-- svelte-ignore a11y-no-static-element-interactions -->
              <div class="photo-thumb-wrap" on:click={() => lightboxSrc = appt.photoResultUrl}>
                <SecureImage src={appt.photoResultUrl} alt="Результат" className="photo-thumb" style="width:56px;height:56px;object-fit:cover;border-radius:8px;" />
                <span class="photo-thumb-label">📷 Фото результата</span>
              </div>
            {/if}
          </div>
          <span class="badge badge-{appt.status}">{statusLabel(appt.status)}</span>
        </div>
      {/each}
    {/if}

    {#if past.length > 0}
      <div class="section-title">История</div>
      {#each past as appt}
        <div class="appt-row status-{appt.status}">
          <div class="appt-row-left">
            <div class="appt-row-date">{formatDate(appt.appointmentDate)}</div>
            <div class="appt-row-service">{appt.serviceName || 'Услуга'}</div>
            {#if appt.photoResultUrl}
              <!-- svelte-ignore a11y-click-events-have-key-events -->
              <!-- svelte-ignore a11y-no-static-element-interactions -->
              <div class="photo-thumb-wrap" on:click={() => lightboxSrc = appt.photoResultUrl}>
                <SecureImage src={appt.photoResultUrl} alt="Результат" className="photo-thumb" style="width:56px;height:56px;object-fit:cover;border-radius:8px;" />
                <span class="photo-thumb-label">📷 Фото результата</span>
              </div>
            {/if}
          </div>
          <span class="badge badge-{appt.status}">{statusLabel(appt.status)}</span>
        </div>
      {/each}
    {/if}

    {#if appointments.length === 0}
      <div class="empty-hist">
        <div class="empty-icon">📋</div>
        <p>У этого клиента пока нет записей</p>
      </div>
    {/if}
  {/if}
</div>

<style>
  .history-page {
    padding: 0 0 80px;
    background: var(--tg-theme-secondary-bg-color, #f5f5f5);
    min-height: 100vh;
  }

  /* Header */
  .history-header {
    background: var(--tg-theme-bg-color, #fff);
    padding: 12px 16px 16px;
    border-bottom: 1px solid var(--tg-theme-hint-color, #eee);
  }

  .back-btn {
    background: none;
    border: none;
    color: var(--tg-theme-button-color, #3390ec);
    font-size: 16px;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    margin-bottom: 12px;
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  .client-header-info {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  .client-avatar {
    width: 52px;
    height: 52px;
    border-radius: 50%;
    background: linear-gradient(135deg, #3390ec, #a855f7);
    color: #fff;
    font-size: 22px;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .client-meta {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }

  .client-name-title {
    font-size: 18px;
    font-weight: 700;
    color: var(--tg-theme-text-color, #000);
  }

  .tg-link {
    font-size: 14px;
    color: #3390ec;
    text-decoration: none;
    font-weight: 500;
  }
  .tg-link:hover { text-decoration: underline; }

  .tg-guest {
    font-size: 13px;
    color: var(--tg-theme-hint-color, #999);
  }

  .client-phone {
    font-size: 13px;
    color: var(--tg-theme-hint-color, #999);
  }

  /* Stats strip */
  .stats-strip {
    display: flex;
    background: var(--tg-theme-bg-color, #fff);
    margin: 12px 16px;
    border-radius: 14px;
    overflow: hidden;
    box-shadow: 0 1px 4px rgba(0,0,0,0.06);
  }

  .stat {
    flex: 1;
    padding: 14px 8px;
    text-align: center;
    border-right: 1px solid var(--tg-theme-hint-color, #eee);
  }
  .stat:last-child { border-right: none; }

  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: var(--tg-theme-text-color, #000);
  }

  .stat-label {
    font-size: 11px;
    color: var(--tg-theme-hint-color, #999);
    margin-top: 2px;
  }

  /* Notes */
  .notes-card {
    margin: 0 16px 12px;
    background: rgba(51,144,236,0.07);
    border-left: 3px solid #3390ec;
    border-radius: 8px;
    padding: 10px 12px;
    font-size: 14px;
    color: var(--tg-theme-text-color, #000);
  }
  .notes-label { font-weight: 600; }

  /* Section title */
  .section-title {
    font-size: 13px;
    font-weight: 600;
    color: var(--tg-theme-hint-color, #999);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 0 16px;
    margin: 16px 0 8px;
  }

  /* Appointment rows */
  .appt-row {
    background: var(--tg-theme-bg-color, #fff);
    margin: 0 16px 10px;
    border-radius: 12px;
    padding: 14px;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    border-left: 4px solid var(--tg-theme-button-color, #3390ec);
    box-shadow: 0 1px 3px rgba(0,0,0,0.04);
  }
  .appt-row.status-1 { border-left-color: #4CAF50; }
  .appt-row.status-2 { border-left-color: #F44336; opacity: 0.6; }

  .appt-row-left { flex: 1; }

  .appt-row-date {
    font-size: 13px;
    color: var(--tg-theme-hint-color, #999);
    margin-bottom: 4px;
  }

  .appt-row-service {
    font-size: 15px;
    font-weight: 600;
    color: var(--tg-theme-text-color, #000);
  }

  /* Photo thumb */
  .photo-thumb-wrap {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 10px;
    cursor: pointer;
  }

  .photo-thumb {
    width: 56px;
    height: 56px;
    object-fit: cover;
    border-radius: 8px;
    border: 1px solid var(--tg-theme-hint-color, #ddd);
    flex-shrink: 0;
  }

  .photo-thumb-label {
    font-size: 13px;
    color: var(--tg-theme-button-color, #3390ec);
    font-weight: 500;
  }

  /* Badge */
  .badge {
    font-size: 11px;
    font-weight: 700;
    padding: 4px 10px;
    border-radius: 20px;
    white-space: nowrap;
    flex-shrink: 0;
  }
  .badge-0 { background: rgba(51,144,236,0.12); color: #3390ec; }
  .badge-1 { background: rgba(76,175,80,0.15);  color: #4CAF50; }
  .badge-2 { background: rgba(244,67,54,0.12);  color: #F44336; }

  /* Lightbox */
  .lightbox {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.88);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 999;
    padding: 20px;
  }

  .lightbox-img {
    max-width: 100%;
    max-height: 85vh;
    border-radius: 12px;
    object-fit: contain;
  }

  .lightbox-close {
    position: absolute;
    top: 16px;
    right: 16px;
    background: rgba(255,255,255,0.15);
    border: none;
    color: #fff;
    font-size: 20px;
    width: 36px;
    height: 36px;
    border-radius: 50%;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  /* Loading / empty */
  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 60px 20px;
    color: var(--tg-theme-hint-color, #999);
  }

  .spinner-lg {
    width: 36px;
    height: 36px;
    border: 3px solid var(--tg-theme-hint-color, #ccc);
    border-top-color: var(--tg-theme-button-color, #3390ec);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    margin-bottom: 12px;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  .error-msg {
    text-align: center;
    padding: 40px 20px;
    color: #F44336;
  }

  .empty-hist {
    text-align: center;
    padding: 60px 20px;
    color: var(--tg-theme-hint-color, #999);
  }

  .empty-icon {
    font-size: 48px;
    margin-bottom: 12px;
  }
</style>

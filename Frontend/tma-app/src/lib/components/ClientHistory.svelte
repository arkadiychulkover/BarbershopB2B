<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { apiFetch, fetchImageBlob } from '../api';
  import SecureImage from './SecureImage.svelte';
  import Icon from './Icon.svelte';

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

  let editingCommentId: string | null = null;
  let commentText = '';

  function openCommentEdit(appt: any) {
    editingCommentId = appt.id;
    commentText = appt.resultNote || '';
  }

  function cancelCommentEdit() {
    editingCommentId = null;
  }

  async function saveComment(apptId: string) {
    try {
      const res = await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
        method: 'POST',
        body: { comment: commentText }
      });
      const index = appointments.findIndex(a => a.id === apptId);
      if (index !== -1) {
        appointments[index].resultNote = res.comment;
        appointments = [...appointments];
      }
      editingCommentId = null;
    } catch (e) {
      console.error(e);
      alert('Ошибка при сохранении комментария');
    }
  }

  async function deleteComment(apptId: string) {
    if (!confirm('Удалить комментарий?')) return;
    try {
      await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
        method: 'DELETE'
      });
      const index = appointments.findIndex(a => a.id === apptId);
      if (index !== -1) {
        appointments[index].resultNote = null;
        appointments = [...appointments];
      }
    } catch (e) {
      console.error(e);
      alert('Ошибка при удалении комментария');
    }
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
    <button class="lightbox-close" on:click={() => lightboxSrc = null}>
      <Icon name="x" size={20} />
    </button>
  </div>
{/if}

<div class="history-page">
  <div class="history-header">
    <button class="back-btn" on:click={() => dispatch('back')}>
      <Icon name="chevron-left" size={16} />
      <span>Назад</span>
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
            <div class="client-phone">
              <Icon name="phone" size={13} color="var(--pastel-lavender)" />
              <span>{client.phone}</span>
            </div>
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
        <div class="appt-card-wrapper status-{appt.status}">
          <div class="appt-row">
            <div class="appt-row-left">
              <div class="appt-row-date">{formatDate(appt.appointmentDate)}</div>
              <div class="appt-row-service">{appt.serviceName || 'Услуга'}</div>
              {#if appt.photoResultUrl}
                <!-- svelte-ignore a11y-click-events-have-key-events -->
                <!-- svelte-ignore a11y-no-static-element-interactions -->
                <div class="photo-thumb-wrap" on:click={() => lightboxSrc = appt.photoResultUrl}>
                  <SecureImage src={appt.photoResultUrl} alt="Результат" className="photo-thumb" style="width:56px;height:56px;object-fit:cover;border-radius:8px;" />
                  <span class="photo-thumb-label">
                    <Icon name="camera" size={14} color="var(--pastel-rose)" />
                    <span>Фото результата</span>
                  </span>
                </div>
              {/if}
            </div>
            <span class="badge badge-{appt.status}">{statusLabel(appt.status)}</span>
          </div>

          <!-- Comment Section -->
          <div class="appt-comment-section">
            {#if editingCommentId === appt.id}
              <textarea class="comment-input" bind:value={commentText} placeholder="Комментарий / Заметка..."></textarea>
              <div class="comment-actions">
                <button class="btn-save" on:click={() => saveComment(appt.id)}>Сохранить</button>
                <button class="btn-cancel" on:click={cancelCommentEdit}>Отмена</button>
              </div>
            {:else if appt.resultNote}
              <div class="comment-display">
                <div class="comment-text">
                  <Icon name="comment" size={13} color="var(--pastel-rose)" />
                  <span>{appt.resultNote}</span>
                </div>
                <div class="comment-actions-sm">
                  <button on:click={() => openCommentEdit(appt)}>Ред.</button>
                  <button class="text-danger" on:click={() => deleteComment(appt.id)}>Удал.</button>
                </div>
              </div>
            {:else}
              <button class="btn-add-comment" on:click={() => openCommentEdit(appt)}>+ Добавить комментарий</button>
            {/if}
          </div>
        </div>
      {/each}
    {/if}

    {#if past.length > 0}
      <div class="section-title">История</div>
      {#each past as appt}
        <div class="appt-card-wrapper status-{appt.status}">
          <div class="appt-row">
            <div class="appt-row-left">
              <div class="appt-row-date">{formatDate(appt.appointmentDate)}</div>
              <div class="appt-row-service">{appt.serviceName || 'Услуга'}</div>
              {#if appt.photoResultUrl}
                <!-- svelte-ignore a11y-click-events-have-key-events -->
                <!-- svelte-ignore a11y-no-static-element-interactions -->
                <div class="photo-thumb-wrap" on:click={() => lightboxSrc = appt.photoResultUrl}>
                  <SecureImage src={appt.photoResultUrl} alt="Результат" className="photo-thumb" style="width:56px;height:56px;object-fit:cover;border-radius:8px;" />
                  <span class="photo-thumb-label">
                    <Icon name="camera" size={14} color="var(--pastel-rose)" />
                    <span>Фото результата</span>
                  </span>
                </div>
              {/if}
            </div>
            <span class="badge badge-{appt.status}">{statusLabel(appt.status)}</span>
          </div>

          <!-- Comment Section -->
          <div class="appt-comment-section">
            {#if editingCommentId === appt.id}
              <textarea class="comment-input" bind:value={commentText} placeholder="Комментарий / Заметка..."></textarea>
              <div class="comment-actions">
                <button class="btn-save" on:click={() => saveComment(appt.id)}>Сохранить</button>
                <button class="btn-cancel" on:click={cancelCommentEdit}>Отмена</button>
              </div>
            {:else if appt.resultNote}
              <div class="comment-display">
                <div class="comment-text">
                  <Icon name="comment" size={13} color="var(--pastel-rose)" />
                  <span>{appt.resultNote}</span>
                </div>
                <div class="comment-actions-sm">
                  <button on:click={() => openCommentEdit(appt)}>Ред.</button>
                  <button class="text-danger" on:click={() => deleteComment(appt.id)}>Удал.</button>
                </div>
              </div>
            {:else}
              <button class="btn-add-comment" on:click={() => openCommentEdit(appt)}>+ Добавить комментарий</button>
            {/if}
          </div>
        </div>
      {/each}
    {/if}

    {#if appointments.length === 0}
      <div class="empty-hist">
        <div class="empty-icon">
          <Icon name="clipboard" size={44} color="var(--pastel-rose)" />
        </div>
        <p>У этого клиента пока нет записей</p>
      </div>
    {/if}
  {/if}
</div>

<style>
  .history-page {
    padding: 0 0 80px;
    background: var(--bg-canvas);
    min-height: 100vh;
    animation: fadeIn 0.3s var(--ease-spring);
  }

  /* Header */
  .history-header {
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    padding: 16px 18px 18px;
    border-bottom: 1px solid var(--border-subtle);
    position: sticky;
    top: 0;
    z-index: 50;
  }

  .back-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--pastel-rose);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    margin-bottom: 14px;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: all 0.2s var(--ease-spring);
  }

  .back-btn:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
  }

  .back-btn:active {
    transform: scale(0.95);
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
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-lavender));
    color: var(--text-inverse);
    font-size: 22px;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
  }

  .client-meta {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }

  .client-name-title {
    font-size: 18px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .tg-link {
    font-size: 14px;
    color: var(--pastel-lavender);
    text-decoration: none;
    font-weight: 600;
    transition: color 0.2s;
  }
  .tg-link:hover { color: var(--pastel-rose); text-decoration: underline; }

  .tg-guest {
    font-size: 13px;
    color: var(--text-muted);
  }

  .client-phone {
    font-size: 13px;
    color: var(--text-secondary);
  }

  /* Stats strip */
  .stats-strip {
    display: flex;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    margin: 14px 16px;
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow-glass);
    border: 1px solid var(--border-subtle);
  }

  .stat {
    flex: 1;
    padding: 16px 8px;
    text-align: center;
    border-right: 1px solid var(--border-subtle);
  }
  .stat:last-child { border-right: none; }

  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: var(--pastel-rose);
    font-variant-numeric: tabular-nums;
  }

  .stat-label {
    font-size: 11px;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-top: 4px;
  }

  /* Notes */
  .notes-card {
    margin: 0 16px 14px;
    background: var(--pastel-rose-dim);
    border-left: 3px solid var(--pastel-rose);
    border-radius: var(--radius-md);
    padding: 12px 16px;
    font-size: 14px;
    color: var(--text-primary);
    line-height: 1.4;
  }
  .notes-label { font-weight: 700; color: var(--pastel-rose); }

  /* Section title */
  .section-title {
    font-size: 13px;
    font-weight: 700;
    color: var(--pastel-rose);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    padding: 0 20px;
    margin: 20px 0 10px;
  }

  /* Appointment rows */
  .appt-card-wrapper {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    margin: 0 16px 12px;
    border-radius: var(--radius-lg);
    display: flex;
    flex-direction: column;
    border: 1px solid var(--border-subtle);
    border-left: 4px solid var(--pastel-amber);
    box-shadow: var(--shadow-glass);
    overflow: hidden;
    transition: all 0.2s var(--ease-spring);
  }
  .appt-card-wrapper:hover {
    border-color: var(--border-glass);
  }
  .appt-card-wrapper.status-1 { border-left-color: var(--pastel-sage); }
  .appt-card-wrapper.status-2 { border-left-color: var(--pastel-coral); opacity: 0.65; }

  .appt-row {
    padding: 16px;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
  }

  /* Comment UI */
  .appt-comment-section {
    padding: 0 16px 16px;
  }
  .comment-input {
    width: 100%;
    min-height: 64px;
    padding: 10px 14px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    font-size: 14px;
    resize: vertical;
    margin-bottom: 8px;
    background: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-family: var(--font-family);
    box-sizing: border-box;
    transition: border-color 0.2s;
  }
  .comment-input:focus {
    outline: none;
    border-color: var(--border-active);
  }
  .comment-actions {
    display: flex;
    gap: 8px;
  }
  .btn-save {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 2px 8px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }
  .btn-save:active { transform: scale(0.95); }

  .btn-cancel {
    background: transparent;
    color: var(--text-secondary);
    border: 1px solid var(--border-subtle);
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .btn-cancel:hover { color: var(--text-primary); border-color: var(--border-glass); }

  .comment-display {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    padding: 12px 14px;
    border-radius: var(--radius-md);
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 8px;
  }
  .comment-text {
    font-size: 13px;
    color: var(--text-primary);
    line-height: 1.5;
    white-space: pre-wrap;
    flex: 1;
  }
  .comment-actions-sm {
    display: flex;
    gap: 8px;
  }
  .comment-actions-sm button {
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
  }
  .comment-actions-sm button.text-danger {
    color: var(--pastel-coral);
  }
  .btn-add-comment {
    background: none;
    border: none;
    color: var(--text-muted);
    font-size: 13px;
    cursor: pointer;
    padding: 0;
    display: flex;
    align-items: center;
    transition: color 0.2s;
  }
  .btn-add-comment:hover { color: var(--pastel-rose); }

  .appt-row-left { flex: 1; }

  .appt-row-date {
    font-size: 13px;
    color: var(--text-muted);
    margin-bottom: 4px;
    font-variant-numeric: tabular-nums;
  }

  .appt-row-service {
    font-size: 15px;
    font-weight: 600;
    color: var(--text-primary);
  }

  /* Photo thumb */
  .photo-thumb-wrap {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 10px;
    cursor: pointer;
  }

  .photo-thumb {
    width: 56px;
    height: 56px;
    object-fit: cover;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    flex-shrink: 0;
  }

  .photo-thumb-label {
    font-size: 13px;
    color: var(--pastel-rose);
    font-weight: 600;
  }

  /* Badge */
  .badge {
    font-size: 11px;
    font-weight: 700;
    padding: 4px 12px;
    border-radius: var(--radius-pill);
    white-space: nowrap;
    flex-shrink: 0;
    letter-spacing: 0.02em;
  }
  .badge-0 { background: var(--pastel-amber-dim); color: var(--pastel-amber); border: 1px solid rgba(229, 190, 138, 0.25); }
  .badge-1 { background: var(--pastel-sage-dim);  color: var(--pastel-sage);  border: 1px solid rgba(152, 193, 169, 0.25); }
  .badge-2 { background: var(--pastel-coral-dim); color: var(--pastel-coral); border: 1px solid rgba(232, 130, 130, 0.25); }

  /* Lightbox */
  .lightbox {
    position: fixed;
    inset: 0;
    background: rgba(12, 14, 18, 0.92);
    backdrop-filter: blur(24px);
    -webkit-backdrop-filter: blur(24px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 999;
    padding: 20px;
    animation: fadeIn 0.2s ease;
  }

  .lightbox-img {
    max-width: 100%;
    max-height: 85vh;
    border-radius: var(--radius-lg);
    object-fit: contain;
    box-shadow: 0 16px 48px rgba(0,0,0,0.7);
  }

  .lightbox-close {
    position: absolute;
    top: 20px;
    right: 20px;
    background: rgba(255,255,255,0.1);
    border: 1px solid rgba(255,255,255,0.15);
    color: #fff;
    font-size: 20px;
    width: 40px;
    height: 40px;
    border-radius: 50%;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.2s;
  }
  .lightbox-close:hover {
    background: rgba(255,255,255,0.2);
  }

  /* Loading / empty */
  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 80px 20px;
    color: var(--text-secondary);
  }

  .spinner-lg {
    width: 40px;
    height: 40px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
    margin-bottom: 14px;
    box-shadow: 0 0 16px var(--pastel-rose-glow);
  }

  .error-msg {
    text-align: center;
    padding: 50px 20px;
    color: var(--pastel-coral);
  }

  .empty-hist {
    text-align: center;
    padding: 60px 20px;
    color: var(--text-secondary);
  }

  .empty-icon {
    font-size: 44px;
    margin-bottom: 12px;
  }
</style>

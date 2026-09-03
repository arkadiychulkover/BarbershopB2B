<script lang="ts">
  import { createEventDispatcher, onMount } from "svelte";
  import { apiFetch, fetchImageBlob } from "../api";
  import {
    showAlert,
    showConfirm,
    hapticSuccess,
    hapticError,
    hapticWarning,
  } from "../telegram";
  import SecureImage from "./SecureImage.svelte";
  import Icon from "./Icon.svelte";

  export let clientId: string;

  const dispatch = createEventDispatcher();

  let loading = true;
  let client: any = null;
  let appointments: any[] = [];
  let error = "";

  // Photo upload state
  let uploadingId: string | null = null;
  let photoInputEl: HTMLInputElement;
  let pendingPhotoApptId: string | null = null;

  onMount(async () => {
    await loadHistory();
  });

  async function loadHistory() {
    loading = true;
    error = "";
    try {
      const data = await apiFetch(`/api/Barber/client-history/${clientId}`);
      client = data.client;
      appointments = data.appointments;
    } catch (e) {
      error = m.tma_history_load_error();
      console.error(e);
    } finally {
      loading = false;
    }
  }

  function statusLabel(status: number): string {
    if (status === 1) return m.tma_status_done();
    if (status === 2) return m.tma_status_cancelled();
    return m.tma_status_scheduled();
  }

  function isPureNumeric(val: string | null | undefined): boolean {
    return !!val && /^\d+$/.test(val.trim());
  }

  function formatDate(iso: string, endIso?: string): string {
    if (!iso) return "";
    const raw = String(iso).replace("Z", "").replace("T", " ");
    const parts = raw.match(/(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2})/);
    if (!parts) return String(iso);
    const start = `${parts[4]}:${parts[5]}`;
    let end = "";
    if (endIso) {
      const endRaw = String(endIso).replace("Z", "").replace("T", " ");
      const endParts = endRaw.match(/(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2})/);
      if (endParts) end = `${endParts[4]}:${endParts[5]}`;
    }
    const timeDisplay = end && end !== start ? `${start} – ${end}` : start;
    return `${parts[3]}.${parts[2]}.${parts[1]}, ${timeDisplay}`;
  }

  function triggerPhotoUpload(apptId: string, event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    const appt = appointments.find((a) => a.id === apptId);
    if (appt && appt.status !== 1) {
      showAlert(
        m.tma_photo_only_completed(),
      );
      return;
    }
    pendingPhotoApptId = apptId;
    if (photoInputEl) {
      photoInputEl.value = "";
      photoInputEl.click();
    }
  }

  async function handlePhotoSelected(event: any) {
    const file = event.target.files?.[0];
    if (!file || !pendingPhotoApptId) return;

    uploadingId = pendingPhotoApptId;
    try {
      const formData = new FormData();
      formData.append("photo", file);

      const result = await apiFetch(`/api/Barber/put-photo/${pendingPhotoApptId}`, {
        method: "POST",
        body: formData,
      });

      const idx = appointments.findIndex((a) => a.id === pendingPhotoApptId);
      if (idx !== -1 && result?.photoUrl) {
        appointments[idx].photoResultUrl = result.photoUrl;
        appointments = [...appointments];
      }
      hapticSuccess();
    } catch (e: any) {
      hapticError();
      showAlert(m.tma_photo_upload_err() + (e?.message || ""));
    } finally {
      uploadingId = null;
      pendingPhotoApptId = null;
    }
  }

  let editingCommentId: string | null = null;
  let commentText = "";

  function openCommentEdit(appt: any) {
    editingCommentId = appt.id;
    commentText = appt.resultNote || "";
  }

  function cancelCommentEdit() {
    editingCommentId = null;
  }

  async function saveComment(apptId: string) {
    try {
      const res = await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
        method: "POST",
        body: { comment: commentText },
      });
      const index = appointments.findIndex((a) => a.id === apptId);
      if (index !== -1) {
        appointments[index].resultNote = res.comment;
        appointments = [...appointments];
      }
      editingCommentId = null;
    } catch (e) {
      console.error(e);
      alert(m.tma_comment_save_error());
    }
  }

  async function deleteComment(apptId: string) {
    if (!confirm(m.tma_delete_comment_confirm())) return;
    try {
      await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
        method: "DELETE",
      });
      const index = appointments.findIndex((a) => a.id === apptId);
      if (index !== -1) {
        appointments[index].resultNote = null;
        appointments = [...appointments];
      }
    } catch (e) {
      console.error(e);
      alert(m.tma_delete_comment_error());
    }
  }

  function cancelAppointment(apptId: string) {
    hapticWarning();
    showConfirm(
      m.tma_cancel_appt_confirm(),
      async (confirmed) => {
        if (!confirmed) return;
        try {
          await apiFetch(`/api/Barber/my-appointments/cancel/${apptId}`, {
            method: "PUT",
          });
          hapticSuccess();
          showAlert(m.tma_appt_cancelled_success());
          await loadHistory();
        } catch (e: any) {
          hapticError();
          showAlert(m.tma_cancel_error() + (e.message || ""));
        }
      },
    );
  }

  // split appointments by upcoming vs past
  $: upcoming = appointments.filter(
    (a) => new Date(a.appointmentDate) >= new Date() && a.status !== 2,
  );
  $: past = appointments.filter(
    (a) => new Date(a.appointmentDate) < new Date() || a.status === 2,
  );

  let isEditingClientNotes = false;
  let clientNotesText = "";
  let savingClientNotes = false;

  function startEditClientNotes() {
    clientNotesText = client?.notes || "";
    isEditingClientNotes = true;
  }

  function cancelEditClientNotes() {
    isEditingClientNotes = false;
  }

  async function saveClientNotes() {
    savingClientNotes = true;
    try {
      await apiFetch(`/api/Barber/client-notes/${clientId}`, {
        method: "PATCH",
        body: { notes: clientNotesText },
      });
      if (client) {
        client.notes = clientNotesText.trim() || null;
      }
      isEditingClientNotes = false;
      hapticSuccess();
    } catch (e) {
      alert(m.tma_notes_save_error());
    } finally {
      savingClientNotes = false;
    }
  }

  let lightboxSrc: string | null = null; // server path
</script>

<input
  type="file"
  accept="image/*"
  style="display:none"
  bind:this={photoInputEl}
  on:change={handlePhotoSelected}
/>

<!-- Lightbox overlay -->
{#if lightboxSrc}
  <!-- svelte-ignore a11y-click-events-have-key-events -->
  <!-- svelte-ignore a11y-no-static-element-interactions -->
  <div class="lightbox" on:click={() => (lightboxSrc = null)}>
    <SecureImage
      src={lightboxSrc}
      alt={m.tma_result()}
      className="lightbox-img"
      style="max-height:85vh;object-fit:contain;"
    />
    <button class="lightbox-close" on:click={() => (lightboxSrc = null)}>
      <Icon name="x" size={20} />
    </button>
  </div>
{/if}

<div class="history-page">
  <div class="history-header">
    <button class="back-btn" on:click={() => dispatch("back")}>
      <Icon name="chevron-left" size={16} />
      <span>{m.tma_back()}</span>
    </button>
    {#if client}
      <div class="client-header-info">
        <div class="client-avatar">{(client.name || "?")[0].toUpperCase()}</div>
        <div class="client-meta">
          <div class="client-name-title">{client.name || m.tma_client()}</div>
          {#if client.telegramUsername}
            <a
              class="tg-link"
              href="https://t.me/{client.telegramUsername}"
              target="_blank"
              rel="noopener noreferrer"
            >
              @{client.telegramUsername}
            </a>
          {:else if client.telegramId && client.telegramId !== "WALKIN"}
            {#if isPureNumeric(client.telegramId)}
              <span class="tg-id-text">ID: {client.telegramId}</span>
            {:else}
              <a
                class="tg-link"
                href="https://t.me/{client.telegramId.replace(/^@/, '')}"
                target="_blank"
                rel="noopener noreferrer"
              >
                @{client.telegramId.replace(/^@/, '')}
              </a>
            {/if}
          {:else}
            <span class="tg-guest">{m.tma_guest_no_tg()}</span>
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
      <p>{m.tma_loading_history()}</p>
    </div>
  {:else if error}
    <div class="error-msg">{error}</div>
  {:else}
    <!-- Stats strip -->
    <div class="stats-strip">
      <div class="stat">
        <div class="stat-value">{appointments.length}</div>
        <div class="stat-label">{m.tma_total()}</div>
      </div>
      <div class="stat">
        <div class="stat-value">
          {appointments.filter((a) => a.status === 1).length}
        </div>
        <div class="stat-label">{m.tma_status_done()}</div>
      </div>
      <div class="stat">
        <div class="stat-value">{upcoming.length}</div>
        <div class="stat-label">{m.tma_upcoming()}</div>
      </div>
    </div>

    <div class="notes-card">
      <div class="notes-header-row">
        <span class="notes-label">{m.tma_haircut_profile()}</span>
        {#if !isEditingClientNotes}
          <button class="notes-edit-btn" on:click={startEditClientNotes}>
            <Icon name="edit" size={13} />
            <span>{client?.notes ? m.tma_change() : m.tma_add()}</span>
          </button>
        {/if}
      </div>
      {#if isEditingClientNotes}
        <textarea
          class="notes-input"
          bind:value={clientNotesText}
          placeholder={m.tma_notes_placeholder()}
        ></textarea>
        <div class="notes-actions">
          <button
            class="btn-save"
            on:click={saveClientNotes}
            disabled={savingClientNotes}
          >
            {savingClientNotes ? m.tma_saving() : m.tma_save()}
          </button>
          <button class="btn-cancel" on:click={cancelEditClientNotes}
            >{m.tma_cancel()}</button>
        </div>
      {:else if client?.notes}
        <div class="notes-content">{client.notes}</div>
      {:else}
        <div class="notes-placeholder">{m.tma_no_notes()}</div>
      {/if}
    </div>

    {#if upcoming.length > 0}
      <div class="section-title">{m.tma_upcoming_section()}</div>
      {#each upcoming as appt}
        <div class="appt-card-wrapper status-{appt.status}">
          <div class="appt-row">
            <div class="appt-header-row">
              <div class="appt-row-date">
                {formatDate(appt.appointmentDate, appt.appointmentEndDate)}
              </div>
              <span class="badge badge-{appt.status}"
                >{statusLabel(appt.status)}</span
              >
            </div>

            <div class="appt-row-service">{appt.serviceName || m.tma_service()}</div>

            {#if appt.photoResultUrl}
              <div class="photo-result-row">
                <!-- svelte-ignore a11y-click-events-have-key-events -->
                <!-- svelte-ignore a11y-no-static-element-interactions -->
                <div
                  class="photo-thumb-wrap"
                  on:click={() => (lightboxSrc = appt.photoResultUrl)}
                >
                  <SecureImage
                    src={appt.photoResultUrl}
                    alt={m.tma_result()}
                    className="photo-thumb"
                    style="width:56px;height:56px;object-fit:cover;border-radius:8px;"
                  />
                  <span class="photo-thumb-label">
                    <Icon
                      name="camera"
                      size={14}
                      color="var(--pastel-rose)"
                    />
                    <span>{m.tma_photo_result()}</span>
                  </span>
                </div>
                {#if appt.status === 1}
                  <button
                    type="button"
                    class="btn-change-photo"
                    on:click={(e) => triggerPhotoUpload(appt.id, e)}
                    disabled={uploadingId === appt.id}
                    title={m.tma_replace_photo()}
                  >
                    {#if uploadingId === appt.id}
                      <span class="spinner-sm"></span>
                    {:else}
                      <Icon
                        name="refresh"
                        size={13}
                        color="var(--pastel-lavender)"
                      />
                      <span>{m.tma_replace()}</span>
                    {/if}
                  </button>
                {/if}
              </div>
            {:else if appt.status === 1}
              <div class="attach-photo-wrap">
                <button
                  type="button"
                  class="btn-attach-photo"
                  on:click={(e) => triggerPhotoUpload(appt.id, e)}
                  disabled={uploadingId === appt.id}
                >
                  {#if uploadingId === appt.id}
                    <span class="spinner-sm"></span>
                    <span>{m.tma_loading()}</span>
                  {:else}
                    <Icon
                      name="camera"
                      size={14}
                      color="var(--pastel-rose)"
                    />
                    <span>{m.tma_attach_result_photo()}</span>
                  {/if}
                </button>
              </div>
            {/if}

            {#if appt.status !== 2}
              <div class="appt-actions-row">
                <button
                  type="button"
                  class="btn-cancel-appt"
                  on:click={() => cancelAppointment(appt.id)}
                >
                  <Icon name="x" size={12} />
                  <span>{m.tma_cancel()}</span>
                </button>
              </div>
            {/if}
          </div>

          <!-- Comment Section -->
          <div class="appt-comment-section">
            {#if editingCommentId === appt.id}
              <textarea
                class="comment-input"
                bind:value={commentText}
                placeholder={m.tma_comment_placeholder()}
              ></textarea>
              <div class="comment-actions">
                <button class="btn-save" on:click={() => saveComment(appt.id)}
                  >{m.tma_save()}</button>
                <button class="btn-cancel" on:click={cancelCommentEdit}
                  >{m.tma_cancel()}</button>
              </div>
            {:else if appt.resultNote}
              <div class="comment-display">
                <div class="comment-text">
                  <Icon name="comment" size={13} color="var(--pastel-rose)" />
                  <span>{appt.resultNote}</span>
                </div>
                <div class="comment-actions-sm">
                  <button on:click={() => openCommentEdit(appt)}>{m.tma_edit_short()}</button>
                  <button
                    class="text-danger"
                    on:click={() => deleteComment(appt.id)}>{m.tma_delete_short()}</button>
                </div>
              </div>
            {:else}
              <button
                class="btn-add-comment"
                on:click={() => openCommentEdit(appt)}
                >{m.tma_add_comment()}</button>
            {/if}
          </div>
        </div>
      {/each}
    {/if}

    {#if past.length > 0}
      <div class="section-title">{m.tma_history_section()}</div>
      {#each past as appt}
        <div class="appt-card-wrapper status-{appt.status}">
          <div class="appt-row">
            <div class="appt-header-row">
              <div class="appt-row-date">
                {formatDate(appt.appointmentDate, appt.appointmentEndDate)}
              </div>
              <span class="badge badge-{appt.status}"
                >{statusLabel(appt.status)}</span
              >
            </div>

            <div class="appt-row-service">{appt.serviceName || m.tma_service()}</div>

            {#if appt.photoResultUrl}
              <div class="photo-result-row">
                <!-- svelte-ignore a11y-click-events-have-key-events -->
                <!-- svelte-ignore a11y-no-static-element-interactions -->
                <div
                  class="photo-thumb-wrap"
                  on:click={() => (lightboxSrc = appt.photoResultUrl)}
                >
                  <SecureImage
                    src={appt.photoResultUrl}
                    alt={m.tma_result()}
                    className="photo-thumb"
                    style="width:56px;height:56px;object-fit:cover;border-radius:8px;"
                  />
                  <span class="photo-thumb-label">
                    <Icon
                      name="camera"
                      size={14}
                      color="var(--pastel-rose)"
                    />
                    <span>{m.tma_photo_result()}</span>
                  </span>
                </div>
                {#if appt.status === 1}
                  <button
                    type="button"
                    class="btn-change-photo"
                    on:click={(e) => triggerPhotoUpload(appt.id, e)}
                    disabled={uploadingId === appt.id}
                    title={m.tma_replace_photo()}
                  >
                    {#if uploadingId === appt.id}
                      <span class="spinner-sm"></span>
                    {:else}
                      <Icon
                        name="refresh"
                        size={13}
                        color="var(--pastel-lavender)"
                      />
                      <span>{m.tma_replace()}</span>
                    {/if}
                  </button>
                {/if}
              </div>
            {:else if appt.status === 1}
              <div class="attach-photo-wrap">
                <button
                  type="button"
                  class="btn-attach-photo"
                  on:click={(e) => triggerPhotoUpload(appt.id, e)}
                  disabled={uploadingId === appt.id}
                >
                  {#if uploadingId === appt.id}
                    <span class="spinner-sm"></span>
                    <span>{m.tma_loading()}</span>
                  {:else}
                    <Icon
                      name="camera"
                      size={14}
                      color="var(--pastel-rose)"
                    />
                    <span>{m.tma_attach_result_photo()}</span>
                  {/if}
                </button>
              </div>
            {/if}
          </div>

          <!-- Comment Section -->
          <div class="appt-comment-section">
            {#if editingCommentId === appt.id}
              <textarea
                class="comment-input"
                bind:value={commentText}
                placeholder={m.tma_comment_placeholder()}
              ></textarea>
              <div class="comment-actions">
                <button class="btn-save" on:click={() => saveComment(appt.id)}
                  >{m.tma_save()}</button>
                <button class="btn-cancel" on:click={cancelCommentEdit}
                  >{m.tma_cancel()}</button>
              </div>
            {:else if appt.resultNote}
              <div class="comment-display">
                <div class="comment-text">
                  <Icon name="comment" size={13} color="var(--pastel-rose)" />
                  <span>{appt.resultNote}</span>
                </div>
                <div class="comment-actions-sm">
                  <button on:click={() => openCommentEdit(appt)}>{m.tma_edit_short()}</button>
                  <button
                    class="text-danger"
                    on:click={() => deleteComment(appt.id)}>{m.tma_delete_short()}</button>
                </div>
              </div>
            {:else}
              <button
                class="btn-add-comment"
                on:click={() => openCommentEdit(appt)}
                >{m.tma_add_comment()}</button>
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
        <p>{m.tma_client_no_appts()}</p>
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
    background: linear-gradient(
      135deg,
      var(--pastel-rose),
      var(--pastel-lavender)
    );
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
  .tg-link:hover {
    color: var(--pastel-rose);
    text-decoration: underline;
  }

  .tg-id-text {
    font-size: 13px;
    color: var(--text-muted);
    font-weight: 500;
    user-select: text;
  }

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
  .stat:last-child {
    border-right: none;
  }

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
    padding: 14px 16px;
    font-size: 14px;
    color: var(--text-primary);
    line-height: 1.4;
  }
  .notes-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
  }
  .notes-label {
    font-weight: 700;
    color: var(--pastel-rose);
  }
  .notes-edit-btn {
    background: rgba(224, 163, 154, 0.2);
    border: none;
    color: var(--pastel-rose);
    border-radius: 6px;
    padding: 3px 8px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
  }
  .notes-input {
    width: 100%;
    min-height: 70px;
    background: rgba(0, 0, 0, 0.25);
    color: #fff;
    border: 1px solid var(--border-subtle);
    border-radius: 8px;
    padding: 8px 10px;
    font-family: inherit;
    font-size: 13px;
    resize: vertical;
    outline: none;
    box-sizing: border-box;
    margin-bottom: 8px;
  }
  .notes-actions {
    display: flex;
    gap: 8px;
  }
  .notes-placeholder {
    font-size: 13px;
    color: var(--text-muted);
    font-style: italic;
  }
  .notes-content {
    white-space: pre-wrap;
    font-size: 14px;
  }

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
  .appt-card-wrapper.status-1 {
    border-left-color: var(--pastel-sage);
  }
  .appt-card-wrapper.status-2 {
    border-left-color: var(--pastel-coral);
    opacity: 0.65;
  }

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
  .btn-save:active {
    transform: scale(0.95);
  }

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
  .btn-cancel:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
  }

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
  .btn-add-comment:hover {
    color: var(--pastel-rose);
  }

  .appt-row {
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    min-width: 0;
  }

  .appt-header-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    min-width: 0;
    width: 100%;
  }

  .appt-row-date {
    font-size: 13px;
    color: var(--text-muted);
    font-variant-numeric: tabular-nums;
    font-weight: 500;
  }

  .appt-row-service {
    font-size: 15px;
    font-weight: 600;
    color: var(--text-primary);
  }

  .appt-actions-row {
    display: flex;
    justify-content: flex-end;
    margin-top: 4px;
  }

  /* Photo thumb & result row */
  .photo-result-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-top: 6px;
    background: var(--bg-surface-elevated);
    padding: 6px 12px 6px 6px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    width: 100%;
    box-sizing: border-box;
  }

  .photo-thumb-wrap {
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    min-width: 0;
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
    white-space: nowrap;
  }

  .btn-change-photo {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    font-size: 12px;
    font-weight: 600;
    padding: 6px 12px;
    border-radius: var(--radius-pill);
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    flex-shrink: 0;
  }
  .btn-change-photo:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }
  .btn-change-photo:active {
    transform: scale(0.96);
  }

  .attach-photo-wrap {
    margin-top: 6px;
  }

  .btn-attach-photo {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    background: var(--bg-surface-elevated);
    border: 1px dashed rgba(223, 158, 142, 0.4);
    color: var(--pastel-rose);
    font-size: 13px;
    font-weight: 600;
    padding: 8px 14px;
    border-radius: var(--radius-md);
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
  }
  .btn-attach-photo:hover {
    background: var(--bg-surface-hover);
    border-color: var(--pastel-rose);
  }
  .btn-attach-photo:active {
    transform: scale(0.97);
  }

  .spinner-sm {
    width: 14px;
    height: 14px;
    border: 2px solid rgba(223, 158, 142, 0.25);
    border-top: 2px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.8s linear infinite;
    display: inline-block;
    vertical-align: middle;
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
  .badge-0 {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
  }
  .badge-1 {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
  }
  .badge-2 {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.25);
  }

  .btn-cancel-appt {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.25);
    border-radius: var(--radius-pill);
    padding: 4px 10px;
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 4px;
    transition: all 0.2s;
  }
  .btn-cancel-appt:hover {
    background: rgba(232, 130, 130, 0.22);
    border-color: var(--pastel-coral);
  }
  .btn-cancel-appt:active {
    transform: scale(0.95);
  }

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
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.7);
  }

  .lightbox-close {
    position: absolute;
    top: 20px;
    right: 20px;
    background: rgba(255, 255, 255, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.15);
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
    background: rgba(255, 255, 255, 0.2);
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

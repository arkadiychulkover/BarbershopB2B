<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest, BASE_URL } from '../lib/api';
  import { m } from '../lib/paraglide/messages.js';
  import { 
    UserPlus, 
    User, 
    Briefcase, 
    Send, 
    Trash2, 
    ShieldCheck, 
    AlertCircle,
    X,
    Users,
    Edit3,
    AtSign,
    Check,
    ExternalLink,
    Camera
  } from 'lucide-svelte';
  
  let masters = [];
  let isLoading = true;
  let errorMsg = '';
  
  let showAddForm = false;
  let newMaster = { name: '', description: '', telegramId: '', telegramUsername: '' };
  let isSaving = false;

  let editingMaster = null;
  let isUpdating = false;
  let isUploadingPhoto = false;

  async function handleMasterPhotoUpload(event) {
    const file = event.target.files?.[0];
    if (!file || !editingMaster) return;

    if (file.size > 30 * 1024 * 1024) {
      alert(m.masters_max_photo_size());
      return;
    }

    isUploadingPhoto = true;
    try {
      const formData = new FormData();
      formData.append('photo', file);

      const res = await apiRequest(`/api/Barber/upload-photo/${editingMaster.barberId}`, {
        method: 'POST',
        body: formData
      });

      if (res && res.photoUrl) {
        editingMaster.photoUrl = res.photoUrl;
        masters = masters.map(m => m.id === editingMaster.barberId ? { ...m, photoUrl: res.photoUrl } : m);
      }
    } catch (e) {
      alert(m.masters_photo_upload_error() + (e.message || ''));
    } finally {
      isUploadingPhoto = false;
    }
  }
  
  onMount(async () => {
    await loadMasters();
  });

  async function loadMasters() {
    isLoading = true;
    errorMsg = '';
    try {
      masters = await apiRequest('/api/Barber/all');
    } catch (e) {
      errorMsg = m.masters_load_error();
    } finally {
      isLoading = false;
    }
  }

  async function deleteMaster(id) {
    if (!confirm(m.masters_delete_confirm())) return;
    try {
      await apiRequest('/api/Barber/delete', {
        method: 'DELETE',
        body: JSON.stringify({ barberId: id })
      });
      masters = masters.filter(m => m.id !== id);
    } catch (e) {
      alert(m.masters_delete_error() + e.message);
    }
  }

  async function addMaster() {
    isSaving = true;
    try {
      const cleanUsername = newMaster.telegramUsername ? newMaster.telegramUsername.trim().replace(/^@/, '') : '';
      const payload = {
        name: newMaster.name.trim(),
        description: newMaster.description?.trim() || null,
        telegramId: newMaster.telegramId?.trim() || null,
        telegramUsername: cleanUsername || null
      };

      const res = await apiRequest('/api/Barber/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      
      masters = [
        ...masters, 
        { 
          id: res.barberId, 
          name: payload.name, 
          description: payload.description, 
          telegramId: payload.telegramId, 
          telegramUsername: payload.telegramUsername,
          isActive: true 
        }
      ];
      showAddForm = false;
      newMaster = { name: '', description: '', telegramId: '', telegramUsername: '' };
    } catch (e) {
      alert(m.masters_add_error() + (e.message || ''));
    } finally {
      isSaving = false;
    }
  }

  function startEdit(master) {
    editingMaster = {
      barberId: master.id,
      name: master.name || '',
      description: master.description || '',
      telegramId: master.telegramId || '',
      telegramUsername: master.telegramUsername ? master.telegramUsername.replace(/^@/, '') : '',
      photoUrl: master.photoUrl || null,
      isActive: master.isActive ?? true
    };
  }

  function cancelEdit() {
    editingMaster = null;
  }

  async function saveEdit() {
    if (!editingMaster) return;
    isUpdating = true;
    try {
      const cleanUsername = editingMaster.telegramUsername ? editingMaster.telegramUsername.trim().replace(/^@/, '') : '';
      const payload = {
        barberId: editingMaster.barberId,
        name: editingMaster.name.trim(),
        description: editingMaster.description?.trim() || null,
        telegramId: editingMaster.telegramId?.trim() || null,
        telegramUsername: cleanUsername || null,
        photoUrl: editingMaster.photoUrl,
        isActive: editingMaster.isActive
      };

      await apiRequest('/api/Barber/update', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      masters = masters.map(m => m.id === payload.barberId ? { ...m, ...payload, id: payload.barberId } : m);
      editingMaster = null;
    } catch (e) {
      alert(m.masters_update_error() + (e.message || ''));
    } finally {
      isUpdating = false;
    }
  }
</script>

<DashboardLayout>
  <div class="masters-page">
    <header class="page-header">
      <div class="header-left">
        <h1>{m.masters_heading()}</h1>
        <p class="header-subtitle">{m.masters_sub()}</p>
      </div>
      <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
        {#if showAddForm}
          <X size={18} />
          <span>{m.masters_hide_form()}</span>
        {:else}
          <UserPlus size={18} />
          <span>{m.masters_add_btn()}</span>
        {/if}
      </button>
    </header>

    {#if showAddForm}
      <div class="card add-card mb-4">
        <div class="card-head">
          <h3>{m.masters_new_title()}</h3>
          <p>{m.masters_new_desc()}</p>
        </div>

        <form on:submit|preventDefault={addMaster}>
          <div class="form-grid">
            <div class="form-group">
              <label for="masterName">{m.masters_name_label()}</label>
              <div class="input-icon-wrap">
                <User size={16} class="input-icon" />
                <input id="masterName" type="text" class="input has-icon" bind:value={newMaster.name} placeholder="Alex" required />
              </div>
            </div>

            <div class="form-group">
              <label for="masterDesc">{m.masters_desc_label()}</label>
              <div class="input-icon-wrap">
                <Briefcase size={16} class="input-icon" />
                <input id="masterDesc" type="text" class="input has-icon" bind:value={newMaster.description} placeholder="Master / Barber" />
              </div>
            </div>

            <div class="form-group">
              <label for="masterTg">{m.masters_tg_id_label()}</label>
              <div class="input-icon-wrap">
                <Send size={16} class="input-icon" />
                <input id="masterTg" type="text" class="input has-icon" bind:value={newMaster.telegramId} placeholder="123456789" required />
              </div>
              <span class="field-hint">Authorization in Telegram Mini App</span>
            </div>

            <div class="form-group">
              <label for="masterTgUsername">{m.masters_tg_user_label()}</label>
              <div class="input-icon-wrap">
                <AtSign size={16} class="input-icon" />
                <input id="masterTgUsername" type="text" class="input has-icon" bind:value={newMaster.telegramUsername} placeholder="username" />
              </div>
              <span class="field-hint">Clients can chat directly via «Contact Barber»</span>
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" disabled={isSaving || !newMaster.name || !newMaster.telegramId}>
              {isSaving ? m.common_loading() : m.common_save()}
            </button>
            <button type="button" class="btn btn-secondary" on:click={() => showAddForm = false}>
              {m.common_cancel()}
            </button>
          </div>
        </form>
      </div>
    {/if}

    {#if editingMaster}
      <div class="card add-card mb-4 edit-card">
        <div class="card-head">
          <div class="card-head-title">
            <Edit3 size={20} class="text-rose" />
            <h3>{m.masters_edit_title()} {editingMaster.name}</h3>
          </div>
          <p>{m.masters_edit_desc()}</p>
        </div>

        <form on:submit|preventDefault={saveEdit}>
          <div class="form-grid">
            <div class="form-group">
              <label for="editName">{m.masters_edit_name_label()}</label>
              <div class="input-icon-wrap">
                <User size={16} class="input-icon" />
                <input id="editName" type="text" class="input has-icon" bind:value={editingMaster.name} required />
              </div>
            </div>

            <div class="form-group">
              <label for="editDesc">{m.masters_edit_desc_label()}</label>
              <div class="input-icon-wrap">
                <Briefcase size={16} class="input-icon" />
                <input id="editDesc" type="text" class="input has-icon" bind:value={editingMaster.description} placeholder={m.masters_edit_desc_placeholder()} />
              </div>
            </div>

            <div class="form-group">
              <label for="editTgId">{m.masters_edit_tgid_label()}</label>
              <div class="input-icon-wrap">
                <Send size={16} class="input-icon" />
                <input id="editTgId" type="text" class="input has-icon" bind:value={editingMaster.telegramId} placeholder={m.masters_edit_tgid_placeholder()} />
              </div>
            </div>

            <div class="form-group">
              <label for="editTgUsername">{m.masters_edit_tguser_label()}</label>
              <div class="input-icon-wrap">
                <AtSign size={16} class="input-icon" />
                <input id="editTgUsername" type="text" class="input has-icon" bind:value={editingMaster.telegramUsername} placeholder="username" />
              </div>
              <span class="field-hint">{m.masters_edit_tguser_hint()}</span>
            </div>

            <div class="form-group full-width">
              <label for="editPhoto">{m.masters_edit_photo_label()}</label>
              <div class="photo-upload-row">
                {#if editingMaster.photoUrl}
                  <img src={editingMaster.photoUrl.startsWith('http') ? editingMaster.photoUrl : `${BASE_URL}${editingMaster.photoUrl}`} alt={m.masters_edit_photo_alt()} class="edit-preview-avatar" />
                {/if}
                <input type="file" id="editPhoto" accept="image/*" on:change={handleMasterPhotoUpload} class="file-input" disabled={isUploadingPhoto} />
                {#if isUploadingPhoto}
                  <span class="uploading-text">{m.masters_edit_uploading()}</span>
                {/if}
              </div>
            </div>

            <div class="form-group full-width">
              <label class="checkbox-label">
                <input type="checkbox" bind:checked={editingMaster.isActive} />
                <span>{m.masters_edit_active_label()}</span>
              </label>
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" disabled={isUpdating || !editingMaster.name}>
              {isUpdating ? m.common_loading() : m.common_save()}
            </button>
            <button type="button" class="btn btn-secondary" on:click={cancelEdit}>
              {m.common_cancel()}
            </button>
          </div>
        </form>
      </div>
    {/if}

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>{m.common_loading()}</p>
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
        <h3>{m.masters_empty_title()}</h3>
        <p>{m.masters_empty_desc()}</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>
          <UserPlus size={17} />
          <span>{m.masters_add_first_btn()}</span>
        </button>
      </div>
    {:else}
      <div class="grid">
        {#each masters as master}
          <div class="card master-card">
            <div class="master-top">
              <div class="master-avatar">
                {#if master.photoUrl}
                  <img src={master.photoUrl.startsWith('http') ? master.photoUrl : `${BASE_URL}${master.photoUrl}`} alt={master.name} class="master-avatar-img" />
                {:else}
                  {(master.name || 'M')[0].toUpperCase()}
                {/if}
              </div>
              <div class="master-details">
                <h3>{master.name}</h3>
                <span class="master-role">{master.description || 'Master'}</span>
              </div>
              <span class="status-badge" class:active={master.isActive}>
                {master.isActive ? m.masters_status_active() : m.masters_status_inactive()}
              </span>
            </div>

            <div class="master-meta">
              {#if master.telegramUsername}
                <div class="meta-row highlight-meta">
                  <AtSign size={14} class="meta-icon text-rose" />
                  <span>Username: <strong>@{master.telegramUsername.replace(/^@/, '')}</strong></span>
                  <a 
                    href={`https://t.me/${master.telegramUsername.replace(/^@/, '')}`} 
                    target="_blank" 
                    rel="noopener noreferrer"
                    class="tg-ext-link"
                    title="Open in Telegram"
                  >
                    <ExternalLink size={12} />
                  </a>
                </div>
              {:else}
                <div class="meta-row empty-meta">
                  <AtSign size={14} class="meta-icon text-muted" />
                  <span>Username: <em>—</em></span>
                </div>
              {/if}

              {#if master.telegramId}
                <div class="meta-row">
                  <Send size={14} class="meta-icon" />
                  <span>TG ID: <strong>{master.telegramId}</strong></span>
                </div>
              {/if}
            </div>

            <div class="master-footer">
              <button class="btn btn-secondary btn-sm" on:click={() => startEdit(master)}>
                <Edit3 size={14} />
                <span>{m.common_edit()}</span>
              </button>
              <button class="btn btn-danger btn-sm" on:click={() => deleteMaster(master.id)}>
                <Trash2 size={14} />
                <span>{m.common_delete()}</span>
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

  .edit-card {
    border-color: rgba(223, 158, 142, 0.35);
    background: linear-gradient(180deg, rgba(28, 32, 44, 0.9) 0%, rgba(20, 24, 34, 0.85) 100%);
    box-shadow: 0 12px 36px rgba(0, 0, 0, 0.35), 0 0 20px rgba(223, 158, 142, 0.1);
  }

  .card-head {
    margin-bottom: 1.5rem;
  }

  .card-head-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
  }

  .card-head h3 {
    font-size: 1.3rem;
    margin: 0;
  }

  .card-head p {
    color: var(--text-secondary);
    font-size: 0.9rem;
    margin-top: 0.25rem;
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

  .field-hint {
    font-size: 0.75rem;
    color: var(--text-muted);
  }

  .checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    cursor: pointer;
    font-size: 0.9rem;
    color: var(--text-primary);
    padding: 0.5rem 0;
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
    overflow: hidden;
  }

  .master-avatar-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border-radius: 50%;
  }

  .photo-upload-row {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .edit-preview-avatar {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    object-fit: cover;
    border: 2px solid var(--pastel-rose);
  }

  .file-input {
    font-size: 0.88rem;
    color: var(--text-secondary);
  }

  .uploading-text {
    font-size: 0.85rem;
    color: var(--pastel-rose);
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
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
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

  .highlight-meta {
    color: var(--text-primary);
  }

  .highlight-meta strong {
    color: var(--pastel-rose);
  }

  .empty-meta {
    color: var(--text-muted);
  }

  .tg-ext-link {
    color: var(--pastel-rose);
    display: inline-flex;
    align-items: center;
    margin-left: auto;
    opacity: 0.7;
    transition: opacity 0.2s ease;
  }

  .tg-ext-link:hover {
    opacity: 1;
  }

  :global(.meta-icon) {
    color: var(--pastel-lavender);
    flex-shrink: 0;
  }

  :global(.text-rose) {
    color: var(--pastel-rose) !important;
  }

  :global(.text-muted) {
    color: var(--text-muted) !important;
  }

  .master-footer {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    margin-top: auto;
  }

  .btn-sm {
    padding: 0.5rem 1rem;
    font-size: 0.85rem;
  }
</style>

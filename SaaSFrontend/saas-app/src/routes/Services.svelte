<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { m } from '../lib/paraglide/messages.js';
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
      errorMsg = m.services_load_error();
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
      alert(m.services_save_error() + ' ' + (e.message || ''));
    } finally {
      isSaving = false;
    }
  }

  async function deleteService(id) {
    if (!confirm(m.services_delete_confirm())) return;
    try {
      await apiRequest(`/api/ServiceNames/${id}`, {
        method: 'DELETE'
      });
      services = services.filter(s => s.id !== id);
    } catch (e) {
      alert(m.services_delete_error() + ' ' + e.message);
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
      alert(m.services_save_error() + ' ' + e.message);
    }
  }
</script>

<DashboardLayout>
  <div class="services-page">
    <header class="page-header">
      <div class="header-left">
        <h1>{m.services_catalog_title()}</h1>
        <p class="header-subtitle">{m.services_catalog_subtitle()}</p>
      </div>
      <button class="btn btn-primary" on:click={() => showAddForm = !showAddForm}>
        {#if showAddForm}
          <X size={18} />
          <span>{m.services_hide_form()}</span>
        {:else}
          <Plus size={18} />
          <span>{m.services_add_btn()}</span>
        {/if}
      </button>
    </header>

    {#if showAddForm}
      <div class="card add-card mb-4">
        <div class="card-head">
          <h3>{m.services_create_title()}</h3>
          <p>{m.services_create_desc()}</p>
        </div>

        <form on:submit|preventDefault={addService}>
          <div class="form-group">
            <label for="newService">{m.services_name_label()}</label>
            <div class="input-icon-wrap">
              <Scissors size={16} class="input-icon" />
              <input 
                id="newService" 
                type="text" 
                class="input has-icon" 
                bind:value={newServiceName} 
                placeholder={m.services_name_placeholder()} 
                required 
              />
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" disabled={isSaving || !newServiceName.trim()}>
              {isSaving ? m.common_loading() : m.services_create_submit()}
            </button>
            <button type="button" class="btn btn-secondary" on:click={() => showAddForm = false}>
              {m.common_cancel()}
            </button>
          </div>
        </form>
      </div>
    {/if}

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>{m.services_catalog_loading()}</p>
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
        <h3>{m.services_empty_title()}</h3>
        <p>{m.services_empty_desc()}</p>
        <button class="btn btn-primary mt-2" on:click={() => showAddForm = true}>
          <Plus size={17} />
          <span>{m.services_create_first()}</span>
        </button>
      </div>
    {:else}
      <div class="grid">
        {#each services as service}
          <div class="card service-card">
            {#if editingServiceId === service.id}
              <div class="edit-mode">
                <label for="edit-service-input" class="edit-label">{m.services_edit_title()}</label>
                <input id="edit-service-input" type="text" class="input" bind:value={editingServiceName} />
                <div class="actions mt-3">
                  <button class="btn btn-primary btn-sm" on:click={saveEdit}>
                    <Check size={15} />
                    <span>{m.common_save()}</span>
                  </button>
                  <button class="btn btn-secondary btn-sm" on:click={cancelEdit}>
                    <span>{m.common_cancel()}</span>
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
                    <span class="service-status">{m.services_in_catalog()}</span>
                  </div>
                </div>

                <div class="actions">
                  <button class="btn btn-secondary btn-sm" on:click={() => startEdit(service)}>
                    <Edit2 size={14} />
                    <span>{m.common_edit()}</span>
                  </button>
                  <button class="btn btn-danger btn-sm" on:click={() => deleteService(service.id)}>
                    <Trash2 size={14} />
                    <span>{m.common_delete()}</span>
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
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 1.5rem;
  }

  .service-card {
    display: flex;
    flex-direction: column;
    padding: 1.5rem;
    gap: 1.25rem;
  }

  .view-mode, .edit-mode {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    flex: 1;
  }

  .service-top {
    display: flex;
    align-items: center;
    gap: 1rem;
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
    align-items: center;
    flex-wrap: wrap;
    margin-top: auto;
  }

  .btn-sm {
    padding: 0.45rem 0.9rem;
    font-size: 0.85rem;
  }

  .mt-3 {
    margin-top: 1rem;
  }

  @media (max-width: 768px) {
    .page-header {
      flex-direction: column;
      align-items: stretch;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .page-header .btn {
      width: 100%;
    }

    .header-left h1 {
      font-size: 1.65rem;
    }

    .add-card {
      padding: 1.25rem 1rem;
    }

    .grid {
      grid-template-columns: 1fr;
      gap: 1rem;
    }

    .service-card {
      padding: 1.25rem 1rem;
    }

    .form-actions {
      flex-direction: column;
      gap: 0.5rem;
    }

    .form-actions .btn {
      width: 100%;
    }
  }

  @media (max-width: 480px) {
    .header-left h1 {
      font-size: 1.4rem;
    }
  }
</style>

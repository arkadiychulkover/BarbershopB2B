<script lang="ts">
  import { onMount, createEventDispatcher } from 'svelte';
  import { apiFetch } from '../api';
  import { showAlert, showConfirm, hapticSuccess, hapticError, hapticWarning } from '../telegram';
  import SecureImage from './SecureImage.svelte';
  import Icon from './Icon.svelte';

  const dispatch = createEventDispatcher();

  let innerWidth = 0;
  $: isDesktop = innerWidth >= 768;

  let currentDate = new Date();
  let currentWeekStart = getMonday(new Date());
  
  let appointments = [];
  let services = [];
  let loading = true;
  
  let view: 'list' | 'form' = 'list';
  let editingAppt = null;

  // expanded card id (mobile list)
  let expandedId: string | null = null;

  // photo upload state per appointment
  let uploadingId: string | null = null;
  let photoInputEl: HTMLInputElement;
  let pendingPhotoApptId: string | null = null;
  
  // form state
  let formDateStr = '';
  let formTime = '';
  let formServiceId = '';
  let formStatus = 0;
  
  $: dateString = new Date(currentDate.getTime() - currentDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
  
  function getMonday(d) {
    d = new Date(d);
    var day = d.getDay(),
        diff = d.getDate() - day + (day == 0 ? -6: 1);
    return new Date(d.setDate(diff));
  }
  
  function getDaysOfWeek(startDate) {
    let days = [];
    for(let i=0; i<7; i++) {
        let d = new Date(startDate);
        d.setDate(d.getDate() + i);
        days.push(d);
    }
    return days;
  }
  
  $: days = getDaysOfWeek(currentWeekStart);

  onMount(async () => {
    await loadData();
  });
  
  async function loadData() {
    loading = true;
    try {
      const [apptsData, servicesData] = await Promise.all([
        apiFetch('/api/Barber/get-master-appointments'),
        apiFetch('/api/Barber/my-services')
      ]);
      appointments = apptsData;
      services = servicesData.filter(s => s.serviceId && s.isActive);
    } catch (error) {
      console.error(error);
      showAlert('Ошибка при загрузке данных');
    } finally {
      loading = false;
    }
  }
  
  function prevDay() {
    let d = new Date(currentDate);
    d.setDate(d.getDate() - 1);
    currentDate = d;
  }
  
  function nextDay() {
    let d = new Date(currentDate);
    d.setDate(d.getDate() + 1);
    currentDate = d;
  }

  function prevWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() - 7);
    currentWeekStart = d;
  }
  
  function nextWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() + 7);
    currentWeekStart = d;
  }
  
  function getApptsForDayDate(date) {
    const dStr = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    return appointments.filter(a => {
        const apptDate = new Date(a.appointmentDate);
        const apptDateStr = new Date(apptDate.getTime() - apptDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
        return apptDateStr === dStr;
    }).sort((a,b) => a.appointmentDate.localeCompare(b.appointmentDate));
  }

  $: currentDayAppts = getApptsForDayDate(currentDate);

  function toggleExpand(apptId: string) {
    expandedId = expandedId === apptId ? null : apptId;
  }
    
  function openNewForm(dateToUse) {
    expandedId = null;
    editingAppt = null;
    formDateStr = new Date(dateToUse.getTime() - dateToUse.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = '10:00';
    formServiceId = services.length > 0 ? services[0].serviceId : '';
    formStatus = 0;
    view = 'form';
  }
  
  function openEditForm(appt) {
    expandedId = null;
    editingAppt = appt;
    const d = new Date(appt.appointmentDate);
    formDateStr = new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = d.toTimeString().substring(0,5);
    formServiceId = appt.serviceId || (services.length > 0 ? services[0].serviceId : '');
    formStatus = appt.status;
    view = 'form';
  }
  
  function cancelForm() {
    view = 'list';
    editingAppt = null;
  }
  
  async function saveAppt() {
    if (!formServiceId) {
        showAlert('Выберите услугу!');
        return;
    }
    
    const appointmentDate = new Date(`${formDateStr}T${formTime}:00`).toISOString();
    
    const body = {
      serviceId: formServiceId,
      appointmentDate: appointmentDate,
      status: parseInt(formStatus)
    };
    
    try {
      if (editingAppt) {
        await apiFetch(`/api/Barber/my-appointments/update/${editingAppt.id}`, {
          method: 'PUT',
          body
        });
      } else {
        await apiFetch('/api/Barber/my-appointments/add', {
          method: 'POST',
          body
        });
      }
      hapticSuccess();
      view = 'list';
      await loadData();
    } catch(e) {
      hapticError();
      if (e.status === 409) {
          showAlert('Это время пересекается с другой записью!');
      } else {
          showAlert("Ошибка: " + e.message);
      }
    }
  }
  
  function deleteAppt() {
    hapticWarning();
    showConfirm('Вы уверены, что хотите удалить эту запись?', async (confirmed) => {
      if (confirmed && editingAppt) {
        try {
          await apiFetch(`/api/Barber/my-appointments/delete/${editingAppt.id}`, {
            method: 'DELETE'
          });
          hapticSuccess();
          view = 'list';
          await loadData();
        } catch (error) {
          hapticError();
          showAlert('Не удалось удалить запись');
        }
      }
    });
  }

  function triggerPhotoUpload(apptId: string) {
    pendingPhotoApptId = apptId;
    photoInputEl.value = '';
    photoInputEl.click();
  }

  async function handlePhotoSelected(event) {
    const file = event.target.files?.[0];
    if (!file || !pendingPhotoApptId) return;

    uploadingId = pendingPhotoApptId;
    try {
      const formData = new FormData();
      formData.append('photo', file);

      const { authStore } = await import('../stores/auth');
      let token = null;
      const unsub = authStore.subscribe(s => { token = s.token; });
      unsub();

      const res = await fetch(`/api/Barber/put-photo/${pendingPhotoApptId}`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData
      });

      if (!res.ok) throw new Error(await res.text());

      hapticSuccess();
      await loadData();
      // keep card expanded after upload
      expandedId = pendingPhotoApptId;
    } catch(e) {
      hapticError();
      showAlert('Ошибка загрузки фото: ' + (e.message || 'неизвестная ошибка'));
    } finally {
      uploadingId = null;
      pendingPhotoApptId = null;
    }
  }

  function statusLabel(status: number): string {
    if (status === 1) return 'Выполнено';
    if (status === 2) return 'Отменено';
    return 'Запланировано';
  }

  // --- Comment Logic ---
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
      hapticSuccess();
    } catch (e) {
      console.error(e);
      hapticError();
      showAlert('Ошибка при сохранении комментария');
    }
  }

  async function deleteComment(apptId: string) {
    showConfirm('Удалить комментарий?', async (confirmed) => {
      if (!confirmed) return;
      try {
        await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
          method: 'DELETE'
        });
        const index = appointments.findIndex(a => a.id === apptId);
        if (index !== -1) {
          appointments[index].resultNote = null;
          appointments = [...appointments];
        }
        hapticSuccess();
      } catch (e) {
        console.error(e);
        hapticError();
        showAlert('Ошибка при удалении комментария');
      }
    });
  }
</script>

<svelte:window bind:innerWidth />

<div class="appointments-container" class:desktop-mode={isDesktop}>
  {#if view === 'list'}
    
    {#if isDesktop}
      <div class="desktop-header">
        <div class="week-nav">
          <button class="nav-btn" on:click={prevWeek}>&larr;</button>
          <span class="week-label">
            {days[0].toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})} - 
            {days[6].toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})}
          </span>
          <button class="nav-btn" on:click={nextWeek}>&rarr;</button>
        </div>
        <button class="primary-btn" on:click={() => openNewForm(new Date())}>Новая запись</button>
      </div>

      {#if loading}
        <div class="loading">Загрузка расписания...</div>
      {:else}
        <div class="calendar-grid">
          {#each days as day}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="day-col" on:click={() => openNewForm(day)}>
              <div class="day-header">
                <div class="day-name">{day.toLocaleDateString('ru-RU', {weekday: 'short'})}</div>
                <div class="day-date">{day.toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})}</div>
              </div>
              <div class="day-body">
                {#each getApptsForDayDate(day) as appt}
                  <!-- svelte-ignore a11y-click-events-have-key-events -->
                  <!-- svelte-ignore a11y-no-static-element-interactions -->
                  <div
                    class="appt-card mobile compact status-{appt.status}"
                    class:expanded={expandedId === appt.id}
                    on:click|stopPropagation={() => toggleExpand(appt.id)}
                  >
                    <div class="card-main-row">
                      <div class="appt-time">
                        {new Date(appt.appointmentDate).toLocaleTimeString('ru-RU', {hour: '2-digit', minute: '2-digit'})}
                      </div>
                      <div class="appt-details">
                        <div class="service-name">{appt.serviceName || services.find(s => s.serviceId === appt.serviceId)?.name || 'Услуга'}</div>
                        <div class="client-name">
                          {#if appt.clientName && appt.clientTelegramId !== 'WALKIN'}
                            {appt.clientName}
                          {:else}
                            Гость (Вручную)
                          {/if}
                        </div>
                      </div>
                      <div class="card-right">
                        <div class="status-icon">
                          {#if appt.status === 1}
                            <Icon name="check" size={14} color="var(--pastel-sage)" />
                          {:else if appt.status === 2}
                            <Icon name="x" size={14} color="var(--pastel-coral)" />
                          {/if}
                        </div>
                        <div class="expand-arrow" class:open={expandedId === appt.id}>
                          <Icon name="chevron-right" size={14} />
                        </div>
                      </div>
                    </div>

                    {#if expandedId === appt.id}
                      <div class="card-expanded" on:click|stopPropagation>
                        <div class="expand-divider"></div>
                        <div class="expand-meta">
                          <span class="meta-badge status-badge-{appt.status}">
                            {statusLabel(appt.status)}
                          </span>
                          <button class="edit-link" on:click={() => openEditForm(appt)}>
                            <Icon name="edit" size={13} />
                            <span>Редактировать</span>
                          </button>
                        </div>
                        {#if appt.clientTelegramId && appt.clientTelegramId !== 'WALKIN'}
                          <div class="client-tg-row">
                            <span class="client-tg-label">Клиент:</span>
                            <!-- svelte-ignore a11y-click-events-have-key-events -->
                            <!-- svelte-ignore a11y-no-static-element-interactions -->
                            <span
                              class="client-tg-id"
                              on:click={() => dispatch('openClientHistory', appt.clientId)}
                            >
                              <Icon name="user" size={13} color="var(--pastel-lavender)" />
                              <span>{appt.clientName || appt.clientTelegramId} · @{appt.clientTelegramId}</span>
                              <Icon name="chevron-right" size={12} />
                            </span>
                          </div>
                        {/if}
                        {#if appt.photoResultUrl}
                          <div class="photo-preview-wrap">
                            <SecureImage src={appt.photoResultUrl} alt="Результат" className="photo-preview" style="width:100%;max-height:200px;object-fit:cover;border-radius:8px;" />
                            <button class="change-photo-btn" on:click={() => triggerPhotoUpload(appt.id)} disabled={uploadingId === appt.id}>
                              {#if uploadingId === appt.id}
                                <span class="spinner"></span> Загрузка...
                              {:else}
                                <Icon name="refresh" size={13} />
                                <span>заменить фото</span>
                              {/if}
                            </button>
                          </div>
                        {:else}
                          <button class="attach-photo-btn" on:click={() => triggerPhotoUpload(appt.id)} disabled={uploadingId === appt.id}>
                            {#if uploadingId === appt.id}
                              <span class="spinner"></span> Загрузка...
                            {:else}
                              <Icon name="paperclip" size={15} />
                              <span>Прикрепить результат</span>
                            {/if}
                          </button>
                        {/if}
                        <!-- Comment Section -->
                        <div class="appt-comment-section">
                          {#if editingCommentId === appt.id}
                            <textarea class="comment-input" bind:value={commentText} placeholder="Комментарий / Заметка..." on:click|stopPropagation></textarea>
                            <div class="comment-actions" on:click|stopPropagation>
                              <button class="btn-save" on:click={() => saveComment(appt.id)}>Сохранить</button>
                              <button class="btn-cancel" on:click={cancelCommentEdit}>Отмена</button>
                            </div>
                          {:else if appt.resultNote}
                            <div class="comment-display" on:click|stopPropagation>
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
                            <button class="btn-add-comment" on:click|stopPropagation={() => openCommentEdit(appt)}>+ Добавить комментарий</button>
                          {/if}
                        </div>
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
            </div>
          {/each}
        </div>
      {/if}

    {:else}
      <!-- Mobile Layout -->
      <div class="date-selector">
        <button class="nav-btn" on:click={prevDay}>&larr;</button>
        <div class="current-date">
          {currentDate.toLocaleDateString('ru-RU', {weekday: 'long', day: 'numeric', month: 'long'})}
        </div>
        <button class="nav-btn" on:click={nextDay}>&rarr;</button>
      </div>

      {#if loading}
        <div class="loading">Загрузка расписания...</div>
      {:else if currentDayAppts.length === 0}
        <div class="empty-state">
          <div class="icon">
            <Icon name="empty-calendar" size={44} color="var(--pastel-rose)" />
          </div>
          <p>На этот день записей нет.</p>
          <button class="primary-btn" on:click={() => openNewForm(currentDate)}>Добавить запись</button>
        </div>
      {:else}
        <!-- hidden photo file input -->
      <input
        type="file"
        accept="image/*"
        style="display:none"
        bind:this={photoInputEl}
        on:change={handlePhotoSelected}
      />

      <div class="appointments-list">
          {#each currentDayAppts as appt}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div
              class="appt-card mobile status-{appt.status}"
              class:expanded={expandedId === appt.id}
              on:click={() => toggleExpand(appt.id)}
            >
              <div class="card-main-row">
                <div class="appt-time">
                  {new Date(appt.appointmentDate).toLocaleTimeString('ru-RU', {hour: '2-digit', minute: '2-digit'})}
                </div>
                <div class="appt-details">
                  <div class="service-name">{appt.serviceName || services.find(s => s.serviceId === appt.serviceId)?.name || 'Услуга'}</div>
                  <div class="client-name">
                    {#if appt.clientName && appt.clientTelegramId !== 'WALKIN'}
                      {appt.clientName}
                    {:else}
                      Гость (Вручную)
                    {/if}
                  </div>
                </div>
                <div class="card-right">
                  <div class="status-icon">
                    {#if appt.status === 1}
                      <Icon name="check" size={16} color="var(--pastel-sage)" />
                    {:else if appt.status === 2}
                      <Icon name="x" size={16} color="var(--pastel-coral)" />
                    {/if}
                  </div>
                  <div class="expand-arrow" class:open={expandedId === appt.id}>
                    <Icon name="chevron-right" size={16} />
                  </div>
                </div>
              </div>

              {#if expandedId === appt.id}
                <div class="card-expanded" on:click|stopPropagation>
                  <div class="expand-divider"></div>

                  <div class="expand-meta">
                    <span class="meta-badge status-badge-{appt.status}">
                      {statusLabel(appt.status)}
                    </span>
                    <button class="edit-link" on:click={() => openEditForm(appt)}>
                      <Icon name="edit" size={13} />
                      <span>Редактировать</span>
                    </button>
                  </div>

                  {#if appt.clientTelegramId && appt.clientTelegramId !== 'WALKIN'}
                    <div class="client-tg-row">
                      <span class="client-tg-label">Клиент:</span>
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <span
                        class="client-tg-id"
                        on:click={() => dispatch('openClientHistory', appt.clientId)}
                      >
                        <Icon name="user" size={14} color="var(--pastel-lavender)" />
                        <span>{appt.clientName || appt.clientTelegramId} · @{appt.clientTelegramId}</span>
                        <Icon name="chevron-right" size={12} />
                      </span>
                    </div>
                  {/if}

                  {#if appt.photoResultUrl}
                    <div class="photo-preview-wrap">
                      <SecureImage
                        src={appt.photoResultUrl}
                        alt="Результат"
                        className="photo-preview"
                        style="width:100%;max-height:200px;object-fit:cover;border-radius:8px;"
                      />
                      <button
                        class="change-photo-btn"
                        on:click={() => triggerPhotoUpload(appt.id)}
                        disabled={uploadingId === appt.id}
                      >
                        {#if uploadingId === appt.id}
                          <span class="spinner"></span> Загрузка...
                        {:else}
                          <Icon name="refresh" size={13} />
                          <span>заменить фото</span>
                        {/if}
                      </button>
                    </div>
                  {:else}
                    <button
                      class="attach-photo-btn"
                      on:click={() => triggerPhotoUpload(appt.id)}
                      disabled={uploadingId === appt.id}
                    >
                      {#if uploadingId === appt.id}
                        <span class="spinner"></span> Загрузка...
                      {:else}
                        <Icon name="paperclip" size={15} />
                        <span>Прикрепить результат</span>
                      {/if}
                    </button>
                  {/if}

                  <!-- Comment Section -->
                  <div class="appt-comment-section">
                    {#if editingCommentId === appt.id}
                      <textarea class="comment-input" bind:value={commentText} placeholder="Комментарий / Заметка..." on:click|stopPropagation></textarea>
                      <div class="comment-actions" on:click|stopPropagation>
                        <button class="btn-save" on:click={() => saveComment(appt.id)}>Сохранить</button>
                        <button class="btn-cancel" on:click={cancelCommentEdit}>Отмена</button>
                      </div>
                    {:else if appt.resultNote}
                      <div class="comment-display" on:click|stopPropagation>
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
                      <button class="btn-add-comment" on:click|stopPropagation={() => openCommentEdit(appt)}>+ Добавить комментарий</button>
                    {/if}
                  </div>
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
      
      {#if !loading}
        <button class="add-btn" on:click={() => openNewForm(currentDate)}>+</button>
      {/if}
    {/if}
    
  {:else}
    <div class="form-container card">
      <h2>{editingAppt ? 'Изменение записи' : 'Новая запись'}</h2>
      
      <div class="form-group">
        <label>Дата</label>
        <input type="date" class="input" bind:value={formDateStr} />
      </div>

      <div class="form-group">
        <label>Время</label>
        <input type="time" class="input" bind:value={formTime} />
      </div>
      
      <div class="form-group">
        <label>Услуга</label>
        <select class="input" bind:value={formServiceId}>
          {#each services as s}
            <option value={s.serviceId}>{s.name} ({s.price} ₴)</option>
          {/each}
        </select>
      </div>
      
      <div class="form-group">
        <label>Статус</label>
        <select class="input" bind:value={formStatus}>
          <option value="0">Запланировано</option>
          <option value="1">Выполнено</option>
          <option value="2">Отменено</option>
        </select>
      </div>
      
      <div class="form-actions">
        <button class="primary-btn flex-1" on:click={saveAppt}>Сохранить</button>
        {#if editingAppt}
          <button class="danger-btn flex-1" on:click={deleteAppt}>Удалить</button>
        {/if}
        <button class="secondary-btn flex-1" on:click={cancelForm}>Отмена</button>
      </div>
    </div>
  {/if}
</div>

<style>
  .appointments-container {
    height: 100%;
  }
  
  .desktop-mode {
    padding-bottom: 30px;
  }

  /* Date Selector Header */
  .date-selector {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    padding: 12px 16px;
    border-radius: var(--radius-pill);
    margin-bottom: 16px;
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
  }
  
  .nav-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    width: 38px;
    height: 38px;
    border-radius: 50%;
    font-size: 16px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s var(--ease-spring);
  }

  .nav-btn:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
    color: var(--pastel-rose);
  }

  .nav-btn:active {
    transform: scale(0.92);
  }
  
  .current-date {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
    text-transform: capitalize;
    letter-spacing: 0.02em;
  }
  
  .appointments-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  
  /* Appointment Cards */
  .appt-card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-glass);
    border: 1px solid var(--border-subtle);
    border-left: 4px solid var(--pastel-amber);
    cursor: pointer;
    transition: all 0.25s var(--ease-spring);
    overflow: hidden;
  }

  .appt-card:hover {
    border-color: var(--border-glass);
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.45);
  }

  .appt-card.mobile {
    padding: 0;
  }

  .appt-card.mobile.expanded {
    border-color: rgba(223, 158, 142, 0.35);
    box-shadow: 0 12px 36px rgba(0, 0, 0, 0.55), 0 0 20px var(--pastel-rose-dim);
  }

  .appt-card.compact {
    padding: 0;
    margin-bottom: 8px;
    border-radius: var(--radius-md);
  }

  .appt-card.compact .card-main-row {
    padding: 10px 12px;
    gap: 10px;
    align-items: center;
  }
  
  .appt-card.status-0 {
    border-left-color: var(--pastel-amber);
  }
  .appt-card.status-1 {
    border-left-color: var(--pastel-sage);
  }
  .appt-card.status-2 {
    border-left-color: var(--pastel-coral);
    opacity: 0.65;
  }

  /* Card Main Row */
  .card-main-row {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 16px;
  }

  .card-right {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
  }

  .expand-arrow {
    font-size: 20px;
    color: var(--text-muted);
    transition: transform 0.25s var(--ease-spring), color 0.2s;
    line-height: 1;
    user-select: none;
  }

  .expand-arrow.open {
    transform: rotate(90deg);
    color: var(--pastel-rose);
  }
  
  /* Expanded Section */
  .card-expanded {
    padding: 0 16px 16px;
    animation: fadeIn 0.25s var(--ease-spring);
  }

  .expand-divider {
    height: 1px;
    background: var(--border-subtle);
    margin-bottom: 14px;
  }

  .expand-meta {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
  }

  .meta-badge {
    font-size: 12px;
    font-weight: 600;
    padding: 4px 12px;
    border-radius: var(--radius-pill);
    letter-spacing: 0.02em;
  }
  .status-badge-0 { background: var(--pastel-amber-dim); color: var(--pastel-amber); border: 1px solid rgba(229, 190, 138, 0.25); }
  .status-badge-1 { background: var(--pastel-sage-dim);  color: var(--pastel-sage); border: 1px solid rgba(152, 193, 169, 0.25); }
  .status-badge-2 { background: var(--pastel-coral-dim); color: var(--pastel-coral); border: 1px solid rgba(232, 130, 130, 0.25); }

  .edit-link {
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    transition: opacity 0.2s;
  }
  .edit-link:hover { opacity: 0.8; }

  .client-tg-row {
    display: flex;
    align-items: center;
    gap: 8px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 10px 14px;
    margin-bottom: 12px;
  }

  .client-tg-label {
    font-size: 13px;
    color: var(--text-muted);
    flex-shrink: 0;
  }

  .client-tg-id {
    font-size: 13px;
    font-weight: 600;
    color: var(--pastel-lavender);
    cursor: pointer;
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .client-tg-id:hover {
    color: var(--pastel-rose);
    text-decoration: underline;
  }

  .attach-photo-btn {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 12px;
    border-radius: var(--radius-md);
    border: 1.5px dashed rgba(223, 158, 142, 0.4);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
  }
  .attach-photo-btn:hover {
    background: rgba(223, 158, 142, 0.22);
    border-color: var(--pastel-rose);
  }
  .attach-photo-btn:disabled {
    opacity: 0.5;
    cursor: default;
  }

  .photo-preview-wrap {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .photo-preview {
    width: 100%;
    max-height: 200px;
    object-fit: cover;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
  }
  .change-photo-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
    padding: 8px;
    font-size: 13px;
    color: var(--text-secondary);
    cursor: pointer;
    transition: all 0.2s;
  }
  .change-photo-btn:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
  }
  .change-photo-btn:disabled { opacity: 0.5; cursor: default; }

  /* Comment UI */
  .appt-comment-section {
    margin-top: 14px;
  }
  .comment-input {
    width: 100%;
    min-height: 70px;
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
    padding: 2px 0;
    display: flex;
    align-items: center;
    transition: color 0.2s;
  }
  .btn-add-comment:hover {
    color: var(--pastel-rose);
  }
  
  .appt-time {
    font-weight: 700;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
  }
  .mobile .card-main-row .appt-time {
    font-size: 18px;
    min-width: 60px;
  }
  .compact .appt-time {
    font-size: 14px;
    min-width: 44px;
  }
  
  .appt-details {
    flex: 1;
  }
  
  .service-name {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
  }
  .compact .service-name {
    font-size: 13px;
    line-height: 1.3;
  }
  
  .client-name {
    font-size: 13px;
    color: var(--text-secondary);
    margin-top: 3px;
  }
  .compact .client-name {
    font-size: 12px;
  }
  
  .status-icon {
    font-size: 16px;
    color: var(--text-muted);
  }
  .compact .status-icon {
    font-size: 13px;
  }
  .appt-card.status-0 .status-icon { color: var(--pastel-amber); }
  .appt-card.status-1 .status-icon { color: var(--pastel-sage); }
  .appt-card.status-2 .status-icon { color: var(--pastel-coral); }
  
  .add-btn {
    position: fixed;
    bottom: 24px;
    right: 20px;
    width: 54px;
    height: 54px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    font-size: 28px;
    line-height: 1;
    box-shadow: 0 8px 24px var(--pastel-rose-glow);
    cursor: pointer;
    z-index: 90;
    transition: all 0.25s var(--ease-spring);
  }
  .add-btn:hover {
    transform: scale(1.06) translateY(-2px);
  }
  .add-btn:active {
    transform: scale(0.94);
  }
  
  /* Form Container */
  .card {
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    padding: 24px;
    box-shadow: var(--shadow-glass);
    max-width: 520px;
    margin: 0 auto;
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .card h2 {
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 20px;
  }
  
  .form-group {
    margin-bottom: 18px;
  }
  
  .form-group label {
    display: block;
    font-size: 13px;
    font-weight: 500;
    color: var(--text-secondary);
    margin-bottom: 6px;
  }
  
  .input {
    width: 100%;
    box-sizing: border-box;
    padding: 12px 16px;
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    font-size: 15px;
    background: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-family: var(--font-family);
    transition: border-color 0.2s;
  }

  .input:focus {
    outline: none;
    border-color: var(--border-active);
  }

  select.input {
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23a3a9bf' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 14px center;
  }
  
  .form-actions {
    display: flex;
    flex-direction: column;
    gap: 10px;
    margin-top: 28px;
  }
  
  .flex-1 { flex: 1; }
  
  .primary-btn {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }
  .primary-btn:active { transform: scale(0.96); }
  
  .secondary-btn {
    background: var(--bg-surface-elevated);
    color: var(--text-secondary);
    border: 1px solid var(--border-subtle);
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .secondary-btn:hover { color: var(--text-primary); border-color: var(--border-glass); }
  .secondary-btn:active { transform: scale(0.96); }
  
  .danger-btn {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.2);
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .danger-btn:active { transform: scale(0.96); }
  
  .loading, .empty-state {
    text-align: center;
    padding: 50px 20px;
    color: var(--text-secondary);
  }
  .empty-state .icon {
    font-size: 44px;
    margin-bottom: 14px;
  }

  /* Desktop Grid Styles */
  .desktop-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding: 0 4px;
  }
  
  .week-nav {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  
  .week-label {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
  }
  
  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 8px;
    padding: 2px;
  }
  
  .day-col {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    display: flex;
    flex-direction: column;
    min-height: 440px;
    cursor: pointer;
    overflow: hidden;
    transition: border-color 0.2s;
  }

  .day-col:hover {
    border-color: var(--border-glass);
  }
  
  .day-header {
    background: var(--bg-surface-elevated);
    padding: 10px 8px;
    text-align: center;
    border-bottom: 1px solid var(--border-subtle);
  }
  
  .day-name {
    text-transform: capitalize;
    font-weight: 600;
    color: var(--pastel-rose);
    font-size: 13px;
  }
  
  .day-date {
    font-size: 12px;
    color: var(--text-secondary);
    margin-top: 2px;
    font-variant-numeric: tabular-nums;
  }
  
  .day-body {
    flex: 1;
    padding: 8px 6px;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .spinner {
    display: inline-block;
    width: 14px;
    height: 14px;
    border: 2px solid currentColor;
    border-right-color: transparent;
    border-radius: 50%;
    animation: spinSmooth 0.75s linear infinite;
  }
</style>

<script lang="ts">
  import { onMount, createEventDispatcher } from 'svelte';
  import { apiFetch } from '../api';
  import { showAlert, showConfirm, hapticSuccess, hapticError, hapticWarning } from '../telegram';
  import SecureImage from './SecureImage.svelte';

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
                  <div class="appt-card compact status-{appt.status}" on:click|stopPropagation={() => openEditForm(appt)}>
                    <div class="appt-time">
                      {new Date(appt.appointmentDate).toLocaleTimeString('ru-RU', {hour: '2-digit', minute: '2-digit'})}
                    </div>
                    <div class="appt-details">
                      <div class="service-name">{services.find(s => s.serviceId === appt.serviceId)?.name || 'Услуга'}</div>
                    </div>
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
          <div class="icon">🌴</div>
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
                    {#if appt.status === 1}✓
                    {:else if appt.status === 2}✕
                    {/if}
                  </div>
                  <div class="expand-arrow" class:open={expandedId === appt.id}>›</div>
                </div>
              </div>

              {#if expandedId === appt.id}
                <div class="card-expanded" on:click|stopPropagation>
                  <div class="expand-divider"></div>

                  <div class="expand-meta">
                    <span class="meta-badge status-badge-{appt.status}">
                      {statusLabel(appt.status)}
                    </span>
                    <button class="edit-link" on:click={() => openEditForm(appt)}>✏️ Редактировать</button>
                  </div>

                  {#if appt.clientTelegramId && appt.clientTelegramId !== 'WALKIN'}
                    <div class="client-tg-row">
                      <span class="client-tg-label">Клиент:</span>
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <span
                        class="client-tg-id"
                        on:click={() => dispatch('openClientHistory', appt.clientId)}
                      >👤 {appt.clientName || appt.clientTelegramId} · @{appt.clientTelegramId} →</span>
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
                        {uploadingId === appt.id ? '⏳ Загрузка...' : '🔄 заменить фото'}
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
                        📎 Прикрепить результат
                      {/if}
                    </button>
                  {/if}
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
    padding-bottom: 20px;
  }

  /* Shared / Mobile styles */
  .date-selector {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--tg-theme-bg-color, #fff);
    padding: 12px;
    border-radius: 12px;
    margin-bottom: 16px;
    box-shadow: 0 1px 3px rgba(0,0,0,0.05);
  }
  
  .nav-btn {
    background: var(--tg-theme-secondary-bg-color, #f0f0f0);
    border: none;
    color: var(--tg-theme-text-color, #000);
    width: 36px;
    height: 36px;
    border-radius: 8px;
    font-size: 18px;
    cursor: pointer;
  }
  
  .current-date {
    font-weight: 600;
    text-transform: capitalize;
  }
  
  .appointments-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  
  .appt-card {
    background: var(--tg-theme-bg-color, #fff);
    border-radius: 12px;
    box-shadow: 0 1px 4px rgba(0,0,0,0.05);
    border-left: 4px solid var(--tg-theme-button-color, #3390ec);
    cursor: pointer;
    transition: box-shadow 0.2s, transform 0.15s;
    overflow: hidden;
  }
  .appt-card.mobile {
    padding: 0;
  }
  .appt-card.mobile.expanded {
    box-shadow: 0 4px 16px rgba(0,0,0,0.12);
  }
  .appt-card.compact {
    display: flex;
    padding: 8px;
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
    margin-bottom: 8px;
  }
  
  .appt-card.status-1 {
    border-left-color: #4CAF50;
  }
  .appt-card.status-2 {
    border-left-color: #F44336;
    opacity: 0.55;
  }

  /* Mobile card rows */
  .card-main-row {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 16px;
  }

  .card-right {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-shrink: 0;
  }

  .expand-arrow {
    font-size: 22px;
    color: var(--tg-theme-hint-color, #aaa);
    transition: transform 0.25s;
    line-height: 1;
    user-select: none;
  }
  .expand-arrow.open {
    transform: rotate(90deg);
  }
  
  /* Expanded section */
  .card-expanded {
    padding: 0 16px 16px;
    animation: slideDown 0.2s ease;
  }
  @keyframes slideDown {
    from { opacity: 0; transform: translateY(-6px); }
    to   { opacity: 1; transform: translateY(0); }
  }

  .expand-divider {
    height: 1px;
    background: var(--tg-theme-hint-color, #eee);
    opacity: 0.4;
    margin-bottom: 12px;
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
    padding: 4px 10px;
    border-radius: 20px;
  }
  .status-badge-0 { background: rgba(51,144,236,0.12); color: #3390ec; }
  .status-badge-1 { background: rgba(76,175,80,0.15);  color: #4CAF50; }
  .status-badge-2 { background: rgba(244,67,54,0.12);  color: #F44336; }

  .edit-link {
    background: none;
    border: none;
    color: var(--tg-theme-link-color, #3390ec);
    font-size: 14px;
    cursor: pointer;
    padding: 0;
  }

  .client-tg-row {
    display: flex;
    align-items: center;
    gap: 6px;
    background: var(--tg-theme-secondary-bg-color, #f5f5f5);
    border-radius: 8px;
    padding: 8px 12px;
    margin-bottom: 10px;
  }

  .client-tg-label {
    font-size: 13px;
    color: var(--tg-theme-hint-color, #999);
    flex-shrink: 0;
  }

  .client-tg-id {
    font-size: 13px;
    font-weight: 600;
    color: var(--tg-theme-button-color, #3390ec);
    cursor: pointer;
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .client-tg-id:hover {
    text-decoration: underline;
  }

  .attach-photo-btn {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 12px;
    border-radius: 10px;
    border: 2px dashed var(--tg-theme-button-color, #3390ec);
    background: rgba(51,144,236,0.05);
    color: var(--tg-theme-button-color, #3390ec);
    font-size: 15px;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.2s;
  }
  .attach-photo-btn:hover {
    background: rgba(51,144,236,0.1);
  }
  .attach-photo-btn:disabled {
    opacity: 0.6;
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
    border-radius: 8px;
    border: 1px solid var(--tg-theme-hint-color, #eee);
  }
  .change-photo-btn {
    background: none;
    border: 1px solid var(--tg-theme-hint-color, #ccc);
    border-radius: 8px;
    padding: 8px;
    font-size: 13px;
    color: var(--tg-theme-hint-color, #888);
    cursor: pointer;
  }
  .change-photo-btn:disabled { opacity: 0.6; cursor: default; }

  .future-note {
    text-align: center;
    font-size: 13px;
    color: var(--tg-theme-hint-color, #aaa);
    padding: 8px 0;
  }

  .spinner {
    display: inline-block;
    width: 14px;
    height: 14px;
    border: 2px solid currentColor;
    border-top-color: transparent;
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }
  
  .appt-time {
    font-weight: 700;
    color: var(--tg-theme-text-color, #000);
  }
  .mobile .card-main-row .appt-time {
    font-size: 18px;
    min-width: 60px;
  }
  .compact .appt-time {
    font-size: 14px;
  }
  
  .appt-details {
    flex: 1;
  }
  
  .service-name {
    font-weight: 600;
    font-size: 16px;
    color: var(--tg-theme-text-color, #000);
  }
  .compact .service-name {
    font-size: 13px;
    line-height: 1.2;
  }
  
  .client-name {
    font-size: 14px;
    color: var(--tg-theme-hint-color, #999);
    margin-top: 4px;
  }
  
  .status-icon {
    font-size: 18px;
    color: var(--tg-theme-hint-color, #999);
  }
  .appt-card.status-1 .status-icon { color: #4CAF50; }
  .appt-card.status-2 .status-icon { color: #F44336; }
  
  .add-btn {
    position: fixed;
    bottom: 24px;
    right: 24px;
    width: 56px;
    height: 56px;
    border-radius: 28px;
    background: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #fff);
    border: none;
    font-size: 32px;
    line-height: 1;
    box-shadow: 0 4px 12px rgba(51, 144, 236, 0.4);
    cursor: pointer;
    z-index: 100;
  }
  
  .card {
    background: var(--tg-theme-bg-color, #fff);
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
    max-width: 500px;
    margin: 0 auto;
  }
  
  .form-group {
    margin-bottom: 16px;
  }
  
  .form-group label {
    display: block;
    font-size: 14px;
    color: var(--tg-theme-hint-color, #999);
    margin-bottom: 8px;
  }
  
  .input {
    width: 100%;
    box-sizing: border-box;
    padding: 12px;
    border: 1px solid var(--tg-theme-hint-color, #eee);
    border-radius: 8px;
    font-size: 16px;
    background: var(--tg-theme-bg-color, #fff);
    color: var(--tg-theme-text-color, #000);
  }
  
  .form-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
    margin-top: 24px;
  }
  
  .flex-1 { flex: 1; }
  
  .primary-btn {
    background: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #fff);
    border: none;
    padding: 14px;
    border-radius: 8px;
    font-weight: 600;
    font-size: 16px;
    cursor: pointer;
  }
  
  .secondary-btn {
    background: var(--tg-theme-secondary-bg-color, #f0f0f0);
    color: var(--tg-theme-text-color, #000);
    border: none;
    padding: 14px;
    border-radius: 8px;
    font-weight: 600;
    font-size: 16px;
    cursor: pointer;
  }
  
  .danger-btn {
    background: rgba(244, 67, 54, 0.1);
    color: #F44336;
    border: none;
    padding: 14px;
    border-radius: 8px;
    font-weight: 600;
    font-size: 16px;
    cursor: pointer;
  }
  
  .loading, .empty-state {
    text-align: center;
    padding: 40px 20px;
    color: var(--tg-theme-hint-color, #999);
  }
  .empty-state .icon {
    font-size: 48px;
    margin-bottom: 16px;
  }

  /* Desktop Grid Styles */
  .desktop-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }
  
  .week-nav {
    display: flex;
    align-items: center;
    gap: 16px;
  }
  
  .week-label {
    font-weight: 600;
    font-size: 16px;
    color: var(--tg-theme-text-color, #000);
  }
  
  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 1px;
    background: var(--tg-theme-hint-color, #eee);
    border: 1px solid var(--tg-theme-hint-color, #eee);
    border-radius: 8px;
    overflow: hidden;
  }
  
  .day-col {
    background: var(--tg-theme-secondary-bg-color, #f5f5f5);
    display: flex;
    flex-direction: column;
    min-height: 400px;
    cursor: pointer;
  }
  
  .day-header {
    background: var(--tg-theme-bg-color, #fff);
    padding: 12px;
    text-align: center;
    border-bottom: 1px solid var(--tg-theme-hint-color, #eee);
  }
  
  .day-name {
    text-transform: capitalize;
    font-weight: 600;
    color: var(--tg-theme-text-color, #000);
    font-size: 14px;
  }
  
  .day-date {
    font-size: 12px;
    color: var(--tg-theme-hint-color, #999);
    margin-top: 4px;
  }
  
  .day-body {
    flex: 1;
    padding: 8px;
    display: flex;
    flex-direction: column;
  }
  
  .day-body:hover {
    background: rgba(0,0,0,0.02);
  }
</style>

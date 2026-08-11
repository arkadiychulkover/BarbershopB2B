<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../api';
  import { showAlert, showConfirm, hapticSuccess, hapticError, hapticWarning } from '../telegram';
  
  let innerWidth = 0;
  $: isDesktop = innerWidth >= 768;

  let currentDate = new Date();
  let currentWeekStart = getMonday(new Date());
  
  let appointments = [];
  let services = [];
  let loading = true;
  
  let view: 'list' | 'form' = 'list';
  let editingAppt = null;
  
  // form state
  let formDateStr = '';
  let formTime = '';
  let formServiceId = '';
  let formStatus = 0;
  
  $: dateString = new Date(currentDate.getTime() - currentDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
  
  function getMonday(d) {
    d = new Date(d);
    var day = d.getDay(),
        diff = d.getDate() - day + (day == 0 ? -6: 1); // adjust when day is sunday
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
      // only active services with an ID
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
    
  function openNewForm(dateToUse) {
    editingAppt = null;
    formDateStr = new Date(dateToUse.getTime() - dateToUse.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = '10:00';
    formServiceId = services.length > 0 ? services[0].serviceId : '';
    formStatus = 0;
    view = 'form';
  }
  
  function openEditForm(appt) {
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
        <div class="appointments-list">
          {#each currentDayAppts as appt}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="appt-card mobile status-{appt.status}" on:click={() => openEditForm(appt)}>
              <div class="appt-time">
                {new Date(appt.appointmentDate).toLocaleTimeString('ru-RU', {hour: '2-digit', minute: '2-digit'})}
              </div>
              <div class="appt-details">
                <div class="service-name">{services.find(s => s.serviceId === appt.serviceId)?.name || 'Услуга'}</div>
                <div class="client-name">{appt.clientId ? 'Забронировано' : 'Гость (Вручную)'}</div>
              </div>
              <div class="status-icon">
                {#if appt.status === 1}✓
                {:else if appt.status === 2}✕
                {/if}
              </div>
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
    display: flex;
    background: var(--tg-theme-bg-color, #fff);
    border-radius: 12px;
    box-shadow: 0 1px 4px rgba(0,0,0,0.05);
    border-left: 4px solid var(--tg-theme-button-color, #3390ec);
    cursor: pointer;
  }
  .appt-card.mobile {
    padding: 16px;
    align-items: center;
    gap: 16px;
  }
  .appt-card.compact {
    padding: 8px;
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
    margin-bottom: 8px;
  }
  
  .appt-card.status-1 {
    border-left-color: #4CAF50;
    opacity: 0.8;
  }
  
  .appt-card.status-2 {
    border-left-color: #F44336;
    opacity: 0.5;
  }
  
  .appt-time {
    font-weight: 700;
    color: var(--tg-theme-text-color, #000);
  }
  .mobile .appt-time {
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

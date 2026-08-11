<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  
  let masters = [];
  let selectedMasterId = '';
  
  let currentWeekStart = getMonday(new Date());
  
  let appointments = [];
  let services = [];
  let isLoading = false;
  
  let showModal = false;
  let editingAppt = null;
  
  // form state
  let formDate = '';
  let formTime = '';
  let formServiceId = '';
  let formClientId = '';
  let formStatus = 0;
  
  function getMonday(d) {
    d = new Date(d);
    var day = d.getDay(),
        diff = d.getDate() - day + (day == 0 ? -6: 1); // adjust when day is sunday
    return new Date(d.setDate(diff));
  }
  
  function getDaysOfWeek() {
    let days = [];
    for(let i=0; i<7; i++) {
        let d = new Date(currentWeekStart);
        d.setDate(d.getDate() + i);
        days.push(d);
    }
    return days;
  }
  
  $: days = getDaysOfWeek(currentWeekStart);
  
  onMount(async () => {
    try {
      masters = await apiRequest('/api/Barber/all');
      if (masters.length > 0) {
        selectedMasterId = masters[0].id;
      }
    } catch (e) {
      console.error(e);
    }
  });
  
  $: if (selectedMasterId) {
    fetchData();
  }
  
  async function fetchData() {
    if (!selectedMasterId) return;
    isLoading = true;
    try {
      const allAppts = await apiRequest('/api/Barber/admin-appointments/all');
      appointments = allAppts.filter(a => a.masterId === selectedMasterId);
      
      services = await apiRequest(`/api/Barber/admin-services/${selectedMasterId}`);
    } catch(e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  }
  
  function nextWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() + 7);
    currentWeekStart = d;
  }
  
  function prevWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() - 7);
    currentWeekStart = d;
  }
  
  function getApptsForDay(date) {
    const dateString = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    return appointments.filter(a => {
        // Handle timezone issues properly by comparing just the dates
        const apptDate = new Date(a.appointmentDate);
        const apptDateString = new Date(apptDate.getTime() - apptDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
        return apptDateString === dateString;
    }).sort((a,b) => a.appointmentDate.localeCompare(b.appointmentDate));
  }
  
  function openAddModal(date) {
    editingAppt = null;
    formDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = '10:00';
    formServiceId = services.length > 0 ? services[0].serviceId : '';
    formStatus = 0;
    showModal = true;
  }
  
  function openEditModal(appt) {
    editingAppt = appt;
    const d = new Date(appt.appointmentDate);
    formDate = new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = d.toTimeString().substring(0,5);
    formServiceId = appt.serviceId || (services.length > 0 ? services[0].serviceId : '');
    formClientId = appt.clientId;
    formStatus = appt.status;
    showModal = true;
  }
  
  async function saveAppt() {
    const appointmentDate = new Date(`${formDate}T${formTime}:00`).toISOString();
    
    const body = {
      masterId: selectedMasterId,
      clientId: formClientId || null,
      serviceId: formServiceId,
      appointmentDate: appointmentDate,
      status: parseInt(formStatus)
    };
    
    try {
      if (editingAppt) {
        await apiRequest(`/api/Barber/admin-appointments/update/${editingAppt.id}`, {
          method: 'PUT',
          headers: {'Content-Type': 'application/json'},
          body: JSON.stringify(body)
        });
      } else {
        await apiRequest('/api/Barber/admin-appointments/add', {
          method: 'POST',
          headers: {'Content-Type': 'application/json'},
          body: JSON.stringify(body)
        });
      }
      showModal = false;
      await fetchData();
    } catch(e) {
      alert("Ошибка: " + e.message);
    }
  }
  
  async function deleteAppt() {
    if (!editingAppt) return;
    if (!confirm('Удалить запись?')) return;
    try {
      await apiRequest(`/api/Barber/admin-appointments/delete/${editingAppt.id}`, { method: 'DELETE' });
      showModal = false;
      await fetchData();
    } catch(e) {
      alert("Ошибка: " + e.message);
    }
  }
</script>

<DashboardLayout>
  <div class="schedule-page">
    <header class="page-header">
      <div class="header-left">
        <h1>Расписание</h1>
        <div class="controls">
          <select class="input master-select" bind:value={selectedMasterId}>
            {#each masters as m}
              <option value={m.id}>{m.name}</option>
            {/each}
          </select>
          <div class="week-nav">
            <button class="btn btn-secondary" on:click={prevWeek}>&larr;</button>
            <span class="week-label">
              {days[0].toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})} - 
              {days[6].toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})}
            </span>
            <button class="btn btn-secondary" on:click={nextWeek}>&rarr;</button>
          </div>
        </div>
      </div>
      <button class="btn btn-primary" on:click={() => openAddModal(new Date())}>Новая запись</button>
    </header>

    {#if isLoading}
      <div class="loading">Загрузка...</div>
    {:else}
      <div class="calendar-grid">
        {#each days as day}
          <div class="day-col">
            <div class="day-header">
              <div class="day-name">{day.toLocaleDateString('ru-RU', {weekday: 'long'})}</div>
              <div class="day-date">{day.toLocaleDateString('ru-RU', {day:'2-digit', month:'2-digit'})}</div>
            </div>
            <div class="day-body" on:click={() => openAddModal(day)}>
              {#each getApptsForDay(day) as appt}
                <div class="appt-card status-{appt.status}" on:click|stopPropagation={() => openEditModal(appt)}>
                  <div class="appt-time">
                    {new Date(appt.appointmentDate).toLocaleTimeString('ru-RU', {hour:'2-digit', minute:'2-digit'})} - 
                    {new Date(appt.appointmentEndDate).toLocaleTimeString('ru-RU', {hour:'2-digit', minute:'2-digit'})}
                  </div>
                  <div class="appt-service">
                    {services.find(s => s.serviceId === appt.serviceId)?.name || 'Услуга'}
                  </div>
                </div>
              {/each}
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
  
  {#if showModal}
    <div class="modal-backdrop" on:click={() => showModal = false}>
      <div class="modal-content card" on:click|stopPropagation>
        <h2>{editingAppt ? 'Редактировать запись' : 'Новая запись'}</h2>
        
        <div class="form-group">
          <label>Дата</label>
          <input type="date" class="input" bind:value={formDate}>
        </div>
        <div class="form-group mt-2">
          <label>Время</label>
          <input type="time" class="input" bind:value={formTime}>
        </div>
        <div class="form-group mt-2">
          <label>Услуга</label>
          <select class="input" bind:value={formServiceId}>
            {#each services as s}
              <option value={s.serviceId}>{s.name} ({s.price} ₴)</option>
            {/each}
          </select>
        </div>
        <div class="form-group mt-2">
          <label>Статус</label>
          <select class="input" bind:value={formStatus}>
            <option value="0">Запланировано</option>
            <option value="1">Выполнено</option>
            <option value="2">Отменено</option>
          </select>
        </div>
        
        <div class="modal-actions mt-4">
          <button class="btn btn-primary" on:click={saveAppt}>Сохранить</button>
          {#if editingAppt}
            <button class="btn btn-danger" on:click={deleteAppt}>Удалить</button>
          {/if}
          <button class="btn btn-secondary" on:click={() => showModal = false}>Отмена</button>
        </div>
      </div>
    </div>
  {/if}
</DashboardLayout>

<style>
  .schedule-page {
    height: 100%;
    display: flex;
    flex-direction: column;
  }
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 2rem;
  }
  .header-left h1 { margin-bottom: 1rem; }
  .controls {
    display: flex;
    gap: 1rem;
    align-items: center;
  }
  .master-select {
    width: 250px;
  }
  .week-nav {
    display: flex;
    align-items: center;
    gap: 1rem;
    background: var(--bg-secondary);
    padding: 0.25rem;
    border-radius: var(--border-radius);
    border: 1px solid var(--border-color);
  }
  .week-label {
    font-weight: 500;
  }
  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 1px;
    background: var(--border-color);
    border: 1px solid var(--border-color);
    border-radius: var(--border-radius);
    overflow: hidden;
  }
  .day-col {
    background: var(--bg-color);
    display: flex;
    flex-direction: column;
    min-height: 600px;
  }
  .day-header {
    background: var(--bg-secondary);
    padding: 1rem;
    text-align: center;
    border-bottom: 1px solid var(--border-color);
  }
  .day-name {
    text-transform: capitalize;
    font-weight: 600;
    color: var(--text-primary);
  }
  .day-date {
    font-size: 0.875rem;
    color: var(--text-muted);
    margin-top: 0.25rem;
  }
  .day-body {
    flex: 1;
    padding: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    cursor: crosshair;
  }
  .day-body:hover {
    background: rgba(255,255,255,0.02);
  }
  .appt-card {
    background: var(--bg-tertiary);
    border: 1px solid var(--border-color);
    border-left: 4px solid var(--accent);
    padding: 0.75rem;
    border-radius: 6px;
    cursor: pointer;
    transition: transform 0.1s;
  }
  .appt-card:hover {
    transform: translateY(-2px);
    border-color: var(--accent-muted);
  }
  .status-1 { border-left-color: var(--success); } /* Completed */
  .status-2 { border-left-color: var(--danger); opacity: 0.6; } /* Cancelled */
  
  .appt-time {
    font-size: 0.75rem;
    color: var(--text-muted);
    margin-bottom: 0.25rem;
  }
  .appt-service {
    font-size: 0.875rem;
    font-weight: 500;
  }
  
  .modal-backdrop {
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(0,0,0,0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }
  .modal-content {
    width: 400px;
    max-width: 90vw;
  }
  .modal-content h2 { margin-bottom: 1.5rem; }
  .modal-actions {
    display: flex;
    gap: 1rem;
  }
  .mt-2 { margin-top: 1rem; }
  .mt-4 { margin-top: 2rem; }
</style>

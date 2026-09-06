<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { currentLocale } from '../lib/locale.js';
  import { m } from '../lib/paraglide/messages.js';
  import { 
    ChevronLeft, 
    ChevronRight, 
    Plus, 
    Calendar, 
    Clock, 
    Scissors, 
    CheckCircle2, 
    Trash2,
    Sparkles,
    User,
    X
  } from 'lucide-svelte';
  
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
        diff = d.getDate() - day + (day == 0 ? -6: 1);
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

  let selectedDayIndex = 0;

  function isToday(date) {
    const today = new Date();
    return date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear();
  }

  $: {
    if (days && days.length) {
      const todayIdx = days.findIndex(d => isToday(d));
      if (todayIdx !== -1 && selectedDayIndex === 0) {
        selectedDayIndex = todayIdx;
      }
    }
  }
  
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
        const apptDate = new Date(a.appointmentDate);
        const apptDateString = new Date(apptDate.getTime() - apptDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
        return apptDateString === dateString;
    }).sort((a,b) => a.appointmentDate.localeCompare(b.appointmentDate));
  }

  function extractTime(dateStr) {
    if (!dateStr) return '??:??';
    const m = String(dateStr).match(/(?:T|\s|^)(\d{2}):(\d{2})/);
    return m ? `${m[1]}:${m[2]}` : '??:??';
  }

  function addMinutesToTime(timeStr, mins) {
    if (!timeStr || timeStr === '??:??') return '??:??';
    const [h, m] = timeStr.split(':').map(Number);
    if (isNaN(h) || isNaN(m)) return timeStr;
    const total = h * 60 + m + mins;
    const eh = Math.floor(total / 60) % 24;
    const em = total % 60;
    return `${String(eh).padStart(2, '0')}:${String(em).padStart(2, '0')}`;
  }

  function extractEndTime(appt) {
    if (!appt) return '??:??';
    const start = extractTime(appt.appointmentDate);
    let end = extractTime(appt.appointmentEndDate);
    if (!end || end === '??:??' || end === start) {
      const svc = services.find(s => s.serviceId === appt.serviceId);
      const duration = svc?.duration || 30;
      end = addMinutesToTime(start, duration);
    }
    return end;
  }

  function extractDate(dateStr) {
    const m = String(dateStr).match(/(\d{4})-(\d{2})-(\d{2})/);
    return m ? `${m[1]}-${m[2]}-${m[3]}` : '';
  }
  
  function openAddModal(date) {
    editingAppt = null;
    formDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    formTime = '10:00';
    formServiceId = services.length > 0 ? services[0].serviceId : '';
    formStatus = '0';
    showModal = true;
  }
  
  function openEditModal(appt) {
    editingAppt = appt;
    formDate = extractDate(appt.appointmentDate);
    formTime = extractTime(appt.appointmentDate);
    formServiceId = appt.serviceId || (services.length > 0 ? services[0].serviceId : '');
    formClientId = appt.clientId;
    if (appt.status !== undefined && appt.status !== null) {
      if (typeof appt.status === 'number') {
        formStatus = String(appt.status);
      } else {
        const statuses = ['Scheduled', 'Completed', 'Cancelled', 'NoShow'];
        const idx = statuses.indexOf(appt.status);
        formStatus = idx !== -1 ? String(idx) : (['0', '1', '2', '3'].includes(String(appt.status)) ? String(appt.status) : '0');
      }
    } else {
      formStatus = '0';
    }
    showModal = true;
  }
  
  async function saveAppt() {
    const appointmentDate = `${formDate}T${formTime}:00Z`;
    
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
      alert(m.common_error() + ": " + e.message);
    }
  }
  
  async function deleteAppt() {
    if (!editingAppt) return;
    if (!confirm(m.schedule_delete_confirm())) return;
    try {
      await apiRequest(`/api/Barber/admin-appointments/delete/${editingAppt.id}`, { method: 'DELETE' });
      showModal = false;
      await fetchData();
    } catch(e) {
      alert(m.common_error() + ": " + e.message);
    }
  }
</script>

<DashboardLayout>
  <div class="schedule-page">
    <header class="page-header">
      <div class="header-left">
        <h1>{m.schedule_title()}</h1>
        <div class="controls">
          <div class="select-wrap">
            <User size={16} class="select-icon" />
            <select class="input master-select" bind:value={selectedMasterId}>
              {#each masters as master}
                <option value={master.id}>{master.name}</option>
              {/each}
            </select>
          </div>

          <div class="week-nav">
            <button class="nav-arrow-btn" on:click={prevWeek} title="Previous week">
              <ChevronLeft size={18} />
            </button>
            <span class="week-label">
              {days[0].toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', {day:'2-digit', month:'short'})} — {days[6].toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', {day:'2-digit', month:'short'})}
            </span>
            <button class="nav-arrow-btn" on:click={nextWeek} title="Next week">
              <ChevronRight size={18} />
            </button>
          </div>
        </div>
      </div>

      <button class="btn btn-primary" on:click={() => openAddModal(new Date())}>
        <Plus size={18} />
        <span>{m.schedule_add_appt()}</span>
      </button>
    </header>

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>{m.common_loading()}</p>
      </div>
    {:else}
      <!-- Mobile Day Selector Tabs -->
      <div class="mobile-day-tabs">
        {#each days as day, idx}
          {@const appts = getApptsForDay(day)}
          <button 
            type="button"
            class="day-tab" 
            class:active={selectedDayIndex === idx} 
            class:is-today={isToday(day)}
            on:click={() => selectedDayIndex = idx}
          >
            <span class="day-tab-name">{day.toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', {weekday: 'short'})}</span>
            <span class="day-tab-date">{day.getDate()}</span>
            {#if appts.length > 0}
              <span class="day-tab-dot"></span>
            {/if}
          </button>
        {/each}
      </div>

      <div class="calendar-grid">
        {#each days as day, idx}
          <div class="day-col" class:is-today={isToday(day)} class:mobile-active={selectedDayIndex === idx}>
            <div class="day-header">
              <div class="day-name">{day.toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', {weekday: 'short'})}</div>
              <div class="day-date" class:today-pill={isToday(day)}>
                {day.toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', {day:'numeric', month:'numeric'})}
              </div>
            </div>
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="day-body" on:click={() => openAddModal(day)}>
              {#each getApptsForDay(day) as appt}
                <!-- svelte-ignore a11y-click-events-have-key-events -->
                <!-- svelte-ignore a11y-no-static-element-interactions -->
                <div class="appt-card status-{appt.status}" on:click|stopPropagation={() => openEditModal(appt)}>
                  <div class="appt-time">
                    <Clock size={12} />
                    <span>
                      {extractTime(appt.appointmentDate)} – {extractEndTime(appt)}
                    </span>
                  </div>
                  <div class="appt-service">
                    {services.find(s => s.serviceId === appt.serviceId)?.name || m.nav_services()}
                  </div>
                  {#if appt.clientName}
                    <div class="appt-client">
                      {appt.clientName}
                    </div>
                  {/if}
                </div>
              {:else}
                <div class="empty-slot">
                  <span>+ {m.schedule_add_appt()}</span>
                </div>
              {/each}
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
  
  {#if showModal}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => showModal = false}>
      <div class="modal-content card" on:click|stopPropagation>
        <div class="modal-header">
          <div class="modal-header-text">
            <h2>{editingAppt ? m.schedule_edit_appt() : m.schedule_add_appt()}</h2>
            <p class="modal-subtitle">Appointment details</p>
          </div>
          <button class="modal-close-btn" on:click={() => showModal = false} type="button" aria-label={m.common_close()}>
            <X size={18} />
          </button>
        </div>
        
        <div class="form-group">
          <label for="modal-form-date">{m.common_date()}</label>
          <input id="modal-form-date" type="date" class="input" bind:value={formDate}>
        </div>
        <div class="form-group mt-2">
          <label for="modal-form-time">{m.common_time()}</label>
          <input id="modal-form-time" type="time" class="input" bind:value={formTime}>
        </div>
        <div class="form-group mt-2">
          <label for="modal-form-service">{m.nav_services()}</label>
          <select id="modal-form-service" class="input" bind:value={formServiceId}>
            {#each services as s}
              <option value={s.serviceId}>{s.name} ({s.price} ₴)</option>
            {/each}
          </select>
        </div>
        <div class="form-group mt-2">
          <label for="modal-form-status">{m.common_status()}</label>
          <select id="modal-form-status" class="input" bind:value={formStatus}>
            <option value="0">{m.status_scheduled()}</option>
            <option value="1">{m.status_completed()}</option>
            <option value="2">{m.status_cancelled()}</option>
            <option value="3">No Show</option>
          </select>
        </div>
        
        <div class="modal-actions mt-4">
          <button class="btn btn-primary flex-1" on:click={saveAppt}>{m.common_save()}</button>
          {#if editingAppt}
            <button class="btn btn-danger" on:click={deleteAppt}>
              <Trash2 size={16} />
              <span>{m.common_delete()}</span>
            </button>
          {/if}
          <button class="btn btn-secondary" on:click={() => showModal = false}>{m.common_cancel()}</button>
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
    animation: pageFadeIn 0.3s ease-out;
  }

  @keyframes pageFadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 2rem;
    flex-wrap: wrap;
    gap: 1.25rem;
  }

  .header-left h1 { 
    font-size: 2.2rem;
    margin-bottom: 1rem; 
  }

  .controls {
    display: flex;
    gap: 1rem;
    align-items: center;
    flex-wrap: wrap;
  }

  .select-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.select-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .master-select {
    width: 240px;
    padding-left: 2.6rem;
  }

  .week-nav {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    background: var(--bg-surface);
    backdrop-filter: blur(14px);
    padding: 0.35rem 0.6rem;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
  }

  .nav-arrow-btn {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;
  }

  .nav-arrow-btn:hover {
    color: var(--pastel-rose);
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }

  .week-label {
    font-weight: 600;
    font-size: 0.9rem;
    color: var(--text-primary);
    padding: 0 0.4rem;
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 5rem 0;
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

  /* Mobile Day Selector Tabs */
  .mobile-day-tabs {
    display: none;
    gap: 0.4rem;
    overflow-x: auto;
    padding-bottom: 0.75rem;
    margin-bottom: 0.75rem;
    -webkit-overflow-scrolling: touch;
  }

  .day-tab {
    flex: 1;
    min-width: 44px;
    padding: 0.5rem 0.3rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.2rem;
    cursor: pointer;
    color: var(--text-secondary);
    position: relative;
    transition: all 0.2s;
  }

  .day-tab.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: #ffffff;
    border-color: transparent;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
  }

  .day-tab.is-today:not(.active) {
    border-color: var(--pastel-rose);
    color: var(--pastel-rose);
  }

  .day-tab-name {
    font-size: 0.72rem;
    font-weight: 600;
    text-transform: capitalize;
  }

  .day-tab-date {
    font-size: 0.95rem;
    font-weight: 700;
  }

  .day-tab-dot {
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: var(--pastel-sage);
    position: absolute;
    bottom: 4px;
  }

  .day-tab.active .day-tab-dot {
    background: #ffffff;
  }

  /* Calendar Grid */
  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 10px;
    background: transparent;
  }

  @media (max-width: 1024px) {
    .calendar-grid {
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    }
  }

  .day-col {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    display: flex;
    flex-direction: column;
    min-height: 540px;
    overflow: hidden;
    transition: all 0.2s;
  }

  .day-col.is-today {
    border-color: rgba(223, 158, 142, 0.35);
    box-shadow: 0 0 20px rgba(223, 158, 142, 0.08);
  }

  .day-header {
    background: rgba(255, 255, 255, 0.02);
    padding: 1rem 0.75rem;
    text-align: center;
    border-bottom: 1px solid var(--border-subtle);
  }

  .day-name {
    text-transform: capitalize;
    font-weight: 700;
    font-size: 0.85rem;
    color: var(--text-secondary);
  }

  .day-date {
    font-size: 0.95rem;
    font-weight: 700;
    color: var(--text-primary);
    margin-top: 0.25rem;
    display: inline-block;
  }

  .today-pill {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    padding: 0.15rem 0.55rem;
    border-radius: var(--radius-pill);
    box-shadow: 0 2px 8px var(--pastel-rose-glow);
  }

  .day-body {
    flex: 1;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
    cursor: pointer;
  }

  .appt-card {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-left: 3.5px solid var(--pastel-amber);
    padding: 0.75rem 0.85rem;
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
  }

  .appt-card:hover {
    transform: translateY(-2px);
    border-color: var(--border-glass);
    box-shadow: var(--shadow-sm);
  }

  .status-1 { 
    border-left-color: var(--pastel-sage); 
  }
  .status-2 { 
    border-left-color: var(--pastel-coral); 
    opacity: 0.55; 
  }
  
  .appt-time {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-muted);
    margin-bottom: 0.25rem;
    display: flex;
    align-items: center;
    gap: 0.3rem;
  }

  .appt-service {
    font-size: 0.88rem;
    font-weight: 700;
    color: var(--text-primary);
    line-height: 1.3;
  }

  .appt-client {
    font-size: 0.78rem;
    color: var(--pastel-rose);
    margin-top: 0.2rem;
  }

  .empty-slot {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--text-muted);
    font-size: 0.8rem;
    opacity: 0;
    transition: opacity 0.2s;
    border: 1px dashed transparent;
    border-radius: var(--radius-sm);
    min-height: 60px;
  }

  .day-body:hover .empty-slot {
    opacity: 0.7;
    border-color: var(--border-subtle);
  }

  /* Modal */
  .modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(10, 12, 16, 0.85);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    padding: 1.5rem;
    box-sizing: border-box;
    animation: modalBgFade 0.2s ease-out;
  }

  @keyframes modalBgFade {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  .modal-content {
    width: 480px;
    max-width: 95vw;
    max-height: 90vh;
    overflow-y: auto;
    margin: auto;
    background: var(--bg-surface-solid);
    border: 1px solid var(--border-glass);
    border-radius: var(--radius-lg);
    padding: 2.25rem 2rem;
    box-shadow: 0 24px 64px rgba(0, 0, 0, 0.8), 0 0 24px rgba(223, 158, 142, 0.15);
    display: flex;
    flex-direction: column;
    box-sizing: border-box;
    animation: modalPop 0.25s var(--ease-spring);
  }

  @keyframes modalPop {
    from { opacity: 0; transform: scale(0.96); }
    to { opacity: 1; transform: scale(1); }
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1rem;
    margin-bottom: 1.5rem;
  }

  .modal-header-text {
    flex: 1;
  }

  .modal-header h2 { 
    font-size: 1.45rem;
    font-weight: 700;
    margin-bottom: 0.25rem;
    color: var(--text-primary);
  }

  .modal-subtitle {
    color: var(--text-secondary);
    font-size: 0.88rem;
  }

  .modal-close-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-muted);
    width: 32px;
    height: 32px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s;
    flex-shrink: 0;
  }

  .modal-close-btn:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  label {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .modal-actions {
    display: flex;
    gap: 0.75rem;
    margin-top: 1.75rem;
  }

  .flex-1 {
    flex: 1;
  }

  .mt-2 { margin-top: 0.85rem; }
  .mt-4 { margin-top: 1.5rem; }

  @media (max-width: 768px) {
    .mobile-day-tabs {
      display: flex;
    }

    .calendar-grid {
      display: block;
    }

    .day-col {
      display: none;
      min-height: auto;
    }

    .day-col.mobile-active {
      display: flex;
    }

    .page-header {
      flex-direction: column;
      align-items: stretch;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }

    .header-left {
      width: 100%;
    }

    .header-left h1 {
      font-size: 1.6rem;
      margin-bottom: 0.75rem;
    }

    .controls {
      flex-direction: column;
      align-items: stretch;
      gap: 0.75rem;
      width: 100%;
    }

    .select-wrap, .master-select {
      width: 100%;
    }

    .week-nav {
      justify-content: space-between;
      width: 100%;
      box-sizing: border-box;
    }

    .page-header .btn-primary {
      width: 100%;
    }

    .modal-content {
      width: calc(100% - 1.5rem) !important;
      max-width: 100% !important;
      margin: 0.75rem;
      padding: 1.25rem;
      max-height: 90vh;
      overflow-y: auto;
    }

    .modal-actions {
      flex-direction: column;
      gap: 0.5rem;
    }

    .modal-actions .btn {
      width: 100%;
    }
  }

  @media (max-width: 480px) {
    .day-tab {
      padding: 0.4rem 0.2rem;
      min-width: 38px;
    }

    .day-tab-name {
      font-size: 0.65rem;
    }

    .day-tab-date {
      font-size: 0.85rem;
    }

    .day-body {
      padding: 0.6rem;
    }
  }
</style>

<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { apiRequest } from '../lib/api';
  import { m } from '../lib/paraglide/messages.js';
  import { currentLocale } from '../lib/locale.js';
  import { ChevronLeft, ChevronRight, Clock, User, Scissors, Calendar, RefreshCw } from 'lucide-svelte';

  let selectedDate = new Date();
  selectedDate.setHours(0, 0, 0, 0);

  let masters = [];
  let appointments = [];
  let shifts = {};      // masterId -> Shift[]
  let isLoading = false;
  let error = '';

  const HOUR_START = 8;
  const HOUR_END = 22;
  const SLOT_HEIGHT_PX = 48; // px / 30 min

  function timeSlots() {
    const slots = [];
    for (let h = HOUR_START; h < HOUR_END; h++) {
      slots.push(`${String(h).padStart(2,'0')}:00`);
      slots.push(`${String(h).padStart(2,'0')}:30`);
    }
    return slots;
  }
  const TIME_SLOTS = timeSlots();

  function slotTop(timeStr) {
    const [h, min] = timeStr.split(':').map(Number);
    const totalMins = (h - HOUR_START) * 60 + min;
    return (totalMins / 30) * SLOT_HEIGHT_PX;
  }

  function apptTop(appt) {
    const d = new Date(appt.appointmentDate);
    const h = d.getUTCHours();
    const min = d.getUTCMinutes();
    return slotTop(`${String(h).padStart(2,'0')}:${String(min).padStart(2,'0')}`);
  }

  function apptHeight(appt) {
    const start = new Date(appt.appointmentDate);
    const end = new Date(appt.appointmentEndDate);
    const durationMins = (end - start) / 60000;
    return (durationMins / 30) * SLOT_HEIGHT_PX;
  }

  function totalGridHeight() {
    return TIME_SLOTS.length * SLOT_HEIGHT_PX;
  }

  function formatDate(d) {
    return d.toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US', { weekday: 'long', day: 'numeric', month: 'long' });
  }

  function prevDay() {
    const d = new Date(selectedDate);
    d.setDate(d.getDate() - 1);
    selectedDate = d;
    loadData();
  }

  function nextDay() {
    const d = new Date(selectedDate);
    d.setDate(d.getDate() + 1);
    selectedDate = d;
    loadData();
  }

  function formatTime(dateStr) {
    const d = new Date(dateStr);
    return d.toUTCString().slice(17, 22);
  }

  function getStatusColor(status) {
    switch(status) {
      case 'Completed': return 'var(--clr-green)';
      case 'Cancelled': return 'var(--clr-red)';
      default: return 'var(--clr-accent)';
    }
  }

  function getMasterAppointments(masterId) {
    const dateStr = selectedDate.toISOString().slice(0, 10);
    return appointments.filter(a => {
      const aDate = new Date(a.appointmentDate).toISOString().slice(0, 10);
      return a.masterId === masterId && aDate === dateStr;
    });
  }

  async function loadData() {
    isLoading = true;
    error = '';
    try {
      const barbersData = await apiRequest('/api/Barber/all');
      masters = Array.isArray(barbersData) ? barbersData : [];

      const allAppointments = await apiRequest('/api/Barber/admin-appointments/all');
      appointments = Array.isArray(allAppointments) ? allAppointments : [];
    } catch(e) {
      error = e.message || m.common_error();
    } finally {
      isLoading = false;
    }
  }

  onMount(loadData);
</script>

<svelte:head>
  <title>{m.timeline_title()}</title>
</svelte:head>

<DashboardLayout>
  <div class="timeline-page">
    <div class="page-header">
      <div class="header-left">
        <Calendar size={22} />
        <h1>{m.timeline_title()}</h1>
      </div>
      <div class="date-nav">
        <button class="nav-btn" on:click={prevDay}><ChevronLeft size={18}/></button>
        <span class="date-label">{formatDate(selectedDate)}</span>
        <button class="nav-btn" on:click={nextDay}><ChevronRight size={18}/></button>
        <button class="refresh-btn" on:click={loadData} title={m.common_refresh()}><RefreshCw size={16}/></button>
      </div>
    </div>

    {#if error}
      <div class="error-banner">{error}</div>
    {/if}

    {#if isLoading}
      <div class="loading-overlay">
        <div class="spinner"></div>
        <span>{m.common_loading()}</span>
      </div>
    {:else}
      <div class="timeline-container">
        <div class="time-axis">
          <div class="col-header time-col-header"></div>
          <div class="time-rows" style="height:{totalGridHeight()}px; position:relative;">
            {#each TIME_SLOTS as slot, i}
              <div class="time-label" style="top:{i * SLOT_HEIGHT_PX}px">
                {#if slot.endsWith(':00')}<Clock size={11} />{slot}{/if}
              </div>
            {/each}
          </div>
        </div>

        <div class="masters-scroll">
          {#each masters as master}
            <div class="master-col">
              <div class="col-header">
                {#if master.photoUrl}
                  <img src={master.photoUrl} alt={master.name} class="master-avatar"/>
                {:else}
                  <div class="master-avatar-placeholder"><User size={18}/></div>
                {/if}
                <div class="master-col-name">
                  <span class="master-name">{master.name}</span>
                  {#if master.isOnVacation}
                    <span class="vacation-badge">{m.masters_on_vacation()}</span>
                  {/if}
                </div>
              </div>

              <div class="slots-grid" style="height:{totalGridHeight()}px; position:relative;">
                {#each TIME_SLOTS as slot, i}
                  <div
                    class="slot-cell {slot.endsWith(':00') ? 'hour-start' : ''}"
                    style="top:{i * SLOT_HEIGHT_PX}px; height:{SLOT_HEIGHT_PX}px;"
                  ></div>
                {/each}

                {#each getMasterAppointments(master.id) as appt}
                  <div
                    class="appointment-block status-{appt.status?.toLowerCase()}"
                    style="top:{apptTop(appt)}px; height:{Math.max(apptHeight(appt), SLOT_HEIGHT_PX)}px;"
                    title="{appt.clientName || m.schedule_client()} — {appt.serviceName || m.schedule_service()}"
                  >
                    <div class="appt-time">{formatTime(appt.appointmentDate)}</div>
                    <div class="appt-client">
                      <User size={11}/> {appt.clientName || '—'}
                    </div>
                    <div class="appt-service">
                      <Scissors size={11}/> {appt.serviceName || '—'}
                    </div>
                  </div>
                {/each}
              </div>
            </div>
          {/each}

          {#if masters.length === 0}
            <div class="empty-state">{m.timeline_no_masters()}</div>
          {/if}
        </div>
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .timeline-page {
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    min-height: 100vh;
  }

  .page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .header-left h1 {
    font-size: 1.35rem;
    font-weight: 700;
    color: var(--clr-text, #fff);
    margin: 0;
  }

  .date-nav {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--clr-surface2, rgba(255,255,255,0.06));
    border-radius: 10px;
    padding: 0.4rem 0.75rem;
    border: 1px solid var(--clr-border, rgba(255,255,255,0.1));
  }

  .nav-btn, .refresh-btn {
    background: transparent;
    border: none;
    color: var(--clr-text, #fff);
    cursor: pointer;
    border-radius: 6px;
    padding: 0.2rem;
    display: flex;
    align-items: center;
    transition: background 0.2s;
  }
  .nav-btn:hover, .refresh-btn:hover {
    background: rgba(255,255,255,0.1);
  }

  .date-label {
    font-weight: 600;
    font-size: 0.95rem;
    color: var(--clr-text, #fff);
    min-width: 200px;
    text-align: center;
    text-transform: capitalize;
  }

  .error-banner {
    background: rgba(239,68,68,0.15);
    color: #ef4444;
    border: 1px solid rgba(239,68,68,0.3);
    border-radius: 8px;
    padding: 0.75rem 1rem;
    font-size: 0.9rem;
  }

  .loading-overlay {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    padding: 4rem;
    color: var(--clr-text-secondary, #aaa);
  }

  .spinner {
    width: 32px; height: 32px;
    border: 3px solid rgba(255,255,255,0.1);
    border-top-color: var(--clr-accent, #7c3aed);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  /* ── TIMELINE GRID ── */
  .timeline-container {
    display: flex;
    background: var(--clr-surface, rgba(255,255,255,0.04));
    border: 1px solid var(--clr-border, rgba(255,255,255,0.1));
    border-radius: 14px;
    overflow: hidden;
  }

  .time-axis {
    flex-shrink: 0;
    width: 64px;
    border-right: 1px solid var(--clr-border, rgba(255,255,255,0.08));
    background: var(--clr-surface2, rgba(0,0,0,0.2));
  }

  .time-col-header {
    height: 64px;
    border-bottom: 1px solid var(--clr-border, rgba(255,255,255,0.08));
  }

  .time-rows {
    overflow: hidden;
  }

  .time-label {
    position: absolute;
    left: 0;
    right: 0;
    display: flex;
    align-items: center;
    gap: 3px;
    font-size: 0.65rem;
    color: var(--clr-text-secondary, #888);
    padding-left: 6px;
    line-height: 1;
  }

  .masters-scroll {
    display: flex;
    overflow-x: auto;
    flex: 1;
  }

  .master-col {
    flex: 0 0 180px;
    min-width: 180px;
    border-right: 1px solid var(--clr-border, rgba(255,255,255,0.06));
  }
  .master-col:last-child {
    border-right: none;
  }

  .col-header {
    height: 64px;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0 0.75rem;
    border-bottom: 1px solid var(--clr-border, rgba(255,255,255,0.08));
    background: var(--clr-surface2, rgba(0,0,0,0.15));
    position: sticky;
    top: 0;
    z-index: 10;
  }

  .master-avatar {
    width: 32px; height: 32px;
    border-radius: 50%;
    object-fit: cover;
    flex-shrink: 0;
  }

  .master-avatar-placeholder {
    width: 32px; height: 32px;
    border-radius: 50%;
    background: rgba(124,58,237,0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--clr-accent, #7c3aed);
    flex-shrink: 0;
  }

  .master-col-name {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
  }

  .master-name {
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--clr-text, #fff);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .vacation-badge {
    font-size: 0.62rem;
    background: rgba(239,68,68,0.2);
    color: #ef4444;
    border-radius: 4px;
    padding: 1px 5px;
    font-weight: 600;
  }

  .slots-grid {
    position: relative;
  }

  .slot-cell {
    position: absolute;
    left: 0; right: 0;
    border-bottom: 1px solid rgba(255,255,255,0.04);
  }
  .slot-cell.hour-start {
    border-bottom-color: rgba(255,255,255,0.1);
  }

  /* ── APPOINTMENT BLOCK ── */
  .appointment-block {
    position: absolute;
    left: 4px; right: 4px;
    border-radius: 8px;
    padding: 4px 7px;
    background: rgba(124,58,237,0.25);
    border-left: 3px solid var(--clr-accent, #7c3aed);
    cursor: default;
    overflow: hidden;
    transition: opacity 0.2s;
    z-index: 5;
    box-sizing: border-box;
  }

  .appointment-block:hover {
    opacity: 0.85;
  }

  .appointment-block.status-completed {
    background: rgba(34,197,94,0.18);
    border-left-color: #22c55e;
  }

  .appointment-block.status-cancelled {
    background: rgba(239,68,68,0.12);
    border-left-color: #ef4444;
    opacity: 0.6;
  }

  .appt-time {
    font-size: 0.62rem;
    font-weight: 700;
    color: var(--clr-accent, #a78bfa);
    margin-bottom: 2px;
  }

  .appt-client, .appt-service {
    display: flex;
    align-items: center;
    gap: 3px;
    font-size: 0.65rem;
    color: var(--clr-text, #ddd);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .confirmed-badge {
    position: absolute;
    top: 3px; right: 5px;
    font-size: 0.65rem;
    color: #22c55e;
    font-weight: 700;
  }

  .empty-state {
    padding: 3rem;
    color: var(--clr-text-secondary, #888);
    text-align: center;
    font-size: 0.9rem;
  }
</style>

<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../lib/api';
  import { showAlert, showConfirm, hapticSuccess, hapticError, hapticWarning } from '../lib/telegram';
  import ShiftCard from '../lib/components/ShiftCard.svelte';
  import ShiftForm from '../lib/components/ShiftForm.svelte';
  import BarberServices from '../lib/components/BarberServices.svelte';
  import BarberAppointments from '../lib/components/BarberAppointments.svelte';
  import BarberReviews from '../lib/components/BarberReviews.svelte';
  import BarberProfile from '../lib/components/BarberProfile.svelte';
  import ClientHistory from '../lib/components/ClientHistory.svelte';
  import Icon from '../lib/components/Icon.svelte';

  let activeTab: 'appointments' | 'shifts' | 'services' | 'reviews' | 'profile' = 'appointments';
  let clientHistoryId: string | null = null;

  let shifts = [];
  let loading = true;
  let view: 'list' | 'form' = 'list';
  let editingShift = null;

  onMount(async () => {
    await loadShifts();
  });

  async function loadShifts() {
    loading = true;
    try {
      shifts = await apiFetch('/api/Barber/my-shift/all');
      shifts.sort((a, b) => {
        if (a.dayOfWeek !== b.dayOfWeek) {
            let aDay = a.dayOfWeek === 0 ? 7 : a.dayOfWeek;
            let bDay = b.dayOfWeek === 0 ? 7 : b.dayOfWeek;
            return aDay - bDay;
        }
        return a.startTime.localeCompare(b.startTime);
      });
    } catch (error) {
      console.error('Failed to load shifts', error);
      showAlert('Ошибка при загрузке смен');
    } finally {
      loading = false;
    }
  }
  $: groupedShifts = shifts.reduce((acc, shift) => {
    if (!acc[shift.dayOfWeek]) acc[shift.dayOfWeek] = [];
    acc[shift.dayOfWeek].push(shift);
    return acc;
  }, {});

  function openNewForm() {
    editingShift = null;
    view = 'form';
  }

  function openEditForm(event) {
    editingShift = event.detail;
    view = 'form';
  }

  function handleCancelForm() {
    view = 'list';
    editingShift = null;
  }

  async function handleSaveForm(event) {
    const formData = event.detail;
    
    try {
      if (editingShift) {
        await apiFetch(`/api/Barber/my-shift/update/${editingShift.id}`, {
          method: 'PUT',
          body: formData
        });
      } else {
        await apiFetch('/api/Barber/my-shift/add', {
          method: 'POST',
          body: formData
        });
      }
      
      hapticSuccess();
      view = 'list';
      await loadShifts();
    } catch (error) {
      hapticError();
      if (error.status === 409) {
        showAlert(error.message || 'Смена пересекается с уже существующей!');
      } else {
        showAlert('Не удалось сохранить смену');
      }
    }
  }

  function handleDeleteShift(event) {
    const shift = event.detail;
    
    hapticWarning();
    showConfirm('Вы уверены, что хотите удалить эту смену?', async (confirmed) => {
      if (confirmed) {
        try {
          await apiFetch(`/api/Barber/my-shift/delete/${shift.id}`, {
            method: 'DELETE'
          });
          hapticSuccess();
          await loadShifts();
        } catch (error) {
          hapticError();
          showAlert('Не удалось удалить смену');
        }
      }
    });
  }
  const daysOfWeek = ['Воскресенье', 'Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота'];
  function formatDayOfWeek(day: number) {
    return daysOfWeek[day] || '';
  }
</script>

<div class="dashboard">
  <div class="tabs-wrapper">
    <div class="tabs">
      <button 
        class="tab" 
        class:active={activeTab === 'appointments'} 
        on:click={() => activeTab = 'appointments'}
      >
        <span class="tab-icon"><Icon name="scissors" size={15} /></span>
        <span class="tab-label">Записи</span>
      </button>
      <button 
        class="tab" 
        class:active={activeTab === 'shifts'} 
        on:click={() => activeTab = 'shifts'}
      >
        <span class="tab-icon"><Icon name="calendar" size={15} /></span>
        <span class="tab-label">Смены</span>
      </button>
      <button 
        class="tab" 
        class:active={activeTab === 'services'} 
        on:click={() => activeTab = 'services'}
      >
        <span class="tab-icon"><Icon name="services" size={15} /></span>
        <span class="tab-label">Услуги</span>
      </button>
      <button 
        class="tab" 
        class:active={activeTab === 'reviews'} 
        on:click={() => activeTab = 'reviews'}
      >
        <span class="tab-icon"><Icon name="star" size={15} /></span>
        <span class="tab-label">Отзывы</span>
      </button>
      <button 
        class="tab" 
        class:active={activeTab === 'profile'} 
        on:click={() => activeTab = 'profile'}
      >
        <span class="tab-icon"><Icon name="user" size={15} /></span>
        <span class="tab-label">Профиль</span>
      </button>
    </div>
  </div>

  <div class="dashboard-content">
    {#if activeTab === 'shifts'}
      <div class="shifts-content">
        {#if view === 'list'}
          {#if loading}
            <div class="loading-wrap">
              <div class="spinner-sm"></div>
              <p>Загрузка смен...</p>
            </div>
          {:else if shifts.length === 0}
            <div class="empty-state">
              <div class="empty-icon-circle">
                <Icon name="calendar" size={28} color="var(--pastel-rose)" />
              </div>
              <p class="empty-title">У вас пока нет смен</p>
              <p class="empty-subtitle">Создайте свой первый рабочий график на неделю</p>
              <button class="primary-btn" on:click={openNewForm}>+ Создать смену</button>
            </div>
          {:else}
            <div class="shifts-list">
              {#each Object.entries(groupedShifts) as [day, dayShifts]}
                <div class="date-group">
                  <h3 class="date-title">{formatDayOfWeek(Number(day))}</h3>
                  {#each dayShifts as shift (shift.id)}
                    <ShiftCard 
                      {shift} 
                      on:edit={openEditForm}
                      on:delete={handleDeleteShift}
                    />
                  {/each}
                </div>
              {/each}
            </div>
          {/if}
        {:else}
          <ShiftForm 
            initialData={editingShift} 
            on:save={handleSaveForm} 
            on:cancel={handleCancelForm} 
          />
        {/if}
      </div>
      
      {#if view === 'list' && !loading}
        <button class="floating-add-btn" on:click={openNewForm} title="Добавить смену">
          <span>+</span>
        </button>
      {/if}
    {:else if activeTab === 'services'}
      <BarberServices />
    {:else if activeTab === 'appointments'}
      <BarberAppointments on:openClientHistory={(e) => { clientHistoryId = e.detail; }} />
    {:else if activeTab === 'reviews'}
      <BarberReviews />
    {:else if activeTab === 'profile'}
      <BarberProfile />
    {/if}
  </div>
</div>

{#if clientHistoryId}
  <div class="history-overlay">
    <ClientHistory
      clientId={clientHistoryId}
      on:back={() => clientHistoryId = null}
    />
  </div>
{/if}

<style>
  .dashboard {
    padding: 0 0 80px 0;
    min-height: 100vh;
    background-color: var(--bg-canvas);
  }

  .tabs-wrapper {
    position: sticky;
    top: 0;
    z-index: 100;
    padding: 10px 10px 6px;
    background: rgba(12, 14, 18, 0.85);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-bottom: 1px solid var(--border-subtle);
  }

  .tabs {
    display: flex;
    gap: 4px;
    background: rgba(23, 26, 35, 0.7);
    padding: 3px;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
    overflow-x: auto;
    scrollbar-width: none;
  }

  .tabs::-webkit-scrollbar {
    display: none;
  }

  .tab {
    flex: 1;
    min-width: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 4px;
    padding: 8px 6px;
    background: transparent;
    border: none;
    border-radius: var(--radius-pill);
    color: var(--text-secondary);
    font-weight: 600;
    font-size: 12px;
    cursor: pointer;
    transition: all 0.25s var(--ease-spring);
    white-space: nowrap;
  }

  .tab-icon {
    font-size: 14px;
  }

  .tab:hover {
    color: var(--text-primary);
  }

  .tab.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    transform: scale(1.02);
  }

  .tab:active {
    transform: scale(0.96);
  }

  .dashboard-content {
    padding: 16px;
  }

  .history-overlay {
    position: fixed;
    inset: 0;
    background: var(--bg-canvas);
    z-index: 300;
    overflow-y: auto;
    animation: fadeIn 0.25s var(--ease-spring);
  }

  .floating-add-btn {
    position: fixed;
    right: 20px;
    bottom: 24px;
    width: 54px;
    height: 54px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    font-size: 28px;
    font-weight: 400;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    box-shadow: 0 8px 24px var(--pastel-rose-glow), 0 2px 6px rgba(0,0,0,0.4);
    transition: all 0.25s var(--ease-spring);
    z-index: 90;
  }

  .floating-add-btn:hover {
    transform: scale(1.06) translateY(-2px);
    box-shadow: 0 12px 30px var(--pastel-rose-glow);
  }

  .floating-add-btn:active {
    transform: scale(0.94);
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 60px 20px;
    color: var(--text-secondary);
    gap: 12px;
  }

  .spinner-sm {
    width: 32px;
    height: 32px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 60px 24px;
    text-align: center;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-glass);
    margin: 8px 0;
  }

  .empty-icon-circle {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: var(--pastel-rose-dim);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 28px;
    margin-bottom: 16px;
    border: 1px solid rgba(223, 158, 142, 0.2);
  }

  .empty-title {
    font-size: 17px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 6px;
  }

  .empty-subtitle {
    font-size: 14px;
    color: var(--text-secondary);
    margin: 0 0 24px;
    max-width: 280px;
    line-height: 1.4;
  }

  .primary-btn {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 12px 24px;
    border-radius: var(--radius-pill);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }

  .primary-btn:active {
    transform: scale(0.96);
  }

  .date-group {
    margin-bottom: 24px;
  }

  .date-title {
    font-size: 14px;
    font-weight: 600;
    color: var(--pastel-rose);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    margin: 0 0 12px 4px;
  }
</style>

<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../lib/api';
  import { showAlert, showConfirm, hapticSuccess, hapticError, hapticWarning } from '../lib/telegram';
  import ShiftCard from '../lib/components/ShiftCard.svelte';
  import ShiftForm from '../lib/components/ShiftForm.svelte';

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
        if (a.date !== b.date) return a.date.localeCompare(b.date);
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
    if (!acc[shift.date]) acc[shift.date] = [];
    acc[shift.date].push(shift);
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
  function formatDate(dateStr: string) {
    const [year, month, day] = dateStr.split('-');
    return `${day}.${month}.${year}`;
  }
</script>

{#if view === 'list'}
  <div class="dashboard">
    <div class="header">
      <h1>Мои смены</h1>
      <button class="add-btn" on:click={openNewForm}>+</button>
    </div>

    {#if loading}
      <div class="loading">Загрузка смен...</div>
    {:else if shifts.length === 0}
      <div class="empty-state">
        <div class="icon">📅</div>
        <p>У вас пока нет добавленных смен.</p>
        <button class="primary-btn" on:click={openNewForm}>Создать первую смену</button>
      </div>
    {:else}
      <div class="shifts-list">
        {#each Object.entries(groupedShifts) as [date, dayShifts]}
          <div class="date-group">
            <h3 class="date-title">{formatDate(date)}</h3>
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
  </div>
{:else}
  <ShiftForm 
    initialData={editingShift} 
    on:save={handleSaveForm} 
    on:cancel={handleCancelForm} 
  />
{/if}

<style>
  .dashboard {
    padding: 16px;
    padding-bottom: 80px; 
  }

  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24px;
  }

  h1 {
    font-size: 24px;
    margin: 0;
  }

  .add-btn {
    background-color: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #ffffff);
    width: 40px;
    height: 40px;
    border-radius: 50%;
    border: none;
    font-size: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  }

  .loading {
    text-align: center;
    padding: 40px;
    color: var(--tg-theme-hint-color, #999);
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 40px 20px;
    text-align: center;
  }

  .empty-state .icon {
    font-size: 48px;
    margin-bottom: 16px;
  }

  .empty-state p {
    color: var(--tg-theme-hint-color, #999);
    margin-bottom: 24px;
  }

  .primary-btn {
    background-color: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #ffffff);
    border: none;
    padding: 12px 24px;
    border-radius: 8px;
    font-size: 16px;
    font-weight: 500;
    cursor: pointer;
  }

  .date-group {
    margin-bottom: 24px;
  }

  .date-title {
    font-size: 16px;
    color: var(--tg-theme-hint-color, #999);
    margin-bottom: 12px;
    font-weight: 500;
  }
</style>

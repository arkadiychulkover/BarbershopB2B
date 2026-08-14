<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { showMainButton, hideMainButton, showBackButton, hideBackButton } from '../telegram';
  
  export let initialData: any = null;
  
  const dispatch = createEventDispatcher();
  
  let dayOfWeek = 1;
  let startTime = '';
  let endTime = '';

  const daysOfWeekOptions = [
    { value: 1, label: 'Понедельник' },
    { value: 2, label: 'Вторник' },
    { value: 3, label: 'Среда' },
    { value: 4, label: 'Четверг' },
    { value: 5, label: 'Пятница' },
    { value: 6, label: 'Суббота' },
    { value: 0, label: 'Воскресенье' }
  ];

  onMount(() => {
    if (initialData) {
      dayOfWeek = initialData.dayOfWeek;
      startTime = initialData.startTime.substring(0, 5);
      endTime = initialData.endTime.substring(0, 5);
    } else {
      dayOfWeek = 1;
      startTime = '09:00';
      endTime = '18:00';
    }
    
    showMainButton('Сохранить', onSubmit);
    showBackButton(onCancel);
  });
  
  onDestroy(() => {
    hideMainButton(onSubmit);
    hideBackButton(onCancel);
  });
  $: isValid = (dayOfWeek >= 0 && dayOfWeek <= 6) && startTime && endTime && (startTime < endTime);
  import { tg } from '../telegram';
  $: {
    if (tg && tg.MainButton) {
      if (isValid) {
        tg.MainButton.enable();
        tg.MainButton.setParams({ color: tg.themeParams.button_color || '#3390ec' });
      } else {
        tg.MainButton.disable();
        tg.MainButton.setParams({ color: tg.themeParams.hint_color || '#999999' });
      }
    }
  }

  function onSubmit() {
    if (!isValid) return;
    
    const formData = {
      dayOfWeek,
      startTime: `${startTime}:00`,
      endTime: `${endTime}:00`
    };
    
    dispatch('save', formData);
  }
  
  function onCancel() {
    dispatch('cancel');
  }
</script>

<div class="shift-form">
  <h2>{initialData ? 'Редактирование смены' : 'Новая смена'}</h2>
  
  <div class="form-group">
    <label for="dayOfWeek">День недели</label>
    <select id="dayOfWeek" bind:value={dayOfWeek}>
      {#each daysOfWeekOptions as option}
        <option value={option.value}>{option.label}</option>
      {/each}
    </select>
  </div>
  
  <div class="time-inputs">
    <div class="form-group">
      <label for="startTime">Начало</label>
      <input type="time" id="startTime" bind:value={startTime} />
    </div>
    
    <div class="form-group">
      <label for="endTime">Конец</label>
      <input type="time" id="endTime" bind:value={endTime} />
    </div>
  </div>
  
  {#if startTime && endTime && startTime >= endTime}
    <p class="error-msg">Время начала должно быть раньше времени окончания</p>
  {/if}
</div>

<style>
  .shift-form {
    padding: 24px;
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    animation: fadeIn 0.3s var(--ease-spring);
  }

  h2 {
    margin-top: 0;
    margin-bottom: 24px;
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .form-group {
    display: flex;
    flex-direction: column;
    margin-bottom: 18px;
    flex: 1;
  }

  .time-inputs {
    display: flex;
    gap: 14px;
  }

  label {
    font-size: 13px;
    font-weight: 600;
    color: var(--text-secondary);
    margin-bottom: 6px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  input, select {
    background-color: var(--bg-surface-elevated);
    color: var(--text-primary);
    border: 1px solid var(--border-subtle);
    padding: 12px 14px;
    border-radius: var(--radius-md);
    font-size: 15px;
    font-family: var(--font-family);
    outline: none;
    transition: border-color 0.2s;
  }

  input:focus, select:focus {
    border-color: var(--border-active);
  }

  select {
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23a3a9bf' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 14px center;
  }
  
  .error-msg {
    color: var(--pastel-coral);
    font-size: 13px;
    margin-top: 8px;
    font-weight: 500;
  }
</style>

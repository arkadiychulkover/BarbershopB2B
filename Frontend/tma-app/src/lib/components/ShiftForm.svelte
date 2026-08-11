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
    padding: 20px;
  }

  h2 {
    margin-top: 0;
    margin-bottom: 24px;
    font-size: 20px;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    margin-bottom: 16px;
    flex: 1;
  }

  .time-inputs {
    display: flex;
    gap: 16px;
  }

  label {
    font-size: 14px;
    color: var(--tg-theme-hint-color, #999);
    margin-bottom: 6px;
  }

  input, select {
    background-color: var(--tg-theme-bg-color, #fff);
    color: var(--tg-theme-text-color, #000);
    border: 1px solid var(--tg-theme-hint-color, #ccc);
    padding: 12px;
    border-radius: 8px;
    font-size: 16px;
    outline: none;
    -webkit-appearance: none;
  }

  input:focus, select:focus {
    border-color: var(--tg-theme-button-color, #3390ec);
  }
  
  .error-msg {
    color: var(--tg-theme-destructive-text-color, #ff3b30);
    font-size: 14px;
    margin-top: 8px;
  }
</style>

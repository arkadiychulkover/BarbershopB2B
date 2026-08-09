<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { showMainButton, hideMainButton, showBackButton, hideBackButton } from '../telegram';
  
  export let initialData: any = null;
  
  const dispatch = createEventDispatcher();
  
  let date = '';
  let startTime = '';
  let endTime = '';
  const today = new Date().toISOString().split('T')[0];

  onMount(() => {
    if (initialData) {
      date = initialData.date;
      startTime = initialData.startTime.substring(0, 5);
      endTime = initialData.endTime.substring(0, 5);
    } else {
      date = today;
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
  $: isValid = date && startTime && endTime && (startTime < endTime);
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
      date,
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
    <label for="date">Дата</label>
    <input type="date" id="date" bind:value={date} min={today} />
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

  input {
    background-color: var(--tg-theme-bg-color, #fff);
    color: var(--tg-theme-text-color, #000);
    border: 1px solid var(--tg-theme-hint-color, #ccc);
    padding: 12px;
    border-radius: 8px;
    font-size: 16px;
    outline: none;
    -webkit-appearance: none;
  }

  input:focus {
    border-color: var(--tg-theme-button-color, #3390ec);
  }
  
  .error-msg {
    color: var(--tg-theme-destructive-text-color, #ff3b30);
    font-size: 14px;
    margin-top: 8px;
  }
</style>

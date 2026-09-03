<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { hideMainButton, showBackButton, hideBackButton } from '../telegram';
  import Icon from './Icon.svelte';
  import { m } from '../paraglide/messages.js';
  
  export let initialData: any = null;
  export let saving = false;
  
  const dispatch = createEventDispatcher();
  
  let dayOfWeek = 1;
  let startTime = '09:00';
  let endTime = '18:00';
  let breakDurationMinutes = 0;

  $: daysOfWeekOptions = [
    { value: 1, label: m.tma_day_mon() },
    { value: 2, label: m.tma_day_tue() },
    { value: 3, label: m.tma_day_wed() },
    { value: 4, label: m.tma_day_thu() },
    { value: 5, label: m.tma_day_fri() },
    { value: 6, label: m.tma_day_sat() },
    { value: 0, label: m.tma_day_sun() }
  ];

  $: breakOptions = [
    { value: 0, label: m.tma_break_none() },
    { value: 5, label: m.tma_break_5() },
    { value: 10, label: m.tma_break_10() },
    { value: 15, label: m.tma_break_15() },
    { value: 20, label: m.tma_break_20() },
    { value: 30, label: m.tma_break_30() }
  ];

  onMount(() => {
    // Hide Telegram bottom MainButton
    hideMainButton();

    if (initialData) {
      dayOfWeek = Number(initialData.dayOfWeek);
      startTime = initialData.startTime ? initialData.startTime.substring(0, 5) : '09:00';
      endTime = initialData.endTime ? initialData.endTime.substring(0, 5) : '18:00';
      breakDurationMinutes = initialData.breakDurationMinutes !== undefined ? Number(initialData.breakDurationMinutes) : 0;
    } else {
      dayOfWeek = 1;
      startTime = '09:00';
      endTime = '18:00';
      breakDurationMinutes = 0;
    }
    
    showBackButton(onCancel);
  });
  
  onDestroy(() => {
    hideMainButton();
    hideBackButton(onCancel);
  });

  $: isValid = (Number(dayOfWeek) >= 0 && Number(dayOfWeek) <= 6) && !!startTime && !!endTime && (startTime < endTime);

  function onSubmit() {
    if (!isValid || saving) return;
    
    const formData = {
      dayOfWeek: Number(dayOfWeek),
      startTime: `${startTime}:00`,
      endTime: `${endTime}:00`,
      breakDurationMinutes: Number(breakDurationMinutes) || 0
    };
    
    dispatch('save', formData);
  }
  
  function onCancel() {
    dispatch('cancel');
  }
</script>

<div class="shift-form">
  <div class="form-header">
    <h2>{initialData ? m.tma_shift_edit_title() : m.tma_shift_new_title()}</h2>
    <button class="close-btn" type="button" on:click={onCancel} aria-label={m.tma_close()}>
      <Icon name="x" size={18} />
    </button>
  </div>
  
  <div class="form-group">
    <label for="dayOfWeek">{m.tma_day_of_week()}</label>
    <select id="dayOfWeek" bind:value={dayOfWeek}>
      {#each daysOfWeekOptions as option}
        <option value={option.value}>{option.label}</option>
      {/each}
    </select>
  </div>
  
  <div class="time-inputs">
    <div class="form-group">
      <label for="startTime">{m.tma_start_time()}</label>
      <input type="time" id="startTime" bind:value={startTime} />
    </div>
    
    <div class="form-group">
      <label for="endTime">{m.tma_end_time()}</label>
      <input type="time" id="endTime" bind:value={endTime} />
    </div>
  </div>
  
  <div class="form-group">
    <label for="breakDuration">{m.tma_break_after_each()}</label>
    <select id="breakDuration" bind:value={breakDurationMinutes}>
      {#each breakOptions as opt}
        <option value={opt.value}>{opt.label}</option>
      {/each}
    </select>
  </div>
  
  {#if startTime && endTime && startTime >= endTime}
    <p class="error-msg">{m.tma_time_order_error()}</p>
  {/if}

  <div class="form-actions">
    <button 
      type="button" 
      class="btn-submit flex-1" 
      on:click={onSubmit} 
      disabled={!isValid || saving}
    >
      {saving ? m.tma_saving() : (initialData ? m.tma_save_changes() : m.tma_shift_add_btn())}
    </button>
    <button 
      type="button" 
      class="btn-cancel flex-1" 
      on:click={onCancel}
      disabled={saving}
    >
      {m.tma_cancel()}
    </button>
  </div>
</div>

<style>
  .shift-form {
    padding: 22px;
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    animation: fadeIn 0.25s var(--ease-spring);
    margin-bottom: 24px;
  }

  .form-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

  .form-header h2 {
    margin: 0;
    font-size: 19px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .close-btn {
    background: none;
    border: none;
    color: var(--text-secondary);
    cursor: pointer;
    padding: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: var(--radius-sm);
    transition: color 0.15s;
  }

  .close-btn:hover {
    color: var(--text-primary);
  }

  .form-group {
    display: flex;
    flex-direction: column;
    margin-bottom: 16px;
    flex: 1;
  }

  .time-inputs {
    display: flex;
    gap: 14px;
  }

  label {
    font-size: 12px;
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
    transition: border-color 0.2s, box-shadow 0.2s;
  }

  input:focus, select:focus {
    border-color: var(--border-active);
    box-shadow: 0 0 0 2px rgba(212, 165, 165, 0.15);
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
    margin-top: 4px;
    margin-bottom: 14px;
    font-weight: 500;
  }

  .form-actions {
    display: flex;
    gap: 12px;
    margin-top: 24px;
  }

  .flex-1 {
    flex: 1;
  }

  .btn-submit, .btn-cancel {
    padding: 13px 18px;
    border-radius: var(--radius-md);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: inherit;
  }

  .btn-submit {
    background: linear-gradient(135deg, var(--pastel-rose, #e0a39a), #c88777);
    color: #ffffff;
    border: none;
    box-shadow: 0 4px 14px var(--pastel-rose-glow, rgba(224, 163, 154, 0.25));
  }

  .btn-submit:hover:not(:disabled) {
    box-shadow: 0 6px 20px var(--pastel-rose-glow, rgba(224, 163, 154, 0.4));
    transform: translateY(-1px);
  }

  .btn-submit:active:not(:disabled) {
    transform: scale(0.98);
  }

  .btn-submit:disabled {
    opacity: 0.45;
    cursor: not-allowed;
    background: var(--bg-surface-elevated, #23222a);
    color: var(--text-tertiary, #888888);
    box-shadow: none;
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.08));
  }

  .btn-cancel {
    background: var(--bg-surface-elevated, #23222a);
    color: var(--text-primary, #ffffff);
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.12));
  }

  .btn-cancel:hover:not(:disabled) {
    background: var(--bg-surface, #1e1d24);
    border-color: var(--border-glass, rgba(255, 255, 255, 0.2));
  }

  .btn-cancel:active:not(:disabled) {
    transform: scale(0.98);
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(6px); }
    to { opacity: 1; transform: translateY(0); }
  }
</style>

<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  export let shift: { id: string, date: string, startTime: string, endTime: string };

  const dispatch = createEventDispatcher();
  function formatTime(timeStr: string) {
    if (!timeStr) return '';
    return timeStr.substring(0, 5);
  }
</script>

<div 
  class="shift-card" 
  role="button" 
  tabindex="0" 
  on:click={() => dispatch('edit', shift)}
  on:keydown={(e) => { if (e.key === 'Enter' || e.key === ' ') dispatch('edit', shift); }}
>
  <div class="time-range">
    <span class="time">{formatTime(shift.startTime)}</span>
    <span class="separator">—</span>
    <span class="time">{formatTime(shift.endTime)}</span>
  </div>
  
  <button class="delete-btn" on:click|stopPropagation={() => dispatch('delete', shift)}>
    Удалить
  </button>
</div>

<style>
  .shift-card {
    width: 100%;
    text-align: left;
    font-family: inherit;
    border: none;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background-color: var(--tg-theme-secondary-bg-color, #f5f5f5);
    padding: 16px;
    border-radius: 12px;
    margin-bottom: 12px;
    cursor: pointer;
    transition: opacity 0.2s;
  }

  .shift-card:active {
    opacity: 0.7;
  }

  .time-range {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 18px;
    font-weight: 500;
  }

  .separator {
    color: var(--tg-theme-hint-color, #999);
  }

  .delete-btn {
    background-color: var(--tg-theme-destructive-text-color, #ff3b30);
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
  }
</style>

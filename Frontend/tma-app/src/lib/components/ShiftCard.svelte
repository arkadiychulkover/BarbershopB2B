<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  export let shift: { id: string, dayOfWeek: number, startTime: string, endTime: string };

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
    border: 1px solid var(--border-subtle);
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    padding: 16px 18px;
    border-radius: var(--radius-lg);
    margin-bottom: 10px;
    cursor: pointer;
    box-shadow: var(--shadow-glass);
    transition: all 0.2s var(--ease-spring);
  }

  .shift-card:hover {
    border-color: var(--border-glass);
  }

  .shift-card:active {
    transform: scale(0.98);
  }

  .time-range {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 17px;
    font-weight: 700;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
  }

  .separator {
    color: var(--pastel-rose);
  }

  .delete-btn {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.2);
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s;
  }

  .delete-btn:active {
    transform: scale(0.94);
  }
</style>

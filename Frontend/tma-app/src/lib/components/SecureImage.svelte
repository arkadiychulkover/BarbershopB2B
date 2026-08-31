<script lang="ts">
  import { getFullImageUrl } from '../api';
  import Icon from './Icon.svelte';

  export let src: string = '';
  export let alt: string = '';
  export let className: string = '';
  export let style: string = '';

  let failed = false;
  $: fullUrl = getFullImageUrl(src);
  $: if (src) { failed = false; }
</script>

{#if !src || failed}
  <div class="si-error {className}" {style}>
    <Icon name="alert" size={20} color="var(--pastel-coral)" />
  </div>
{:else}
  <!-- svelte-ignore a11y-img-redundant-alt -->
  <img 
    src={fullUrl} 
    {alt} 
    class={className} 
    {style} 
    on:error={() => { failed = true; }} 
  />
{/if}

<style>
  .si-placeholder {
    background: var(--tg-theme-hint-color, #e0e0e0);
    animation: shimmer 1.2s infinite;
    border-radius: 8px;
  }
  .si-error {
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--tg-theme-secondary-bg-color, #f5f5f5);
    border-radius: 8px;
    color: var(--tg-theme-hint-color, #aaa);
    font-size: 20px;
  }
  @keyframes shimmer {
    0%   { opacity: 0.5; }
    50%  { opacity: 1; }
    100% { opacity: 0.5; }
  }
</style>

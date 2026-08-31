<script lang="ts">
  import { getFullImageUrl } from '../api';

  export let src: string = '';
  export let alt: string = '';
  export let className: string = '';
  export let style: string = '';
  export let fallbackText: string = '';

  let failed = false;
  let currentSrc = '';

  $: fullUrl = getFullImageUrl(src);
  $: if (src !== currentSrc) {
    currentSrc = src;
    failed = false;
  }
</script>

{#if !src || failed}
  <div class="si-fallback {className}" {style}>
    {#if fallbackText}
      <span class="si-initial">{fallbackText.charAt(0).toUpperCase()}</span>
    {:else if alt}
      <span class="si-initial">{alt.charAt(0).toUpperCase()}</span>
    {:else}
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
        <circle cx="12" cy="7" r="4"></circle>
      </svg>
    {/if}
  </div>
{:else}
  <!-- svelte-ignore a11y-img-redundant-alt -->
  <img 
    src={fullUrl} 
    {alt} 
    class={className} 
    {style} 
    loading="lazy"
    on:error={() => { failed = true; }} 
  />
{/if}

<style>
  .si-fallback {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, rgba(224, 163, 154, 0.3), rgba(184, 169, 201, 0.3));
    border: 1.5px solid rgba(255, 255, 255, 0.15);
    color: var(--text-primary, #ffffff);
    flex-shrink: 0;
    user-select: none;
    box-sizing: border-box;
    overflow: hidden;
  }

  .si-initial {
    font-weight: 700;
    font-size: 16px;
    line-height: 1;
    color: var(--text-primary, #ffffff);
    text-transform: uppercase;
  }
</style>

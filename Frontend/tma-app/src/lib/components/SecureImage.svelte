<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { fetchImageBlob, getFullImageUrl } from '../api';
  import Icon from './Icon.svelte';

  export let src: string = '';
  export let alt: string = '';
  export let className: string = '';
  export let style: string = '';

  let blobUrl: string | null = null;
  let loading = false;
  let failed = false;
  let currentSrc = '';

  $: fullUrl = getFullImageUrl(src);

  $: if (src && src !== currentSrc) {
    currentSrc = src;
    loadImage(src);
  }

  async function loadImage(path: string) {
    if (!path) {
      loading = false;
      failed = true;
      return;
    }
    loading = true;
    failed = false;

    if (blobUrl) {
      URL.revokeObjectURL(blobUrl);
      blobUrl = null;
    }

    try {
      blobUrl = await fetchImageBlob(path);
    } catch (err) {
      console.warn('fetchImageBlob failed, fallback to direct fullUrl', err);
      // Fallback handled in template
      failed = false;
    } finally {
      loading = false;
    }
  }

  onMount(() => {
    if (src) {
      currentSrc = src;
      loadImage(src);
    }
  });

  onDestroy(() => {
    if (blobUrl) {
      URL.revokeObjectURL(blobUrl);
      blobUrl = null;
    }
  });
</script>

{#if loading}
  <div class="si-placeholder {className}" {style}></div>
{:else if blobUrl}
  <!-- svelte-ignore a11y-img-redundant-alt -->
  <img src={blobUrl} {alt} class={className} {style} />
{:else if !failed && fullUrl}
  <!-- svelte-ignore a11y-img-redundant-alt -->
  <img 
    src={fullUrl} 
    {alt} 
    class={className} 
    {style} 
    on:error={() => { failed = true; }} 
  />
{:else}
  <div class="si-error {className}" {style}>
    <Icon name="user" size={20} color="var(--text-muted)" />
  </div>
{/if}

<style>
  .si-placeholder {
    background: rgba(255, 255, 255, 0.08);
    animation: shimmer 1.2s infinite ease-in-out;
    border-radius: inherit;
    width: 100%;
    height: 100%;
  }

  .si-error {
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255, 255, 255, 0.05);
    border-radius: inherit;
    width: 100%;
    height: 100%;
  }

  @keyframes shimmer {
    0% { opacity: 0.3; }
    50% { opacity: 0.8; }
    100% { opacity: 0.3; }
  }
</style>

<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { fetchImageBlob } from '../api';
  import Icon from './Icon.svelte';

  export let src: string;        // server path, e.g. /results/abc.jpg
  export let alt: string = '';
  export let className: string = '';
  export let style: string = '';

  let blobUrl: string | null = null;
  let loading = true;
  let failed = false;
  let lastSrc = '';

  // react to src changes
  $: if (src && src !== lastSrc) {
    lastSrc = src;
    load(src);
  }

  async function load(path: string) {
    loading = true;
    failed = false;
    // revoke previous blob to free memory
    if (blobUrl) {
      URL.revokeObjectURL(blobUrl);
      blobUrl = null;
    }
    try {
      blobUrl = await fetchImageBlob(path);
    } catch {
      failed = true;
    } finally {
      loading = false;
    }
  }

  onMount(() => { if (src) load(src); });
  onDestroy(() => { if (blobUrl) URL.revokeObjectURL(blobUrl); });
</script>

{#if loading}
  <div class="si-placeholder {className}" {style}></div>
{:else if failed || !blobUrl}
  <div class="si-error {className}" {style}>
    <Icon name="alert" size={20} color="var(--pastel-coral)" />
  </div>
{:else}
  <!-- svelte-ignore a11y-img-redundant-alt -->
  <img src={blobUrl} {alt} class={className} {style} />
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

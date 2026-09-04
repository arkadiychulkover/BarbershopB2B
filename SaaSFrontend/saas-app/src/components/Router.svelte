<script>
  import { onMount } from 'svelte';
  import { currentPath, normalizePath, push } from '../lib/router.js';

  export let routes = {};

  // Global link interception for SPA navigation:
  // Catches all clicks on internal <a> tags (like <a href="/login">)
  onMount(() => {
    function handleLinkClick(event) {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return;
      }

      // Find closest anchor tag
      const anchor = event.target.closest('a');
      if (!anchor) return;

      const href = anchor.getAttribute('href');
      if (!href) return;

      // Ignore external links, mailto, tel, target="_blank", or download
      if (
        anchor.target === '_blank' ||
        anchor.hasAttribute('download') ||
        anchor.getAttribute('rel')?.includes('external') ||
        href.startsWith('http://') ||
        href.startsWith('https://') ||
        href.startsWith('mailto:') ||
        href.startsWith('tel:')
      ) {
        return;
      }

      // Handle internal paths or legacy hashes
      if (href.startsWith('/') || href.startsWith('#/')) {
        event.preventDefault();
        push(href);
      }
    }

    document.addEventListener('click', handleLinkClick);
    return () => {
      document.removeEventListener('click', handleLinkClick);
    };
  });

  $: matchedComponent = routes[$currentPath] || routes['*'] || routes['/'] || null;
</script>

{#if matchedComponent}
  <svelte:component this={matchedComponent} />
{/if}

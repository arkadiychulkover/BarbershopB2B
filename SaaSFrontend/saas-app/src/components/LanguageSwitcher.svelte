<script>
  import { onMount } from 'svelte';
  import { currentLocale, switchLocale } from '../lib/locale.js';

  export let variant = 'pill'; // 'pill' | 'sidebar'
  export let dropUp = false;
  export let fullWidth = false;

  const languages = [
    { code: 'en', label: 'English', short: 'EN' },
    { code: 'ru', label: 'Русский', short: 'RU' }
  ];

  let open = false;
  let switcherEl;

  $: currentLang = languages.find(l => l.code === $currentLocale) || languages[0];

  function toggle() {
    open = !open;
  }

  function selectLanguage(code) {
    switchLocale(code);
    open = false;
  }

  function onWindowClick(e) {
    if (open && switcherEl && !switcherEl.contains(e.target)) {
      open = false;
    }
  }

  onMount(() => {
    window.addEventListener('click', onWindowClick);
    return () => window.removeEventListener('click', onWindowClick);
  });
</script>

<div 
  class="lang-switcher" 
  class:sidebar-variant={variant === 'sidebar'} 
  class:full-width={fullWidth} 
  bind:this={switcherEl}
>
  <button
    type="button"
    class="lang-btn"
    class:active={open}
    class:sidebar-btn={variant === 'sidebar'}
    on:click|stopPropagation={toggle}
    aria-label="Select language"
    title="Change language / Сменить язык"
  >
    <svg class="globe" viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="12" cy="12" r="10"></circle>
      <line x1="2" y1="12" x2="22" y2="12"></line>
      <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
    </svg>
    <span class="code">{currentLang.short}</span>
    <svg class="chevron" class:rotated={open} viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2.2" fill="none" stroke-linecap="round" stroke-linejoin="round">
      <polyline points="6 9 12 15 18 9"></polyline>
    </svg>
  </button>

  {#if open}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div 
      class="lang-dropdown" 
      class:drop-up={dropUp}
      on:click|stopPropagation
    >
      {#each languages as lang}
        <button
          type="button"
          class="lang-option"
          class:selected={lang.code === $currentLocale}
          on:click={() => selectLanguage(lang.code)}
        >
          <span class="label">{lang.label}</span>
          {#if lang.code === $currentLocale}
            <svg class="check" viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" stroke-width="2.5" fill="none" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="20 6 9 17 4 12"></polyline>
            </svg>
          {/if}
        </button>
      {/each}
    </div>
  {/if}
</div>

<style>
  .lang-switcher {
    position: relative;
    display: inline-block;
    z-index: 1000;
  }

  .lang-switcher.full-width {
    width: 100%;
    display: block;
  }

  .lang-btn {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    height: 34px;
    padding: 0 11px;
    border-radius: 9999px;
    background: var(--bg-surface-elevated, rgba(255, 255, 255, 0.06));
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.12));
    color: var(--text-primary, #ffffff);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    backdrop-filter: blur(12px);
    -webkit-backdrop-filter: blur(12px);
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    user-select: none;
    box-sizing: border-box;
  }

  .lang-switcher.full-width .lang-btn {
    width: 100%;
    justify-content: center;
  }

  .lang-btn.sidebar-btn {
    border-radius: var(--radius-md, 14px);
    height: 36px;
    font-size: 0.8rem;
    padding: 0 0.6rem;
    gap: 0.4rem;
  }

  .lang-btn:hover, .lang-btn.active {
    background: var(--bg-surface-hover, rgba(255, 255, 255, 0.12));
    border-color: var(--border-glass, rgba(255, 255, 255, 0.25));
    box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
    transform: translateY(-1px);
  }

  .globe {
    color: var(--pastel-rose, #df9e8e);
    opacity: 0.9;
    flex-shrink: 0;
  }

  .code {
    letter-spacing: 0.04em;
    font-weight: 700;
  }

  .chevron {
    transition: transform 0.2s ease;
    opacity: 0.7;
  }

  .chevron.rotated {
    transform: rotate(180deg);
    opacity: 1;
  }

  .lang-dropdown {
    position: absolute;
    top: calc(100% + 6px);
    right: 0;
    min-width: 145px;
    padding: 6px;
    background: var(--bg-surface-solid, #161a23);
    border: 1px solid var(--border-glass, rgba(255, 255, 255, 0.14));
    border-radius: 14px;
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.45), 0 0 0 1px var(--border-subtle, rgba(255, 255, 255, 0.05));
    animation: dropdownFadeIn 0.18s cubic-bezier(0.16, 1, 0.3, 1);
    display: flex;
    flex-direction: column;
    gap: 3px;
    z-index: 1000;
  }

  .lang-dropdown.drop-up {
    top: auto;
    bottom: calc(100% + 6px);
    animation: dropdownFadeUp 0.18s cubic-bezier(0.16, 1, 0.3, 1);
  }

  .lang-option {
    display: flex;
    align-items: center;
    gap: 9px;
    width: 100%;
    padding: 8px 10px;
    border-radius: 9px;
    background: transparent;
    border: none;
    color: var(--text-secondary, #a1a1aa);
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;
  }

  .lang-option:hover {
    background: var(--bg-surface-hover, rgba(255, 255, 255, 0.08));
    color: var(--text-primary, #ffffff);
  }

  .lang-option.selected {
    background: var(--pastel-rose-dim, rgba(223, 158, 142, 0.15));
    color: var(--pastel-rose, #df9e8e);
    font-weight: 600;
  }

  .lang-option .label {
    flex: 1;
  }

  .check {
    color: var(--pastel-rose, #e09f8f);
  }

  @keyframes dropdownFadeIn {
    from {
      opacity: 0;
      transform: translateY(-6px) scale(0.96);
    }
    to {
      opacity: 1;
      transform: translateY(0) scale(1);
    }
  }

  @keyframes dropdownFadeUp {
    from {
      opacity: 0;
      transform: translateY(6px) scale(0.96);
    }
    to {
      opacity: 1;
      transform: translateY(0) scale(1);
    }
  }
</style>

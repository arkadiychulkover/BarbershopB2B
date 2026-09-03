<script lang="ts">
  import { onMount } from 'svelte';
  import { currentLocale, switchLocale } from '../locale';

  const languages = [
    { code: 'en', label: 'English', short: 'EN', flag: '🇺🇸' },
    { code: 'ru', label: 'Русский', short: 'RU', flag: '🇷🇺' }
  ];

  let open = false;
  let switcherEl: HTMLElement;

  $: currentLang = languages.find(l => l.code === $currentLocale) || languages[0];

  function toggle() {
    open = !open;
  }

  function selectLanguage(code: string) {
    switchLocale(code);
    open = false;
  }

  function onWindowClick(e: MouseEvent) {
    if (open && switcherEl && !switcherEl.contains(e.target as Node)) {
      open = false;
    }
  }

  onMount(() => {
    window.addEventListener('click', onWindowClick);
    return () => window.removeEventListener('click', onWindowClick);
  });
</script>

<div class="lang-switcher" bind:this={switcherEl}>
  <button
    type="button"
    class="lang-btn"
    class:active={open}
    on:click|stopPropagation={toggle}
    aria-label="Select language"
  >
    <span class="flag">{currentLang.flag}</span>
    <span class="code">{currentLang.short}</span>
    <svg class="chevron" class:rotated={open} viewBox="0 0 24 24" width="12" height="12" stroke="currentColor" stroke-width="2.2" fill="none" stroke-linecap="round" stroke-linejoin="round">
      <polyline points="6 9 12 15 18 9"></polyline>
    </svg>
  </button>

  {#if open}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="lang-dropdown" on:click|stopPropagation>
      {#each languages as lang}
        <button
          type="button"
          class="lang-option"
          class:selected={lang.code === $currentLocale}
          on:click={() => selectLanguage(lang.code)}
        >
          <span class="flag">{lang.flag}</span>
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
    z-index: 1100;
  }

  .lang-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    height: 30px;
    padding: 0 10px;
    border-radius: 9999px;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.12);
    color: var(--text-primary, #ffffff);
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    transition: all 0.2s ease;
  }

  .lang-btn:hover, .lang-btn.active {
    background: rgba(255, 255, 255, 0.14);
    border-color: rgba(255, 255, 255, 0.22);
  }

  .flag {
    font-size: 13px;
    line-height: 1;
  }

  .code {
    font-weight: 700;
    letter-spacing: 0.03em;
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
    min-width: 140px;
    padding: 5px;
    background: rgba(26, 28, 36, 0.96);
    border: 1px solid rgba(255, 255, 255, 0.14);
    border-radius: 12px;
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
    animation: dropdownFadeIn 0.18s cubic-bezier(0.16, 1, 0.3, 1);
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .lang-option {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    padding: 7px 9px;
    border-radius: 8px;
    background: transparent;
    border: none;
    color: var(--text-secondary, #a1a1aa);
    font-size: 12px;
    font-weight: 500;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;
  }

  .lang-option:hover {
    background: rgba(255, 255, 255, 0.08);
    color: var(--text-primary, #ffffff);
  }

  .lang-option.selected {
    background: rgba(223, 158, 142, 0.15);
    color: var(--pastel-rose, #e09f8f);
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
      transform: translateY(-5px) scale(0.96);
    }
    to {
      opacity: 1;
      transform: translateY(0) scale(1);
    }
  }
</style>

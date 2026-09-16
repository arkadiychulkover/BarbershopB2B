<script>
  import { onMount } from 'svelte';
  import { push } from '../lib/router.js';
  import { authStore, profileStore, currentLocation } from '../lib/store';
  import { apiRequest } from '../lib/api';
  import { theme, toggleTheme } from '../lib/theme';
  import { m } from '../lib/paraglide/messages.js';
  import LanguageSwitcher from './LanguageSwitcher.svelte';
  import { 
    LayoutDashboard, 
    Settings, 
    Users, 
    BarChart3, 
    Bot, 
    LogOut, 
    Scissors, 
    CalendarDays, 
    Menu, 
    X, 
    ShieldCheck,
    UserCheck,
    Sun,
    Moon,
    HelpCircle,
    Send,
    ExternalLink
  } from 'lucide-svelte';
  
  let isLoading = true;
  let isMobileOpen = false;

  onMount(async () => {
    if (!$authStore.isAuthenticated) {
      push('/login');
      return;
    }

    try {
      const settings = await apiRequest('/api/Settings');
      profileStore.set({
        ownerId: settings.id,
        ownerName: settings.ownerName,
        barbershopName: settings.barbershopName,
        status: settings.status,
        email: settings.email || $profileStore.email || '',
        botUsername: settings.botUsername || ''
      });
      if (settings.status !== 'Active' && $currentLocation !== '/payment') {
        push('/payment');
      }
    } catch (e) {
      console.error(e);
    } finally {
      isLoading = false;
    }
  });

  function handleLogout() {
    import('../lib/store').then(({ setAuthToken }) => {
      setAuthToken(null);
      push('/login');
    });
  }

  function closeMobile() {
    isMobileOpen = false;
  }
</script>

<svelte:window on:keydown={(e) => { if (e.key === 'Escape') closeMobile(); }} />

{#if isLoading}
  <div class="loader-container">
    <div class="spinner"></div>
    <p class="loader-text">{m.common_loading()}</p>
  </div>
{:else}
  <div class="dashboard-layout">
    <!-- Mobile header bar -->
    <header class="mobile-header">
      <div class="mobile-brand">
        <span class="brand-dot"></span>
        <span class="brand-name">ARCH SYSTEM</span>
      </div>
      <div class="mobile-header-actions">
        <LanguageSwitcher />
        <button class="mobile-menu-btn" on:click={() => isMobileOpen = !isMobileOpen} aria-label={m.nav_menu()}>
          {#if isMobileOpen}
            <X size={22} />
          {:else}
            <Menu size={22} />
          {/if}
        </button>
      </div>
    </header>

    <!-- Mobile overlay -->
    {#if isMobileOpen}
      <!-- svelte-ignore a11y-click-events-have-key-events -->
      <!-- svelte-ignore a11y-no-static-element-interactions -->
      <div class="mobile-backdrop" on:click={closeMobile}></div>
    {/if}

    <!-- Sidebar -->
    <aside class="sidebar" class:mobile-open={isMobileOpen}>
      <div class="logo">
        <div class="brand-wrapper">
          <span class="brand-dot"></span>
          <h2>ARCH SYSTEM</h2>
        </div>
        <span class="badge" class:active={$profileStore.status === 'Active'}>
          <ShieldCheck size={12} />
          {$profileStore.status || 'Status'}
        </span>
      </div>

      <nav class="nav-menu">
        <a href="/dashboard" class="nav-item" class:active={$currentLocation === '/dashboard' || $currentLocation === '/dashboard/'} on:click={closeMobile}>
          <LayoutDashboard size={19} />
          <span>{m.nav_dashboard()}</span>
        </a>
        <a href="/dashboard/schedule" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/schedule')} on:click={closeMobile}>
          <CalendarDays size={19} />
          <span>{m.nav_schedule()}</span>
        </a>
        <a href="/dashboard/masters" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/masters')} on:click={closeMobile}>
          <Users size={19} />
          <span>{m.nav_masters()}</span>
        </a>
        <a href="/dashboard/clients" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/clients')} on:click={closeMobile}>
          <UserCheck size={19} />
          <span>{m.nav_clients()}</span>
        </a>
        <a href="/dashboard/services" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/services')} on:click={closeMobile}>
          <Scissors size={19} />
          <span>{m.nav_services()}</span>
        </a>
        <a href="/dashboard/statistics" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/statistics')} on:click={closeMobile}>
          <BarChart3 size={19} />
          <span>{m.nav_analytics()}</span>
        </a>
        <a href="/dashboard/bot-setup" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/bot-setup')} on:click={closeMobile}>
          <Bot size={19} />
          <span>{m.nav_bot_setup()}</span>
        </a>
        <a href="/dashboard/settings" class="nav-item" class:active={$currentLocation.startsWith('/dashboard/settings')} on:click={closeMobile}>
          <Settings size={19} />
          <span>{m.nav_settings()}</span>
        </a>
      </nav>

      <div class="sidebar-footer">
        <div class="sidebar-actions-row">
          <button class="theme-toggle-btn" on:click={toggleTheme} title={m.admin_layout_theme_toggle()}>
            {#if $theme === 'dark'}
              <Sun size={16} class="text-amber" />
              <span>{m.admin_layout_light_theme()}</span>
            {:else}
              <Moon size={16} class="text-lavender" />
              <span>{m.admin_layout_dark_theme()}</span>
            {/if}
          </button>

          <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer" class="support-btn" title={m.admin_layout_support_title()}>
            <Send size={14} />
            <span>{m.admin_layout_support()}</span>
          </a>
        </div>

        <div class="user-pill">
          <div class="user-avatar">
            {($profileStore.ownerName || $profileStore.barbershopName || $profileStore.email || 'B')[0].toUpperCase()}
          </div>
          <div class="user-info">
            <span class="user-name">{$profileStore.ownerName || $profileStore.barbershopName || 'Owner'}</span>
            <span class="user-email">{$profileStore.email || 'master@shop.com'}</span>
          </div>
        </div>
        <button class="logout-btn" on:click={handleLogout} title={m.nav_logout()}>
          <LogOut size={17} />
          <span>{m.nav_logout()}</span>
        </button>
      </div>
    </aside>

    <main class="main-content">
      <div class="desktop-top-bar">
        <LanguageSwitcher />
      </div>
      <div class="content-wrapper">
        <slot></slot>
      </div>
    </main>
  </div>
{/if}

<style>
  .loader-container {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    background-color: var(--bg-canvas);
  }
  
  .spinner {
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    width: 44px;
    height: 44px;
    animation: spinSmooth 0.85s linear infinite;
    margin-bottom: 1.25rem;
    box-shadow: 0 0 20px var(--pastel-rose-glow);
  }

  .loader-text {
    color: var(--text-secondary);
    font-size: 0.95rem;
    font-weight: 500;
  }

  .dashboard-layout {
    display: flex;
    min-height: 100vh;
    background-color: var(--bg-canvas);
  }
  
  /* Mobile Header */
  .mobile-header {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 60px;
    background: rgba(22, 26, 35, 0.88);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-bottom: 1px solid var(--border-subtle);
    padding: 0 1.25rem;
    align-items: center;
    justify-content: space-between;
    z-index: 100;
  }

  .mobile-header-actions {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .desktop-top-bar {
    display: flex;
    justify-content: flex-end;
    align-items: center;
    padding: 1.25rem 2rem 0;
    max-width: 1400px;
    margin: 0 auto;
  }

  @media (max-width: 1024px) {
    .desktop-top-bar {
      display: none;
    }
  }

  .mobile-brand {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-weight: 700;
    font-size: 1.1rem;
    color: var(--text-primary);
  }

  .mobile-menu-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    width: 38px;
    height: 38px;
    border-radius: var(--radius-sm);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .mobile-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(12, 14, 18, 0.8);
    backdrop-filter: blur(8px);
    z-index: 140;
    animation: fadeIn 0.2s ease;
  }

  /* Sidebar */
  .sidebar {
    width: 270px;
    background: var(--bg-surface);
    backdrop-filter: blur(24px);
    -webkit-backdrop-filter: blur(24px);
    border-right: 1px solid var(--border-subtle);
    display: flex;
    flex-direction: column;
    position: fixed;
    height: 100vh;
    left: 0;
    top: 0;
    z-index: 150;
    transition: transform 0.3s var(--ease-spring);
  }
  
  .logo {
    padding: 1.5rem 1.4rem;
    border-bottom: 1px solid var(--border-subtle);
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .brand-wrapper {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .brand-dot {
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-sage));
    box-shadow: 0 0 10px var(--pastel-rose-glow);
  }
  
  .logo h2 {
    font-size: 1.15rem;
    font-weight: 700;
    margin: 0;
    color: var(--text-primary);
  }
  
  .badge {
    font-size: 0.72rem;
    padding: 0.25rem 0.6rem;
    border-radius: var(--radius-pill);
    background-color: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.2);
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
  }

  .badge.active {
    background-color: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border-color: rgba(152, 193, 169, 0.25);
    box-shadow: 0 0 12px var(--pastel-sage-glow);
  }
  
  .nav-menu {
    padding: 1.25rem 0.85rem;
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    overflow-y: auto;
  }
  
  .nav-item {
    display: flex;
    align-items: center;
    padding: 0.75rem 1rem;
    color: var(--text-secondary);
    gap: 0.85rem;
    transition: all 0.22s var(--ease-spring);
    background: transparent;
    border-radius: var(--radius-md);
    font-size: 0.93rem;
    font-weight: 600;
  }
  
  .nav-item:hover {
    color: var(--text-primary);
    background-color: var(--bg-surface-hover);
    transform: translateX(3px);
  }
  
  .nav-item.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: #ffffff !important;
    font-weight: 700;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
  }

  .nav-item.active:hover {
    transform: none;
  }
  
  /* Sidebar Footer */
  .sidebar-footer {
    padding: 1.2rem 1rem;
    border-top: 1px solid var(--border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .sidebar-actions-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
  }

  .theme-toggle-btn, .support-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.4rem;
    padding: 0.55rem 0.6rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    text-decoration: none;
    transition: all 0.2s ease;
  }

  .theme-toggle-btn:hover {
    background: var(--bg-surface-hover);
    color: var(--pastel-rose);
    border-color: var(--border-glass);
  }

  .support-btn:hover {
    background: var(--bg-surface-hover);
    color: var(--pastel-sky, #8ec3df);
    border-color: var(--border-glass);
  }

  .user-pill {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.55rem 0.65rem;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
  }

  .user-avatar {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose-dim), var(--pastel-lavender-dim));
    border: 1px solid rgba(223, 158, 142, 0.3);
    color: var(--pastel-rose);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.9rem;
  }

  .user-info {
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .user-name {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .user-email {
    font-size: 0.72rem;
    color: var(--text-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  
  .logout-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.65rem;
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.2);
    border-radius: var(--radius-md);
    font-size: 0.88rem;
    font-weight: 600;
    transition: all 0.2s;
  }
  
  .logout-btn:hover {
    background: rgba(232, 130, 130, 0.2);
    border-color: var(--pastel-coral);
  }

  .logout-btn:active {
    transform: scale(0.97);
  }
  
  /* Main Content */
  .main-content {
    flex: 1;
    margin-left: 270px;
    min-height: 100vh;
    display: flex;
    flex-direction: column;
  }

  .content-wrapper {
    flex: 1;
    padding: 2.25rem 2.5rem 3rem;
    max-width: 1350px;
    width: 100%;
    margin: 0 auto;
    box-sizing: border-box;
    animation: contentFadeIn 0.3s ease-out;
  }

  @keyframes contentFadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }
  
  @media (max-width: 900px) {
    .mobile-header {
      display: flex;
    }

    .sidebar {
      transform: translateX(-100%);
      width: min(290px, 84vw);
    }

    .sidebar.mobile-open {
      transform: translateX(0);
      box-shadow: 10px 0 40px rgba(0, 0, 0, 0.7);
    }
    
    .main-content {
      margin-left: 0;
      padding-top: 60px;
    }

    .content-wrapper {
      padding: 1.5rem 1.25rem 2.5rem;
    }
  }

  @media (max-width: 640px) {
    .content-wrapper {
      padding: 1.1rem 0.9rem 2rem;
    }
  }

  @media (max-width: 480px) {
    .mobile-header {
      padding: 0 0.85rem;
    }

    .mobile-brand {
      font-size: 1rem;
      gap: 0.45rem;
    }

    .content-wrapper {
      padding: 0.9rem 0.65rem 1.75rem;
    }
  }
</style>

<script>
  import { onMount } from 'svelte';
  import { push } from '../lib/router.js';
  import { authStore, setAuthToken, currentLocation } from '../lib/store';
  import { apiRequest } from '../lib/api';
  import { theme, toggleTheme } from '../lib/theme';
  import { m } from '../lib/paraglide/messages.js';
  import LanguageSwitcher from './LanguageSwitcher.svelte';
  import { 
    LayoutDashboard, 
    Building2, 
    Users, 
    UserCheck, 
    Calendar, 
    Shield, 
    LogOut, 
    Menu, 
    X, 
    ExternalLink,
    Sparkles,
    Sun,
    Moon,
    Send
  } from 'lucide-svelte';

  export let activeSection = 'overview';
  export let onSectionChange = (sec) => {};

  let isLoading = true;
  let adminEmail = 'admin';
  let isMobileOpen = false;

  onMount(async () => {
    if (!$authStore.isAuthenticated) {
      push('/login');
      return;
    }

    try {
      const check = await apiRequest('/api/Admin/check');
      if (check && check.email) {
        adminEmail = check.email;
      }
    } catch (e) {
      console.error('Admin authorization failed:', e);
      setAuthToken(null);
      push('/login');
      return;
    } finally {
      isLoading = false;
    }
  });

  function handleLogout() {
    setAuthToken(null);
    push('/login');
  }

  function selectTab(tab) {
    if (onSectionChange) onSectionChange(tab);
    isMobileOpen = false;
  }

  function toggleMobileMenu() {
    isMobileOpen = !isMobileOpen;
  }
</script>

{#if isLoading}
  <div class="loader-container">
    <div class="spinner"></div>
    <p class="loader-text">{m.admin_layout_checking()}</p>
  </div>
{:else}
  <div class="admin-layout">
    <!-- Mobile header bar -->
    <header class="mobile-header">
      <div class="mobile-brand">
        <div class="admin-shield-icon">
          <Shield size={16} />
        </div>
        <span class="brand-name">ARCH SYSTEM <span class="badge-admin">Admin</span></span>
      </div>
      <div class="mobile-header-actions">
        <LanguageSwitcher />
        <button class="mobile-toggle-btn" on:click={toggleMobileMenu} aria-label="Toggle Navigation">
          {#if isMobileOpen}
            <X size={20} />
          {:else}
            <Menu size={20} />
          {/if}
        </button>
      </div>
    </header>

    <!-- Backdrop for mobile -->
    {#if isMobileOpen}
      <div class="sidebar-backdrop" on:click={() => isMobileOpen = false}></div>
    {/if}

    <aside class="sidebar" class:mobile-open={isMobileOpen}>
      <div class="logo">
        <div class="brand-wrapper">
          <div class="admin-shield-icon">
            <Shield size={18} />
          </div>
          <div>
            <h2>ARCH SYSTEM</h2>
            <span class="sub-brand">{m.admin_layout_control_panel()}</span>
          </div>
        </div>
        <span class="superadmin-badge">
          <Sparkles size={11} />
          <span>SUPERADMIN</span>
        </span>
      </div>

      <nav class="nav-menu">
        <button 
          class="nav-item" 
          class:active={activeSection === 'overview'} 
          on:click={() => selectTab('overview')}
        >
          <LayoutDashboard size={18} />
          <span>{m.admin_layout_summary_metrics()}</span>
        </button>

        <button 
          class="nav-item" 
          class:active={activeSection === 'owners'} 
          on:click={() => selectTab('owners')}
        >
          <Building2 size={18} />
          <span>{m.admin_layout_salons()}</span>
        </button>

        <button 
          class="nav-item" 
          class:active={activeSection === 'masters'} 
          on:click={() => selectTab('masters')}
        >
          <UserCheck size={18} />
          <span>{m.admin_layout_barbers()}</span>
        </button>

        <button 
          class="nav-item" 
          class:active={activeSection === 'clients'} 
          on:click={() => selectTab('clients')}
        >
          <Users size={18} />
          <span>{m.admin_layout_clients()}</span>
        </button>

        <button 
          class="nav-item" 
          class:active={activeSection === 'appointments'} 
          on:click={() => selectTab('appointments')}
        >
          <Calendar size={18} />
          <span>{m.admin_layout_all_appointments()}</span>
        </button>

        <button 
          class="nav-item" 
          class:active={activeSection === 'admins'} 
          on:click={() => selectTab('admins')}
        >
          <Shield size={18} />
          <span>{m.admin_layout_admins()}</span>
        </button>
      </nav>

      <div class="sidebar-footer">
        <div class="sidebar-actions-row">
          <button class="theme-toggle-btn" on:click={toggleTheme} title={m.admin_layout_theme_toggle()}>
            {#if $theme === 'dark'}
              <Sun size={15} class="text-amber" />
              <span>{m.admin_layout_light_theme()}</span>
            {:else}
              <Moon size={15} class="text-lavender" />
              <span>{m.admin_layout_dark_theme()}</span>
            {/if}
          </button>

          <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer" class="support-btn" title={m.admin_layout_support_title()}>
            <Send size={13} />
            <span>{m.admin_layout_support()}</span>
          </a>
        </div>

        <div class="admin-user-card">
          <div class="admin-avatar">
            {(adminEmail || 'A')[0].toUpperCase()}
          </div>
          <div class="admin-info">
            <span class="admin-role-title">{m.admin_layout_role_admin()}</span>
            <span class="admin-email" title={adminEmail}>{adminEmail}</span>
          </div>
        </div>

        <button class="logout-btn" on:click={handleLogout} title={m.nav_logout()}>
          <LogOut size={16} />
          <span>{m.nav_logout()}</span>
        </button>
      </div>
    </aside>

    <!-- Main Content Area -->
    <main class="main-content">
      <div class="desktop-top-bar">
        <LanguageSwitcher />
      </div>
      <slot />
    </main>
  </div>
{/if}

<style>
  .admin-layout {
    display: flex;
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
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

  .mobile-header {
    display: none;
  }

  .mobile-backdrop {
    display: none;
  }

  /* Sidebar */
  .sidebar {
    width: 270px;
    background: var(--bg-surface);
    border-right: 1px solid var(--border-subtle);
    display: flex;
    flex-direction: column;
    padding: 1.5rem 1.15rem;
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    z-index: 100;
    backdrop-filter: blur(20px);
    box-shadow: 2px 0 24px rgba(0, 0, 0, 0.35);
  }

  .logo {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    padding-bottom: 1.25rem;
    border-bottom: 1px solid var(--border-subtle);
    margin-bottom: 1.25rem;
  }

  .brand-wrapper {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .admin-shield-icon {
    width: 34px;
    height: 34px;
    border-radius: var(--radius-md);
    background: linear-gradient(135deg, rgba(223, 158, 142, 0.2), rgba(179, 183, 219, 0.2));
    border: 1px solid rgba(223, 158, 142, 0.35);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--pastel-rose);
    box-shadow: 0 0 16px var(--pastel-rose-glow);
    flex-shrink: 0;
  }

  .brand-wrapper h2 {
    font-size: 1.1rem;
    font-weight: 700;
    margin: 0;
    letter-spacing: -0.02em;
    color: var(--text-primary);
  }

  .sub-brand {
    font-size: 0.72rem;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.06em;
  }

  .superadmin-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.3rem 0.65rem;
    border-radius: var(--radius-pill);
    background: linear-gradient(135deg, rgba(223, 158, 142, 0.15), rgba(179, 183, 219, 0.15));
    border: 1px solid rgba(223, 158, 142, 0.3);
    color: var(--pastel-rose);
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    width: fit-content;
  }

  .badge-admin {
    font-size: 0.7rem;
    padding: 2px 6px;
    border-radius: var(--radius-pill);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
    margin-left: 0.3rem;
  }

  .nav-menu {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    flex: 1;
    overflow-y: auto;
  }

  .nav-item {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    padding: 0.75rem 1rem;
    border-radius: var(--radius-md);
    color: var(--text-secondary);
    font-weight: 600;
    font-size: 0.92rem;
    background: transparent;
    border: none;
    cursor: pointer;
    text-align: left;
    width: 100%;
    transition: all 0.2s var(--ease-spring);
  }

  .nav-item:hover {
    color: var(--text-primary);
    background-color: rgba(255, 255, 255, 0.04);
    transform: translateX(2px);
  }

  .nav-item.active {
    color: var(--text-inverse);
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
    font-weight: 600;
  }

  .sidebar-footer {
    padding: 1.25rem 1.4rem;
    border-top: 1px solid var(--border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .sidebar-actions-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
  }

  .theme-toggle-btn, .support-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.35rem;
    padding: 0.45rem 0.5rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-secondary);
    font-size: 0.76rem;
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

  .admin-user-card {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.65rem 0.85rem;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
  }

  .admin-avatar {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-lavender-dim), var(--pastel-rose-dim));
    border: 1px solid rgba(223, 158, 142, 0.4);
    color: var(--pastel-rose);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.85rem;
    flex-shrink: 0;
  }

  .admin-info {
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .admin-role-title {
    font-size: 0.72rem;
    color: var(--pastel-rose);
    font-weight: 600;
  }

  .admin-email {
    font-size: 0.8rem;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .logout-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.65rem 1rem;
    border-radius: var(--radius-md);
    background: rgba(242, 139, 130, 0.08);
    border: 1px solid rgba(242, 139, 130, 0.2);
    color: var(--pastel-coral);
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .logout-btn:hover {
    background: rgba(242, 139, 130, 0.16);
    transform: translateY(-1px);
  }

  /* Main Content */
  .main-content {
    flex: 1;
    margin-left: 270px;
    padding: 2.25rem;
    max-width: 1400px;
    min-width: 0;
    box-sizing: border-box;
  }

  /* Loader */
  .loader-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    gap: 1rem;
  }

  .spinner {
    width: 44px;
    height: 44px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
  }

  .loader-text {
    color: var(--text-secondary);
    font-size: 0.95rem;
  }

  /* Mobile Styles */
  @media (max-width: 900px) {
    .sidebar {
      transform: translateX(-100%);
      transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);
      width: 280px;
      z-index: 1000;
    }

    .sidebar.mobile-open {
      transform: translateX(0);
    }

    .mobile-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.9rem 1.25rem;
      background: var(--bg-surface);
      border-bottom: 1px solid var(--border-subtle);
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 99;
      backdrop-filter: blur(20px);
    }

    .mobile-brand {
      display: flex;
      align-items: center;
      gap: 0.6rem;
    }

    .mobile-menu-btn {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-md);
      color: var(--text-primary);
      padding: 0.4rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .mobile-backdrop {
      display: block;
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.65);
      backdrop-filter: blur(4px);
      z-index: 999;
    }

    .main-content {
      margin-left: 0;
      padding: 5rem 1rem 2rem;
    }
  }
</style>

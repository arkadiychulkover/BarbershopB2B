<script>
  import { apiRequest } from '../lib/api';
  import { Mail, ArrowRight, ArrowLeft, CheckCircle2, AlertCircle, Sparkles, X, Send } from 'lucide-svelte';
  import { m } from '../lib/paraglide/messages.js';
  import LanguageSwitcher from '../components/LanguageSwitcher.svelte';

  let email = '';
  let isLoading = false;
  let errorMsg = '';
  let isSubmitted = false;

  async function handleForgotPassword() {
    if (!email || !email.trim()) {
      errorMsg = m.auth_enter_email_error();
      return;
    }

    isLoading = true;
    errorMsg = '';

    try {
      await apiRequest('/api/Regestration/forgot-password', {
        method: 'POST',
        body: JSON.stringify({ email: email.trim() })
      });

      isSubmitted = true;
    } catch (err) {
      errorMsg = err.message || m.auth_generic_error();
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-top-actions">
    <LanguageSwitcher />
  </div>

  <div class="card auth-card">
    <a href="#/" class="btn-close-auth" title={m.common_close()} aria-label={m.common_close()}>
      <X size={18} />
    </a>

    <a href="#/" class="brand-header-link" title="ARCH SYSTEM">
      <div class="brand-header">
        <span class="brand-dot"></span>
        <span class="brand-name">ARCH SYSTEM</span>
      </div>
    </a>

    {#if !isSubmitted}
      <h2>{m.auth_reset_title()}</h2>
      <p class="subtitle">Enter the email associated with your barbershop</p>
      
      {#if errorMsg}
        <div class="alert alert-danger">
          <AlertCircle size={18} />
          <span>{errorMsg}</span>
        </div>
      {/if}

      <form on:submit|preventDefault={handleForgotPassword}>
        <div class="form-group">
          <label for="email">{m.auth_email_label()}</label>
          <div class="input-icon-wrap">
            <Mail size={17} class="input-icon" />
            <input 
              id="email" 
              type="email" 
              class="input has-icon" 
              bind:value={email} 
              placeholder="name@business.com" 
              required 
            />
          </div>
        </div>

        <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading || !email}>
          <span>{isLoading ? m.common_loading() : m.auth_reset_btn()}</span>
          {#if !isLoading}
            <ArrowRight size={17} />
          {/if}
        </button>
      </form>
      
      <div class="auth-links">
        <a href="#/login" class="back-link">
          <ArrowLeft size={15} />
          <span>{m.common_back()}</span>
        </a>
      </div>
    {:else}
      <div class="success-box">
        <div class="success-icon-wrap">
          <Send size={44} class="text-rose" />
        </div>
        <h2>{m.auth_check_telegram_title()}</h2>
        <p class="success-desc">
          {m.auth_check_telegram_desc()} <strong class="user-email">{email}</strong>.
        </p>
        <div class="info-banner">
          <Sparkles size={16} class="info-icon" />
          <span>{m.auth_telegram_link_expiry()}</span>
        </div>
        
        <div class="success-actions">
          <a href="#/login" class="btn btn-secondary w-full">
            <span>{m.auth_go_to_login()}</span>
          </a>
          <button type="button" class="btn-text" on:click={() => { isSubmitted = false; email = ''; }}>
            {m.auth_try_another_email()}
          </button>
        </div>
      </div>
    {/if}
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    position: relative;
    align-items: center;
    min-height: 100vh;
    padding: 1.5rem;
    background-color: var(--bg-canvas);
    background-image: 
      radial-gradient(ellipse 60% 50% at 50% 20%, rgba(223, 158, 142, 0.08), transparent 70%),
      radial-gradient(ellipse 40% 40% at 80% 80%, rgba(179, 183, 219, 0.05), transparent 70%);
  }

  .auth-top-actions {
    position: absolute;
    top: 1.5rem;
    right: 1.5rem;
    z-index: 10;
  }
  
  .auth-card {
    position: relative;
    width: 100%;
    max-width: 440px;
    padding: 2.5rem 2.25rem;
    animation: fadeIn 0.35s var(--ease-spring);
  }

  .btn-close-auth {
    position: absolute;
    top: 1.25rem;
    right: 1.25rem;
    width: 32px;
    height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    color: var(--text-muted);
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.06);
    transition: all 0.2s;
    text-decoration: none;
  }

  .btn-close-auth:hover {
    color: var(--text-primary);
    background: rgba(255, 255, 255, 0.1);
    transform: scale(1.05);
  }

  .brand-header-link {
    text-decoration: none;
    display: inline-block;
  }

  .brand-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1.5rem;
  }

  .brand-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background-color: var(--pastel-rose);
    box-shadow: 0 0 10px var(--pastel-rose-glow);
  }

  .brand-name {
    font-size: 0.875rem;
    font-weight: 700;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    color: var(--text-secondary);
  }

  h2 {
    font-size: 1.625rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    margin-bottom: 0.375rem;
    color: var(--text-primary);
  }

  .subtitle {
    font-size: 0.875rem;
    color: var(--text-muted);
    line-height: 1.5;
    margin-bottom: 1.5rem;
  }

  .form-group {
    margin-bottom: 1.25rem;
  }

  .form-group label {
    display: block;
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--text-secondary);
    margin-bottom: 0.4rem;
  }

  .input-icon-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.input-icon) {
    position: absolute;
    left: 0.875rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .input.has-icon {
    padding-left: 2.6rem;
  }

  .submit-btn {
    width: 100%;
    margin-top: 0.5rem;
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 0.5rem;
    padding: 0.8rem 1.25rem;
    font-size: 0.9375rem;
  }

  .auth-links {
    margin-top: 1.75rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--border-subtle);
    display: flex;
    justify-content: center;
    align-items: center;
    font-size: 0.875rem;
  }

  .back-link {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    color: var(--text-secondary);
    text-decoration: none;
    font-weight: 600;
    transition: color 0.2s;
  }

  .back-link:hover {
    color: var(--pastel-rose);
  }

  /* Success View */
  .success-box {
    text-align: center;
  }

  .success-icon-wrap {
    width: 68px;
    height: 68px;
    margin: 0 auto 1.25rem;
    border-radius: 50%;
    background: rgba(223, 158, 142, 0.12);
    border: 1px solid rgba(223, 158, 142, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 4px 20px var(--pastel-rose-glow);
  }

  :global(.text-rose) {
    color: var(--pastel-rose);
  }

  .success-desc {
    font-size: 0.9375rem;
    color: var(--text-secondary);
    line-height: 1.6;
    margin-bottom: 1.25rem;
  }

  .user-email {
    color: var(--pastel-rose);
    font-weight: 700;
  }

  .info-banner {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 0.75rem 1rem;
    font-size: 0.8125rem;
    color: var(--text-muted);
    display: flex;
    align-items: flex-start;
    gap: 0.6rem;
    text-align: left;
    margin-bottom: 1.5rem;
  }

  :global(.info-icon) {
    color: var(--pastel-lavender);
    flex-shrink: 0;
    margin-top: 2px;
  }

  .success-actions {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .w-full {
    width: 100%;
  }

  .btn-text {
    background: none;
    border: none;
    color: var(--text-muted);
    font-size: 0.8125rem;
    cursor: pointer;
    text-decoration: underline;
    transition: color 0.2s;
    padding: 0.25rem;
  }

  .btn-text:hover {
    color: var(--pastel-rose);
  }
</style>

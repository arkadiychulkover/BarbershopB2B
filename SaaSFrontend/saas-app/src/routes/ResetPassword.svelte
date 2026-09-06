<script>
  import { onMount } from 'svelte';
  import { apiRequest } from '../lib/api';
  import { Lock, Eye, EyeOff, CheckCircle2, AlertCircle, ArrowRight, ArrowLeft, RefreshCw, KeyRound, ShieldAlert } from 'lucide-svelte';
  import { m } from '../lib/paraglide/messages.js';
  import LanguageSwitcher from '../components/LanguageSwitcher.svelte';

  // states: 'verifying' | 'form' | 'invalid' | 'success'
  let state = 'verifying';
  let token = '';
  let maskedEmail = '';
  let verifyErrorMsg = '';

  let newPassword = '';
  let confirmPassword = '';
  let showNewPassword = false;
  let showConfirmPassword = false;

  let isSubmitting = false;
  let submitErrorMsg = '';

  onMount(async () => {
    extractTokenAndVerify();
  });

  function extractToken() {
    // 1. Check hash query: #/reset-password?token=...
    const hash = window.location.hash;
    const qIndex = hash.indexOf('?');
    if (qIndex !== -1) {
      const hashParams = new URLSearchParams(hash.substring(qIndex));
      const t = hashParams.get('token');
      if (t) return t;
    }
    // 2. Check search params: ?token=...#/reset-password
    const searchParams = new URLSearchParams(window.location.search);
    return searchParams.get('token') || '';
  }

  async function extractTokenAndVerify() {
    token = extractToken();

    if (!token) {
      state = 'invalid';
      verifyErrorMsg = m.reset_invalid_desc();
      return;
    }

    state = 'verifying';
    verifyErrorMsg = '';

    try {
      const data = await apiRequest(`/api/Regestration/verify-reset-token?token=${encodeURIComponent(token)}`);
      maskedEmail = data?.email || '';
      state = 'form';
    } catch (err) {
      state = 'invalid';
      verifyErrorMsg = err.message || m.reset_invalid_desc();
    }
  }

  async function handleResetPassword() {
    submitErrorMsg = '';

    if (!newPassword || newPassword.length < 6) {
      submitErrorMsg = m.reset_pwd_min_len();
      return;
    }

    if (newPassword !== confirmPassword) {
      submitErrorMsg = m.reset_pwd_mismatch();
      return;
    }

    isSubmitting = true;

    try {
      await apiRequest('/api/Regestration/reset-password', {
        method: 'POST',
        body: JSON.stringify({
          token: token.trim(),
          newPassword: newPassword
        })
      });

      state = 'success';
    } catch (err) {
      submitErrorMsg = err.message || m.reset_failed_error();
    } finally {
      isSubmitting = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-top-actions">
    <LanguageSwitcher />
  </div>

  <div class="card auth-card">
    <div class="brand-header">
      <span class="brand-dot"></span>
      <span class="brand-name">ARCH SYSTEM</span>
    </div>

    <!-- 1. VERIFYING STATE -->
    {#if state === 'verifying'}
      <div class="status-center-box">
        <div class="spinner-lg"></div>
        <h2>{m.reset_verifying_title()}</h2>
        <p class="subtitle">{m.reset_verifying_subtitle()}</p>
      </div>

    <!-- 2. INVALID / EXPIRED STATE -->
    {:else if state === 'invalid'}
      <div class="status-center-box">
        <div class="error-icon-wrap">
          <ShieldAlert size={40} class="text-coral" />
        </div>
        <h2>{m.reset_invalid_title()}</h2>
        <p class="error-desc">
          {verifyErrorMsg || m.reset_invalid_desc()}
        </p>

        <div class="actions-stack">
          <a href="/forgot-password" class="btn btn-primary w-full">
            <RefreshCw size={16} />
            <span>{m.reset_request_new_link()}</span>
          </a>
          <a href="/login" class="btn btn-secondary w-full">
            <ArrowLeft size={16} />
            <span>{m.reset_back_to_login()}</span>
          </a>
        </div>
      </div>

    <!-- 3. FORM STATE -->
    {:else if state === 'form'}
      <h2>{m.reset_new_password_title()}</h2>
      <p class="subtitle">
        {m.reset_new_password_subtitle()}
        {#if maskedEmail} {m.reset_for_account()} <strong class="text-rose">{maskedEmail}</strong>{/if}
      </p>

      {#if submitErrorMsg}
        <div class="alert alert-danger">
          <AlertCircle size={18} />
          <span>{submitErrorMsg}</span>
        </div>
      {/if}

      <form on:submit|preventDefault={handleResetPassword}>
        <div class="form-group">
          <label for="newPassword">{m.reset_new_password_label()}</label>
          <div class="input-icon-wrap">
            <Lock size={17} class="input-icon" />
            <input 
              id="newPassword" 
              type={showNewPassword ? 'text' : 'password'} 
              class="input has-icon has-trailing" 
              bind:value={newPassword} 
              placeholder={m.auth_password_placeholder()} 
              minlength="6"
              required 
            />
            <button 
              type="button" 
              class="btn-toggle-eye" 
              on:click={() => showNewPassword = !showNewPassword}
              tabindex="-1"
            >
              {#if showNewPassword}
                <EyeOff size={16} />
              {:else}
                <Eye size={16} />
              {/if}
            </button>
          </div>
        </div>

        <div class="form-group">
          <label for="confirmPassword">{m.reset_confirm_password_label()}</label>
          <div class="input-icon-wrap">
            <KeyRound size={17} class="input-icon" />
            <input 
              id="confirmPassword" 
              type={showConfirmPassword ? 'text' : 'password'} 
              class="input has-icon has-trailing" 
              bind:value={confirmPassword} 
              placeholder={m.reset_confirm_password_placeholder()} 
              minlength="6"
              required 
            />
            <button 
              type="button" 
              class="btn-toggle-eye" 
              on:click={() => showConfirmPassword = !showConfirmPassword}
              tabindex="-1"
            >
              {#if showConfirmPassword}
                <EyeOff size={16} />
              {:else}
                <Eye size={16} />
              {/if}
            </button>
          </div>
        </div>

        <button type="submit" class="btn btn-primary submit-btn" disabled={isSubmitting || !newPassword || !confirmPassword}>
          <span>{isSubmitting ? m.common_loading() : m.reset_btn_submit()}</span>
          {#if !isSubmitting}
            <ArrowRight size={17} />
          {/if}
        </button>
      </form>

      <div class="auth-links">
        <a href="/login" class="back-link">
          <ArrowLeft size={15} />
          <span>{m.reset_cancel_and_login()}</span>
        </a>
      </div>

    <!-- 4. SUCCESS STATE -->
    {:else if state === 'success'}
      <div class="status-center-box">
        <div class="success-icon-wrap">
          <CheckCircle2 size={44} class="text-rose" />
        </div>
        <h2>{m.reset_success_title()}</h2>
        <p class="success-desc">
          {m.reset_success_desc()}
        </p>

        <a href="/login" class="btn btn-primary w-full glow">
          <span>{m.reset_login_btn()}</span>
          <ArrowRight size={17} />
        </a>
      </div>
    {/if}
  </div>
</div>

<style>
  .auth-container {
    position: relative;
    display: flex;
    justify-content: center;
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
    width: 100%;
    max-width: 440px;
    padding: 2.5rem 2.25rem;
    animation: fadeIn 0.35s var(--ease-spring);
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

  .input.has-trailing {
    padding-right: 2.6rem;
  }

  .btn-toggle-eye {
    position: absolute;
    right: 0.75rem;
    background: none;
    border: none;
    color: var(--text-muted);
    cursor: pointer;
    padding: 0.25rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: color 0.2s;
  }

  .btn-toggle-eye:hover {
    color: var(--text-primary);
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

  /* Status and Center Views */
  .status-center-box {
    text-align: center;
    padding: 1rem 0;
  }

  .spinner-lg {
    width: 44px;
    height: 44px;
    border: 3px solid rgba(223, 158, 142, 0.2);
    border-top-color: var(--pastel-rose);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    margin: 0 auto 1.5rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }

  .error-icon-wrap {
    width: 68px;
    height: 68px;
    margin: 0 auto 1.25rem;
    border-radius: 50%;
    background: rgba(244, 114, 182, 0.12);
    border: 1px solid rgba(244, 114, 182, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  :global(.text-coral) {
    color: #f87171;
  }

  .error-desc {
    font-size: 0.9375rem;
    color: var(--text-secondary);
    line-height: 1.6;
    margin-bottom: 1.75rem;
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

  .success-desc {
    font-size: 0.9375rem;
    color: var(--text-secondary);
    line-height: 1.6;
    margin-bottom: 1.75rem;
  }

  :global(.text-rose) {
    color: var(--pastel-rose);
  }

  .actions-stack {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .w-full {
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 0.5rem;
  }

  .glow {
    box-shadow: 0 6px 20px var(--pastel-rose-glow);
  }

  @media (max-width: 480px) {
    .auth-container {
      padding: 1rem 0.75rem;
    }

    .auth-card {
      padding: 1.5rem 1.15rem;
      border-radius: var(--radius-md);
    }

    .auth-card h2 {
      font-size: 1.45rem;
    }

    .auth-top-actions {
      top: 1rem;
      right: 1rem;
    }
  }
</style>

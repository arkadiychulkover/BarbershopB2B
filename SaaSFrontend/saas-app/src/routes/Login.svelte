<script>
  import { push } from '../lib/router.js';
  import { apiRequest } from '../lib/api';
  import { setAuthToken, profileStore } from '../lib/store';
  import { Mail, Lock, AlertCircle, ArrowRight, X, ArrowLeft } from 'lucide-svelte';
  import { m } from '../lib/paraglide/messages.js';
  import LanguageSwitcher from '../components/LanguageSwitcher.svelte';

  let email = '';
  let password = '';
  let isLoading = false;
  let errorMsg = '';

  async function handleLogin() {
    isLoading = true;
    errorMsg = '';
    
    try {
      const response = await apiRequest('/api/Regestration/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });
      
      if (response && response.token) {
        setAuthToken(response.token, response.role);

        if (response.role === 'Admin') {
          push('/admin');
          return;
        }

        // Owner flow
        try {
          const settings = await apiRequest('/api/Settings');
          profileStore.set({
            ownerId: settings.id,
            ownerName: settings.ownerName,
            barbershopName: settings.barbershopName,
            status: settings.status,
            email: email
          });
          
          if (settings.status === 'Active') {
            push('/dashboard');
          } else {
            push('/payment');
          }
        } catch (settingsErr) {
          push('/dashboard');
        }
      }
    } catch (err) {
      errorMsg = err.message || m.auth_invalid_credentials();
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
    <a href="/" class="btn-close-auth" title={m.common_close()} aria-label={m.common_close()}>
      <X size={18} />
    </a>

    <a href="/" class="brand-header-link" title="ARCH SYSTEM">
      <div class="brand-header">
        <span class="brand-dot"></span>
        <span class="brand-name">ARCH SYSTEM</span>
      </div>
    </a>

    <h2>{m.auth_login_title()}</h2>
    <p class="subtitle">{m.auth_login_subtitle()}</p>
    
    {#if errorMsg}
      <div class="alert alert-danger">
        <AlertCircle size={18} />
        <span>{errorMsg}</span>
      </div>
    {/if}

    <form on:submit|preventDefault={handleLogin}>
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

      <div class="form-group">
        <div class="label-row">
          <label for="password">{m.auth_password_label()}</label>
          <a href="/forgot-password" class="forgot-pwd-link" tabindex="-1">{m.auth_forgot_password()}</a>
        </div>
        <div class="input-icon-wrap">
          <Lock size={17} class="input-icon" />
          <input 
            id="password" 
            type="password" 
            class="input has-icon" 
            bind:value={password} 
            placeholder="••••••••" 
            required 
          />
        </div>
      </div>

      <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading}>
        <span>{isLoading ? m.common_loading() : m.auth_login_title()}</span>
        {#if !isLoading}
          <ArrowRight size={17} />
        {/if}
      </button>
    </form>
    
    <div class="auth-links">
      <span>{m.auth_dont_have_account()}</span>
      <a href="/register">{m.nav_register()}</a>
    </div>

    <div class="back-home-wrap">
      <a href="/" class="back-home-link">
        <ArrowLeft size={15} />
        <span>{m.common_back()}</span>
      </a>
    </div>
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
    position: relative;
    width: 100%;
    max-width: 420px;
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
    width: 100%;
  }

  .brand-header {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    margin-bottom: 1.5rem;
  }

  .brand-dot {
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-sage));
    box-shadow: 0 0 10px var(--pastel-rose-glow);
  }

  .brand-name {
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-secondary);
    letter-spacing: -0.01em;
  }
  
  .auth-card h2 {
    text-align: center;
    font-size: 1.75rem;
    margin-bottom: 0.4rem;
  }
  
  .subtitle {
    text-align: center;
    color: var(--text-secondary);
    font-size: 0.92rem;
    margin-bottom: 2rem;
  }
  
  .form-group {
    margin-bottom: 1.35rem;
  }

  .label-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.45rem;
  }
  
  label {
    display: block;
    margin-bottom: 0;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .forgot-pwd-link {
    font-size: 0.8125rem;
    color: var(--pastel-rose);
    text-decoration: none;
    font-weight: 600;
    transition: all 0.2s;
  }

  .forgot-pwd-link:hover {
    color: var(--text-primary);
    text-decoration: underline;
  }

  .input-icon-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.input-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .input.has-icon {
    padding-left: 2.75rem;
  }
  
  .submit-btn {
    width: 100%;
    margin-top: 1.25rem;
    padding: 0.85rem 1.5rem;
    font-size: 1rem;
  }
  
  .auth-links {
    margin-top: 1.75rem;
    text-align: center;
    font-size: 0.9rem;
    color: var(--text-secondary);
    display: flex;
    justify-content: center;
    gap: 0.4rem;
  }

  .auth-links a {
    font-weight: 600;
  }

  .back-home-wrap {
    margin-top: 1.25rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--border-subtle);
    text-align: center;
  }

  .back-home-link {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    font-size: 0.85rem;
    color: var(--text-muted);
    text-decoration: none;
    transition: all 0.2s;
  }

  .back-home-link:hover {
    color: var(--text-primary);
    transform: translateX(-3px);
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

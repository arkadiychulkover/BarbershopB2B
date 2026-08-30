<script>
  import { Mail, ArrowRight, ArrowLeft, CheckCircle2, AlertCircle, Sparkles } from 'lucide-svelte';

  let email = '';
  let isLoading = false;
  let errorMsg = '';
  let isSubmitted = false;

  async function handleForgotPassword() {
    if (!email || !email.trim()) {
      errorMsg = 'Пожалуйста, введите адрес электронной почты';
      return;
    }

    isLoading = true;
    errorMsg = '';

    try {
      const res = await fetch('/api/Regestration/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim() })
      });

      const data = await res.json().catch(() => null);

      if (!res.ok) {
        throw new Error(data?.message || 'Не удалось отправить запрос на восстановление');
      }

      isSubmitted = true;
    } catch (err) {
      errorMsg = err.message || 'Произошла ошибка при отправке. Пожалуйста, повторите позже.';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card auth-card">
    <div class="brand-header">
      <span class="brand-dot"></span>
      <span class="brand-name">ARCH SYSTEM</span>
    </div>

    {#if !isSubmitted}
      <h2>Восстановление доступа</h2>
      <p class="subtitle">Укажите email, привязанный к вашему заведению, и мы вышлем ссылку для сброса пароля</p>
      
      {#if errorMsg}
        <div class="alert alert-danger">
          <AlertCircle size={18} />
          <span>{errorMsg}</span>
        </div>
      {/if}

      <form on:submit|preventDefault={handleForgotPassword}>
        <div class="form-group">
          <label for="email">Email</label>
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
          <span>{isLoading ? 'Отправка письма...' : 'Отправить ссылку для сброса'}</span>
          {#if !isLoading}
            <ArrowRight size={17} />
          {/if}
        </button>
      </form>
      
      <div class="auth-links">
        <a href="#/login" class="back-link">
          <ArrowLeft size={15} />
          <span>Вернуться ко входу</span>
        </a>
      </div>
    {:else}
      <div class="success-box">
        <div class="success-icon-wrap">
          <CheckCircle2 size={44} class="text-rose" />
        </div>
        <h2>Проверьте почту</h2>
        <p class="success-desc">
          Мы отправили письмо с кнопкой и ссылкой для сброса пароля на адрес <strong class="user-email">{email}</strong>.
        </p>
        <div class="info-banner">
          <Sparkles size={16} class="info-icon" />
          <span>Ссылка действительна в течение <strong>2 часов</strong>. Если письма нет, проверьте папку «Спам».</span>
        </div>
        
        <div class="success-actions">
          <a href="#/login" class="btn btn-secondary w-full">
            <span>Перейти к авторизации</span>
          </a>
          <button type="button" class="btn-text" on:click={() => { isSubmitted = false; email = ''; }}>
            Отправить на другой email
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
    align-items: center;
    min-height: 100vh;
    padding: 1.5rem;
    background-color: var(--bg-canvas);
    background-image: 
      radial-gradient(ellipse 60% 50% at 50% 20%, rgba(223, 158, 142, 0.08), transparent 70%),
      radial-gradient(ellipse 40% 40% at 80% 80%, rgba(179, 183, 219, 0.05), transparent 70%);
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

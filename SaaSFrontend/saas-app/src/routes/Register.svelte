<script>
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';
  import { 
    User, 
    Mail, 
    Phone, 
    Send, 
    Building2, 
    MapPin, 
    Bot, 
    Lock, 
    Globe, 
    AlertCircle, 
    CheckCircle2, 
    ArrowRight,
    X,
    ArrowLeft
  } from 'lucide-svelte';

  let formData = {
    ownerName: '',
    phoneNumber: '',
    email: '',
    telegramId: '',
    barbershopName: '',
    barbershopAddress: '',
    barbershopDescription: '',
    botToken: '',
    botUsername: '',
    timeZone: 'Europe/Kyiv',
    password: ''
  };

  let isLoading = false;
  let errorMsg = '';
  let successMsg = '';

  async function handleRegister() {
    isLoading = true;
    errorMsg = '';
    
    if (formData.phoneNumber) {
      const cleaned = formData.phoneNumber.trim().replace(/[\s\-\(\)]/g, '');
      const phoneRegex = /^\+[0-9]{1,3}[0-9]{9}$/;
      if (!phoneRegex.test(cleaned)) {
        errorMsg = 'Некорректный номер телефона. Формат: +380991234567 или +79991234567 (+, 1-3 цифры кода, 9 цифр номера)';
        isLoading = false;
        return;
      }
      formData.phoneNumber = cleaned;
    }

    try {
      await apiRequest('/api/Regestration/register', {
        method: 'POST',
        body: JSON.stringify(formData)
      });
      
      successMsg = 'Регистрация успешна! Сейчас вы будете перенаправлены на страницу входа...';
      setTimeout(() => {
        push('/login');
      }, 2000);
    } catch (err) {
      errorMsg = err.message || 'Ошибка регистрации. Проверьте данные.';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card auth-card">
    <a href="#/" class="btn-close-auth" title="Вернуться на главную" aria-label="Вернуться на главную">
      <X size={18} />
    </a>

    <a href="#/" class="brand-header-link" title="На главную">
      <div class="brand-header">
        <span class="brand-dot"></span>
        <span class="brand-name">ARCH SYSTEM</span>
      </div>
    </a>

    <h2>Создать аккаунт</h2>
    <p class="subtitle">Подключите ваше заведение к платформе онлайн-записи</p>
    
    {#if errorMsg}
      <div class="alert alert-danger">
        <AlertCircle size={18} />
        <span>{errorMsg}</span>
      </div>
    {/if}
    
    {#if successMsg}
      <div class="alert alert-success">
        <CheckCircle2 size={18} />
        <span>{successMsg}</span>
      </div>
    {/if}

    <form on:submit|preventDefault={handleRegister}>
      <div class="section-divider">
        <span>1. Личные данные владельца</span>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <label for="ownerName">Ваше имя</label>
          <div class="input-icon-wrap">
            <User size={16} class="input-icon" />
            <input id="ownerName" type="text" class="input has-icon" bind:value={formData.ownerName} placeholder="Александр" required />
          </div>
        </div>
        
        <div class="form-group">
          <label for="email">Email</label>
          <div class="input-icon-wrap">
            <Mail size={16} class="input-icon" />
            <input id="email" type="email" class="input has-icon" bind:value={formData.email} placeholder="owner@shop.com" required />
          </div>
        </div>

        <div class="form-group">
          <label for="phoneNumber">Телефон</label>
          <div class="input-icon-wrap">
            <Phone size={16} class="input-icon" />
            <input id="phoneNumber" type="tel" class="input has-icon" bind:value={formData.phoneNumber} placeholder="+380..." required />
          </div>
        </div>

        <div class="form-group">
          <label for="telegramId">Telegram ID владельца</label>
          <div class="input-icon-wrap">
            <Send size={16} class="input-icon" />
            <input id="telegramId" type="text" class="input has-icon" bind:value={formData.telegramId} placeholder="123456789 или username" required />
          </div>
        </div>
      </div>

      <div class="section-divider">
        <span>2. Информация о заведении</span>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <label for="barbershopName">Название заведения</label>
          <div class="input-icon-wrap">
            <Building2 size={16} class="input-icon" />
            <input id="barbershopName" type="text" class="input has-icon" bind:value={formData.barbershopName} placeholder="Название студии или салона" required />
          </div>
        </div>

        <div class="form-group">
          <label for="barbershopAddress">Адрес</label>
          <div class="input-icon-wrap">
            <MapPin size={16} class="input-icon" />
            <input id="barbershopAddress" type="text" class="input has-icon" bind:value={formData.barbershopAddress} placeholder="ул. Центральная, 12" required />
          </div>
        </div>
      </div>

      <div class="form-group full-width">
        <label for="barbershopDescription">Описание для клиентов (в боте)</label>
        <textarea 
          id="barbershopDescription" 
          class="input" 
          bind:value={formData.barbershopDescription} 
          rows="2" 
          placeholder="Уютное заведение в центре города с опытными мастерами..."
          required
        ></textarea>
      </div>
      
      <div class="section-divider">
        <span>3. Бот и безопасность</span>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <label for="botToken">Токен Telegram-бота (от @BotFather)</label>
          <div class="input-icon-wrap">
            <Bot size={16} class="input-icon" />
            <input id="botToken" type="text" class="input has-icon" bind:value={formData.botToken} placeholder="123456:ABC-DEF..." required />
          </div>
        </div>

        <div class="form-group">
          <label for="botUsername">Username бота (без @)</label>
          <div class="input-icon-wrap">
            <Bot size={16} class="input-icon" />
            <input id="botUsername" type="text" class="input has-icon" bind:value={formData.botUsername} placeholder="my_booking_bot" required />
          </div>
        </div>

        <div class="form-group">
          <label for="password">Пароль для входа</label>
          <div class="input-icon-wrap">
            <Lock size={16} class="input-icon" />
            <input id="password" type="password" class="input has-icon" bind:value={formData.password} placeholder="Минимум 6 символов" required minlength="6" />
          </div>
        </div>
        
        <div class="form-group">
          <label for="timeZone">Часовой пояс</label>
          <div class="input-icon-wrap">
            <Globe size={16} class="input-icon" />
            <select id="timeZone" class="input has-icon" bind:value={formData.timeZone}>
              <option value="Europe/Kyiv">Киев (UTC+2/3)</option>
              <option value="Europe/Warsaw">Варшава (UTC+1/2)</option>
              <option value="Europe/London">Лондон (UTC+0/1)</option>
              <option value="Europe/Moscow">Москва (UTC+3)</option>
            </select>
          </div>
        </div>
      </div>

      <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading}>
        <span>{isLoading ? 'Создание кабинета...' : 'Зарегистрировать заведение'}</span>
        {#if !isLoading}
          <ArrowRight size={17} />
        {/if}
      </button>
    </form>
    
    <div class="auth-links">
      <span>Уже есть аккаунт?</span>
      <a href="#/login">Войти</a>
    </div>

    <div class="back-home-wrap">
      <a href="#/" class="back-home-link">
        <ArrowLeft size={15} />
        <span>Вернуться на главную страницу</span>
      </a>
    </div>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 3rem 1.5rem;
    background-color: var(--bg-canvas);
    background-image: 
      radial-gradient(ellipse 70% 50% at 50% 10%, rgba(223, 158, 142, 0.07), transparent 70%),
      radial-gradient(ellipse 50% 50% at 85% 85%, rgba(152, 193, 169, 0.05), transparent 70%);
  }
  
  .auth-card {
    position: relative;
    width: 100%;
    max-width: 820px;
    padding: 2.75rem 2.5rem;
    animation: fadeIn 0.35s var(--ease-spring);
  }

  .btn-close-auth {
    position: absolute;
    top: 1.5rem;
    right: 1.5rem;
    width: 34px;
    height: 34px;
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
    margin-bottom: 1.25rem;
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
  }
  
  .auth-card h2 {
    text-align: center;
    font-size: 2rem;
    margin-bottom: 0.35rem;
  }
  
  .subtitle {
    text-align: center;
    color: var(--text-secondary);
    font-size: 0.95rem;
    margin-bottom: 2rem;
  }

  .section-divider {
    display: flex;
    align-items: center;
    margin: 1.5rem 0 1rem;
  }

  .section-divider span {
    font-size: 0.82rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--pastel-rose);
    background: var(--pastel-rose-dim);
    padding: 0.25rem 0.75rem;
    border-radius: var(--radius-pill);
    border: 1px solid rgba(223, 158, 142, 0.2);
  }
  
  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.15rem;
  }
  
  @media (max-width: 680px) {
    .form-grid {
      grid-template-columns: 1fr;
    }
    .auth-card {
      padding: 2rem 1.25rem;
    }
  }
  
  .form-group {
    margin-bottom: 0.75rem;
  }
  
  .full-width {
    margin-bottom: 0.75rem;
  }
  
  label {
    display: block;
    margin-bottom: 0.4rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
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

  textarea.input {
    resize: vertical;
    min-height: 70px;
  }
  
  .submit-btn {
    width: 100%;
    margin-top: 1.75rem;
    padding: 0.9rem 1.5rem;
    font-size: 1.05rem;
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
</style>

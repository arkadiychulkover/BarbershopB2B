<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { profileStore } from '../lib/store';
  import { apiRequest } from '../lib/api';
  import { 
    Bot, 
    Smartphone, 
    CheckCircle2, 
    ExternalLink, 
    Copy, 
    Check, 
    Sparkles, 
    HelpCircle, 
    AlertCircle 
  } from 'lucide-svelte';
  
  let copied = false;
  let isLoading = false;
  const tmaBaseUrl = (import.meta.env.VITE_TMA_URL || 'https://tma-app-rho.vercel.app').replace(/\/+$/, '');
  let botUsername = '';

  $: tenant = $profileStore.ownerId || '';
  $: tmaLink = `${tmaBaseUrl}/${tenant}/`;
  $: effectiveBotUser = botUsername || $profileStore.botUsername || '';

  onMount(async () => {
    if (!$profileStore.ownerId || !$profileStore.botUsername) {
      isLoading = true;
      try {
        const settings = await apiRequest('/api/Settings');
        if (settings) {
          botUsername = settings.botUsername || '';
          profileStore.update(s => ({
            ...s,
            ownerId: settings.id || s.ownerId,
            botUsername: settings.botUsername || s.botUsername,
            email: settings.email || s.email
          }));
        }
      } catch (e) {
        console.error('Failed to load settings in BotSetup:', e);
      } finally {
        isLoading = false;
      }
    } else {
      botUsername = $profileStore.botUsername || '';
    }
  });

  function copyLink() {
    navigator.clipboard.writeText(tmaLink);
    copied = true;
    setTimeout(() => copied = false, 2500);
  }
</script>

<DashboardLayout>
  <div class="bot-setup-page">
    <header class="page-header">
      <div class="header-left">
        <h1>Настройка Telegram-бота</h1>
        <p class="header-subtitle">Инструкция по подключению Telegram Mini App для онлайн-записи клиентов</p>
      </div>
      <div class="header-badge">
        <Sparkles size={15} />
        <span>3 простых шага</span>
      </div>
    </header>

    <!-- Quick Copy Banner -->
    <div class="quick-copy-card card mb-4">
      <div class="quick-copy-body">
        <div class="quick-copy-label">Ссылка для бота в @BotFather (/setmenubutton):</div>
        <div class="quick-copy-url">
          <code>{tmaLink}</code>
        </div>
      </div>
      <button class="btn-copy-main" on:click={copyLink} title="Скопировать ссылку">
        {#if copied}
          <Check size={18} class="check-icon" />
          <span>Скопировано!</span>
        {:else}
          <Copy size={18} />
          <span>Скопировать ссылку</span>
        {/if}
      </button>
    </div>

    {#if !effectiveBotUser}
      <div class="alert-box warning-box mb-4">
        <AlertCircle size={20} class="alert-icon" />
        <div class="alert-body">
          <strong>Telegram-бот еще не привязан к заведению</strong>
          <p>Укажите полученный токен и юзернейм бота в <a href="#/dashboard/settings" class="inline-link">настройках заведения</a>.</p>
        </div>
      </div>
    {:else}
      <div class="alert-box success-box mb-4">
        <CheckCircle2 size={20} class="alert-icon" />
        <div class="alert-body">
          <strong>Бот подключен: @{effectiveBotUser}</strong>
          <p>
            Прямая ссылка: 
            <a href="https://t.me/{effectiveBotUser}" target="_blank" rel="noreferrer" class="inline-link">
              t.me/{effectiveBotUser} <ExternalLink size={12} />
            </a>
          </p>
        </div>
      </div>
    {/if}

    <div class="steps-container">
      <!-- Step 1 -->
      <div class="card step-card">
        <div class="step-badge rose">1</div>
        <div class="step-content">
          <div class="step-head">
            <Bot size={22} class="step-icon rose" />
            <h3>Создайте бота в @BotFather</h3>
          </div>
          <p>
            Откройте официального бота 
            <a href="https://t.me/BotFather" target="_blank" rel="noreferrer" class="ext-link">
              @BotFather <ExternalLink size={13} />
            </a> 
            и отправьте команду <code>/newbot</code>. Придумайте имя и юзернейм (например, <code>my_barbershop_bot</code>).
            Скопируйте HTTP API Token и сохраните его в
            <a href="#/dashboard/settings" class="inline-link">настройках заведения</a>.
          </p>
        </div>
      </div>

      <!-- Step 2 -->
      <div class="card step-card">
        <div class="step-badge sage">2</div>
        <div class="step-content">
          <div class="step-head">
            <Smartphone size={22} class="step-icon sage" />
            <h3>Привяжите Mini App к кнопке меню</h3>
          </div>
          <p>
            В @BotFather отправьте команду <code>/setmenubutton</code>, выберите вашего созданного бота и отправьте эту ссылку:
          </p>
          
          <div class="code-box">
            <code>{tmaLink}</code>
            <button class="copy-btn" on:click={copyLink} title="Скопировать ссылку">
              {#if copied}
                <Check size={16} class="check-icon" />
                <span>Скопировано!</span>
              {:else}
                <Copy size={16} />
                <span>Копировать</span>
              {/if}
            </button>
          </div>
          
          <div class="hint-card">
            <span class="hint-title">Подсказка:</span> После ссылки @BotFather попросит текст для кнопки меню. Введите <strong>«Онлайн-запись»</strong> или <strong>«Записаться»</strong>.
          </div>
        </div>
      </div>

      <!-- Step 3 -->
      <div class="card step-card">
        <div class="step-badge lavender">3</div>
        <div class="step-content">
          <div class="step-head">
            <CheckCircle2 size={22} class="step-icon lavender" />
            <h3>Проверьте запуск Mini App</h3>
          </div>
          <p>
            {#if effectiveBotUser}
              Откройте вашего бота <a href="https://t.me/{effectiveBotUser}" target="_blank" rel="noreferrer" class="inline-link">@{effectiveBotUser}</a> и нажмите на кнопку меню в левом нижнем углу.
            {:else}
              Откройте вашего созданного бота в Telegram и нажмите на кнопку меню слева внизу.
            {/if}
            Откроется мобильное приложение для клиентов с онлайн-записью, списком мастеров и услуг.
          </p>
          
          <div class="support-box">
            <HelpCircle size={18} class="support-icon" />
            <span>Возникли сложности? Наша служба поддержки всегда на связи:</span>
            <a href="https://t.me/Eyed_Graff" target="_blank" rel="noreferrer" class="support-link">
              <span>Написать @Eyed_Graff</span>
              <ExternalLink size={14} />
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</DashboardLayout>

<style>
  .bot-setup-page {
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 1.75rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-left h1 {
    font-size: 2.2rem;
    margin-bottom: 0.35rem;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 1rem;
  }

  .header-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.45rem 1rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    color: var(--text-secondary);
    font-size: 0.88rem;
    font-weight: 500;
  }

  /* Quick Copy Card */
  .quick-copy-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1.25rem 1.75rem;
    background: linear-gradient(135deg, rgba(223, 158, 142, 0.08) 0%, rgba(152, 193, 169, 0.06) 100%);
    border: 1px solid rgba(223, 158, 142, 0.3);
    border-radius: var(--radius-lg);
    gap: 1.5rem;
    flex-wrap: wrap;
    max-width: 860px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
  }

  .quick-copy-body {
    flex: 1;
    min-width: 260px;
  }

  .quick-copy-label {
    font-size: 0.84rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--pastel-rose);
    margin-bottom: 0.35rem;
  }

  .quick-copy-url code {
    display: inline-block;
    font-size: 1.05rem;
    font-weight: 700;
    color: var(--text-primary);
    background: var(--bg-surface-elevated);
    padding: 0.4rem 0.85rem;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    word-break: break-all;
  }

  .btn-copy-main {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1.4rem;
    background: var(--pastel-rose);
    color: #ffffff;
    border: none;
    border-radius: var(--radius-pill);
    font-size: 0.95rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    flex-shrink: 0;
  }

  .btn-copy-main:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 20px var(--pastel-rose-glow);
    filter: brightness(1.05);
  }

  .alert-box {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 1.25rem;
    border-radius: var(--radius-md);
    font-size: 0.92rem;
    line-height: 1.5;
    max-width: 860px;
  }

  .warning-box {
    background: rgba(245, 158, 11, 0.1);
    border: 1px solid rgba(245, 158, 11, 0.3);
    color: #f59e0b;
  }

  .success-box {
    background: rgba(16, 185, 129, 0.1);
    border: 1px solid rgba(16, 185, 129, 0.3);
    color: #10b981;
  }

  .alert-icon {
    flex-shrink: 0;
    margin-top: 0.15rem;
  }

  .alert-body p {
    margin: 0.2rem 0 0;
    color: var(--text-secondary);
    font-size: 0.88rem;
  }

  .inline-link {
    color: var(--pastel-rose);
    text-decoration: underline;
    font-weight: 600;
  }

  .steps-container {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-width: 860px;
  }
  
  .step-card {
    display: flex;
    gap: 1.5rem;
    padding: 1.75rem 2rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
  }

  @media (max-width: 640px) {
    .step-card {
      flex-direction: column;
      padding: 1.25rem;
      gap: 1rem;
    }
  }
  
  .step-badge {
    width: 44px;
    height: 44px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
    font-weight: 800;
    flex-shrink: 0;
  }

  .step-badge.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
    box-shadow: 0 0 16px var(--pastel-rose-glow);
  }

  .step-badge.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.3);
    box-shadow: 0 0 16px var(--pastel-sage-glow);
  }

  .step-badge.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.3);
    box-shadow: 0 0 16px var(--pastel-lavender-glow);
  }

  .step-content {
    flex: 1;
  }

  .step-head {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.75rem;
  }

  .step-head h3 {
    font-size: 1.25rem;
    margin: 0;
  }

  :global(.step-icon.rose) { color: var(--pastel-rose); }
  :global(.step-icon.sage) { color: var(--pastel-sage); }
  :global(.step-icon.lavender) { color: var(--pastel-lavender); }
  
  .step-content p {
    color: var(--text-secondary);
    line-height: 1.6;
    margin-bottom: 1.25rem;
    font-size: 0.95rem;
  }

  .ext-link {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    font-weight: 600;
    color: var(--pastel-rose);
  }

  code {
    background: var(--bg-surface-elevated);
    padding: 0.2rem 0.5rem;
    border-radius: var(--radius-sm);
    color: var(--pastel-amber);
    font-family: monospace;
    font-size: 0.9em;
    border: 1px solid var(--border-subtle);
  }
  
  .code-box {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: var(--bg-surface-elevated);
    padding: 0.85rem 1.25rem;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    margin-bottom: 1rem;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .code-box code {
    background: transparent;
    padding: 0;
    border: none;
    color: var(--pastel-rose);
    word-break: break-all;
    font-size: 0.98rem;
    font-weight: 600;
  }

  .copy-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.5rem 1rem;
    background: rgba(223, 158, 142, 0.12);
    border: 1px solid rgba(223, 158, 142, 0.25);
    border-radius: var(--radius-pill);
    color: var(--pastel-rose);
    font-size: 0.85rem;
    font-weight: 600;
    transition: all 0.2s var(--ease-spring);
    cursor: pointer;
    flex-shrink: 0;
  }

  .copy-btn:hover {
    background: rgba(223, 158, 142, 0.22);
    transform: translateY(-1px);
  }

  :global(.check-icon) {
    color: var(--pastel-sage);
  }

  .hint-card {
    background: rgba(255, 255, 255, 0.02);
    border-left: 3px solid var(--pastel-sage);
    padding: 0.75rem 1rem;
    border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
    font-size: 0.88rem;
    color: var(--text-secondary);
  }

  .hint-title {
    color: var(--pastel-sage);
    font-weight: 700;
  }

  .support-box {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1rem 1.25rem;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    flex-wrap: wrap;
    font-size: 0.88rem;
    color: var(--text-secondary);
  }

  :global(.support-icon) {
    color: var(--pastel-lavender);
    flex-shrink: 0;
  }

  .support-link {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    color: var(--pastel-rose);
    font-weight: 600;
  }

  .mb-4 {
    margin-bottom: 1.5rem;
  }
</style>

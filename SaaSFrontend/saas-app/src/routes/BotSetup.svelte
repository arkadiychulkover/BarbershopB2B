<script>
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { profileStore } from '../lib/store';
  import { 
    Bot, 
    Smartphone, 
    CheckCircle2, 
    ExternalLink, 
    Copy, 
    Check, 
    Sparkles,
    HelpCircle 
  } from 'lucide-svelte';
  
  let copied = false;
  $: tmaLink = `https://t.me/barbershop_bot/app?startapp=${$profileStore.ownerId || 'ID'}`;

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
        <p class="header-subtitle">Инструкция по подключению Telegram Mini App для ваших клиентов</p>
      </div>
      <div class="header-badge">
        <Sparkles size={15} />
        <span>3 простых шага</span>
      </div>
    </header>

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
            Если вы ещё не сделали этого при регистрации, перейдите в официальный бот 
            <a href="https://t.me/BotFather" target="_blank" rel="noreferrer" class="ext-link">
              @BotFather <ExternalLink size={13} />
            </a> 
            и отправьте команду <code>/newbot</code>. Скопируйте полученный токен в 
            <a href="#/dashboard/settings">настройки барбершопа</a>.
          </p>
        </div>
      </div>

      <!-- Step 2 -->
      <div class="card step-card">
        <div class="step-badge sage">2</div>
        <div class="step-content">
          <div class="step-head">
            <Smartphone size={22} class="step-icon sage" />
            <h3>Настройте кнопку Menu (Web App)</h3>
          </div>
          <p>
            В @BotFather выполните команду <code>/setmenubutton</code>, выберите созданного бота и отправьте ссылку на ваше приложение:
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
            <span class="hint-title">Подсказка:</span> В качестве названия кнопки введите <strong>«Онлайн-запись»</strong> или <strong>«Записаться»</strong>.
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
            Откройте вашего бота в Telegram и нажмите на синюю кнопку меню в левом нижнем углу. Откроется интерфейс онлайн-записи со списком ваших мастеров и услуг.
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
    margin-bottom: 2.25rem;
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

  .steps-container {
    display: flex;
    flex-direction: column;
    gap: 1.75rem;
    max-width: 860px;
  }
  
  .step-card {
    display: flex;
    gap: 1.5rem;
    padding: 2rem 2.25rem;
  }

  @media (max-width: 640px) {
    .step-card {
      flex-direction: column;
      padding: 1.5rem;
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
    font-size: 0.92rem;
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
</style>

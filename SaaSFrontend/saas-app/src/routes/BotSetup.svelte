<script>
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { profileStore } from '../lib/store';
  import { CheckCircle, ExternalLink } from 'lucide-svelte';
  
  const botUsername = 'your_bot_username'; 
  const tmaLink = `https://t.me/barbershop_bot/app?startapp=${$profileStore.ownerId || 'ID'}`;
</script>

<DashboardLayout>
  <div class="bot-setup">
    <header class="page-header">
      <h1>Настройка Telegram-бота</h1>
      <p>Подключите приложение для онлайн-записи к вашему боту.</p>
    </header>

    <div class="steps-container">
      <div class="step-card">
        <div class="step-number">1</div>
        <div class="step-content">
          <h3>Создайте бота в @BotFather</h3>
          <p>Если вы ещё этого не сделали при регистрации, перейдите в <a href="https://t.me/BotFather" target="_blank" rel="noreferrer">@BotFather</a> и используйте команду <code>/newbot</code>. Скопируйте полученный токен в <a href="#/dashboard/settings">настройках</a>.</p>
        </div>
      </div>

      <div class="step-card">
        <div class="step-number">2</div>
        <div class="step-content">
          <h3>Настройте кнопку Menu (Web App)</h3>
          <p>В @BotFather отправьте команду <code>/setmenubutton</code>, выберите вашего бота, затем отправьте ссылку на ваше приложение:</p>
          <div class="code-block">
            <code>{tmaLink}</code>
          </div>
          <p class="hint">В качестве названия кнопки напишите "Онлайн-запись".</p>
        </div>
      </div>

      <div class="step-card">
        <div class="step-number">3</div>
        <div class="step-content">
          <h3>Проверьте работу</h3>
          <p>Откройте вашего бота в Telegram и нажмите кнопку "Онлайн-запись" слева от поля ввода. Должно открыться мини-приложение для клиентов.</p>
          <a href="https://t.me/your_support_bot" target="_blank" class="support-link">
            <ExternalLink size={16} />
            Нужна помощь? Напишите в поддержку
          </a>
        </div>
      </div>
    </div>
  </div>
</DashboardLayout>

<style>
  .page-header {
    margin-bottom: 2rem;
  }
  
  .steps-container {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-width: 800px;
  }
  
  .step-card {
    display: flex;
    gap: 1.5rem;
    background-color: var(--bg-secondary);
    padding: 1.5rem;
    border-radius: var(--border-radius);
    border: 1px solid var(--border-color);
  }
  
  .step-number {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background-color: var(--accent);
    color: #000;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
    font-weight: 700;
    flex-shrink: 0;
  }
  
  .step-content h3 {
    margin-bottom: 0.75rem;
  }
  
  .step-content p {
    color: var(--text-secondary);
    margin-bottom: 1rem;
  }
  
  .code-block {
    background-color: var(--bg-color);
    padding: 1rem;
    border-radius: var(--border-radius);
    font-family: monospace;
    border: 1px solid var(--border-color);
    margin-bottom: 0.5rem;
    word-break: break-all;
  }
  
  .hint {
    font-size: 0.875rem;
  }
  
  .support-link {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--accent);
    margin-top: 0.5rem;
  }
</style>

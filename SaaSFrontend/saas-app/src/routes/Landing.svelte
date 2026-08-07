<script>
  import { onMount } from 'svelte';
  import { link } from 'svelte-spa-router';
  import { Scissors, Calendar, Users, BarChart3, BellRing } from 'lucide-svelte';
  import { apiRequest } from '../lib/api';

  let price = null;

  onMount(async () => {
    try {
      const data = await apiRequest('/api/Payment/price');
      price = data.price;
    } catch(e) {
      console.error('Failed to fetch price', e);
    }
  });
</script>

<div class="landing">
  <header class="hero">
    <div class="container">
      <h1>Управляйте своим барбершопом <span>как профи</span></h1>
      <p class="subtitle">Онлайн-запись через Telegram, автоматические напоминания и аналитика в одном месте.</p>
      <a href="#/register" class="btn btn-primary btn-lg">
        {price !== null ? `Начать за ${price} TON/мес` : 'Зарегистрироваться'}
      </a>
    </div>
  </header>

  <section class="features">
    <div class="container">
      <h2>Всё необходимое для роста</h2>
      <div class="grid">
        <div class="feature-card">
          <Calendar size={32} class="icon" />
          <h3>Telegram Mini App</h3>
          <p>Ваши клиенты записываются в 3 клика прямо через вашего собственного Telegram-бота.</p>
        </div>
        <div class="feature-card">
          <BellRing size={32} class="icon" />
          <h3>Авто-напоминания</h3>
          <p>Система сама напомнит клиенту о визите, снижая количество неявок к нулю.</p>
        </div>
        <div class="feature-card">
          <Users size={32} class="icon" />
          <h3>Команда мастеров</h3>
          <p>Добавляйте мастеров, управляйте их графиком и просматривайте индивидуальную статистику.</p>
        </div>
        <div class="feature-card">
          <BarChart3 size={32} class="icon" />
          <h3>Глубокая аналитика</h3>
          <p>Понятные графики выручки и загруженности помогут принимать правильные решения.</p>
        </div>
      </div>
    </div>
  </section>

  <section class="cta">
    <div class="container">
      <h2>Готовы вывести бизнес на новый уровень?</h2>
      <a href="#/register" class="btn btn-primary btn-lg">
        {price !== null ? `Купить подписку за ${price} TON/мес` : 'Купить подписку'}
      </a>
    </div>
  </section>
</div>

<style>
  .hero {
    padding: 6rem 0;
    text-align: center;
    background: linear-gradient(to bottom, var(--bg-tertiary), var(--bg-color));
  }
  
  h1 {
    font-size: 3.5rem;
    margin-bottom: 1.5rem;
  }
  
  h1 span {
    color: var(--accent);
  }
  
  .subtitle {
    font-size: 1.25rem;
    color: var(--text-secondary);
    margin-bottom: 2.5rem;
    max-width: 600px;
    margin-left: auto;
    margin-right: auto;
  }
  
  .btn-lg {
    padding: 1rem 2.5rem;
    font-size: 1.125rem;
  }
  
  .features {
    padding: 5rem 0;
  }
  
  .features h2 {
    text-align: center;
    font-size: 2.5rem;
    margin-bottom: 3rem;
  }
  
  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 2rem;
  }
  
  .feature-card {
    background-color: var(--bg-secondary);
    padding: 2rem;
    border-radius: var(--border-radius);
    border: 1px solid var(--border-color);
  }
  
  :global(.feature-card .icon) {
    color: var(--accent);
    margin-bottom: 1rem;
  }
  
  .feature-card h3 {
    margin-bottom: 0.75rem;
  }
  
  .feature-card p {
    color: var(--text-secondary);
  }
  
  .cta {
    padding: 5rem 0;
    text-align: center;
    background-color: var(--bg-secondary);
    border-top: 1px solid var(--border-color);
  }
  
  .cta h2 {
    font-size: 2rem;
    margin-bottom: 2rem;
  }
</style>

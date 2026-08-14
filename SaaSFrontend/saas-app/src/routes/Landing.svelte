<script>
  import { onMount } from 'svelte';
  import { 
    Calendar, 
    Users, 
    BarChart3, 
    BellRing, 
    Bot, 
    Sparkles, 
    ShieldCheck, 
    ArrowRight,
    LogIn
  } from 'lucide-svelte';
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

  /**
   * Svelte action to calculate distance from viewport center
   * and dynamically adjust opacity, scale, and translateY as user scrolls towards and past elements.
   */
  function scrollReveal(node) {
    let ticking = false;

    function update() {
      if (!node) return;
      const rect = node.getBoundingClientRect();
      const windowHeight = window.innerHeight || document.documentElement.clientHeight;
      
      const elementCenter = rect.top + rect.height / 2;
      const viewportCenter = windowHeight / 2;
      const distanceFromCenter = Math.abs(elementCenter - viewportCenter);
      
      // Maximum distance before the element starts fading to minimum
      const maxRange = (windowHeight / 2) + (rect.height / 2) + 120;
      
      // Compute 0..1 ratio
      let progress = Math.max(0, Math.min(1, 1 - (distanceFromCenter / maxRange)));
      
      // Smooth curve for organic feel
      const smoothProgress = Math.min(1, Math.max(0, (progress - 0.04) / 0.72));
      
      node.style.setProperty('--scroll-progress', smoothProgress.toFixed(3));
      ticking = false;
    }

    function onScroll() {
      if (!ticking) {
        requestAnimationFrame(update);
        ticking = true;
      }
    }

    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll, { passive: true });
    
    // Initial calculation after DOM paint
    setTimeout(update, 60);

    return {
      destroy() {
        window.removeEventListener('scroll', onScroll);
        window.removeEventListener('resize', onScroll);
      }
    };
  }
</script>

<div class="landing">
  <!-- Top Navigation -->
  <nav class="top-nav">
    <div class="container nav-container">
      <div class="brand">
        <span class="brand-dot"></span>
        <span class="brand-title">BarbershopB2B</span>
      </div>
      <div class="nav-actions">
        <a href="#/login" class="btn btn-secondary btn-sm">
          <LogIn size={16} />
          <span>Войти</span>
        </a>
        <a href="#/register" class="btn btn-primary btn-sm">
          <span>Регистрация</span>
        </a>
      </div>
    </div>
  </nav>

  <!-- Hero Section -->
  <header class="hero">
    <div class="container hero-container">
      <div class="hero-badge">
        <Sparkles size={14} />
        <span>Telegram Mini App & Cloud CRM для барбершопов</span>
      </div>

      <h1 class="hero-title">
        Управляйте своим барбершопом <span class="gradient-text">как профессионал</span>
      </h1>

      <p class="subtitle">
        Онлайн-запись клиентов через фирменный Telegram-бот, гибкое управление мастерами, автоматические уведомления и глубокая финансовая аналитика.
      </p>

      <div class="hero-actions">
        <a href="#/register" class="btn btn-primary btn-lg">
          <span>{price !== null ? `Начать за ${price} TON/мес` : 'Начать бесплатно'}</span>
          <ArrowRight size={18} />
        </a>
        <a href="#/login" class="btn btn-secondary btn-lg">
          <span>Вход в панель</span>
        </a>
      </div>
    </div>
  </header>

  <!-- Features Grid -->
  <section class="features">
    <div class="container">
      <div class="section-head" use:scrollReveal>
        <h2>Всё необходимое для роста вашего бизнеса</h2>
        <p class="section-sub">Автоматизируйте рутину и сосредоточьтесь на качественном сервисе для клиентов.</p>
      </div>

      <div class="grid">
        <div class="feature-card feature-rose" use:scrollReveal>
          <div class="icon-wrap rose">
            <Bot size={28} />
          </div>
          <h3>Telegram Mini App</h3>
          <p>Клиенты записываются за 3 клика прямо внутри Telegram. Без паролей, скачивания лишних приложений и смс.</p>
        </div>

        <div class="feature-card feature-sage" use:scrollReveal>
          <div class="icon-wrap sage">
            <BellRing size={28} />
          </div>
          <h3>Авто-напоминания</h3>
          <p>Система автоматически напоминает клиентам о времени визита, снижая неявки до минимума.</p>
        </div>

        <div class="feature-card feature-lavender" use:scrollReveal>
          <div class="icon-wrap lavender">
            <Users size={28} />
          </div>
          <h3>Команда мастеров</h3>
          <p>Индивидуальные рабочие графики, управление услугами, история визитов и контроль расписания.</p>
        </div>

        <div class="feature-card feature-amber" use:scrollReveal>
          <div class="icon-wrap amber">
            <BarChart3 size={28} />
          </div>
          <h3>Глубокая аналитика</h3>
          <p>Наглядные графики выручки, средний чек, количество записей по дням, неделям и месяцам.</p>
        </div>
      </div>
    </div>
  </section>

  <!-- CTA Box with Floating Blurred Ambient Orbs -->
  <section class="cta-section">
    <div class="container">
      <div class="cta-card" use:scrollReveal>
        <!-- Animated Glowing Orbs Background -->
        <div class="ambient-glow-mesh" aria-hidden="true">
          <div class="glow-orb orb-rose"></div>
          <div class="glow-orb orb-sage"></div>
          <div class="glow-orb orb-lavender"></div>
          <div class="glow-orb orb-amber"></div>
        </div>

        <!-- Content Overlay -->
        <div class="cta-content">
          <div class="cta-badge">
            <ShieldCheck size={16} />
            <span>Быстрый старт за 2 минуты</span>
          </div>
          <h2>Готовы вывести барбершоп на новый уровень?</h2>
          <p>Подключите вашего бота и принимайте первые онлайн-записи уже сегодня.</p>
          <a href="#/register" class="btn btn-primary btn-lg">
            <span>{price !== null ? `Купить подписку (${price} TON/мес)` : 'Создать аккаунт'}</span>
            <ArrowRight size={18} />
          </a>
        </div>
      </div>
    </div>
  </section>
</div>

<style>
  .landing {
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    overflow-x: hidden;
  }

  /* Top Nav */
  .top-nav {
    position: sticky;
    top: 0;
    z-index: 50;
    background: rgba(12, 14, 18, 0.82);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-bottom: 1px solid var(--border-subtle);
    padding: 0.9rem 0;
  }

  .nav-container {
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .brand-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-sage));
    box-shadow: 0 0 12px var(--pastel-rose-glow);
  }

  .brand-title {
    font-size: 1.2rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    background: linear-gradient(135deg, #ffffff 40%, var(--pastel-rose) 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }

  .nav-actions {
    display: flex;
    gap: 0.75rem;
  }

  .btn-sm {
    padding: 0.5rem 1.1rem;
    font-size: 0.88rem;
  }

  /* Hero */
  .hero {
    padding: 6.5rem 0 4.5rem;
    text-align: center;
    position: relative;
  }

  .hero-container {
    max-width: 860px;
  }

  .hero-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4rem 1rem;
    border-radius: var(--radius-pill);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.25);
    font-size: 0.85rem;
    font-weight: 600;
    margin-bottom: 2rem;
    box-shadow: 0 0 20px var(--pastel-rose-glow);
    animation: fadeIn 0.5s ease-out;
  }

  .hero-title {
    font-size: 3.6rem;
    font-weight: 800;
    margin-bottom: 1.5rem;
    letter-spacing: -0.03em;
    line-height: 1.15;
    animation: fadeIn 0.6s ease-out;
  }

  .gradient-text {
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-amber));
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }

  .subtitle {
    font-size: 1.2rem;
    color: var(--text-secondary);
    margin-bottom: 2.75rem;
    line-height: 1.6;
    max-width: 680px;
    margin-left: auto;
    margin-right: auto;
    animation: fadeIn 0.7s ease-out;
  }

  .hero-actions {
    display: flex;
    justify-content: center;
    gap: 1.25rem;
    flex-wrap: wrap;
    animation: fadeIn 0.8s ease-out;
  }

  .btn-lg {
    padding: 0.95rem 2rem;
    font-size: 1.05rem;
  }

  /* Features */
  .features {
    padding: 5rem 0 4rem;
  }

  .section-head {
    text-align: center;
    margin-bottom: 3.5rem;
    max-width: 620px;
    margin-left: auto;
    margin-right: auto;
    
    /* Scroll reveal dynamic state */
    --p: var(--scroll-progress, 0.9);
    opacity: calc(0.2 + 0.8 * var(--p));
    transform: translateY(calc(24px * (1 - var(--p))));
    transition: opacity 0.15s ease-out, transform 0.15s ease-out;
    will-change: opacity, transform;
  }

  .section-head h2 {
    font-size: 2.3rem;
    margin-bottom: 0.75rem;
  }

  .section-sub {
    font-size: 1.05rem;
    color: var(--text-secondary);
  }

  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    gap: 1.75rem;
  }

  /* Feature Card with Dynamic Scroll Reveal & Exit */
  .feature-card {
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    padding: 2.25rem 2rem;
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    position: relative;

    /* Dynamic scroll-driven opacity and scale */
    --p: var(--scroll-progress, 0.85);
    opacity: calc(0.18 + 0.82 * var(--p));
    transform: translateY(calc(34px * (1 - var(--p)))) scale(calc(0.93 + 0.07 * var(--p)));
    transition: opacity 0.12s ease-out, transform 0.12s ease-out, border-color 0.25s, box-shadow 0.25s;
    will-change: opacity, transform;
  }

  .feature-card:hover {
    transform: translateY(-5px) scale(1.02) !important;
    border-color: var(--border-glass);
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.55);
    opacity: 1 !important;
  }

  .icon-wrap {
    width: 58px;
    height: 58px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 1.5rem;
  }

  .icon-wrap.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.25);
    box-shadow: 0 0 16px var(--pastel-rose-glow);
  }

  .icon-wrap.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
    box-shadow: 0 0 16px var(--pastel-sage-glow);
  }

  .icon-wrap.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.25);
    box-shadow: 0 0 16px var(--pastel-lavender-glow);
  }

  .icon-wrap.amber {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
    box-shadow: 0 0 16px var(--pastel-amber-glow);
  }

  .feature-card h3 {
    font-size: 1.25rem;
    margin-bottom: 0.75rem;
  }

  .feature-card p {
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.55;
  }

  /* CTA Section */
  .cta-section {
    padding: 4rem 0 6.5rem;
  }

  .cta-card {
    background: linear-gradient(135deg, rgba(26, 31, 43, 0.82), rgba(16, 19, 27, 0.85));
    backdrop-filter: blur(28px);
    -webkit-backdrop-filter: blur(28px);
    border: 1px solid var(--border-glass);
    border-radius: var(--radius-xl);
    padding: 4.5rem 2.5rem;
    text-align: center;
    max-width: 860px;
    margin: 0 auto;
    box-shadow: var(--shadow-lg);
    position: relative;
    overflow: hidden;

    /* Dynamic scroll-driven opacity and scale */
    --p: var(--scroll-progress, 0.85);
    opacity: calc(0.2 + 0.8 * var(--p));
    transform: translateY(calc(38px * (1 - var(--p)))) scale(calc(0.93 + 0.07 * var(--p)));
    transition: opacity 0.15s ease-out, transform 0.15s ease-out;
    will-change: opacity, transform;
  }

  /* Animated Glowing Light Orbs Mesh */
  .ambient-glow-mesh {
    position: absolute;
    inset: 0;
    overflow: hidden;
    border-radius: var(--radius-xl);
    pointer-events: none;
    z-index: 0;
  }

  .glow-orb {
    position: absolute;
    border-radius: 50%;
    filter: blur(65px);
    opacity: 0.65;
    mix-blend-mode: screen;
    will-change: transform;
  }

  .orb-rose {
    width: 290px;
    height: 290px;
    background: radial-gradient(circle, rgba(223, 158, 142, 0.7) 0%, rgba(223, 158, 142, 0) 70%);
    top: -20%;
    left: 10%;
    animation: orbFloat1 13s ease-in-out infinite alternate;
  }

  .orb-sage {
    width: 320px;
    height: 320px;
    background: radial-gradient(circle, rgba(152, 193, 169, 0.6) 0%, rgba(152, 193, 169, 0) 70%);
    bottom: -25%;
    right: 12%;
    animation: orbFloat2 16s ease-in-out infinite alternate;
  }

  .orb-lavender {
    width: 260px;
    height: 260px;
    background: radial-gradient(circle, rgba(179, 183, 219, 0.55) 0%, rgba(179, 183, 219, 0) 70%);
    top: 30%;
    right: 25%;
    animation: orbFloat3 19s ease-in-out infinite alternate;
  }

  .orb-amber {
    width: 240px;
    height: 240px;
    background: radial-gradient(circle, rgba(229, 190, 138, 0.5) 0%, rgba(229, 190, 138, 0) 70%);
    bottom: 15%;
    left: 20%;
    animation: orbFloat4 14s ease-in-out infinite alternate;
  }

  @keyframes orbFloat1 {
    0% {
      transform: translate(0, 0) scale(1);
    }
    50% {
      transform: translate(110px, 50px) scale(1.18);
    }
    100% {
      transform: translate(50px, -45px) scale(0.92);
    }
  }

  @keyframes orbFloat2 {
    0% {
      transform: translate(0, 0) scale(1);
    }
    50% {
      transform: translate(-110px, -60px) scale(1.22);
    }
    100% {
      transform: translate(-45px, 45px) scale(0.88);
    }
  }

  @keyframes orbFloat3 {
    0% {
      transform: translate(0, 0) scale(0.92);
    }
    50% {
      transform: translate(-100px, 70px) scale(1.25);
    }
    100% {
      transform: translate(35px, -50px) scale(1.05);
    }
  }

  @keyframes orbFloat4 {
    0% {
      transform: translate(0, 0) scale(1.12);
    }
    50% {
      transform: translate(75px, -45px) scale(0.95);
    }
    100% {
      transform: translate(-55px, 60px) scale(1.2);
    }
  }

  .cta-content {
    position: relative;
    z-index: 1;
  }

  .cta-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    color: var(--pastel-sage);
    font-size: 0.85rem;
    font-weight: 600;
    margin-bottom: 1.25rem;
  }

  .cta-card h2 {
    font-size: 2.2rem;
    margin-bottom: 1rem;
  }

  .cta-card p {
    color: var(--text-secondary);
    font-size: 1.1rem;
    margin-bottom: 2.5rem;
    max-width: 540px;
    margin-left: auto;
    margin-right: auto;
  }

  @media (max-width: 768px) {
    .hero-title {
      font-size: 2.5rem;
    }
    .cta-card {
      padding: 3rem 1.5rem;
    }
  }
</style>

<script>
  import { onMount } from "svelte";
  import {
    Calendar,
    Users,
    BarChart3,
    BellRing,
    Bot,
    Sparkles,
    ShieldCheck,
    ArrowRight,
    LogIn,
    Sun,
    Moon,
    Send,
    Scissors,
    Sparkle,
    Smile,
    Heart,
    Check,
    Coins,
    ChevronDown,
    HelpCircle,
    Calculator,
  } from "lucide-svelte";
  import miniappMockup from "../assets/miniapp_mockup.jpg";
  import { apiRequest } from "../lib/api";
  import { theme, toggleTheme } from "../lib/theme"; //
  import { m } from "../lib/paraglide/messages.js";
  import LanguageSwitcher from "../components/LanguageSwitcher.svelte";
  import RoiCalculator from "../components/RoiCalculator.svelte";

  let price = null;
  let activeFaq = null;

  function toggleFaq(index) {
    activeFaq = activeFaq === index ? null : index;
  }

  $: faqs = [
    {
      q: m.landing_faq_q1(),
      a: m.landing_faq_a1(),
    },
    {
      q: m.landing_faq_q2(),
      a: m.landing_faq_a2(),
    },
    {
      q: m.landing_faq_q3(),
      a: m.landing_faq_a3(),
    },
    {
      q: m.landing_faq_q4(),
      a: m.landing_faq_a4(),
    },
    {
      q: m.landing_faq_q5(),
      a: m.landing_faq_a5(),
    },
    {
      q: m.landing_faq_q6(),
      a: m.landing_faq_a6(),
    },
  ];

  onMount(async () => {
    try {
      const data = await apiRequest("/api/Payment/price");
      price = data.price;
    } catch (e) {
      console.error("Failed to fetch price", e);
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
      const windowHeight =
        window.innerHeight || document.documentElement.clientHeight;

      const elementCenter = rect.top + rect.height / 2;
      const viewportCenter = windowHeight / 2;
      const distanceFromCenter = Math.abs(elementCenter - viewportCenter);

      // Maximum distance before the element starts fading to minimum
      const maxRange = windowHeight / 2 + rect.height / 2 + 120;

      // Compute 0..1 ratio
      let progress = Math.max(
        0,
        Math.min(1, 1 - distanceFromCenter / maxRange),
      );

      // Smooth curve for organic feel
      const smoothProgress = Math.min(1, Math.max(0, (progress - 0.04) / 0.72));

      node.style.setProperty("--scroll-progress", smoothProgress.toFixed(3));
      ticking = false;
    }

    function onScroll() {
      if (!ticking) {
        requestAnimationFrame(update);
        ticking = true;
      }
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });

    // Initial calculation after DOM paint
    setTimeout(update, 60);

    return {
      destroy() {
        window.removeEventListener("scroll", onScroll);
        window.removeEventListener("resize", onScroll);
      },
    };
  }
</script>

<div class="landing">
  <!-- Top Navigation -->
  <nav class="top-nav">
    <div class="container nav-container">
      <div class="brand">
        <span class="brand-dot"></span>
        <span class="brand-title">ARCH SYSTEM</span>
      </div>
      <div class="nav-actions">
        <LanguageSwitcher />
        <button
          class="btn btn-secondary btn-sm theme-btn"
          on:click={toggleTheme}
          title={m.admin_layout_theme_toggle()}
        >
          {#if $theme === "dark"}
            <Sun size={16} class="text-amber" />
          {:else}
            <Moon size={16} class="text-lavender" />
          {/if}
        </button>
        <a
          href="https://t.me/Eyed_Graff"
          target="_blank"
          rel="noopener noreferrer"
          class="btn btn-secondary btn-sm"
          title={m.admin_layout_support_title()}
        >
          <Send size={14} />
          <span>{m.admin_layout_support()}</span>
        </a>
        <a
          href="#calculator"
          class="btn btn-secondary btn-sm nav-calc-link"
          title={m.nav_calculator()}
        >
          <Calculator size={14} />
          <span>{m.nav_calculator()}</span>
        </a>
        <a href="/login" class="btn btn-secondary btn-sm">
          <LogIn size={16} />
          <span>{m.nav_login()}</span>
        </a>
        <a href="/register" class="btn btn-primary btn-sm">
          <span>{m.nav_register()}</span>
        </a>
      </div>
    </div>
  </nav>

  <!-- Hero Section -->
  <header class="hero">
    <div class="container hero-container">
      <div class="hero-badge">
        <Sparkles size={14} />
        <span>{m.landing_badge()}</span>
      </div>

      <h1 class="hero-title">
        {m.landing_hero_title_1()}
        <span class="gradient-text">{m.landing_hero_title_2()}</span>
      </h1>

      <p class="subtitle">
        {m.landing_hero_subtitle()}
      </p>

      <div class="hero-actions">
        <a href="/register" class="btn btn-primary btn-lg glow">
          <span
            >{m.landing_cta_trial()} ({price !== null
              ? `${price} TON`
              : "18 TON"}) →</span
          >
        </a>
        <a href="#calculator" class="btn btn-secondary btn-lg">
          <Calculator size={18} />
          <span>{m.landing_calc_btn()}</span>
        </a>
      </div>

      <!-- Niches Bar -->
      <div class="niches-strip" use:scrollReveal>
        <span class="niches-label">{m.landing_niche_label()}</span>
        <div class="niches-tags">
          <div class="niche-tag">
            <Scissors size={15} />
            <span>{m.landing_niche_barber()}</span>
          </div>
          <div class="niche-tag">
            <Sparkle size={15} />
            <span>{m.landing_niche_salon()}</span>
          </div>
          <div class="niche-tag">
            <Smile size={15} />
            <span>{m.landing_niche_nails()}</span>
          </div>
          <div class="niche-tag">
            <Heart size={15} />
            <span>{m.landing_niche_spa()}</span>
          </div>
          <div class="niche-tag">
            <Users size={15} />
            <span>{m.landing_niche_solo()}</span>
          </div>
        </div>
      </div>

      <!-- Telegram Mini App Showcase / Mockup -->
      <div class="product-preview" use:scrollReveal>
        <div class="preview-glass-wrapper">
          <div class="preview-glow"></div>
          <img
            src={miniappMockup}
            alt={m.landing_mockup_alt()}
            class="preview-image"
            loading="lazy"
          />
          <div class="preview-badge-overlay">
            <Bot size={18} class="text-rose" />
            <div class="preview-badge-text">
              <strong>{m.landing_mockup_badge()}</strong>
              <span>{m.landing_mockup_sub()}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </header>

  <!-- Feature Grid -->
  <section class="features">
    <div class="container">
      <div class="section-head" use:scrollReveal>
        <h2>{m.landing_features_heading()}</h2>
        <p class="section-sub">{m.landing_features_subheading()}</p>
      </div>

      <div class="grid">
        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap rose">
            <Bot size={24} />
          </div>
          <h3>{m.landing_feat_tg_title()}</h3>
          <p>{m.landing_feat_tg_desc()}</p>
        </div>

        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap sage">
            <Calendar size={24} />
          </div>
          <h3>{m.landing_feat_schedule_title()}</h3>
          <p>{m.landing_feat_schedule_desc()}</p>
        </div>

        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap lavender">
            <Users size={24} />
          </div>
          <h3>{m.landing_feat_crm_title()}</h3>
          <p>{m.landing_feat_crm_desc()}</p>
        </div>

        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap amber">
            <BarChart3 size={24} />
          </div>
          <h3>{m.landing_feat_analytics_title()}</h3>
          <p>{m.landing_feat_analytics_desc()}</p>
        </div>

        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap rose">
            <BellRing size={24} />
          </div>
          <h3>{m.landing_feat_notif_title()}</h3>
          <p>{m.landing_feat_notif_desc()}</p>
        </div>

        <div class="feature-card" use:scrollReveal>
          <div class="icon-wrap sage">
            <ShieldCheck size={24} />
          </div>
          <h3>{m.landing_feat_payment_title()}</h3>
          <p>{m.landing_feat_payment_desc()}</p>
        </div>
      </div>
    </div>
  </section>

  <!-- Interactive ROI & Lead Growth Calculator -->
  <RoiCalculator />

  <!-- FAQ Section for SEO, AI Search, and User Trust -->
  <section class="faq-section" id="faq">
    <div class="container faq-container">
      <div class="section-head" use:scrollReveal>
        <div class="hero-badge">
          <HelpCircle size={14} />
          <span>{m.landing_faq_badge()}</span>
        </div>
        <h2>{m.landing_faq_title()}</h2>
        <p class="section-sub">{m.landing_faq_sub()}</p>
      </div>

      <div class="faq-list" use:scrollReveal>
        {#each faqs as item, idx}
          <div class="faq-item" class:open={activeFaq === idx}>
            <button
              class="faq-question"
              on:click={() => toggleFaq(idx)}
              aria-expanded={activeFaq === idx}
            >
              <span>{item.q}</span>
              <span class="faq-arrow"><ChevronDown size={18} /></span>
            </button>
            {#if activeFaq === idx}
              <div class="faq-answer">
                <p>{item.a}</p>
              </div>
            {/if}
          </div>
        {/each}
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
            <span>{m.landing_cta_badge()}</span>
          </div>
          <h2>{m.landing_cta_heading()}</h2>
          <p>{m.landing_cta_sub()}</p>
          <div class="cta-actions-group">
            <a href="/register" class="btn btn-primary btn-lg glow">
              <span
                >{m.landing_cta_trial()} ({price !== null
                  ? `${price} TON`
                  : "18 TON"}) →</span
              >
            </a>
            <div class="cta-pricing-subtext">
              <span>{m.landing_cta_ton()}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- Footer -->
  <footer class="landing-footer">
    <div class="container footer-container">
      <div class="footer-brand">
        <span class="brand-dot"></span>
        <span class="brand-title">ARCH SYSTEM</span>
        <span class="footer-copy">{m.landing_footer_rights()}</span>
      </div>

      <div class="footer-links">
        <a href="/privacy" class="footer-legal-link"
          >{m.landing_footer_privacy()}</a
        >
        <span class="footer-sep">•</span>
        <a href="/terms" class="footer-legal-link">{m.landing_footer_terms()}</a
        >
        <span class="footer-sep">•</span>
        <a
          href="https://t.me/Eyed_Graff"
          target="_blank"
          rel="noopener noreferrer"
          class="footer-support-link"
        >
          <Send size={15} />
          <span>{m.landing_footer_support()}</span>
        </a>
      </div>
    </div>
  </footer>
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
    background: var(--bg-surface);
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
    color: var(--text-primary);
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
    background: linear-gradient(
      135deg,
      var(--pastel-rose),
      var(--pastel-amber)
    );
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

  /* Niches Strip */
  .niches-strip {
    margin-top: 3rem;
    margin-bottom: 2.5rem;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.85rem;
    animation: fadeIn 0.9s ease-out;
  }

  .niches-label {
    font-size: 0.85rem;
    color: var(--text-muted);
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .niches-tags {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 0.65rem;
  }

  .niche-tag {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.45rem 0.95rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    font-size: 0.86rem;
    color: var(--text-secondary);
    transition: all 0.25s var(--ease-spring);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  }

  .niche-tag:hover {
    color: var(--text-primary);
    border-color: var(--pastel-rose);
    background: var(--pastel-rose-dim);
    transform: translateY(-2px);
  }

  /* Product Preview / Mini App Showcase */
  .product-preview {
    margin-top: 2rem;
    position: relative;
    max-width: 900px;
    margin-left: auto;
    margin-right: auto;

    /* Scroll reveal dynamic state */
    --p: var(--scroll-progress, 0.9);
    opacity: calc(0.25 + 0.75 * var(--p));
    transform: translateY(calc(30px * (1 - var(--p))))
      scale(calc(0.95 + 0.05 * var(--p)));
    transition:
      opacity 0.18s ease-out,
      transform 0.18s ease-out;
    will-change: opacity, transform;
  }

  .preview-glass-wrapper {
    position: relative;
    border-radius: var(--radius-xl);
    padding: 0.75rem;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow:
      0 24px 60px rgba(0, 0, 0, 0.65),
      0 0 40px rgba(223, 158, 142, 0.12);
    overflow: hidden;
  }

  .preview-glow {
    position: absolute;
    top: -50%;
    left: 20%;
    width: 60%;
    height: 100%;
    background: radial-gradient(
      ellipse,
      rgba(223, 158, 142, 0.2) 0%,
      rgba(152, 193, 169, 0) 70%
    );
    filter: blur(50px);
    pointer-events: none;
  }

  .preview-image {
    width: 100%;
    height: auto;
    display: block;
    border-radius: calc(var(--radius-xl) - 4px);
    object-fit: cover;
  }

  .preview-badge-overlay {
    position: absolute;
    bottom: 1.75rem;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.7rem 1.4rem;
    background: rgba(20, 22, 28, 0.88);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: var(--radius-pill);
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
    white-space: nowrap;
  }

  .preview-badge-text {
    display: flex;
    flex-direction: column;
    text-align: left;
  }

  .preview-badge-text strong {
    font-size: 0.86rem;
    color: var(--text-primary);
  }

  .preview-badge-text span {
    font-size: 0.76rem;
    color: var(--text-muted);
  }

  @media (max-width: 640px) {
    .preview-badge-overlay {
      display: none;
    }
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
    transition:
      opacity 0.15s ease-out,
      transform 0.15s ease-out;
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
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 1.5rem;
  }

  /* Feature Card with Dynamic Scroll Reveal & Exit */
  .feature-card {
    flex: 0 1 calc(25% - 1.25rem);
    min-width: 250px;
    max-width: 275px;
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    padding: 2.25rem 1.75rem;
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    position: relative;
    box-sizing: border-box;

    /* Dynamic scroll-driven opacity and scale */
    --p: var(--scroll-progress, 0.85);
    opacity: calc(0.18 + 0.82 * var(--p));
    transform: translateY(calc(34px * (1 - var(--p))))
      scale(calc(0.93 + 0.07 * var(--p)));
    transition:
      opacity 0.12s ease-out,
      transform 0.12s ease-out,
      border-color 0.25s,
      box-shadow 0.25s;
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
    background: var(--bg-surface);
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
    transform: translateY(calc(38px * (1 - var(--p))))
      scale(calc(0.93 + 0.07 * var(--p)));
    transition:
      opacity 0.15s ease-out,
      transform 0.15s ease-out;
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
    background: radial-gradient(
      circle,
      rgba(223, 158, 142, 0.7) 0%,
      rgba(223, 158, 142, 0) 70%
    );
    top: -20%;
    left: 10%;
    animation: orbFloat1 13s ease-in-out infinite alternate;
  }

  .orb-sage {
    width: 320px;
    height: 320px;
    background: radial-gradient(
      circle,
      rgba(152, 193, 169, 0.6) 0%,
      rgba(152, 193, 169, 0) 70%
    );
    bottom: -25%;
    right: 12%;
    animation: orbFloat2 16s ease-in-out infinite alternate;
  }

  .orb-lavender {
    width: 260px;
    height: 260px;
    background: radial-gradient(
      circle,
      rgba(179, 183, 219, 0.55) 0%,
      rgba(179, 183, 219, 0) 70%
    );
    top: 30%;
    right: 25%;
    animation: orbFloat3 19s ease-in-out infinite alternate;
  }

  .orb-amber {
    width: 240px;
    height: 240px;
    background: radial-gradient(
      circle,
      rgba(229, 190, 138, 0.5) 0%,
      rgba(229, 190, 138, 0) 70%
    );
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
    color: var(--text-primary);
  }

  .cta-card p {
    color: var(--text-secondary);
    font-size: 1.1rem;
    margin-bottom: 2.5rem;
    max-width: 540px;
    margin-left: auto;
    margin-right: auto;
  }

  .cta-actions-group {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
  }

  .cta-pricing-subtext {
    font-size: 0.92rem;
    font-weight: 500;
    color: var(--text-muted);
  }

  .cta-pricing-subtext span {
    padding: 0.25rem 0.75rem;
    border-radius: var(--radius-pill);
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
  }

  @media (max-width: 768px) {
    .hero-title {
      font-size: 2.5rem;
    }
    .cta-card {
      padding: 3rem 1.5rem;
    }
  }

  .theme-btn {
    padding: 0.5rem 0.65rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }

  /* Landing Footer */
  .landing-footer {
    border-top: 1px solid var(--border-subtle);
    padding: 2rem 0;
    margin-top: 4rem;
    background: var(--bg-surface);
  }

  .footer-container {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 1.25rem;
  }

  .footer-brand {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .footer-copy {
    font-size: 0.82rem;
    color: var(--text-muted);
    margin-left: 0.5rem;
  }

  .footer-support-link {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    font-size: 0.88rem;
    font-weight: 600;
    color: var(--pastel-rose);
    text-decoration: none;
    transition: all 0.2s ease;
  }

  .footer-support-link:hover {
    color: var(--accent-hover);
    text-decoration: underline;
  }

  .footer-legal-link {
    font-size: 0.85rem;
    color: var(--text-muted);
    text-decoration: none;
    transition: color 0.2s ease;
  }

  .footer-legal-link:hover {
    color: var(--text-primary);
    text-decoration: underline;
  }

  .footer-sep {
    color: var(--border-subtle);
    font-size: 0.8rem;
    user-select: none;
  }

  /* Specific overrides for Light Theme on Landing */
  :global([data-theme="light"]) .cta-card {
    background: #ffffff;
    border-color: rgba(0, 0, 0, 0.08);
    box-shadow: 0 16px 40px rgba(0, 0, 0, 0.06);
  }

  :global([data-theme="light"]) .cta-card h2 {
    color: #0f172a;
  }

  :global([data-theme="light"]) .cta-card p {
    color: #475569;
  }

  :global([data-theme="light"]) .ambient-glow-mesh .glow-orb {
    opacity: 0.12;
    mix-blend-mode: normal;
  }

  :global([data-theme="light"]) .top-nav {
    background: rgba(255, 255, 255, 0.92);
  }

  :global([data-theme="light"]) .landing-footer {
    background: rgba(255, 255, 255, 0.92);
  }

  /* FAQ Section */
  .faq-section {
    padding: 5rem 0 3rem;
  }

  .faq-container {
    max-width: 820px;
    margin: 0 auto;
  }

  .faq-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    margin-top: 2.5rem;
  }

  .faq-item {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    overflow: hidden;
    transition: all 0.25s ease;
  }

  .faq-item:hover {
    border-color: var(--pastel-rose-dim);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05);
  }

  .faq-item.open {
    border-color: rgba(223, 158, 142, 0.4);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  }

  .faq-question {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1.25rem;
    padding: 1.25rem 1.5rem;
    background: transparent;
    border: none;
    color: var(--text-primary);
    font-size: 1.05rem;
    font-weight: 600;
    text-align: left;
    cursor: pointer;
    transition: color 0.2s ease;
  }

  .faq-question:hover {
    color: var(--pastel-rose);
  }

  .faq-arrow {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    color: var(--text-secondary);
    transition: transform 0.25s ease;
  }

  .faq-item.open .faq-arrow {
    transform: rotate(180deg);
    color: var(--pastel-rose);
  }

  .faq-answer {
    padding: 0 1.5rem 1.35rem;
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.65;
    animation: fadeIn 0.25s ease-out;
  }

  .faq-answer p {
    margin: 0;
  }
</style>

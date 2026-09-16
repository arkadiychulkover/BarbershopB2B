<script>
  import { 
    TrendingUp, 
    Users, 
    Coins, 
    ArrowRight, 
    Sparkles, 
    CheckCircle2, 
    Zap,
    BarChart3
  } from 'lucide-svelte';
  import { m } from '../lib/paraglide/messages.js';

  // Inputs
  let clients = 180;
  let avgCheck = 500;

  // Formula: current is 90% of potential, additional 10% is (clients / 90) * 10
  $: safeClients = Math.max(1, Number(clients) || 0);
  $: safeCheck = Math.max(1, Number(avgCheck) || 0);
  $: addedClients = Math.round((safeClients / 90) * 10);
  $: potentialTotalClients = safeClients + addedClients;
  $: currentRevenue = safeClients * safeCheck;
  $: addedRevenue = addedClients * safeCheck;
  $: potentialTotalRevenue = currentRevenue + addedRevenue;
  $: annualAddedRevenue = addedRevenue * 12;

  const presets = [
    { labelKey: 'calc_preset_small', clients: 90, check: 400 },
    { labelKey: 'calc_preset_med', clients: 180, check: 500 },
    { labelKey: 'calc_preset_large', clients: 360, check: 650 },
    { labelKey: 'calc_preset_pro', clients: 720, check: 800 }
  ];

  function applyPreset(preset) {
    clients = preset.clients;
    avgCheck = preset.check;
  }

  function formatMoney(val) {
    return Number(val || 0).toLocaleString('ru-RU');
  }
</script>

<section class="roi-calc-section" id="calculator">
  <div class="container roi-container">
    <!-- Header -->
    <div class="roi-header">
      <div class="roi-badge">
        <Sparkles size={14} class="badge-icon" />
        <span>{m.calc_badge()}</span>
      </div>
      <h2 class="roi-title">{m.calc_title()}</h2>
      <p class="roi-subtitle">{m.calc_sub()}</p>
    </div>

    <!-- Quick presets -->
    <div class="presets-row">
      {#each presets as p}
        <button
          type="button"
          class="preset-chip"
          class:active={clients === p.clients && avgCheck === p.check}
          on:click={() => applyPreset(p)}
        >
          {m[p.labelKey]()}
        </button>
      {/each}
    </div>

    <!-- Main Card -->
    <div class="calc-card">
      <!-- Left side: Inputs -->
      <div class="calc-inputs">
        <!-- Input 1: Clients -->
        <div class="input-block">
          <div class="input-head">
            <div class="label-wrap">
              <Users size={18} class="input-icon text-rose" />
              <label for="clients-slider">{m.calc_clients_label()}</label>
            </div>
            <div class="number-display">
              <input 
                id="clients-number"
                type="number" 
                class="compact-number-input"
                bind:value={clients}
                min="10"
                max="3000"
                step="10"
              />
              <span class="unit-text">{m.calc_clients_unit()}</span>
            </div>
          </div>
          
          <input
            id="clients-slider"
            type="range"
            class="range-slider"
            bind:value={clients}
            min="20"
            max="1500"
            step="10"
            style="--progress: {Math.min(100, Math.max(0, ((clients - 20) / (1500 - 20)) * 100))}%"
          />
          <div class="slider-ticks">
            <span>20</span>
            <span>500</span>
            <span>1000</span>
            <span>1500+</span>
          </div>
          <span class="field-hint-text">{m.calc_clients_hint()}</span>
        </div>

        <!-- Input 2: Average Check -->
        <div class="input-block">
          <div class="input-head">
            <div class="label-wrap">
              <Coins size={18} class="input-icon text-amber" />
              <label for="check-slider">{m.calc_check_label()}</label>
            </div>
            <div class="number-display">
              <input 
                id="check-number"
                type="number" 
                class="compact-number-input"
                bind:value={avgCheck}
                min="50"
                max="5000"
                step="50"
              />
              <span class="unit-text">{m.calc_currency()}</span>
            </div>
          </div>

          <input
            id="check-slider"
            type="range"
            class="range-slider"
            bind:value={avgCheck}
            min="100"
            max="2500"
            step="50"
            style="--progress: {Math.min(100, Math.max(0, ((avgCheck - 100) / (2500 - 100)) * 100))}%"
          />
          <div class="slider-ticks">
            <span>100 {m.calc_currency()}</span>
            <span>750 {m.calc_currency()}</span>
            <span>1500 {m.calc_currency()}</span>
            <span>2500+ {m.calc_currency()}</span>
          </div>
          <span class="field-hint-text">{m.calc_check_hint()}</span>
        </div>

        <!-- Baseline comparison bar -->
        <div class="comparison-bar-wrap">
          <div class="bar-labels">
            <span class="bar-lbl">{m.calc_current_desc()}</span>
            <span class="bar-lbl target">{m.calc_projected_desc()}</span>
          </div>
          <div class="progress-track">
            <div class="progress-baseline" style="width: 90%;">
              <span class="bar-val">90%</span>
            </div>
            <div class="progress-bonus" style="width: 10%;">
              <span class="bar-bonus-val">+10%</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right side: Results -->
      <div class="calc-results">
        <div class="results-header">
          <div class="growth-pill">
            <TrendingUp size={14} />
            <span>{m.calc_potential_badge()}</span>
          </div>
        </div>

        <!-- Stat 1: Added Clients -->
        <div class="stat-card">
          <div class="stat-top">
            <div class="stat-icon-circle rose">
              <Users size={20} />
            </div>
            <div class="stat-meta">
              <span class="stat-title">{m.calc_added_clients_title()}</span>
              <span class="stat-sub">{m.calc_added_clients_sub()}</span>
            </div>
          </div>
          <div class="stat-number text-rose">
            +{addedClients}
            <span class="stat-unit">/ {m.calc_clients_unit()}</span>
          </div>
        </div>

        <!-- Stat 2: Extra Revenue -->
        <div class="stat-card highlight">
          <div class="stat-top">
            <div class="stat-icon-circle sage">
              <Coins size={20} />
            </div>
            <div class="stat-meta">
              <span class="stat-title">{m.calc_added_revenue_title()}</span>
              <span class="stat-sub">{m.calc_added_revenue_sub()}</span>
            </div>
          </div>
          <div class="stat-number text-sage">
            +{formatMoney(addedRevenue)}
            <span class="stat-unit">{m.calc_currency()}</span>
          </div>
        </div>

        <!-- Stat 3: Annual Projection -->
        <div class="annual-strip">
          <div class="annual-info">
            <Zap size={16} class="text-amber" />
            <span class="annual-label">{m.calc_annual_title()}:</span>
          </div>
          <div class="annual-val text-amber">
            +{formatMoney(annualAddedRevenue)} {m.calc_currency()}
          </div>
        </div>

        <!-- CTA Button -->
        <a href="/register" class="btn-calc-cta">
          <span>{m.calc_cta_btn()}</span>
          <ArrowRight size={18} />
        </a>
      </div>
    </div>
  </div>
</section>

<style>
  .roi-calc-section {
    padding: 6rem 0;
    position: relative;
    background: radial-gradient(circle at 50% 30%, rgba(223, 158, 142, 0.04) 0%, transparent 60%);
    overflow: hidden;
  }

  .roi-container {
    max-width: 1140px;
    margin: 0 auto;
    padding: 0 1.5rem;
  }

  /* Header */
  .roi-header {
    text-align: center;
    max-width: 760px;
    margin: 0 auto 2.5rem;
  }

  .roi-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.35rem 0.95rem;
    background: rgba(223, 158, 142, 0.12);
    border: 1px solid rgba(223, 158, 142, 0.3);
    border-radius: 999px;
    color: var(--pastel-rose, #e09f80);
    font-size: 0.85rem;
    font-weight: 600;
    margin-bottom: 1.1rem;
    letter-spacing: 0.02em;
  }

  .roi-title {
    font-size: 2.35rem;
    font-weight: 800;
    letter-spacing: -0.025em;
    color: var(--text-primary);
    margin-bottom: 0.85rem;
    line-height: 1.2;
  }

  .roi-subtitle {
    font-size: 1.05rem;
    color: var(--text-secondary);
    line-height: 1.55;
    margin: 0;
  }

  /* Presets Row */
  .presets-row {
    display: flex;
    justify-content: center;
    gap: 0.65rem;
    flex-wrap: wrap;
    margin-bottom: 2.25rem;
  }

  .preset-chip {
    background: var(--bg-surface-elevated, rgba(255, 255, 255, 0.03));
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.08));
    color: var(--text-secondary);
    padding: 0.5rem 1.1rem;
    border-radius: 999px;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .preset-chip:hover {
    color: var(--text-primary);
    border-color: rgba(223, 158, 142, 0.4);
    background: rgba(223, 158, 142, 0.08);
    transform: translateY(-1px);
  }

  .preset-chip.active {
    background: rgba(223, 158, 142, 0.16);
    border-color: var(--pastel-rose, #e09f80);
    color: var(--text-primary);
    box-shadow: 0 0 16px rgba(223, 158, 142, 0.2);
  }

  /* Calc Card */
  .calc-card {
    display: grid;
    grid-template-columns: 1.25fr 1fr;
    gap: 2.25rem;
    background: var(--bg-surface, rgba(26, 31, 42, 0.7));
    backdrop-filter: blur(24px);
    -webkit-backdrop-filter: blur(24px);
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.08));
    border-radius: var(--radius-xl, 24px);
    padding: 2.5rem;
    box-shadow: 0 24px 60px -12px rgba(0, 0, 0, 0.35), 0 0 0 1px rgba(255, 255, 255, 0.03);
    position: relative;
  }

  /* Left: Inputs */
  .calc-inputs {
    display: flex;
    flex-direction: column;
    gap: 2rem;
  }

  .input-block {
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
    padding-bottom: 1.5rem;
    border-bottom: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.06));
  }

  .input-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .label-wrap {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .label-wrap label {
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .number-display {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: var(--bg-surface-elevated, rgba(255, 255, 255, 0.05));
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.12));
    padding: 0.35rem 0.75rem;
    border-radius: var(--radius-md, 10px);
  }

  .compact-number-input {
    width: 75px;
    background: transparent;
    border: none;
    outline: none;
    font-size: 1.15rem;
    font-weight: 800;
    color: var(--text-primary);
    text-align: right;
    font-family: inherit;
  }

  .compact-number-input::-webkit-inner-spin-button,
  .compact-number-input::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  .unit-text {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  /* Range Slider */
  .range-slider {
    -webkit-appearance: none;
    appearance: none;
    width: 100%;
    height: 8px;
    border-radius: 4px;
    background: linear-gradient(to right, var(--pastel-rose, #e09f80) 0%, var(--pastel-rose, #e09f80) var(--progress), rgba(255, 255, 255, 0.1) var(--progress), rgba(255, 255, 255, 0.1) 100%);
    outline: none;
    cursor: pointer;
    transition: background 0.1s ease;
  }

  .range-slider::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 24px;
    height: 24px;
    border-radius: 50%;
    background: #ffffff;
    border: 3px solid var(--pastel-rose, #e09f80);
    box-shadow: 0 0 12px rgba(223, 158, 142, 0.5);
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .range-slider::-webkit-slider-thumb:hover {
    transform: scale(1.15);
    box-shadow: 0 0 18px rgba(223, 158, 142, 0.8);
  }

  .range-slider::-moz-range-thumb {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    background: #ffffff;
    border: 3px solid var(--pastel-rose, #e09f80);
    box-shadow: 0 0 12px rgba(223, 158, 142, 0.5);
    cursor: pointer;
  }

  .slider-ticks {
    display: flex;
    justify-content: space-between;
    font-size: 0.75rem;
    color: var(--text-muted, #7c8ba1);
    font-weight: 500;
  }

  .field-hint-text {
    font-size: 0.82rem;
    color: var(--text-secondary);
    line-height: 1.4;
  }

  /* Comparison progress bar */
  .comparison-bar-wrap {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    padding: 1.1rem 1.25rem;
    background: rgba(255, 255, 255, 0.02);
    border: 1px dashed var(--border-subtle, rgba(255, 255, 255, 0.08));
    border-radius: var(--radius-lg, 16px);
  }

  .bar-labels {
    display: flex;
    justify-content: space-between;
    font-size: 0.8rem;
    color: var(--text-secondary);
    font-weight: 600;
  }

  .bar-labels .target {
    color: var(--pastel-sage, #98c1a9);
  }

  .progress-track {
    display: flex;
    height: 24px;
    border-radius: 8px;
    overflow: hidden;
    background: rgba(255, 255, 255, 0.05);
  }

  .progress-baseline {
    background: rgba(255, 255, 255, 0.15);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.75rem;
    font-weight: 700;
    color: var(--text-secondary);
  }

  .progress-bonus {
    background: linear-gradient(90deg, #98c1a9, #56ab91);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.75rem;
    font-weight: 800;
    color: #0b1f14;
    box-shadow: 0 0 12px rgba(152, 193, 169, 0.5);
  }

  /* Right: Results */
  .calc-results {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    background: var(--bg-surface-elevated, rgba(16, 20, 28, 0.75));
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.08));
    border-radius: var(--radius-lg, 18px);
    padding: 1.75rem;
    justify-content: space-between;
  }

  .growth-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.35rem 0.85rem;
    background: rgba(152, 193, 169, 0.15);
    border: 1px solid rgba(152, 193, 169, 0.35);
    border-radius: 999px;
    color: var(--pastel-sage, #98c1a9);
    font-size: 0.82rem;
    font-weight: 700;
  }

  .stat-card {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    background: rgba(255, 255, 255, 0.02);
    border: 1px solid var(--border-subtle, rgba(255, 255, 255, 0.06));
    border-radius: var(--radius-md, 12px);
    padding: 1.1rem 1.25rem;
    transition: transform 0.2s ease, border-color 0.2s ease;
  }

  .stat-card.highlight {
    background: linear-gradient(135deg, rgba(152, 193, 169, 0.08) 0%, rgba(152, 193, 169, 0.02) 100%);
    border-color: rgba(152, 193, 169, 0.25);
  }

  .stat-top {
    display: flex;
    align-items: flex-start;
    gap: 0.85rem;
  }

  .stat-icon-circle {
    width: 38px;
    height: 38px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .stat-icon-circle.rose {
    background: rgba(223, 158, 142, 0.15);
    color: var(--pastel-rose, #e09f80);
  }

  .stat-icon-circle.sage {
    background: rgba(152, 193, 169, 0.18);
    color: var(--pastel-sage, #98c1a9);
  }

  .stat-meta {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
  }

  .stat-title {
    font-size: 0.92rem;
    font-weight: 700;
    color: var(--text-primary);
  }

  .stat-sub {
    font-size: 0.76rem;
    color: var(--text-secondary);
    line-height: 1.35;
  }

  .stat-number {
    font-size: 2.15rem;
    font-weight: 800;
    line-height: 1;
    letter-spacing: -0.02em;
    display: flex;
    align-items: baseline;
    gap: 0.35rem;
  }

  .text-rose {
    color: var(--pastel-rose, #e09f80);
  }

  .text-sage {
    color: var(--pastel-sage, #98c1a9);
  }

  .text-amber {
    color: var(--pastel-amber, #f4a261);
  }

  .stat-unit {
    font-size: 0.95rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  /* Annual strip */
  .annual-strip {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.85rem 1.1rem;
    background: rgba(244, 162, 97, 0.08);
    border: 1px solid rgba(244, 162, 97, 0.2);
    border-radius: var(--radius-md, 10px);
  }

  .annual-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .annual-label {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .annual-val {
    font-size: 1.05rem;
    font-weight: 800;
    letter-spacing: -0.01em;
  }

  /* CTA Button */
  .btn-calc-cta {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    width: 100%;
    padding: 0.95rem 1.25rem;
    background: linear-gradient(135deg, var(--pastel-rose, #e09f80) 0%, #d47e62 100%);
    color: #ffffff;
    font-weight: 700;
    font-size: 0.95rem;
    border-radius: var(--radius-md, 12px);
    text-decoration: none;
    box-shadow: 0 8px 24px -4px rgba(223, 158, 142, 0.4);
    transition: all 0.2s ease;
    margin-top: 0.5rem;
  }

  .btn-calc-cta:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 30px -4px rgba(223, 158, 142, 0.6);
    color: #ffffff;
  }

  /* Responsive */
  @media (max-width: 960px) {
    .calc-card {
      grid-template-columns: 1fr;
      padding: 1.75rem;
      gap: 1.75rem;
    }

    .roi-title {
      font-size: 1.85rem;
    }
  }

  @media (max-width: 640px) {
    .roi-calc-section {
      padding: 4rem 0;
    }

    .calc-card {
      padding: 1.25rem;
    }

    .stat-number {
      font-size: 1.75rem;
    }

    .presets-row {
      gap: 0.4rem;
    }

    .preset-chip {
      padding: 0.4rem 0.85rem;
      font-size: 0.78rem;
    }
  }
</style>

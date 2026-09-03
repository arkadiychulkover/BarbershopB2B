<script>
  import { onMount } from 'svelte';
  import DashboardLayout from '../components/DashboardLayout.svelte';
  import { profileStore } from '../lib/store';
  import { apiRequest } from '../lib/api';
  import { m } from '../lib/paraglide/messages.js';
  import { 
    Bot, 
    Smartphone, 
    CheckCircle2, 
    ExternalLink, 
    Copy, 
    Check, 
    Sparkles, 
    HelpCircle, 
    AlertCircle,
    KeyRound
  } from 'lucide-svelte';
  
  let copied = false;
  let isLoading = false;
  const tmaBaseUrl = (import.meta.env.VITE_TMA_URL || 'https://arch-shop-bot.online').replace(/\/+$/, '');
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
        <h1>{m.bot_setup_title()}</h1>
        <p class="header-subtitle">{m.bot_setup_subtitle()}</p>
      </div>
      <div class="header-badge">
        <Sparkles size={15} />
        <span>{m.bot_setup_steps_badge()}</span>
      </div>
    </header>

    <!-- Quick Copy Banner -->
    <div class="quick-copy-card card mb-4">
      <div class="quick-copy-body">
        <div class="quick-copy-label">{m.bot_setup_quick_copy_label()}</div>
        <div class="quick-copy-url">
          <code>{tmaLink}</code>
        </div>
      </div>
      <button class="btn-copy-main" on:click={copyLink} title={m.bot_setup_copy_btn()}>
        {#if copied}
          <Check size={18} class="check-icon" />
          <span>{m.bot_setup_copied()}</span>
        {:else}
          <Copy size={18} />
          <span>{m.bot_setup_copy_btn()}</span>
        {/if}
      </button>
    </div>

    {#if !effectiveBotUser}
      <div class="alert-box warning-box mb-4">
        <AlertCircle size={20} class="alert-icon" />
        <div class="alert-body">
          <strong>{m.bot_setup_not_connected_title()}</strong>
          <p>{m.bot_setup_not_connected_desc()} <a href="#/dashboard/settings" class="inline-link">{m.settings_title()}</a>.</p>
        </div>
      </div>
    {:else}
      <div class="alert-box success-box mb-4">
        <CheckCircle2 size={20} class="alert-icon" />
        <div class="alert-body">
          <strong>{m.bot_setup_connected_title()}{effectiveBotUser}</strong>
          <p>
            {m.bot_setup_direct_link()} 
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
            <h3>{m.bot_setup_step1_title()}</h3>
          </div>
          <p class="step-intro">
            {m.bot_setup_step1_intro()}
          </p>

          <ol class="action-steps-list">
            <li>
              <span class="action-num">1.1</span>
              <div>
                {m.bot_setup_step1_1()} 
                <a href="https://t.me/BotFather" target="_blank" rel="noreferrer" class="ext-link">
                  @BotFather <ExternalLink size={13} />
                </a> 
                {m.bot_setup_step1_1_end()}
              </div>
            </li>
            <li>
              <span class="action-num">1.2</span>
              <div>
                {m.bot_setup_step1_2()}
              </div>
            </li>
            <li>
              <span class="action-num">1.3</span>
              <div>
                {m.bot_setup_step1_3_ask()}
                <br />
                {m.bot_setup_step1_3_action()}
              </div>
            </li>
            <li>
              <span class="action-num">1.4</span>
              <div>
                {m.bot_setup_step1_4_ask()}
                <br />
                {m.bot_setup_step1_4_action()}
              </div>
            </li>
            <li>
              <span class="action-num">1.5</span>
              <div>
                {m.bot_setup_step1_5_text()}
                <br />
                <strong>Use this token to access the HTTP API:</strong>
                <br />
                <span class="token-example"><code>1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ...</code></span>
                <br />
                {m.bot_setup_step1_5_sub()}
              </div>
            </li>
          </ol>
        </div>
      </div>

      <!-- Step 2 -->
      <div class="card step-card">
        <div class="step-badge amber">2</div>
        <div class="step-content">
          <div class="step-head">
            <KeyRound size={22} class="step-icon amber" />
            <h3>{m.bot_setup_step2_title()}</h3>
          </div>
          <p class="step-intro">
            {m.bot_setup_step2_intro()}
          </p>

          <ol class="action-steps-list">
            <li>
              <span class="action-num">2.1</span>
              <div>
                {m.bot_setup_step2_1()} <a href="#/dashboard/settings" class="inline-link">«{m.settings_title()}» <ExternalLink size={12} /></a> {m.bot_setup_step2_1_end()}
              </div>
            </li>
            <li>
              <span class="action-num">2.2</span>
              <div>
                {m.bot_setup_step2_2()}
              </div>
            </li>
            <li>
              <span class="action-num">2.3</span>
              <div>
                {m.bot_setup_step2_3()}
              </div>
            </li>
            <li>
              <span class="action-num">2.4</span>
              <div>
                {m.bot_setup_step2_4()}
              </div>
            </li>
            <li>
              <span class="action-num">2.5</span>
              <div>
                {m.bot_setup_step2_5()}
              </div>
            </li>
          </ol>
        </div>
      </div>

      <!-- Step 3 -->
      <div class="card step-card">
        <div class="step-badge sage">3</div>
        <div class="step-content">
          <div class="step-head">
            <Smartphone size={22} class="step-icon sage" />
            <h3>{m.bot_setup_step3_title()}</h3>
          </div>
          <p class="step-intro">
            {m.bot_setup_step3_intro()}
          </p>
          
          <ol class="action-steps-list">
            <li>
              <span class="action-num">3.1</span>
              <div>
                {m.bot_setup_step3_1()}
                <div class="code-box mt-2">
                  <code>{tmaLink}</code>
                  <button class="copy-btn" on:click={copyLink} title={m.bot_setup_copy_btn()}>
                    {#if copied}
                      <Check size={16} class="check-icon" />
                      <span>{m.bot_setup_copied()}</span>
                    {:else}
                      <Copy size={16} />
                      <span>{m.common_copy()}</span>
                    {/if}
                  </button>
                </div>
              </div>
            </li>
            <li>
              <span class="action-num">3.2</span>
              <div>
                {m.bot_setup_step3_2()} 
                <a href="https://t.me/BotFather" target="_blank" rel="noreferrer" class="ext-link">
                  @BotFather <ExternalLink size={13} />
                </a> 
                (/setmenubutton).
              </div>
            </li>
            <li>
              <span class="action-num">3.3</span>
              <div>
                {m.bot_setup_step3_3()}
              </div>
            </li>
            <li>
              <span class="action-num">3.4</span>
              <div>
                {m.bot_setup_step3_4()}
              </div>
            </li>
            <li>
              <span class="action-num">3.5</span>
              <div>
                {m.bot_setup_step3_5()}
              </div>
            </li>
          </ol>

          <div class="hint-card mt-3">
            <span class="hint-title">{m.bot_setup_step3_done_title()}</span> {m.bot_setup_step3_done_sub()}
          </div>
        </div>
      </div>

      <!-- Step 4 -->
      <div class="card step-card">
        <div class="step-badge lavender">4</div>
        <div class="step-content">
          <div class="step-head">
            <CheckCircle2 size={22} class="step-icon lavender" />
            <h3>{m.bot_setup_step4_title()}</h3>
          </div>
          <p class="step-intro">
            {m.bot_setup_step4_intro()}
          </p>

          <ol class="action-steps-list">
            <li>
              <span class="action-num">4.1</span>
              <div>
                {#if effectiveBotUser}
                  {m.bot_setup_step4_1_open()}
                  <a href="https://t.me/{effectiveBotUser}" target="_blank" rel="noreferrer" class="bot-open-btn">
                    <span>{m.bot_setup_step4_1_btn()}</span>
                    <ExternalLink size={14} />
                  </a>
                {:else}
                  {m.bot_setup_step4_1_search()}
                {/if}
              </div>
            </li>
            <li>
              <span class="action-num">4.2</span>
              <div>
                {m.bot_setup_step4_2()}
              </div>
            </li>
            <li>
              <span class="action-num">4.3</span>
              <div>
                {m.bot_setup_step4_3()}
              </div>
            </li>
            <li>
              <span class="action-num">4.4</span>
              <div>
                {m.bot_setup_step4_4()}
              </div>
            </li>
          </ol>
          
          <div class="support-box mt-3">
            <HelpCircle size={20} class="support-icon" />
            <div class="support-text">
              <strong>{m.bot_setup_support_title()}</strong>
              <p>{m.bot_setup_support_desc()}</p>
            </div>
            <a href="https://t.me/Eyed_Graff" target="_blank" rel="noreferrer" class="support-link-btn">
              <span>{m.bot_setup_support_btn()}</span>
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

  .step-badge.amber {
    background: rgba(245, 158, 11, 0.12);
    color: #f59e0b;
    border: 1px solid rgba(245, 158, 11, 0.3);
    box-shadow: 0 0 16px rgba(245, 158, 11, 0.2);
  }

  .step-intro {
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.6;
    margin-bottom: 1.25rem;
  }

  .action-steps-list {
    list-style: none;
    padding: 0;
    margin: 0 0 1.25rem 0;
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
  }

  .action-steps-list li {
    display: flex;
    align-items: flex-start;
    gap: 0.9rem;
    font-size: 0.93rem;
    line-height: 1.6;
    color: var(--text-primary);
    background: rgba(255, 255, 255, 0.02);
    padding: 0.75rem 1rem;
    border-radius: var(--radius-md);
    border: 1px solid rgba(255, 255, 255, 0.04);
  }

  .action-num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0.2rem 0.55rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--pastel-rose);
    flex-shrink: 0;
    margin-top: 0.1rem;
  }

  .token-example {
    display: inline-block;
    margin-top: 0.35rem;
  }

  .token-example code {
    background: var(--bg-surface-elevated);
    border: 1px dashed var(--pastel-amber);
    color: var(--pastel-amber);
    padding: 0.3rem 0.6rem;
    border-radius: var(--radius-sm);
    word-break: break-all;
  }

  .bot-open-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    margin-top: 0.35rem;
    padding: 0.45rem 0.9rem;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
    border-radius: var(--radius-pill);
    font-size: 0.88rem;
    font-weight: 600;
    text-decoration: none;
    transition: all 0.2s;
  }

  .bot-open-btn:hover {
    background: rgba(223, 158, 142, 0.25);
    transform: translateY(-1px);
  }

  .support-text {
    flex: 1;
    min-width: 200px;
  }

  .support-text strong {
    display: block;
    color: var(--text-primary);
    font-size: 0.92rem;
    margin-bottom: 0.2rem;
  }

  .support-text p {
    margin: 0;
    color: var(--text-secondary);
    font-size: 0.86rem;
    line-height: 1.4;
  }

  .support-link-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.6rem 1.1rem;
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.3);
    border-radius: var(--radius-pill);
    font-size: 0.88rem;
    font-weight: 600;
    text-decoration: none;
    transition: all 0.2s;
    flex-shrink: 0;
  }

  .support-link-btn:hover {
    background: rgba(179, 183, 219, 0.25);
    transform: translateY(-1px);
  }

  .mt-2 { margin-top: 0.5rem; }
  .mt-3 { margin-top: 0.85rem; }

  :global(.step-icon.amber) { color: #f59e0b; }

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

<script>
  import { onMount } from 'svelte';
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';
  import { profileStore } from '../lib/store';
  import QRCode from 'qrcode';
  import { 
    Copy, 
    Check, 
    Wallet, 
    Coins, 
    CheckCircle2, 
    AlertCircle, 
    ShieldCheck, 
    ArrowRight, 
    Sparkles, 
    ExternalLink,
    Edit3
  } from 'lucide-svelte';

  let canvas;
  let txHash = '';
  let isLoading = true;
  let isSaving = false;
  let errorMsg = '';
  let successMsg = '';

  let settings = null;
  let userWalletAddress = '';
  let copied = false;

  let platformWalletAddress = '';
  let subscriptionAmount = '10';
  
  $: paymentLink = `ton://transfer/${platformWalletAddress}?amount=${Number(subscriptionAmount) * 1000000000}`;

  onMount(async () => {
    try {
      const [settingsRes, priceRes] = await Promise.all([
        apiRequest('/api/Settings'),
        apiRequest('/api/Payment/price').catch(() => ({ price: '10', platformWalletAddress: '' }))
      ]);
      settings = settingsRes;
      userWalletAddress = settings.walletAddress || '';
      subscriptionAmount = priceRes.price || '10';
      platformWalletAddress = priceRes.platformWalletAddress || '';
    } catch (e) {
      errorMsg = 'Не удалось загрузить данные для оплаты';
    } finally {
      isLoading = false;
      if (userWalletAddress) {
        renderQR();
      }
    }
  });

  function renderQR() {
    setTimeout(() => {
      if (canvas && platformWalletAddress) {
        QRCode.toCanvas(canvas, paymentLink, {
          width: 220,
          margin: 1,
          color: {
            dark: '#0c0e12',
            light: '#ffffff'
          }
        }, (error) => {
          if (error) console.error(error);
        });
      }
    }, 0);
  }

  function copyAddress() {
    if (!platformWalletAddress) return;
    navigator.clipboard.writeText(platformWalletAddress);
    copied = true;
    setTimeout(() => copied = false, 2500);
  }

  async function handleSaveWallet() {
    isSaving = true;
    errorMsg = '';
    successMsg = '';
    try {
      await apiRequest('/api/Settings/wallet', {
        method: 'PUT',
        body: JSON.stringify({ walletAddress: userWalletAddress })
      });
      settings.walletAddress = userWalletAddress;
      successMsg = 'Кошелек сохранен. Теперь вы можете оплатить подписку.';
      renderQR();
      setTimeout(() => successMsg = '', 4000);
    } catch(e) {
      errorMsg = e.message || 'Ошибка сохранения кошелька';
    } finally {
      isSaving = false;
    }
  }

  async function handleVerify() {
    if (!txHash) return;
    
    isLoading = true;
    errorMsg = '';
    
    try {
      await apiRequest('/api/Payment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(txHash)
      });
      
      successMsg = 'Оплата успешно подтверждена! Добро пожаловать!';
      profileStore.update(s => ({ ...s, status: 'Active' }));
      
      setTimeout(() => {
        push('/dashboard');
      }, 2000);
      
    } catch (err) {
      errorMsg = err.message || 'Ошибка проверки транзакции. Убедитесь, что платеж прошел в сети TON.';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="payment-container">
  <div class="card payment-card">
    <div class="brand-header">
      <span class="brand-dot"></span>
      <span class="brand-name">BarbershopB2B</span>
    </div>

    <div class="header-center">
      <h2>Активация подписки</h2>
      <p class="subtitle">Полный доступ к Telegram Mini App, управлению мастерами и финансовой аналитике</p>
    </div>
    
    {#if isLoading && !settings}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>Загрузка данных...</p>
      </div>
    {:else}
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

      {#if !settings?.walletAddress}
        <div class="wallet-setup">
          <div class="notice-box">
            <Wallet size={20} class="notice-icon" />
            <p>Укажите ваш TON-кошелек перед оплатой. С него платформа будет автоматически сопоставлять входящие транзакции.</p>
          </div>

          <form on:submit|preventDefault={handleSaveWallet} class="verify-form">
            <div class="form-group">
              <label for="userWalletAddress">Ваш TON-кошелек</label>
              <div class="input-icon-wrap">
                <Wallet size={16} class="input-icon" />
                <input 
                  id="userWalletAddress" 
                  type="text" 
                  class="input has-icon" 
                  bind:value={userWalletAddress} 
                  required 
                  placeholder="EQ... или UQ..." 
                />
              </div>
            </div>

            <button type="submit" class="btn btn-primary submit-btn" disabled={isSaving || !userWalletAddress}>
              <span>{isSaving ? 'Сохранение...' : 'Сохранить и перейти к оплате'}</span>
              <ArrowRight size={17} />
            </button>
          </form>
        </div>
      {:else}
        <!-- Wallet connected pill -->
        <div class="connected-wallet-strip">
          <div class="strip-left">
            <Wallet size={16} class="strip-icon" />
            <span>Оплата с кошелька:</span>
            <strong>{settings.walletAddress.substring(0, 6)}...{settings.walletAddress.substring(settings.walletAddress.length - 4)}</strong>
          </div>
          <button class="edit-link" on:click={() => settings.walletAddress = ''}>
            <Edit3 size={13} />
            <span>Изменить</span>
          </button>
        </div>

        <!-- Payment Info Card -->
        <div class="payment-info-box">
          <div class="qr-container">
            <canvas bind:this={canvas}></canvas>
          </div>
          
          <div class="details">
            <div class="amount-badge">
              <Coins size={16} />
              <span>Сумма к оплате: <strong>{subscriptionAmount} TON</strong></span>
            </div>
            
            <p class="address-label">Адрес для перевода:</p>
            <div class="wallet-address-box">
              <code>{platformWalletAddress}</code>
              <button class="copy-btn" on:click={copyAddress} title="Скопировать адрес">
                {#if copied}
                  <Check size={16} class="check-icon" />
                {:else}
                  <Copy size={16} />
                {/if}
              </button>
            </div>
            
            <p class="hint">Отсканируйте QR-код в кошельке (Tonkeeper, MyTonWallet, Tonhub) или отправьте средства вручную.</p>
          </div>
        </div>

        <!-- Transaction Hash Form -->
        <form on:submit|preventDefault={handleVerify} class="verify-form">
          <div class="form-group">
            <label for="txHash">Хэш транзакции (Transaction Hash или BOC)</label>
            <input 
              id="txHash" 
              type="text" 
              class="input" 
              bind:value={txHash} 
              required 
              placeholder="Вставьте хэш отправленной транзакции" 
            />
          </div>

          <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading || !txHash}>
            <span>{isLoading ? 'Проверка блокчейна...' : 'Подтвердить оплату'}</span>
            {#if !isLoading}
              <ShieldCheck size={18} />
            {/if}
          </button>
        </form>

        <div class="payment-support-hint">
          <span>Возникли вопросы по оплате или активации? Поддержка: </span>
          <a href="https://t.me/Eyed_Graff" target="_blank" rel="noreferrer" class="support-link">
            <span>@Eyed_Graff</span>
            <ExternalLink size={13} />
          </a>
        </div>
      {/if}
    {/if}
  </div>
</div>

<style>
  .payment-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 3rem 1.5rem;
    background-color: var(--bg-canvas);
    background-image: 
      radial-gradient(ellipse 70% 50% at 50% 20%, rgba(223, 158, 142, 0.08), transparent 70%),
      radial-gradient(ellipse 50% 50% at 85% 85%, rgba(152, 193, 169, 0.05), transparent 70%);
  }
  
  .payment-card {
    width: 100%;
    max-width: 640px;
    padding: 2.75rem 2.5rem;
    animation: fadeIn 0.35s var(--ease-spring);
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
  
  .header-center {
    text-align: center;
    margin-bottom: 2rem;
  }

  .header-center h2 {
    font-size: 2rem;
    margin-bottom: 0.4rem;
  }
  
  .subtitle {
    color: var(--text-secondary);
    font-size: 0.95rem;
    line-height: 1.55;
  }

  .notice-box {
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;
    padding: 1.25rem;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    margin-bottom: 1.75rem;
    font-size: 0.92rem;
    color: var(--text-secondary);
    line-height: 1.55;
  }

  :global(.notice-icon) {
    color: var(--pastel-amber);
    flex-shrink: 0;
    margin-top: 2px;
  }

  .connected-wallet-strip {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.85rem 1.25rem;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    margin-bottom: 1.5rem;
    font-size: 0.88rem;
  }

  .strip-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-secondary);
  }

  .strip-left strong {
    color: var(--text-primary);
  }

  :global(.strip-icon) {
    color: var(--pastel-sage);
  }

  .edit-link {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 0.82rem;
    font-weight: 600;
    cursor: pointer;
  }

  /* Payment Info Box */
  .payment-info-box {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.5rem;
    padding: 2rem;
    background: var(--bg-surface-elevated);
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    margin-bottom: 2rem;
  }
  
  .qr-container {
    background: #ffffff;
    padding: 0.75rem;
    border-radius: var(--radius-md);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;
  }
  
  .details {
    text-align: center;
    width: 100%;
  }

  .amount-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4rem 1rem;
    border-radius: var(--radius-pill);
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
    font-size: 0.95rem;
    font-weight: 600;
    margin-bottom: 1.25rem;
  }

  .address-label {
    font-size: 0.82rem;
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-bottom: 0.4rem;
  }
  
  .wallet-address-box {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background-color: var(--bg-canvas);
    padding: 0.75rem 1.1rem;
    border-radius: var(--radius-md);
    margin-bottom: 1rem;
    border: 1px solid var(--border-subtle);
    gap: 0.75rem;
  }
  
  .wallet-address-box code {
    word-break: break-all;
    font-family: monospace;
    font-size: 0.88rem;
    color: var(--pastel-rose);
  }
  
  .copy-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    cursor: pointer;
    padding: 0.45rem;
    border-radius: var(--radius-sm);
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;
    flex-shrink: 0;
  }
  
  .copy-btn:hover {
    background-color: var(--bg-surface-hover);
    color: var(--pastel-rose);
    border-color: var(--border-glass);
  }

  :global(.check-icon) {
    color: var(--pastel-sage);
  }
  
  .hint {
    font-size: 0.85rem;
    color: var(--text-secondary);
    line-height: 1.5;
  }
  
  .verify-form {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }
  
  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }
  
  label {
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
  
  .submit-btn {
    width: 100%;
    padding: 0.85rem 1.5rem;
    font-size: 1rem;
    margin-top: 0.5rem;
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 0;
    color: var(--text-secondary);
    gap: 1rem;
  }

  .spinner-sm {
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    width: 32px;
    height: 32px;
    animation: spinSmooth 0.85s linear infinite;
  }

  .payment-support-hint {
    margin-top: 1.5rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--border-subtle);
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.4rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
    text-align: center;
    flex-wrap: wrap;
  }

  .payment-support-hint .support-link {
    color: var(--pastel-rose);
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
  }

  .payment-support-hint .support-link:hover {
    text-decoration: underline;
  }
</style>

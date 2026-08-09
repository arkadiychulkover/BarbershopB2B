<script>
  import { onMount } from 'svelte';
  import { push } from 'svelte-spa-router';
  import { apiRequest } from '../lib/api';
  import { profileStore } from '../lib/store';
  import QRCode from 'qrcode';
  import { Copy, Check } from 'lucide-svelte';

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
      errorMsg = 'Не удалось загрузить настройки профиля';
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
          width: 250,
          margin: 1,
          color: {
            dark: '#000000',
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
    setTimeout(() => copied = false, 2000);
  }

  async function handleSaveWallet() {
    isSaving = true;
    errorMsg = '';
    successMsg = '';
    try {
      const updatedSettings = { ...settings, walletAddress: userWalletAddress };
      await apiRequest('/api/Settings', {
        method: 'PUT',
        body: JSON.stringify(updatedSettings)
      });
      settings.walletAddress = userWalletAddress;
      successMsg = 'Кошелек сохранен. Теперь вы можете оплатить подписку.';
      renderQR();
      setTimeout(() => successMsg = '', 4000);
    } catch(e) {
      errorMsg = e.message || 'Ошибка сохранения';
    } finally {
      isSaving = false;
    }
  }

  async function handleVerify() {
    if (!txHash) return;
    
    isLoading = true;
    errorMsg = '';
    
    try {
      const response = await apiRequest('/api/Payment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(txHash)
      });
      
      successMsg = 'Оплата успешно подтверждена!';
      profileStore.update(s => ({ ...s, status: 'Active' }));
      
      setTimeout(() => {
        push('/dashboard');
      }, 2000);
      
    } catch (err) {
      errorMsg = err.message || 'Ошибка проверки транзакции';
    } finally {
      isLoading = false;
    }
  }
</script>

<div class="payment-container">
  <div class="card payment-card">
    <h2>Оплата подписки</h2>
    
    {#if isLoading && !settings}
      <div class="loader-container">
        <p>Загрузка...</p>
      </div>
    {:else}

      {#if errorMsg}
        <div class="alert alert-danger">{errorMsg}</div>
      {/if}
      
      {#if successMsg}
        <div class="alert alert-success">{successMsg}</div>
      {/if}

      {#if !settings?.walletAddress}
        <div class="wallet-setup">
          <p style="margin-bottom: 1.5rem; color: var(--text-secondary);">Перед тем как оплатить подписку, пожалуйста, укажите ваш TON-кошелек. С него мы будем ожидать поступление средств.</p>
          <form on:submit|preventDefault={handleSaveWallet} class="verify-form" style="border-top: none; padding-top: 0;">
            <div class="form-group">
              <label for="userWalletAddress">Кошелёк для оплаты подписки</label>
              <input id="userWalletAddress" type="text" class="input" bind:value={userWalletAddress} required placeholder="Введите ваш TON кошелек (EQ... / UQ...)" />
            </div>
            <button type="submit" class="btn btn-primary submit-btn" disabled={isSaving || !userWalletAddress}>
              {isSaving ? 'Сохранение...' : 'Сохранить и перейти к оплате'}
            </button>
          </form>
        </div>
      {:else}
        <div class="wallet-setup" style="margin-bottom: 1.5rem; padding: 1rem; background-color: rgba(255,255,255,0.05); border-radius: 8px;">
          <p style="margin: 0; font-size: 0.9rem;">
            Вы оплачиваете с кошелька: <strong title={settings.walletAddress}>{settings.walletAddress.substring(0, 8)}...{settings.walletAddress.substring(settings.walletAddress.length - 4)}</strong> 
            <button class="btn-link" style="margin-left: 0.5rem; background: none; border: none; color: var(--accent); cursor: pointer; text-decoration: underline;" on:click={() => settings.walletAddress = ''}>Изменить</button>
          </p>
        </div>

        <div class="payment-info">
          <div class="qr-container">
            <canvas bind:this={canvas}></canvas>
          </div>
          
          <div class="details">
            <p>Для активации подписки отправьте <strong>{subscriptionAmount} TON</strong> на адрес платформы:</p>
            <div class="wallet-address-box">
              <code>{platformWalletAddress}</code>
              <button class="btn-icon" on:click={copyAddress} title="Скопировать">
                {#if copied}
                  <Check size={18} color="var(--success)" />
                {:else}
                  <Copy size={18} />
                {/if}
              </button>
            </div>
            <p class="hint">Или отсканируйте QR-код в вашем кошельке (Tonkeeper, Tonhub и др.)</p>
          </div>
        </div>

        <form on:submit|preventDefault={handleVerify} class="verify-form">
          <div class="form-group">
            <label for="txHash">Хэш транзакции (BOC или TX ID)</label>
            <input id="txHash" type="text" class="input" bind:value={txHash} required placeholder="Введите хэш после отправки средств" />
          </div>

          <button type="submit" class="btn btn-primary submit-btn" disabled={isLoading || !txHash}>
            {isLoading ? 'Проверка...' : 'Я оплатил, проверить'}
          </button>
        </form>
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
    padding: 2rem 1rem;
    background-color: var(--bg-color);
  }
  
  .payment-card {
    width: 100%;
    max-width: 600px;
  }
  
  .payment-card h2 {
    text-align: center;
    margin-bottom: 1.5rem;
  }
  
  .payment-info {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.5rem;
    margin-bottom: 2rem;
    padding: 1.5rem;
    background-color: var(--bg-tertiary);
    border-radius: var(--border-radius);
  }
  
  .qr-container {
    background: white;
    padding: 0.5rem;
    border-radius: 8px;
  }
  
  .details {
    text-align: center;
    width: 100%;
  }
  
  .wallet-address-box {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background-color: var(--bg-color);
    padding: 0.75rem 1rem;
    border-radius: var(--border-radius);
    margin: 1rem 0;
    border: 1px solid var(--border-color);
  }
  
  .wallet-address-box code {
    word-break: break-all;
    font-family: monospace;
    font-size: 0.9rem;
    color: var(--text-primary);
  }
  
  .btn-icon {
    background: none;
    border: none;
    color: var(--text-secondary);
    cursor: pointer;
    padding: 0.5rem;
    border-radius: 4px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;
    margin-left: 1rem;
    flex-shrink: 0;
  }
  
  .btn-icon:hover {
    background-color: rgba(255, 255, 255, 0.1);
    color: var(--text-primary);
  }
  
  .hint {
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .verify-form {
    border-top: 1px solid var(--border-color);
    padding-top: 1.5rem;
  }
  
  .form-group {
    margin-bottom: 1rem;
  }
  
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
    color: var(--text-secondary);
  }
  
  .submit-btn {
    width: 100%;
  }
  
  .alert {
    padding: 1rem;
    border-radius: var(--border-radius);
    margin-bottom: 1.5rem;
    font-size: 0.875rem;
  }
  
  .alert-danger {
    background-color: rgba(239, 68, 68, 0.1);
    color: var(--danger);
    border: 1px solid rgba(239, 68, 68, 0.2);
  }
  
  .alert-success {
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--success);
    border: 1px solid rgba(16, 185, 129, 0.2);
  }
  
  .loader-container {
    text-align: center;
    padding: 2rem;
    color: var(--text-secondary);
  }
</style>

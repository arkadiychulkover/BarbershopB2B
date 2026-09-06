<script>
  import { onMount } from "svelte";
  import DashboardLayout from "../components/DashboardLayout.svelte";
  import { apiRequest } from "../lib/api";
  import { profileStore } from "../lib/store";
  import { m } from "../lib/paraglide/messages.js";
  import { currentLocale } from "../lib/locale.js";
  import QRCode from "qrcode";
  import {
    Building2,
    User,
    MapPin,
    Globe,
    Wallet,
    Bell,
    CheckCircle2,
    AlertCircle,
    Save,
    Sun,
    Moon,
    Send,
    HelpCircle,
    ExternalLink,
    Coins,
    ShieldCheck,
    Copy,
    Check,
    RefreshCw,
    Sparkles,
    CalendarCheck,
    CreditCard,
    ArrowRight,
    Phone,
    Bot,
    Eye,
    EyeOff,
    Lock,
    KeyRound,
    Mail,
    Percent
  } from "lucide-svelte";
  import { theme, toggleTheme, setTheme } from "../lib/theme";

  let isLoading = true;
  let isSaving = false;
  let isRenewing = false;
  let isRenewOpen = false;
  let successMsg = "";
  let errorMsg = "";
  let renewSuccessMsg = "";
  let renewErrorMsg = "";

  let settings = {
    id: "",
    email: "",
    barbershopName: "",
    barbershopAddress: "",
    barbershopDescription: "",
    ownerName: "",
    timeZone: "",
    phoneNumber: "",
    telegramId: "",
    botToken: "",
    botUsername: "",
    reminderHoursBefore: 24,
    winBackDays: 0,
    walletAddress: "",
    masterFee: 0,
    isSubscribed: false,
    status: "Active",
    nextPayment: null,
    lastPayment: null,
  };

  let showBotToken = false;

  // Password change state
  let currentPassword = "";
  let newPassword = "";
  let confirmPassword = "";
  let showCurrentPassword = false;
  let showNewPassword = false;
  let showConfirmPassword = false;
  let isChangingPassword = false;
  let isSendingResetEmail = false;
  let passwordSuccessMsg = "";
  let passwordErrorMsg = "";

  let platformWalletAddress = "";
  let subscriptionAmount = 5;
  let txHash = "";
  let copied = false;
  let qrCanvas;

  $: paymentLink = `ton://transfer/${platformWalletAddress}?amount=${subscriptionAmount * 1e9}&text=renew_${settings.ownerName || 'owner'}`;
  
  $: remainingDays = settings.nextPayment 
    ? Math.max(0, Math.ceil((new Date(settings.nextPayment).getTime() - Date.now()) / (1000 * 60 * 60 * 24)))
    : 0;

  $: isSubscriptionActive = settings.status === 'Active' && settings.nextPayment && new Date(settings.nextPayment).getTime() > Date.now();

  onMount(async () => {
    try {
      const [data, priceData] = await Promise.all([
        apiRequest("/api/Settings"),
        apiRequest("/api/Payment/price").catch(() => ({ amount: 5, platformWalletAddress: "" }))
      ]);
      settings = { ...settings, ...data };
      if (priceData) {
        subscriptionAmount = priceData.amount || 5;
        platformWalletAddress = priceData.platformWalletAddress || "";
      }
    } catch (e) {
      errorMsg = m.settings_load_error();
    } finally {
      isLoading = false;
    }
  });

  function toggleRenewForm() {
    isRenewOpen = !isRenewOpen;
    renewErrorMsg = "";
    renewSuccessMsg = "";
    if (isRenewOpen) {
      renderQR();
    }
  }

  function renderQR() {
    setTimeout(() => {
      if (qrCanvas && platformWalletAddress) {
        QRCode.toCanvas(qrCanvas, paymentLink, {
          width: 170,
          margin: 1,
          color: {
            dark: '#0c0e12',
            light: '#ffffff'
          }
        }, (error) => {
          if (error) console.error(error);
        });
      }
    }, 50);
  }

  function copyAddress() {
    if (!platformWalletAddress) return;
    navigator.clipboard.writeText(platformWalletAddress);
    copied = true;
    setTimeout(() => (copied = false), 2500);
  }

  async function handleRenewSubscription() {
    if (!txHash.trim()) {
      renewErrorMsg = m.settings_renew_hash_error();
      return;
    }

    isRenewing = true;
    renewErrorMsg = "";
    renewSuccessMsg = "";

    try {
      const res = await apiRequest("/api/Payment/renew", {
        method: "POST",
        body: JSON.stringify({ txHash: txHash.trim() }),
      });

      renewSuccessMsg = res.message || m.settings_renew_success();
      settings.nextPayment = res.nextPayment;
      settings.lastPayment = res.lastPayment;
      settings.status = res.status || "Active";
      settings.isSubscribed = true;
      profileStore.update(s => ({ ...s, status: "Active" }));
      txHash = "";
      setTimeout(() => {
        renewSuccessMsg = "";
      }, 5000);
    } catch (err) {
      renewErrorMsg = err.message || m.settings_renew_fail();
    } finally {
      isRenewing = false;
    }
  }

  function formatDate(isoStr) {
    if (!isoStr) return "—";
    const date = new Date(isoStr);
    return date.toLocaleDateString($currentLocale === "ru" ? "ru-RU" : "en-US", {
      day: "numeric",
      month: "long",
      year: "numeric"
    });
  }

  async function handleSave() {
    isSaving = true;
    successMsg = "";
    errorMsg = "";

    try {
      await apiRequest("/api/Settings", {
        method: "PUT",
        body: JSON.stringify({
          barbershopName: settings.barbershopName,
          barbershopAddress: settings.barbershopAddress,
          barbershopDescription: settings.barbershopDescription,
          ownerName: settings.ownerName,
          timeZone: settings.timeZone,
          phoneNumber: settings.phoneNumber,
          telegramId: settings.telegramId,
          botToken: settings.botToken,
          botUsername: settings.botUsername,
          reminderHoursBefore: parseInt(settings.reminderHoursBefore, 10) || 24,
          winBackDays: parseInt(settings.winBackDays, 10) || 0,
          masterFee: parseFloat(settings.masterFee) || 0,
          walletAddress: settings.walletAddress || ""
        }),
      });
      profileStore.update(s => ({
        ...s,
        ownerName: settings.ownerName,
        botUsername: (settings.botUsername || '').replace(/^@/, '')
      }));
      successMsg = m.settings_save_success();
      setTimeout(() => (successMsg = ""), 3500);
    } catch (e) {
      errorMsg = e.message || m.settings_save_error();
    } finally {
      isSaving = false;
    }
  }

  async function handleChangePassword() {
    passwordSuccessMsg = "";
    passwordErrorMsg = "";

    if (!currentPassword || !newPassword) {
      passwordErrorMsg = m.settings_pwd_fill_error();
      return;
    }
    if (newPassword.length < 6) {
      passwordErrorMsg = m.settings_pwd_len_error();
      return;
    }
    if (newPassword !== confirmPassword) {
      passwordErrorMsg = m.settings_pwd_match_error();
      return;
    }

    isChangingPassword = true;
    try {
      const res = await apiRequest("/api/Regestration/change-password", {
        method: "POST",
        body: JSON.stringify({
          currentPassword,
          newPassword
        })
      });
      passwordSuccessMsg = res?.message || m.settings_pwd_success();
      currentPassword = "";
      newPassword = "";
      confirmPassword = "";
      setTimeout(() => (passwordSuccessMsg = ""), 5000);
    } catch (e) {
      passwordErrorMsg = e.message || m.settings_pwd_fail();
    } finally {
      isChangingPassword = false;
    }
  }

  async function handleSendResetEmail() {
    if (!settings.email) {
      passwordErrorMsg = m.settings_email_not_linked();
      return;
    }
    isSendingResetEmail = true;
    passwordSuccessMsg = "";
    passwordErrorMsg = "";

    try {
      const res = await apiRequest("/api/Regestration/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email: settings.email })
      });
      passwordSuccessMsg = res?.message || `${m.settings_reset_email_sent()} ${settings.email}`;
      setTimeout(() => (passwordSuccessMsg = ""), 6000);
    } catch (e) {
      passwordErrorMsg = e.message || m.settings_reset_email_fail();
    } finally {
      isSendingResetEmail = false;
    }
  }
</script>

<DashboardLayout>
  <div class="settings-page">
    <header class="page-header">
      <div class="header-left">
        <h1>{m.settings_title()}</h1>
        <p class="header-subtitle">
          {m.settings_subtitle()}
        </p>
      </div>
    </header>

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>{m.common_loading()}</p>
      </div>
    {:else}
      {#if successMsg}
        <div class="alert alert-success">
          <CheckCircle2 size={18} />
          <span>{successMsg}</span>
        </div>
      {/if}
      {#if errorMsg}
        <div class="alert alert-danger">
          <AlertCircle size={18} />
          <span>{errorMsg}</span>
        </div>
      {/if}

      <div class="card settings-card subscription-card">
        <div class="sub-header-row">
          <div class="sub-title-wrap">
            <div class="sub-icon-box">
              <Coins size={22} class="text-rose" />
            </div>
            <div>
              <div class="sub-heading-flex">
                <h3>{m.settings_sub_title()}</h3>
                {#if isSubscriptionActive}
                  <span class="badge badge-active">
                    <span class="badge-dot"></span>
                    {m.settings_sub_active()} ({remainingDays} {m.settings_sub_days()})
                  </span>
                {:else}
                  <span class="badge badge-expired">
                    <span class="badge-dot"></span>
                    {m.settings_sub_expired()}
                  </span>
                {/if}
              </div>
              <p class="sub-description">
                {m.settings_sub_desc()}
              </p>
            </div>
          </div>

          <div class="sub-actions-top">
            <button 
              type="button" 
              class="btn btn-primary"
              on:click={toggleRenewForm}
            >
              <RefreshCw size={16} />
              <span>{isRenewOpen ? m.settings_sub_renew_hide() : m.settings_sub_renew_btn()}</span>
            </button>
          </div>
        </div>

        <div class="sub-meta-grid">
          <div class="sub-meta-item">
            <span class="meta-label">{m.settings_sub_valid_until()}</span>
            <strong class="meta-value">{formatDate(settings.nextPayment)}</strong>
          </div>
          <div class="sub-meta-item">
            <span class="meta-label">{m.settings_sub_last_payment()}</span>
            <strong class="meta-value">{formatDate(settings.lastPayment)}</strong>
          </div>
          <div class="sub-meta-item">
            <span class="meta-label">{m.settings_sub_cost()}</span>
            <strong class="meta-value text-rose">{subscriptionAmount} {m.settings_sub_cost_suffix()}</strong>
          </div>
        </div>

        {#if isRenewOpen}
          <div class="renew-panel">
            <div class="renew-panel-header">
              <Sparkles size={18} class="text-rose" />
              <h4>{m.settings_ton_renew_title()}</h4>
            </div>

            {#if renewSuccessMsg}
              <div class="alert alert-success">
                <CheckCircle2 size={18} />
                <span>{renewSuccessMsg}</span>
              </div>
            {/if}
            {#if renewErrorMsg}
              <div class="alert alert-danger">
                <AlertCircle size={18} />
                <span>{renewErrorMsg}</span>
              </div>
            {/if}

            <div class="renew-grid">
              <div class="renew-left-box">
                <div class="qr-code-card">
                  <canvas bind:this={qrCanvas}></canvas>
                </div>
                <a 
                  href={paymentLink} 
                  target="_blank" 
                  rel="noopener noreferrer" 
                  class="btn-wallet-link"
                >
                  <ExternalLink size={13} />
                  <span>{m.settings_open_in_wallet()}</span>
                </a>
              </div>

              <div class="renew-right-box">
                <div class="step-card">
                  <span class="step-num">1</span>
                  <div class="step-content">
                    <h5>{m.settings_send_ton_to({ amount: subscriptionAmount })}</h5>
                    <div class="copy-wallet-box">
                      <code class="wallet-text">{platformWalletAddress || m.settings_loading_address()}</code>
                      <button 
                        type="button" 
                        class="btn-copy-sm" 
                        on:click={copyAddress}
                        title={m.common_copy()}
                      >
                        {#if copied}
                          <Check size={15} color="var(--pastel-sage)" />
                        {:else}
                          <Copy size={15} />
                        {/if}
                      </button>
                    </div>
                  </div>
                </div>

                <div class="step-card">
                  <span class="step-num">2</span>
                  <div class="step-content">
                    <h5>{m.settings_paste_hash_label()}</h5>
                    <div class="tx-input-wrap">
                      <input 
                        type="text" 
                        class="input" 
                        bind:value={txHash}
                        placeholder={m.settings_hash_placeholder()} 
                      />
                      <button 
                        type="button" 
                        class="btn btn-primary"
                        disabled={isRenewing || !txHash.trim()}
                        on:click={handleRenewSubscription}
                      >
                        <ShieldCheck size={16} />
                        <span>{isRenewing ? m.settings_checking_renewal() : m.settings_confirm_renewal()}</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        {/if}
      </div>

      <form on:submit|preventDefault={handleSave} class="card settings-card mt-4">
        <div class="section-title">
          <Building2 size={20} class="sec-icon rose" />
          <div>
            <h3>{m.settings_general_title()}</h3>
            <p>{m.settings_general_desc()}</p>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label for="ownerName">{m.settings_owner_name_label()}</label>
            <div class="input-icon-wrap">
              <User size={16} class="input-icon" />
              <input
                id="ownerName"
                type="text"
                class="input has-icon"
                bind:value={settings.ownerName}
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label for="barbershopName">{m.settings_salon_name_label()}</label>
            <div class="input-icon-wrap">
              <Building2 size={16} class="input-icon" />
              <input
                id="barbershopName"
                type="text"
                class="input has-icon"
                bind:value={settings.barbershopName}
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label for="barbershopAddress">{m.settings_address_label()}</label>
            <div class="input-icon-wrap">
              <MapPin size={16} class="input-icon" />
              <input
                id="barbershopAddress"
                type="text"
                class="input has-icon"
                bind:value={settings.barbershopAddress}
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label for="timeZone">{m.settings_timezone_label()}</label>
            <div class="input-icon-wrap">
              <Globe size={16} class="input-icon" />
              <input
                id="timeZone"
                type="text"
                class="input has-icon"
                bind:value={settings.timeZone}
                placeholder="Europe/Kyiv"
                required
              />
            </div>
          </div>
          <div class="form-group">
            <label for="phoneNumber">{m.settings_phone_label()}</label>
            <div class="input-icon-wrap">
              <Phone size={16} class="input-icon" />
              <input
                id="phoneNumber"
                type="tel"
                class="input has-icon"
                bind:value={settings.phoneNumber}
                placeholder="+380991234567"
              />
            </div>
            <small class="hint">{m.settings_phone_hint()}</small>
          </div>

          <div class="form-group">
            <label for="telegramId">{m.settings_tg_id_label()}</label>
            <div class="input-icon-wrap">
              <Send size={16} class="input-icon" />
              <input
                id="telegramId"
                type="text"
                class="input has-icon"
                bind:value={settings.telegramId}
                placeholder="123456789"
              />
            </div>
            <small class="hint">{m.settings_tg_id_hint()}</small>
          </div>
        </div>

        <div class="form-group full-width mt-3">
          <label for="description">{m.settings_bot_desc_label()}</label>
          <textarea
            id="description"
            class="input"
            bind:value={settings.barbershopDescription}
            rows="3"
            required
          ></textarea>
        </div>

        <div class="divider"></div>

        <div class="section-title">
          <Bot size={20} class="sec-icon rose" />
          <div class="flex-between-head">
            <div>
              <h3>{m.settings_bot_title()}</h3>
              <p>{m.settings_bot_desc()}</p>
            </div>
            <a href="/dashboard/bot-setup" class="guide-link">
              <Sparkles size={14} />
              <span>{m.settings_step_guide()}</span>
              <ExternalLink size={12} />
            </a>
          </div>
        </div>

        <!-- Quick 30-sec cheat sheet card -->
        <div class="bot-quick-help-card">
          <div class="quick-help-header">
            <strong>{m.settings_quick_help_title()}</strong>
            <a href="/dashboard/bot-setup" class="quick-help-more">{m.settings_quick_help_link()}</a>
          </div>
          <div class="quick-help-steps">
            <div class="quick-step-chip">
              <span class="chip-num">1</span>
              <span>{m.settings_step1_bullet()}</span>
            </div>
            <div class="quick-step-chip">
              <span class="chip-num">2</span>
              <span>{m.settings_step2_bullet()}</span>
            </div>
            <div class="quick-step-chip">
              <span class="chip-num">3</span>
              <span>{m.settings_step3_bullet()}</span>
            </div>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group full-width">
            <label for="botToken">{m.settings_bot_token_label()}</label>
            <div class="input-icon-wrap">
              <Bot size={16} class="input-icon" />
              <input
                id="botToken"
                type={showBotToken ? "text" : "password"}
                class="input has-icon has-toggle"
                bind:value={settings.botToken}
                placeholder="1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ..."
              />
              <button
                type="button"
                class="btn-input-toggle"
                on:click={() => (showBotToken = !showBotToken)}
                title={showBotToken ? m.settings_hide_token() : m.settings_show_token()}
              >
                {#if showBotToken}
                  <EyeOff size={16} />
                {:else}
                  <Eye size={16} />
                {/if}
              </button>
            </div>
            <small class="hint">{m.settings_bot_token_hint()}</small>
          </div>

          <div class="form-group full-width">
            <label for="botUsername">{m.settings_bot_user_label()}</label>
            <div class="input-icon-wrap">
              <span class="input-prefix">@</span>
              <input
                id="botUsername"
                type="text"
                class="input has-prefix"
                bind:value={settings.botUsername}
                placeholder="my_barbershop_bot"
              />
              {#if settings.botUsername}
                <a
                  href="https://t.me/{settings.botUsername.replace(/^@/, '')}"
                  target="_blank"
                  rel="noreferrer"
                  class="btn-input-link"
                  title={m.settings_bot_open_tooltip()}
                >
                  <ExternalLink size={13} />
                  <span>{m.settings_bot_open()}</span>
                </a>
              {/if}
            </div>
            <small class="hint">{m.settings_bot_user_hint()}</small>
          </div>
        </div>

        <div class="divider"></div>

        <div class="section-title">
          <Bell size={20} class="sec-icon sage" />
          <div>
            <h3>{m.settings_params_title()}</h3>
            <p>{m.settings_params_desc()}</p>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group full-width">
            <label for="walletAddress">{m.settings_wallet_label()}</label>
            <div class="input-icon-wrap">
              <Wallet size={16} class="input-icon" />
              <input
                id="walletAddress"
                type="text"
                class="input has-icon"
                bind:value={settings.walletAddress}
                placeholder="UQD..."
              />
            </div>
          </div>

          <div class="form-group">
            <label for="reminder">{m.settings_reminder_label()}</label>
            <div class="input-icon-wrap">
              <Bell size={16} class="input-icon" />
              <input
                id="reminder"
                type="number"
                class="input has-icon"
                bind:value={settings.reminderHoursBefore}
                min="1"
                max="72"
                required
              />
            </div>
            <small class="hint">{m.settings_reminder_hint()}</small>
          </div>

          <div class="form-group">
            <label for="winBackDays">{m.settings_winback_label()}</label>
            <div class="input-icon-wrap">
              <Bell size={16} class="input-icon" />
              <input
                id="winBackDays"
                type="number"
                class="input has-icon"
                bind:value={settings.winBackDays}
                min="0"
                max="365"
                placeholder={m.settings_winback_placeholder()}
              />
            </div>
            <small class="hint">{m.settings_winback_hint()}</small>
          </div>

          <div class="form-group">
            <label for="masterFee">{m.settings_fee_label()}</label>
            <div class="input-icon-wrap">
              <Percent size={16} class="input-icon" />
              <input
                id="masterFee"
                type="number"
                step="0.1"
                min="0"
                max="100"
                class="input has-icon"
                bind:value={settings.masterFee}
                placeholder="0"
              />
            </div>
            <small class="hint">{m.settings_fee_hint()}</small>
          </div>
        </div>

        <div class="actions">
          <button
            type="submit"
            class="btn btn-primary btn-lg"
            disabled={isSaving}
          >
            <Save size={18} />
            <span>{isSaving ? m.common_loading() : m.settings_save_btn()}</span>
          </button>
        </div>
      </form>

      <!-- Password & Security Card -->
      <div class="card settings-card mt-4">
        <div class="section-title">
          <KeyRound size={20} class="sec-icon lavender" />
          <div>
            <h3>{m.settings_security_title()}</h3>
            <p>{m.settings_security_desc()}</p>
          </div>
        </div>

        {#if settings.email}
          <div class="email-info-badge">
            <Mail size={15} />
            <span>{m.settings_linked_account()} <strong>{settings.email}</strong></span>
          </div>
        {/if}

        {#if passwordSuccessMsg}
          <div class="alert alert-success mt-3">
            <CheckCircle2 size={18} />
            <span>{passwordSuccessMsg}</span>
          </div>
        {/if}
        {#if passwordErrorMsg}
          <div class="alert alert-danger mt-3">
            <AlertCircle size={18} />
            <span>{passwordErrorMsg}</span>
          </div>
        {/if}

        <form on:submit|preventDefault={handleChangePassword} class="password-form mt-3">
          <div class="form-grid">
            <div class="form-group full-width">
              <label for="currentPassword">{m.settings_current_pwd_label()}</label>
              <div class="input-icon-wrap">
                <Lock size={16} class="input-icon" />
                <input
                  id="currentPassword"
                  type={showCurrentPassword ? "text" : "password"}
                  class="input has-icon has-toggle"
                  bind:value={currentPassword}
                  placeholder="••••••••"
                  required
                />
                <button
                  type="button"
                  class="btn-input-toggle"
                  on:click={() => (showCurrentPassword = !showCurrentPassword)}
                  title={showCurrentPassword ? m.settings_hide_pwd() : m.settings_show_pwd()}
                >
                  {#if showCurrentPassword}
                    <EyeOff size={16} />
                  {:else}
                    <Eye size={16} />
                  {/if}
                </button>
              </div>
            </div>

            <div class="form-group">
              <label for="newPassword">{m.settings_new_pwd_label()}</label>
              <div class="input-icon-wrap">
                <Lock size={16} class="input-icon" />
                <input
                  id="newPassword"
                  type={showNewPassword ? "text" : "password"}
                  class="input has-icon has-toggle"
                  bind:value={newPassword}
                  placeholder={m.settings_new_pwd_placeholder()}
                  required
                />
                <button
                  type="button"
                  class="btn-input-toggle"
                  on:click={() => (showNewPassword = !showNewPassword)}
                  title={showNewPassword ? m.settings_hide_pwd() : m.settings_show_pwd()}
                >
                  {#if showNewPassword}
                    <EyeOff size={16} />
                  {:else}
                    <Eye size={16} />
                  {/if}
                </button>
              </div>
            </div>

            <div class="form-group">
              <label for="confirmPassword">{m.settings_confirm_new_pwd_label()}</label>
              <div class="input-icon-wrap">
                <Lock size={16} class="input-icon" />
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? "text" : "password"}
                  class="input has-icon has-toggle"
                  bind:value={confirmPassword}
                  placeholder="••••••••"
                  required
                />
                <button
                  type="button"
                  class="btn-input-toggle"
                  on:click={() => (showConfirmPassword = !showConfirmPassword)}
                  title={showConfirmPassword ? m.settings_hide_pwd() : m.settings_show_pwd()}
                >
                  {#if showConfirmPassword}
                    <EyeOff size={16} />
                  {:else}
                    <Eye size={16} />
                  {/if}
                </button>
              </div>
            </div>
          </div>

          <div class="password-actions mt-3">
            <button
              type="submit"
              class="btn btn-primary"
              disabled={isChangingPassword}
            >
              <Lock size={16} />
              <span>{isChangingPassword ? m.settings_updating_pwd() : m.settings_update_pwd_btn()}</span>
            </button>

            <button
              type="button"
              class="btn-forgot-link"
              disabled={isSendingResetEmail || !settings.email}
              on:click={handleSendResetEmail}
              title={m.settings_reset_email_tooltip()}
            >
              <Mail size={15} />
              <span>{isSendingResetEmail ? m.settings_sending_reset() : m.settings_forgot_pwd_btn()}</span>
            </button>
          </div>
        </form>
      </div>

      <div class="card settings-card mt-4">
        <div class="card-head">
          <Sun size={20} class="text-rose" />
          <div>
            <h3>{m.settings_appearance_title()}</h3>
            <p>{m.settings_appearance_desc()}</p>
          </div>
        </div>

        <div class="theme-options-grid">
          <button 
            type="button" 
            class="theme-card-option" 
            class:active={$theme === 'dark'}
            on:click={() => setTheme('dark')}
          >
            <div class="theme-preview dark-preview">
              <div class="preview-sidebar"></div>
              <div class="preview-content">
                <div class="preview-line"></div>
                <div class="preview-box"></div>
              </div>
            </div>
            <div class="theme-option-info">
              <Moon size={16} />
              <span>{m.settings_dark_theme()}</span>
            </div>
          </button>

          <button 
            type="button" 
            class="theme-card-option" 
            class:active={$theme === 'light'}
            on:click={() => setTheme('light')}
          >
            <div class="theme-preview light-preview">
              <div class="preview-sidebar"></div>
              <div class="preview-content">
                <div class="preview-line"></div>
                <div class="preview-box"></div>
              </div>
            </div>
            <div class="theme-option-info">
              <Sun size={16} />
              <span>{m.settings_light_theme()}</span>
            </div>
          </button>
        </div>
      </div>

      <div class="card settings-card mt-4">
        <div class="card-head">
          <HelpCircle size={20} class="text-rose" />
          <div>
            <h3>{m.settings_support_title()}</h3>
            <p>{m.settings_support_desc()}</p>
          </div>
        </div>

        <div class="support-card-content">
          <div class="support-info-text">
            <span>{m.settings_support_availability()}</span>
            <strong class="support-handle">@Eyed_Graff</strong>
          </div>
          <a 
            href="https://t.me/Eyed_Graff" 
            target="_blank" 
            rel="noopener noreferrer" 
            class="btn btn-secondary"
          >
            <Send size={16} />
            <span>{m.settings_support_btn()}</span>
            <ExternalLink size={14} />
          </a>
        </div>
      </div>
    {/if}
  </div>
</DashboardLayout>

<style>
  .settings-page {
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .page-header {
    margin-bottom: 2rem;
  }

  .header-left h1 {
    font-size: 1.85rem;
    font-weight: 700;
    margin-bottom: 0.35rem;
    color: var(--text-primary);
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 0.95rem;
  }

  .settings-card {
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 2rem;
    box-shadow: var(--shadow-glass);
  }

  /* Subscription Card */
  .subscription-card {
    border: 1px solid var(--border-glass);
    background: linear-gradient(135deg, var(--bg-surface), var(--bg-surface-elevated));
  }

  .sub-header-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1.5rem;
    flex-wrap: wrap;
  }

  .sub-title-wrap {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    flex: 1;
    min-width: 280px;
  }

  .sub-icon-box {
    width: 44px;
    height: 44px;
    border-radius: var(--radius-md);
    background: var(--pastel-rose-dim);
    border: 1px solid rgba(223, 158, 142, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .sub-heading-flex {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.35rem;
    flex-wrap: wrap;
  }

  .sub-heading-flex h3 {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .sub-description {
    color: var(--text-secondary);
    font-size: 0.9rem;
    margin: 0;
  }

  .badge {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.25rem 0.75rem;
    border-radius: var(--radius-pill);
    font-size: 0.78rem;
    font-weight: 700;
  }

  .badge-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: currentColor;
  }

  .badge-active {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.3);
  }

  .badge-expired {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.3);
  }

  .sub-meta-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1.25rem;
    margin-top: 1.5rem;
    padding-top: 1.5rem;
    border-top: 1px solid var(--border-subtle);
  }

  .sub-meta-item {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .meta-label {
    font-size: 0.8rem;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    font-weight: 600;
  }

  .meta-value {
    font-size: 1.05rem;
    color: var(--text-primary);
    font-weight: 700;
  }

  /* Renewal Panel */
  .renew-panel {
    margin-top: 1.75rem;
    padding-top: 1.5rem;
    border-top: 1px solid var(--border-subtle);
    animation: fadeIn 0.25s var(--ease-spring);
  }

  .renew-panel-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1.25rem;
  }

  .renew-panel-header h4 {
    font-size: 1.05rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .renew-grid {
    display: grid;
    grid-template-columns: 190px 1fr;
    gap: 1.75rem;
    align-items: start;
  }

  @media (max-width: 820px) {
    .renew-grid {
      grid-template-columns: 1fr;
    }
  }

  .renew-left-box {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
  }

  .qr-code-card {
    background: #ffffff;
    padding: 8px;
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-sm);
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    box-sizing: border-box;
  }

  .qr-code-card canvas {
    width: 100% !important;
    height: auto !important;
    display: block;
  }

  .btn-wallet-link {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.45rem;
    padding: 0.6rem 0.85rem;
    font-size: 0.82rem;
    font-weight: 600;
    width: 100%;
    border-radius: var(--radius-md);
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    text-decoration: none;
    transition: all 0.2s ease;
    box-sizing: border-box;
    text-align: center;
  }

  .btn-wallet-link:hover {
    background: var(--bg-surface-hover);
    color: var(--pastel-rose);
    border-color: var(--border-glass);
  }

  .step-card {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1.15rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    margin-bottom: 1rem;
    overflow: hidden;
  }

  .step-card:last-child {
    margin-bottom: 0;
  }

  .step-num {
    width: 26px;
    height: 26px;
    border-radius: 50%;
    background: var(--pastel-rose);
    color: #ffffff;
    font-weight: 700;
    font-size: 0.82rem;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .step-content {
    flex: 1;
    min-width: 0;
  }

  .step-content h5 {
    margin: 0 0 0.5rem 0;
    font-size: 0.92rem;
    font-weight: 600;
    color: var(--text-primary);
  }

  .copy-wallet-box {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--bg-canvas);
    padding: 0.5rem 0.75rem;
    border-radius: var(--radius-sm);
    border: 1px solid var(--border-subtle);
  }

  .wallet-text {
    font-family: monospace;
    font-size: 0.85rem;
    color: var(--text-secondary);
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .btn-copy-sm {
    background: transparent;
    border: none;
    color: var(--text-primary);
    cursor: pointer;
    padding: 4px;
    border-radius: var(--radius-sm);
    display: flex;
    align-items: center;
    justify-content: center;
    transition: opacity 0.2s;
  }

  .btn-copy-sm:hover {
    opacity: 0.75;
  }

  .tx-input-wrap {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    flex-wrap: wrap;
    width: 100%;
  }

  .tx-input-wrap input {
    flex: 1 1 240px;
    min-width: 0;
  }

  .tx-input-wrap button {
    flex-shrink: 0;
    white-space: nowrap;
  }

  /* Form & Settings styles */
  .card-head {
    display: flex;
    align-items: center;
    gap: 0.85rem;
  }

  .card-head h3 {
    font-size: 1.15rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .card-head p {
    color: var(--text-secondary);
    font-size: 0.88rem;
    margin: 0;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    margin-bottom: 1.5rem;
  }

  :global(.sec-icon) {
    padding: 8px;
    border-radius: var(--radius-md);
    flex-shrink: 0;
  }

  :global(.sec-icon.rose) {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
  }

  :global(.sec-icon.sage) {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
  }

  .section-title h3 {
    font-size: 1.15rem;
    font-weight: 700;
    margin: 0 0 0.15rem;
    color: var(--text-primary);
  }

  .bot-quick-help-card {
    background: rgba(223, 158, 142, 0.05);
    border: 1px solid rgba(223, 158, 142, 0.2);
    border-radius: var(--radius-md);
    padding: 0.9rem 1.15rem;
    margin-bottom: 1.25rem;
  }

  .quick-help-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.65rem;
    font-size: 0.88rem;
    color: var(--text-primary);
  }

  .quick-help-more {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--pastel-rose);
    text-decoration: none;
  }

  .quick-help-more:hover {
    text-decoration: underline;
  }

  .quick-help-steps {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
  }

  .quick-step-chip {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.84rem;
    color: var(--text-secondary);
  }

  .chip-num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    font-size: 0.72rem;
    font-weight: 700;
    flex-shrink: 0;
  }

  .form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.25rem;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .form-group.full-width {
    grid-column: 1 / -1;
    margin-top: 0.5rem;
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

  .has-icon {
    padding-left: 2.75rem !important;
  }

  .hint {
    font-size: 0.8rem;
    color: var(--text-muted);
    margin-top: 0.2rem;
  }

  .divider {
    height: 1px;
    background: var(--border-subtle);
    margin: 2.25rem 0;
  }

  .actions {
    margin-top: 2rem;
    display: flex;
    justify-content: flex-end;
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 5rem 0;
    color: var(--text-secondary);
    gap: 1rem;
  }

  .spinner-sm {
    width: 32px;
    height: 32px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
  }

  .mt-4 { margin-top: 1.5rem; }
  .text-rose { color: var(--pastel-rose); }
  .full-width { width: 100%; }

  /* Theme Options */
  .theme-options-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1.25rem;
    margin-top: 1.25rem;
  }

  .theme-card-option {
    background: var(--bg-surface-elevated);
    border: 2px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
    cursor: pointer;
    transition: all 0.2s ease;
    text-align: left;
  }

  .theme-card-option:hover {
    border-color: var(--border-glass);
    transform: translateY(-2px);
  }

  .theme-card-option.active {
    border-color: var(--pastel-rose);
    box-shadow: 0 0 0 1px var(--pastel-rose);
  }

  .theme-preview {
    height: 90px;
    border-radius: var(--radius-sm);
    display: flex;
    overflow: hidden;
    border: 1px solid rgba(255, 255, 255, 0.08);
  }

  .dark-preview {
    background: #0c0e12;
  }

  .dark-preview .preview-sidebar {
    width: 25%;
    background: #161a23;
    border-right: 1px solid rgba(255, 255, 255, 0.05);
  }

  .dark-preview .preview-content {
    flex: 1;
    padding: 8px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .dark-preview .preview-line {
    height: 8px;
    width: 50%;
    background: #df9e8e;
    border-radius: 4px;
  }

  .dark-preview .preview-box {
    flex: 1;
    background: #1e2430;
    border-radius: 4px;
  }

  .light-preview {
    background: #f8fafc;
  }

  .light-preview .preview-sidebar {
    width: 25%;
    background: #ffffff;
    border-right: 1px solid rgba(0, 0, 0, 0.06);
  }

  .light-preview .preview-content {
    flex: 1;
    padding: 8px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .light-preview .preview-line {
    height: 8px;
    width: 50%;
    background: #c86e5f;
    border-radius: 4px;
  }

  .light-preview .preview-box {
    flex: 1;
    background: #ffffff;
    border: 1px solid rgba(0, 0, 0, 0.08);
    border-radius: 4px;
  }

  .theme-option-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 600;
    font-size: 0.9rem;
    color: var(--text-primary);
  }

  /* Support Card */
  .support-card-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1.25rem;
    margin-top: 1.25rem;
    flex-wrap: wrap;
  }

  .support-info-text {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    font-size: 0.92rem;
    color: var(--text-secondary);
  }

  .support-handle {
    color: var(--pastel-rose);
    font-size: 1.05rem;
    font-weight: 700;
  }

  /* Password & Security Styles */
  .has-toggle {
    padding-right: 2.8rem;
  }

  .btn-input-toggle {
    position: absolute;
    right: 0.8rem;
    top: 50%;
    transform: translateY(-50%);
    background: transparent;
    border: none;
    color: var(--text-muted);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.3rem;
    border-radius: var(--radius-sm);
    transition: color 0.2s;
  }

  .btn-input-toggle:hover {
    color: var(--text-primary);
  }

  .input-prefix {
    position: absolute;
    left: 1rem;
    top: 50%;
    transform: translateY(-50%);
    color: var(--pastel-rose);
    font-weight: 700;
    font-size: 1.1rem;
    pointer-events: none;
  }

  .has-prefix {
    padding-left: 2.2rem;
    padding-right: 6.5rem;
  }

  .btn-input-link {
    position: absolute;
    right: 0.6rem;
    top: 50%;
    transform: translateY(-50%);
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.35rem 0.65rem;
    background: rgba(223, 158, 142, 0.12);
    border: 1px solid rgba(223, 158, 142, 0.25);
    border-radius: var(--radius-pill);
    color: var(--pastel-rose);
    font-size: 0.78rem;
    font-weight: 600;
    text-decoration: none;
    transition: all 0.2s;
  }

  .btn-input-link:hover {
    background: rgba(223, 158, 142, 0.22);
  }

  .flex-between-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .guide-link {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.85rem;
    color: var(--pastel-rose);
    font-weight: 600;
    text-decoration: none;
    padding: 0.35rem 0.75rem;
    background: rgba(223, 158, 142, 0.08);
    border: 1px solid rgba(223, 158, 142, 0.2);
    border-radius: var(--radius-pill);
    transition: all 0.2s;
  }

  .guide-link:hover {
    background: rgba(223, 158, 142, 0.18);
  }

  .email-info-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.45rem 0.85rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-secondary);
    font-size: 0.86rem;
    margin-top: 0.5rem;
  }

  .email-info-badge strong {
    color: var(--text-primary);
  }

  .password-actions {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
    margin-top: 2rem !important;
    padding-top: 0.5rem;
  }

  .btn-forgot-link {
    background: transparent;
    border: none;
    color: var(--pastel-rose);
    font-size: 0.88rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.5rem 0;
    transition: opacity 0.2s;
  }

  .btn-forgot-link:hover {
    text-decoration: underline;
  }

  .btn-forgot-link:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  @media (max-width: 768px) {
    .settings-card {
      padding: 1.25rem 1rem;
      border-radius: var(--radius-md);
    }

    .header-left h1 {
      font-size: 1.6rem;
    }

    .form-grid {
      grid-template-columns: 1fr;
      gap: 1rem;
    }

    .sub-title-wrap {
      min-width: 100%;
    }

    .sub-meta-grid {
      grid-template-columns: 1fr;
      gap: 0.85rem;
    }

    .actions .btn {
      width: 100%;
    }

    .password-actions {
      flex-direction: column-reverse;
      align-items: stretch;
      gap: 0.75rem;
    }

    .password-actions .btn {
      width: 100%;
    }

    .btn-forgot-link {
      justify-content: center;
    }
  }

  @media (max-width: 480px) {
    .header-left h1 {
      font-size: 1.35rem;
    }

    .theme-options-grid {
      grid-template-columns: 1fr;
    }
  }
</style>

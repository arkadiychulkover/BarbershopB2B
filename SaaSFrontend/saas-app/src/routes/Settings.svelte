<script>
  import { onMount } from "svelte";
  import DashboardLayout from "../components/DashboardLayout.svelte";
  import { apiRequest } from "../lib/api";
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
  } from "lucide-svelte";

  let isLoading = true;
  let isSaving = false;
  let successMsg = "";
  let errorMsg = "";

  let settings = {
    barbershopName: "",
    barbershopAddress: "",
    barbershopDescription: "",
    ownerName: "",
    timeZone: "",
    reminderHoursBefore: 24,
    walletAddress: "",
    // Hidden defaults to preserve backend contract
    logoUrl: "",
    brandColor: "#df9e8e",
    depositEnabled: false,
    depositPercent: 0,
    masterFee: 0,
  };

  onMount(async () => {
    try {
      const data = await apiRequest("/api/Settings");
      settings = { ...settings, ...data };
    } catch (e) {
      errorMsg = "Не удалось загрузить настройки";
    } finally {
      isLoading = false;
    }
  });

  async function handleSave() {
    isSaving = true;
    successMsg = "";
    errorMsg = "";

    try {
      await apiRequest("/api/Settings", {
        method: "PUT",
        body: JSON.stringify(settings),
      });
      successMsg = "Настройки успешно сохранены!";
      setTimeout(() => (successMsg = ""), 3500);
    } catch (e) {
      errorMsg = e.message || "Ошибка при сохранении";
    } finally {
      isSaving = false;
    }
  }
</script>

<DashboardLayout>
  <div class="settings-page">
    <header class="page-header">
      <div class="header-left">
        <h1>Настройки заведения</h1>
        <p class="header-subtitle">
          Управление профилем барбершопа, контактными данными и параметрами
          уведомлений
        </p>
      </div>
    </header>

    {#if isLoading}
      <div class="loading-wrap">
        <div class="spinner-sm"></div>
        <p>Загрузка настроек...</p>
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

      <form on:submit|preventDefault={handleSave} class="card settings-card">
        <!-- Section 1: General Info -->
        <div class="section-title">
          <Building2 size={20} class="sec-icon rose" />
          <div>
            <h3>Основная информация</h3>
            <p>Контактные данные и адрес вашего барбершопа</p>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label for="ownerName">Имя владельца</label>
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
            <label for="barbershopName">Название барбершопа</label>
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
            <label for="barbershopAddress">Адрес</label>
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
            <label for="timeZone">Часовой пояс</label>
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
        </div>

        <div class="form-group full-width">
          <label for="description"
            >Описание для клиентов (в Telegram боте)</label
          >
          <textarea
            id="description"
            class="input"
            bind:value={settings.barbershopDescription}
            rows="3"
            required
          ></textarea>
        </div>

        <div class="divider"></div>

        <!-- Section 2: Parameters & Notifications -->
        <div class="section-title">
          <Bell size={20} class="sec-icon sage" />
          <div>
            <h3>Параметры и уведомления</h3>
            <p>TON кошелек заведения и интервалы напоминаний для клиентов</p>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group full-width">
            <label for="walletAddress">TON кошелёк (для оплаты тарифа)</label>
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

          <div class="form-group full-width">
            <label for="reminder"
              >Авто-напоминание клиентам (за N часов до визита)</label
            >
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
            <small class="hint"
              >Бот автоматически отправит уведомление клиенту о предстоящей
              записи.</small
            >
          </div>
        </div>

        <div class="actions">
          <button
            type="submit"
            class="btn btn-primary btn-lg"
            disabled={isSaving}
          >
            <Save size={18} />
            <span>{isSaving ? "Сохранение..." : "Сохранить настройки"}</span>
          </button>
        </div>
      </form>
    {/if}
  </div>
</DashboardLayout>

<style>
  .settings-page {
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .page-header {
    margin-bottom: 2.25rem;
  }

  .header-left h1 {
    font-size: 2.2rem;
    margin-bottom: 0.35rem;
  }

  .header-subtitle {
    color: var(--text-secondary);
    font-size: 1rem;
  }

  .settings-card {
    padding: 2.75rem 2.5rem;
    max-width: 860px;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1.5rem;
  }

  :global(.sec-icon) {
    width: 44px;
    height: 44px;
    padding: 10px;
    border-radius: var(--radius-md);
    flex-shrink: 0;
    box-sizing: border-box;
  }

  :global(.sec-icon.rose) {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.25);
  }

  :global(.sec-icon.sage) {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
  }

  .section-title h3 {
    font-size: 1.25rem;
    margin-bottom: 0.15rem;
  }

  .section-title p {
    font-size: 0.85rem;
    color: var(--text-secondary);
  }

  .divider {
    height: 1px;
    background: var(--border-subtle);
    margin: 2.25rem 0;
  }

  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
  }

  @media (max-width: 680px) {
    .form-grid {
      grid-template-columns: 1fr;
    }
    .settings-card {
      padding: 1.75rem 1.25rem;
    }
  }

  .full-width {
    grid-column: 1 / -1;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    margin-bottom: 0.5rem;
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

  textarea.input {
    resize: vertical;
    min-height: 80px;
  }

  .hint {
    color: var(--text-muted);
    font-size: 0.78rem;
    margin-top: 0.2rem;
  }

  .actions {
    margin-top: 2.5rem;
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
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    width: 32px;
    height: 32px;
    animation: spinSmooth 0.85s linear infinite;
  }
</style>

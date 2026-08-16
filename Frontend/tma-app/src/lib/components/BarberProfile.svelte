<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../api';
  import { showAlert, hapticSuccess, hapticError } from '../telegram';
  import Icon from './Icon.svelte';

  interface MasterProfile {
    id: string;
    name: string;
    description: string | null;
    telegramId: string | null;
    telegramUsername: string | null;
    photoUrl: string | null;
    rating: number;
    reviewsCount: number;
    isActive: boolean;
  }

  let profile: MasterProfile | null = null;
  let loading = true;
  let saving = false;
  let errorMsg = '';
  let successMsg = '';

  let formName = '';
  let formUsername = '';
  let formDescription = '';

  onMount(async () => {
    await loadProfile();
  });

  async function loadProfile() {
    loading = true;
    errorMsg = '';
    try {
      profile = await apiFetch('/api/Barber/my-profile');
      if (profile) {
        formName = profile.name || '';
        formUsername = profile.telegramUsername ? profile.telegramUsername.replace(/^@/, '') : '';
        formDescription = profile.description || '';
      }
    } catch (e: any) {
      console.error('Failed to load profile:', e);
      errorMsg = 'Не удалось загрузить профиль мастера';
    } finally {
      loading = false;
    }
  }

  async function saveProfile() {
    if (!formName.trim()) {
      showAlert('Пожалуйста, укажите имя');
      return;
    }

    saving = true;
    errorMsg = '';
    successMsg = '';

    try {
      const cleanUsername = formUsername.trim().replace(/^@/, '');
      if (cleanUsername) {
        const usernameRegex = /^[a-zA-Z0-9_]{5,32}$/;
        if (!usernameRegex.test(cleanUsername)) {
          showAlert('Некорректный username. Допустимы латинские буквы, цифры и _ (от 5 до 32 символов).');
          saving = false;
          return;
        }
      }

      const response = await apiFetch('/api/Barber/my-profile', {
        method: 'PUT',
        body: {
          name: formName.trim(),
          description: formDescription.trim() || null,
          telegramUsername: cleanUsername || null
        }
      });

      if (response && response.profile) {
        profile = response.profile;
        formUsername = profile?.telegramUsername ? profile.telegramUsername.replace(/^@/, '') : '';
      }

      hapticSuccess();
      successMsg = 'Данные профиля успешно сохранены!';
      setTimeout(() => {
        successMsg = '';
      }, 4000);
    } catch (e: any) {
      hapticError();
      console.error('Save profile error:', e);
      errorMsg = e?.message || 'Ошибка сохранения профиля';
      showAlert(errorMsg);
    } finally {
      saving = false;
    }
  }
</script>

<div class="profile-container">
  {#if loading}
    <div class="loading-wrap">
      <div class="spinner-sm"></div>
      <p>Загрузка профиля...</p>
    </div>
  {:else if errorMsg && !profile}
    <div class="error-card">
      <Icon name="alert" size={24} color="var(--pastel-coral)" />
      <p>{errorMsg}</p>
      <button class="retry-btn" on:click={loadProfile}>Повторить</button>
    </div>
  {:else if profile}
    <!-- Profile Header Card -->
    <div class="card profile-header-card">
      <div class="avatar-wrap">
        <div class="avatar">
          {(profile.name || 'M')[0].toUpperCase()}
        </div>
        {#if profile.isActive}
          <span class="online-indicator" title="Активен"></span>
        {/if}
      </div>

      <div class="header-info">
        <h2>{profile.name}</h2>
        <span class="role-badge">{profile.description || 'Мастер'}</span>
      </div>

      <div class="stats-row">
        <div class="stat-pill">
          <Icon name="star" size={14} color="var(--pastel-amber)" />
          <span class="stat-val">{profile.rating > 0 ? profile.rating.toFixed(1) : '—'}</span>
          <span class="stat-lbl">Рейтинг</span>
        </div>
        <div class="stat-pill">
          <Icon name="comment" size={14} color="var(--pastel-lavender)" />
          <span class="stat-val">{profile.reviewsCount}</span>
          <span class="stat-lbl">Отзывов</span>
        </div>
      </div>
    </div>

    <!-- Edit Profile & Telegram Username Form -->
    <form class="card profile-form-card" on:submit|preventDefault={saveProfile}>
      <div class="section-title-wrap">
        <Icon name="user" size={18} color="var(--pastel-rose)" />
        <h3>Настройки аккаунта и связи</h3>
      </div>

      {#if successMsg}
        <div class="alert-success">
          <Icon name="check-circle" size={16} color="var(--pastel-sage)" />
          <span>{successMsg}</span>
        </div>
      {/if}

      {#if errorMsg}
        <div class="alert-error">
          <Icon name="alert" size={16} color="var(--pastel-coral)" />
          <span>{errorMsg}</span>
        </div>
      {/if}

      <!-- Telegram Username Field (Key feature) -->
      <div class="form-group highlight-group">
        <label for="tg-username">
          <span>Telegram Юзернейм</span>
          <span class="badge-accent">Для связи с клиентами</span>
        </label>
        <div class="input-with-prefix">
          <span class="prefix">@</span>
          <input
            id="tg-username"
            type="text"
            bind:value={formUsername}
            placeholder="username"
            autocomplete="off"
            spellcheck="false"
          />
        </div>
        <p class="field-hint">
          Клиенты смогут нажать «Написать мастеру» в своей записи, и Telegram откроет прямой диалог с вами по ссылке 
          {#if formUsername.trim()}
            <strong>t.me/{formUsername.trim().replace(/^@/, '')}</strong>
          {:else}
            <em>(укажите ваш @username)</em>
          {/if}.
        </p>
      </div>

      <!-- Telegram ID (Read-only badge) -->
      {#if profile.telegramId}
        <div class="form-group">
          <label for="tg-id">Telegram ID (цифровой идентификатор)</label>
          <input
            id="tg-id"
            type="text"
            value={profile.telegramId}
            disabled
            class="readonly-input"
          />
          <p class="field-hint">Привязан к вашей учетной записи мастера.</p>
        </div>
      {/if}

      <!-- Master Name -->
      <div class="form-group">
        <label for="master-name">Имя мастера</label>
        <input
          id="master-name"
          type="text"
          bind:value={formName}
          placeholder="Ваше имя"
          required
        />
      </div>

      <!-- Description / Role -->
      <div class="form-group">
        <label for="master-desc">Специализация / Описание</label>
        <input
          id="master-desc"
          type="text"
          bind:value={formDescription}
          placeholder="Top Barber / Fade & Beard Master"
        />
      </div>

      <div class="form-actions">
        <button type="submit" class="save-btn" disabled={saving || !formName.trim()}>
          {#if saving}
            <div class="spinner-inline"></div>
            <span>Сохранение...</span>
          {:else}
            <Icon name="check" size={16} color="var(--text-inverse)" />
            <span>Сохранить профиль</span>
          {/if}
        </button>
      </div>
    </form>
  {/if}
</div>

<style>
  .profile-container {
    display: flex;
    flex-direction: column;
    gap: 16px;
    animation: fadeIn 0.3s var(--ease-spring);
    padding-bottom: 24px;
  }

  .card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-glass);
    backdrop-filter: blur(16px);
    padding: 20px;
    box-sizing: border-box;
  }

  .profile-header-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 12px;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
  }

  .avatar-wrap {
    position: relative;
  }

  .avatar {
    width: 72px;
    height: 72px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), #9e5b4d);
    color: var(--text-inverse);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 28px;
    font-weight: 700;
    box-shadow: 0 8px 24px var(--pastel-rose-glow);
    border: 2px solid rgba(255, 255, 255, 0.12);
  }

  .online-indicator {
    position: absolute;
    bottom: 2px;
    right: 2px;
    width: 14px;
    height: 14px;
    border-radius: 50%;
    background: var(--pastel-sage);
    border: 2px solid var(--bg-surface);
    box-shadow: 0 0 8px rgba(168, 213, 186, 0.6);
  }

  .header-info h2 {
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 4px;
  }

  .role-badge {
    display: inline-block;
    font-size: 13px;
    color: var(--pastel-rose);
    background: var(--pastel-rose-dim);
    padding: 4px 12px;
    border-radius: var(--radius-pill);
    font-weight: 500;
    border: 1px solid rgba(223, 158, 142, 0.2);
  }

  .stats-row {
    display: flex;
    gap: 12px;
    width: 100%;
    margin-top: 4px;
  }

  .stat-pill {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 10px 14px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
  }

  .stat-val {
    font-size: 15px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .stat-lbl {
    font-size: 12px;
    color: var(--text-secondary);
  }

  .profile-form-card {
    display: flex;
    flex-direction: column;
    gap: 18px;
  }

  .section-title-wrap {
    display: flex;
    align-items: center;
    gap: 8px;
    border-bottom: 1px solid var(--border-subtle);
    padding-bottom: 12px;
  }

  .section-title-wrap h3 {
    margin: 0;
    font-size: 16px;
    font-weight: 600;
    color: var(--text-primary);
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .form-group label {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .badge-accent {
    font-size: 11px;
    font-weight: 600;
    color: var(--pastel-rose);
    background: var(--pastel-rose-dim);
    padding: 2px 8px;
    border-radius: var(--radius-pill);
  }

  .highlight-group {
    background: var(--pastel-rose-dim);
    padding: 14px;
    border-radius: var(--radius-md);
    border: 1px solid rgba(223, 158, 142, 0.2);
  }

  .input-with-prefix {
    display: flex;
    align-items: center;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    overflow: hidden;
    transition: all 0.2s ease;
  }

  .input-with-prefix:focus-within {
    border-color: var(--pastel-rose);
    box-shadow: 0 0 0 2px var(--pastel-rose-dim);
  }

  .prefix {
    padding: 0 0 0 14px;
    font-size: 16px;
    font-weight: 600;
    color: var(--pastel-rose);
  }

  .input-with-prefix input {
    flex: 1;
    border: none;
    background: transparent;
    padding: 12px 14px 12px 6px;
    color: var(--text-primary);
    font-size: 14px;
    outline: none;
  }

  input[type="text"] {
    width: 100%;
    padding: 12px 14px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-size: 14px;
    box-sizing: border-box;
    outline: none;
    transition: all 0.2s ease;
  }

  input[type="text"]:focus {
    border-color: var(--pastel-rose);
    box-shadow: 0 0 0 2px var(--pastel-rose-dim);
  }

  .readonly-input {
    opacity: 0.85;
    background: var(--bg-canvas-subtle) !important;
    color: var(--text-secondary) !important;
    cursor: not-allowed;
  }

  .field-hint {
    font-size: 12px;
    color: var(--text-secondary);
    margin: 2px 0 0;
    line-height: 1.4;
  }

  .field-hint strong {
    color: var(--pastel-rose);
  }

  .alert-success {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 14px;
    background: rgba(168, 213, 186, 0.12);
    border: 1px solid rgba(168, 213, 186, 0.3);
    border-radius: var(--radius-md);
    color: var(--pastel-sage);
    font-size: 13px;
    font-weight: 500;
  }

  .alert-error {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 14px;
    background: rgba(242, 139, 130, 0.12);
    border: 1px solid rgba(242, 139, 130, 0.3);
    border-radius: var(--radius-md);
    color: var(--pastel-coral);
    font-size: 13px;
    font-weight: 500;
  }

  .form-actions {
    margin-top: 6px;
  }

  .save-btn {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 14px 20px;
    border-radius: var(--radius-pill);
    font-size: 15px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 6px 20px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }

  .save-btn:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 8px 24px var(--pastel-rose-glow);
  }

  .save-btn:active:not(:disabled) {
    transform: scale(0.98);
  }

  .save-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .loading-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 60px 20px;
    color: var(--text-secondary);
    gap: 12px;
  }

  .spinner-sm {
    width: 32px;
    height: 32px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
  }

  .spinner-inline {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.3);
    border-top: 2px solid #fff;
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
  }

  .error-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 32px 20px;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    text-align: center;
    color: var(--text-secondary);
  }

  .retry-btn {
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    cursor: pointer;
  }
</style>

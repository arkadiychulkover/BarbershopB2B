<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../lib/api';
  import Icon from '../lib/components/Icon.svelte';
  import { theme, toggleTmaTheme } from '../lib/stores/theme';

  let currentView: 'booking' | 'appointments' | 'profile' = 'booking';
  let myAppointments = [];
  let loadingAppointments = false;

  let step = 1;
  let loading = false;
  let error = '';

  let masters = [];
  let services = [];
  let availableSlots = [];

  let selectedMaster = null;
  let selectedService = null;
  let selectedDate = '';
  let selectedTime = '';

  let masterRating = null;
  let masterReviews = [];
  let showReviews = false;

  let reviewingAppt = null;
  let reviewRating = 5;
  let reviewComment = '';
  let submittingReview = false;
  let clientProfile = null;
  let showPhoneModal = false;
  let phoneInput = '';
  let savingPhone = false;
  let phoneError = '';

  let editingPhone = false;
  let profilePhoneInput = '';
  let profilePhoneError = '';
  let profilePhoneSuccess = '';
  let savingProfilePhone = false;

  function validatePhone(phone: string): { valid: boolean; cleaned: string; error?: string } {
    if (!phone || !phone.trim()) {
      return { valid: false, cleaned: '', error: 'Укажите номер телефона' };
    }
    const cleaned = phone.trim().replace(/[\s\-\(\)]/g, '');
    const regex = /^\+[0-9]{1,3}[0-9]{9}$/;
    if (!regex.test(cleaned)) {
      return { 
        valid: false, 
        cleaned, 
        error: 'Номер должен начинаться с + и содержать код страны (1-3 цифры) и 9 цифр номера (например, +380991234567 или +79991234567)' 
      };
    }
    return { valid: true, cleaned };
  }

  function startEditPhone() {
    profilePhoneInput = clientProfile?.phone || '';
    profilePhoneError = '';
    profilePhoneSuccess = '';
    editingPhone = true;
  }

  function cancelEditPhone() {
    editingPhone = false;
    profilePhoneError = '';
  }

  async function saveProfilePhone() {
    const val = validatePhone(profilePhoneInput);
    if (!val.valid) {
      profilePhoneError = val.error || 'Некорректный номер';
      return;
    }

    savingProfilePhone = true;
    profilePhoneError = '';
    profilePhoneSuccess = '';
    try {
      await apiFetch('/api/Clients/update-phone', {
        method: 'POST',
        body: { phone: val.cleaned }
      });
      if (!clientProfile) clientProfile = {};
      clientProfile.phone = val.cleaned;
      profilePhoneSuccess = 'Номер телефона успешно сохранен!';
      editingPhone = false;
      setTimeout(() => {
        profilePhoneSuccess = '';
      }, 3000);
    } catch (err: any) {
      profilePhoneError = err.message || 'Ошибка сохранения номера';
    } finally {
      savingProfilePhone = false;
    }
  }

  onMount(async () => {
    await fetchMasters();
    fetchClientProfile();
  });

  async function fetchClientProfile() {
    try {
      clientProfile = await apiFetch('/api/Clients/profile');
    } catch (e) {
      console.error('Failed to load profile', e);
    }
  }

  async function fetchMasters() {
    loading = true;
    error = '';
    try {
      masters = await apiFetch('/api/Clients/Masters');
    } catch (err) {
      error = err.message || 'Ошибка загрузки мастеров';
    } finally {
      loading = false;
    }
  }

  async function selectMaster(master) {
    selectedMaster = master;
    step = 2;
    showReviews = false;
    loading = true;
    error = '';
    
    // Fetch rating in background
    apiFetch(`/api/Clients/get-master-rating?barberId=${master.id}`)
      .then(res => { masterRating = res.rating; })
      .catch(() => { masterRating = null; });

    try {
      services = await apiFetch(`/api/Clients/Services/${master.id}`);
    } catch (err) {
      error = err.message || 'Ошибка загрузки услуг';
    } finally {
      loading = false;
    }
  }

  async function loadReviews(masterId) {
    try {
      masterReviews = await apiFetch(`/api/Clients/get-reviews?barberId=${masterId}`);
    } catch (e) {
      masterReviews = [];
    }
  }

  function openReviewModal(appt) {
    reviewingAppt = appt;
    reviewRating = 5;
    reviewComment = '';
    error = '';
  }

  function closeReviewModal() {
    reviewingAppt = null;
    error = '';
  }

  async function submitReview() {
    if (!reviewingAppt) return;
    submittingReview = true;
    error = '';
    try {
      await apiFetch('/api/Clients/submit-review', {
        method: 'POST',
        body: {
          barberId: reviewingAppt.masterId,
          appointmentId: reviewingAppt.id,
          rating: reviewRating,
          comment: reviewComment ? reviewComment.trim() : ''
        }
      });
      reviewingAppt.hasReview = true;
      const idx = myAppointments.findIndex(a => a.id === reviewingAppt.id);
      if (idx !== -1) {
        myAppointments[idx].hasReview = true;
      }
      myAppointments = [...myAppointments];
      closeReviewModal();
    } catch(err) {
      error = err.message || 'Ошибка отправки отзыва';
    } finally {
      submittingReview = false;
    }
  }

  function selectService(service) {
    selectedService = service;
    step = 3;
    // Set default date to today
    const today = new Date();
    selectedDate = today.toISOString().split('T')[0];
    fetchAvailableSlots();
  }

  async function fetchAvailableSlots() {
    if (!selectedDate) return;
    loading = true;
    error = '';
    availableSlots = [];
    selectedTime = '';
    try {
      const query = new URLSearchParams({
        masterId: selectedMaster.id,
        serviceId: selectedService.id,
        date: selectedDate
      }).toString();
      availableSlots = await apiFetch(`/api/Clients/AvailableTimeSlots?${query}`);
    } catch (err) {
      error = err.message || 'Ошибка загрузки расписания';
    } finally {
      loading = false;
    }
  }

  async function confirmBooking() {
    error = '';
    
    // Check if client profile has phone
    if (!clientProfile) {
      loading = true;
      try {
        clientProfile = await apiFetch('/api/Clients/profile');
      } catch (e) {
        console.error(e);
      } finally {
        loading = false;
      }
    }

    if (!clientProfile?.phone || clientProfile.phone.trim() === '') {
      showPhoneModal = true;
      phoneInput = '';
      phoneError = '';
      return;
    }

    await executeBooking();
  }

  async function savePhoneAndBook() {
    const val = validatePhone(phoneInput);
    if (!val.valid) {
      phoneError = val.error || 'Некорректный номер';
      return;
    }

    savingPhone = true;
    phoneError = '';
    try {
      await apiFetch('/api/Clients/update-phone', {
        method: 'POST',
        body: { phone: val.cleaned }
      });
      if (!clientProfile) clientProfile = {};
      clientProfile.phone = val.cleaned;
      showPhoneModal = false;
      
      if (step === 3 && selectedTime && selectedMaster && selectedService) {
        await executeBooking();
      }
    } catch (err: any) {
      phoneError = err.message || 'Не удалось сохранить номер';
    } finally {
      savingPhone = false;
    }
  }

  async function executeBooking() {
    loading = true;
    error = '';
    try {
      // Combine date and time to ISO string
      const dateTimeString = `${selectedDate}T${selectedTime}:00Z`;
      
      await apiFetch('/api/Clients/Zapisatsa', {
        method: 'POST',
        body: {
          masterId: selectedMaster.id,
          serviceId: selectedService.id,
          appointmentDate: dateTimeString
        }
      });
      step = 4;
    } catch (err) {
      if (err.message && (err.message.includes('телефон') || err.message.includes('phone') || err.message.includes('Phone'))) {
        showPhoneModal = true;
        phoneInput = '';
      } else {
        error = err.message || 'Ошибка при оформлении записи';
      }
    } finally {
      loading = false;
    }
  }

  function calculateSlotEndTime(timeStr: string, durationMinutes: number): string {
    if (!timeStr || !durationMinutes) return timeStr;
    const parts = timeStr.split(':');
    if (parts.length < 2) return timeStr;
    const totalMins = parseInt(parts[0], 10) * 60 + parseInt(parts[1], 10) + durationMinutes;
    const endH = Math.floor(totalMins / 60) % 24;
    const endM = totalMins % 60;
    return `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`;
  }

  function formatMasterHandle(handle: string): string {
    if (!handle) return '';
    return handle.startsWith('@') ? handle : `@${handle}`;
  }

  function openBarberChat(username: string, event?: Event) {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    if (!username) {
      showAlert('У мастера не настроен Telegram @username');
      return;
    }
    const clean = String(username).trim().replace(/^@/, '');
    if (!clean || /^\d+$/.test(clean)) {
      showAlert('У мастера не настроен Telegram @username');
      return;
    }

    const tmeUrl = `https://t.me/${clean}`;
    const tg = (window as any).Telegram?.WebApp;
    if (tg && typeof tg.openTelegramLink === 'function') {
      tg.openTelegramLink(tmeUrl);
    } else {
      window.open(tmeUrl, '_blank', 'noopener,noreferrer');
    }
  }

  function goBack() {
    if (step > 1 && step < 4) {
      step--;
      error = '';
    }
  }

  function reset() {
    step = 1;
    selectedMaster = null;
    selectedService = null;
    selectedDate = '';
    selectedTime = '';
    error = '';
  }

  async function fetchMyAppointments() {
    loadingAppointments = true;
    error = '';
    try {
      myAppointments = await apiFetch('/api/Clients/get-my-appointments');
    } catch (err) {
      error = err.message || 'Ошибка загрузки записей';
    } finally {
      loadingAppointments = false;
    }
  }

  async function cancelAppointment(id) {
    if (!confirm('Вы уверены, что хотите отменить запись?')) return;
    
    loadingAppointments = true;
    error = '';
    try {
      await apiFetch(`/api/Clients/cancel-appointment?appointmentId=${id}`, {
        method: 'DELETE'
      });
      await fetchMyAppointments();
    } catch (err) {
      error = err.message || 'Ошибка отмены записи';
      loadingAppointments = false;
    }
  }

  function formatStatus(status) {
    switch(status) {
      case 0: return 'Запланирована';
      case 1: return 'Завершена';
      case 2: return 'Отменена';
      case 3: return 'Не пришел';
      default: return 'Неизвестно';
    }
  }

  function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleString('ru-RU', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  function switchView(view: 'booking' | 'appointments' | 'profile') {
    currentView = view;
    error = '';
    if (view === 'appointments') {
      fetchMyAppointments();
    } else if (view === 'profile') {
      fetchClientProfile();
      fetchMyAppointments();
    }
  }
</script>

<div class="booking-container">
  <div class="top-nav-bar">
    <div class="tabs">
      <button class="tab-btn {currentView === 'booking' ? 'active' : ''}" on:click={() => switchView('booking')}>Запись</button>
      <button class="tab-btn {currentView === 'appointments' ? 'active' : ''}" on:click={() => switchView('appointments')}>Мои записи</button>
      <button class="tab-btn {currentView === 'profile' ? 'active' : ''}" on:click={() => switchView('profile')}>Профиль</button>
    </div>

    <button class="theme-tma-toggle" on:click={toggleTmaTheme} title="Сменить тему">
      {#if $theme === 'dark'}
        <Icon name="sun" size={17} color="var(--pastel-amber)" />
      {:else}
        <Icon name="moon" size={17} color="var(--pastel-lavender)" />
      {/if}
    </button>
  </div>

  {#if currentView === 'booking'}
  {#if step < 4}
    <div class="header">
      {#if step > 1}
      <button class="back-btn" on:click={goBack}>
        <Icon name="chevron-left" size={16} />
        <span>Назад</span>
      </button>
    {/if}
      <h2>
        {#if step === 1}Выбор мастера
        {:else if step === 2}Выбор услуги
        {:else if step === 3}Дата и время
        {/if}
      </h2>
    </div>
  {/if}

  {#if error}
    <div class="error">{error}</div>
  {/if}

  {#if loading}
    <div class="loader-container">
      <div class="spinner"></div>
    </div>
  {:else}
    {#if step === 1}
      <div class="list">
        {#each masters as master}
          <!-- svelte-ignore a11y-click-events-have-key-events -->
          <!-- svelte-ignore a11y-no-static-element-interactions -->
          <div class="card master-select-card" on:click={() => selectMaster(master)}>
            <div class="info">
              <div class="master-title-line">
                <h3>{master.name}</h3>
                {#if master.username}
                  <!-- svelte-ignore a11y-click-events-have-key-events -->
                  <!-- svelte-ignore a11y-no-static-element-interactions -->
                  <button 
                    type="button"
                    class="contact-master-btn" 
                    on:click|stopPropagation={(e) => openBarberChat(master.username, e)}
                    title="Написать мастеру в Telegram"
                  >
                    <Icon name="comment" size={13} color="var(--pastel-lavender)" />
                    <span>Написать мастеру</span>
                  </button>
                {/if}
              </div>
              {#if master.description}
                <p class="master-desc-text">{master.description}</p>
              {/if}
            </div>
            <div class="arrow">
              <Icon name="chevron-right" size={18} />
            </div>
          </div>
        {:else}
          <p class="empty">Нет доступных мастеров</p>
        {/each}
      </div>
    {/if}

    {#if step === 2}
      <div class="master-header">
        <div class="info">
          <div class="master-title-line">
            <h3>Мастер: {selectedMaster.name}</h3>
            {#if selectedMaster.username}
              <button 
                type="button"
                class="contact-master-btn inline" 
                on:click={(e) => openBarberChat(selectedMaster.username, e)}
                title="Написать мастеру в Telegram"
              >
                <Icon name="comment" size={13} color="var(--pastel-lavender)" />
                <span>Написать мастеру</span>
              </button>
            {/if}
          </div>
          {#if masterRating !== null}
            <p>
              Рейтинг: 
              <Icon name="star" size={14} color="var(--pastel-amber)" />
              <span>{masterRating > 0 ? masterRating.toFixed(1) : 'Нет оценок'}</span>
            </p>
          {/if}
        </div>
        <button class="secondary-btn" on:click={() => { showReviews = !showReviews; if (showReviews) loadReviews(selectedMaster.id); }}>
          {showReviews ? 'Скрыть отзывы' : 'Отзывы'}
        </button>
      </div>

      {#if showReviews}
        <div class="reviews-list">
          {#each masterReviews as r}
            <div class="review-card">
              <div class="review-head">
                <strong>{r.clientName || 'Клиент'}</strong>
                <span>
                  <Icon name="star" size={14} color="var(--pastel-amber)" />
                  <span>{r.rating}</span>
                </span>
              </div>
              <p>{r.comment}</p>
            </div>
          {:else}
            <p class="empty">Отзывов пока нет.</p>
          {/each}
        </div>
      {:else}
        <div class="list">
          {#each services as service}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="card" on:click={() => selectService(service)}>
              <div class="info">
                <h3>{service.name}</h3>
                <p>{service.duration} мин • {service.price} ₴</p>
              </div>
              <div class="arrow">
                <Icon name="chevron-right" size={18} />
              </div>
            </div>
          {:else}
            <p class="empty">У этого мастера нет доступных услуг</p>
          {/each}
        </div>
      {/if}
    {/if}

    {#if step === 3}
      <div class="datetime-picker">
        <div class="booking-service-preview">
          <div class="service-preview-left">
            <span class="preview-service-name">{selectedService.name}</span>
            <span class="preview-service-details">
              <span>{selectedService.duration} мин</span>
              <span class="dot">•</span>
              <span class="preview-service-price">{selectedService.price} ₴</span>
            </span>
          </div>
        </div>

        <label for="date">Выберите дату:</label>
        <input type="date" id="date" bind:value={selectedDate} on:change={fetchAvailableSlots} />
        
        <label>Доступное время (длительность: {selectedService.duration} мин):</label>
        <div class="slots">
          {#each availableSlots as slot}
            <button 
              class="slot-btn {selectedTime === slot ? 'selected' : ''}" 
              on:click={() => selectedTime = slot}
            >
              <span class="slot-time-main">{slot}</span>
              <span class="slot-time-sub">{slot}–{calculateSlotEndTime(slot, selectedService.duration)}</span>
            </button>
          {:else}
            <p class="empty-slots">Нет свободного времени на эту дату для услуги длительностью {selectedService.duration} мин</p>
          {/each}
        </div>

        {#if selectedTime}
          <div class="selected-slot-banner">
            <Icon name="check" size={16} color="var(--pastel-sage)" />
            <span>Время записи: <strong>{selectedTime} — {calculateSlotEndTime(selectedTime, selectedService.duration)}</strong> ({selectedService.duration} мин)</span>
          </div>
        {/if}

        <div class="client-phone-info-banner">
          <div class="phone-banner-content">
            <Icon name="phone" size={15} color="var(--pastel-lavender)" />
            <div class="phone-banner-text">
              <span class="phone-banner-label">Телефон для записи:</span>
              <span class="phone-banner-value">{clientProfile?.phone || 'Не указан'}</span>
            </div>
          </div>
          <button 
            type="button" 
            class="phone-banner-edit-btn" 
            on:click={() => { phoneInput = clientProfile?.phone || ''; phoneError = ''; showPhoneModal = true; }}
          >
            <Icon name="edit" size={12} />
            <span>{clientProfile?.phone ? 'Изменить' : 'Указать'}</span>
          </button>
        </div>

        <button 
          class="primary-btn" 
          disabled={!selectedTime} 
          on:click={confirmBooking}
        >
          Записаться на {selectedTime ? `${selectedTime} (${selectedService.duration} мин)` : 'выбранное время'}
        </button>
      </div>
    {/if}

    {#if step === 4}
      <div class="success-screen">
        <div class="icon">
          <Icon name="check-circle" size={54} color="var(--pastel-sage)" />
        </div>
        <h2>Вы успешно записаны!</h2>
        <div class="success-details-card">
          <div class="success-master-line">
            <span class="success-label">Мастер:</span>
            <span class="success-val">{selectedMaster.name}</span>
            {#if selectedMaster.username}
              <button 
                type="button"
                class="contact-master-btn sm" 
                on:click={(e) => openBarberChat(selectedMaster.username, e)}
                title="Написать мастеру в Telegram"
              >
                <Icon name="comment" size={12} color="var(--pastel-lavender)" />
                <span>Написать мастеру</span>
              </button>
            {/if}
          </div>
          <div class="success-detail-row">
            <span class="success-label">Услуга:</span>
            <span class="success-val">{selectedService.name}</span>
          </div>
          <div class="success-detail-row">
            <span class="success-label">Дата и время:</span>
            <span class="success-val highlight">{selectedDate} в {selectedTime}</span>
          </div>
        </div>
        <button class="primary-btn mt-3" on:click={() => { reset(); switchView('appointments'); }}>Мои записи</button>
      </div>
    {/if}
  {/if}
  {/if}

  {#if reviewingAppt}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-overlay" on:click={closeReviewModal}>
      <!-- svelte-ignore a11y-click-events-have-key-events -->
      <!-- svelte-ignore a11y-no-static-element-interactions -->
      <div class="modal-content review-modal-card" on:click|stopPropagation>
        <div class="modal-header-row">
          <div class="modal-header-text">
            <h3>Оставить отзыв</h3>
            <p class="modal-sub">Поделитесь впечатлениями о визите</p>
          </div>
          <button class="modal-close-btn" on:click={closeReviewModal} type="button" aria-label="Закрыть">
            <Icon name="x" size={18} />
          </button>
        </div>

        <div class="review-target-box">
          <div class="review-target-row">
            <span class="target-label">Мастер:</span>
            <span class="target-value">{reviewingAppt.masterName || 'Барбер'}</span>
          </div>
          <div class="review-target-row">
            <span class="target-label">Услуга:</span>
            <span class="target-value highlight">{reviewingAppt.serviceName || 'Услуга'}</span>
          </div>
        </div>
        
        <div class="stars-wrap">
          <span class="stars-label">Ваша оценка:</span>
          <div class="stars">
            {#each [1,2,3,4,5] as star}
              <button 
                type="button"
                class="star-btn" 
                class:active={reviewRating >= star}
                on:click={() => reviewRating = star}
                aria-label="{star} звезд"
              >
                <Icon 
                  name={reviewRating >= star ? 'star' : 'star-outline'} 
                  size={36} 
                  color="var(--pastel-amber)" 
                />
              </button>
            {/each}
          </div>
          <div class="rating-text-hint">
            {reviewRating === 5 ? '⭐⭐⭐⭐⭐ Отлично' : reviewRating === 4 ? '⭐⭐⭐⭐ Хорошо' : reviewRating === 3 ? '⭐⭐⭐ Нормально' : '⭐⭐ Есть замечания'}
          </div>
        </div>
        
        <div class="form-group">
          <label for="client-review-comment">Комментарий (необязательно)</label>
          <textarea id="client-review-comment" bind:value={reviewComment} placeholder="Что вам понравилось больше всего?"></textarea>
        </div>
        
        {#if error}<p class="error-msg-sm">{error}</p>{/if}
        
        <div class="modal-actions">
          <button class="primary-btn flex-1" on:click={submitReview} disabled={submittingReview}>
            {submittingReview ? 'Отправка...' : 'Отправить отзыв'}
          </button>
          <button class="secondary-btn" on:click={closeReviewModal} disabled={submittingReview}>Отмена</button>
        </div>
      </div>
    </div>
  {/if}

  {#if showPhoneModal}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-overlay" on:click={() => showPhoneModal = false}>
      <div class="modal-content" on:click|stopPropagation>
        <div class="modal-header-icon">
          <Icon name="phone" size={26} color="var(--pastel-rose)" />
        </div>
        <h3 style="text-align:center;">{clientProfile?.phone ? 'Изменить номер телефона' : 'Укажите номер телефона'}</h3>
        <p style="text-align:center;">Контактный номер необходим мастеру для подтверждения и связи по вашей записи.</p>

        <div class="phone-input-wrap">
          <input
            type="tel"
            class="phone-input"
            bind:value={phoneInput}
            placeholder="+380... или +7..."
            on:keydown={(e) => e.key === 'Enter' && savePhoneAndBook()}
          />
          <div class="phone-format-hint">
            Формат: <code>+380991234567</code> или <code>+79991234567</code> (+, 1-3 цифры кода, 9 цифр номера)
          </div>
        </div>

        {#if phoneError}
          <div class="error-msg-sm">{phoneError}</div>
        {/if}

        <div class="modal-actions">
          <button class="secondary-btn" on:click={() => showPhoneModal = false} disabled={savingPhone}>
            Отмена
          </button>
          <button class="primary-btn" on:click={savePhoneAndBook} disabled={savingPhone || !phoneInput.trim()}>
            {savingPhone ? 'Сохранение...' : (step === 3 && selectedTime ? 'Сохранить и записаться' : 'Сохранить')}
          </button>
        </div>
      </div>
    </div>
  {/if}

  {#if currentView === 'appointments'}
    {#if loadingAppointments}
      <div class="loader-container">
        <div class="spinner"></div>
      </div>
    {:else}
      <div class="list">
        {#each myAppointments as appt}
          <div class="card appt-card">
            <div class="appt-info">
              <h3>{appt.serviceName}</h3>
              <div class="appt-master-row">
                <span class="appt-master-name">Мастер: <strong>{appt.masterName}</strong></span>
                {#if appt.masterUsername}
                  <button 
                    type="button"
                    class="contact-master-btn sm" 
                    on:click={(e) => openBarberChat(appt.masterUsername, e)}
                    title="Написать мастеру в Telegram"
                  >
                    <Icon name="comment" size={12} color="var(--pastel-lavender)" />
                    <span>Написать мастеру</span>
                  </button>
                {/if}
              </div>
              <p>Дата: {formatDate(appt.appointmentDate)}</p>
              <p>Статус: <span class="status-{appt.status}">{formatStatus(appt.status)}</span></p>
              <p>Цена: {appt.price ? appt.price + ' ₴' : 'Не указана'}</p>
              {#if appt.resultNote}
                <div class="master-note">
                  <strong>Заметка мастера:</strong> {appt.resultNote}
                </div>
              {/if}
            </div>
            {#if appt.status === 0}
              <button class="cancel-btn" on:click={() => cancelAppointment(appt.id)}>Отменить</button>
            {/if}
            {#if appt.status === 1 && !appt.hasReview}
              <button class="primary-btn mt-2" on:click={() => openReviewModal(appt)}>Оценить</button>
            {/if}
          </div>
        {:else}
          <p class="empty">У вас пока нет записей</p>
        {/each}
      </div>
    {/if}
  {/if}

  {#if currentView === 'profile'}
    <div class="client-profile-container">
      <div class="profile-card">
        <div class="profile-avatar-wrap">
          <div class="profile-avatar">
            <Icon name="user" size={32} color="var(--pastel-lavender)" />
          </div>
          <div class="profile-meta">
            <h3>{clientProfile?.name || 'Клиент'}</h3>
            <span class="profile-tg-id">ID: {clientProfile?.telegramId || '—'}</span>
          </div>
        </div>

        {#if profilePhoneSuccess}
          <div class="success-alert">
            <Icon name="check" size={16} color="var(--pastel-sage)" />
            <span>{profilePhoneSuccess}</span>
          </div>
        {/if}

        <div class="profile-field-group">
          <div class="field-label-row">
            <span class="field-title">
              <Icon name="phone" size={15} color="var(--pastel-rose)" />
              Контактный номер телефона
            </span>
            {#if !editingPhone}
              <button class="edit-action-btn" type="button" on:click={startEditPhone}>
                <Icon name="edit" size={13} />
                <span>{clientProfile?.phone ? 'Изменить' : 'Указать'}</span>
              </button>
            {/if}
          </div>

          {#if editingPhone}
            <div class="edit-phone-box">
              <input
                type="tel"
                class="phone-input"
                bind:value={profilePhoneInput}
                placeholder="+380991234567 или +79991234567"
                on:keydown={(e) => e.key === 'Enter' && saveProfilePhone()}
              />
              <div class="phone-format-hint">
                Формат: <code>+380991234567</code> или <code>+79991234567</code> (+, 1-3 цифры кода, 9 цифр номера)
              </div>

              {#if profilePhoneError}
                <div class="error-msg-sm">{profilePhoneError}</div>
              {/if}

              <div class="edit-phone-actions">
                <button class="secondary-btn sm" type="button" on:click={cancelEditPhone} disabled={savingProfilePhone}>
                  Отмена
                </button>
                <button class="primary-btn sm" type="button" on:click={saveProfilePhone} disabled={savingProfilePhone || !profilePhoneInput.trim()}>
                  {savingProfilePhone ? 'Сохранение...' : 'Сохранить'}
                </button>
              </div>
            </div>
          {:else}
            <div class="phone-display-card">
              {#if clientProfile?.phone}
                <span class="phone-value">{clientProfile.phone}</span>
                <span class="phone-verified-tag">
                  <Icon name="check" size={12} color="var(--pastel-sage)" /> Привязан
                </span>
              {:else}
                <span class="phone-empty">Номер не указан</span>
                <button class="btn-add-phone-sm" type="button" on:click={startEditPhone}>
                  + Добавить номер
                </button>
              {/if}
            </div>
          {/if}
        </div>

        <div class="profile-stats-card">
          <div class="profile-stat-item">
            <span class="stat-num">{myAppointments.length || 0}</span>
            <span class="stat-label">Всего записей</span>
          </div>
          <div class="profile-stat-divider"></div>
          <button class="profile-stat-link" type="button" on:click={() => switchView('appointments')}>
            <span>История визитов</span>
            <Icon name="chevron-right" size={16} />
          </button>
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .booking-container {
    padding: 16px 16px 80px;
    box-sizing: border-box;
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    font-family: var(--font-family);
    animation: pageFadeIn 0.3s ease-out;
  }

  .top-nav-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 20px;
  }

  .tabs {
    display: flex;
    align-items: center;
    background: var(--bg-surface-elevated);
    padding: 4px;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
    flex: 1;
    margin: 0;
  }

  .tab-btn {
    flex: 1;
    padding: 9px 14px;
    background: transparent;
    border: none;
    border-radius: var(--radius-pill);
    color: var(--text-secondary);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .tab-btn:hover {
    color: var(--text-primary);
  }

  .tab-btn.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: #ffffff !important;
    font-weight: 700;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
  }

  .theme-tma-toggle {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    flex-shrink: 0;
    padding: 0;
  }

  .theme-tma-toggle:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
  }

  .theme-tma-toggle:active {
    transform: scale(0.92);
  }

  @keyframes pageFadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  .header {
    display: flex;
    align-items: center;
    margin-bottom: 20px;
    position: relative;
  }

  .back-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--pastel-rose);
    font-size: 14px;
    font-weight: 600;
    padding: 8px 14px;
    border-radius: var(--radius-pill);
    margin-right: 14px;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    display: flex;
    align-items: center;
    gap: 4px;
  }

  .back-btn:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
  }

  .back-btn:active {
    transform: scale(0.94);
  }

  h2 {
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .error {
    background-color: var(--pastel-coral-dim);
    border: 1px solid rgba(232, 130, 130, 0.25);
    color: var(--pastel-coral);
    padding: 12px 16px;
    border-radius: var(--radius-md);
    margin-bottom: 16px;
    font-size: 14px;
  }

  .list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    padding: 18px 20px;
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    transition: all 0.25s var(--ease-spring);
  }

  .card:hover {
    border-color: var(--border-glass);
    transform: translateY(-2px);
    box-shadow: 0 10px 30px rgba(0,0,0,0.45);
  }

  .card:active {
    transform: scale(0.98);
  }

  .card h3 {
    margin: 0 0 6px 0;
    font-size: 16px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .card p {
    margin: 0;
    font-size: 13px;
    color: var(--text-secondary);
  }

  .arrow {
    color: var(--pastel-rose);
    font-size: 20px;
    font-weight: 600;
  }

  .empty {
    text-align: center;
    color: var(--text-secondary);
    margin-top: 40px;
    font-size: 15px;
  }

  .datetime-picker {
    display: flex;
    flex-direction: column;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 20px;
    box-shadow: var(--shadow-glass);
  }

  label {
    margin-bottom: 8px;
    font-size: 13px;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  input[type="date"] {
    padding: 14px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    margin-bottom: 22px;
    font-size: 15px;
    font-family: var(--font-family);
    background-color: var(--bg-surface-elevated);
    color: var(--text-primary);
    outline: none;
    transition: border-color 0.2s;
  }

  input[type="date"]:focus {
    border-color: var(--border-active);
  }

  .master-title-line {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }

  .master-username-badge {
    font-size: 12px;
    font-weight: 600;
    color: var(--pastel-lavender);
    background: var(--pastel-lavender-dim);
    border: 1px solid rgba(186, 168, 222, 0.25);
    padding: 2px 8px;
    border-radius: var(--radius-pill);
    letter-spacing: 0.02em;
  }

  .master-username-badge.inline {
    font-size: 13px;
    padding: 3px 10px;
  }

  .master-desc-text {
    margin-top: 4px;
    font-size: 13px;
    color: var(--text-secondary);
  }

  .booking-service-preview {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px 14px;
    margin-bottom: 18px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .service-preview-left {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }

  .preview-service-name {
    font-weight: 700;
    font-size: 15px;
    color: var(--text-primary);
  }

  .preview-service-details {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    color: var(--text-secondary);
  }

  .preview-service-price {
    font-weight: 600;
    color: var(--pastel-rose);
  }

  .dot {
    color: var(--text-muted);
  }

  .slots {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(92px, 1fr));
    gap: 10px;
    margin-bottom: 20px;
  }

  .slot-btn {
    padding: 10px 6px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    background-color: var(--bg-surface-elevated);
    color: var(--text-primary);
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 2px;
  }

  .slot-time-main {
    font-size: 15px;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }

  .slot-time-sub {
    font-size: 11px;
    color: var(--text-muted);
    font-variant-numeric: tabular-nums;
  }

  .slot-btn:hover {
    border-color: var(--border-glass);
    color: var(--pastel-rose);
  }

  .slot-btn:hover .slot-time-sub {
    color: var(--pastel-rose);
  }

  .slot-btn.selected {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border-color: transparent;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    transform: scale(1.04);
  }

  .slot-btn.selected .slot-time-sub {
    color: rgba(255, 255, 255, 0.85);
  }

  .selected-slot-banner {
    display: flex;
    align-items: center;
    gap: 8px;
    background: var(--pastel-sage-dim);
    border: 1px solid rgba(152, 193, 169, 0.3);
    border-radius: var(--radius-md);
    padding: 10px 14px;
    font-size: 13px;
    color: var(--text-primary);
    margin-bottom: 18px;
    animation: fadeIn 0.2s ease;
  }

  .empty-slots {
    grid-column: 1 / -1;
    color: var(--text-muted);
    text-align: center;
    padding: 24px 0;
    font-size: 14px;
  }

  .primary-btn {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 14px 20px;
    border-radius: var(--radius-pill);
    font-size: 15px;
    font-weight: 600;
    width: 100%;
    cursor: pointer;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }

  .primary-btn:active:not(:disabled) {
    transform: scale(0.97);
  }

  .primary-btn:disabled {
    opacity: 0.45;
    cursor: not-allowed;
    box-shadow: none;
  }

  .success-screen {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
    padding: 40px 24px;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-glass);
    margin-top: 20px;
    animation: fadeIn 0.4s var(--ease-spring);
  }

  .success-screen .icon {
    font-size: 54px;
    margin-bottom: 16px;
    filter: drop-shadow(0 0 16px var(--pastel-sage-glow));
  }

  .success-screen h2 {
    color: var(--pastel-sage);
    margin-bottom: 12px;
  }

  .success-screen p {
    margin: 6px 0;
    color: var(--text-secondary);
    font-size: 15px;
  }

  .loader-container {
    display: flex;
    justify-content: center;
    padding: 50px;
  }

  .spinner {
    width: 36px;
    height: 36px;
    border: 3px solid rgba(223, 158, 142, 0.15);
    border-top: 3px solid var(--pastel-rose);
    border-radius: 50%;
    animation: spinSmooth 0.85s linear infinite;
    box-shadow: 0 0 16px var(--pastel-rose-glow);
  }


  /* Appointment Card */
  .appt-card {
    flex-direction: column;
    align-items: flex-start;
    gap: 14px;
    border-left: 4px solid var(--pastel-amber);
  }

  .appt-info {
    width: 100%;
  }

  .appt-info h3 {
    margin-bottom: 8px;
    font-size: 16px;
    font-weight: 700;
  }

  .appt-info p {
    margin-bottom: 4px;
    font-size: 13px;
    color: var(--text-secondary);
  }

  .status-0 { color: var(--pastel-amber); font-weight: 600; }
  .status-1 { color: var(--pastel-sage); font-weight: 600; }
  .status-2 { color: var(--pastel-coral); font-weight: 600; }
  .status-3 { color: var(--text-muted); font-weight: 600; }

  .master-note {
    margin-top: 10px;
    padding: 10px 14px;
    background-color: var(--bg-surface-elevated);
    border-left: 3px solid var(--pastel-rose);
    border-radius: var(--radius-md);
    font-size: 13px;
    color: var(--text-primary);
    line-height: 1.4;
  }

  .cancel-btn {
    background-color: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.2);
    padding: 10px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    font-weight: 600;
    width: 100%;
    cursor: pointer;
    margin-top: 4px;
    transition: all 0.2s;
  }

  .cancel-btn:active {
    transform: scale(0.97);
  }

  .master-header {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    padding: 16px 18px;
    border-radius: var(--radius-lg);
    margin-bottom: 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: var(--shadow-glass);
  }

  .secondary-btn {
    background: var(--bg-surface-elevated);
    color: var(--pastel-rose);
    border: 1px solid var(--border-subtle);
    padding: 10px 16px;
    border-radius: var(--radius-pill);
    cursor: pointer;
    font-size: 13px;
    font-weight: 600;
    transition: all 0.2s;
  }
  .secondary-btn:hover { border-color: var(--border-glass); }
  .secondary-btn:active { transform: scale(0.95); }

  .mt-2 { margin-top: 8px; }
  
  .reviews-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .review-card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    padding: 14px 16px;
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-glass);
  }

  .review-head {
    display: flex;
    justify-content: space-between;
    margin-bottom: 6px;
    font-size: 14px;
    font-weight: 600;
  }

  .review-card p {
    margin: 0;
    font-size: 13px;
    color: var(--text-secondary);
  }
  
  /* Modal Overlay */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(10, 12, 16, 0.85);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    padding: 16px;
    box-sizing: border-box;
    animation: modalBgFade 0.2s ease-out;
  }

  @keyframes modalBgFade {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  .modal-content {
    background: var(--bg-surface-solid);
    border: 1px solid var(--border-glass);
    padding: 24px;
    border-radius: var(--radius-lg);
    width: 92%;
    max-width: 440px;
    max-height: 90vh;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 16px;
    color: var(--text-primary);
    box-shadow: 0 24px 64px rgba(0, 0, 0, 0.8), 0 0 24px rgba(223, 158, 142, 0.15);
    animation: modalPop 0.25s var(--ease-spring);
    box-sizing: border-box;
    margin: auto;
  }

  @keyframes modalPop {
    from { opacity: 0; transform: scale(0.96); }
    to { opacity: 1; transform: scale(1); }
  }

  .modal-header-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 12px;
    border-bottom: 1px solid var(--border-subtle);
    padding-bottom: 12px;
  }

  .modal-header-text h3 {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .modal-sub {
    margin: 3px 0 0;
    font-size: 13px;
    color: var(--text-secondary);
  }

  .modal-close-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-muted);
    width: 32px;
    height: 32px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s;
    flex-shrink: 0;
  }

  .modal-close-btn:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
    background: var(--bg-surface-hover);
  }

  .review-target-box {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .review-target-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 14px;
  }

  .target-label {
    color: var(--text-secondary);
    font-size: 13px;
  }

  .target-value {
    font-weight: 600;
    color: var(--text-primary);
  }

  .target-value.highlight {
    color: var(--pastel-rose);
  }

  .stars-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 6px 0;
  }

  .stars-label {
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-secondary);
  }

  .stars {
    display: flex;
    justify-content: center;
    gap: 10px;
    margin: 4px 0;
  }

  .star-btn {
    background: transparent;
    border: none;
    padding: 4px;
    cursor: pointer;
    opacity: 0.3;
    transition: all 0.2s var(--ease-spring);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .star-btn:hover,
  .star-btn.active {
    opacity: 1;
    transform: scale(1.15);
    filter: drop-shadow(0 0 8px var(--pastel-amber-glow));
  }

  .rating-text-hint {
    font-size: 13px;
    font-weight: 600;
    color: var(--pastel-amber);
    min-height: 18px;
  }

  textarea {
    width: 100%;
    height: 90px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    padding: 12px;
    background: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-family: var(--font-family);
    box-sizing: border-box;
    resize: none;
    font-size: 14px;
    transition: border-color 0.2s;
  }
  textarea:focus {
    outline: none;
    border-color: var(--border-active);
  }

  .modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 10px;
  }

  .modal-header-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 50px;
    height: 50px;
    background: var(--pastel-rose-dim);
    border: 1px solid rgba(223, 158, 142, 0.25);
    border-radius: 50%;
    margin: 0 auto 4px;
    box-shadow: 0 0 16px var(--pastel-rose-glow);
  }

  .phone-input-wrap {
    width: 100%;
  }

  .phone-input {
    width: 100%;
    padding: 13px 16px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-family: var(--font-family);
    font-size: 15px;
    box-sizing: border-box;
    outline: none;
    transition: border-color 0.2s, box-shadow 0.2s;
  }

  .phone-input:focus {
    border-color: var(--border-active);
    box-shadow: 0 0 0 3px rgba(223, 158, 142, 0.15);
  }

  .error-msg-sm {
    color: var(--pastel-coral);
    font-size: 13px;
    text-align: center;
  }

  /* Contact Master Buttons & Info Styles */
  .contact-master-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 160, 219, 0.3);
    padding: 6px 12px;
    border-radius: var(--radius-pill);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    text-decoration: none;
    transition: all 0.2s var(--ease-spring);
    white-space: nowrap;
    user-select: none;
  }

  .contact-master-btn:hover {
    background: rgba(179, 160, 219, 0.22);
    border-color: var(--pastel-lavender);
    transform: translateY(-1px);
    box-shadow: 0 4px 12px var(--pastel-lavender-glow);
  }

  .contact-master-btn:active {
    transform: scale(0.96);
  }

  .contact-master-btn.inline {
    margin-left: 8px;
  }

  .contact-master-btn.sm {
    padding: 4px 9px;
    font-size: 11px;
    gap: 4px;
  }

  .appt-master-row {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
    margin: 4px 0;
  }

  .appt-master-name {
    font-size: 14px;
    color: var(--text-primary);
  }

  .success-details-card {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 14px 16px;
    margin: 16px 0;
    display: flex;
    flex-direction: column;
    gap: 8px;
    text-align: left;
  }

  .success-master-line {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    flex-wrap: wrap;
  }

  .success-detail-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 14px;
  }

  .success-label {
    color: var(--text-secondary);
    font-size: 13px;
  }

  .success-val {
    font-weight: 600;
    color: var(--text-primary);
  }

  .success-val.highlight {
    color: var(--pastel-rose);
  }

  .mt-1 { margin-top: 6px; }
  .mt-3 { margin-top: 14px; }

  .phone-format-hint {
    font-size: 11px;
    color: var(--text-secondary);
    margin-top: 6px;
    text-align: left;
  }

  .phone-format-hint code {
    background: var(--bg-surface);
    padding: 2px 5px;
    border-radius: 4px;
    color: var(--pastel-rose);
    font-size: 11px;
  }

  .client-phone-info-banner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px 14px;
    margin: 12px 0 16px;
  }

  .phone-banner-content {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .phone-banner-text {
    display: flex;
    flex-direction: column;
    gap: 2px;
    text-align: left;
  }

  .phone-banner-label {
    font-size: 11px;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .phone-banner-value {
    font-size: 14px;
    font-weight: 600;
    color: var(--text-primary);
  }

  .phone-banner-edit-btn {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 160, 219, 0.3);
    padding: 5px 10px;
    border-radius: var(--radius-pill);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    white-space: nowrap;
  }

  .phone-banner-edit-btn:hover {
    background: rgba(179, 160, 219, 0.25);
    transform: translateY(-1px);
  }

  /* Profile View Styles */
  .client-profile-container {
    animation: pageFadeIn 0.25s ease-out;
  }

  .profile-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 20px 16px;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .profile-avatar-wrap {
    display: flex;
    align-items: center;
    gap: 14px;
    padding-bottom: 14px;
    border-bottom: 1px solid var(--border-subtle);
  }

  .profile-avatar {
    width: 56px;
    height: 56px;
    border-radius: 50%;
    background: var(--pastel-lavender-dim);
    border: 1px solid rgba(179, 160, 219, 0.3);
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 16px var(--pastel-lavender-glow);
    flex-shrink: 0;
  }

  .profile-meta h3 {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .profile-tg-id {
    font-size: 12px;
    color: var(--text-secondary);
    margin-top: 3px;
    display: block;
  }

  .success-alert {
    display: flex;
    align-items: center;
    gap: 8px;
    background: rgba(184, 216, 190, 0.15);
    border: 1px solid var(--pastel-sage);
    border-radius: var(--radius-md);
    padding: 10px 14px;
    color: var(--pastel-sage);
    font-size: 13px;
    font-weight: 600;
  }

  .profile-field-group {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .field-label-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .field-title {
    display: flex;
    align-items: center;
    gap: 7px;
    font-size: 13px;
    font-weight: 600;
    color: var(--text-secondary);
  }

  .edit-action-btn {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 4px 8px;
    border-radius: var(--radius-sm);
    transition: background 0.2s;
  }

  .edit-action-btn:hover {
    background: var(--pastel-rose-dim);
  }

  .phone-display-card {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 14px 16px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .phone-value {
    font-size: 16px;
    font-weight: 700;
    color: var(--text-primary);
    letter-spacing: 0.5px;
  }

  .phone-verified-tag {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: 11px;
    font-weight: 600;
    color: var(--pastel-sage);
    background: rgba(184, 216, 190, 0.12);
    padding: 3px 8px;
    border-radius: var(--radius-pill);
  }

  .phone-empty {
    color: var(--text-secondary);
    font-size: 14px;
    font-style: italic;
  }

  .btn-add-phone-sm {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
    padding: 5px 12px;
    border-radius: var(--radius-pill);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
  }

  .btn-add-phone-sm:hover {
    background: rgba(223, 158, 142, 0.25);
  }

  .edit-phone-box {
    display: flex;
    flex-direction: column;
    gap: 8px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-active);
    border-radius: var(--radius-md);
    padding: 14px;
  }

  .edit-phone-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 6px;
  }

  .profile-stats-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px 16px;
    margin-top: 4px;
  }

  .profile-stat-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .stat-num {
    font-size: 18px;
    font-weight: 700;
    color: var(--pastel-rose);
  }

  .stat-label {
    font-size: 11px;
    color: var(--text-secondary);
  }

  .profile-stat-divider {
    width: 1px;
    height: 30px;
    background: var(--border-subtle);
  }

  .profile-stat-link {
    display: flex;
    align-items: center;
    gap: 6px;
    background: none;
    border: none;
    color: var(--text-primary);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 6px 10px;
    border-radius: var(--radius-sm);
    transition: all 0.2s;
  }

  .profile-stat-link:hover {
    color: var(--pastel-rose);
    background: var(--bg-surface);
  }
</style>

<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../lib/api';
  import Icon from '../lib/components/Icon.svelte';

  let currentView = 'booking'; // 'booking' | 'appointments'
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
  let clientProfile = null;
  let showPhoneModal = false;
  let phoneInput = '';
  let savingPhone = false;
  let phoneError = '';

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
  }

  function closeReviewModal() {
    reviewingAppt = null;
    error = '';
  }

  async function submitReview() {
    submittingReview = true;
    error = '';
    try {
      await apiFetch('/api/Clients/submit-review', {
        method: 'POST',
        body: {
          barberId: reviewingAppt.masterId,
          appointmentId: reviewingAppt.id,
          rating: reviewRating,
          comment: reviewComment
        }
      });
      reviewingAppt.hasReview = true;
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
    const trimmed = phoneInput.trim();
    if (!trimmed || trimmed.length < 6) {
      phoneError = 'Введите корректный номер телефона (не менее 6 символов)';
      return;
    }

    savingPhone = true;
    phoneError = '';
    try {
      await apiFetch('/api/Clients/update-phone', {
        method: 'POST',
        body: { phone: trimmed }
      });
      if (!clientProfile) clientProfile = {};
      clientProfile.phone = trimmed;
      showPhoneModal = false;
      await executeBooking();
    } catch (err) {
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

  function switchView(view) {
    currentView = view;
    error = '';
    if (view === 'appointments') {
      fetchMyAppointments();
    }
  }
</script>

<div class="booking-container">
  <div class="tabs">
    <button class="tab-btn {currentView === 'booking' ? 'active' : ''}" on:click={() => switchView('booking')}>Новая запись</button>
    <button class="tab-btn {currentView === 'appointments' ? 'active' : ''}" on:click={() => switchView('appointments')}>Мои записи</button>
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
          <div class="card" on:click={() => selectMaster(master)}>
            <div class="info">
              <h3>{master.name}</h3>
              {#if master.description}
                <p>{master.description}</p>
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
          <h3>Мастер: {selectedMaster.name}</h3>
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
        <label for="date">Выберите дату:</label>
        <input type="date" id="date" bind:value={selectedDate} on:change={fetchAvailableSlots} />
        
        <label>Доступное время:</label>
        <div class="slots">
          {#each availableSlots as slot}
            <button 
              class="slot-btn {selectedTime === slot ? 'selected' : ''}" 
              on:click={() => selectedTime = slot}
            >
              {slot}
            </button>
          {:else}
            <p class="empty-slots">Нет свободного времени на эту дату</p>
          {/each}
        </div>

        <button 
          class="primary-btn" 
          disabled={!selectedTime} 
          on:click={confirmBooking}
        >
          Записаться
        </button>
      </div>
    {/if}

    {#if step === 4}
      <div class="success-screen">
        <div class="icon">
          <Icon name="check-circle" size={54} color="var(--pastel-sage)" />
        </div>
        <h2>Вы успешно записаны!</h2>
        <p>Мастер: {selectedMaster.name}</p>
        <p>Услуга: {selectedService.name}</p>
        <p>Дата: {selectedDate} в {selectedTime}</p>
        <button class="primary-btn" on:click={() => { reset(); switchView('appointments'); }}>Мои записи</button>
      </div>
    {/if}
  {/if}
  {/if}

  {#if reviewingAppt}
    <div class="modal-overlay">
      <div class="modal-content">
        <h3>Оставить отзыв</h3>
        <p>Мастер: {reviewingAppt.masterName}</p>
        <p>Услуга: {reviewingAppt.serviceName}</p>
        
        <div class="stars">
          {#each [1,2,3,4,5] as star}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <span 
              class="star {reviewRating >= star ? 'active' : ''}" 
              on:click={() => reviewRating = star}
            >
              <Icon 
                name={reviewRating >= star ? 'star' : 'star-outline'} 
                size={32} 
                color="var(--pastel-amber)" 
              />
            </span>
          {/each}
        </div>
        
        <textarea bind:value={reviewComment} placeholder="Напишите ваш отзыв..."></textarea>
        
        {#if error}<p class="error">{error}</p>{/if}
        
        <div class="modal-actions">
          <button class="secondary-btn" on:click={closeReviewModal}>Отмена</button>
          <button class="primary-btn" on:click={submitReview} disabled={submittingReview}>
            {submittingReview ? 'Отправка...' : 'Отправить'}
          </button>
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
        <h3 style="text-align:center;">Укажите номер телефона</h3>
        <p style="text-align:center;">Для завершения записи мастеру необходим контактный номер телефона.</p>

        <div class="phone-input-wrap">
          <input
            type="tel"
            class="phone-input"
            bind:value={phoneInput}
            placeholder="+380... или +7..."
            on:keydown={(e) => e.key === 'Enter' && savePhoneAndBook()}
          />
        </div>

        {#if phoneError}
          <div class="error-msg-sm">{phoneError}</div>
        {/if}

        <div class="modal-actions">
          <button class="secondary-btn" on:click={() => showPhoneModal = false} disabled={savingPhone}>
            Отмена
          </button>
          <button class="primary-btn" on:click={savePhoneAndBook} disabled={savingPhone || !phoneInput.trim()}>
            {savingPhone ? 'Сохранение...' : 'Записаться'}
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
              <p>Мастер: {appt.masterName}</p>
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
</div>

<style>
  .booking-container {
    padding: 16px 16px 80px;
    box-sizing: border-box;
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    font-family: var(--font-family);
    animation: fadeIn 0.3s var(--ease-spring);
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

  .slots {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(78px, 1fr));
    gap: 10px;
    margin-bottom: 26px;
  }

  .slot-btn {
    padding: 12px 6px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    background-color: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
    font-variant-numeric: tabular-nums;
  }

  .slot-btn:hover {
    border-color: var(--border-glass);
    color: var(--pastel-rose);
  }

  .slot-btn.selected {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border-color: transparent;
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
    transform: scale(1.04);
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

  /* Tabs */
  .tabs {
    display: flex;
    margin-bottom: 24px;
    background: rgba(23, 26, 35, 0.7);
    border-radius: var(--radius-pill);
    padding: 4px;
    border: 1px solid var(--border-subtle);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
  }

  .tab-btn {
    flex: 1;
    padding: 10px 14px;
    border: none;
    background: transparent;
    border-radius: var(--radius-pill);
    color: var(--text-secondary);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.25s var(--ease-spring);
  }

  .tab-btn.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    box-shadow: 0 4px 14px var(--pastel-rose-glow);
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
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(12, 14, 18, 0.85);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 200;
    animation: fadeIn 0.2s ease;
  }

  .modal-content {
    background: var(--bg-surface-solid);
    border: 1px solid var(--border-glass);
    padding: 24px;
    border-radius: var(--radius-lg);
    width: 90%;
    max-width: 400px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    color: var(--text-primary);
    box-shadow: var(--shadow-lg);
  }

  .modal-content h3 { margin: 0; font-size: 18px; font-weight: 700; }
  .modal-content p { margin: 0; font-size: 14px; color: var(--text-secondary); }
  
  .stars {
    display: flex;
    justify-content: center;
    gap: 8px;
    font-size: 32px;
    margin: 8px 0;
  }

  .star {
    cursor: pointer;
    opacity: 0.25;
    transition: opacity 0.2s, transform 0.15s;
  }
  .star.active {
    opacity: 1;
    transform: scale(1.1);
    filter: drop-shadow(0 0 8px var(--pastel-amber-glow));
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
</style>

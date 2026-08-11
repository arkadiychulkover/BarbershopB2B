<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../lib/api';

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
  let submittingReview = false;

  onMount(async () => {
    await fetchMasters();
  });

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
      error = err.message || 'Ошибка при оформлении записи';
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
        <button class="back-btn" on:click={goBack}>← Назад</button>
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
            <div class="arrow">→</div>
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
            <p>Рейтинг: ⭐ {masterRating > 0 ? masterRating.toFixed(1) : 'Нет оценок'}</p>
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
                <span>⭐ {r.rating}</span>
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
              <div class="arrow">→</div>
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
        <div class="icon">✅</div>
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
            <span 
              class="star {reviewRating >= star ? 'active' : ''}" 
              on:click={() => reviewRating = star}
            >⭐</span>
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
    padding: 16px;
    box-sizing: border-box;
    min-height: 100vh;
    background-color: var(--tg-theme-bg-color, #ffffff);
    color: var(--tg-theme-text-color, #000000);
  }

  .header {
    display: flex;
    align-items: center;
    margin-bottom: 20px;
    position: relative;
  }

  .back-btn {
    background: none;
    border: none;
    color: var(--tg-theme-link-color, #3390ec);
    font-size: 16px;
    padding: 0;
    margin-right: 16px;
    cursor: pointer;
  }

  h2 {
    font-size: 20px;
    margin: 0;
  }

  .error {
    background-color: #ffebee;
    color: #c62828;
    padding: 12px;
    border-radius: 8px;
    margin-bottom: 16px;
  }

  .list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .card {
    background-color: var(--tg-theme-secondary-bg-color, #f4f4f5);
    padding: 16px;
    border-radius: 12px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    transition: background-color 0.2s;
  }

  .card:active {
    opacity: 0.8;
  }

  .card h3 {
    margin: 0 0 4px 0;
    font-size: 16px;
  }

  .card p {
    margin: 0;
    font-size: 14px;
    color: var(--tg-theme-hint-color, #999999);
  }

  .arrow {
    color: var(--tg-theme-hint-color, #999999);
    font-size: 20px;
  }

  .empty {
    text-align: center;
    color: var(--tg-theme-hint-color, #999999);
    margin-top: 40px;
  }

  .datetime-picker {
    display: flex;
    flex-direction: column;
  }

  label {
    margin-bottom: 8px;
    font-weight: 500;
  }

  input[type="date"] {
    padding: 12px;
    border-radius: 8px;
    border: 1px solid var(--tg-theme-hint-color, #ccc);
    margin-bottom: 20px;
    font-size: 16px;
    background-color: var(--tg-theme-bg-color, #fff);
    color: var(--tg-theme-text-color, #000);
  }

  .slots {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
    gap: 10px;
    margin-bottom: 24px;
  }

  .slot-btn {
    padding: 10px;
    border-radius: 8px;
    border: 1px solid var(--tg-theme-button-color, #3390ec);
    background-color: transparent;
    color: var(--tg-theme-button-color, #3390ec);
    font-size: 14px;
    cursor: pointer;
    transition: all 0.2s;
  }

  .slot-btn.selected {
    background-color: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #fff);
  }

  .empty-slots {
    grid-column: 1 / -1;
    color: var(--tg-theme-hint-color, #999999);
    text-align: center;
    padding: 20px 0;
  }

  .primary-btn {
    background-color: var(--tg-theme-button-color, #3390ec);
    color: var(--tg-theme-button-text-color, #ffffff);
    border: none;
    padding: 16px;
    border-radius: 12px;
    font-size: 16px;
    font-weight: 600;
    width: 100%;
    cursor: pointer;
  }

  .primary-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .success-screen {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
    margin-top: 40px;
  }

  .success-screen .icon {
    font-size: 64px;
    margin-bottom: 16px;
  }

  .success-screen p {
    margin: 8px 0;
    color: var(--tg-theme-hint-color, #999999);
  }

  .loader-container {
    display: flex;
    justify-content: center;
    padding: 40px;
  }

  .spinner {
    width: 32px;
    height: 32px;
    border: 3px solid var(--tg-theme-hint-color, #ccc);
    border-top: 3px solid var(--tg-theme-button-color, #3390ec);
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }

  .tabs {
    display: flex;
    margin-bottom: 24px;
    background-color: var(--tg-theme-secondary-bg-color, #f4f4f5);
    border-radius: 12px;
    padding: 4px;
  }

  .tab-btn {
    flex: 1;
    padding: 12px;
    border: none;
    background: transparent;
    border-radius: 8px;
    color: var(--tg-theme-hint-color, #999999);
    font-size: 15px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
  }

  .tab-btn.active {
    background-color: var(--tg-theme-bg-color, #ffffff);
    color: var(--tg-theme-text-color, #000000);
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
  }

  .appt-card {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .appt-info h3 {
    margin-bottom: 8px;
  }

  .appt-info p {
    margin-bottom: 4px;
  }

  .status-0 { color: #f57c00; font-weight: 500; }
  .status-1 { color: #388e3c; font-weight: 500; }
  .status-2 { color: #d32f2f; font-weight: 500; }
  .status-3 { color: #757575; font-weight: 500; }

  .cancel-btn {
    background-color: #ffebee;
    color: #c62828;
    border: none;
    padding: 10px 16px;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    width: 100%;
    cursor: pointer;
    margin-top: 8px;
  }

  .master-header {
    background-color: var(--tg-theme-secondary-bg-color, #f4f4f5);
    padding: 16px;
    border-radius: 12px;
    margin-bottom: 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  .secondary-btn {
    background: transparent;
    color: var(--tg-theme-button-color, #3390ec);
    border: 1px solid var(--tg-theme-button-color, #3390ec);
    padding: 12px;
    border-radius: 8px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    flex: 1;
  }
  .mt-2 { margin-top: 8px; }
  
  .reviews-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .review-card {
    background-color: var(--tg-theme-secondary-bg-color, #f4f4f5);
    padding: 12px;
    border-radius: 8px;
  }
  .review-head {
    display: flex;
    justify-content: space-between;
    margin-bottom: 6px;
  }
  .review-card p {
    margin: 0;
    font-size: 14px;
    color: var(--tg-theme-text-color, #000);
  }
  
  .modal-overlay {
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 100;
  }
  .modal-content {
    background: var(--tg-theme-bg-color, #ffffff);
    padding: 20px;
    border-radius: 16px;
    width: 90%;
    max-width: 400px;
    display: flex;
    flex-direction: column;
    gap: 12px;
    color: var(--tg-theme-text-color, #000);
  }
  .modal-content h3 { margin: 0; }
  .modal-content p { margin: 0; font-size: 14px; color: var(--tg-theme-hint-color, #999); }
  
  .stars {
    display: flex;
    justify-content: center;
    gap: 8px;
    font-size: 32px;
    margin: 10px 0;
  }
  .star {
    cursor: pointer;
    opacity: 0.3;
  }
  .star.active {
    opacity: 1;
  }
  textarea {
    width: 100%;
    height: 80px;
    border-radius: 8px;
    border: 1px solid var(--tg-theme-hint-color, #ccc);
    padding: 8px;
    background: var(--tg-theme-secondary-bg-color, #f4f4f5);
    color: var(--tg-theme-text-color, #000);
    box-sizing: border-box;
    font-family: inherit;
    resize: none;
  }
  .modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 8px;
  }
</style>

<script lang="ts">
  import { onMount, createEventDispatcher } from "svelte";
  import { apiFetch } from "../api";
  import {
    showAlert,
    showConfirm,
    hapticSuccess,
    hapticError,
    hapticWarning,
  } from "../telegram";
  import SecureImage from "./SecureImage.svelte";
  import Icon from "./Icon.svelte";
  import { m } from "../paraglide/messages.js";
  import { currentLocale } from "../locale";

  const dispatch = createEventDispatcher();

  let innerWidth = 0;
  $: isDesktop = innerWidth >= 768;
  $: dateLocale = $currentLocale === "en" ? "en-US" : "ru-RU";

  let currentDate = new Date();
  let currentWeekStart = getMonday(new Date());

  let appointments = [];
  let services = [];
  let loading = true;

  let view: "list" | "form" = "list";
  let editingAppt = null;

  // expanded card id (mobile list)
  let expandedId: string | null = null;
  // selected appointment for full details modal (desktop/fullscreen)
  let selectedDetailAppt: any = null;

  // photo upload state per appointment
  let uploadingId: string | null = null;
  let photoInputEl: HTMLInputElement;
  let pendingPhotoApptId: string | null = null;

  // form state
  let formDateStr = "";
  let formTime = "";
  let formServiceId = "";
  let formStatus: number | string = 0;
  let formClientName = "";
  let formClientPhone = "";
  let formComment = "";
  let saving = false;

  function extractDate(dateStr: any): string {
    if (!dateStr) return "";
    const m = String(dateStr).match(/(\d{4})-(\d{2})-(\d{2})/);
    return m ? `${m[1]}-${m[2]}-${m[3]}` : "";
  }

  function isPureNumeric(val: string | null | undefined): boolean {
    return !!val && /^\d+$/.test(val.trim());
  }

  function extractTime(dateStr: any): string {
    if (!dateStr) return "";
    const m = String(dateStr).match(/(?:T|\s|^)(\d{2}):(\d{2})/);
    return m ? `${m[1]}:${m[2]}` : "";
  }

  function addMinutesToTimeString(timeStr: string, minutes: number): string {
    if (!timeStr) return "";
    const [h, m] = timeStr.split(":").map(Number);
    if (isNaN(h) || isNaN(m)) return timeStr;
    const totalMinutes = h * 60 + m + minutes;
    const endH = Math.floor(totalMinutes / 60) % 24;
    const endM = totalMinutes % 60;
    return `${String(endH).padStart(2, "0")}:${String(endM).padStart(2, "0")}`;
  }

  function extractEndTime(appt: any): string {
    if (!appt) return "";
    const start = extractTime(appt.appointmentDate);
    let end = extractTime(appt.appointmentEndDate);
    if (!end || end === start) {
      const svc = services.find((s) => s.serviceId === appt.serviceId);
      const duration = svc?.duration || 30;
      end = addMinutesToTimeString(start, duration);
    }
    return end;
  }

  function formatDateInput(d: Date): string {
    if (!d) return "";
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function formatModalDate(dateStr: any): string {
    const raw = extractDate(dateStr);
    if (!raw) return "";
    const parts = raw.split("-");
    return `${parts[2]}.${parts[1]}.${parts[0]}`;
  }

  $: dateString = formatDateInput(currentDate);

  function getMonday(d) {
    d = new Date(d);
    var day = d.getDay(),
      diff = d.getDate() - day + (day == 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  }

  function getDaysOfWeek(startDate) {
    let days = [];
    for (let i = 0; i < 7; i++) {
      let d = new Date(startDate);
      d.setDate(d.getDate() + i);
      days.push(d);
    }
    return days;
  }

  $: days = getDaysOfWeek(currentWeekStart);

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    try {
      const [apptsData, servicesData] = await Promise.all([
        apiFetch("/api/Barber/get-master-appointments"),
        apiFetch("/api/Barber/my-services"),
      ]);
      appointments = apptsData;
      services = servicesData.filter((s) => s.serviceId && s.isActive);
    } catch (error) {
      console.error(error);
      showAlert(m.tma_error_server());
    } finally {
      loading = false;
    }
  }

  function prevDay() {
    let d = new Date(currentDate);
    d.setDate(d.getDate() - 1);
    currentDate = d;
  }

  function nextDay() {
    let d = new Date(currentDate);
    d.setDate(d.getDate() + 1);
    currentDate = d;
  }

  function prevWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() - 7);
    currentWeekStart = d;
  }

  function nextWeek() {
    let d = new Date(currentWeekStart);
    d.setDate(d.getDate() + 7);
    currentWeekStart = d;
  }

  function getApptsForDayDate(date, apptsList = appointments) {
    if (!date || !apptsList) return [];
    const targetDateStr = formatDateInput(date);
    return apptsList
      .filter((a) => {
        const apptDateStr = extractDate(a.appointmentDate);
        return apptDateStr === targetDateStr;
      })
      .sort((a, b) => (a.appointmentDate || "").localeCompare(b.appointmentDate || ""));
  }

  $: currentDayAppts = getApptsForDayDate(currentDate, appointments);

  function toggleExpand(apptId: string) {
    expandedId = expandedId === apptId ? null : apptId;
  }

  function openNewForm(dateToUse) {
    expandedId = null;
    editingAppt = null;
    const targetDate = dateToUse ? new Date(dateToUse) : new Date();
    formDateStr = formatDateInput(targetDate);
    formTime = "10:00";
    formServiceId = services.length > 0 ? services[0].serviceId : "";
    formStatus = 0;
    formClientName = "";
    formClientPhone = "";
    formComment = "";
    view = "form";
  }

  function openEditForm(appt) {
    expandedId = null;
    editingAppt = appt;
    formDateStr = extractDate(appt.appointmentDate);
    formTime = extractTime(appt.appointmentDate) || "10:00";
    formServiceId =
      appt.serviceId || (services.length > 0 ? services[0].serviceId : "");
    formStatus = appt.status;
    formClientName = appt.clientName || "";
    formClientPhone = appt.clientPhone || "";
    formComment = appt.resultNote || "";
    view = "form";
  }

  function cancelForm() {
    view = "list";
    editingAppt = null;
  }

  async function saveAppt() {
    if (saving) return;

    if (!formServiceId) {
      showAlert(m.tma_select_service_err());
      return;
    }

    if (!formDateStr || !formTime) {
      showAlert(m.tma_select_datetime_err());
      return;
    }

    const timeParts = formTime.split(":");
    const h = parseInt(timeParts[0], 10) || 0;
    const m = parseInt(timeParts[1], 10) || 0;

    const appointmentDate = `${formDateStr}T${formTime}:00Z`;

    const body: any = {
      clientId: editingAppt ? (editingAppt.clientId || null) : null,
      serviceId: formServiceId,
      appointmentDate: appointmentDate,
      status: parseInt(String(formStatus), 10) || 0,
      clientName: formClientName ? formClientName.trim() : null,
      clientPhone: formClientPhone ? formClientPhone.trim() : null,
      comment: formComment ? formComment.trim() : null,
    };

    saving = true;
    try {
      let createdApptId: string | null = null;
      if (editingAppt) {
        await apiFetch(`/api/Barber/my-appointments/update/${editingAppt.id}`, {
          method: "PUT",
          body,
        });
        createdApptId = editingAppt.id;
      } else {
        const res = await apiFetch("/api/Barber/my-appointments/add", {
          method: "POST",
          body,
        });
        createdApptId = res?.appointmentId || null;
      }
      hapticSuccess();

      // Immediately sync active view date to the appointment date
      const dateParts = formDateStr.split("-");
      const apptYear = parseInt(dateParts[0], 10) || new Date().getFullYear();
      const apptMonth = parseInt(dateParts[1], 10) || 1;
      const apptDay = parseInt(dateParts[2], 10) || 1;
      currentDate = new Date(apptYear, apptMonth - 1, apptDay, 12, 0, 0);
      currentWeekStart = getMonday(currentDate);

      view = "list";
      editingAppt = null;
      if (createdApptId) {
        expandedId = createdApptId;
      }
      await loadData();
    } catch (e: any) {
      hapticError();
      if (e.status === 409) {
        showAlert(m.tma_overlap_err());
      } else {
        showAlert(m.tma_save_error() + ": " + (e.message || ""));
      }
    } finally {
      saving = false;
    }
  }

  function deleteAppt() {
    hapticWarning();
    showConfirm(
      m.tma_delete_appt_confirm(),
      async (confirmed) => {
        if (confirmed && editingAppt) {
          try {
            await apiFetch(
              `/api/Barber/my-appointments/delete/${editingAppt.id}`,
              {
                method: "DELETE",
              },
            );
            hapticSuccess();
            view = "list";
            await loadData();
          } catch (error) {
            hapticError();
            showAlert(m.tma_delete_appt_err());
          }
        }
      },
    );
  }

  function cancelAppointment(apptId: string) {
    hapticWarning();
    showConfirm(
      m.tma_cancel_appt_confirm(),
      async (confirmed) => {
        if (!confirmed) return;
        try {
          await apiFetch(`/api/Barber/my-appointments/cancel/${apptId}`, {
            method: "PUT",
          });
          hapticSuccess();
          showAlert(m.tma_appt_cancelled_success());
          await loadData();
          if (selectedDetailAppt && selectedDetailAppt.id === apptId) {
            selectedDetailAppt.status = 2;
            selectedDetailAppt = { ...selectedDetailAppt };
          }
        } catch (error: any) {
          hapticError();
          showAlert(m.tma_cancel_error() + (error.message || ""));
        }
      },
    );
  }

  function triggerPhotoUpload(apptId: string) {
    const targetAppt =
      appointments.find((a) => a.id === apptId) ||
      (selectedDetailAppt?.id === apptId ? selectedDetailAppt : null);
    if (targetAppt && targetAppt.status !== 1) {
      showAlert(
        m.tma_photo_only_completed(),
      );
      return;
    }
    pendingPhotoApptId = apptId;
    photoInputEl.value = "";
    photoInputEl.click();
  }

  async function handlePhotoSelected(event) {
    const file = event.target.files?.[0];
    if (!file || !pendingPhotoApptId) return;

    uploadingId = pendingPhotoApptId;
    try {
      const formData = new FormData();
      formData.append("photo", file);

      const result = await apiFetch(`/api/Barber/put-photo/${pendingPhotoApptId}`, {
        method: "POST",
        body: formData,
      });

      hapticSuccess();
      await loadData();
      // keep card expanded after upload
      expandedId = pendingPhotoApptId;
      if (selectedDetailAppt && selectedDetailAppt.id === pendingPhotoApptId) {
        selectedDetailAppt =
          appointments.find((a) => a.id === pendingPhotoApptId) || null;
      }
    } catch (e: any) {
      hapticError();
      showAlert(m.tma_photo_upload_err() + (e?.message || ""));
    } finally {
      uploadingId = null;
      pendingPhotoApptId = null;
    }
  }

  function statusLabel(status: number): string {
    if (status === 1) return m.tma_status_done();
    if (status === 2) return m.tma_status_cancelled();
    return m.tma_status_scheduled();
  }

  // --- Comment Logic ---
  let editingCommentId: string | null = null;
  let commentText = "";

  function openCommentEdit(appt: any) {
    editingCommentId = appt.id;
    commentText = appt.resultNote || "";
  }

  function cancelCommentEdit() {
    editingCommentId = null;
  }

  async function saveComment(apptId: string) {
    try {
      const res = await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
        method: "POST",
        body: { comment: commentText },
      });
      const index = appointments.findIndex((a) => a.id === apptId);
      if (index !== -1) {
        appointments[index].resultNote = res.comment;
        appointments = [...appointments];
      }
      if (selectedDetailAppt && selectedDetailAppt.id === apptId) {
        selectedDetailAppt.resultNote = res.comment;
        selectedDetailAppt = { ...selectedDetailAppt };
      }
      editingCommentId = null;
      hapticSuccess();
    } catch (e) {
      console.error(e);
      hapticError();
      showAlert(m.tma_comment_save_error());
    }
  }

  async function deleteComment(apptId: string) {
    showConfirm(m.tma_delete_comment_confirm(), async (confirmed) => {
      if (!confirmed) return;
      try {
        await apiFetch(`/api/Barber/appointment-comment/${apptId}`, {
          method: "DELETE",
        });
        const index = appointments.findIndex((a) => a.id === apptId);
        if (index !== -1) {
          appointments[index].resultNote = null;
          appointments = [...appointments];
        }
        if (selectedDetailAppt && selectedDetailAppt.id === apptId) {
          selectedDetailAppt.resultNote = null;
          selectedDetailAppt = { ...selectedDetailAppt };
        }
        hapticSuccess();
      } catch (e) {
        console.error(e);
        hapticError();
        showAlert(m.tma_delete_comment_error());
      }
    });
  }
</script>

<svelte:window bind:innerWidth />

<div class="appointments-container" class:desktop-mode={isDesktop}>
  <!-- hidden photo file input for both desktop and mobile -->
  <input
    type="file"
    accept="image/*"
    style="display:none"
    bind:this={photoInputEl}
    on:change={handlePhotoSelected}
  />

  {#if view === "list"}
    {#if isDesktop}
      <div class="desktop-header">
        <div class="week-nav">
          <button class="nav-btn" on:click={prevWeek}>&larr;</button>
          <span class="week-label">
            {days[0].toLocaleDateString(dateLocale, {
              day: "2-digit",
              month: "2-digit",
            })} -
            {days[6].toLocaleDateString(dateLocale, {
              day: "2-digit",
              month: "2-digit",
            })}
          </span>
          <button class="nav-btn" on:click={nextWeek}>&rarr;</button>
        </div>
        <button class="primary-btn" on:click={() => openNewForm(new Date())}
          >{m.tma_new_appt()}</button>
      </div>

      {#if loading}
        <div class="loading">{m.tma_loading_schedule()}</div>
      {:else}
        <div class="calendar-grid">
          {#each days as day}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="day-col" on:click={() => openNewForm(day)}>
              <div class="day-header">
                <div class="day-name">
                  {day.toLocaleDateString(dateLocale, { weekday: "short" })}
                </div>
                <div class="day-date">
                  {day.toLocaleDateString(dateLocale, {
                    day: "2-digit",
                    month: "2-digit",
                  })}
                </div>
              </div>
              <div class="day-body">
                {#each getApptsForDayDate(day, appointments) as appt}
                  <!-- svelte-ignore a11y-click-events-have-key-events -->
                  <!-- svelte-ignore a11y-no-static-element-interactions -->
                  <div
                    class="appt-card compact desktop-card status-{appt.status}"
                    class:has-photo={!!appt.photoResultUrl}
                    on:click|stopPropagation={() => {
                      selectedDetailAppt = appt;
                    }}
                  >
                    <div class="card-main-row">
                      <div class="appt-time">
                        <span class="time-start">{extractTime(appt.appointmentDate)}</span>
                        <span class="time-sep">–</span>
                        <span class="time-end">{extractEndTime(appt)}</span>
                      </div>
                      <div class="appt-details">
                        <div class="service-name">
                          {appt.serviceName ||
                            services.find((s) => s.serviceId === appt.serviceId)
                              ?.name ||
                            m.tma_service()}
                        </div>
                        <div class="client-name">
                          {#if appt.clientName && appt.clientName !== "\u0413\u043e\u0441\u0442\u044c"}
                            {appt.clientName}
                          {:else if appt.clientTelegramId && appt.clientTelegramId !== "WALKIN"}
                            {appt.clientName || m.tma_client()}
                          {:else}
                            {m.tma_walkin_guest()}
                          {/if}
                        </div>
                      </div>
                      <div class="card-right">
                        <div class="status-icon">
                          {#if appt.status === 1}
                            <Icon
                              name="check"
                              size={14}
                              color="var(--pastel-sage)"
                            />
                          {:else if appt.status === 2}
                            <Icon
                              name="x"
                              size={14}
                              color="var(--pastel-coral)"
                            />
                          {/if}
                        </div>
                        {#if appt.photoResultUrl}
                          <Icon
                            name="camera"
                            size={12}
                            color="var(--pastel-rose)"
                          />
                        {/if}
                        <div class="expand-arrow">
                          <Icon name="chevron-right" size={14} />
                        </div>
                      </div>
                    </div>
                  </div>
                {/each}
              </div>
            </div>
          {/each}
        </div>

        {#if selectedDetailAppt}
          <!-- svelte-ignore a11y-click-events-have-key-events -->
          <!-- svelte-ignore a11y-no-static-element-interactions -->
          <div
            class="modal-overlay"
            on:click={() => (selectedDetailAppt = null)}
          >
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div class="detail-modal-card" on:click|stopPropagation>
              <!-- Modal Header -->
              <div class="detail-modal-header">
                <div class="detail-modal-title-wrap">
                  <div class="detail-time-badge">
                    <Icon name="clock" size={16} />
                    <span
                      >{extractTime(selectedDetailAppt.appointmentDate)} – {extractEndTime(selectedDetailAppt)}</span
                    >
                    <span class="detail-date-sub"
                      >({formatModalDate(selectedDetailAppt.appointmentDate)})</span
                    >
                  </div>
                  <span
                    class="meta-badge status-badge-{selectedDetailAppt.status}"
                  >
                    {statusLabel(selectedDetailAppt.status)}
                  </span>
                </div>
                <button
                  class="modal-close-btn"
                  on:click={() => (selectedDetailAppt = null)}
                >
                  <Icon name="x" size={18} />
                </button>
              </div>

              <div class="detail-modal-body">
                <!-- Service & Client Info Grid -->
                <div class="detail-info-grid">
                  <div class="detail-info-box">
                    <span class="detail-box-label">{m.tma_service()}</span>
                    <span class="detail-box-value highlight"
                      >{selectedDetailAppt.serviceName ||
                        services.find(
                          (s) => s.serviceId === selectedDetailAppt.serviceId,
                        )?.name ||
                        m.tma_service()}</span
                    >
                    <span class="detail-box-sub">
                      {services.find(
                        (s) => s.serviceId === selectedDetailAppt.serviceId,
                      )?.duration || 30} {m.tma_min_dot()}
                      {services.find(
                        (s) => s.serviceId === selectedDetailAppt.serviceId,
                      )?.price ||
                        selectedDetailAppt.price ||
                        0} ₴
                    </span>
                  </div>

                  <div class="detail-info-box">
                    <span class="detail-box-label">{m.tma_client()}</span>
                    {#if selectedDetailAppt.clientId}
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <div
                        class="detail-client-link"
                        on:click={() => {
                          const cId = selectedDetailAppt.clientId;
                          selectedDetailAppt = null;
                          dispatch("openClientHistory", cId);
                        }}
                        title={m.tma_client_history_title()}
                      >
                        <span class="detail-box-value"
                          >{selectedDetailAppt.clientName && selectedDetailAppt.clientName !== "\u0413\u043e\u0441\u0442\u044c"
                            ? selectedDetailAppt.clientName
                            : (selectedDetailAppt.clientTelegramId === "WALKIN" ? "{m.tma_walkin_guest()}" : m.tma_client())}</span
                        >
                        {#if selectedDetailAppt.clientPhone}
                          <a 
                            class="detail-client-handle-link"
                            href="tel:{selectedDetailAppt.clientPhone}"
                            on:click|stopPropagation
                          >{selectedDetailAppt.clientPhone}</a>
                        {/if}
                        {#if selectedDetailAppt.clientTelegramUsername}
                          <a 
                            class="detail-client-handle-link"
                            href="https://t.me/{selectedDetailAppt.clientTelegramUsername}"
                            target="_blank" 
                            rel="noopener noreferrer"
                            on:click|stopPropagation
                          >@{selectedDetailAppt.clientTelegramUsername}</a>
                        {:else if selectedDetailAppt.clientTelegramId && selectedDetailAppt.clientTelegramId !== "WALKIN"}
                          {#if isPureNumeric(selectedDetailAppt.clientTelegramId)}
                            <span class="detail-client-handle">ID: {selectedDetailAppt.clientTelegramId}</span>
                          {:else}
                            <a 
                              class="detail-client-handle-link"
                              href="https://t.me/{selectedDetailAppt.clientTelegramId.replace(/^@/, '')}"
                              target="_blank" 
                              rel="noopener noreferrer"
                              on:click|stopPropagation
                            >@{selectedDetailAppt.clientTelegramId.replace(/^@/, '')}</a>
                          {/if}
                        {/if}
                        <div class="client-history-action-row">
                          <button
                            type="button"
                            class="btn-open-history-pill"
                            on:click|stopPropagation={() => {
                              const cId = selectedDetailAppt.clientId;
                              selectedDetailAppt = null;
                              dispatch("openClientHistory", cId);
                            }}
                          >
                            <Icon name="clock" size={12} />
                            <span>{m.tma_visit_history()} &rarr;</span>
                          </button>
                        </div>
                      </div>
                    {:else}
                      <span class="detail-box-value"
                        >{selectedDetailAppt.clientName && selectedDetailAppt.clientName !== "\u0413\u043e\u0441\u0442\u044c"
                          ? selectedDetailAppt.clientName
                          : "{m.tma_walkin_guest()}"}</span
                      >
                    {/if}
                  </div>
                </div>

                <!-- Photo Section -->
                {#if selectedDetailAppt.photoResultUrl}
                  <div class="detail-section">
                    <span class="detail-section-label"
                      >{m.tma_photo_result()}</span
                    >
                    <div class="detail-photo-card">
                      <SecureImage
                        src={selectedDetailAppt.photoResultUrl}
                        alt={m.tma_result()}
                        className="detail-photo-img"
                        style="width:100%;max-height:280px;object-fit:contain;border-radius:12px;background:#111;"
                      />
                      {#if selectedDetailAppt.status === 1}
                        <button
                          class="change-photo-btn mt-2"
                          on:click={() =>
                            triggerPhotoUpload(selectedDetailAppt.id)}
                          disabled={uploadingId === selectedDetailAppt.id}
                        >
                          {#if uploadingId === selectedDetailAppt.id}
                            <span class="spinner"></span> {m.tma_loading()}
                          {:else}
                            <Icon name="refresh" size={14} />
                            <span>{m.tma_replace_photo()}</span>
                          {/if}
                        </button>
                      {/if}
                    </div>
                  </div>
                {:else if selectedDetailAppt.status === 1}
                  <div class="detail-section">
                    <span class="detail-section-label"
                      >{m.tma_photo_result()}</span
                    >
                    <button
                      class="attach-photo-btn"
                      on:click={() => triggerPhotoUpload(selectedDetailAppt.id)}
                      disabled={uploadingId === selectedDetailAppt.id}
                    >
                      {#if uploadingId === selectedDetailAppt.id}
                        <span class="spinner"></span> {m.tma_loading()}
                      {:else}
                        <Icon name="paperclip" size={16} />
                        <span>{m.tma_attach_result_photo()}</span>
                      {/if}
                    </button>
                  </div>
                {/if}

                <!-- Notes / Comments Section -->
                <div class="detail-section">
                  <span class="detail-section-label"
                    >{m.tma_master_note_title()}</span
                  >
                  {#if editingCommentId === selectedDetailAppt.id}
                    <textarea
                      class="comment-input"
                      bind:value={commentText}
                      placeholder={m.tma_master_note_placeholder()}
                    ></textarea>
                    <div class="comment-actions">
                      <button
                        class="btn-save"
                        on:click={() => saveComment(selectedDetailAppt.id)}
                        >{m.tma_save()}</button>
                      <button class="btn-cancel" on:click={cancelCommentEdit}
                        >{m.tma_cancel()}</button>
                    </div>
                  {:else if selectedDetailAppt.resultNote}
                    <div class="comment-display">
                      <div class="comment-text">
                        <Icon
                          name="comment"
                          size={14}
                          color="var(--pastel-rose)"
                        />
                        <span>{selectedDetailAppt.resultNote}</span>
                      </div>
                      <div class="comment-actions-sm">
                        <button
                          on:click={() => openCommentEdit(selectedDetailAppt)}
                          >{m.tma_edit()}</button>
                        <button
                          class="text-danger"
                          on:click={() => deleteComment(selectedDetailAppt.id)}
                          >{m.tma_delete()}</button>
                      </div>
                    </div>
                  {:else}
                    <button
                      class="btn-add-comment"
                      on:click={() => openCommentEdit(selectedDetailAppt)}
                    >
                      <Icon name="plus" size={14} />
                      <span>{m.tma_add_note()}</span>
                    </button>
                  {/if}
                </div>
              </div>

              <!-- Modal Footer Actions -->
              <div class="detail-modal-footer">
                <button
                  class="primary-btn flex-1"
                  on:click={() => {
                    const a = selectedDetailAppt;
                    selectedDetailAppt = null;
                    openEditForm(a);
                  }}
                >
                  <Icon name="edit" size={15} />
                  <span>{m.tma_edit()}</span>
                </button>
                {#if selectedDetailAppt.clientId}
                  <button
                    type="button"
                    class="history-btn flex-1"
                    on:click={() => {
                      const cId = selectedDetailAppt.clientId;
                      selectedDetailAppt = null;
                      dispatch("openClientHistory", cId);
                    }}
                    title={m.tma_client_history_title()}
                  >
                    <Icon name="clock" size={15} />
                    <span>{m.tma_visit_history()}</span>
                  </button>
                {/if}
                {#if selectedDetailAppt.status !== 2}
                  <button
                    class="danger-btn flex-1"
                    on:click={() => cancelAppointment(selectedDetailAppt.id)}
                  >
                    <Icon name="x" size={15} />
                    <span>{m.tma_cancel_appt_btn()}</span>
                  </button>
                {/if}
                <button
                  class="secondary-btn flex-1"
                  on:click={() => (selectedDetailAppt = null)}
                >{m.tma_close()}</button>
              </div>
            </div>
          </div>
        {/if}
      {/if}
    {:else}
      <!-- Mobile Layout -->
      <div class="date-selector">
        <button class="nav-btn" on:click={prevDay}>&larr;</button>
        <div class="current-date">
          {currentDate.toLocaleDateString(dateLocale, {
            weekday: "long",
            day: "numeric",
            month: "long",
          })}
        </div>
        <button class="nav-btn" on:click={nextDay}>&rarr;</button>
      </div>

      {#if loading}
        <div class="loading">{m.tma_loading_schedule()}</div>
      {:else if currentDayAppts.length === 0}
        <div class="empty-state">
          <div class="icon">
            <Icon name="empty-calendar" size={44} color="var(--pastel-rose)" />
          </div>
          <p>{m.tma_no_appts_today()}</p>
          <button class="primary-btn" on:click={() => openNewForm(currentDate)}
            >{m.tma_add_appt_btn()}</button>
        </div>
      {:else}
        <!-- hidden photo file input -->
        <input
          type="file"
          accept="image/*"
          style="display:none"
          bind:this={photoInputEl}
          on:change={handlePhotoSelected}
        />

        <div class="appointments-list">
          {#each currentDayAppts as appt}
            <!-- svelte-ignore a11y-click-events-have-key-events -->
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <div
              class="appt-card mobile status-{appt.status}"
              class:expanded={expandedId === appt.id}
              on:click={() => toggleExpand(appt.id)}
            >
              <div class="card-main-row">
                <div class="appt-time">
                  <span class="time-start">{extractTime(appt.appointmentDate)}</span>
                  <span class="time-sep">–</span>
                  <span class="time-end">{extractEndTime(appt)}</span>
                </div>
                <div class="appt-details">
                  <div class="service-name">
                    {appt.serviceName ||
                      services.find((s) => s.serviceId === appt.serviceId)
                        ?.name ||
                      m.tma_service()}
                  </div>
                  <div class="client-name">
                    {#if appt.clientName && appt.clientName !== "\u0413\u043e\u0441\u0442\u044c"}
                      {appt.clientName}
                    {:else if appt.clientTelegramId && appt.clientTelegramId !== "WALKIN"}
                      {appt.clientName || m.tma_client()}
                    {:else}
                      {m.tma_walkin_guest()}
                    {/if}
                  </div>
                </div>
                <div class="card-right">
                  <div class="status-icon">
                    {#if appt.status === 1}
                      <Icon name="check" size={16} color="var(--pastel-sage)" />
                    {:else if appt.status === 2}
                      <Icon name="x" size={16} color="var(--pastel-coral)" />
                    {/if}
                  </div>
                  <div class="expand-arrow" class:open={expandedId === appt.id}>
                    <Icon name="chevron-right" size={16} />
                  </div>
                </div>
              </div>

              {#if expandedId === appt.id}
                <div class="card-expanded" on:click|stopPropagation>
                  <div class="expand-divider"></div>

                  <div class="expand-meta">
                    <span class="meta-badge status-badge-{appt.status}">
                      {statusLabel(appt.status)}
                    </span>
                    <div class="expand-actions">
                      <button
                        class="edit-link"
                        on:click={() => openEditForm(appt)}
                      >
                        <Icon name="edit" size={13} />
                        <span>{m.tma_edit()}</span>
                      </button>
                      {#if appt.status !== 2}
                        <button
                          class="cancel-link"
                          on:click={() => cancelAppointment(appt.id)}
                        >
                          <Icon name="x" size={13} />
                          <span>{m.tma_cancel()}</span>
                        </button>
                      {/if}
                    </div>
                  </div>

                  {#if appt.clientId}
                    <div class="client-tg-row">
                      <span class="client-tg-label">{m.tma_client()}:</span>
                      <!-- svelte-ignore a11y-click-events-have-key-events -->
                      <!-- svelte-ignore a11y-no-static-element-interactions -->
                      <span
                        class="client-tg-id"
                        on:click={() =>
                          dispatch("openClientHistory", appt.clientId)}
                        title={m.tma_client_history_title()}
                      >
                        <Icon
                          name="user"
                          size={14}
                          color="var(--pastel-lavender)"
                        />
                        {#if appt.clientTelegramUsername}
                          <span class="client-handle-text">
                            {appt.clientName || `@${appt.clientTelegramUsername}`} · 
                            <a 
                              class="tg-chat-link" 
                              href="https://t.me/{appt.clientTelegramUsername}" 
                              target="_blank" 
                              rel="noopener noreferrer"
                              on:click|stopPropagation
                            >@{appt.clientTelegramUsername}</a>
                            {#if appt.clientPhone}
                              <a 
                                class="tg-chat-link" 
                                href="tel:{appt.clientPhone}" 
                                on:click|stopPropagation
                              >· {appt.clientPhone}</a>
                            {/if}
                          </span>
                        {:else if appt.clientTelegramId && appt.clientTelegramId !== "WALKIN"}
                          {#if isPureNumeric(appt.clientTelegramId)}
                            <span>{appt.clientName || m.tma_client()} · ID: {appt.clientTelegramId}{#if appt.clientPhone} · <a class="tg-chat-link" href="tel:{appt.clientPhone}" on:click|stopPropagation>{appt.clientPhone}</a>{/if}</span>
                          {:else}
                            <span class="client-handle-text">
                              {appt.clientName || appt.clientTelegramId} · 
                              <a 
                                class="tg-chat-link" 
                                href="https://t.me/{appt.clientTelegramId.replace(/^@/, '')}" 
                                target="_blank" 
                                rel="noopener noreferrer"
                                on:click|stopPropagation
                              >@{appt.clientTelegramId.replace(/^@/, '')}</a>
                              {#if appt.clientPhone}
                                <a 
                                  class="tg-chat-link" 
                                  href="tel:{appt.clientPhone}" 
                                  on:click|stopPropagation
                                >· {appt.clientPhone}</a>
                              {/if}
                            </span>
                          {/if}
                        {:else}
                          <span>{appt.clientName && appt.clientName !== "\u0413\u043e\u0441\u0442\u044c" ? appt.clientName : "{m.tma_walkin_guest()}"}{#if appt.clientPhone} · <a class="tg-chat-link" href="tel:{appt.clientPhone}" on:click|stopPropagation>{appt.clientPhone}</a>{/if}</span>
                        {/if}
                        <Icon name="chevron-right" size={12} />
                      </span>
                      <button
                        type="button"
                        class="btn-history-pill"
                        on:click|stopPropagation={() =>
                          dispatch("openClientHistory", appt.clientId)}
                        title={m.tma_client_history_title()}
                      >
                        <Icon name="clock" size={11} />
                        <span>{m.tma_visit_history()}</span>
                      </button>
                    </div>
                  {/if}

                  {#if appt.photoResultUrl}
                    <div class="photo-preview-wrap">
                      <SecureImage
                        src={appt.photoResultUrl}
                        alt={m.tma_result()}
                        className="photo-preview"
                        style="width:100%;max-height:200px;object-fit:cover;border-radius:8px;"
                      />
                      {#if appt.status === 1}
                        <button
                          class="change-photo-btn"
                          on:click={() => triggerPhotoUpload(appt.id)}
                          disabled={uploadingId === appt.id}
                        >
                          {#if uploadingId === appt.id}
                            <span class="spinner"></span> {m.tma_loading()}
                          {:else}
                            <Icon name="refresh" size={13} />
                            <span>{m.tma_replace_photo()}</span>
                          {/if}
                        </button>
                      {/if}
                    </div>
                  {:else if appt.status === 1}
                    <button
                      class="attach-photo-btn"
                      on:click={() => triggerPhotoUpload(appt.id)}
                      disabled={uploadingId === appt.id}
                    >
                      {#if uploadingId === appt.id}
                        <span class="spinner"></span> {m.tma_loading()}
                      {:else}
                        <Icon name="paperclip" size={15} />
                        <span>{m.tma_attach_result_photo()}</span>
                      {/if}
                    </button>
                  {/if}

                  <!-- Comment Section -->
                  <div class="appt-comment-section">
                    {#if editingCommentId === appt.id}
                      <textarea
                        class="comment-input"
                        bind:value={commentText}
                        placeholder={m.tma_comment_placeholder()}
                        on:click|stopPropagation
                      ></textarea>
                      <div class="comment-actions" on:click|stopPropagation>
                        <button
                          class="btn-save"
                          on:click={() => saveComment(appt.id)}
                          >{m.tma_save()}</button>
                        <button class="btn-cancel" on:click={cancelCommentEdit}
                          >{m.tma_cancel()}</button>
                      </div>
                    {:else if appt.resultNote}
                      <div class="comment-display" on:click|stopPropagation>
                        <div class="comment-text">
                          <Icon
                            name="comment"
                            size={13}
                            color="var(--pastel-rose)"
                          />
                          <span>{appt.resultNote}</span>
                        </div>
                        <div class="comment-actions-sm">
                          <button on:click={() => openCommentEdit(appt)}
                            >{m.tma_edit_short()}</button>
                          <button
                            class="text-danger"
                            on:click={() => deleteComment(appt.id)}
                            >{m.tma_delete_short()}</button>
                        </div>
                      </div>
                    {:else}
                      <button
                        class="btn-add-comment"
                        on:click|stopPropagation={() => openCommentEdit(appt)}
                        >{m.tma_add_comment()}</button>
                    {/if}
                  </div>
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}

      {#if !loading}
        <button class="add-btn" on:click={() => openNewForm(currentDate)}
          >+</button
        >
      {/if}
    {/if}
  {/if}

  {#if view === "form"}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-overlay" on:click={cancelForm}>
      <!-- svelte-ignore a11y-click-events-have-key-events -->
      <!-- svelte-ignore a11y-no-static-element-interactions -->
      <div class="form-modal-card" on:click|stopPropagation>
        <div class="modal-header">
          <h2>{editingAppt ? m.tma_edit_appt() : m.tma_new_appt()}</h2>
          <button
            class="modal-close-btn"
            on:click={cancelForm}
            type="button"
            aria-label={m.tma_close()}
          >
            <Icon name="x" size={18} />
          </button>
        </div>

        {#if services.length === 0}
          <div
            class="services-empty-warning"
            style="margin-bottom:16px;padding:12px;border-radius:10px;background:rgba(235,160,50,0.12);color:var(--pastel-peach);font-size:13px;border:1px solid rgba(235,160,50,0.25);"
          >
            {m.tma_no_services_warning()}
          </div>
        {/if}

        <div class="form-group">
          <label for="barber-form-client">{m.tma_client_name_opt()}</label>
          <input
            id="barber-form-client"
            type="text"
            class="input"
            bind:value={formClientName}
            placeholder={m.tma_guest_walkin()}
          />
        </div>

        <div class="form-group">
          <label for="barber-form-phone">{m.tma_client_phone_opt()}</label>
          <input
            id="barber-form-phone"
            type="tel"
            class="input"
            bind:value={formClientPhone}
            placeholder="+1 555 000-0000"
          />
        </div>

        <div class="form-group">
          <label for="barber-form-date">{m.tma_date()}</label>
          <input
            id="barber-form-date"
            type="date"
            class="input"
            bind:value={formDateStr}
          />
        </div>

        <div class="form-group">
          <label for="barber-form-time">{m.tma_time()}</label>
          <input
            id="barber-form-time"
            type="time"
            class="input"
            bind:value={formTime}
          />
        </div>

        <div class="form-group">
          <label for="barber-form-service">{m.tma_service()}</label>
          <select
            id="barber-form-service"
            class="input"
            bind:value={formServiceId}
          >
            {#each services as s}
              <option value={s.serviceId}>{s.name} ({s.price} ₴)</option>
            {/each}
          </select>
        </div>

        <div class="form-group">
          <label for="barber-form-status">{m.tma_status()}</label>
          <select id="barber-form-status" class="input" bind:value={formStatus}>
            <option value={0}>{m.tma_scheduled()}</option>
            <option value={1}>{m.tma_completed()}</option>
            <option value={2}>{m.tma_cancelled()}</option>
          </select>
        </div>

        <div class="form-group">
          <label for="barber-form-comment">{m.tma_master_note()}</label>
          <textarea
            id="barber-form-comment"
            class="input"
            bind:value={formComment}
            placeholder={m.tma_comment_placeholder()}
            rows="2"
            style="resize:vertical;"
          ></textarea>
        </div>

        <div class="form-actions">
          <button
            class="primary-btn flex-1"
            on:click={saveAppt}
            disabled={saving || services.length === 0}
          >
            {saving ? m.tma_loading() : m.tma_save()}
          </button>
          {#if editingAppt && editingAppt.status !== 2}
            <button
              type="button"
              class="warning-btn flex-1"
              on:click={() => {
                const id = editingAppt.id;
                cancelForm();
                cancelAppointment(id);
              }}
              disabled={saving}
            >
              {m.tma_cancel_appt()}
            </button>
          {/if}
          {#if editingAppt}
            <button
              class="danger-btn flex-1"
              on:click={deleteAppt}
              disabled={saving}>{m.tma_delete_appt()}</button
            >
          {/if}
          <button
            class="secondary-btn flex-1"
            on:click={cancelForm}
            disabled={saving}>{m.tma_cancel()}</button
          >
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .appointments-container {
    height: 100%;
  }

  .desktop-mode {
    padding-bottom: 30px;
  }

  /* Date Selector Header */
  .date-selector {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    padding: 12px 16px;
    border-radius: var(--radius-pill);
    margin-bottom: 16px;
    border: 1px solid var(--border-subtle);
    box-shadow: var(--shadow-glass);
  }

  .nav-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
    width: 38px;
    height: 38px;
    border-radius: 50%;
    font-size: 16px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s var(--ease-spring);
  }

  .nav-btn:hover {
    background: var(--bg-surface-hover);
    border-color: var(--border-glass);
    color: var(--pastel-rose);
  }

  .nav-btn:active {
    transform: scale(0.92);
  }

  .current-date {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
    text-transform: capitalize;
    letter-spacing: 0.02em;
  }

  .appointments-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  /* Appointment Cards */
  .appt-card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-glass);
    border: 1px solid var(--border-subtle);
    border-left: 4px solid var(--pastel-amber);
    cursor: pointer;
    transition: all 0.25s var(--ease-spring);
    overflow: hidden;
  }

  .appt-card:hover {
    border-color: var(--border-glass);
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.45);
  }

  .appt-card.mobile {
    padding: 0;
  }

  .appt-card.mobile.expanded {
    border-color: rgba(223, 158, 142, 0.35);
    box-shadow:
      0 12px 36px rgba(0, 0, 0, 0.55),
      0 0 20px var(--pastel-rose-dim);
  }

  .appt-card.compact {
    padding: 0;
    margin-bottom: 8px;
    border-radius: var(--radius-md);
  }

  .appt-card.compact .card-main-row {
    padding: 10px 12px;
    gap: 10px;
    align-items: center;
  }

  .appt-card.status-0 {
    border-left-color: var(--pastel-amber);
  }
  .appt-card.status-1 {
    border-left-color: var(--pastel-sage);
  }
  .appt-card.status-2 {
    border-left-color: var(--pastel-coral);
    opacity: 0.65;
  }

  /* Card Main Row */
  .card-main-row {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 14px 16px;
    min-width: 0;
  }

  .card-right {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-shrink: 0;
  }

  .expand-arrow {
    font-size: 20px;
    color: var(--text-muted);
    transition:
      transform 0.25s var(--ease-spring),
      color 0.2s;
    line-height: 1;
    user-select: none;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .expand-arrow.open {
    transform: rotate(90deg);
    color: var(--pastel-rose);
  }

  /* Expanded Section */
  .card-expanded {
    padding: 0 16px 16px;
    animation: fadeIn 0.25s var(--ease-spring);
    min-width: 0;
  }

  .expand-divider {
    height: 1px;
    background: var(--border-subtle);
    margin-bottom: 12px;
  }

  .expand-meta {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px 12px;
    margin-bottom: 12px;
    min-width: 0;
    width: 100%;
  }

  .meta-badge {
    font-size: 12px;
    font-weight: 600;
    padding: 4px 10px;
    border-radius: var(--radius-pill);
    letter-spacing: 0.02em;
    white-space: nowrap;
    flex-shrink: 0;
  }
  .status-badge-0 {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.25);
  }
  .status-badge-1 {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
  }
  .status-badge-2 {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.25);
  }

  .expand-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-left: auto;
  }

  .edit-link {
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 2px 4px;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    transition: opacity 0.2s;
    white-space: nowrap;
  }
  .edit-link:hover {
    opacity: 0.8;
  }

  .cancel-link {
    background: none;
    border: none;
    color: var(--pastel-coral);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 2px 4px;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    transition: opacity 0.2s;
    white-space: nowrap;
    margin-left: auto;
  }
  .cancel-link:hover {
    opacity: 0.8;
  }

  .client-tg-row {
    display: flex;
    align-items: center;
    gap: 8px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 10px 12px;
    margin-bottom: 12px;
    min-width: 0;
    overflow: hidden;
  }

  .client-tg-label {
    font-size: 13px;
    color: var(--text-muted);
    flex-shrink: 0;
  }

  .client-tg-id {
    font-size: 13px;
    font-weight: 600;
    color: var(--pastel-lavender);
    cursor: pointer;
    flex: 1;
    min-width: 0;
    display: flex;
    align-items: center;
    gap: 6px;
    overflow: hidden;
  }
  .client-tg-id:hover {
    color: var(--pastel-rose);
  }

  .client-handle-text {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    flex: 1;
    min-width: 0;
  }

  .tg-chat-link {
    color: var(--pastel-rose);
    text-decoration: underline;
    font-weight: 600;
    transition: opacity 0.2s;
  }
  .tg-chat-link:hover {
    opacity: 0.8;
  }

  .attach-photo-btn {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 12px;
    border-radius: var(--radius-md);
    border: 1.5px dashed rgba(223, 158, 142, 0.4);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s var(--ease-spring);
  }
  .attach-photo-btn:hover {
    background: rgba(223, 158, 142, 0.22);
    border-color: var(--pastel-rose);
  }
  .attach-photo-btn:disabled {
    opacity: 0.5;
    cursor: default;
  }

  .photo-preview-wrap {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .photo-preview {
    width: 100%;
    max-height: 200px;
    object-fit: cover;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
  }
  .change-photo-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-sm);
    padding: 8px;
    font-size: 13px;
    color: var(--text-secondary);
    cursor: pointer;
    transition: all 0.2s;
  }
  .change-photo-btn:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
  }
  .change-photo-btn:disabled {
    opacity: 0.5;
    cursor: default;
  }

  /* Comment UI */
  .appt-comment-section {
    margin-top: 14px;
  }
  .comment-input {
    width: 100%;
    min-height: 70px;
    padding: 10px 14px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
    font-size: 14px;
    resize: vertical;
    margin-bottom: 8px;
    background: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-family: var(--font-family);
    box-sizing: border-box;
    transition: border-color 0.2s;
  }
  .comment-input:focus {
    outline: none;
    border-color: var(--border-active);
  }
  .comment-actions {
    display: flex;
    gap: 8px;
  }
  .btn-save {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 2px 8px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }
  .btn-save:active {
    transform: scale(0.95);
  }

  .btn-cancel {
    background: transparent;
    color: var(--text-secondary);
    border: 1px solid var(--border-subtle);
    padding: 8px 16px;
    border-radius: var(--radius-pill);
    font-size: 13px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .btn-cancel:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
  }

  .comment-display {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    padding: 12px 14px;
    border-radius: var(--radius-md);
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 8px;
  }
  .comment-text {
    font-size: 13px;
    color: var(--text-primary);
    line-height: 1.5;
    white-space: pre-wrap;
    flex: 1;
  }
  .comment-actions-sm {
    display: flex;
    gap: 8px;
  }
  .comment-actions-sm button {
    background: none;
    border: none;
    color: var(--pastel-rose);
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
  }
  .comment-actions-sm button.text-danger {
    color: var(--pastel-coral);
  }
  .btn-add-comment {
    background: none;
    border: none;
    color: var(--text-muted);
    font-size: 13px;
    cursor: pointer;
    padding: 2px 0;
    display: flex;
    align-items: center;
    transition: color 0.2s;
  }
  .btn-add-comment:hover {
    color: var(--pastel-rose);
  }

  .appt-time {
    font-weight: 700;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
    display: flex;
    align-items: baseline;
    gap: 3px;
    white-space: nowrap;
  }
  .mobile .card-main-row .appt-time {
    font-size: 15px;
    min-width: 95px;
    align-items: center;
    gap: 4px;
    flex-shrink: 0;
  }
  .compact .appt-time {
    font-size: 13px;
    min-width: 50px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 1px;
    line-height: 1.2;
    flex-shrink: 0;
  }
  .compact .appt-time .time-start {
    font-size: 13px;
    font-weight: 700;
    color: var(--text-primary);
  }
  .compact .appt-time .time-sep {
    display: none;
  }
  .compact .appt-time .time-end {
    font-size: 11px;
    font-weight: 600;
    color: var(--text-muted);
  }
  .compact .appt-time .time-end::before {
    content: "– ";
  }

  .appt-details {
    flex: 1;
    min-width: 0;
    overflow: hidden;
  }

  .service-name {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .compact .service-name {
    font-size: 13px;
    line-height: 1.3;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .client-name {
    font-size: 13px;
    color: var(--text-secondary);
    margin-top: 2px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .compact .client-name {
    font-size: 12px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  @media (max-width: 375px) {
    .card-main-row {
      padding: 12px 14px;
      gap: 8px;
    }
    .mobile .card-main-row .appt-time {
      font-size: 14px;
      min-width: 85px;
    }
    .service-name {
      font-size: 14px;
    }
    .client-name {
      font-size: 12px;
    }
    .card-expanded {
      padding: 0 14px 14px;
    }
    .expand-meta {
      flex-wrap: wrap;
    }
    .expand-actions {
      width: 100%;
      justify-content: space-between;
      margin-left: 0;
      gap: 8px;
    }
    .edit-link, .cancel-link {
      font-size: 12px;
    }
    .cancel-link {
      margin-left: auto;
    }
  }

  .status-icon {
    font-size: 16px;
    color: var(--text-muted);
  }
  .compact .status-icon {
    font-size: 13px;
  }
  .appt-card.status-0 .status-icon {
    color: var(--pastel-amber);
  }
  .appt-card.status-1 .status-icon {
    color: var(--pastel-sage);
  }
  .appt-card.status-2 .status-icon {
    color: var(--pastel-coral);
  }

  .add-btn {
    position: fixed;
    bottom: 24px;
    right: 20px;
    width: 54px;
    height: 54px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    font-size: 28px;
    line-height: 1;
    box-shadow: 0 8px 24px var(--pastel-rose-glow);
    cursor: pointer;
    z-index: 90;
    transition: all 0.25s var(--ease-spring);
  }
  .add-btn:hover {
    transform: scale(1.06) translateY(-2px);
  }
  .add-btn:active {
    transform: scale(0.94);
  }

  /* Form Container */
  .card {
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-radius: var(--radius-lg);
    border: 1px solid var(--border-subtle);
    padding: 24px;
    box-shadow: var(--shadow-glass);
    max-width: 520px;
    margin: 0 auto;
    animation: fadeIn 0.3s var(--ease-spring);
  }

  .card h2 {
    font-size: 20px;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 20px;
  }

  .form-group {
    margin-bottom: 18px;
  }

  .form-group label {
    display: block;
    font-size: 13px;
    font-weight: 500;
    color: var(--text-secondary);
    margin-bottom: 6px;
  }

  .input {
    width: 100%;
    box-sizing: border-box;
    padding: 12px 16px;
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    font-size: 15px;
    background: var(--bg-surface-elevated);
    color: var(--text-primary);
    font-family: var(--font-family);
    transition: border-color 0.2s;
  }

  .input:focus {
    outline: none;
    border-color: var(--border-active);
  }

  select.input {
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%23a3a9bf' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 14px center;
  }

  .form-actions {
    display: flex;
    flex-direction: column;
    gap: 10px;
    margin-top: 28px;
  }

  .flex-1 {
    flex: 1;
  }

  .primary-btn {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
    border: none;
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    box-shadow: 0 4px 16px var(--pastel-rose-glow);
    transition: all 0.2s var(--ease-spring);
  }
  .primary-btn:active {
    transform: scale(0.96);
  }

  .secondary-btn {
    background: var(--bg-surface-elevated);
    color: var(--text-secondary);
    border: 1px solid var(--border-subtle);
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .secondary-btn:hover {
    color: var(--text-primary);
    border-color: var(--border-glass);
  }
  .secondary-btn:active {
    transform: scale(0.96);
  }

  .danger-btn {
    background: var(--pastel-coral-dim);
    color: var(--pastel-coral);
    border: 1px solid rgba(232, 130, 130, 0.2);
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .danger-btn:active {
    transform: scale(0.96);
  }

  .warning-btn {
    background: var(--pastel-amber-dim);
    color: var(--pastel-amber);
    border: 1px solid rgba(229, 190, 138, 0.3);
    padding: 14px;
    border-radius: var(--radius-pill);
    font-weight: 600;
    font-size: 15px;
    cursor: pointer;
    transition: all 0.2s;
  }
  .warning-btn:active {
    transform: scale(0.96);
  }

  .loading,
  .empty-state {
    text-align: center;
    padding: 50px 20px;
    color: var(--text-secondary);
  }
  .empty-state .icon {
    font-size: 44px;
    margin-bottom: 14px;
  }

  /* Desktop Grid Styles */
  .desktop-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding: 0 4px;
  }

  .week-nav {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .week-label {
    font-weight: 600;
    font-size: 15px;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
  }

  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 8px;
    padding: 2px;
  }

  .day-col {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    display: flex;
    flex-direction: column;
    min-height: 440px;
    cursor: pointer;
    overflow: hidden;
    transition: border-color 0.2s;
  }

  .day-col:hover {
    border-color: var(--border-glass);
  }

  .day-header {
    background: var(--bg-surface-elevated);
    padding: 10px 8px;
    text-align: center;
    border-bottom: 1px solid var(--border-subtle);
  }

  .day-name {
    text-transform: capitalize;
    font-weight: 600;
    color: var(--pastel-rose);
    font-size: 13px;
  }

  .day-date {
    font-size: 12px;
    color: var(--text-secondary);
    margin-top: 2px;
    font-variant-numeric: tabular-nums;
  }

  .day-body {
    flex: 1;
    padding: 8px 6px;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .spinner {
    display: inline-block;
    width: 14px;
    height: 14px;
    border: 2px solid currentColor;
    border-right-color: transparent;
    border-radius: 50%;
    animation: spinSmooth 0.75s linear infinite;
  }

  /* Desktop Cards & Modal Styles */
  .desktop-card {
    transition: all 0.2s var(--ease-spring);
    user-select: none;
  }

  .desktop-card:hover {
    transform: translateY(-2px);
    border-color: var(--border-glass);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
  }

  .desktop-card:active {
    transform: scale(0.98);
  }

  .desktop-card .expand-arrow {
    display: flex;
    align-items: center;
    justify-content: center;
  }

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
    from {
      opacity: 0;
    }
    to {
      opacity: 1;
    }
  }

  .detail-modal-card,
  .form-modal-card {
    background: var(--bg-surface-solid);
    border: 1px solid var(--border-glass);
    padding: 24px;
    border-radius: var(--radius-lg);
    width: 92%;
    max-width: 520px;
    max-height: 88vh;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 18px;
    color: var(--text-primary);
    box-shadow:
      0 24px 64px rgba(0, 0, 0, 0.8),
      0 0 24px rgba(223, 158, 142, 0.15);
    animation: modalPop 0.25s var(--ease-spring);
    box-sizing: border-box;
    margin: auto;
  }

  @keyframes modalPop {
    from {
      opacity: 0;
      transform: scale(0.96);
    }
    to {
      opacity: 1;
      transform: scale(1);
    }
  }

  .detail-modal-header,
  .form-modal-card .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid var(--border-subtle);
    padding-bottom: 14px;
  }

  .form-modal-card .modal-header h2 {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .detail-modal-title-wrap {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }

  .detail-time-badge {
    display: flex;
    align-items: center;
    gap: 6px;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    padding: 6px 12px;
    border-radius: var(--radius-pill);
    font-size: 15px;
    font-weight: 700;
    color: var(--text-primary);
    font-variant-numeric: tabular-nums;
  }

  .detail-date-sub {
    font-size: 13px;
    font-weight: 500;
    color: var(--text-secondary);
  }

  .modal-close-btn {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-muted);
    width: 34px;
    height: 34px;
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

  .detail-modal-body {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .detail-info-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }

  .detail-info-box {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .detail-box-label {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
  }

  .detail-box-value {
    font-size: 15px;
    font-weight: 700;
    color: var(--text-primary);
  }

  .detail-box-value.highlight {
    color: var(--pastel-rose);
  }

  .detail-box-sub {
    font-size: 12px;
    color: var(--text-secondary);
  }

  .detail-client-link {
    display: flex;
    flex-direction: column;
    gap: 2px;
    cursor: pointer;
  }

  .detail-client-link:hover .detail-box-value {
    color: var(--pastel-rose);
    text-decoration: underline;
  }

  .detail-client-handle {
    font-size: 13px;
    color: var(--text-muted);
    font-weight: 500;
  }

  .detail-client-handle-link {
    font-size: 13px;
    color: var(--pastel-lavender);
    text-decoration: none;
    font-weight: 600;
    transition: color 0.2s;
  }
  .detail-client-handle-link:hover {
    color: var(--pastel-rose);
    text-decoration: underline;
  }

  .tg-chat-link {
    color: var(--pastel-lavender);
    text-decoration: none;
    font-weight: 600;
  }
  .tg-chat-link:hover {
    color: var(--pastel-rose);
    text-decoration: underline;
  }

  .detail-section {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .detail-section-label {
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-secondary);
  }

  .detail-photo-card {
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 12px;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .detail-modal-footer {
    display: flex;
    gap: 12px;
    border-top: 1px solid var(--border-subtle);
    padding-top: 16px;
  }

  .client-history-action-row {
    margin-top: 8px;
  }

  .btn-open-history-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.35);
    border-radius: var(--radius-pill);
    padding: 4px 12px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s;
  }

  .btn-open-history-pill:hover {
    background: var(--pastel-rose);
    color: var(--text-inverse);
    border-color: var(--pastel-rose);
  }

  .history-btn {
    background: var(--bg-surface-elevated);
    color: var(--pastel-lavender);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    padding: 10px 16px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    transition: all 0.2s;
  }

  .history-btn:hover {
    background: var(--pastel-lavender-dim);
    border-color: var(--pastel-lavender);
    color: var(--text-primary);
  }

  .btn-history-pill {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.35);
    border-radius: var(--radius-pill);
    padding: 3px 10px;
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
    margin-left: auto;
    transition: all 0.2s;
  }

  .btn-history-pill:hover {
    background: var(--pastel-rose);
    color: var(--text-inverse);
  }
</style>

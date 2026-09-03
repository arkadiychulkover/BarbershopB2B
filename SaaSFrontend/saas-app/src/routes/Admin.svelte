<script>
  import { onMount } from "svelte";
  import AdminLayout from "../components/AdminLayout.svelte";
  import { apiRequest, BASE_URL } from "../lib/api";
  import { authStore } from "../lib/store";
  import { m } from "../lib/paraglide/messages.js";
  import { currentLocale } from "../lib/locale.js";
  import {
    Building2,
    Users,
    UserCheck,
    Calendar,
    DollarSign,
    Search,
    Plus,
    Edit2,
    Trash2,
    CheckCircle2,
    XCircle,
    Clock,
    Shield,
    ExternalLink,
    RefreshCw,
    Filter,
    ChevronRight,
    TrendingUp,
    Send,
    AlertCircle,
    Check,
    FileSpreadsheet,
    Download,
    BarChart3,
    Sparkles,
    CalendarRange,
    User,
    Mail,
    Phone,
    MapPin,
    Bot,
    Lock,
    Globe,
  } from "lucide-svelte";

  function formatDateInput(d) {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  let activeSection = "overview";

  // Platform Metrics
  let stats = {
    totalOwners: 0,
    activeOwners: 0,
    inactiveOwners: 0,
    totalMasters: 0,
    activeMasters: 0,
    totalClients: 0,
    totalAppointments: 0,
    completedAppointments: 0,
    totalRevenue: 0,
  };

  // Admin Chart Analytics
  let adminChartData = {
    points: [],
    totalVisits: 0,
    totalRevenue: 0,
    averageDailyVisits: 0,
    averageDailyRevenue: 0,
    peakDate: "—",
    peakRevenue: 0,
  };
  let chartPeriod = "30d"; // '7d' | '30d' | '90d' | 'year' | 'custom'
  let adminCustomStartDate = formatDateInput(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
  );
  let adminCustomEndDate = formatDateInput(new Date());
  let chartOwnerFilter = "";
  let isChartLoading = false;
  let hoveredChartPoint = null;

  // Data collections
  let owners = [];
  let masters = [];
  let clients = [];
  let appointments = [];
  let admins = [];

  // Search & Filters
  let ownerSearch = "";
  let ownerStatusFilter = "";
  let masterSearch = "";
  let clientSearch = "";
  let appointmentStatusFilter = "";

  // Loading states
  let isStatsLoading = true;
  let isOwnersLoading = false;
  let isMastersLoading = false;
  let isClientsLoading = false;
  let isAppointmentsLoading = false;
  let isAdminsLoading = false;
  let isExporting = false;
  let actionLoading = false;

  // Alerts / Notifications
  let successMsg = "";
  let errorMsg = "";

  // Modals state
  let showExtendModal = false;
  let selectedOwnerForSub = null;
  let extendDaysInput = 30;

  let showEditOwnerModal = false;
  let editingOwner = null;

  let showEditMasterModal = false;
  let editingMaster = null;

  let showCreateAdminModal = false;
  let newAdminEmail = "";
  let newAdminPassword = "";

  let showCreateOwnerModal = false;
  let newOwner = {
    email: "",
    password: "",
    subscriptionDays: 30,
    ownerName: "",
    phoneNumber: "",
    telegramId: "",
    barbershopName: "",
    barbershopAddress: "",
    barbershopDescription: "",
    botToken: "",
    botUsername: "",
    timeZone: "Europe/Kyiv"
  };

  onMount(async () => {
    await fetchStats();
    await fetchOwners();
    await fetchAdminChartData();
  });

  function showSuccess(msg) {
    successMsg = msg;
    errorMsg = "";
    setTimeout(() => {
      successMsg = "";
    }, 4000);
  }

  function showError(msg) {
    errorMsg = msg;
    successMsg = "";
    setTimeout(() => {
      errorMsg = "";
    }, 5000);
  }

  // ─── Data Fetching ──────────────────────────────────────────────────────────-

  async function fetchStats() {
    isStatsLoading = true;
    try {
      stats = await apiRequest("/api/Admin/stats");
    } catch (e) {
      console.error(e);
    } finally {
      isStatsLoading = false;
    }
  }

  async function fetchAdminChartData() {
    isChartLoading = true;
    try {
      let url = `/api/Admin/chart-analytics?period=${chartPeriod}`;
      if (chartPeriod === "custom") {
        url += `&startDate=${adminCustomStartDate}&endDate=${adminCustomEndDate}`;
      }
      if (chartOwnerFilter) url += `&ownerId=${chartOwnerFilter}`;
      adminChartData = await apiRequest(url);
    } catch (e) {
      console.error("Failed to load admin chart data:", e);
    } finally {
      isChartLoading = false;
    }
  }

  async function setChartPeriod(period) {
    chartPeriod = period;
    await fetchAdminChartData();
  }

  async function fetchOwners() {
    isOwnersLoading = true;
    try {
      let url = "/api/Admin/owners?";
      if (ownerSearch) url += `search=${encodeURIComponent(ownerSearch)}&`;
      if (ownerStatusFilter)
        url += `status=${encodeURIComponent(ownerStatusFilter)}&`;
      owners = await apiRequest(url);
    } catch (e) {
      showError(m.admin_err_load_owners() + e.message);
    } finally {
      isOwnersLoading = false;
    }
  }

  async function fetchMasters() {
    isMastersLoading = true;
    try {
      let url = "/api/Admin/masters?";
      if (masterSearch) url += `search=${encodeURIComponent(masterSearch)}&`;
      masters = await apiRequest(url);
    } catch (e) {
      showError(m.admin_err_load_masters() + e.message);
    } finally {
      isMastersLoading = false;
    }
  }

  async function fetchClients() {
    isClientsLoading = true;
    try {
      let url = "/api/Admin/clients?";
      if (clientSearch) url += `search=${encodeURIComponent(clientSearch)}&`;
      clients = await apiRequest(url);
    } catch (e) {
      showError(m.admin_err_load_clients() + e.message);
    } finally {
      isClientsLoading = false;
    }
  }

  async function fetchAppointments() {
    isAppointmentsLoading = true;
    try {
      let url = "/api/Admin/appointments?limit=150&";
      if (appointmentStatusFilter !== "")
        url += `status=${encodeURIComponent(appointmentStatusFilter)}&`;
      appointments = await apiRequest(url);
    } catch (e) {
      showError(m.admin_err_load_appts() + e.message);
    } finally {
      isAppointmentsLoading = false;
    }
  }

  async function fetchAdmins() {
    isAdminsLoading = true;
    try {
      admins = await apiRequest("/api/Admin/admins");
    } catch (e) {
      showError(m.admin_err_load_admins() + e.message);
    } finally {
      isAdminsLoading = false;
    }
  }

  function handleSectionChange(section) {
    activeSection = section;
    if (section === "overview") {
      fetchStats();
      fetchOwners();
      fetchAdminChartData();
    } else if (section === "owners") fetchOwners();
    else if (section === "masters") fetchMasters();
    else if (section === "clients") fetchClients();
    else if (section === "appointments") fetchAppointments();
    else if (section === "admins") fetchAdmins();
  }

  // ─── Excel Export ───────────────────────────────────────────────────────────

  async function exportAllClientsToExcel(ownerId = null) {
    isExporting = true;
    try {
      const token = $authStore.token;
      let url = "/api/Admin/export/clients";
      if (ownerId) url += `?ownerId=${ownerId}`;

      const res = await fetch(`${BASE_URL}${url}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!res.ok) throw new Error(m.admin_err_excel_gen());

      const blob = await res.blob();
      const blobUrl = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = blobUrl;
      a.download = `platform_clients_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(blobUrl);
      document.body.removeChild(a);

      showSuccess(m.admin_success_excel_export());
    } catch (e) {
      showError(e.message || m.admin_err_excel_export());
    } finally {
      isExporting = false;
    }
  }

  // ─── Owner Actions ──────────────────────────────────────────────────────────

  async function toggleOwnerSubscription(owner) {
    actionLoading = true;
    try {
      const isCurrentlyActive =
        owner.status === "Active" &&
        !owner.isBlocked &&
        new Date(owner.nextPayment) > new Date();
      await apiRequest(`/api/Admin/owners/${owner.id}/subscription`, {
        method: "PUT",
        body: JSON.stringify({ isActive: !isCurrentlyActive }),
      });
      showSuccess(
        `${m.admin_sub_status_changed()}: ${owner.barbershopName}`,
      );
      await fetchOwners();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_change_sub() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  function openExtendModal(owner) {
    selectedOwnerForSub = owner;
    extendDaysInput = 30;
    showExtendModal = true;
  }

  async function submitExtendSubscription() {
    if (!selectedOwnerForSub) return;
    actionLoading = true;
    try {
      await apiRequest(
        `/api/Admin/owners/${selectedOwnerForSub.id}/subscription`,
        {
          method: "PUT",
          body: JSON.stringify({ extendDays: parseInt(extendDaysInput, 10) }),
        },
      );
      showSuccess(m.admin_sub_extended_days({ days: extendDaysInput }));
      showExtendModal = false;
      await fetchOwners();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_extend_sub() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  function openEditOwnerModal(owner) {
    editingOwner = { ...owner };
    showEditOwnerModal = true;
  }

  async function submitEditOwner() {
    if (!editingOwner) return;

    if (editingOwner.phoneNumber) {
      const cleaned = editingOwner.phoneNumber
        .trim()
        .replace(/[\s\-\(\)]/g, "");
      const phoneRegex = /^\+[0-9]{1,3}[0-9]{9}$/;
      if (!phoneRegex.test(cleaned)) {
        showError(
          m.admin_phone_format_error(),
        );
        return;
      }
      editingOwner.phoneNumber = cleaned;
    }

    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/owners/${editingOwner.id}`, {
        method: "PUT",
        body: JSON.stringify({
          ownerName: editingOwner.ownerName,
          phoneNumber: editingOwner.phoneNumber,
          barbershopName: editingOwner.barbershopName,
          barbershopAddress: editingOwner.barbershopAddress,
          barbershopDescription: editingOwner.barbershopDescription,
          botToken: editingOwner.botToken,
          botUsername: editingOwner.botUsername,
          newPassword: editingOwner.newPassword?.trim()
            ? editingOwner.newPassword.trim()
            : undefined,
        }),
      });
      showSuccess(m.admin_owner_data_updated());
      showEditOwnerModal = false;
      await fetchOwners();
    } catch (e) {
      showError(m.admin_err_update_owner() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  async function sendOwnerResetPasswordEmail(email) {
    if (!email) return;
    actionLoading = true;
    try {
      await apiRequest("/api/Regestration/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email: email.trim() }),
      });
      showSuccess(`${m.admin_reset_link_sent()} ${email}`);
    } catch (e) {
      showError(m.admin_err_send_reset() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  async function deleteOwner(owner) {
    if (
      !confirm(
        m.admin_delete_owner_warning(),
      )
    ) {
      return;
    }
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/owners/${owner.id}`, { method: "DELETE" });
      showSuccess(`${m.admin_owner_deleted_msg()}: ${owner.barbershopName}`);
      await fetchOwners();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_delete() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Master Actions ─────────────────────────────────────────────────────────

  function openEditMasterModal(master) {
    editingMaster = { ...master };
    showEditMasterModal = true;
  }

  async function submitEditMaster() {
    if (!editingMaster) return;
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/masters/${editingMaster.id}`, {
        method: "PUT",
        body: JSON.stringify({
          name: editingMaster.name,
          description: editingMaster.description,
          telegramId: editingMaster.telegramId,
          telegramUsername: editingMaster.telegramUsername,
          isActive: editingMaster.isActive,
        }),
      });
      showSuccess(m.admin_master_data_updated());
      showEditMasterModal = false;
      await fetchMasters();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_update_master() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  async function toggleMasterActive(master) {
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/masters/${master.id}`, {
        method: "PUT",
        body: JSON.stringify({ isActive: !master.isActive }),
      });
      showSuccess(`${m.admin_master_status_changed()}: ${master.name}`);
      await fetchMasters();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_change_master_status() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  async function deleteMaster(master) {
    if (!confirm(`${m.admin_delete_master_prompt()} ${master.name}`)) return;
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/masters/${master.id}`, { method: "DELETE" });
      showSuccess(`${m.admin_master_deleted_msg()}: ${master.name}`);
      await fetchMasters();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_delete() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Client Actions ─────────────────────────────────────────────────────────

  async function deleteClient(client) {
    if (!confirm(`${m.admin_delete_client_prompt()} ${client.name}`)) return;
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/clients/${client.id}`, { method: "DELETE" });
      showSuccess(`${m.admin_client_deleted_msg()}: ${client.name}`);
      await fetchClients();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_delete() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Appointment Actions ───────────────────────────────────────────────────

  async function deleteAppointment(appt) {
    if (
      !confirm(
        `${m.admin_delete_appt_prompt()}: ${appt.clientName} -> ${appt.masterName}`,
      )
    )
      return;
    actionLoading = true;
    try {
      await apiRequest(`/api/Admin/appointments/${appt.id}`, {
        method: "DELETE",
      });
      showSuccess(m.admin_appt_deleted_msg());
      await fetchAppointments();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_delete_appt() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Admin Creation ────────────────────────────────────────────────────────

  async function submitCreateAdmin() {
    if (!newAdminEmail || !newAdminPassword) {
      showError(m.admin_fill_email_pwd());
      return;
    }
    actionLoading = true;
    try {
      await apiRequest("/api/Admin/admins", {
        method: "POST",
        body: JSON.stringify({
          email: newAdminEmail,
          password: newAdminPassword,
        }),
      });
      showSuccess(m.admin_new_admin_registered());
      newAdminEmail = "";
      newAdminPassword = "";
      showCreateAdminModal = false;
      await fetchAdmins();
    } catch (e) {
      showError(m.admin_err_create_admin() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Owner Creation ────────────────────────────────────────────────────────

  function resetNewOwnerForm() {
    newOwner = {
      email: "",
      password: "",
      subscriptionDays: 30,
      ownerName: "",
      phoneNumber: "",
      telegramId: "",
      barbershopName: "",
      barbershopAddress: "",
      barbershopDescription: "",
      botToken: "",
      botUsername: "",
      timeZone: "Europe/Kyiv"
    };
  }

  async function submitCreateOwner() {
    if (!newOwner.email || !newOwner.password) {
      showError(m.admin_fill_email_pwd_min6());
      return;
    }
    const days = parseInt(newOwner.subscriptionDays, 10);
    if (isNaN(days) || days < 1) {
      showError(m.admin_fill_valid_sub_days());
      return;
    }

    if (newOwner.phoneNumber) {
      const cleaned = newOwner.phoneNumber.trim().replace(/[\s\-\(\)]/g, "");
      const phoneRegex = /^\+[0-9]{1,3}[0-9]{9}$/;
      if (!phoneRegex.test(cleaned)) {
        showError(
          m.admin_phone_format_error(),
        );
        return;
      }
      newOwner.phoneNumber = cleaned;
    }

    actionLoading = true;
    try {
      await apiRequest("/api/Admin/owners", {
        method: "POST",
        body: JSON.stringify({
          email: newOwner.email.trim(),
          password: newOwner.password,
          subscriptionDays: days,
          ownerName: newOwner.ownerName?.trim() || undefined,
          phoneNumber: newOwner.phoneNumber?.trim() || undefined,
          telegramId: newOwner.telegramId?.trim() || undefined,
          barbershopName: newOwner.barbershopName?.trim() || undefined,
          barbershopAddress: newOwner.barbershopAddress?.trim() || undefined,
          barbershopDescription: newOwner.barbershopDescription?.trim() || undefined,
          botToken: newOwner.botToken?.trim() || undefined,
          botUsername: newOwner.botUsername?.trim()?.replace(/^@/, '') || undefined,
          timeZone: newOwner.timeZone || "Europe/Kyiv",
        }),
      });
      showSuccess(`${m.admin_salon_created_msg()}: ${newOwner.email}`);
      resetNewOwnerForm();
      showCreateOwnerModal = false;
      await fetchOwners();
      await fetchStats();
    } catch (e) {
      showError(m.admin_err_create_salon() + e.message);
    } finally {
      actionLoading = false;
    }
  }

  // ─── Chart SVG Math ────────────────────────────────────────────────────────
  const svgWidth = 860;
  const svgHeight = 240;
  const padding = { top: 25, right: 30, bottom: 40, left: 65 };

  $: chartWidth = svgWidth - padding.left - padding.right;
  $: chartHeight = svgHeight - padding.top - padding.bottom;

  $: maxRev = Math.max(
    ...(adminChartData.points || []).map((p) => p.revenue),
    100,
  );
  $: maxVis = Math.max(
    ...(adminChartData.points || []).map((p) => p.visits),
    5,
  );

  $: adminRevPoints = (adminChartData.points || []).map((p, i) => {
    const x =
      padding.left +
      (i / Math.max(adminChartData.points.length - 1, 1)) * chartWidth;
    const y = padding.top + chartHeight - (p.revenue / maxRev) * chartHeight;
    return { ...p, x, y };
  });

  $: adminVisPoints = (adminChartData.points || []).map((p, i) => {
    const x =
      padding.left +
      (i / Math.max(adminChartData.points.length - 1, 1)) * chartWidth;
    const y = padding.top + chartHeight - (p.visits / maxVis) * chartHeight;
    return { ...p, x, y };
  });

  function makeLinePath(points) {
    if (!points || points.length === 0) return "";
    return points.reduce((acc, p, i) => {
      if (i === 0) return `M ${p.x} ${p.y}`;
      const prev = points[i - 1];
      const cx1 = prev.x + (p.x - prev.x) / 2;
      const cy1 = prev.y;
      const cx2 = prev.x + (p.x - prev.x) / 2;
      const cy2 = p.y;
      return `${acc} C ${cx1} ${cy1}, ${cx2} ${cy2}, ${p.x} ${p.y}`;
    }, "");
  }

  function makeAreaPath(points) {
    if (!points || points.length === 0) return "";
    const line = makeLinePath(points);
    const lastX = points[points.length - 1].x;
    const firstX = points[0].x;
    const bottomY = padding.top + chartHeight;
    return `${line} L ${lastX} ${bottomY} L ${firstX} ${bottomY} Z`;
  }

  $: adminRevLine = makeLinePath(adminRevPoints);
  $: adminRevArea = makeAreaPath(adminRevPoints);

  $: adminVisLine = makeLinePath(adminVisPoints);
  $: adminVisArea = makeAreaPath(adminVisPoints);

  // ─── Helpers ───────────────────────────────────────────────────────────────

  function formatDate(d) {
    if (!d) return "—";
    try {
      const date = new Date(d);
      return date.toLocaleDateString("ru-RU", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return "—";
    }
  }

  function formatDateTime(d) {
    if (!d) return "—";
    try {
      const date = new Date(d);
      return date.toLocaleDateString("ru-RU", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return "—";
    }
  }

  function formatCurrency(num) {
    return (num || 0).toLocaleString("ru-RU") + " ₴";
  }

  function isOwnerSubActive(owner) {
    return (
      owner.status === "Active" &&
      !owner.isBlocked &&
      new Date(owner.nextPayment) > new Date()
    );
  }

  function getStatusLabel(status) {
    if (status === 0) return m.schedule_status_pending();
    if (status === 1) return m.schedule_status_completed();
    if (status === 2) return m.schedule_status_cancelled();
    return m.common_unknown();
  }
</script>

<AdminLayout {activeSection} onSectionChange={handleSectionChange}>
  <!-- Notification Toast Alerts -->
  {#if successMsg}
    <div class="toast toast-success">
      <CheckCircle2 size={18} />
      <span>{successMsg}</span>
    </div>
  {/if}

  {#if errorMsg}
    <div class="toast toast-danger">
      <AlertCircle size={18} />
      <span>{errorMsg}</span>
    </div>
  {/if}

  <!-- Header section -->
  <div class="page-header">
    <div>
      <h1>
        {#if activeSection === "overview"}{m.admin_tab_overview()}{/if}
        {#if activeSection === "owners"}{m.admin_tab_owners()}{/if}
        {#if activeSection === "masters"}{m.admin_tab_masters()}{/if}
        {#if activeSection === "clients"}{m.admin_header_clients_base()}{/if}
        {#if activeSection === "appointments"}{m.admin_header_all_appts()}{/if}
        {#if activeSection === "admins"}{m.admin_header_admins()}{/if}
      </h1>
      <p class="subtitle">
        {#if activeSection === "overview"}{m.admin_header_overview_desc()}{/if}
        {#if activeSection === "owners"}{m.admin_header_owners_desc()}{/if}
        {#if activeSection === "masters"}{m.admin_header_masters_desc()}{/if}
        {#if activeSection === "clients"}{m.admin_header_clients_desc()}{/if}
        {#if activeSection === "appointments"}{m.admin_header_appts_desc()}{/if}
        {#if activeSection === "admins"}{m.admin_header_admins_desc()}{/if}
      </p>
    </div>

    <div class="header-actions">
      <!-- Excel Export Button for Admin -->
      <button
        class="btn btn-secondary"
        on:click={() => exportAllClientsToExcel()}
        disabled={isExporting}
        title={m.admin_export_all_clients_tooltip()}
      >
        <FileSpreadsheet size={16} class="text-rose" />
        <span
          >{isExporting
            ? m.common_loading() : m.admin_export_clients_btn()}</span
        >
      </button>

      {#if activeSection === "owners"}
        <button
          class="btn btn-primary"
          on:click={() => (showCreateOwnerModal = true)}
        >
          <Plus size={16} />
          <span>{m.admin_create_owner_btn()}</span>
        </button>
      {:else if activeSection === "admins"}
        <button
          class="btn btn-primary"
          on:click={() => (showCreateAdminModal = true)}
        >
          <Plus size={16} />
          <span>{m.admin_add_admin_btn()}</span>
        </button>
      {/if}
      <button
        class="btn btn-secondary"
        on:click={() => handleSectionChange(activeSection)}
        title={m.common_refresh()}
      >
        <RefreshCw size={16} />
        <span>{m.common_refresh()}</span>
      </button>
    </div>
  </div>

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 1. OVERVIEW & METRICS                                                     -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "overview"}
    <div class="metrics-grid">
      <div class="metric-card glow-rose">
        <div class="metric-icon-wrap rose">
          <Building2 size={22} />
        </div>
        <div class="metric-content">
          <span class="metric-label">{m.admin_total_salons()}</span>
          <div class="metric-value">{stats.totalOwners}</div>
          <div class="metric-sub">
            <span class="active-pill">{m.admin_active_count({ count: stats.activeOwners })}</span>
            <span class="inactive-pill">{m.admin_inactive_count({ count: stats.inactiveOwners })}</span>
          </div>
        </div>
      </div>

      <div class="metric-card glow-sage">
        <div class="metric-icon-wrap sage">
          <DollarSign size={22} />
        </div>
        <div class="metric-content">
          <span class="metric-label">{m.admin_platform_turnover()}</span>
          <div class="metric-value">{formatCurrency(stats.totalRevenue)}</div>
          <div class="metric-sub">
            <span>{m.admin_completed_visits({ count: stats.completedAppointments })}</span>
          </div>
        </div>
      </div>

      <div class="metric-card glow-lavender">
        <div class="metric-icon-wrap lavender">
          <UserCheck size={22} />
        </div>
        <div class="metric-content">
          <span class="metric-label">{m.admin_barbers_count()}</span>
          <div class="metric-value">{stats.totalMasters}</div>
          <div class="metric-sub">
            <span class="active-pill">{m.admin_active_count({ count: stats.activeMasters })}</span>
          </div>
        </div>
      </div>

      <div class="metric-card glow-coral">
        <div class="metric-icon-wrap coral">
          <Users size={22} />
        </div>
        <div class="metric-content">
          <span class="metric-label">{m.admin_clients_telegram()}</span>
          <div class="metric-value">{stats.totalClients}</div>
          <div class="metric-sub">
            <span>{m.admin_total_records({ count: stats.totalAppointments })}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ════════════════════════════════════════════════════════════════════════ -->
    <!-- INTERACTIVE CHARTS FOR SUPERADMIN                                        -->
    <!-- ════════════════════════════════════════════════════════════════════════ -->
    <div class="chart-controls-panel card mb-4">
      <div class="chart-panel-left">
        <div class="chart-panel-title">
          <BarChart3 size={18} class="text-rose" />
          <span>{m.admin_global_dynamics()}</span>
        </div>

        <!-- Salon Filter -->
        <div class="filter-select-wrap chart-select">
          <Building2 size={15} />
          <select bind:value={chartOwnerFilter} on:change={fetchAdminChartData}>
            <option value="">{m.admin_all_salons_option()}</option>
            {#each owners as o}
              <option value={o.id}>{o.barbershopName} ({o.ownerName})</option>
            {/each}
          </select>
        </div>
      </div>

      <!-- Period Tabs -->
      <div class="period-tabs">
        <button
          class="period-tab"
          class:active={chartPeriod === "7d"}
          on:click={() => setChartPeriod("7d")}
        >
          <span>{m.stats_range_7d()}</span>
        </button>
        <button
          class="period-tab"
          class:active={chartPeriod === "30d"}
          on:click={() => setChartPeriod("30d")}
        >
          <span>{m.stats_range_30d()}</span>
        </button>
        <button
          class="period-tab"
          class:active={chartPeriod === "90d"}
          on:click={() => setChartPeriod("90d")}
        >
          <span>{m.stats_range_3m()}</span>
        </button>
        <button
          class="period-tab"
          class:active={chartPeriod === "year"}
          on:click={() => setChartPeriod("year")}
        >
          <span>{m.stats_range_12m()}</span>
        </button>
        <button
          class="period-tab"
          class:active={chartPeriod === "custom"}
          on:click={() => setChartPeriod("custom")}
        >
          <CalendarRange size={13} />
          <span>{m.stats_range_custom()}</span>
        </button>
      </div>
    </div>

    <!-- Custom Date Range Bar for Admin -->
    {#if chartPeriod === "custom"}
      <div class="custom-range-card card mb-4">
        <div class="custom-range-inner">
          <div class="custom-range-title">
            <CalendarRange size={18} class="text-rose" />
            <span>{m.admin_custom_analytics_hint()}</span>
          </div>

          <div class="custom-range-inputs">
            <div class="date-input-group">
              <label for="adminCustomStart">{m.stats_from()}</label>
              <input
                id="adminCustomStart"
                type="date"
                class="input date-field"
                bind:value={adminCustomStartDate}
              />
            </div>

            <div class="date-input-group">
              <label for="adminCustomEnd">{m.stats_to()}</label>
              <input
                id="adminCustomEnd"
                type="date"
                class="input date-field"
                bind:value={adminCustomEndDate}
              />
            </div>

            <button
              class="btn btn-primary btn-sm"
              on:click={fetchAdminChartData}
              disabled={isChartLoading}
            >
              <span>{isChartLoading ? m.common_loading() : m.stats_apply()}</span>
            </button>
          </div>
        </div>
      </div>
    {/if}

    <!-- Chart 1: Platform Gross Turnover -->
    <div class="section-card mb-4">
      <div class="section-header">
        <div class="section-title-wrap">
          <TrendingUp size={20} class="text-rose" />
          <div>
            <h2>{m.admin_turnover_chart_title()}</h2>
            <p class="section-desc">
              {m.admin_turnover_chart_desc()}
            </p>
          </div>
        </div>
        <div class="chart-badge-tag rose">
          {m.admin_volume()} {formatCurrency(adminChartData.totalRevenue)}
        </div>
      </div>

      <div class="svg-chart-container">
        <svg viewBox="0 0 {svgWidth} {svgHeight}" class="chart-svg">
          <defs>
            <linearGradient
              id="adminRoseGrad"
              x1="0%"
              y1="0%"
              x2="0%"
              y2="100%"
            >
              <stop offset="0%" stop-color="#DF9E8E" stop-opacity="0.35" />
              <stop offset="100%" stop-color="#DF9E8E" stop-opacity="0.0" />
            </linearGradient>
          </defs>

          {#each [0, 0.25, 0.5, 0.75, 1] as fraction}
            {@const yPos = padding.top + chartHeight * (1 - fraction)}
            <line
              x1={padding.left}
              y1={yPos}
              x2={svgWidth - padding.right}
              y2={yPos}
              stroke="rgba(255, 255, 255, 0.05)"
              stroke-dasharray="4 4"
            />
            <text
              x={padding.left - 10}
              y={yPos + 4}
              class="axis-text"
              text-anchor="end"
            >
              {Math.round(maxRev * fraction)} ₴
            </text>
          {/each}

          {#if adminRevArea}
            <path d={adminRevArea} fill="url(#adminRoseGrad)" />
          {/if}

          {#if adminRevLine}
            <path
              d={adminRevLine}
              fill="none"
              stroke="var(--pastel-rose)"
              stroke-width="3"
              stroke-linecap="round"
            />
          {/if}

          {#each adminRevPoints as pt, i}
            {#if adminChartData.points.length <= 15 || i % Math.ceil(adminChartData.points.length / 10) === 0 || i === adminChartData.points.length - 1}
              <text
                x={pt.x}
                y={svgHeight - 12}
                class="axis-text"
                text-anchor="middle">{pt.label}</text
              >
            {/if}
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <circle
              cx={pt.x}
              cy={pt.y}
              r={hoveredChartPoint?.fullDate === pt.fullDate &&
              hoveredChartPoint?.type === "revenue"
                ? 6
                : 4}
              fill="var(--bg-surface)"
              stroke="var(--pastel-rose)"
              stroke-width="2.5"
              class="chart-point"
              on:mouseenter={() =>
                (hoveredChartPoint = { ...pt, type: "revenue" })}
              on:mouseleave={() => (hoveredChartPoint = null)}
            />
          {/each}

          {#if hoveredChartPoint && hoveredChartPoint.type === "revenue"}
            <g
              transform="translate({hoveredChartPoint.x}, {hoveredChartPoint.y -
                12})"
            >
              <rect
                x="-65"
                y="-42"
                width="130"
                height="38"
                rx="6"
                fill="#1E293B"
                stroke="rgba(223, 158, 142, 0.4)"
                filter="drop-shadow(0 4px 12px rgba(0,0,0,0.5))"
              />
              <text
                x="0"
                y="-24"
                text-anchor="middle"
                fill="#94A3B8"
                font-size="10"
                font-weight="600">{hoveredChartPoint.label}</text
              >
              <text
                x="0"
                y="-10"
                text-anchor="middle"
                fill="#DF9E8E"
                font-size="12"
                font-weight="700"
                >{formatCurrency(hoveredChartPoint.revenue)}</text
              >
            </g>
          {/if}
        </svg>
      </div>
    </div>

    <!-- Chart 2: Client Visits Over Time -->
    <div class="section-card mb-4">
      <div class="section-header">
        <div class="section-title-wrap">
          <Users size={20} class="text-sage" />
          <div>
            <h2>{m.admin_visits_chart_title()}</h2>
            <p class="section-desc">
              {m.admin_visits_chart_desc()}
            </p>
          </div>
        </div>
        <div class="chart-badge-tag sage">
          {m.admin_total_visits_count()} {adminChartData.totalVisits}
        </div>
      </div>

      <div class="svg-chart-container">
        <svg viewBox="0 0 {svgWidth} {svgHeight}" class="chart-svg">
          <defs>
            <linearGradient
              id="adminSageGrad"
              x1="0%"
              y1="0%"
              x2="0%"
              y2="100%"
            >
              <stop offset="0%" stop-color="#A8C69B" stop-opacity="0.35" />
              <stop offset="100%" stop-color="#A8C69B" stop-opacity="0.0" />
            </linearGradient>
          </defs>

          {#each [0, 0.25, 0.5, 0.75, 1] as fraction}
            {@const yPos = padding.top + chartHeight * (1 - fraction)}
            <line
              x1={padding.left}
              y1={yPos}
              x2={svgWidth - padding.right}
              y2={yPos}
              stroke="rgba(255, 255, 255, 0.05)"
              stroke-dasharray="4 4"
            />
            <text
              x={padding.left - 10}
              y={yPos + 4}
              class="axis-text"
              text-anchor="end"
            >
              {Math.round(maxVis * fraction)}
            </text>
          {/each}

          {#if adminVisArea}
            <path d={adminVisArea} fill="url(#adminSageGrad)" />
          {/if}

          {#if adminVisLine}
            <path
              d={adminVisLine}
              fill="none"
              stroke="var(--pastel-sage)"
              stroke-width="3"
              stroke-linecap="round"
            />
          {/if}

          {#each adminVisPoints as pt, i}
            {#if adminChartData.points.length <= 15 || i % Math.ceil(adminChartData.points.length / 10) === 0 || i === adminChartData.points.length - 1}
              <text
                x={pt.x}
                y={svgHeight - 12}
                class="axis-text"
                text-anchor="middle">{pt.label}</text
              >
            {/if}
            <!-- svelte-ignore a11y-no-static-element-interactions -->
            <circle
              cx={pt.x}
              cy={pt.y}
              r={hoveredChartPoint?.fullDate === pt.fullDate &&
              hoveredChartPoint?.type === "visits"
                ? 6
                : 4}
              fill="var(--bg-surface)"
              stroke="var(--pastel-sage)"
              stroke-width="2.5"
              class="chart-point"
              on:mouseenter={() =>
                (hoveredChartPoint = { ...pt, type: "visits" })}
              on:mouseleave={() => (hoveredChartPoint = null)}
            />
          {/each}

          {#if hoveredChartPoint && hoveredChartPoint.type === "visits"}
            <g
              transform="translate({hoveredChartPoint.x}, {hoveredChartPoint.y -
                12})"
            >
              <rect
                x="-60"
                y="-42"
                width="120"
                height="38"
                rx="6"
                fill="#1E293B"
                stroke="rgba(168, 198, 155, 0.4)"
                filter="drop-shadow(0 4px 12px rgba(0,0,0,0.5))"
              />
              <text
                x="0"
                y="-24"
                text-anchor="middle"
                fill="#94A3B8"
                font-size="10"
                font-weight="600">{hoveredChartPoint.label}</text
              >
              <text
                x="0"
                y="-10"
                text-anchor="middle"
                fill="#A8C69B"
                font-size="12"
                font-weight="700">{hoveredChartPoint.visits} {m.stats_visits_unit()}</text
              >
            </g>
          {/if}
        </svg>
      </div>
    </div>

    <!-- Recent Owners Preview -->
    <div class="section-card mt-4">
      <div class="section-header">
        <div class="section-title-wrap">
          <Building2 size={20} />
          <h2>{m.admin_recently_added_salons()}</h2>
        </div>
        <button
          class="btn btn-sm btn-secondary"
          on:click={() => handleSectionChange("owners")}
        >
          {m.admin_all_salons_count({ count: owners.length })}
        </button>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>{m.admin_col_salon()}</th>
              <th>{m.admin_col_owner()}</th>
              <th>{m.admin_col_sub_until()}</th>
              <th>{m.admin_col_status()}</th>
              <th>{m.admin_col_barbers()}</th>
              <th>{m.admin_col_turnover()}</th>
              <th>{m.admin_col_actions()}</th>
            </tr>
          </thead>
          <tbody>
            {#each owners.slice(0, 5) as owner}
              <tr>
                <td>
                  <div class="table-cell-bold">{owner.barbershopName}</div>
                  <div class="table-cell-sub">
                    @{owner.botUsername || m.admin_bot_not_specified()}
                  </div>
                </td>
                <td>
                  <div>{owner.ownerName}</div>
                  <div class="table-cell-sub">{owner.email}</div>
                </td>
                <td>
                  <div class="date-cell">{formatDate(owner.nextPayment)}</div>
                </td>
                <td>
                  {#if isOwnerSubActive(owner)}
                    <span class="badge badge-active">{m.admin_sub_active()}</span>
                  {:else}
                    <span class="badge badge-inactive">{m.admin_sub_inactive()}</span>
                  {/if}
                </td>
                <td>{owner.mastersCount}</td>
                <td>{formatCurrency(owner.turnover)}</td>
                <td>
                  <button
                    class="btn-icon"
                    on:click={() => openExtendModal(owner)}
                    title={m.admin_extend()}
                  >
                    <Clock size={15} />
                  </button>
                  <button
                    class="btn-icon"
                    on:click={() => openEditOwnerModal(owner)}
                    title={m.admin_edit_tooltip()}
                  >
                    <Edit2 size={15} />
                  </button>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="7" class="text-center py-4 text-muted"
                  >{m.admin_no_salons_connected()}</td
                >
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 2. OWNERS / SALONS MANAGEMENT                                             -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "owners"}
    <div class="filter-bar">
      <div class="search-input-wrap">
        <Search size={17} class="search-icon" />
        <input
          type="text"
          placeholder={m.admin_search_owners()}
          bind:value={ownerSearch}
          on:input={() => fetchOwners()}
        />
      </div>

      <div class="filter-select-wrap">
        <Filter size={16} />
        <select bind:value={ownerStatusFilter} on:change={() => fetchOwners()}>
          <option value="">{m.admin_filter_all_salons_status()}</option>
          <option value="Active">{m.admin_filter_active_only()}</option>
          <option value="Frozen">{m.admin_filter_frozen_only()}</option>
          <option value="Pending">{m.admin_filter_pending_only()}</option>
        </select>
      </div>

      <button
        class="btn btn-primary"
        on:click={() => (showCreateOwnerModal = true)}
      >
        <Plus size={16} />
        <span>{m.admin_create_owner_btn()}</span>
      </button>
    </div>

    <div class="section-card">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>{m.admin_th_salon_bot()}</th>
              <th>{m.admin_th_owner_contacts()}</th>
              <th>{m.admin_th_subscription()}</th>
              <th>{m.admin_th_metrics()}</th>
              <th>{m.admin_col_status()}</th>
              <th class="text-right">{m.admin_th_management()}</th>
            </tr>
          </thead>
          <tbody>
            {#each owners as owner}
              <tr>
                <td>
                  <div class="table-cell-bold">{owner.barbershopName}</div>
                  <div class="table-cell-sub">
                    {owner.barbershopAddress || m.admin_address_not_specified()}
                  </div>
                  {#if owner.botUsername}
                    <a
                      href="https://t.me/{owner.botUsername}"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="bot-link"
                    >
                      <Send size={11} />
                      <span>@{owner.botUsername}</span>
                    </a>
                  {/if}
                </td>
                <td>
                  <div class="table-cell-bold">{owner.ownerName}</div>
                  <div class="table-cell-sub">{owner.email}</div>
                  <div class="table-cell-sub">{owner.phoneNumber || "—"}</div>
                </td>
                <td>
                  <div class="sub-date-wrap">
                    <span class="date-val">{formatDate(owner.nextPayment)}</span
                    >
                    <button
                      class="btn btn-xs btn-outline"
                      on:click={() => openExtendModal(owner)}
                    >{m.admin_extend()}</button>
                  </div>
                </td>
                <td>
                  <div class="stats-mini-grid">
                    <span>{m.admin_metric_barbers()} <strong>{owner.mastersCount}</strong></span>
                    <span>{m.admin_metric_clients()} <strong>{owner.clientsCount}</strong></span>
                    <span
                      >{m.admin_metric_appts()} <strong>{owner.appointmentsCount}</strong></span
                    >
                    <span
                      >{m.admin_metric_turnover()} <strong>{formatCurrency(owner.turnover)}</strong
                      ></span
                    >
                  </div>
                </td>
                <td>
                  <button
                    class="status-toggle-btn"
                    class:active={isOwnerSubActive(owner)}
                    on:click={() => toggleOwnerSubscription(owner)}
                    disabled={actionLoading}
                    title={m.admin_toggle_sub_tooltip()}
                  >
                    {#if isOwnerSubActive(owner)}
                      <CheckCircle2 size={14} />
                      <span>{m.admin_sub_active()}</span>
                    {:else}
                      <XCircle size={14} />
                      <span>{m.admin_sub_frozen()}</span>
                    {/if}
                  </button>
                </td>
                <td class="text-right">
                  <div class="action-buttons-wrap">
                    <button
                      class="btn-icon"
                      on:click={() => openEditOwnerModal(owner)}
                      title={m.admin_edit_tooltip()}
                    >
                      <Edit2 size={16} />
                    </button>
                    <button
                      class="btn-icon danger"
                      on:click={() => deleteOwner(owner)}
                      title={m.admin_delete_salon_tooltip()}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="6" class="text-center py-5 text-muted">
                  {#if isOwnersLoading}{m.admin_loading_salons()}{:else}{m.admin_no_owners_found()}{/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 3. MASTERS GLOBAL MANAGEMENT                                              -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "masters"}
    <div class="filter-bar">
      <div class="search-input-wrap">
        <Search size={17} class="search-icon" />
        <input
          type="text"
          placeholder={m.admin_search_masters()}
          bind:value={masterSearch}
          on:input={() => fetchMasters()}
        />
      </div>
    </div>

    <div class="section-card">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>{m.admin_th_barber()}</th>
              <th>{m.admin_col_salon()}</th>
              <th>{m.admin_th_telegram_contact()}</th>
              <th>{m.admin_th_rating_reviews()}</th>
              <th>{m.admin_th_appts_count()}</th>
              <th>{m.admin_col_status()}</th>
              <th class="text-right">{m.admin_col_actions()}</th>
            </tr>
          </thead>
          <tbody>
            {#each masters as master}
              <tr>
                <td>
                  <div class="master-cell">
                    <div class="master-avatar-circle">
                      {(master.name || "M")[0].toUpperCase()}
                    </div>
                    <div>
                      <div class="table-cell-bold">{master.name}</div>
                      <div class="table-cell-sub">
                        {master.description || m.admin_no_description()}
                      </div>
                    </div>
                  </div>
                </td>
                <td>
                  <div class="table-cell-bold">{master.barbershopName}</div>
                  <div class="table-cell-sub">{master.ownerName}</div>
                </td>
                <td>
                  {#if master.telegramUsername}
                    <a
                      href="https://t.me/{master.telegramUsername.replace(
                        /^@/,
                        '',
                      )}"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="tg-username-badge"
                    >
                      <Send size={12} />
                      <span>@{master.telegramUsername.replace(/^@/, "")}</span>
                    </a>
                  {:else}
                    <span class="text-muted text-xs">{m.admin_username_not_specified()}</span>
                  {/if}
                  {#if master.telegramId}
                    <div class="table-cell-sub">ID: {master.telegramId}</div>
                  {/if}
                </td>
                <td>
                  <div class="rating-badge">
                    ★ {master.rating > 0 ? master.rating.toFixed(1) : "5.0"}
                    <span class="reviews-count">({master.reviewsCount})</span>
                  </div>
                </td>
                <td><strong>{master.appointmentsCount}</strong></td>
                <td>
                  <button
                    class="status-toggle-btn sm"
                    class:active={master.isActive}
                    on:click={() => toggleMasterActive(master)}
                    title={m.admin_toggle_activity()}
                  >
                    {#if master.isActive}
                      <span>{m.admin_status_active()}</span>
                    {:else}
                      <span>{m.admin_status_disabled()}</span>
                    {/if}
                  </button>
                </td>
                <td class="text-right">
                  <div class="action-buttons-wrap">
                    <button
                      class="btn-icon"
                      on:click={() => openEditMasterModal(master)}
                      title={m.admin_edit_tooltip()}
                    >
                      <Edit2 size={16} />
                    </button>
                    <button
                      class="btn-icon danger"
                      on:click={() => deleteMaster(master)}
                      title={m.admin_delete_barber_tooltip()}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="7" class="text-center py-5 text-muted">
                  {#if isMastersLoading}{m.admin_loading_barbers()}{:else}{m.admin_no_masters_found()}{/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 4. CLIENTS MANAGEMENT                                                     -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "clients"}
    <div class="filter-bar">
      <div class="search-input-wrap">
        <Search size={17} class="search-icon" />
        <input
          type="text"
          placeholder={m.admin_search_clients()}
          bind:value={clientSearch}
          on:input={() => fetchClients()}
        />
      </div>

      <button
        class="btn btn-primary"
        on:click={() => exportAllClientsToExcel()}
        disabled={isExporting}
      >
        <FileSpreadsheet size={16} />
        <span>{isExporting ? m.common_loading() : m.admin_export_excel()}</span>
      </button>
    </div>

    <div class="section-card">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>{m.admin_th_client()}</th>
              <th>{m.admin_col_salon()}</th>
              <th>{m.admin_th_phone()}</th>
              <th>Telegram ID</th>
              <th>{m.admin_th_total_appts()}</th>
              <th class="text-right">{m.admin_col_actions()}</th>
            </tr>
          </thead>
          <tbody>
            {#each clients as client}
              <tr>
                <td>
                  <div class="table-cell-bold">{client.name || m.admin_guest()}</div>
                </td>
                <td>{client.barbershopName}</td>
                <td>{client.phone || "\u041d\u0435 \u0443\u043a\u0430\u0437\u0430\u043d"}</td>
                <td><code>{client.telegramId}</code></td>
                <td><strong>{client.appointmentsCount}</strong></td>
                <td class="text-right">
                  <button
                    class="btn-icon danger"
                    on:click={() => deleteClient(client)}
                    title={m.admin_delete_client_tooltip()}
                  >
                    <Trash2 size={16} />
                  </button>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="6" class="text-center py-5 text-muted">
                  {#if isClientsLoading}{m.admin_loading_clients()}{:else}{m.admin_no_clients_found()}{/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 5. GLOBAL APPOINTMENTS AUDIT                                              -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "appointments"}
    <div class="filter-bar">
      <div class="filter-select-wrap">
        <Filter size={16} />
        <select
          bind:value={appointmentStatusFilter}
          on:change={() => fetchAppointments()}
        >
          <option value="">{m.admin_filter_all_statuses()}</option>
          <option value="0">{m.admin_filter_pending()}</option>
          <option value="1">{m.admin_filter_completed()}</option>
          <option value="2">{m.admin_filter_cancelled()}</option>
        </select>
      </div>
    </div>

    <div class="section-card">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>{m.admin_th_datetime()}</th>
              <th>{m.admin_col_salon()}</th>
              <th>{m.admin_th_barber()}</th>
              <th>{m.admin_th_client()}</th>
              <th>{m.admin_th_service_price()}</th>
              <th>{m.admin_col_status()}</th>
              <th class="text-right">{m.admin_th_management()}</th>
            </tr>
          </thead>
          <tbody>
            {#each appointments as appt}
              <tr>
                <td>
                  <div class="table-cell-bold">
                    {formatDateTime(appt.appointmentDate)}
                  </div>
                </td>
                <td>{appt.barbershopName}</td>
                <td><strong class="text-lavender">{appt.masterName}</strong></td
                >
                <td>
                  <div>{appt.clientName}</div>
                  <div class="table-cell-sub">{appt.clientPhone || "—"}</div>
                </td>
                <td>
                  <div>{appt.serviceName}</div>
                  <div class="table-cell-bold text-sage">
                    {formatCurrency(appt.price)}
                  </div>
                </td>
                <td>
                  <span class="badge badge-status-{appt.status}">
                    {getStatusLabel(appt.status)}
                  </span>
                </td>
                <td class="text-right">
                  <button
                    class="btn-icon danger"
                    on:click={() => deleteAppointment(appt)}
                    title={m.admin_delete_appt_tooltip()}
                  >
                    <Trash2 size={16} />
                  </button>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="7" class="text-center py-5 text-muted">
                  {#if isAppointmentsLoading}{m.admin_loading_appts()}{:else}{m.admin_no_appts_found()}{/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- 6. ADMINS MANAGEMENT                                                      -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  {#if activeSection === "admins"}
    <div class="section-card">
      <div class="section-header">
        <div class="section-title-wrap">
          <Shield size={20} class="text-rose" />
          <h2>{m.admin_superadmin_accounts()}</h2>
        </div>
        <button
          class="btn btn-primary"
          on:click={() => (showCreateAdminModal = true)}
        >
          <Plus size={16} />
          <span>{m.admin_create_admin_btn()}</span>
        </button>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Email</th>
              <th>{m.admin_th_role()}</th>
              <th>{m.admin_th_reg_date()}</th>
              <th>{m.admin_col_status()}</th>
            </tr>
          </thead>
          <tbody>
            {#each admins as admin}
              <tr>
                <td>
                  <div class="table-cell-bold">{admin.email}</div>
                  <div class="table-cell-sub">ID: {admin.id}</div>
                </td>
                <td>
                  <span class="badge badge-admin-role">Superadmin</span>
                </td>
                <td>{formatDateTime(admin.createdAt)}</td>
                <td>
                  <span class="badge badge-active">{m.admin_status_active()}</span>
                </td>
              </tr>
            {:else}
              <tr>
                <td colspan="4" class="text-center py-5 text-muted">
                  {#if isAdminsLoading}{m.admin_loading_admins()}{:else}{m.admin_no_admins_found()}{/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {/if}

  <!-- ══════════════════════════════════════════════════════════════════════════ -->
  <!-- MODALS                                                                    -->
  <!-- ══════════════════════════════════════════════════════════════════════════ -->

  <!-- Modal: Extend Subscription -->
  {#if showExtendModal && selectedOwnerForSub}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => (showExtendModal = false)}>
      <div class="modal-card" on:click|stopPropagation>
        <div class="modal-header">
          <h3>{m.admin_extend_sub_title()}</h3>
          <button class="modal-close" on:click={() => (showExtendModal = false)}
            >&times;</button
          >
        </div>
        <div class="modal-body">
          <p class="modal-info-text">
            {m.admin_extend_salon()} <strong>{selectedOwnerForSub.barbershopName}</strong>
            ({selectedOwnerForSub.ownerName})
          </p>
          <p class="modal-info-text">
            {m.admin_current_expiry()} <strong
              >{formatDate(selectedOwnerForSub.nextPayment)}</strong
            >
          </p>

          <div class="form-group mt-3">
            <label for="extendDays">{m.admin_extend_days_label()}</label>
            <div class="quick-days-presets">
              <button
                type="button"
                class="btn btn-xs {extendDaysInput === 14 ? 'btn-primary' : 'btn-outline'}"
                on:click={() => (extendDaysInput = 14)}>{m.admin_days_14()}</button
              >
              <button
                type="button"
                class="btn btn-xs {extendDaysInput === 30 ? 'btn-primary' : 'btn-outline'}"
                on:click={() => (extendDaysInput = 30)}>{m.admin_days_30()}</button
              >
              <button
                type="button"
                class="btn btn-xs {extendDaysInput === 90 ? 'btn-primary' : 'btn-outline'}"
                on:click={() => (extendDaysInput = 90)}>{m.admin_days_90()}</button
              >
              <button
                type="button"
                class="btn btn-xs {extendDaysInput === 180 ? 'btn-primary' : 'btn-outline'}"
                on:click={() => (extendDaysInput = 180)}>{m.admin_days_180()}</button
              >
              <button
                type="button"
                class="btn btn-xs {extendDaysInput === 365 ? 'btn-primary' : 'btn-outline'}"
                on:click={() => (extendDaysInput = 365)}>{m.admin_days_365()}</button
              >
            </div>
            <input
              id="extendDays"
              type="number"
              class="input mt-2"
              bind:value={extendDaysInput}
              min="1"
              max="3650"
            />
          </div>
        </div>
        <div class="modal-footer">
          <button
            class="btn btn-secondary"
            on:click={() => (showExtendModal = false)}>{m.common_cancel()}</button>
          <button
            class="btn btn-primary"
            on:click={submitExtendSubscription}
            disabled={actionLoading}
          >
            <span>{actionLoading ? m.common_loading() : m.admin_extend_btn()}</span>
          </button>
        </div>
      </div>
    </div>
  {/if}

  <!-- Modal: Edit Owner -->
  {#if showEditOwnerModal && editingOwner}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => (showEditOwnerModal = false)}>
      <div class="modal-card modal-lg" on:click|stopPropagation>
        <div class="modal-header">
          <h3>{m.admin_edit_owner_title()}</h3>
          <button
            class="modal-close"
            on:click={() => (showEditOwnerModal = false)}>&times;</button
          >
        </div>
        <form on:submit|preventDefault={submitEditOwner}>
          <div class="modal-body modal-grid-2">
            <div class="form-group">
              <label for="eoName">{m.admin_owner_name_label()}</label>
              <input
                id="eoName"
                type="text"
                class="input"
                bind:value={editingOwner.ownerName}
                required
              />
            </div>

            <div class="form-group">
              <label for="eoPhone">{m.admin_phone_label()}</label>
              <input
                id="eoPhone"
                type="text"
                class="input"
                bind:value={editingOwner.phoneNumber}
              />
            </div>

            <div class="form-group">
              <label for="eoShopName">{m.admin_salon_name_label()}</label>
              <input
                id="eoShopName"
                type="text"
                class="input"
                bind:value={editingOwner.barbershopName}
                required
              />
            </div>

            <div class="form-group">
              <label for="eoBotUsername">{m.admin_bot_username_label()}</label>
              <input
                id="eoBotUsername"
                type="text"
                class="input"
                bind:value={editingOwner.botUsername}
                placeholder="my_booking_bot"
              />
            </div>

            <div class="form-group full-width">
              <label for="eoAddress">{m.admin_salon_address_label()}</label>
              <input
                id="eoAddress"
                type="text"
                class="input"
                bind:value={editingOwner.barbershopAddress}
                placeholder={m.admin_address_placeholder()}
              />
            </div>

            <div class="form-group full-width">
              <label for="eoBotToken">Telegram Bot Token</label>
              <input
                id="eoBotToken"
                type="text"
                class="input"
                bind:value={editingOwner.botToken}
                placeholder="1234567890:ABCdef..."
              />
            </div>

            <div class="form-group full-width">
              <label for="eoNewPassword"
                >{m.admin_new_password_label()}</label>
              <input
                id="eoNewPassword"
                type="password"
                class="input"
                bind:value={editingOwner.newPassword}
                placeholder={m.admin_password_placeholder()}
                minlength="6"
              />
            </div>

            <div class="form-group full-width">
              <label for="eoDesc">{m.admin_salon_desc_label()}</label>
              <textarea
                id="eoDesc"
                class="input textarea"
                bind:value={editingOwner.barbershopDescription}
                rows="3"
              ></textarea>
            </div>
          </div>
          <div class="modal-footer">
            {#if editingOwner.email}
              <button
                type="button"
                class="btn btn-secondary mr-auto"
                disabled={actionLoading}
                on:click={() => sendOwnerResetPasswordEmail(editingOwner.email)}
                title={m.admin_reset_pwd_email_tooltip()}
              >
                {m.admin_reset_pwd_email_btn()}
              </button>
            {/if}
            <button
              type="button"
              class="btn btn-secondary"
              on:click={() => (showEditOwnerModal = false)}>{m.common_cancel()}</button>
            <button
              type="submit"
              class="btn btn-primary"
              disabled={actionLoading}
            >
              <span
                >{actionLoading ? m.common_loading() : m.common_save()}</span
              >
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- Modal: Edit Master -->
  {#if showEditMasterModal && editingMaster}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => (showEditMasterModal = false)}>
      <div class="modal-card" on:click|stopPropagation>
        <div class="modal-header">
          <h3>{m.admin_edit_master_title()}</h3>
          <button
            class="modal-close"
            on:click={() => (showEditMasterModal = false)}>&times;</button
          >
        </div>
        <form on:submit|preventDefault={submitEditMaster}>
          <div class="modal-body">
            <div class="form-group">
              <label for="emName">{m.admin_barber_name_label()}</label>
              <input
                id="emName"
                type="text"
                class="input"
                bind:value={editingMaster.name}
                required
              />
            </div>

            <div class="form-group">
              <label for="emUsername">Telegram Username (@username)</label>
              <input
                id="emUsername"
                type="text"
                class="input"
                bind:value={editingMaster.telegramUsername}
                placeholder="alex_barber"
              />
            </div>

            <div class="form-group">
              <label for="emTgId">Telegram Numeric ID</label>
              <input
                id="emTgId"
                type="text"
                class="input"
                bind:value={editingMaster.telegramId}
                placeholder="123456789"
              />
            </div>

            <div class="form-group">
              <label for="emDesc">{m.admin_barber_desc_label()}</label>
              <input
                id="emDesc"
                type="text"
                class="input"
                bind:value={editingMaster.description}
                placeholder="Top Barber"
              />
            </div>

            <div class="form-checkbox-wrap mt-2">
              <label class="checkbox-label">
                <input type="checkbox" bind:checked={editingMaster.isActive} />
                <span>{m.admin_barber_active_checkbox()}</span>
              </label>
            </div>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              on:click={() => (showEditMasterModal = false)}>{m.common_cancel()}</button>
            <button
              type="submit"
              class="btn btn-primary"
              disabled={actionLoading}
            >
              <span>{actionLoading ? m.common_loading() : m.common_save()}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- Modal: Create Admin -->
  {#if showCreateAdminModal}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => (showCreateAdminModal = false)}>
      <div class="modal-card" on:click|stopPropagation>
        <div class="modal-header">
          <h3>{m.admin_create_admin_title()}</h3>
          <button
            class="modal-close"
            on:click={() => (showCreateAdminModal = false)}>&times;</button
          >
        </div>
        <form on:submit|preventDefault={submitCreateAdmin}>
          <div class="modal-body">
            <div class="form-group">
              <label for="naEmail">{m.admin_admin_email_label()}</label>
              <input
                id="naEmail"
                type="email"
                class="input"
                bind:value={newAdminEmail}
                placeholder="admin2@barbershop.b2b"
                required
              />
            </div>

            <div class="form-group">
              <label for="naPassword">{m.admin_password_label()}</label>
              <input
                id="naPassword"
                type="password"
                class="input"
                bind:value={newAdminPassword}
                placeholder="••••••••"
                required
              />
            </div>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              on:click={() => (showCreateAdminModal = false)}>{m.common_cancel()}</button>
            <button
              type="submit"
              class="btn btn-primary"
              disabled={actionLoading}
            >
              <span
                >{actionLoading
                  ? m.common_loading() : m.admin_create_admin_btn()}</span
              >
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}
  <!-- Modal: Create Owner -->
  {#if showCreateOwnerModal}
    <!-- svelte-ignore a11y-click-events-have-key-events -->
    <!-- svelte-ignore a11y-no-static-element-interactions -->
    <div class="modal-backdrop" on:click={() => (showCreateOwnerModal = false)}>
      <div class="modal-card modal-lg" on:click|stopPropagation>
        <div class="modal-header">
          <h3>{m.admin_create_owner_title()}</h3>
          <button
            class="modal-close"
            on:click={() => (showCreateOwnerModal = false)}>&times;</button
          >
        </div>
        <form on:submit|preventDefault={submitCreateOwner}>
          <div class="modal-body modal-grid-2">
            <!-- Section 1 -->
            <div class="form-section-title full-width">
              <span>{m.admin_sec1_title()}</span>
            </div>

            <div class="form-group">
              <label for="noEmail">{m.admin_salon_email_label()}</label>
              <div class="input-icon-wrap">
                <Mail size={16} class="input-icon" />
                <input
                  id="noEmail"
                  type="email"
                  class="input has-icon"
                  bind:value={newOwner.email}
                  placeholder="owner@example.com"
                  required
                />
              </div>
            </div>

            <div class="form-group">
              <label for="noPassword">{m.admin_password_label()} *</label>
              <div class="input-icon-wrap">
                <Lock size={16} class="input-icon" />
                <input
                  id="noPassword"
                  type="password"
                  class="input has-icon"
                  bind:value={newOwner.password}
                  placeholder={m.settings_new_pwd_placeholder()}
                  minlength="6"
                  required
                />
              </div>
            </div>

            <div class="form-group full-width">
              <label for="noDuration">{m.admin_sub_duration_label()}</label>
              <div class="quick-days-presets">
                <button
                  type="button"
                  class="btn btn-xs {newOwner.subscriptionDays === 14
                    ? 'btn-primary'
                    : 'btn-outline'}"
                  on:click={() => (newOwner.subscriptionDays = 14)}
                  >{m.admin_dur_14()}</button
                >
                <button
                  type="button"
                  class="btn btn-xs {newOwner.subscriptionDays === 30
                    ? 'btn-primary'
                    : 'btn-outline'}"
                  on:click={() => (newOwner.subscriptionDays = 30)}
                  >{m.admin_dur_30()}</button
                >
                <button
                  type="button"
                  class="btn btn-xs {newOwner.subscriptionDays === 90
                    ? 'btn-primary'
                    : 'btn-outline'}"
                  on:click={() => (newOwner.subscriptionDays = 90)}
                  >{m.admin_dur_90()}</button
                >
                <button
                  type="button"
                  class="btn btn-xs {newOwner.subscriptionDays === 180
                    ? 'btn-primary'
                    : 'btn-outline'}"
                  on:click={() => (newOwner.subscriptionDays = 180)}
                  >{m.admin_dur_180()}</button
                >
                <button
                  type="button"
                  class="btn btn-xs {newOwner.subscriptionDays === 365
                    ? 'btn-primary'
                    : 'btn-outline'}"
                  on:click={() => (newOwner.subscriptionDays = 365)}
                  >{m.admin_dur_365()}</button
                >
              </div>
              <input
                id="noDuration"
                type="number"
                class="input mt-2"
                bind:value={newOwner.subscriptionDays}
                min="1"
                max="3650"
                required
              />
            </div>

            <!-- Section 2 -->
            <div class="form-section-title full-width">
              <span>{m.admin_sec2_title()}</span>
            </div>

            <div class="form-group">
              <label for="noOwnerName">{m.admin_owner_name_label()}</label>
              <div class="input-icon-wrap">
                <User size={16} class="input-icon" />
                <input
                  id="noOwnerName"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.ownerName}
                  placeholder={m.admin_owner_name_placeholder()}
                />
              </div>
            </div>

            <div class="form-group">
              <label for="noPhone">{m.admin_owner_phone_label()}</label>
              <div class="input-icon-wrap">
                <Phone size={16} class="input-icon" />
                <input
                  id="noPhone"
                  type="tel"
                  class="input has-icon"
                  bind:value={newOwner.phoneNumber}
                  placeholder="+380991234567"
                />
              </div>
            </div>

            <div class="form-group full-width">
              <label for="noTgId">{m.admin_owner_tgid_label()}</label>
              <div class="input-icon-wrap">
                <Send size={16} class="input-icon" />
                <input
                  id="noTgId"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.telegramId}
                  placeholder={m.admin_owner_tgid_placeholder()}
                />
              </div>
            </div>

            <!-- Section 3 -->
            <div class="form-section-title full-width">
              <span>{m.admin_sec3_title()}</span>
            </div>

            <div class="form-group">
              <label for="noBarbershopName">{m.admin_salon_name_label()}</label>
              <div class="input-icon-wrap">
                <Building2 size={16} class="input-icon" />
                <input
                  id="noBarbershopName"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.barbershopName}
                  placeholder={m.admin_salon_name_placeholder()}
                />
              </div>
            </div>

            <div class="form-group">
              <label for="noTimeZone">{m.admin_timezone_label()}</label>
              <div class="input-icon-wrap">
                <Globe size={16} class="input-icon" />
                <select id="noTimeZone" class="input has-icon" bind:value={newOwner.timeZone}>
                  <option value="Europe/Kyiv">Kyiv (UTC+2/3)</option>
                  <option value="Europe/Warsaw">Warsaw (UTC+1/2)</option>
                  <option value="Europe/London">London (UTC+0/1)</option>
                  <option value="Europe/Moscow">Moscow (UTC+3)</option>
                </select>
              </div>
            </div>

            <div class="form-group full-width">
              <label for="noAddress">{m.admin_salon_address_label()}</label>
              <div class="input-icon-wrap">
                <MapPin size={16} class="input-icon" />
                <input
                  id="noAddress"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.barbershopAddress}
                  placeholder={m.admin_salon_address_placeholder()}
                />
              </div>
            </div>

            <div class="form-group full-width">
              <label for="noDesc">{m.admin_salon_desc_for_bot()}</label>
              <textarea
                id="noDesc"
                class="input textarea"
                bind:value={newOwner.barbershopDescription}
                rows="2"
                placeholder={m.admin_salon_desc_placeholder()}
              ></textarea>
            </div>

            <!-- Section 4 -->
            <div class="form-section-title full-width">
              <span>{m.admin_sec4_title()}</span>
            </div>

            <div class="form-group">
              <label for="noBotUsername">{m.admin_bot_username_field()}</label>
              <div class="input-icon-wrap">
                <Bot size={16} class="input-icon" />
                <input
                  id="noBotUsername"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.botUsername}
                  placeholder="my_booking_bot"
                />
              </div>
            </div>

            <div class="form-group">
              <label for="noBotToken">{m.admin_bot_token_field()}</label>
              <div class="input-icon-wrap">
                <Bot size={16} class="input-icon" />
                <input
                  id="noBotToken"
                  type="text"
                  class="input has-icon"
                  bind:value={newOwner.botToken}
                  placeholder="1234567890:ABCdef..."
                />
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              on:click={() => (showCreateOwnerModal = false)}>{m.common_cancel()}</button>
            <button
              type="submit"
              class="btn btn-primary"
              disabled={actionLoading}
            >
              <span>{actionLoading ? m.common_loading() : m.admin_create_owner_btn()}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}
</AdminLayout>
<!-- sdsd -->

<style>
  /* Header */
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1.5rem;
    margin-bottom: 2rem;
    flex-wrap: wrap;
  }

  .page-header h1 {
    font-size: 1.85rem;
    font-weight: 700;
    margin: 0 0 0.4rem;
    letter-spacing: -0.02em;
  }

  .subtitle {
    color: var(--text-secondary);
    font-size: 0.95rem;
    margin: 0;
  }

  .header-actions {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    flex-wrap: wrap;
  }

  /* Toasts */
  .toast {
    position: fixed;
    top: 1.5rem;
    right: 1.5rem;
    z-index: 10000;
    padding: 0.85rem 1.25rem;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 0.92rem;
    font-weight: 600;
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.4);
    animation: fadeIn 0.3s ease;
  }

  .toast-success {
    background: rgba(30, 41, 35, 0.95);
    border: 1px solid rgba(168, 198, 155, 0.4);
    color: var(--pastel-sage);
  }

  .toast-danger {
    background: rgba(45, 25, 25, 0.95);
    border: 1px solid rgba(242, 139, 130, 0.4);
    color: var(--pastel-coral);
  }

  /* Metrics Grid */
  .metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.75rem;
  }

  .metric-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 1.4rem 1.25rem;
    display: flex;
    gap: 1rem;
    position: relative;
    overflow: hidden;
    transition:
      transform 0.25s var(--ease-spring),
      box-shadow 0.25s ease;
  }

  .metric-card:hover {
    transform: translateY(-2px);
  }

  .metric-card.glow-rose:hover {
    box-shadow: 0 8px 24px var(--pastel-rose-glow);
  }
  .metric-card.glow-sage:hover {
    box-shadow: 0 8px 24px var(--pastel-sage-glow);
  }
  .metric-card.glow-lavender:hover {
    box-shadow: 0 8px 24px var(--pastel-lavender-glow);
  }
  .metric-card.glow-coral:hover {
    box-shadow: 0 8px 24px rgba(242, 139, 130, 0.15);
  }

  .metric-icon-wrap {
    width: 46px;
    height: 46px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .metric-icon-wrap.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
  }

  .metric-icon-wrap.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .metric-icon-wrap.lavender {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.3);
  }

  .metric-icon-wrap.coral {
    background: rgba(242, 139, 130, 0.12);
    color: var(--pastel-coral);
    border: 1px solid rgba(242, 139, 130, 0.3);
  }

  .metric-content {
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
  }

  .metric-label {
    font-size: 0.8rem;
    color: var(--text-secondary);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-bottom: 0.35rem;
  }

  .metric-value {
    font-size: 1.65rem;
    font-weight: 700;
    color: var(--text-primary);
    line-height: 1.1;
    margin-bottom: 0.4rem;
  }

  .metric-sub {
    display: flex;
    gap: 0.5rem;
    font-size: 0.78rem;
    color: var(--text-secondary);
    flex-wrap: wrap;
  }

  .active-pill {
    color: var(--pastel-sage);
    font-weight: 600;
  }
  .inactive-pill {
    color: var(--pastel-coral);
    font-weight: 600;
  }

  /* Chart Controls Panel */
  .chart-controls-panel {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.4rem;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .chart-panel-left {
    display: flex;
    align-items: center;
    gap: 1.25rem;
    flex-wrap: wrap;
  }

  .chart-panel-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--text-primary);
  }

  .chart-select select {
    font-size: 0.85rem;
    padding: 0.4rem 0.5rem;
  }

  /* Period Tabs */
  .period-tabs {
    display: flex;
    gap: 0.3rem;
    background: var(--bg-surface-elevated);
    padding: 0.25rem;
    border-radius: var(--radius-pill);
    border: 1px solid var(--border-subtle);
  }

  .period-tab {
    padding: 0.35rem 0.85rem;
    border-radius: var(--radius-pill);
    background: transparent;
    border: none;
    color: var(--text-secondary);
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .period-tab:hover {
    color: var(--text-primary);
    background: rgba(255, 255, 255, 0.04);
  }

  .period-tab.active {
    background: linear-gradient(135deg, var(--pastel-rose), #c88777);
    color: var(--text-inverse);
  }

  /* Custom Range Card */
  .custom-range-card {
    padding: 1.15rem 1.4rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
  }

  .custom-range-inner {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1.25rem;
    flex-wrap: wrap;
  }

  .custom-range-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 600;
    font-size: 0.9rem;
    color: var(--text-primary);
  }

  .custom-range-inputs {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .date-input-group {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    font-size: 0.85rem;
    color: var(--text-secondary);
    font-weight: 600;
  }

  .date-field {
    padding: 0.4rem 0.75rem;
    font-size: 0.85rem;
    border-radius: var(--radius-md);
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    color: var(--text-primary);
  }

  .chart-badge-tag {
    font-size: 0.82rem;
    font-weight: 700;
    padding: 0.35rem 0.85rem;
    border-radius: var(--radius-pill);
  }

  .chart-badge-tag.rose {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
  }

  .chart-badge-tag.sage {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .svg-chart-container {
    width: 100%;
    overflow-x: auto;
  }

  .chart-svg {
    width: 100%;
    height: auto;
    min-width: 600px;
    display: block;
    overflow: visible;
  }

  .axis-text {
    font-size: 10px;
    fill: var(--text-muted);
    font-family: inherit;
  }

  .chart-point {
    cursor: pointer;
    transition: r 0.15s ease;
  }

  .chart-point:hover {
    r: 6.5px;
  }

  /* Section Card & Table */
  .section-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 1.5rem;
    margin-bottom: 1.5rem;
  }

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;
    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .section-title-wrap {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .section-title-wrap h2 {
    font-size: 1.25rem;
    font-weight: 700;
    margin: 0;
  }

  .section-desc {
    font-size: 0.8rem;
    color: var(--text-secondary);
    margin: 0;
  }

  .filter-bar {
    display: flex;
    gap: 1rem;
    margin-bottom: 1.25rem;
    flex-wrap: wrap;
  }

  .search-input-wrap {
    position: relative;
    flex: 1;
    min-width: 260px;
    display: flex;
    align-items: center;
  }

  :global(.search-icon) {
    position: absolute;
    left: 1rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .search-input-wrap input {
    width: 100%;
    padding: 0.7rem 1rem 0.7rem 2.6rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-size: 0.9rem;
    transition: border-color 0.2s ease;
  }

  .search-input-wrap input:focus {
    border-color: var(--pastel-rose);
    outline: none;
  }

  .filter-select-wrap {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    padding: 0 0.85rem;
    color: var(--text-secondary);
  }

  .filter-select-wrap select {
    background: transparent;
    border: none;
    color: var(--text-primary);
    padding: 0.7rem 0.5rem;
    font-size: 0.88rem;
    outline: none;
    cursor: pointer;
  }

  .filter-select-wrap select option,
  .chart-select select option {
    background-color: #161a23;
    color: #f8fafc;
  }

  :global([data-theme="light"]) .filter-select-wrap select option,
  :global([data-theme="light"]) .chart-select select option {
    background-color: #ffffff !important;
    color: #0f172a !important;
  }

  .table-container {
    overflow-x: auto;
  }

  .data-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88rem;
  }

  .data-table th {
    text-align: left;
    padding: 0.85rem 1rem;
    color: var(--text-secondary);
    font-weight: 600;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 1px solid var(--border-subtle);
  }

  .data-table td {
    padding: 1rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    vertical-align: middle;
  }

  .data-table tr:hover td {
    background-color: rgba(255, 255, 255, 0.02);
  }

  .table-cell-bold {
    font-weight: 600;
    color: var(--text-primary);
  }

  .table-cell-sub {
    font-size: 0.78rem;
    color: var(--text-secondary);
    margin-top: 2px;
  }

  .bot-link {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.78rem;
    color: var(--pastel-rose);
    margin-top: 3px;
    text-decoration: none;
    font-weight: 600;
  }

  .bot-link:hover {
    text-decoration: underline;
  }

  .tg-username-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 3px 8px;
    border-radius: var(--radius-pill);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
    font-size: 0.78rem;
    font-weight: 600;
    text-decoration: none;
  }

  .tg-username-badge:hover {
    background: rgba(223, 158, 142, 0.25);
  }

  .master-cell {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .master-avatar-circle {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: var(--pastel-lavender-dim);
    border: 1px solid rgba(179, 183, 219, 0.4);
    color: var(--pastel-lavender);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.85rem;
  }

  .rating-badge {
    color: #e5b869;
    font-weight: 700;
    font-size: 0.9rem;
  }

  .reviews-count {
    color: var(--text-secondary);
    font-size: 0.75rem;
    font-weight: normal;
  }

  .sub-date-wrap {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .date-val {
    font-weight: 600;
  }

  .stats-mini-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.25rem 0.75rem;
    font-size: 0.78rem;
    color: var(--text-secondary);
  }

  /* Badges */
  .badge {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.25rem 0.6rem;
    border-radius: var(--radius-pill);
    font-size: 0.75rem;
    font-weight: 600;
  }

  .badge-active {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .badge-inactive {
    background: rgba(242, 139, 130, 0.12);
    color: var(--pastel-coral);
    border: 1px solid rgba(242, 139, 130, 0.3);
  }

  .badge-admin-role {
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.3);
  }

  .badge-status-0 {
    background: var(--pastel-lavender-dim);
    color: var(--pastel-lavender);
    border: 1px solid rgba(179, 183, 219, 0.3);
  }

  .badge-status-1 {
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(168, 198, 155, 0.3);
  }

  .badge-status-2 {
    background: rgba(242, 139, 130, 0.12);
    color: var(--pastel-coral);
    border: 1px solid rgba(242, 139, 130, 0.3);
  }

  /* Buttons */
  .status-toggle-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.35rem 0.75rem;
    border-radius: var(--radius-pill);
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    border: 1px solid rgba(242, 139, 130, 0.3);
    background: rgba(242, 139, 130, 0.12);
    color: var(--pastel-coral);
    transition: all 0.2s ease;
  }

  .status-toggle-btn.active {
    border-color: rgba(168, 198, 155, 0.3);
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
  }

  .status-toggle-btn.sm {
    padding: 0.2rem 0.55rem;
    font-size: 0.74rem;
  }

  .action-buttons-wrap {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 0.4rem;
  }

  .btn-icon {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);
    padding: 0.45rem;
    border-radius: var(--radius-md);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
  }

  .btn-icon:hover {
    color: var(--text-primary);
    background: rgba(255, 255, 255, 0.08);
  }

  .btn-icon.danger:hover {
    color: var(--pastel-coral);
    background: rgba(242, 139, 130, 0.15);
    border-color: rgba(242, 139, 130, 0.3);
  }

  .btn-xs {
    padding: 0.25rem 0.55rem;
    font-size: 0.75rem;
  }

  .btn-sm {
    padding: 0.4rem 0.8rem;
    font-size: 0.82rem;
  }

  /* Modals */
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.75);
    backdrop-filter: blur(8px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1.5rem;
    animation: fadeIn 0.2s ease;
  }

  .modal-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    width: 100%;
    max-width: 480px;
    padding: 1.75rem;
    box-shadow: 0 16px 40px rgba(0, 0, 0, 0.5);
    max-height: 90vh;
    overflow-y: auto;
  }

  .modal-card.modal-lg {
    max-width: 650px;
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;
  }

  .modal-header h3 {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 700;
  }

  .modal-close {
    background: none;
    border: none;
    color: var(--text-secondary);
    font-size: 1.6rem;
    cursor: pointer;
    line-height: 1;
  }

  .modal-body {
    margin-bottom: 1.5rem;
  }

  .modal-grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }

  .full-width {
    grid-column: 1 / -1;
  }

  .modal-info-text {
    margin: 0 0 0.5rem;
    font-size: 0.92rem;
    color: var(--text-secondary);
  }

  .modal-info-text strong {
    color: var(--text-primary);
  }

  .quick-days-presets {
    display: flex;
    gap: 0.4rem;
    margin-bottom: 0.5rem;
  }

  .form-section-title {
    display: flex;
    align-items: center;
    margin: 0.75rem 0 0.25rem;
    padding-bottom: 0.4rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  .form-section-title span {
    font-size: 0.8rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--pastel-rose);
    background: var(--pastel-rose-dim);
    padding: 0.2rem 0.65rem;
    border-radius: var(--radius-pill);
    border: 1px solid rgba(223, 158, 142, 0.2);
  }

  .input-icon-wrap {
    position: relative;
    display: flex;
    align-items: center;
  }

  :global(.input-icon) {
    position: absolute;
    left: 0.85rem;
    color: var(--text-muted);
    pointer-events: none;
  }

  .input.has-icon {
    padding-left: 2.5rem;
  }

  .form-group {
    margin-bottom: 1rem;
  }

  .form-group label {
    display: block;
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-secondary);
    margin-bottom: 0.4rem;
  }

  .textarea {
    resize: vertical;
  }

  .checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.9rem;
    cursor: pointer;
  }

  .modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
  }

  .text-rose {
    color: var(--pastel-rose);
  }
  .text-sage {
    color: var(--pastel-sage);
  }
  .text-lavender {
    color: var(--pastel-lavender);
  }
  .text-muted {
    color: var(--text-muted);
  }
  .text-xs {
    font-size: 0.75rem;
  }
  .text-center {
    text-align: center;
  }
  .text-right {
    text-align: right;
  }
  .mt-2 {
    margin-top: 0.5rem;
  }
  .mt-3 {
    margin-top: 0.75rem;
  }
  .mt-4 {
    margin-top: 1.25rem;
  }
  .mb-4 {
    margin-bottom: 1.75rem;
  }
  .py-4 {
    padding-top: 1rem;
    padding-bottom: 1rem;
  }
  .py-5 {
    padding-top: 1.5rem;
    padding-bottom: 1.5rem;
  }

  @media (max-width: 768px) {
    .modal-grid-2 {
      grid-template-columns: 1fr;
    }
  }
</style>

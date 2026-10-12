// Real booking wizard -- calls GessFlow's public API directly from the browser (same Tier 3
// cross-origin pattern already proven for the pilot's static prototype), driving plain DOM
// updates instead of a virtual-DOM re-render system, since Razor already rendered the page shell.
(function () {
  "use strict";

  var apiBase = window.GESSFLOW_API_BASE || "";
  var slug = window.BOOKING_SLUG || "";
  var storefront = window.STOREFRONT || { services: [], employees: [], locations: [], policy: null };
  var I18N = window.BOOK_I18N || {};
  var LANG = window.BOOK_LANG === "hi" ? "hi" : "en";
  var LOCALE = LANG === "hi" ? "hi-IN" : "en-US";

  // Looks up a string for the current language and fills {name} style placeholders.
  function t(key, vars) {
    var text = I18N[key] != null ? I18N[key] : key;
    if (vars) Object.keys(vars).forEach(function (k) { text = text.split("{" + k + "}").join(String(vars[k])); });
    return text;
  }

  var state = {
    step: 1,
    svc: null, // { id, name, price, durationMinutes }
    artistId: null,
    calendarMonth: (function () { var d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1); })(),
    day: "", // 'YYYY-MM-DD'
    slot: "", // ISO startAtUtc
    availabilitySlots: [],
    availabilityLoading: false,
    submitting: false,
    confirmation: null,
    form: { firstName: "", lastName: "", mobile: "", email: "", notes: "", consentWhatsApp: false, consentEmailSms: false }
  };

  function pad2(n) { return String(n).padStart(2, "0"); }
  function dateToIso(d) { return d.getFullYear() + "-" + pad2(d.getMonth() + 1) + "-" + pad2(d.getDate()); }
  function todayIso() { return dateToIso(new Date()); }
  function locationTz() { var loc = storefront.locations && storefront.locations[0]; return (loc && loc.timeZone) || "UTC"; }
  function locationId() { var loc = storefront.locations && storefront.locations[0]; return loc ? loc.id : null; }

  function formatSlotTime(iso, tz) {
    try { return new Intl.DateTimeFormat(LOCALE, { hour: "numeric", minute: "2-digit", hour12: true, timeZone: tz || "UTC" }).format(new Date(iso)); }
    catch (e) { return new Date(iso).toLocaleTimeString(LOCALE, { hour: "numeric", minute: "2-digit" }); }
  }
  function fmtInr(n) { return "₹" + Math.round(n).toLocaleString("en-IN"); }

  function currentEmployeeName() {
    if (!state.artistId) return "";
    var e = (storefront.employees || []).find(function (x) { return x.id === state.artistId; });
    return e ? e.name : "";
  }

  function showError(msg) {
    var el = document.getElementById("bookingError");
    if (!el) return;
    if (msg) { el.textContent = msg; el.style.display = "block"; }
    else { el.style.display = "none"; }
  }

  function getForm() {
    function val(key) { var el = document.querySelector('[data-field="' + key + '"]'); return el ? el.value.trim() : ""; }
    function checked(key) { var el = document.querySelector('[data-field="' + key + '"]'); return !!(el && el.checked); }
    return { firstName: val("firstName"), lastName: val("lastName"), mobile: val("mobile"), email: val("email"), notes: val("notes"), consentWhatsApp: checked("consentWhatsApp"), consentEmailSms: checked("consentEmailSms") };
  }

  // ---- Rendering ----
  function renderAll() {
    renderStepIndicators();
    renderServiceList();
    renderArtistList();
    renderCalendar();
    renderSlots();
    renderSummary();
    renderPolicy();
    renderConfirmation();
  }

  function renderStepIndicators() {
    document.querySelectorAll("[data-step-indicator]").forEach(function (el) {
      var n = Number(el.getAttribute("data-step-indicator"));
      el.classList.toggle("active", n <= state.step);
    });
    document.querySelectorAll("[data-book-panel]").forEach(function (el) {
      el.style.display = Number(el.getAttribute("data-book-panel")) === state.step ? "" : "none";
    });
    var nextBtn = document.querySelector("[data-book-next]");
    var backBtn = document.querySelector("[data-book-back]");
    if (backBtn) backBtn.style.display = state.step === 1 ? "none" : "";
    if (nextBtn) {
      if (state.step === 4) { nextBtn.style.display = "none"; }
      else {
        nextBtn.style.display = "";
        nextBtn.disabled = state.submitting;
        nextBtn.textContent = state.submitting ? t("bk.confirming") : (state.step === 3 ? t("bk.confirm") : t("bk.continue"));
      }
    }
  }

  function renderServiceList() {
    document.querySelectorAll("[data-svc-id]").forEach(function (btn) {
      var id = btn.getAttribute("data-svc-id");
      btn.classList.toggle("selected", !!(state.svc && state.svc.id === id));
      btn.onclick = function () {
        state.svc = { id: id, name: btn.getAttribute("data-svc-name"), price: Number(btn.getAttribute("data-svc-price")), durationMinutes: Number(btn.getAttribute("data-svc-duration")) };
        state.artistId = null; state.day = ""; state.slot = ""; state.availabilitySlots = [];
        showError(null);
        renderAll();
      };
    });
  }

  function qualifiedEmployees() {
    if (!state.svc) return [];
    return (storefront.employees || []).filter(function (e) { return (e.serviceIds || []).indexOf(state.svc.id) !== -1; });
  }

  function renderArtistList() {
    var container = document.querySelector("[data-artist-list]");
    if (!container) return;
    container.innerHTML = "";
    qualifiedEmployees().forEach(function (e) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "artist-btn" + (state.artistId === e.id ? " selected" : "");
      btn.innerHTML = e.name + '<span class="artist-role">' + (e.designation || "") + "</span>";
      btn.onclick = function () {
        state.artistId = e.id; state.day = ""; state.slot = ""; state.availabilitySlots = [];
        showError(null);
        renderAll();
      };
      container.appendChild(btn);
    });
  }

  function renderCalendar() {
    var label = document.querySelector("[data-month-label]");
    if (label) label.textContent = t("bk.pickDate") + " · " + state.calendarMonth.toLocaleDateString(LOCALE, { month: "long", year: "numeric" });

    var prevBtn = document.querySelector("[data-prev-month]");
    if (prevBtn) {
      var real = new Date(); var realFirst = new Date(real.getFullYear(), real.getMonth(), 1);
      var canGoPrev = state.calendarMonth > realFirst;
      prevBtn.disabled = !canGoPrev;
      prevBtn.style.opacity = canGoPrev ? "1" : ".4";
    }

    var grid = document.querySelector("[data-calendar-grid]");
    if (!grid) return;
    grid.innerHTML = "";
    var year = state.calendarMonth.getFullYear(), mo = state.calendarMonth.getMonth();
    var daysInMonth = new Date(year, mo + 1, 0).getDate();
    var first = new Date(year, mo, 1).getDay();
    var mondayOffset = (first + 6) % 7;
    var todayStr = todayIso();

    for (var i = 0; i < mondayOffset; i++) {
      var blank = document.createElement("div");
      grid.appendChild(blank);
    }
    for (var n = 1; n <= daysInMonth; n++) {
      var date = new Date(year, mo, n);
      var iso = dateToIso(date);
      var closed = date.getDay() === 0 || iso < todayStr;
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "calendar-cell" + (state.day === iso ? " selected" : "");
      btn.textContent = String(n);
      btn.disabled = closed;
      if (!closed) {
        btn.onclick = function (isoCaptured) {
          return function () {
            state.day = isoCaptured; state.slot = ""; state.availabilitySlots = [];
            showError(null);
            renderAll();
            loadAvailabilityIfReady();
          };
        }(iso);
      }
      grid.appendChild(btn);
    }
  }

  function renderSlots() {
    var prompt = document.getElementById("slotsPrompt");
    var loading = document.getElementById("slotsLoading");
    var empty = document.getElementById("slotsEmpty");
    var list = document.querySelector("[data-slot-list]");
    if (!list) return;

    var hasSelection = !!(state.artistId && state.day);
    if (prompt) prompt.style.display = hasSelection ? "none" : "";
    if (loading) loading.style.display = state.availabilityLoading ? "" : "none";
    if (empty) empty.style.display = (hasSelection && !state.availabilityLoading && state.availabilitySlots.length === 0) ? "" : "none";

    list.innerHTML = "";
    state.availabilitySlots.forEach(function (sl) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "slot-btn" + (state.slot === sl.startAtUtc ? " selected" : "");
      btn.textContent = formatSlotTime(sl.startAtUtc, locationTz());
      btn.onclick = function () { state.slot = sl.startAtUtc; showError(null); renderAll(); };
      list.appendChild(btn);
    });
  }

  function depositFor(total) {
    var policy = storefront.policy;
    if (!policy || !policy.depositRequired) return 0;
    return Math.round(Math.max(total * (policy.depositPercent / 100), policy.depositMinimumAmount || 0));
  }

  function renderSummary() {
    var total = state.svc ? state.svc.price : 0;
    setText("[data-summary-service]", state.svc ? state.svc.name : t("bk.notSelected"));
    setText("[data-summary-artist]", currentEmployeeName() || t("bk.notSelected"));
    setText("[data-summary-date]", state.day ? new Date(state.day + "T00:00:00").toLocaleDateString(LOCALE, { weekday: "short", day: "numeric", month: "short", year: "numeric" }) : t("bk.notSelected"));
    setText("[data-summary-time]", state.slot ? formatSlotTime(state.slot, locationTz()) : t("bk.notSelected"));
    setText("[data-summary-duration]", state.svc ? (state.svc.durationMinutes + " " + t("bk.min")) : "-");
    setText("[data-summary-total]", fmtInr(total));

    var policy = storefront.policy;
    var note = "";
    if (policy && policy.depositRequired) {
      note = t("bk.sumDeposit", { percent: policy.depositPercent, amount: fmtInr(depositFor(total)) }) + " " + t("bk.sumCancel", { hours: policy.cancellationWindowHours });
    } else if (policy) {
      note = t("bk.sumCancel", { hours: policy.cancellationWindowHours });
    }
    setText("[data-summary-policy]", note);
  }

  // The salon's own deposit and cancellation terms, shown on the details step next to Confirm.
  function renderPolicy() {
    var box = document.querySelector("[data-policy-block]");
    if (!box) return;
    var policy = storefront.policy;
    box.innerHTML = "";
    if (!policy) return;
    var lines = [];
    if (policy.depositRequired) lines.push(t("bk.depositMay", { amount: fmtInr(depositFor(state.svc ? state.svc.price : 0)), percent: policy.depositPercent }));
    if (policy.cancellationWindowHours > 0) {
      lines.push(t("bk.cancelWindow", { hours: policy.cancellationWindowHours }));
      if (policy.lateCancellationFeeAmount > 0) lines.push(t("bk.lateFee", { fee: fmtInr(policy.lateCancellationFeeAmount) }));
      else if (policy.depositRequired) lines.push(t("bk.lateForfeit"));
    }
    if (policy.waiverText) lines.push(policy.waiverText);
    if (!lines.length) return;
    var title = document.createElement("div");
    title.className = "label-sm";
    title.textContent = t("bk.policyTitle");
    box.appendChild(title);
    lines.forEach(function (line) {
      var p = document.createElement("div");
      p.textContent = line;
      box.appendChild(p);
    });
  }

  function renderConfirmation() {
    var el = document.querySelector("[data-confirmation-id]");
    if (!el) return;
    el.textContent = state.confirmation ? t("bk.reference", { ref: state.confirmation.appointmentId.slice(0, 8).toUpperCase() }) : "";
    renderDeposit();
  }

  // Deposit due after booking: the amount, a Pay deposit button when the platform gave a payment link,
  // and a note that the slot is only held for a limited time while the deposit is unpaid.
  function renderDeposit() {
    var box = document.querySelector("[data-deposit-box]");
    if (!box) return;
    box.innerHTML = "";
    var c = state.confirmation;
    if (!c || !(c.depositAmount > 0)) { box.style.display = "none"; return; }
    box.style.display = "";
    function add(tag, cls, text) { var e = document.createElement(tag); if (cls) e.className = cls; e.textContent = text; box.appendChild(e); return e; }
    add("p", "deposit-amt", fmtInr(c.depositAmount));
    add("p", "", t("bk.depositDue", { amount: fmtInr(c.depositAmount) }));
    add("p", "", t("bk.depositHold"));
    if (c.depositPaymentLinkUrl && /^https?:\/\//i.test(c.depositPaymentLinkUrl)) {
      var a = add("a", "btn btn-primary", t("bk.payDeposit"));
      a.href = c.depositPaymentLinkUrl;
      a.target = "_blank";
      a.rel = "noopener noreferrer";
      add("p", "", t("bk.depositSent")).style.marginTop = "12px";
    } else {
      add("p", "", t("bk.depositNoLink"));
    }
  }

  function setText(selector, text) {
    var el = document.querySelector(selector);
    if (el) el.textContent = text;
  }

  // ---- Availability ----
  async function loadAvailabilityIfReady() {
    if (!state.svc || !locationId() || !state.artistId || !state.day) return;
    state.availabilityLoading = true;
    state.availabilitySlots = [];
    renderSlots();
    try {
      var url = apiBase + "/api/public/bookings/" + slug + "/availability?locationId=" + locationId() + "&serviceId=" + state.svc.id + "&date=" + state.day + "&employeeId=" + state.artistId;
      var res = await fetch(url);
      if (!res.ok) throw new Error("availability_failed");
      state.availabilitySlots = await res.json();
    } catch (e) {
      showError(t("bk.eAvail"));
    } finally {
      state.availabilityLoading = false;
      renderSlots();
    }
  }

  function gotoMonth(delta) {
    var next = new Date(state.calendarMonth.getFullYear(), state.calendarMonth.getMonth() + delta, 1);
    var real = new Date(); var realFirst = new Date(real.getFullYear(), real.getMonth(), 1);
    if (next < realFirst) return;
    state.calendarMonth = next; state.day = ""; state.slot = ""; state.availabilitySlots = [];
    renderAll();
  }

  // ---- Validation / navigation ----
  function validateStep(step) {
    if (step === 1 && !state.svc) return t("bk.eSelectService");
    if (step === 2 && (!state.artistId || !state.day || !state.slot)) return t("bk.eSelectSlot");
    if (step === 3) {
      state.form = getForm();
      if (!state.form.firstName) return t("bk.eFirstName");
      if (!state.form.mobile && !state.form.email) return t("bk.eContact");
      if (state.form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(state.form.email)) return t("bk.eEmail");
    }
    return "";
  }

  async function stepNext() {
    if (state.step >= 4 || state.submitting) return;
    var err = validateStep(state.step);
    if (err) { showError(err); return; }
    if (state.step === 3) { await submitBooking(); return; }
    state.step = Math.min(4, state.step + 1);
    showError(null);
    renderAll();
  }

  function stepBack() {
    state.step = Math.max(1, state.step - 1);
    showError(null);
    renderAll();
  }

  // Only boxes the guest ticked themselves are sent; nothing is ticked for them.
  function consentPurposes(form) {
    var list = [];
    if (form.consentWhatsApp) list.push("MarketingWhatsApp");
    if (form.consentEmailSms) { list.push("MarketingEmail"); list.push("MarketingSms"); }
    return list.length ? list : null;
  }

  async function submitBooking() {
    if (!state.svc || !locationId() || !state.artistId || !state.day || !state.slot) {
      showError(t("bk.eMissing"));
      return;
    }
    var form = state.form;
    state.submitting = true; showError(null); renderStepIndicators();
    try {
      var res = await fetch(apiBase + "/api/public/bookings/" + slug + "/appointments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          firstName: form.firstName, lastName: form.lastName || null,
          email: form.email || null, phone: form.mobile || null,
          locationId: locationId(), serviceId: state.svc.id, employeeId: state.artistId,
          startAtUtc: state.slot, notes: form.notes || null,
          language: LANG,
          consentPurposes: consentPurposes(form)
        })
      });
      if (!res.ok) {
        var raw = await res.text().catch(function () { return ""; });
        var message = raw;
        try { var parsed = JSON.parse(raw); message = typeof parsed === "string" ? parsed : (parsed && parsed.message) || raw; } catch (e) { /* plain text */ }
        state.submitting = false;
        showError(message || t("bk.errorFallback"));
        if (res.status === 409) { state.slot = ""; renderAll(); loadAvailabilityIfReady(); } else { renderStepIndicators(); }
        return;
      }
      state.confirmation = await res.json();
      state.submitting = false;
      state.step = 4;
      showError(null);
      renderAll();
    } catch (e) {
      state.submitting = false;
      showError(t("bk.eNetwork"));
      renderStepIndicators();
    }
  }

  // ---- Init ----
  function init() {
    var preselected = window.PRESELECTED_SERVICE_ID;
    if (preselected) {
      var btn = document.querySelector('[data-svc-id="' + preselected + '"]');
      if (btn) btn.click();
    }

    var nextBtn = document.querySelector("[data-book-next]");
    var backBtn = document.querySelector("[data-book-back]");
    if (nextBtn) nextBtn.addEventListener("click", stepNext);
    if (backBtn) backBtn.addEventListener("click", stepBack);

    var prevMonthBtn = document.querySelector("[data-prev-month]");
    var nextMonthBtn = document.querySelector("[data-next-month]");
    if (prevMonthBtn) prevMonthBtn.addEventListener("click", function () { gotoMonth(-1); });
    if (nextMonthBtn) nextMonthBtn.addEventListener("click", function () { gotoMonth(1); });

    renderAll();
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
  else init();
})();

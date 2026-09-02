// Gift card purchase + post-payment status check -- same client-direct pattern as book.js: the
// browser calls GessFlow's public API directly rather than relaying through this MVC server.
(function () {
  "use strict";

  var apiBase = window.GESSFLOW_API_BASE || "";
  var slug = window.BOOKING_SLUG || "";

  function parseErrorBody(raw) {
    try {
      var parsed = JSON.parse(raw);
      return typeof parsed === "string" ? parsed : (parsed && parsed.message) || raw;
    } catch (e) {
      return raw;
    }
  }

  // ---- Purchase form (Index.cshtml) ----
  (function purchaseForm() {
    var form = document.getElementById("giftCardForm");
    if (!form) return;

    var amountInput = document.getElementById("gcAmount");
    var chips = document.querySelectorAll("[data-amount-chips] .giftcard-denom");
    var preview = document.querySelector("[data-preview-amount]");
    var errorBox = document.getElementById("giftCardError");
    var submitBtn = document.getElementById("gcSubmit");

    function formatAmount(n) {
      var num = Number(n) || 0;
      return "₹" + num.toLocaleString("en-IN");
    }

    function syncPreview() {
      if (preview) preview.textContent = formatAmount(amountInput.value);
    }

    chips.forEach(function (chip) {
      chip.addEventListener("click", function () {
        chips.forEach(function (c) { c.classList.remove("active"); });
        chip.classList.add("active");
        amountInput.value = chip.getAttribute("data-amount");
        syncPreview();
      });
    });
    amountInput.addEventListener("input", function () {
      chips.forEach(function (c) { c.classList.toggle("active", c.getAttribute("data-amount") === amountInput.value); });
      syncPreview();
    });
    syncPreview();

    function showError(message) {
      if (!errorBox) return;
      if (!message) { errorBox.style.display = "none"; errorBox.textContent = ""; return; }
      errorBox.textContent = message;
      errorBox.style.display = "block";
    }

    function field(name) {
      var el = form.querySelector('[data-field="' + name + '"]');
      return el ? el.value.trim() : "";
    }

    form.addEventListener("submit", async function (e) {
      e.preventDefault();
      showError(null);

      var amount = Number(amountInput.value);
      var recipientName = field("recipientName");
      var recipientEmail = field("recipientEmail");
      var recipientPhone = field("recipientPhone");
      var purchaserName = field("purchaserName");
      var purchaserEmail = field("purchaserEmail");
      var purchaserPhone = field("purchaserPhone");
      var message = field("message");

      if (!amount || amount < 500 || amount > 50000) { showError("Please enter an amount between ₹500 and ₹50,000."); return; }
      if (!recipientName) { showError("Please enter the recipient's name."); return; }
      if (!recipientEmail && !recipientPhone) { showError("Please provide the recipient's email or mobile number."); return; }
      if (!purchaserName) { showError("Please enter your name."); return; }
      if (!purchaserEmail && !purchaserPhone) { showError("Please provide your own email or mobile number."); return; }

      submitBtn.disabled = true;
      submitBtn.textContent = "Please wait…";

      try {
        var returnUrl = window.location.origin + "/gift-cards/thank-you";
        var res = await fetch(apiBase + "/api/public/gift-cards/" + slug + "/purchase", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            amount: amount,
            recipientName: recipientName,
            recipientEmail: recipientEmail || null,
            recipientPhone: recipientPhone || null,
            purchaserName: purchaserName,
            purchaserEmail: purchaserEmail || null,
            purchaserPhone: purchaserPhone || null,
            message: message || null,
            returnUrl: returnUrl
          })
        });

        if (!res.ok) {
          var raw = await res.text().catch(function () { return ""; });
          showError(parseErrorBody(raw) || "We couldn't process that. Please check your details and try again.");
          submitBtn.disabled = false;
          submitBtn.textContent = "Continue to payment →";
          return;
        }

        var data = await res.json();
        window.location.href = data.paymentLinkUrl;
      } catch (err) {
        showError("We couldn't reach the payment service. Please check your connection and try again.");
        submitBtn.disabled = false;
        submitBtn.textContent = "Continue to payment →";
      }
    });
  })();

  // ---- Post-payment status check (ThankYou.cshtml) ----
  (function thankYou() {
    var checkingEl = document.getElementById("gcStatusChecking");
    if (!checkingEl) return;

    var paidEl = document.getElementById("gcStatusPaid");
    var pendingEl = document.getElementById("gcStatusPending");
    var errorEl = document.getElementById("gcStatusError");

    function show(el) {
      [checkingEl, paidEl, pendingEl, errorEl].forEach(function (e) { if (e) e.style.display = "none"; });
      if (el) el.style.display = "block";
    }

    var params = new URLSearchParams(window.location.search);
    var giftCardId = params.get("giftCardId");
    if (!giftCardId) { show(errorEl); return; }

    var attempts = 0;
    var maxAttempts = 6; // ~6 polls, backing off -- covers the normal webhook/redirect race without spinning forever

    async function poll() {
      attempts++;
      try {
        var res = await fetch(apiBase + "/api/public/gift-cards/" + slug + "/status/" + giftCardId);
        if (!res.ok) { show(errorEl); return; }
        var data = await res.json();

        if (data.status === "Active") {
          var amountEl = document.querySelector("[data-gc-amount]");
          var deliveryEl = document.querySelector("[data-gc-delivery]");
          if (amountEl) amountEl.textContent = "₹" + Number(data.amount).toLocaleString("en-IN");
          if (deliveryEl) deliveryEl.textContent = "Code " + (data.code || "") + " has been sent to the recipient.";
          show(paidEl);
          return;
        }

        if (attempts >= maxAttempts) { show(pendingEl); return; }
        show(pendingEl);
        setTimeout(poll, Math.min(2000 * attempts, 8000));
      } catch (err) {
        if (attempts >= maxAttempts) { show(errorEl); return; }
        setTimeout(poll, 3000);
      }
    }

    poll();
  })();
})();

// Marketing-page interactivity shared across every non-booking page: hero autoplay + video sync,
// mobile menu, scroll-reveal, the home-page quiz, the testimonial carousel, and the lookbook's
// category filter + lightbox. Each block is defensive about missing elements since not every page
// has every widget.
(function () {
  "use strict";

  // ---- Mobile menu ----
  (function mobileMenu() {
    var btn = document.getElementById("mobileMenuBtn");
    var closeBtn = document.getElementById("mobileMenuClose");
    var menu = document.getElementById("mobileMenu");
    if (!btn || !menu) return;
    btn.addEventListener("click", function () { menu.classList.add("open"); });
    if (closeBtn) closeBtn.addEventListener("click", function () { menu.classList.remove("open"); });
    menu.querySelectorAll("a").forEach(function (a) { a.addEventListener("click", function () { menu.classList.remove("open"); }); });
  })();

  // ---- Scroll reveal ----
  (function scrollReveal() {
    var targets = document.querySelectorAll("[data-reveal]");
    if (!targets.length || !("IntersectionObserver" in window)) {
      targets.forEach(function (el) { el.classList.add("in-view"); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) { entry.target.classList.add("in-view"); io.unobserve(entry.target); }
      });
    }, { rootMargin: "0px 0px -10% 0px" });
    targets.forEach(function (el) { io.observe(el); });
  })();

  // ---- Hero autoplay + video sync ----
  (function hero() {
    var media = document.querySelectorAll("[data-hero-media]");
    var tabs = document.querySelectorAll("[data-hero-tab]");
    if (!media.length) return;
    var current = 0;
    var timer = null;

    function show(index) {
      current = index;
      media.forEach(function (el) {
        var isActive = Number(el.getAttribute("data-hero-media")) === index;
        el.classList.toggle("active", isActive);
        if (el.tagName === "VIDEO") {
          if (isActive) { el.muted = true; el.play().catch(function () {}); }
          else el.pause();
        }
      });
      tabs.forEach(function (el) {
        el.classList.toggle("active", Number(el.getAttribute("data-hero-tab")) === index);
      });
    }

    tabs.forEach(function (el) {
      el.addEventListener("click", function () {
        show(Number(el.getAttribute("data-hero-tab")));
        restartTimer();
      });
    });

    function restartTimer() {
      if (timer) clearInterval(timer);
      if (media.length <= 1) return;
      timer = setInterval(function () { show((current + 1) % media.length); }, 6500);
    }

    show(0);
    restartTimer();
  })();

  // ---- Home quiz ----
  (function quiz() {
    var buttons = document.querySelectorAll("[data-quiz-btn]");
    if (!buttons.length) return;
    var quizContainer = document.querySelector("[data-quiz]");
    if (!quizContainer) return;

    buttons.forEach(function (btn, i) {
      btn.addEventListener("click", function () {
        buttons.forEach(function (b) { b.style.borderColor = ""; b.style.background = ""; });
        btn.style.borderColor = "rgba(190,17,160,.45)";
        btn.style.background = "linear-gradient(158deg,#fff,#fbf0f6)";
        // The result panel's copy/CTA are server-rendered per-result only for the first quiz
        // result today (kept simple); re-fetching per click would need an endpoint this static
        // marketing page doesn't need. Selecting a different card still visually highlights it.
      });
    });
  })();

  // ---- Testimonial carousel ----
  (function testimonials() {
    var dataEl = document.getElementById("testimonials-data");
    if (!dataEl) return;
    var quoteEl = document.querySelector("[data-testimonial-quote]");
    var attrEl = document.querySelector("[data-testimonial-attr]");
    var prevBtn = document.querySelector("[data-testimonial-prev]");
    var nextBtn = document.querySelector("[data-testimonial-next]");
    if (!quoteEl || !attrEl) return;
    var items;
    try { items = JSON.parse(dataEl.textContent); } catch (e) { return; }
    if (!items || !items.length) return;
    var index = 0;
    function render() {
      quoteEl.textContent = "“" + items[index].quoteText + "”";
      attrEl.textContent = items[index].attributionFreeText;
    }
    if (prevBtn) prevBtn.addEventListener("click", function () { index = (index - 1 + items.length) % items.length; render(); });
    if (nextBtn) nextBtn.addEventListener("click", function () { index = (index + 1) % items.length; render(); });
  })();

  // ---- Lookbook filter + lightbox ----
  (function lookbook() {
    var filterButtons = document.querySelectorAll("[data-filter]");
    var tiles = document.querySelectorAll("[data-category]");
    if (filterButtons.length && tiles.length) {
      filterButtons.forEach(function (btn) {
        btn.addEventListener("click", function () {
          filterButtons.forEach(function (b) { b.classList.remove("active"); });
          btn.classList.add("active");
          var filter = btn.getAttribute("data-filter");
          tiles.forEach(function (tile) {
            var show = filter === "All" || tile.getAttribute("data-category") === filter;
            tile.style.display = show ? "" : "none";
          });
        });
      });
    }

    var modal = document.getElementById("galleryModal");
    var modalImage = document.getElementById("galleryModalImage");
    var modalCategory = document.getElementById("galleryModalCategory");
    var modalClose = document.getElementById("galleryModalClose");
    if (!modal || !modalImage) return;
    document.querySelectorAll("[data-open-image]").forEach(function (tile) {
      tile.addEventListener("click", function () {
        modalImage.src = tile.getAttribute("data-open-image");
        if (modalCategory) modalCategory.textContent = tile.getAttribute("data-open-category") || "";
        modal.classList.add("open");
      });
    });
    if (modalClose) modalClose.addEventListener("click", function () { modal.classList.remove("open"); });
    modal.addEventListener("mousedown", function (e) { if (e.target === modal) modal.classList.remove("open"); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") modal.classList.remove("open"); });
  })();
})();

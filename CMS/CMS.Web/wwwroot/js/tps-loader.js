/*
 * TPS request loader: a slim progress bar at the top of the window.
 * Replaces the full-screen jquery-loading-overlay that ran on "beforeunload".
 *
 * Behaviour
 *  - Tracks page navigations (links, form submits, form.submit()), jQuery AJAX and fetch.
 *  - Counts concurrent requests; the bar finishes only when the last one completes.
 *  - Requests that finish within SHOW_DELAY never show the bar (no flash).
 *  - Once shown it stays at least MIN_VISIBLE so it never blinks.
 *  - Follows RTL via logical CSS.
 *  - File downloads, new-tab links, in-page anchors and cancelled submits are ignored.
 *
 * Preventing repeated actions
 *  - While a form submit / navigation or a mutating request (POST, PUT, PATCH, DELETE via fetch or
 *    jQuery AJAX) is in progress the page is locked: a transparent shield catches clicks at once,
 *    and after BLOCK_VISIBLE_DELAY it dims the page and shows a large centred spinner.
 *  - The submitted form is marked busy: its submit buttons are disabled, the pressed one shows a
 *    spinner, and any further submit of it (click, Enter, form.submit()) is ignored.
 *  - Everything is released when the page is restored from the back/forward cache, the request
 *    completes, or the NAV_TIMEOUT safety net fires.
 *
 * API (window.TPS.loader): start(), done(), reset(), track(promise), block(), unblock()
 * Legacy helpers showLoading()/hideLoading() map to start()/done().
 * Opt out per element with data-no-loader, or per fetch / $.ajax with { tpsLoader: false }.
 */
(function () {
    "use strict";

    var SHOW_DELAY = 120;      // ms before the bar appears
    var MIN_VISIBLE = 300;     // ms the bar stays once visible
    var NAV_TIMEOUT = 20000;   // safety net if a navigation never completes (e.g. download)
    var IGNORED_URLS = [/notifications/i];   // background polling
    var BLOCK_VISIBLE_DELAY = 200;           // ms before the spinner shows (quick requests never flash it)

    var active = 0;
    var bar = null;
    var fill = null;
    var showTimer = null;
    var hideTimer = null;
    var trickleTimer = null;
    var navTimer = null;
    var shownAt = 0;
    var progress = 0;

    function ensureBar() {
        if (bar) return bar;
        bar = document.createElement("div");
        bar.className = "tps-loader";
        bar.setAttribute("aria-hidden", "true");
        fill = document.createElement("div");
        fill.className = "tps-loader__bar";
        bar.appendChild(fill);
        (document.body || document.documentElement).appendChild(bar);
        return bar;
    }

    function setProgress(value) {
        progress = value;
        if (fill) fill.style.transform = "scaleX(" + value + ")";
    }

    function trickle() {
        // Ease towards 90% but never reach it until the work is done
        if (progress < 0.9) setProgress(progress + (0.9 - progress) * 0.08 + 0.004);
    }

    function reveal() {
        showTimer = null;
        clearTimeout(hideTimer);
        ensureBar();
        bar.classList.remove("is-done");
        bar.classList.add("is-active");
        setProgress(0.06);
        // next frame so the width animates from the start
        requestAnimationFrame(function () { if (progress < 0.3) setProgress(0.3); });
        shownAt = Date.now();
        clearInterval(trickleTimer);
        trickleTimer = setInterval(trickle, 260);
        document.documentElement.setAttribute("aria-busy", "true");
    }

    function finish() {
        clearInterval(trickleTimer);
        trickleTimer = null;
        setProgress(1);
        bar.classList.add("is-done");
        hideTimer = setTimeout(function () {
            bar.classList.remove("is-active", "is-done");
            setProgress(0);
            document.documentElement.removeAttribute("aria-busy");
        }, 320);
    }

    function start() {
        active++;
        if (active !== 1) return;

        clearTimeout(hideTimer);
        if (bar && bar.classList.contains("is-active") && !bar.classList.contains("is-done")) return;
        if (!showTimer) showTimer = setTimeout(reveal, SHOW_DELAY);
    }

    function done() {
        active = Math.max(0, active - 1);
        if (active > 0) return;

        if (showTimer) {            // finished before the bar appeared
            clearTimeout(showTimer);
            showTimer = null;
            return;
        }
        if (!bar || !bar.classList.contains("is-active")) return;

        var wait = Math.max(0, MIN_VISIBLE - (Date.now() - shownAt));
        clearTimeout(hideTimer);
        hideTimer = setTimeout(finish, wait);
    }

    function reset() {
        active = 0;
        clearTimeout(navTimer);
        navTimer = null;
        if (showTimer) { clearTimeout(showTimer); showTimer = null; }
        if (bar && bar.classList.contains("is-active")) finish();
    }

    function track(promise) {
        start();
        return Promise.resolve(promise).finally(done);
    }

    /* ------------------------------------------------------------
     * Page lock: stops repeated clicks and submits while work is in progress
     * ---------------------------------------------------------- */
    var blocking = 0;
    var shield = null;
    var shieldTimer = null;

    function ensureShield() {
        if (shield) return shield;
        shield = document.createElement("div");
        shield.className = "tps-blocker";
        shield.innerHTML =
            '<div class="tps-blocker__loader" role="status">' +
            '<span class="tps-blocker__spinner" aria-hidden="true"></span>' +
            '<span class="visually-hidden">Loading…</span></div>';
        (document.body || document.documentElement).appendChild(shield);
        return shield;
    }

    function block() {
        blocking++;
        if (blocking !== 1) return;

        ensureShield().classList.add("is-active");
        // Stop a held Enter key or a focused button from firing again
        if (document.activeElement && document.activeElement !== document.body && document.activeElement.blur)
            document.activeElement.blur();
        clearTimeout(shieldTimer);
        shieldTimer = setTimeout(function () { shield.classList.add("is-visible"); }, BLOCK_VISIBLE_DELAY);
    }

    function unblock(all) {
        blocking = all ? 0 : Math.max(0, blocking - 1);
        if (blocking > 0) return;

        clearTimeout(shieldTimer);
        shieldTimer = null;
        if (shield) shield.classList.remove("is-active", "is-visible");
        unlockForms();
    }

    var SUBMIT_CONTROLS = 'button:not([type]), button[type="submit"], input[type="submit"], input[type="image"]';

    function submitControls(form) {
        var controls = Array.prototype.slice.call(form.querySelectorAll(SUBMIT_CONTROLS));
        if (form.id) {
            var outside = document.querySelectorAll('[form="' + form.id.replace(/"/g, '\\"') + '"]');
            for (var i = 0; i < outside.length; i++)
                if (outside[i].matches(SUBMIT_CONTROLS) && controls.indexOf(outside[i]) < 0) controls.push(outside[i]);
        }
        return controls;
    }

    // Called after the submit event has finished, so the submitter's name/value is already in the request
    function lockForm(form, submitter) {
        if (!form || form.hasAttribute("data-tps-busy")) return;
        form.setAttribute("data-tps-busy", "");
        form.setAttribute("aria-busy", "true");

        submitControls(form).forEach(function (control) {
            if (control.disabled) return;
            control.disabled = true;
            control.setAttribute("data-tps-locked", "");
        });
        // <input> buttons can't render the ::after spinner; they just stay disabled
        if (submitter && submitter.tagName === "BUTTON") submitter.classList.add("tps-btn-busy");
    }

    function unlockForms() {
        document.querySelectorAll("form[data-tps-busy]").forEach(function (form) {
            form.removeAttribute("data-tps-busy");
            form.removeAttribute("aria-busy");
        });
        document.querySelectorAll("[data-tps-locked]").forEach(function (control) {
            control.disabled = false;
            control.removeAttribute("data-tps-locked");
        });
        document.querySelectorAll(".tps-btn-busy").forEach(function (control) {
            control.classList.remove("tps-btn-busy");
        });
    }

    // Capture phase: runs before page scripts, so a second submit never reaches them
    document.addEventListener("submit", function (event) {
        if (blocking > 0 || (event.target && event.target.hasAttribute && event.target.hasAttribute("data-tps-busy"))) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);

    // Keyboard activation (Enter / Space on a focused control) while locked
    document.addEventListener("click", function (event) {
        if (blocking === 0) return;
        if (event.target && event.target.closest && event.target.closest("a, button, input, select, label, [role=button], [onclick]")) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);

    /* ------------------------------------------------------------
     * Navigations
     * ---------------------------------------------------------- */
    var navigating = false;

    function startNavigation() {
        if (navigating) return;
        navigating = true;
        start();
        block();
        clearTimeout(navTimer);
        // The page normally unloads first. If it doesn't (download, 204, blocked), stop.
        navTimer = setTimeout(function () { navigating = false; reset(); unblock(true); }, NAV_TIMEOUT);
    }

    function isIgnoredUrl(url) {
        for (var i = 0; i < IGNORED_URLS.length; i++) if (IGNORED_URLS[i].test(url)) return true;
        return false;
    }

    function looksLikeDownload(url) {
        return /\/download\b|\/attachments\/|export|\.(pdf|docx?|xlsx?|csv|zip|png|jpe?g)(\?|#|$)/i.test(url);
    }

    function shouldTrackLink(a, event) {
        if (!a || a.hasAttribute("data-no-loader") || a.hasAttribute("download")) return false;
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
        var target = (a.getAttribute("target") || "").toLowerCase();
        if (target && target !== "_self") return false;

        var href = a.getAttribute("href");
        if (!href || href.charAt(0) === "#" || /^(javascript|mailto|tel|sms):/i.test(href)) return false;
        // Links that open UI (dropdowns, tabs, modals) rather than navigate; tooltips still navigate
        if (/^(dropdown|tab|pill|collapse|modal|offcanvas)$/i.test(a.getAttribute("data-bs-toggle") || "")) return false;

        var url;
        try { url = new URL(a.href, location.href); } catch (e) { return false; }
        if (url.origin !== location.origin) return false;
        if (url.pathname === location.pathname && url.search === location.search && url.hash) return false;
        return !looksLikeDownload(url.pathname + url.search);
    }

    document.addEventListener("click", function (event) {
        var a = event.target.closest ? event.target.closest("a[href]") : null;
        if (!shouldTrackLink(a, event)) return;
        // Let page handlers run first; skip if one of them cancelled the navigation
        setTimeout(function () { if (!event.defaultPrevented) startNavigation(); }, 0);
    });

    function shouldTrackForm(form, submitter) {
        if (!form || form.hasAttribute("data-no-loader")) return false;
        var target = ((submitter && submitter.getAttribute("formtarget")) || form.getAttribute("target") || "").toLowerCase();
        if (target && target !== "_self") return false;
        var action = (submitter && submitter.getAttribute("formaction")) || form.getAttribute("action") || location.href;
        return !looksLikeDownload(action);
    }

    document.addEventListener("submit", function (event) {
        var form = event.target;
        var submitter = event.submitter;
        if (!shouldTrackForm(form, submitter)) return;
        setTimeout(function () {
            if (event.defaultPrevented) return;    // validation failed or a confirmation dialog is open
            lockForm(form, submitter);
            startNavigation();
        }, 0);
    });

    // form.submit() skips the submit event (used after SweetAlert confirmations)
    var nativeSubmit = HTMLFormElement.prototype.submit;
    HTMLFormElement.prototype.submit = function () {
        if (this.hasAttribute("data-tps-busy")) return;     // already submitting
        if (shouldTrackForm(this)) {
            var form = this;
            var result = nativeSubmit.apply(form, arguments);
            lockForm(form, null);
            startNavigation();
            return result;
        }
        return nativeSubmit.apply(this, arguments);
    };

    // Coming back via the back/forward cache: clear any bar and lock left from before
    window.addEventListener("pageshow", function () { navigating = false; reset(); unblock(true); });

    /* ------------------------------------------------------------
     * fetch and jQuery AJAX
     * ---------------------------------------------------------- */
    function isMutating(method) {
        return !/^(GET|HEAD|OPTIONS)$/i.test(method || "GET");
    }

    if (window.fetch) {
        var nativeFetch = window.fetch;
        window.fetch = function (input, init) {
            var url = typeof input === "string" ? input : (input && input.url) || "";
            if ((init && init.tpsLoader === false) || isIgnoredUrl(url))
                return nativeFetch.apply(this, arguments);

            var method = (init && init.method) || (input && typeof input !== "string" && input.method) || "GET";
            if (!isMutating(method))
                return track(nativeFetch.apply(this, arguments));

            block();
            return track(nativeFetch.apply(this, arguments)).finally(function () { unblock(); });
        };
    }

    function hookJQuery() {
        var $ = window.jQuery;
        if (!$ || $.__tpsLoaderHooked) return;
        $.__tpsLoaderHooked = true;
        $(document).on("ajaxSend", function (e, xhr, settings) {
            if (settings && (settings.tpsLoader === false || isIgnoredUrl(settings.url || ""))) return;
            xhr.__tpsTracked = true;
            start();
            if (isMutating(settings && (settings.type || settings.method))) {
                xhr.__tpsBlocking = true;
                block();
            }
        });
        $(document).on("ajaxComplete", function (e, xhr) {
            if (xhr && xhr.__tpsTracked) { xhr.__tpsTracked = false; done(); }
            if (xhr && xhr.__tpsBlocking) { xhr.__tpsBlocking = false; unblock(); }
        });
    }

    hookJQuery();
    document.addEventListener("DOMContentLoaded", hookJQuery);

    /* ------------------------------------------------------------
     * Public API + legacy helpers used by older scripts
     * ---------------------------------------------------------- */
    window.TPS = window.TPS || {};
    window.TPS.loader = { start: start, done: done, reset: reset, track: track, block: block, unblock: function () { unblock(); } };
    window.showLoading = start;
    window.hideLoading = done;
})();

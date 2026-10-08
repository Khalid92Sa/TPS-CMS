/*
 * TPS shared UI behaviour. Loaded at the end of <body> on every page.
 *  1. Theme switcher (Light / Dark / System)
 *  2. Searchable selects (Tom Select) applied to form selects
 *  3. Tidy table action columns
 *  4. Full screen that survives page changes
 */
(function () {
    "use strict";

    /* ---------------------------------------------------------------
     * 1. Theme
     * ------------------------------------------------------------- */
    var THEME_KEY = "tps-theme";
    var html = document.documentElement;
    var darkQuery = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;

    function readPref() {
        try { return localStorage.getItem(THEME_KEY) || "light"; } catch (e) { return "light"; }
    }

    function resolve(pref) {
        return pref === "dark" || (pref === "system" && darkQuery && darkQuery.matches) ? "dark" : "light";
    }

    function applyTheme(pref, animate) {
        var resolved = resolve(pref);

        if (animate) {
            html.classList.add("tps-theme-switching");
            window.setTimeout(function () { html.classList.remove("tps-theme-switching"); }, 320);
        }

        html.setAttribute("data-bs-theme", resolved);
        html.setAttribute("data-tps-theme-pref", pref);

        try {
            localStorage.setItem(THEME_KEY, pref);
            // Keep Velzon's session copy in step (see tps-theme-init.js).
            sessionStorage.setItem("data-bs-theme", resolved);
            var stored = sessionStorage.getItem("defaultAttribute");
            if (stored) {
                var attrs = JSON.parse(stored);
                delete attrs["data-bs-theme"];
                delete attrs["data-tps-theme-pref"];
                attrs["data-bs-theme"] = resolved;
                attrs["data-tps-theme-pref"] = pref;
                sessionStorage.setItem("defaultAttribute", JSON.stringify(attrs));
            }
        } catch (e) { /* storage blocked: theme still applies for this page */ }

        syncThemeControls(pref, resolved);
        window.dispatchEvent(new CustomEvent("tps:themechange", { detail: { pref: pref, theme: resolved } }));
    }

    function syncThemeControls(pref, resolved) {
        document.querySelectorAll("[data-tps-theme-set]").forEach(function (el) {
            var on = el.getAttribute("data-tps-theme-set") === pref;
            el.classList.toggle("active", on);
            el.setAttribute("aria-checked", on ? "true" : "false");
        });
        // The toggle shows the mode a click switches to: moon in light mode, sun in dark mode.
        document.querySelectorAll("[data-tps-theme-icon]").forEach(function (el) {
            el.classList.remove("ri-sun-line", "ri-moon-clear-line");
            el.classList.add(resolved === "dark" ? "ri-sun-line" : "ri-moon-clear-line");
        });
        document.querySelectorAll("[data-tps-theme-toggle]").forEach(function (el) {
            var label = resolved === "dark" ? "Switch to light mode" : "Switch to dark mode";
            el.setAttribute("aria-label", label);
            el.setAttribute("title", label);
        });
    }

    document.addEventListener("click", function (e) {
        var set = e.target.closest("[data-tps-theme-set]");
        if (set) {
            e.preventDefault();
            applyTheme(set.getAttribute("data-tps-theme-set"), true);
            return;
        }
        var toggle = e.target.closest("[data-tps-theme-toggle]");
        if (toggle) {
            e.preventDefault();
            applyTheme(resolve(readPref()) === "dark" ? "light" : "dark", true);
        }
    });

    if (darkQuery) {
        var onSystemChange = function () { if (readPref() === "system") applyTheme("system", true); };
        if (darkQuery.addEventListener) darkQuery.addEventListener("change", onSystemChange);
        else if (darkQuery.addListener) darkQuery.addListener(onSystemChange);
    }

    // Another tab changed the theme
    window.addEventListener("storage", function (e) {
        if (e.key === THEME_KEY && e.newValue) applyTheme(e.newValue, true);
    });

    syncThemeControls(readPref(), resolve(readPref()));

    /* ---------------------------------------------------------------
     * 2. Searchable selects
     *    Every single/multi <select> inside the app gets the same Tom Select
     *    control. Lists longer than SEARCH_THRESHOLD get a search box.
     *    Opt out with data-native (or class "tps-native").
     * ------------------------------------------------------------- */
    var SEARCH_THRESHOLD = 7;

    function shouldEnhance(select) {
        if (select.tomselect || select.hasAttribute("data-native") || select.classList.contains("tps-native"))
            return false;
        if (select.classList.contains("selectized") || select.classList.contains("tomselected"))
            return false;
        if (select.closest(".dataTables_length, .pagination-container, .swal2-container, .ts-wrapper"))
            return false;
        if (select.size > 1 && !select.multiple)
            return false;
        return select.classList.contains("form-control")
            || select.classList.contains("form-select")
            || select.hasAttribute("data-searchable");
    }

    /* Interview status mapping. Mirrors CMS.Web.Helpers.InterviewStatusUi (C#);
       colors come from the --tps-status-* tokens in tps-design.css. */
    function statusKind(text) {
        var v = String(text || "").trim().toLowerCase();
        if (!v) return null;
        if (v === "pnd" || v.indexOf("pend") === 0) return "pending";
        if (v === "apr" || v.indexOf("approv") === 0 || v.indexOf("accept") === 0) return "approved";
        if (v === "rej" || v.indexOf("reject") === 0) return "rejected";
        if (v === "hold" || v.indexOf("hold") !== -1) return "hold";
        return null;
    }

    function isStatusSelect(select) {
        return /status/i.test((select.name || "") + " " + (select.id || "")) || select.hasAttribute("data-status-select");
    }

    function textDirAttr(text) {
        // Arabic/Hebrew text renders right-to-left inside an otherwise LTR control
        return /[֐-ࣿ]/.test(text || "") ? ' dir="rtl"' : ' dir="auto"';
    }

    function enhanceSelect(select) {
        if (!window.TomSelect || !shouldEnhance(select)) return;

        var realOptions = Array.prototype.filter.call(select.options, function (o) { return o.value !== ""; });
        var searchable = select.hasAttribute("data-searchable") || realOptions.length > SEARCH_THRESHOLD;
        var placeholderOption = Array.prototype.find.call(select.options, function (o) { return o.value === ""; });
        var inModal = select.closest(".modal");
        var statusDots = isStatusSelect(select);

        function label(data, escape) {
            var kind = statusDots ? statusKind(data.text) : null;
            var dot = kind ? '<span class="tps-status-dot tps-status-dot--' + kind + '" aria-hidden="true"></span>' : "";
            return "<div" + textDirAttr(data.text) + ">" + dot + escape(data.text) + "</div>";
        }

        var plugins = [];
        if (select.multiple) plugins.push("remove_button");
        if (searchable && !select.multiple) plugins.push("dropdown_input");

        var settings = {
            plugins: plugins,
            allowEmptyOption: true,
            create: false,
            maxOptions: null,
            hidePlaceholder: false,
            placeholder: placeholderOption ? placeholderOption.text : (select.getAttribute("placeholder") || "Select…"),
            // Inside a modal, leave the list on the control. Tom Select only positions
            // dropdowns parented to body; anywhere else it uses top:100%, which lands
            // at the bottom of the dialog when the list is moved onto .modal-content.
            dropdownParent: inModal ? null : "body",
            searchField: ["text"],
            diacritics: true,
            render: {
                option: label,
                item: label,
                no_results: function (data, escape) {
                    return '<div class="no-results">No matches for “' + escape(data.input) + '”</div>';
                }
            },
            onInitialize: function () {
                this.wrapper.classList.add("tps-select");
                if (searchable) this.wrapper.classList.add("tps-select--searchable");
            }
        };

        // Short lists: pick only, no typing
        if (!searchable && !select.multiple) settings.controlInput = null;

        var ts;
        try { ts = new TomSelect(select, settings); } catch (e) { return; }

        // Tom Select puts its control right after the <select>. Move the (hidden) select after
        // the control so page scripts using $(select).next(".text-danger") still find the message.
        if (ts.wrapper && ts.wrapper.parentNode === select.parentNode && ts.wrapper.previousSibling === select) {
            ts.wrapper.parentNode.insertBefore(select, ts.wrapper.nextSibling);
        }

        // Keep the control in step when page scripts change the underlying <select>
        // (rebuilding options, disabling it, or setting a value and raising "change").
        // Mutations Tom Select makes itself while applying a pick are ignored.
        var ownChange = false;
        ts.on("change", function () {
            ownChange = true;
            window.setTimeout(function () { ownChange = false; }, 0);
        });

        var observer;
        var observeOptions = { childList: true, attributes: true, attributeFilter: ["disabled"] };
        function resync() {
            observer.disconnect();
            try {
                ts.clearOptions();
                ts.sync();
                if (select.disabled) ts.disable(); else ts.enable();
            } finally {
                observer.observe(select, observeOptions);
            }
        }

        observer = new MutationObserver(function (mutations) {
            if (ownChange) return;
            for (var i = 0; i < mutations.length; i++) {
                var m = mutations[i];
                if (m.type === "childList" || m.attributeName === "disabled") { resync(); return; }
            }
        });
        observer.observe(select, observeOptions);

        select.addEventListener("change", function () {
            // A change raised by Tom Select already matches; one raised by page code may not.
            var current = select.multiple
                ? Array.prototype.filter.call(select.options, function (o) { return o.selected; }).map(function (o) { return o.value; })
                : select.value;
            var shown = ts.getValue();
            if (JSON.stringify(current) !== JSON.stringify(shown)) resync();
        });

        // Forms reset with type="reset"
        if (select.form) {
            select.form.addEventListener("reset", function () { window.setTimeout(resync, 0); });
        }
    }

    function enhanceAll(root) {
        (root || document).querySelectorAll("select").forEach(enhanceSelect);
    }

    /* ---------------------------------------------------------------
     * 3. Table action columns: drop the "|" separators between icon links
     *    and group the links so they align and wrap consistently.
     * ------------------------------------------------------------- */
    function tidyActionCells(root) {
        (root || document).querySelectorAll("table td").forEach(function (td) {
            if (td.classList.contains("tps-actions-cell")) return;
            var icons = td.querySelectorAll(':scope > a[class*="bx"], :scope > a[class*="ri-"]');
            if (icons.length < 2) return;

            Array.prototype.slice.call(td.childNodes).forEach(function (node) {
                if (node.nodeType === 3 && /^[\s|]*$/.test(node.nodeValue)) td.removeChild(node);
            });

            var group = document.createElement("div");
            group.className = "tps-actions";
            Array.prototype.slice.call(td.childNodes).forEach(function (node) { group.appendChild(node); });
            td.appendChild(group);
            td.classList.add("tps-actions-cell");

            // Name the icon-only links for screen readers from their tooltip text
            group.querySelectorAll("a").forEach(function (a) {
                var label = a.getAttribute("data-bs-original-title") || a.getAttribute("title");
                if (label && !a.getAttribute("aria-label")) a.setAttribute("aria-label", label);
            });
        });
    }

    /* ---------------------------------------------------------------
     * 4. Full screen that survives page changes
     * A browser always leaves full screen when the page that entered it unloads.
     * So the page that enters full screen stays loaded as a "shell" and shows the
     * app in a frame covering the whole screen. Links and forms navigate inside the
     * frame, so full screen is never interrupted. Leaving full screen (Esc or the
     * button) removes the frame and opens the page the user had reached.
     * ------------------------------------------------------------- */
    var FS_EXIT_MSG = "tps-fullscreen-exit";
    var shellFrame = null;
    var shellPagesLoaded = 0;

    // True when this page is running inside the full-screen frame of a shell page.
    var inFsFrame = false;
    try {
        inFsFrame = window.self !== window.top &&
            window.parent.document.documentElement.hasAttribute("data-tps-fs-shell");
    } catch (e) { /* different origin: not our frame */ }

    function fsElement() {
        return document.fullscreenElement || document.webkitFullscreenElement || document.mozFullScreenElement || null;
    }

    function exitFs() {
        var exit = document.exitFullscreen || document.webkitExitFullscreen || document.mozCancelFullScreen;
        if (!exit) return;
        try {
            var result = exit.call(document);
            if (result && result.catch) result.catch(function () { });
        } catch (e) { }
    }

    function enterShell() {
        var html = document.documentElement;
        var request = html.requestFullscreen || html.webkitRequestFullscreen || html.mozRequestFullScreen;
        if (!request) return;

        var pending;
        try { pending = request.call(html); } catch (e) { return; }

        html.setAttribute("data-tps-fs-shell", "");
        shellPagesLoaded = 0;
        shellFrame = document.createElement("iframe");
        shellFrame.title = document.title;
        shellFrame.src = location.href;
        shellFrame.setAttribute("style",
            "position:fixed;inset:0;width:100%;height:100%;border:0;margin:0;padding:0;" +
            "z-index:2147483647;background:var(--tps-bg, #f3f5f9);");
        shellFrame.addEventListener("load", function () {
            shellPagesLoaded++;
            // Keep the address bar and tab title on the page shown in the frame,
            // so a refresh or bookmark lands on the same page.
            try {
                var w = shellFrame.contentWindow;
                history.replaceState(history.state, "", w.location.href);
                document.title = w.document.title;
            } catch (e) { }
        });
        document.body.style.overflow = "hidden";
        document.body.appendChild(shellFrame);

        if (pending && pending.catch) pending.catch(function () { leaveShell(); });
    }

    function leaveShell() {
        if (!shellFrame) return;
        var url = null;
        try { url = shellFrame.contentWindow.location.href; } catch (e) { }

        document.documentElement.removeAttribute("data-tps-fs-shell");
        if (url && shellPagesLoaded > 1) {
            // The user moved to other pages while in full screen: open where they are now.
            window.location.replace(url);
            return;
        }
        shellFrame.remove();
        shellFrame = null;
        document.body.style.overflow = "";
    }

    function initFullscreen() {
        try { sessionStorage.removeItem("tps-fullscreen"); } catch (e) { } // from the earlier approach

        if (inFsFrame) document.body.classList.add("fullscreen-enable"); // button shows "exit full screen"

        // Runs before Velzon's own handler on the button and replaces it.
        document.addEventListener("click", function (e) {
            var button = e.target.closest && e.target.closest('[data-toggle="fullscreen"]');
            if (!button) return;
            e.preventDefault();
            e.stopPropagation();

            if (inFsFrame) window.parent.postMessage(FS_EXIT_MSG, window.location.origin);
            else if (fsElement()) exitFs();
            else enterShell();
        }, true);

        window.addEventListener("message", function (e) {
            if (e.origin === window.location.origin && e.data === FS_EXIT_MSG) exitFs();
        });

        ["fullscreenchange", "webkitfullscreenchange", "mozfullscreenchange"].forEach(function (name) {
            document.addEventListener(name, function () {
                if (fsElement()) document.body.classList.add("fullscreen-enable");
                else {
                    document.body.classList.remove("fullscreen-enable");
                    leaveShell();
                }
            });
        });
    }

    function init() {
        enhanceAll();
        tidyActionCells();
        initFullscreen();
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
    else init();

    window.TPS = window.TPS || {};
    window.TPS.enhanceSelects = enhanceAll;
    window.TPS.tidyActionCells = tidyActionCells;
    window.TPS.setTheme = function (pref) { applyTheme(pref, true); };
    window.TPS.statusKind = statusKind;
})();

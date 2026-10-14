/*
 * TPS shared UI behaviour. Loaded at the end of <body> on every page.
 *  1. Theme switcher (Light / Dark / System) and brand color picker
 *  2. Searchable selects (Tom Select) applied to form selects
 *  3. Tidy table action columns
 *  4. Full screen that survives page changes
 *  5. Sidebar scroll position that survives page changes
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

    function applyTheme(pref) {
        var resolved = resolve(pref);

        html.setAttribute("data-bs-theme", resolved);
        html.setAttribute("data-tps-theme-pref", pref);

        try {
            localStorage.setItem(THEME_KEY, pref);
            // Update values in place. Deleting and re-adding keys reorders the JSON, and
            // Velzon's layout.js reloads the page when that order no longer matches <html>.
            sessionStorage.setItem("data-bs-theme", resolved);
            var stored = sessionStorage.getItem("defaultAttribute");
            if (stored) {
                var attrs = JSON.parse(stored);
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
            applyTheme(set.getAttribute("data-tps-theme-set"));
            return;
        }
        var toggle = e.target.closest("[data-tps-theme-toggle]");
        if (toggle) {
            e.preventDefault();
            applyTheme(resolve(readPref()) === "dark" ? "light" : "dark");
        }
    });

    if (darkQuery) {
        var onSystemChange = function () { if (readPref() === "system") applyTheme("system"); };
        if (darkQuery.addEventListener) darkQuery.addEventListener("change", onSystemChange);
        else if (darkQuery.addListener) darkQuery.addListener(onSystemChange);
    }

    // Another tab changed the theme
    window.addEventListener("storage", function (e) {
        if (e.key === THEME_KEY && e.newValue) applyTheme(e.newValue);
    });

    syncThemeControls(readPref(), resolve(readPref()));

    /* ---------------------------------------------------------------
     * 1b. Brand color picker (topbar, next to the theme toggle)
     *     Color math and storage live in tps-theme-init.js (window.TPSBrand).
     *     Independent of Light/Dark: the same picked color is adapted per mode.
     * ------------------------------------------------------------- */
    var brand = window.TPSBrand;

    function syncColorControls(picked) {
        var value = picked || brand.DEFAULT;
        document.querySelectorAll("[data-tps-color-input]").forEach(function (el) {
            if (el.value.toLowerCase() !== value) el.value = value;
        });
        document.querySelectorAll("[data-tps-color-hex]").forEach(function (el) {
            if (document.activeElement !== el) el.value = value;
            el.classList.remove("is-invalid");
        });
        document.querySelectorAll("[data-tps-color-reset]").forEach(function (el) {
            el.disabled = !picked;
        });
    }

    // A picked color (even the default blue) themes the whole app, sidebar and sign-in panel
    // included; only "Reset to default" (null) returns to the original light palette.
    function setBrand(picked, persist) {
        brand.apply(picked);
        if (persist) brand.save(picked);
        syncColorControls(picked);
        window.dispatchEvent(new CustomEvent("tps:brandchange", { detail: { color: picked || brand.DEFAULT } }));
    }

    if (brand) {
        // Live preview while dragging; one rAF per frame keeps it smooth.
        var pendingColor = null;
        document.addEventListener("input", function (e) {
            if (e.target.matches("[data-tps-color-input]")) {
                if (pendingColor === null) {
                    window.requestAnimationFrame(function () {
                        brand.apply(pendingColor.toLowerCase());
                        pendingColor = null;
                    });
                }
                pendingColor = e.target.value;
                return;
            }
            if (e.target.matches("[data-tps-color-hex]")) {
                var v = e.target.value.trim();
                if (v && v.charAt(0) !== "#") v = "#" + v;
                var ok = !!brand.parse(v) && v.length === 7;
                e.target.classList.toggle("is-invalid", !ok && v.length >= 7);
                if (ok) setBrand(v.toLowerCase(), true);
            }
        });

        document.addEventListener("change", function (e) {
            if (e.target.matches("[data-tps-color-input]")) setBrand(e.target.value.toLowerCase(), true);
        });

        document.addEventListener("focusout", function (e) {
            if (e.target.matches && e.target.matches("[data-tps-color-hex]")) syncColorControls(brand.read());
        });

        // Popover on the sign-in and landing pages (the app topbar uses a Bootstrap dropdown).
        function setPopOpen(pop, open) {
            pop.classList.toggle("is-open", open);
            var toggle = pop.querySelector("[data-tps-color-pop-toggle]");
            if (toggle) toggle.setAttribute("aria-expanded", open ? "true" : "false");
        }

        function closePops(except) {
            document.querySelectorAll(".tps-color-pop.is-open").forEach(function (pop) {
                if (pop !== except) setPopOpen(pop, false);
            });
        }

        document.addEventListener("click", function (e) {
            if (e.target.closest("[data-tps-color-reset]")) {
                e.preventDefault();
                setBrand(null, true);
                return;
            }
            var popToggle = e.target.closest("[data-tps-color-pop-toggle]");
            if (popToggle) {
                e.preventDefault();
                var pop = popToggle.closest(".tps-color-pop");
                closePops(pop);
                setPopOpen(pop, !pop.classList.contains("is-open"));
                return;
            }
            if (!e.target.closest(".tps-color-pop")) closePops(null);
        });

        document.addEventListener("keydown", function (e) {
            if (e.key !== "Escape") return;
            var open = document.querySelector(".tps-color-pop.is-open");
            if (!open) return;
            setPopOpen(open, false);
            var toggle = open.querySelector("[data-tps-color-pop-toggle]");
            if (toggle) toggle.focus();
        });

        // Another tab changed the color
        window.addEventListener("storage", function (e) {
            if (e.key === brand.KEY) setBrand(brand.parse(e.newValue) ? e.newValue : null, false);
        });

        syncColorControls(brand.read());
    }

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

    /* ---------------------------------------------------------------
     * 5. Sidebar scroll position
     *    The menu used to paint at the top, then jump, because SimpleBar built its
     *    scroller on DOMContentLoaded (after first paint) and only then was scrollTop
     *    restored. Build the one real scroller (#scrollbar) and restore scroll now,
     *    while this parser-blocking script is still running, so the first paint is
     *    already at the saved position. A second SimpleBar on #navbar-nav is not
     *    created (app.js); that nested bar rewrote the menu and reset the scroll.
     *    tps-theme-init.js sets __tpsSidebarRestore so Velzon does not scroll again.
     * ------------------------------------------------------------- */
    var SIDEBAR_KEY = "tps-sidebar-scroll";

    function sidebarHost() {
        return document.getElementById("scrollbar");
    }

    function sidebarScroller() {
        var host = sidebarHost();
        if (!host || !window.SimpleBar) return host ? host.querySelector(".simplebar-content-wrapper") : null;
        var bar = null;
        try {
            if (SimpleBar.instances && typeof SimpleBar.instances.get === "function") bar = SimpleBar.instances.get(host);
        } catch (e) { }
        if (!bar) {
            try { bar = new SimpleBar(host); } catch (e2) { return host.querySelector(".simplebar-content-wrapper"); }
        }
        return (bar && typeof bar.getScrollElement === "function")
            ? bar.getScrollElement()
            : host.querySelector(".simplebar-content-wrapper");
    }

    function saveSidebarScroll() {
        var host = sidebarHost();
        var scroller = host ? host.querySelector(".simplebar-content-wrapper") : null;
        if (!scroller) return;
        try { sessionStorage.setItem(SIDEBAR_KEY, String(Math.round(scroller.scrollTop))); } catch (e) { }
    }

    function savedSidebarScroll() {
        var target = NaN;
        try { target = parseInt(sessionStorage.getItem(SIDEBAR_KEY), 10); } catch (e) { }
        return target;
    }

    function applySidebarScroll(scroller, target) {
        if (isNaN(target)) return true;
        if (!scroller) return false;
        var max = scroller.scrollHeight - scroller.clientHeight;
        if (max < 1 && target > 0) return false;
        var next = Math.min(target, Math.max(0, max));
        if (scroller.scrollTop !== next) scroller.scrollTop = next;
        return Math.abs(scroller.scrollTop - next) <= 1;
    }

    function initSidebarScroll() {
        var host = sidebarHost();
        if (!host) return;
        window.addEventListener("pagehide", saveSidebarScroll);
        document.addEventListener("click", function (e) {
            if (e.target.closest && e.target.closest(".app-menu a[href]")) saveSidebarScroll();
        }, true);

        // The inline script after the menu in _Layout already scrolled #scrollbar natively
        // before the first paint. Building SimpleBar below moves the content into its own
        // scroller (at 0), so carry that position over in this same task: no frame is
        // painted in between.
        var target = savedSidebarScroll();
        if (isNaN(target) && typeof window.__tpsSidebarTarget === "number") target = window.__tpsSidebarTarget;
        if (isNaN(target) && host.scrollTop > 0) target = host.scrollTop;

        // Build SimpleBar, then clear any native offset left on #scrollbar (from the inline
        // script or an early wheel scroll). Left in place, it shifts SimpleBar's whole scroller
        // up: the last items are cut off and a gap opens above the sidebar footer.
        var scroller = sidebarScroller();
        if (host.scrollTop !== 0) host.scrollTop = 0;
        if (isNaN(target)) return;
        if (applySidebarScroll(scroller, target)) return;

        var pending = true;
        var observer = null;
        function retry() {
            if (!pending) return;
            if (applySidebarScroll(sidebarScroller(), target)) {
                pending = false;
                if (observer) observer.disconnect();
            }
        }
        if (window.ResizeObserver) {
            observer = new ResizeObserver(retry);
            observer.observe(sidebarHost());
        }
        requestAnimationFrame(function () {
            retry();
            requestAnimationFrame(retry);
        });
    }

    function init() {
        // Tom Select is loaded after this file so the menu can be restored before that download.
        if (window.TomSelect) enhanceAll();
        else document.addEventListener("DOMContentLoaded", function () { if (window.TomSelect) enhanceAll(); });
        tidyActionCells();
        initFullscreen();
    }

    // The sidebar markup is already parsed. Restore it before first paint, even while
    // document.readyState is still "loading" (waiting for DOMContentLoaded paints the top first).
    initSidebarScroll();

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
    else init();

    window.TPS = window.TPS || {};
    window.TPS.enhanceSelects = enhanceAll;
    window.TPS.tidyActionCells = tidyActionCells;
    window.TPS.setTheme = function (pref) { applyTheme(pref); };
    window.TPS.statusKind = statusKind;
})();

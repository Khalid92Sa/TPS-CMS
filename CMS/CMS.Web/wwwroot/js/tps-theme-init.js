/*
 * Applies the saved Light/Dark/System theme before the page paints.
 * Load in <head> BEFORE theme/js/layout.js.
 *
 * The preference lives in localStorage ("tps-theme") so it survives sign-out and new tabs.
 * Velzon's layout.js/app.js keep their own copy in sessionStorage ("data-bs-theme") and
 * reload the page when the <html> attributes differ from "defaultAttribute"; both are kept
 * in step here so the two never disagree.
 *
 * Also applies the saved brand color ("tps-brand-color", see window.TPSBrand below).
 */
(function () {
    var KEY = "tps-theme";
    var pref = "light";
    try { pref = localStorage.getItem(KEY) || "light"; } catch (e) { /* storage blocked */ }

    var prefersDark = false;
    try { prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches; } catch (e) { }
    var resolved = pref === "dark" || (pref === "system" && prefersDark) ? "dark" : "light";

    var html = document.documentElement;
    html.setAttribute("data-bs-theme", resolved);
    html.setAttribute("data-tps-theme-pref", pref);

    try {
        sessionStorage.setItem("data-bs-theme", resolved);
        var stored = sessionStorage.getItem("defaultAttribute");
        if (stored) {
            var attrs = JSON.parse(stored);
            // Assign in place. Removing the keys and adding them again changes JSON key order,
            // and layout.js reloads the whole page when that string no longer matches <html>.
            attrs["data-bs-theme"] = resolved;
            attrs["data-tps-theme-pref"] = pref;
            var live = {};
            Object.entries(html.attributes).forEach(function (pair) {
                var attr = pair[1];
                if (attr && attr.nodeName && attr.nodeName !== "undefined") live[attr.nodeName] = attr.nodeValue;
            });
            var themeOnly = true;
            var seen = {};
            Object.keys(attrs).forEach(function (k) { seen[k] = 1; });
            Object.keys(live).forEach(function (k) { seen[k] = 1; });
            Object.keys(seen).forEach(function (k) {
                if (k === "data-bs-theme" || k === "data-tps-theme-pref") return;
                if (attrs[k] !== live[k]) themeOnly = false;
            });
            // Same layout settings: keep Velzon's snapshot equal to the element so it does not reload.
            sessionStorage.setItem("defaultAttribute", themeOnly ? JSON.stringify(live) : JSON.stringify(attrs));
        }
    } catch (e) { /* storage blocked */ }

    // The server already rendered the vertical menu. Tell app.js not to serialize it and
    // write it back (it was doing that more than once per navigation).
    window.__tpsPreserveMenu = true;

    // The menu position is set before the first paint by the inline script after the menu
    // in _Layout (saved position, or the active item). Stop app.js from scrolling to the
    // active item 250ms later, which made the menu jump on pages opened without a saved position.
    window.__tpsSidebarRestore = true;
})();

/*
 * Brand color chosen with the topbar color picker.
 * Stored as the raw picked hex in localStorage ("tps-brand-color"); no value means the
 * default palette in tps-design.css. The picked color is adjusted per mode for contrast
 * (darkened against white in light mode, lightened against the dark surface in dark mode)
 * and written as --tps-primary* overrides in one <style> element, together with the sidebar
 * (--tps-side-*) and sign-in panel (--tps-panel-*) tokens painted in that color. Nothing is set on <html>:
 * layout.js reloads the page when <html> attributes change.
 */
(function () {
    var KEY = "tps-brand-color";
    var STYLE_ID = "tps-brand-override";
    var WHITE = [255, 255, 255], BLACK = [0, 0, 0];
    var INK = [15, 26, 42];          // --tps-heading (light)
    var DARK_SURFACE = [17, 26, 39]; // --tps-surface (dark)
    var DARK_BG = [11, 18, 29];      // --tps-bg (dark)

    function parse(hex) {
        var m = /^#?([0-9a-f]{6})$/i.exec(String(hex || "").trim());
        if (!m) return null;
        var n = parseInt(m[1], 16);
        return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
    }

    function round(c) { return c.map(function (v) { return Math.round(Math.max(0, Math.min(255, v))); }); }
    function hex(c) { return "#" + round(c).map(function (v) { return (v < 16 ? "0" : "") + v.toString(16); }).join(""); }
    function rgb(c) { return round(c).join(", "); }
    function mix(a, b, t) { return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; }

    function luminance(c) {
        var f = round(c).map(function (v) { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); });
        return 0.2126 * f[0] + 0.7152 * f[1] + 0.0722 * f[2];
    }

    function contrast(a, b) {
        var x = luminance(a), y = luminance(b);
        return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05);
    }

    // Smallest step from `c` toward `target` that reaches `ratio` contrast against `against`.
    function reach(c, target, against, ratio) {
        for (var t = 0; t <= 1; t += 0.02) {
            var m = mix(c, target, t);
            if (contrast(m, against) >= ratio) return m;
        }
        return target;
    }

    // Sidebar and sign-in panel painted in the brand color `bg`. Text is white or the heading
    // ink, whichever reads better on it; secondary text, borders and states derive from that
    // pair with a minimum contrast, so any picked color stays readable.
    function chrome(bg) {
        var on = contrast(WHITE, bg) >= contrast(INK, bg) ? WHITE : INK;
        var light = on === INK;
        // Menu text a little softer than headings when there is room, never below 4.5:1.
        var text = reach(mix(on, bg, 0.16), on, bg, Math.max(4.5, Math.min(7, contrast(on, bg) * 0.85)));
        var muted = reach(mix(on, bg, 0.42), on, bg, 4.5);
        var border = mix(bg, on, 0.14);
        var a = function (alpha) { return "rgba(" + rgb(on) + "," + alpha + ")"; };
        return "--tps-side-bg:" + hex(bg) + ";" +
            "--tps-side-border:" + hex(border) + ";" +
            "--tps-side-text:" + hex(text) + ";" +
            "--tps-side-text-strong:" + hex(on) + ";" +
            "--tps-side-section:" + hex(muted) + ";" +
            "--tps-side-hover:" + a(0.08) + ";" +
            "--tps-side-active:" + a(0.16) + ";" +
            "--tps-side-active-bar:" + hex(on) + ";" +
            "--tps-panel-bg:" + hex(bg) + ";" +
            "--tps-panel-glow-1:rgba(255,255,255," + (light ? 0.4 : 0.14) + ");" +
            "--tps-panel-glow-2:rgba(0,0,0," + (light ? 0.06 : 0.18) + ");" +
            "--tps-panel-grid:" + a(0.06) + ";" +
            "--tps-panel-border:" + hex(border) + ";" +
            "--tps-panel-text:" + hex(text) + ";" +
            "--tps-panel-heading:" + hex(on) + ";" +
            "--tps-panel-muted:" + hex(muted) + ";" +
            "--tps-panel-accent:" + hex(on) + ";" +
            "--tps-panel-accent-2:" + hex(on) + ";" +
            "--tps-panel-card-bg:" + a(0.08) + ";" +
            "--tps-panel-card-border:" + a(0.18) + ";" +
            "--tps-panel-card-text:" + hex(on) + ";";
    }

    function css(picked) {
        var c = parse(picked);
        if (!c) return "";

        // Light mode: readable as text/fill on white (white button text stays legible).
        var lp = reach(c, BLACK, WHITE, 4.5);
        var lStrong = mix(lp, BLACK, 0.2);
        var deep = mix(lp, BLACK, 0.22);
        // Dark mode: readable on the dark surface (dark button text stays legible).
        var dp = reach(c, WHITE, DARK_SURFACE, 5.5);
        var dStrong = mix(dp, WHITE, 0.22);

        // Sidebar / sign-in panel. Light mode: the picked color itself, nudged darker only when
        // it is a mid tone that white text cannot quite read on (pastels keep dark text).
        // Dark mode: a deep shade of it, so the page stays dark.
        var lChrome = contrast(WHITE, c) >= 3 ? reach(c, BLACK, WHITE, 6) : c;
        var dChrome = reach(mix(c, DARK_BG, 0.68), BLACK, WHITE, 7);

        return "html:root:not([data-bs-theme=dark]){" +
            chrome(lChrome) +
            "--tps-primary:" + hex(lp) + ";" +
            "--tps-primary-rgb:" + rgb(lp) + ";" +
            "--tps-primary-strong:" + hex(lStrong) + ";" +
            "--tps-primary-soft:" + hex(mix(lp, WHITE, 0.9)) + ";" +
            "--tps-primary-deep:" + hex(deep) + ";" +
            "--tps-brand-gradient:linear-gradient(135deg," + hex(lp) + " 0%," + hex(lStrong) + " 100%);" +
            "--tps-brand-gradient-hover:linear-gradient(135deg," + hex(lStrong) + " 0%," + hex(mix(lp, BLACK, 0.32)) + " 100%);" +
            "}" +
            // Extra attribute selector so this outranks the dark block in tps-design.css,
            // which loads after this element.
            "html:root[data-bs-theme=dark][data-bs-theme]{" +
            chrome(dChrome) +
            "--tps-primary:" + hex(dp) + ";" +
            "--tps-primary-rgb:" + rgb(dp) + ";" +
            "--tps-primary-strong:" + hex(dStrong) + ";" +
            "--tps-primary-soft:rgba(" + rgb(dp) + ",0.14);" +
            "--tps-primary-deep:" + hex(deep) + ";" +
            "--tps-brand-gradient:linear-gradient(135deg," + hex(mix(lp, WHITE, 0.08)) + " 0%," + hex(deep) + " 100%);" +
            "--tps-brand-gradient-hover:linear-gradient(135deg," + hex(mix(lp, WHITE, 0.16)) + " 0%," + hex(lp) + " 100%);" +
            "}";
    }

    function read() {
        try { var v = localStorage.getItem(KEY); return parse(v) ? v.toLowerCase() : null; } catch (e) { return null; }
    }

    // Applies `picked` (a #rrggbb string) or, with null, the default palette. Does not save.
    function apply(picked) {
        var text = picked ? css(picked) : "";
        var el = document.getElementById(STYLE_ID);
        if (!text) {
            if (el) el.parentNode.removeChild(el);
            return;
        }
        if (!el) {
            el = document.createElement("style");
            el.id = STYLE_ID;
            document.head.appendChild(el);
        }
        if (el.textContent !== text) el.textContent = text;
    }

    function save(picked) {
        try {
            if (picked) localStorage.setItem(KEY, picked.toLowerCase());
            else localStorage.removeItem(KEY);
        } catch (e) { /* storage blocked: color still applies for this page */ }
    }

    window.TPSBrand = { KEY: KEY, DEFAULT: "#2856c8", parse: parse, hex: hex, read: read, apply: apply, save: save };

    // The color picker is gone. Drop any color still saved in this browser
    // so the default palette is what the page shows.
    try { localStorage.removeItem(KEY); } catch (e) { }
    apply(null);
})();

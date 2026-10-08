/*
 * Applies the saved Light/Dark/System theme before the page paints.
 * Load in <head> BEFORE theme/js/layout.js.
 *
 * The preference lives in localStorage ("tps-theme") so it survives sign-out and new tabs.
 * Velzon's layout.js/app.js keep their own copy in sessionStorage ("data-bs-theme") and
 * reload the page when the <html> attributes differ from "defaultAttribute"; both are kept
 * in step here so the two never disagree.
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
            // Re-append so key order matches the attribute order layout.js compares against.
            delete attrs["data-bs-theme"];
            delete attrs["data-tps-theme-pref"];
            attrs["data-bs-theme"] = resolved;
            attrs["data-tps-theme-pref"] = pref;
            sessionStorage.setItem("defaultAttribute", JSON.stringify(attrs));
        }
    } catch (e) { /* storage blocked */ }

    // Sidebar scroll position from the previous page (saved by tps-ui.js). Hide the menu until
    // tps-ui.js puts it back, and tell Velzon's app.js to skip its own "scroll to active item",
    // so the menu never shows at the top and then jumps while pages load.
    // Uses a <style> element, never a class or attribute on <html>: layout.js compares every
    // <html> attribute with sessionStorage "defaultAttribute" and clears storage and reloads the
    // page when they differ.
    try {
        if (sessionStorage.getItem("tps-sidebar-scroll") !== null) {
            var hide = document.createElement("style");
            hide.id = "tps-sidebar-restoring";
            hide.textContent = ".app-menu #scrollbar{visibility:hidden}";
            document.head.appendChild(hide);
            window.__tpsSidebarRestore = true;
        }
    } catch (e) { /* storage blocked */ }
})();

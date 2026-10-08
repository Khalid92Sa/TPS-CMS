/*
 * Shared rendering for the notification list pages (HR, GM, Interviewer, Architect).
 * Each page keeps its own data loading; it calls TPSNotices to build the rows so every
 * list looks and behaves the same.
 *
 *   TPSNotices.render(container, items, { read: bool })   items from the JSON endpoints
 *   TPSNotices.loading(container)                          skeleton while a request runs
 *   TPSNotices.setCount(selector, n)                       unread badge on the tab
 *   TPSNotices.formatTimes(root)                           relative times for server-rendered rows
 */
(function () {
    "use strict";

    var rtf = window.Intl && Intl.RelativeTimeFormat ? new Intl.RelativeTimeFormat(document.documentElement.lang || undefined, { numeric: "auto" }) : null;

    function relative(date) {
        if (!(date instanceof Date) || isNaN(date)) return "";
        var seconds = Math.round((date.getTime() - Date.now()) / 1000);
        var abs = Math.abs(seconds);
        if (!rtf) return date.toLocaleString();
        if (abs < 60) return rtf.format(seconds, "second");
        if (abs < 3600) return rtf.format(Math.round(seconds / 60), "minute");
        if (abs < 86400) return rtf.format(Math.round(seconds / 3600), "hour");
        if (abs < 604800) return rtf.format(Math.round(seconds / 86400), "day");
        return date.toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
    }

    function absolute(date) {
        return isNaN(date) ? "" : date.toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
    }

    function escapeAttr(value) {
        return String(value == null ? "" : value).replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
    }

    // Titles are stored as HTML by the system (same as before), so they are inserted as HTML.
    function row(n, read) {
        var date = new Date(n.sendDate);
        return '<a class="tps-notice' + (read ? "" : " tps-notice--unread") + '" href="/Notifications/Details/' + encodeURIComponent(n.notificationsId) + '">'
            + '<span class="tps-notice__icon" aria-hidden="true"><i class="' + (read ? "ri-notification-3-line" : "ri-notification-badge-line") + '"></i></span>'
            + '<span><span class="tps-notice__title d-block" dir="auto">' + (n.title || "Notification") + "</span>"
            + '<time class="tps-notice__time" dir="auto" datetime="' + escapeAttr(isNaN(date) ? "" : date.toISOString()) + '" title="' + escapeAttr(absolute(date)) + '">'
            + relative(date) + "</time></span>"
            + '<span class="tps-notice__end">' + (read ? "" : '<span class="tps-notice__dot" title="Unread"></span>')
            + '<i class="ri-arrow-right-s-line" aria-hidden="true"></i></span>'
            + "</a>";
    }

    function empty(read) {
        return '<div class="tps-notices-empty"><i class="' + (read ? "ri-inbox-line" : "ri-checkbox-circle-line") + '"></i>'
            + (read ? "No read notifications yet." : "You're all caught up. No unread notifications.") + "</div>";
    }

    function render(container, items, options) {
        var el = typeof container === "string" ? document.querySelector(container) : container;
        if (!el) return;
        var read = !!(options && options.read);
        var list = Array.isArray(items) ? items : [];
        el.classList.add("tps-notices");
        el.innerHTML = list.length ? list.map(function (n) { return row(n, read); }).join("") : empty(read);
    }

    function loading(container) {
        var el = typeof container === "string" ? document.querySelector(container) : container;
        if (!el) return;
        el.classList.add("tps-notices");
        el.innerHTML = '<div class="tps-notices-loading" aria-busy="true"><div class="tps-skeleton" style="width:70%"></div><div class="tps-skeleton" style="width:45%"></div><div class="tps-skeleton" style="width:60%"></div></div>';
    }

    function setCount(selector, count) {
        var el = document.querySelector(selector);
        if (el) el.textContent = count > 0 ? String(count) : "";
    }

    function formatTimes(root) {
        (root || document).querySelectorAll(".tps-notice time[datetime]").forEach(function (t) {
            var date = new Date(t.getAttribute("datetime"));
            if (isNaN(date)) return;
            t.textContent = relative(date);
            t.title = absolute(date);
        });
    }

    window.TPSNotices = { render: render, loading: loading, setCount: setCount, formatTimes: formatTimes, relative: relative };
    document.addEventListener("DOMContentLoaded", function () { formatTimes(); });
})();

// Reuniverse theme bootstrap.
//
// This must load as a blocking script in <head>, before any content paints, so
// the saved/preferred theme is applied before first render (no theme flash).
//
// The MutationObserver re-applies the theme when Blazor Web App's enhanced
// navigation replaces <html> attributes, which would otherwise clear data-theme.
(function () {
    "use strict";

    window.reuTheme = {
        get: function () {
            return document.documentElement.getAttribute("data-theme") || "dark";
        },
        set: function (theme) {
            document.documentElement.setAttribute("data-theme", theme);
            try { localStorage.setItem("reu-theme", theme); } catch (e) { }
            return theme;
        },
        toggle: function () {
            return window.reuTheme.set(window.reuTheme.get() === "light" ? "dark" : "light");
        }
    };

    function applyTheme() {
        var theme;
        try { theme = localStorage.getItem("reu-theme"); } catch (e) { }
        if (!theme) {
            theme = (window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches)
                ? "light" : "dark";
        }
        document.documentElement.setAttribute("data-theme", theme);
    }

    applyTheme();

    // Blazor Web App's enhanced navigation can strip attributes from <html>.
    // Re-apply the theme whenever data-theme disappears.
    new MutationObserver(function () {
        if (!document.documentElement.getAttribute("data-theme")) {
            applyTheme();
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
})();

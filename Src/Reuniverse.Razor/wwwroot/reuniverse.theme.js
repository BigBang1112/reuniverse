// Reuniverse theme bootstrap.
//
// This must load as a blocking script in <head>, before any content paints, so
// the saved/preferred theme is applied before first render (no theme flash).
//
// The MutationObserver re-applies the theme when Blazor Web App's enhanced
// navigation replaces <html> attributes, which would otherwise clear data-theme.
//
// Without a saved theme, the OS/browser preference decides, which dark mode
// extensions can't change. Dark Reader marks the page it darkens, so when it is
// found the dark theme is used and Dark Reader is told to leave the page alone,
// instead of it inverting the light theme.
(function () {
    "use strict";

    var darkReader = false;

    function getSavedTheme() {
        try { return localStorage.getItem("reu-theme"); } catch (e) { return null; }
    }

    function isDarkReaderActive() {
        var root = document.documentElement;
        var scheme = root.getAttribute("data-darkreader-scheme");
        // Dark Reader's light scheme brightens pages instead, so it doesn't ask for the dark theme.
        if (scheme) {
            return scheme !== "light";
        }
        return root.hasAttribute("data-darkreader-mode") || !!document.querySelector('meta[name="darkreader"]');
    }

    function lockDarkReader() {
        if (document.querySelector('meta[name="darkreader-lock"]')) {
            return;
        }
        var meta = document.createElement("meta");
        meta.name = "darkreader-lock";
        (document.head || document.documentElement).appendChild(meta);
    }

    function setTheme(theme) {
        if (darkReader && theme === "dark") {
            lockDarkReader();
        }
        document.documentElement.setAttribute("data-theme", theme);
    }

    window.reuTheme = {
        get: function () {
            return document.documentElement.getAttribute("data-theme") || "dark";
        },
        set: function (theme) {
            setTheme(theme);
            try { localStorage.setItem("reu-theme", theme); } catch (e) { }
            return theme;
        },
        toggle: function () {
            return window.reuTheme.set(window.reuTheme.get() === "light" ? "dark" : "light");
        }
    };

    function applyTheme() {
        var theme = getSavedTheme();
        if (!theme) {
            theme = (!darkReader && window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches)
                ? "light" : "dark";
        }
        setTheme(theme);
    }

    darkReader = isDarkReaderActive();
    applyTheme();

    // Blazor Web App's enhanced navigation can strip attributes from <html>.
    // Re-apply the theme whenever data-theme disappears.
    new MutationObserver(function () {
        if (!document.documentElement.getAttribute("data-theme")) {
            applyTheme();
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });

    // Dark Reader may mark the page after this script has run. It starts as the page
    // starts loading, so stop looking a little while after the page has loaded.
    if (!darkReader) {
        var darkReaderObserver = new MutationObserver(function () {
            if (isDarkReaderActive()) {
                darkReaderObserver.disconnect();
                darkReader = true;
                applyTheme();
            }
        });
        darkReaderObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ["data-darkreader-mode", "data-darkreader-scheme"]
        });
        if (document.head) {
            darkReaderObserver.observe(document.head, { childList: true });
        }

        var stopLooking = function () {
            setTimeout(function () { darkReaderObserver.disconnect(); }, 3000);
        };
        if (document.readyState === "complete") {
            stopLooking();
        } else {
            window.addEventListener("load", stopLooking);
        }
    }
})();

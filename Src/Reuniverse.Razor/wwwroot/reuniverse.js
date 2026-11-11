// Reuniverse responsive navbar.
//
// It collapses every ReuNavMenu based on the real combined width of its buttons
// rather than fixed viewport breakpoints. Two width thresholds are derived from
// the actual content:
//   fullWidth  - width the bar needs to show every button with its label
//   iconsWidth - width the bar needs to show every button as an icon only
// The currently available width is then compared against those thresholds:
//   available >= fullWidth   -> full labels
//   available >= iconsWidth  -> icons only
//   otherwise                -> burger menu
//
// If this script never loads, the navbar falls back to the static width-based
// media queries (the `.navbar:not([data-reu-nav-mode])` rules) that ship in the CSS.
(function () {
    "use strict";

    const modeAttribute = "data-reu-nav-mode";
    const openAttribute = "data-reu-nav-open";
    const sizingAttribute = "data-reu-nav-sizing";
    const submenuSelector = ".nav-submenu";
    const submenuStateAttribute = "data-reu-nav-submenu";
    const submenuEdgeGap = 8;

    // Kept apart from the element, as Blazor enhanced navigation resets everything
    // rendered on it, and each navbar must only ever be set up once.
    const controllers = new WeakMap();

    function createController(navbar, container, nav) {
        const burgerCheckbox = navbar.querySelector(".nav-toggle input");

        let frame = 0;
        let lastPointerType = "";

        // Kept apart from the elements, as Blazor enhanced navigation resets their attributes.
        const dismissedSubmenus = new Set();

        function syncBurgerOpenState() {
            const isOpen = !!burgerCheckbox && burgerCheckbox.checked && navbar.getAttribute(modeAttribute) === "menu";
            navbar.toggleAttribute(openAttribute, isOpen);
        }

        function closeBurger() {
            if (!burgerCheckbox || !burgerCheckbox.checked) {
                return;
            }

            burgerCheckbox.checked = false;
            syncBurgerOpenState();
        }

        function resetSubmenu(submenu) {
            submenu.removeAttribute(submenuStateAttribute);
            dismissedSubmenus.delete(submenu);
        }

        // Hovering and keyboard focus are up to the CSS. What is left here is touch, which
        // has neither, and hiding what those would otherwise keep showing.
        function closeSubmenus(except) {
            nav.querySelectorAll(`${submenuSelector}[${submenuStateAttribute}]`).forEach(function (submenu) {
                if (!submenu.contains(except)) {
                    resetSubmenu(submenu);
                }
            });
        }

        // Keeps a submenu hidden until the pointer and focus have left it, with focus
        // going back to its item, as it would otherwise be lost along with the panel.
        function dismissSubmenus() {
            closeSubmenus();

            nav.querySelectorAll(submenuSelector).forEach(function (submenu) {
                if (!submenu.matches(":hover, :has(:focus-visible)")) {
                    return;
                }

                if (submenu.querySelector(".nav-submenu-items :focus")) {
                    submenu.querySelector(".nav-item a").focus();
                }

                submenu.setAttribute(submenuStateAttribute, "closed");
                dismissedSubmenus.add(submenu);
            });
        }

        // Panels extend to the left of their item, where a narrow screen can run out of room.
        function keepPanelInView(panel) {
            panel.style.translate = "";

            const overflow = submenuEdgeGap - panel.getBoundingClientRect().left;

            if (overflow > 0) {
                panel.style.translate = `${overflow}px`;
            }
        }

        // Measures the width the bar needs in a given mode by letting the
        // container shrink to its content's natural (max-content) width. This is
        // the combined width of all buttons plus the brand, gaps and padding.
        // Callers run this inside the sizing state so the temporary layout never
        // animates.
        function requiredWidth(mode) {
            navbar.setAttribute(modeAttribute, mode);

            const previousWidth = container.style.width;
            container.style.width = "max-content";
            const width = container.getBoundingClientRect().width;
            container.style.width = previousWidth;

            return width;
        }

        function requiredBurgerWidth() {
            navbar.setAttribute(modeAttribute, "menu");

            // The computed width ignores the scale the closed menu animates from.
            const previousWidth = nav.style.width;
            nav.style.width = "max-content";
            const width = parseFloat(getComputedStyle(nav).width);
            nav.style.width = previousWidth;

            return width;
        }

        function update() {
            frame = 0;

            const previousMode = navbar.getAttribute(modeAttribute);

            // Disable every transition in the navbar while we probe and switch
            // modes so none of the intermediate states paint or animate.
            navbar.setAttribute(sizingAttribute, "");

            const fullWidth = requiredWidth("labels");
            const iconsWidth = requiredWidth("icons");
            const burgerWidth = requiredBurgerWidth();

            navbar.style.setProperty("--reu-nav-menu-width", `${Math.ceil(burgerWidth)}px`);

            navbar.setAttribute(modeAttribute, "labels");
            const available = container.getBoundingClientRect().width;
            const mode = available < iconsWidth ? "menu" : available < fullWidth ? "icons" : "labels";

            navbar.setAttribute(modeAttribute, mode);

            if (mode !== previousMode) {
                closeSubmenus();
            }

            dismissedSubmenus.forEach(function (submenu) {
                submenu.setAttribute(submenuStateAttribute, "closed");
            });

            nav.querySelectorAll(".nav-submenu-items").forEach(keepPanelInView);

            if (mode === "menu") {
                syncBurgerOpenState();
            } else {
                closeBurger();
                navbar.removeAttribute(openAttribute);
            }

            // Flush the final layout while transitions are still off, then
            // re-enable them so the baseline matches the result and nothing
            // animates back.
            container.getBoundingClientRect();
            navbar.removeAttribute(sizingAttribute);
        }

        function schedule() {
            if (frame === 0) {
                frame = requestAnimationFrame(update);
            }
        }

        // React to viewport changes (navbar width tracks the viewport, so
        // observing it never feeds back from our own attribute changes).
        new ResizeObserver(schedule).observe(navbar);

        // Recompute when items are added/removed or their text changes.
        new MutationObserver(schedule).observe(nav, { childList: true, subtree: true, characterData: true });

        // Blazor enhanced navigation keeps the element but resets its attributes.
        new MutationObserver(function () {
            if (!navbar.hasAttribute(modeAttribute)) {
                schedule();
            }
        }).observe(navbar, { attributes: true, attributeFilter: [modeAttribute] });

        nav.addEventListener("pointerdown", function (event) {
            lastPointerType = event.pointerType;
        });

        // Close the burger dropdown after navigating.
        nav.addEventListener("click", function (event) {
            const link = event.target.closest("a");

            if (!link) {
                return;
            }

            const item = link.closest(`${submenuSelector} > .nav-item`);
            const submenu = item ? item.parentElement : null;

            // Touch has no hover, so the first tap shows the submenu and the second follows the link.
            // Enter also arrives as a click, with a detail of 0.
            if (submenu && lastPointerType === "touch" && event.detail > 0
                && navbar.getAttribute(modeAttribute) !== "menu"
                && submenu.getAttribute(submenuStateAttribute) !== "open") {
                event.preventDefault();
                submenu.setAttribute(submenuStateAttribute, "open");
            } else if (link.hasAttribute("href")) {
                closeBurger();
                dismissSubmenus();
            } else {
                closeSubmenus();
            }
        });

        // A dismissed submenu can show again once the pointer and focus have left it.
        // Capturing, as leaving does not bubble.
        nav.addEventListener("pointerleave", function (event) {
            if (dismissedSubmenus.has(event.target) && !event.target.matches(":has(:focus-visible)")) {
                resetSubmenu(event.target);
            }
        }, true);

        nav.addEventListener("focusout", function (event) {
            const submenu = event.target.closest(submenuSelector);

            if (dismissedSubmenus.has(submenu) && !submenu.matches(":hover")) {
                resetSubmenu(submenu);
            }
        });

        if (burgerCheckbox) {
            burgerCheckbox.addEventListener("change", syncBurgerOpenState);
        }

        update();

        return { schedule, closeBurger, closeSubmenus, dismissSubmenus };
    }

    function enhanceNavMenu(navbar) {
        if (controllers.has(navbar)) {
            return;
        }

        const container = navbar.querySelector(".reu-container");
        const nav = navbar.querySelector("nav");

        if (container && nav) {
            controllers.set(navbar, createController(navbar, container, nav));
        }
    }

    function enhanceAll() {
        document.querySelectorAll(".navbar").forEach(enhanceNavMenu);
    }

    function forEachController(action) {
        document.querySelectorAll(".navbar").forEach(function (navbar) {
            const controller = controllers.get(navbar);

            if (controller) {
                action(controller, navbar);
            }
        });
    }

    function start() {
        enhanceAll();

        // Enhance navbars that appear later (e.g. interactive rendering replacing the DOM).
        // Watches the whole document, as enhanced navigation away from an interactive page
        // replaces <body> itself, and this would stop firing if it watched the old one.
        new MutationObserver(enhanceAll).observe(document.documentElement, { childList: true, subtree: true });

        document.addEventListener("pointerdown", function (event) {
            forEachController(function (controller, navbar) {
                controller.closeSubmenus(event.target);

                if (!navbar.contains(event.target)) {
                    controller.closeBurger();
                }
            });
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                forEachController(function (controller) {
                    controller.closeBurger();
                    controller.dismissSubmenus();
                });
            }
        });

        // A web font arriving changes how much room the labels need.
        document.fonts.addEventListener("loadingdone", function () {
            forEachController(function (controller) {
                controller.schedule();
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();

// Reuniverse tooltips.
//
// Any element with a data-reu-tooltip attribute shows its text after a short hover
// or keyboard focus. This is all client-side, with a single listener on the document, so it
// works under every render mode including static SSR, and elements rendered later
// need no setup.
(function () {
    "use strict";

    const attribute = "data-reu-tooltip";
    const selector = `[${attribute}]`;
    const tooltipId = "reu-tooltip";
    const overflowingClass = "reu-tooltip-overflowing";

    const showDelay = 500;
    const cursorGap = 16;
    const edgeGap = 8;

    let tooltip = null;
    let target = null;
    let waitingFor = null;
    let waitTimer = 0;
    let dismissed = null;
    let anchorX = 0;
    let anchorTop = 0;
    let anchorBottom = 0;

    // Blazor enhanced navigation replaces the contents of <body>, so the element
    // has to be able to come back after it was removed.
    function ensureTooltip() {
        if (!tooltip) {
            tooltip = document.createElement("div");
            tooltip.id = tooltipId;
            tooltip.className = "reu-tooltip";
            tooltip.setAttribute("role", "tooltip");
            resizes.observe(tooltip);
        }

        if (!tooltip.isConnected) {
            document.body.append(tooltip);
        }

        return tooltip;
    }

    // Each word goes in its own element that does not wrap, so that the lines break only between
    // words, and not also after the hyphens or slashes in a file name, for example.
    function fill(text) {
        tooltip.classList.remove(overflowingClass);
        tooltip.replaceChildren(...text.split(/(\s+)/).filter(Boolean).map(function (part) {
            if (/^\s/.test(part)) {
                return document.createTextNode(part);
            }

            const word = document.createElement("span");
            word.textContent = part;
            return word;
        }));
    }

    function place() {
        // without the scrollbars, which the tooltip would otherwise end up under
        const viewportWidth = document.documentElement.clientWidth;
        const viewportHeight = document.documentElement.clientHeight;

        // A word wider than the window has to break after all.
        if (tooltip.getBoundingClientRect().width > viewportWidth - 2 * edgeGap) {
            tooltip.classList.add(overflowingClass);
        }

        const { width, height } = tooltip.getBoundingClientRect();
        const maxLeft = viewportWidth - width - edgeGap;
        const fitsBelow = anchorBottom + height + edgeGap <= viewportHeight;

        const left = Math.max(edgeGap, Math.min(anchorX, maxLeft));
        const top = fitsBelow ? anchorBottom : Math.max(edgeGap, anchorTop - height - edgeGap);

        tooltip.style.left = `${left}px`;
        tooltip.style.top = `${top}px`;
    }

    function hide() {
        clearTimeout(waitTimer);
        waitingFor = null;

        if (!target) {
            return;
        }

        if (target.getAttribute("aria-describedby") === tooltipId) {
            target.removeAttribute("aria-describedby");
        }

        target = null;
        tooltip.remove();
        changes.disconnect();
    }

    function show(element) {
        const text = element.isConnected ? element.getAttribute(attribute) : null;

        if (!text) {
            hide();
            return;
        }

        if (element !== target) {
            hide();
            target = element;

            if (!target.hasAttribute("aria-describedby")) {
                target.setAttribute("aria-describedby", tooltipId);
            }

            changes.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: [attribute] });
        }

        ensureTooltip();
        fill(text);
        place();
    }

    // Moving within the element keeps the wait going, so the tooltip appears where
    // the cursor ended up rather than where it came in.
    function request(element, x, top, bottom) {
        anchorX = x;
        anchorTop = top;
        anchorBottom = bottom;

        if (element === dismissed) {
            hide();
        } else if (element === target) {
            show(element);
        } else if (element !== waitingFor) {
            hide();
            waitingFor = element;
            waitTimer = setTimeout(function () {
                waitingFor = null;
                show(element);
            }, showDelay);
        }
    }

    // Places it again when its size changes after it was placed, such as once the font has
    // loaded, as it could otherwise end up past the edge of the window.
    const resizes = new ResizeObserver(function () {
        if (target) {
            place();
        }
    });

    // Keeps up with the target while the tooltip is visible, as an element that is
    // removed or changed under a still cursor does not raise any pointer events.
    const changes = new MutationObserver(function () {
        if (!target) {
            return;
        }

        const text = target.isConnected ? target.getAttribute(attribute) : null;

        if (!text) {
            hide();
        } else if (tooltip.textContent !== text) {
            fill(text);
            place();
        }
    });

    document.addEventListener("pointermove", function (event) {
        if (event.pointerType === "touch") {
            return;
        }

        const element = event.target instanceof Element ? event.target.closest(selector) : null;

        if (element !== dismissed) {
            dismissed = null;
        }

        if (element) {
            request(element, event.clientX, event.clientY, event.clientY + cursorGap);
        } else {
            hide();
        }
    }, { passive: true });

    document.addEventListener("focusin", function (event) {
        const element = event.target.closest(selector);

        if (element && event.target.matches(":focus-visible")) {
            const { left, top, bottom } = element.getBoundingClientRect();
            request(element, left, top, bottom + edgeGap);
        }
    });

    document.addEventListener("focusout", hide);
    document.addEventListener("scroll", hide, { capture: true, passive: true });
    document.documentElement.addEventListener("pointerleave", hide);

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && (target || waitingFor)) {
            dismissed = target ?? waitingFor;
            hide();
        }
    });
})();

// Reuniverse error panel.
//
// Blazor reveals #blazor-error-ui after an unhandled error, while the error itself only
// reaches the console. This keeps what is written there and shows it in a panel in place
// of that element. It is all client-side, so it also works when .NET is what failed, such
// as during startup. If this script never loads, #blazor-error-ui shows as usual.
(function () {
    "use strict";

    const blazorErrorId = "blazor-error-ui";

    const maxEntries = 100;
    const copiedDuration = 1500;

    let entries = [];
    let panel = null;
    let isOpen = false;
    let copiedTimer = 0;

    // Must not throw, as that would turn logging an error into a new one.
    function describe(value) {
        if (value instanceof Error) {
            // Only some browsers start the stack with the message.
            return value.stack?.includes(value.message) ? value.stack : `${value}\n${value.stack ?? ""}`.trim();
        }

        try {
            return typeof value === "object" && value !== null ? JSON.stringify(value) : String(value);
        } catch (e) {
            return Object.prototype.toString.call(value);
        }
    }

    function formatTime(time) {
        const date = new Date(time);
        return `${date.toTimeString().slice(0, 8)}.${String(date.getMilliseconds()).padStart(3, "0")}`;
    }

    function scrollToLatest() {
        const log = panel.querySelector(".reu-error-log");
        const latest = log.lastElementChild;

        // The latest error rather than the very bottom, as that is where its stack ends.
        if (latest) {
            log.scrollTop = latest.offsetTop;
        }
    }

    function render() {
        // Error text can contain anything, so it must only ever be set as text.
        panel.querySelector(".reu-error-log").replaceChildren(...entries.map(function (entry) {
            const row = document.createElement("div");
            const time = document.createElement("span");

            row.className = "reu-error-entry";
            time.className = "reu-error-time";
            time.textContent = formatTime(entry.time);
            row.append(time, entry.text);

            return row;
        }));

        panel.querySelector(".reu-error-details").hidden = entries.length === 0;
        panel.querySelector(".reu-error-copy").hidden = entries.length === 0;

        scrollToLatest();
    }

    function record(text) {
        entries.push({ time: Date.now(), text });

        if (entries.length > maxEntries) {
            entries.shift();
        }

        if (isOpen) {
            render();
        }
    }

    function copy(button) {
        function showCopied() {
            clearTimeout(copiedTimer);
            button.textContent = "Copied";
            copiedTimer = setTimeout(function () {
                button.textContent = "Copy";
            }, copiedDuration);
        }

        // The clipboard can be missing or denied, as in an insecure context, which
        // leaves selecting the text so it can be copied by hand.
        function selectLog() {
            panel.querySelector(".reu-error-details").open = true;
            getSelection().selectAllChildren(panel.querySelector(".reu-error-log"));
        }

        const text = entries.map(function (entry) {
            return `[${formatTime(entry.time)}] ${entry.text}`;
        }).join("\n");

        (navigator.clipboard?.writeText(text) ?? Promise.reject()).then(showCopied, selectLog);
    }

    function dismiss() {
        isOpen = false;
        entries = [];
        panel.remove();
    }

    function createPanel() {
        panel = document.createElement("section");
        panel.className = "reu-error-panel";
        panel.setAttribute("role", "alert");
        panel.innerHTML = `
            <div class="reu-error-header">
                <span class="reu-error-icon" aria-hidden="true">⚠</span>
                <span class="reu-error-title">An unhandled error has occurred</span>
                <button type="button" class="reu-error-dismiss" aria-label="Dismiss">✕</button>
            </div>
            <p class="reu-error-message">The app may stop working as expected. Reloading the page should get it going again.</p>
            <details class="reu-error-details" open>
                <summary>Error log</summary>
                <div class="reu-error-log reu-mono" tabindex="0"></div>
            </details>
            <div class="reu-error-actions">
                <button type="button" class="reu-button reu-error-copy">Copy</button>
                <button type="button" class="reu-button reu-button-blue reu-error-reload">Reload</button>
            </div>`;

        panel.addEventListener("click", function (event) {
            if (event.target.closest(".reu-error-dismiss")) {
                dismiss();
            } else if (event.target.closest(".reu-error-copy")) {
                copy(event.target);
            } else if (event.target.closest(".reu-error-reload")) {
                location.reload();
            }
        });

        panel.querySelector(".reu-error-details").addEventListener("toggle", scrollToLatest);
    }

    function show() {
        if (!panel) {
            createPanel();
        }

        isOpen = true;
        document.body.append(panel);
        render();
    }

    const originalError = console.error;

    console.error = function (...values) {
        originalError.apply(console, values);
        record(values.map(describe).join(" "));
    };

    window.addEventListener("error", function (event) {
        record(describe(event.error ?? event.message));
    });

    window.addEventListener("unhandledrejection", function (event) {
        record(describe(event.reason));
    });

    // Watches the whole document, as the element can be rendered at any point, and as
    // enhanced navigation replaces the contents of <body> along with the panel.
    new MutationObserver(function (mutations) {
        if (isOpen && !panel.isConnected) {
            document.body.append(panel);
        }

        const blazorError = mutations.find(function (mutation) {
            return mutation.target.id === blazorErrorId && mutation.target.style.display === "block";
        })?.target;

        // Hiding it again before it paints, which also makes the next error a change to observe.
        if (blazorError) {
            blazorError.style.display = "none";
            show();
        }
    }).observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ["style"] });
})();

// Reuniverse update prompt.
//
// An installed app keeps running the version its service worker cached, and a new service
// worker only takes over on its own once every tab of the app is closed. This watches for a
// new one and offers to switch to it with a reload. Switching needs the service worker to
// call skipWaiting() when it is sent { type: "SKIP_WAITING" }; one that already does so
// right on install has taken over by then, so the reload is all that is left. It also checks
// for a new version every hour and when the page is shown again, as an app that never
// navigates would otherwise only check on load. Without a service worker it does nothing,
// and neither does it with data-reu-update-prompt="off" on <html>, for an app that handles
// its updates itself.
(function () {
    "use strict";

    if (!("serviceWorker" in navigator)
        || document.documentElement.getAttribute("data-reu-update-prompt") === "off") {
        return;
    }

    const checkInterval = 60 * 60 * 1000;
    const minCheckGap = 5 * 60 * 1000;

    let registration = null;
    let prompt = null;
    let isOpen = false;
    let isUpdating = false;
    let lastCheck = Date.now();

    function update() {
        const waiting = registration.waiting;

        if (!waiting) {
            location.reload();
            return;
        }

        // Reloading before the new worker takes over would only load the old version again.
        isUpdating = true;
        prompt.querySelector(".reu-update-reload").disabled = true;
        prompt.querySelector(".reu-update-reload").textContent = "Updating…";
        waiting.postMessage({ type: "SKIP_WAITING" });
    }

    function dismiss() {
        isOpen = false;
        prompt.remove();
        reattach.disconnect();
    }

    function createPrompt() {
        prompt = document.createElement("section");
        prompt.className = "reu-update-prompt";
        prompt.setAttribute("role", "status");
        prompt.innerHTML = `
            <div class="reu-update-title">A new version is available</div>
            <p class="reu-update-message">Updating reloads the page, so anything unsaved will be lost.</p>
            <div class="reu-update-actions">
                <button type="button" class="reu-button reu-update-later">Later</button>
                <button type="button" class="reu-button reu-button-blue reu-update-reload">Update</button>
            </div>`;

        prompt.addEventListener("click", function (event) {
            if (event.target.closest(".reu-update-later")) {
                dismiss();
            } else if (event.target.closest(".reu-update-reload")) {
                update();
            }
        });
    }

    function show() {
        if (isOpen) {
            return;
        }

        if (!prompt) {
            createPrompt();
        }

        isOpen = true;
        document.body.append(prompt);
        reattach.observe(document.documentElement, { childList: true, subtree: true });
    }

    // Enhanced navigation replaces the contents of <body> along with the prompt.
    const reattach = new MutationObserver(function () {
        if (isOpen && !prompt.isConnected) {
            document.body.append(prompt);
        }
    });

    // Without a controller, this is the first service worker rather than a new version of one.
    function watch(worker) {
        function check() {
            if (worker.state === "installed" && navigator.serviceWorker.controller) {
                show();
            }
        }

        if (worker) {
            worker.addEventListener("statechange", check);
            check();
        }
    }

    function checkForUpdate() {
        lastCheck = Date.now();

        // Being offline leaves the current version running, which is all there is to do.
        registration.update().catch(function () { });
    }

    navigator.serviceWorker.addEventListener("controllerchange", function () {
        if (isUpdating) {
            location.reload();
        }
    });

    navigator.serviceWorker.ready.then(function (ready) {
        registration = ready;

        watch(registration.waiting);
        watch(registration.installing);

        registration.addEventListener("updatefound", function () {
            watch(registration.installing);
        });

        setInterval(checkForUpdate, checkInterval);

        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "visible" && Date.now() - lastCheck >= minCheckGap) {
                checkForUpdate();
            }
        });
    });
})();

// Reuniverse modal focus.
//
// A modal dialog (role="dialog" or "alertdialog" with aria-modal="true", such as an open ReuModal) takes the
// focus when it appears, keeps Tab and Shift+Tab within it, and hands the focus back to where it
// was once it is gone, so the keyboard never ends up behind the backdrop or back at the start of
// the page. The dialog itself is focused unless something in it has autofocus, so that a screen
// reader starts with its title. While it is open, the rest of the page is inert, like behind a
// native modal <dialog>, so that nothing behind it can be focused, clicked, found or read out.
// The error panel and the update prompt stay usable. If this script never loads, the focus stays
// where it was and the page behind stays reachable.
(function () {
    "use strict";

    const dialogSelector = ':is([role="dialog"], [role="alertdialog"])[aria-modal="true"]';
    const focusableSelector = [
        "a[href]",
        "area[href]",
        "button:not([disabled])",
        "input:not([disabled]):not([type=hidden])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "iframe",
        "summary",
        "[contenteditable]:not([contenteditable=false])",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");

    // each open dialog and what had the focus before it appeared
    const openers = new Map();

    // each open dialog and the elements it made inert, so that closing it only undoes its own
    const inerted = new Map();

    // what stays usable behind a dialog: the panels of this script, which can appear at any time,
    // and elements that are never shown anyway
    const keepActiveSelector = "#blazor-error-ui, .reu-error-panel, .reu-update-prompt, .reu-tooltip, script, style, link, template";

    // Makes everything but the dialog and the elements it is in inert, going up from the dialog
    // and taking the siblings at every level. Elements that are inert already are left out,
    // so that they stay inert once the dialog is gone.
    function isolate(dialog) {
        const made = [];

        for (let node = dialog; node.parentElement && node !== document.body; node = node.parentElement) {
            for (const sibling of node.parentElement.children) {
                if (sibling !== node && !sibling.inert && !sibling.matches(keepActiveSelector)) {
                    sibling.inert = true;
                    made.push(sibling);
                }
            }
        }

        inerted.set(dialog, made);
    }

    function release(dialog) {
        inerted.get(dialog)?.forEach(function (element) {
            element.inert = false;
        });

        inerted.delete(dialog);
    }

    function topmost() {
        const dialogs = document.querySelectorAll(dialogSelector);
        return dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;
    }

    function focusables(dialog) {
        return Array.from(dialog.querySelectorAll(focusableSelector)).filter(function (element) {
            return element.tabIndex >= 0 && !element.closest("[inert]") && element.getClientRects().length > 0;
        });
    }

    function open(dialog) {
        openers.set(dialog, document.activeElement);
        isolate(dialog);

        if (dialog.contains(document.activeElement)) {
            return;
        }

        const target = dialog.querySelector("[autofocus]")
            ?? (dialog.hasAttribute("tabindex") ? dialog : focusables(dialog)[0]);

        target?.focus();
    }

    function close(dialog) {
        const opener = openers.get(dialog);
        openers.delete(dialog);

        // first, as an inert opener cannot take the focus back
        release(dialog);

        // only when the focus went away with the dialog, as it may have been moved on purpose
        const lost = !document.activeElement || document.activeElement === document.body;

        if (lost && opener instanceof HTMLElement && opener.isConnected) {
            opener.focus();
        }
    }

    function openAll(root) {
        if (root.matches(dialogSelector) && !openers.has(root)) {
            open(root);
        }

        root.querySelectorAll(dialogSelector).forEach(function (dialog) {
            if (!openers.has(dialog)) {
                open(dialog);
            }
        });
    }

    new MutationObserver(function (mutations) {
        openers.forEach(function (_, dialog) {
            if (!dialog.isConnected) {
                close(dialog);
            }
        });

        for (const mutation of mutations) {
            mutation.addedNodes.forEach(function (node) {
                if (node instanceof Element) {
                    openAll(node);
                }
            });
        }
    }).observe(document.documentElement, { childList: true, subtree: true });

    openAll(document.documentElement);

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Tab" || event.defaultPrevented) {
            return;
        }

        const dialog = topmost();

        if (!dialog) {
            return;
        }

        const items = focusables(dialog);
        const active = document.activeElement;

        if (items.length === 0) {
            event.preventDefault();
            dialog.focus();
            return;
        }

        const first = items[0];
        const last = items[items.length - 1];
        const outside = !dialog.contains(active);

        if (event.shiftKey && (outside || active === first || active === dialog)) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (outside || active === last)) {
            event.preventDefault();
            first.focus();
        }
    });
})();

// Reuniverse tab keys.
//
// ReuTabs switches tabs with the arrow keys, Home and End, which Blazor cannot cancel for some
// keys and not others. This keeps them from also scrolling the page. If this script never
// loads, the keys still switch tabs.
(function () {
    "use strict";

    const keys = new Set(["ArrowLeft", "ArrowRight", "Home", "End"]);

    document.addEventListener("keydown", function (event) {
        if (keys.has(event.key) && event.target instanceof Element && event.target.matches('[role="tablist"] > * > [role="tab"]')) {
            event.preventDefault();
        }
    });
})();

// Reuniverse split pane.
//
// A ReuSplitPane is resized by dragging its handle, or with the arrow keys, Home and End while the
// handle has the focus, and double-clicking the handle goes back to the initial size. The size of the
// first pane is kept in --reu-split-size as a percentage, so it scales with the split pane, and the CSS
// limits it to the minimum sizes. With data-reu-split-key, the size is also kept in localStorage and
// restored for split panes rendered later too. A "reu-split-resize" event with the new size in
// detail.size is raised on the split pane once a resize is done. All of this is client-side, so it
// works under every render mode. If this script never loads, the panes keep their initial size.
(function () {
    "use strict";

    const paneSelector = ".reu-split-pane";
    const handleSelector = ".reu-split-pane > .reu-split-handle";
    const storagePrefix = "reu-split:";
    const keyStep = 10;
    const largeKeyStep = 50;

    function isVertical(pane) {
        return pane.classList.contains("vertical");
    }

    function first(pane) {
        return pane.querySelector(":scope > .reu-split-first");
    }

    function handleOf(pane) {
        return pane.querySelector(":scope > .reu-split-handle");
    }

    // the size that a percentage in flex-basis is relative to
    function available(pane) {
        const style = getComputedStyle(pane);

        return isVertical(pane)
            ? pane.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom)
            : pane.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    }

    function firstSize(pane) {
        const rect = first(pane).getBoundingClientRect();
        return isVertical(pane) ? rect.height : rect.width;
    }

    function storageKey(pane) {
        const key = pane.getAttribute("data-reu-split-key");
        return key ? storagePrefix + key : null;
    }

    function load(key) {
        try { return localStorage.getItem(key); } catch (e) { return null; }
    }

    function save(pane) {
        const key = storageKey(pane);

        if (key) {
            try { localStorage.setItem(key, pane.style.getPropertyValue("--reu-split-size").trim()); } catch (e) { }
        }
    }

    function forget(pane) {
        const key = storageKey(pane);

        if (key) {
            try { localStorage.removeItem(key); } catch (e) { }
        }
    }

    function percentage(value) {
        return Math.round(value * 100) / 100;
    }

    function updateValue(pane) {
        const total = available(pane);

        if (total > 0) {
            handleOf(pane)?.setAttribute("aria-valuenow", String(Math.round(firstSize(pane) / total * 100)));
        }
    }

    // Sets the first pane to the size in pixels, as a percentage of what is available. The CSS keeps it
    // within the minimum sizes, so the size it ends up with is measured and kept instead.
    function resize(pane, size) {
        const total = available(pane);

        if (total <= 0) {
            return;
        }

        pane.style.setProperty("--reu-split-size", percentage(Math.max(0, size) / total * 100) + "%");
        pane.style.setProperty("--reu-split-size", percentage(firstSize(pane) / total * 100) + "%");
        updateValue(pane);
    }

    function finish(pane) {
        save(pane);
        updateValue(pane);
        pane.dispatchEvent(new CustomEvent("reu-split-resize", {
            bubbles: true,
            detail: { size: pane.style.getPropertyValue("--reu-split-size").trim() }
        }));
    }

    function enhance(pane) {
        const key = storageKey(pane);
        const saved = key ? load(key) : null;

        if (saved) {
            pane.style.setProperty("--reu-split-size", saved);
        }

        updateValue(pane);
    }

    function enhanceAll(root) {
        if (root.matches(paneSelector)) {
            enhance(root);
        }

        root.querySelectorAll(paneSelector).forEach(enhance);
    }

    let drag = null;

    document.addEventListener("pointerdown", function (event) {
        if (event.button !== 0 || !(event.target instanceof Element) || !event.target.matches(handleSelector)) {
            return;
        }

        const handle = event.target;
        const pane = handle.parentElement;
        const vertical = isVertical(pane);

        event.preventDefault();
        handle.focus();
        handle.setPointerCapture(event.pointerId);
        handle.classList.add("dragging");
        pane.classList.add("resizing");

        drag = {
            pane: pane,
            handle: handle,
            pointerId: event.pointerId,
            vertical: vertical,
            start: vertical ? event.clientY : event.clientX,
            startSize: firstSize(pane)
        };
    });

    document.addEventListener("pointermove", function (event) {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        const position = drag.vertical ? event.clientY : event.clientX;
        resize(drag.pane, drag.startSize + position - drag.start);
    });

    function endDrag(event) {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        const ended = drag;
        drag = null;

        ended.handle.classList.remove("dragging");
        ended.pane.classList.remove("resizing");

        if (ended.handle.hasPointerCapture(event.pointerId)) {
            ended.handle.releasePointerCapture(event.pointerId);
        }

        finish(ended.pane);
    }

    document.addEventListener("pointerup", endDrag);
    document.addEventListener("pointercancel", endDrag);

    document.addEventListener("dblclick", function (event) {
        if (!(event.target instanceof Element) || !event.target.matches(handleSelector)) {
            return;
        }

        const pane = event.target.parentElement;
        const initial = pane.getAttribute("data-reu-split-size") || "50%";

        forget(pane);
        pane.style.setProperty("--reu-split-size", initial);
        updateValue(pane);
        pane.dispatchEvent(new CustomEvent("reu-split-resize", { bubbles: true, detail: { size: initial } }));
    });

    document.addEventListener("keydown", function (event) {
        if (event.defaultPrevented || !(event.target instanceof Element) || !event.target.matches(handleSelector)) {
            return;
        }

        const pane = event.target.parentElement;
        const vertical = isVertical(pane);
        const step = event.shiftKey ? largeKeyStep : keyStep;
        let size;

        switch (event.key) {
            case vertical ? "ArrowUp" : "ArrowLeft":
                size = firstSize(pane) - step;
                break;
            case vertical ? "ArrowDown" : "ArrowRight":
                size = firstSize(pane) + step;
                break;
            case "Home":
                size = 0;
                break;
            case "End":
                size = available(pane);
                break;
            default:
                return;
        }

        event.preventDefault();
        resize(pane, size);
        finish(pane);
    });

    new MutationObserver(function (mutations) {
        for (const mutation of mutations) {
            mutation.addedNodes.forEach(function (node) {
                if (node instanceof Element) {
                    enhanceAll(node);
                }
            });
        }
    }).observe(document.documentElement, { childList: true, subtree: true });

    enhanceAll(document.documentElement);
})();

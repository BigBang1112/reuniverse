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
        }

        if (!tooltip.isConnected) {
            document.body.append(tooltip);
        }

        return tooltip;
    }

    function place() {
        const { width, height } = tooltip.getBoundingClientRect();
        const maxLeft = window.innerWidth - width - edgeGap;
        const fitsBelow = anchorBottom + height + edgeGap <= window.innerHeight;

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

        ensureTooltip().textContent = text;
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
            tooltip.textContent = text;
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
                button.textContent = "Copy log";
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
                <button type="button" class="reu-button reu-error-copy">Copy log</button>
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

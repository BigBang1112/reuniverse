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
// media queries (the `.navbar:not(.reu-nav-responsive)` rules) that ship in the CSS.
(function () {
    "use strict";

    function enhanceNavMenu(navbar) {
        if (navbar.classList.contains("reu-nav-responsive")) {
            return;
        }

        const container = navbar.querySelector(".reu-container");
        const nav = navbar.querySelector("nav");

        if (!container || !nav) {
            return;
        }

        const burgerCheckbox = navbar.querySelector("#nav-toggle");
        const burgerLabel = navbar.querySelector("label[for='nav-toggle']");

        // Switch from the media-query fallback to JS-driven, content-aware layout.
        navbar.classList.add("reu-nav-responsive");

        let frame = 0;

        function syncBurgerOpenState() {
            if (!burgerCheckbox) {
                return;
            }

            const isOpen = burgerCheckbox.checked && navbar.classList.contains("reu-nav-menu");
            navbar.classList.toggle("reu-nav-menu-open", isOpen);

            if (burgerLabel) {
                burgerLabel.setAttribute("aria-expanded", isOpen ? "true" : "false");
            }
        }

        function closeBurger() {
            if (!burgerCheckbox || !burgerCheckbox.checked) {
                return;
            }

            burgerCheckbox.checked = false;
            syncBurgerOpenState();
        }

        // Measures the width the bar needs in a given mode by letting the
        // container shrink to its content's natural (max-content) width. This is
        // the combined width of all buttons plus the brand, gaps and padding.
        // Callers run this inside the "reu-nav-sizing" state so the temporary
        // layout never animates.
        function requiredWidth(modeClass) {
            navbar.classList.remove("reu-nav-icons", "reu-nav-menu");

            if (modeClass) {
                navbar.classList.add(modeClass);
            }

            const previousWidth = container.style.width;
            container.style.width = "max-content";
            const width = container.getBoundingClientRect().width;
            container.style.width = previousWidth;

            return width;
        }

        function requiredBurgerWidth() {
            navbar.classList.remove("reu-nav-icons", "reu-nav-menu", "reu-nav-menu-open");
            navbar.classList.add("reu-nav-menu");

            const previousWidth = nav.style.width;
            nav.style.width = "max-content";
            const width = nav.getBoundingClientRect().width;
            nav.style.width = previousWidth;

            return width;
        }

        function update() {
            frame = 0;

            // Disable every transition in the navbar while we probe and switch
            // modes so none of the intermediate states paint or animate.
            navbar.classList.add("reu-nav-sizing");

            const fullWidth = requiredWidth(null);
            const iconsWidth = requiredWidth("reu-nav-icons");
            const burgerWidth = requiredBurgerWidth();

            navbar.style.setProperty("--reu-nav-menu-width", `${Math.ceil(burgerWidth)}px`);

            navbar.classList.remove("reu-nav-icons", "reu-nav-menu");
            const available = container.getBoundingClientRect().width;

            if (available < iconsWidth) {
                navbar.classList.add("reu-nav-menu");
            } else if (available < fullWidth) {
                navbar.classList.add("reu-nav-icons");
            }

            if (!navbar.classList.contains("reu-nav-menu")) {
                closeBurger();
            } else {
                syncBurgerOpenState();
            }

            // Flush the final layout while transitions are still off, then
            // re-enable them so the baseline matches the result and nothing
            // animates back.
            container.getBoundingClientRect();
            navbar.classList.remove("reu-nav-sizing");
        }

        function schedule() {
            if (frame === 0) {
                frame = requestAnimationFrame(update);
            }
        }

        // React to viewport changes (navbar width tracks the viewport, so
        // observing it never feeds back from our own internal class toggles).
        const resizeObserver = new ResizeObserver(schedule);
        resizeObserver.observe(navbar);

        // Recompute when items are added/removed or their text changes.
        const mutationObserver = new MutationObserver(schedule);
        mutationObserver.observe(nav, { childList: true, subtree: true, characterData: true });

        // Close the burger dropdown after navigating.
        nav.addEventListener("click", function (event) {
            if (burgerCheckbox && event.target.closest("a")) {
                closeBurger();
            }
        });

        if (burgerCheckbox) {
            burgerCheckbox.addEventListener("change", syncBurgerOpenState);
            syncBurgerOpenState();

            document.addEventListener("pointerdown", function (event) {
                if (!burgerCheckbox.checked || !navbar.classList.contains("reu-nav-menu")) {
                    return;
                }

                if (!navbar.contains(event.target)) {
                    closeBurger();
                }
            });

            document.addEventListener("keydown", function (event) {
                if (event.key === "Escape") {
                    closeBurger();
                }
            });
        }

        update();
    }

    function enhanceAll(root) {
        (root || document).querySelectorAll(".navbar").forEach(enhanceNavMenu);
    }

    function start() {
        enhanceAll(document);

        if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
            window.Blazor.addEventListener("enhancedload", function () {
                document.querySelectorAll(".navbar:not(.reu-nav-responsive)").forEach(function (navbar) {
                    enhanceNavMenu(navbar);

                    // Trigger the navbar's mutation observer after enhanced navigation.
                    const nav = navbar.querySelector("nav");
                    if (nav) {
                        const marker = document.createComment("");
                        nav.append(marker);
                        marker.remove();
                    }
                });
            });
        }

        // Enhance navbars that appear later (e.g. Blazor enhanced navigation or
        // interactive rendering replacing the DOM).
        const observer = new MutationObserver(function (mutations) {
            for (const mutation of mutations) {
                for (const node of mutation.addedNodes) {
                    if (node.nodeType !== 1) {
                        continue;
                    }
                    if (node.classList.contains("navbar")) {
                        enhanceNavMenu(node);
                    }
                    enhanceAll(node);
                }
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
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

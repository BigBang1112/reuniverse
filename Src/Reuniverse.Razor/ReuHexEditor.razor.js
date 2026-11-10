const navigationKeys = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageUp", "PageDown", "Home", "End"]);
const hoverAttribute = "data-reu-hover";
const handlers = new WeakMap();

// The editor handles these keys in .NET, which cannot cancel them for some keys and not others,
// so this keeps them from also scrolling the page or selecting it.
export function init(element) {
    const keydown = (event) => {
        const ctrl = event.ctrlKey || event.metaKey;

        if (navigationKeys.has(event.key)
            || (ctrl && !event.altKey && event.key.toLowerCase() === "a")
            || (!ctrl && !event.altKey && event.key.length === 1)) {
            event.preventDefault();
        }
    };

    // Marks the hovered byte in both columns and its column in the header, which CSS cannot link up.
    // An attribute rather than a class, as Blazor replaces the classes whenever it renders a byte.
    let hovered = [];

    const clearHover = () => {
        hovered.forEach(cell => cell.removeAttribute(hoverAttribute));
        hovered = [];
    };

    const mouseover = (event) => {
        clearHover();

        const cell = event.target instanceof Element ? event.target.closest(".b") : null;
        const row = cell?.closest(".hex-row");

        if (!row || row.classList.contains("hex-header") || !element.contains(row)) {
            return;
        }

        const index = Array.prototype.indexOf.call(cell.parentElement.children, cell);

        hovered = [
            row.querySelector(":scope > .hex")?.children[index],
            row.querySelector(":scope > .text")?.children[index],
            element.querySelector(".hex-header > .hex")?.children[index]
        ].filter(Boolean);

        hovered.forEach(cell => cell.setAttribute(hoverAttribute, ""));
    };

    element.addEventListener("keydown", keydown);
    element.addEventListener("mouseover", mouseover);
    element.addEventListener("mouseleave", clearHover);
    handlers.set(element, { keydown, mouseover, clearHover });
}

export function dispose(element) {
    const handler = handlers.get(element);

    if (handler) {
        handler.clearHover();
        element.removeEventListener("keydown", handler.keydown);
        element.removeEventListener("mouseover", handler.mouseover);
        element.removeEventListener("mouseleave", handler.clearHover);
        handlers.delete(element);
    }
}

// Scrolls just enough for the row to be fully visible below the header, which stays at the top.
export function scrollToRow(element, row, rowHeight) {
    const header = element.querySelector(".hex-header");
    const headerHeight = header ? header.offsetHeight : 0;
    const top = headerHeight + row * rowHeight;

    if (top < element.scrollTop + headerHeight) {
        element.scrollTop = top - headerHeight;
    } else if (top + rowHeight > element.scrollTop + element.clientHeight) {
        element.scrollTop = top + rowHeight - element.clientHeight;
    }
}

export function copyText(text) {
    return navigator.clipboard.writeText(text);
}

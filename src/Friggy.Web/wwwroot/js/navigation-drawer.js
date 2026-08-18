const focusableSelector = [
    "a[href]",
    "button:not([disabled])",
    "input:not([disabled])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "[tabindex]:not([tabindex='-1'])",
].join(",");

let activeDrawer;
let triggerElement;

export function open(drawerId, triggerId) {
    activeDrawer = document.getElementById(drawerId);
    triggerElement = document.getElementById(triggerId);
    if (!activeDrawer) return;

    document.body.classList.add("friggy-drawer-open");
    activeDrawer.addEventListener("keydown", trapFocus);
    const initialFocus = activeDrawer.querySelector("[data-drawer-initial-focus]");
    focusAfterRender(initialFocus);
}

export function close(triggerId) {
    if (activeDrawer) activeDrawer.removeEventListener("keydown", trapFocus);
    document.body.classList.remove("friggy-drawer-open");
    const trigger = document.getElementById(triggerId) ?? triggerElement;
    activeDrawer = undefined;
    triggerElement = undefined;
    focusAfterRender(trigger);
}

function focusAfterRender(element) {
    if (!element) return;
    requestAnimationFrame(() => requestAnimationFrame(() => element.focus({ preventScroll: true })));
}

function trapFocus(event) {
    if (event.key !== "Tab" || !activeDrawer) return;
    const focusable = getFocusableElements(activeDrawer);
    if (focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
    }
}

function getFocusableElements(container) {
    return [...container.querySelectorAll(focusableSelector)]
        .filter(element => !element.hasAttribute("hidden") && element.getAttribute("aria-hidden") !== "true");
}

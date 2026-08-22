const triggers = new WeakMap();

function focusables(dialog) {
    return [...dialog.querySelectorAll('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [href]')]
        .filter(element => !element.hasAttribute('hidden'));
}

function trapFocus(event) {
    if (event.key !== 'Tab') return;
    const items = focusables(event.currentTarget);
    if (!items.length) { event.preventDefault(); return; }
    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
}

export function show(dialog) {
    if (!dialog.open) {
        triggers.set(dialog, document.activeElement);
        dialog.showModal();
        dialog.addEventListener('keydown', trapFocus);
        focusables(dialog)[0]?.focus();
    }
}

export function close(dialog) {
    if (dialog.open) {
        dialog.close();
    }
    dialog.removeEventListener('keydown', trapFocus);
    triggers.get(dialog)?.focus?.();
    triggers.delete(dialog);
}

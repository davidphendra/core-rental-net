// Minimal interop for the native <dialog> element, which supplies the focus trap, the Esc
// handling and the inert background for free.
export function show(element) {
    if (element && !element.open) {
        element.showModal();
    }
}

export function close(element) {
    if (element && element.open) {
        element.close();
    }
}

// Move the caret into the first control so the customer can start typing immediately.
export function focusFirstField(element) {
    const field = element?.querySelector('input, textarea, select, button:not([disabled])');
    if (field) {
        field.focus();
    }
}

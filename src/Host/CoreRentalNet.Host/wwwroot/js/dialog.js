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

// The account menu is a disclosure: Blazor owns whether it is open, and this reports the two
// events a circuit cannot see for itself - a pointer that lands outside the menu, and Escape.
// The listeners live on the document because the pointer that closes the menu lands anywhere on
// the page, and they are captured so a handler that stops propagation cannot leave it open.

const cleanups = new WeakMap();

export function watch(element, dotNetRef) {
    if (!element) {
        return;
    }

    // Idempotent: opening, closing and reopening must not stack listeners.
    unwatch(element);

    const onPointerDown = (event) => {
        if (!element.contains(event.target)) {
            dotNetRef.invokeMethodAsync('CloseFromScriptAsync');
        }
    };

    const onKeyDown = (event) => {
        if (event.key === 'Escape') {
            dotNetRef.invokeMethodAsync('CloseFromScriptAsync');
        }
    };

    document.addEventListener('pointerdown', onPointerDown, true);
    document.addEventListener('keydown', onKeyDown, true);

    cleanups.set(element, () => {
        document.removeEventListener('pointerdown', onPointerDown, true);
        document.removeEventListener('keydown', onKeyDown, true);
    });
}

export function unwatch(element) {
    const cleanup = element ? cleanups.get(element) : null;

    if (cleanup) {
        cleanup();
        cleanups.delete(element);
    }
}

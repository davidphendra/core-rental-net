// Reads a suggestion run as it arrives, and stops it on request.
//
// The endpoint answers with server-sent events, and EventSource cannot POST - so this is a fetch whose body
// is read a chunk at a time. That is the whole reason this file exists: the alternative is a page that waits
// for a 45-second run in silence, which is what the streamed text is there to prevent.
//
// The frames are handed to .NET one at a time rather than collected and returned, because the point is that
// the customer reads each one as it completes.

let running = null;

/// Starts a run. `dotNet` is the component's reference; it is called back per frame.
export async function start(dotNet, query) {
    // One run at a time here as well as on the server: the guard refuses a second one, and a browser that
    // sent two would turn its own second request into a refusal it then had to explain.
    stop();

    const controller = new AbortController();
    running = controller;

    try {
        const response = await fetch('/api/builder/suggest', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ query: query }),
            signal: controller.signal,
        });

        // Refused before anything was streamed: no permission, a run already in flight, or an empty
        // request. The component words whichever it was.
        if (!response.ok) {
            await dotNet.invokeMethodAsync('Refused', response.status);
            return;
        }

        await read(dotNet, response, controller);
    } catch (error) {
        if (error && error.name === 'AbortError') {
            await dotNet.invokeMethodAsync('Stopped');
            return;
        }

        await dotNet.invokeMethodAsync('Broke', error && error.message ? error.message : 'the run failed');
    } finally {
        if (running === controller) {
            running = null;
        }
    }
}

/// Stops the run in flight, if there is one. Aborting the fetch is what cancels the agent call: the server
/// sees the connection go away, which is the same signal a reload produces.
export function stop() {
    if (running) {
        running.abort();
        running = null;
    }
}

/// Enter sends the request, and Shift+Enter starts a new line.
///
/// A textarea inserts a newline for Enter by itself, and nothing in the markup can tell that key from
/// Shift+Enter - so the browser is asked to stop instead, and the component is told. This is what the field did
/// when it was a single line, and it is the key a customer reaches for after typing a sentence.
export function submitOnEnter(element, dotNet) {
    element.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            dotNet.invokeMethodAsync('SubmittedFromKeyboard');
        }
    });
}

async function read(dotNet, response, controller) {
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let pending = '';

    while (true) {
        const { value, done } = await reader.read();

        if (done) {
            break;
        }

        pending += decoder.decode(value, { stream: true });

        // Frames are separated by a blank line, and a chunk can end anywhere - including in the middle of
        // one. What is left over stays in `pending` until the rest of it arrives.
        let boundary;

        while ((boundary = pending.indexOf('\n\n')) >= 0) {
            const frame = pending.slice(0, boundary);
            pending = pending.slice(boundary + 2);

            await deliver(dotNet, frame);
        }
    }

    // The stream ended. A run that ended without a result or a failure is one the component has to be told
    // about, or it would wait for something that is never coming.
    if (running === controller) {
        await dotNet.invokeMethodAsync('Ended');
    }
}

async function deliver(dotNet, frame) {
    let name = '';
    let data = '';

    for (const line of frame.split('\n')) {
        if (line.startsWith('event: ')) {
            name = line.slice('event: '.length);
        } else if (line.startsWith('data: ')) {
            const value = line.slice('data: '.length);
            data = data ? data + '\n' + value : value;
        }
    }

    if (name) {
        await dotNet.invokeMethodAsync('Frame', name, data);
    }
}

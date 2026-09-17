# Prompts

The text a model is sent, kept as a file so that changing it is a diff and reviewing it needs no code.

## Why `.txt` and not `.md`

The whole file **is** the prompt. It is loaded with `File.ReadAllText` and sent unchanged, so there is
no markup to strip, no heading that turns into instructions, and no way for a writer's formatting to
become part of what the model sees. Explainers like this one sit beside the prompts rather than inside
them, which is also why the rationale for a prompt is never a comment in the prompt: the model would
read it.

## The prompts

| File | Answers | Sees |
|---|---|---|
| `workspace-intent.txt` | is this a workspace request at all? | **the customer's words and nothing else** |
| `slot-classifier.txt` | which parts does the request mean? | the words, the parts that exist, and a previous attempt's objections |

The first is the thinnest context in the design, and that is deliberate: it runs before anything is
known and it is the gate the expensive path sits behind, so it is given nothing it could be misled by.
The second runs only when the intent table misses, and only gets to choose among parts that exist.

## The placeholders

| Placeholder | What fills it |
|---|---|
| `{{slots}}` | one line per part that exists: `- Desk: "a desk"` — the name, and the name the application calls it |
| `{{query}}` | the customer's own words, unmodified |
| `{{findings}}` | one line per objection from a previous attempt, or empty |
| `{{openMarker}}`, `{{closeMarker}}` | see below |

A prompt uses the ones it needs and no others: the intent prompt never sees `{{slots}}` or
`{{findings}}`, because it runs before there are any. A test holds every placeholder in every prompt
to this list, so a typo cannot ship as the literal string `{{slto}}` to a model.

Anything not filled is sent as it is, so an unfilled placeholder is visible in the prompt rather than
silently empty.

## The markers are generated per request

`{{openMarker}}` and `{{closeMarker}}` are a matched pair carrying a nonce the code makes for each
call — `<<request-7f3a9c>>` and `<<end-7f3a9c>>`.

This is the delimiter the content-safety policy refers to when it says catalogue content is delimited
wherever it reaches a prompt. A fixed delimiter is not one: the customer's text is the input, so a
customer who types the closing marker can leave the block and address the model directly. A nonce
cannot be guessed, so the block cannot be closed from the outside, and the prompt's rule about the
markers stays true however the request is written.

The same pair wraps the objections, for the same reason and at no extra cost.

## What is enforced in code, not here

A prompt is a request, not a guarantee. The answer is checked before it is used:

- every name is checked against the closed vocabulary, and against the parts the application actually
  sent, so an invented part is dropped rather than becoming one (`Rephraser.Known`);
- the slots are put in the application's order rather than the model's (`Rephraser.Requirements`);
- quantities are clamped to the capacity the request carried, never to a number a model produced;
- nothing the model writes is ever shown to a customer — only codes cross back to the application.

So the prompt asks for the right answer, and the code is what makes a wrong one harmless.

**The reason code is the sharpest case of this, and it is not yet handled.** The intent prompt names
`not_workspace_request` because the result schema's `code` enum holds that value and no other - and
`SuggestionWorkflow` writes the verdict's code into the result unexamined. A model that answered with a
plausible code of its own would produce a result the contract forbids, and the schema is the only thing
the application and the agent share. The prompt asks for the one code; the code that reads the verdict
must **map a negative verdict to that one code rather than pass the model's through**, the same way the
slot answer is checked against the vocabulary instead of trusted. That mapping is not written yet, and
it belongs with the classifier's implementation rather than in the prompt.

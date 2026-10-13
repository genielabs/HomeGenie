# Composite AI solutions

Widget Genie can answer one request with `solution_type: "composite"`, containing
`automation`, `widget`, `language`, `explanation` and `binding` (domain, address, properties).
The generated program is created through AI Action Executor 581, compiled and enabled.
Compiler diagnostics may be returned to the provider up to five times using the same
deployment request, retaining one program ID. A failed program remains disabled for inspection.
An uncertain mutation is not repeated automatically. Existing programs must be updated via Automan.

`$program` resolves to the assigned program ID only for the automation program domain.
The widget receives the actual bound module and must use its fields rather than a guessed ID.
The response saved in chat history is a versioned `ai_solution` envelope containing the actual
program result, deployment request ID and widget binding. No deployment occurs on history replay.

YoT separates the widget sources from the automation sources. The standard inline custom widget
preview waits for its declared module and all required non-null fields (zero and false are valid).
After 60 seconds without data, Retry observes for another minute; it never creates another program.
A failed or unknown deployment blocks the preview. Standalone widgets keep their existing flow.

Deployment runs during the provider request, with the normal chat request progress indicator.
The explicit data-wait placeholder appears once the composite answer has been received.
The readiness gate observes adapter data, not proof of hardware correctness. Dashboard installation,
composite updates, multiple programs/widgets and durable background task scheduling remain future work.

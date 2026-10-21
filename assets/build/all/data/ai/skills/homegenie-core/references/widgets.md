# SECTION 2: CUSTOM WIDGET DEVELOPMENT (ZUIX.JS FRONTEND)

Widgets are dynamically **loadable Web Components** running in Shadow DOM. They consist of **HTML (View)**, **CSS (Style)**, and **JavaScript (Controller)**.

### Required CSS dependency check before returning a widget

Inspect the FINAL HTML and CSS before emitting any ordinary, composite or presentation widget. Do not assume the host or another widget imports its styles: each widget is isolated in Shadow DOM.

- If the HTML uses ANY `layout` or `self` attribute, the widget CSS MUST import `assets/styles/flex-layout-attribute.min.css`.
- If the HTML uses `material-symbols-outlined` or `material-symbols`, the widget CSS MUST import `assets/styles/widget-ui.css`. This file supplies icon/control styling; it DOES NOT supply FLA layout rules. Importing it alone cannot implement `layout="column ..."`.
- Emit all required `@import` statements once at the very beginning of the CSS string, before selectors or other style rules. Do not put them in HTML, the controller, parent-document styles, or after CSS rules.

For a widget with BOTH layout attributes and Material Symbols, start CSS exactly with:

```css
@import 'assets/styles/flex-layout-attribute.min.css';
@import 'assets/styles/widget-ui.css';
```

Use the documented FLA vocabulary: `layout="row center-left"`, `layout="row center-justify"`, `layout="row center-right"`, `layout="column top-stretch"` or `layout="column center-center"`. Do not invent `center-between`, `center-start` or `center-end`. Put self alignment/sizing in a SEPARATE attribute, e.g. `self="stretch size-x1"`; never put `self-stretch` inside `layout`.

Final internal check: scan the HTML for layout/self attributes and icon classes, then verify their imports exist in the actual CSS output. If you intentionally avoid FLA, omit its attributes and implement all needed display/direction/alignment in explicit CSS instead. Keep fixed-size icons and compact controls from unintentionally inheriting full-row width. Shared `widget-ui.css` hides spans in disabled buttons; when navigation must keep its label/icon, override that behavior within the widget (e.g. `.nav-btn:disabled span { display: inline-block; opacity: 1; }`).

### 2.1 Critical Rules (VIOLATION = BROKEN WIDGET)
1.  **ASSET MANAGEMENT:**
    * Prefer local assets for UI and styling: Do NOT use external CDNs for Fonts, Icons, or CSS. Assume these are hosted locally.
    * Chart.js, Anime.js and Three.js are available as local JavaScript assets; use the entry points below. Other external JavaScript libraries require an explicit user request; do not silently add a CDN dependency.
2.  **ELEMENT ACCESS & STYLING:**
    *   **Access:** Use `this.field('my_el')` for elements declared with the `#` directive attribute. Use `this.view().find('valid_query_selector')` to get elements within the view. (Both return a ZxQuery Object).
    *   **Style:** Use `.css({ 'color': 'red' })`. **NEVER** use `.style` directly on `this.field()`. **DO NOT SET** the background color of a widget if you want it to inherit theme settings.
    *   **Raw DOM:** Use `this.field('my_el').get()` and `this.view().find('query_selector').get()` if you absolutely need the HTMLElement.
    *   **IMPORTANT:** Inside the controller `this` is the `ControllerInstance`, `this.$` is the view (ZxQuery). Inside any element of the View (HTML), `this` is the HTMLElement, `$this` is the ZxQuery-wrapped `this`, `context` is the ComponentContext with `context.model()` and other instance members.
3.  **HTML IDS (Binding):**
    *   Use **snake_case** (e.g., `#my_value`).
    *   **NEVER use CamelCase** (e.g., `#myValue` is INVALID).
4.  **SYNTAX & LOGIC:**
    *   **Events:** Never use `$event`. Always use `event` or the scoped context.
    *   **Interpolation:** Never use `{{ variable }}`. **NEVER use Mustache syntax.** Use the framework-specific data-binding via `#` attributes, or via `@` attributes (e.g. `@set="this.innerHTML = model.name"`).
    *   **Active Refresh (@):** Attributes starting with `@` (e.g., `@set`, `@if`, `@hide-if`) are evaluated **ONLY if the element is visible**. To force evaluation even when the element is not visible (e.g., `display: none` or off-screen), you **MUST** include the `@active` attribute on the same element.
    *   **Logic:** `@if` is a logic branch, not a display toggle. Manage visibility explicitly via CSS classes or `this.view().show()/hide()`.
5.  **UI SAFE ZONE:** The host overlays a settings button in the **Top-Right corner (48x48px)**.
    *   **Action:** Add `padding-right: 48px;` **ONLY** to your **Header** or **Top Row**.
6.  **CONTEXT SEPARATION:**
    *   **`this.model()`**: Reactive Proxy for text/values.
    *   **`this.declare()`**: View Context for Methods/Logic.
7.  **SHADOW DOM:** CSS is scoped. Use `:host { display: block; }`. The generated CSS must be bulletproof to prevent Shadow DOM layout collapse. Define the `:host` and root `.container` layout for the selected preset or free-size mode, following section 2.9.

### 2.2 Controller Configuration (`settings`)
Defining the `settings` object in the class is **mandatory**. You MUST always include `defaultSize`.

```javascript
class MyWidget extends ControllerInstance {
  settings = {
    moduleSelect: {
        fieldFilter: 'Status.Level',
        // CRITICAL LIGHTING DEFINITIONS FOR 'typeFilter':
        // - 'Light' = Specifically a binary light (0/1 ON/OFF)
        // - 'Dimmer' = Dimmable light; check the actual field's value range
        // - 'Color' = Color LED light (RGB/HSB)
        typeFilter: 'Light,Dimmer,Color'
    },
    /**
     * @type {'' | 'small' | 'medium' | 'big'}
     * @description The default widget size. Use an empty string '' for an unconstrained,
     * CSS-driven size. Let the CustomWidget host apply the selected size.
     */
    defaultSize: 'medium',
    // Allowed user presets. If defaultSize is '', consider keeping this empty [] to disable manual resizing.
    sizeOptions: ['small', 'medium', 'big']
  };
}
```

**Module selection contract:**
* Valid module types are `dimmer`, `color`, `doorlock`, `doorwindow`, `generic`, `light`, `mediareceiver`, `mediatransmitter`, `program`, `sensor`, `switch`, `shutter`, `motor`, `thermostat`, and `videoinput`. Matching is case-insensitive.
* Use comma-separated names without spaces. Types in `typeFilter` are alternatives (OR); fields in `fieldFilter` are also alternatives (OR). When both filters are set, the module must match BOTH groups (AND). Selection includes online modules only.
* For a general level display, use `fieldFilter: 'Status.Level'` and omit `typeFilter` unless the widget requires specific device types. A lighting widget must include `Color` when it supports color lights.
* An omitted, empty, or `any` filter imposes no restriction for that group. At least one active filter is required to populate the module selector; leaving both groups unrestricted currently produces an empty list.
* `moduleSelect` is OPTIONAL. Omit it for standalone widgets such as games, calculators, clocks, or interactive explanations. Such widgets must work without `boundModule`.
* If binding is needed, render a useful unbound state and expose a user action calling `this.configure()`. Check that `boundModule` and each required field exist before using them. A field list does not guarantee that every listed field exists.
* Current CustomWidget CSS declares `small` as 302x168px, `medium` as 302x316px, and `big` as 492x316px, with padding and card constraints applied separately. These are host layout declarations, not guaranteed inner canvas dimensions. Use the actual view size for canvas/chart sizing and adapt on resize.
* Include `defaultSize` in `sizeOptions` when manual resizing is enabled. Use `sizeOptions: []` to disable manual resizing when a free, CSS-driven layout is appropriate.

### 2.3 Controller Logic & Framework APIs
*   **`onInit()`**: Pre-load external libs (e.g., `zuix.using`).
*   **`onCreate()`**: **MAIN ENTRY POINT**.
    *   **Pattern:** Define a `viewContext` object with Methods and pass it to `this.declare(viewContext)`.
    *   **Binding Check:** Initialize the view and its methods first. If binding is needed and no module is associated, render the unbound state before returning. Standalone widgets must not depend on a bound module.
    *   **Subscribe (CRITICAL EXISTENCE CHECK):** You **MUST ALWAYS** verify that a field exists before subscribing to it. Failing to do so causes a FATAL runtime crash.
        *   *Correct:* `const f = this.boundModule.field('Sensor.Name'); if (f) { this.subscribe(f, callback); }`
*   **`onDispose()`**: **MANDATORY FOR MEMORY SAFETY**. You MUST always generate this method in the controller class. It is strictly required to clear interval/timeout handles with `clearInterval`/`clearTimeout`, cancel animation frames with `cancelAnimationFrame`, unsubscribe from subscriptions created directly by the widget, and remove event handlers registered outside the managed view to prevent memory leaks. Handlers registered with `this.view().on(...)` are automatically unregistered by the framework. Even if your code doesn't actively use timers yet, generate an empty `onDispose()` method.
*   **`this.apiCall('domain/address/method', payload)`**: *FRONTEND ONLY (UI/Widgets)*. Sends a request from the widget to a backend service. Returns an Observable.
    *   **🚨 CRITICAL PAYLOAD PARSING:** The subscription receives an object structured as `{ code: number, response: any }`. **You MUST use `data.response` to access the actual JSON/array returned by the backend API.** Do not assume the raw array/object is returned directly at the root.
    *   *Correct Usage:* `this.apiCall('Custom/MyApi').subscribe({ next: (data) => { const payload = data.response; /* process payload */ } });`
*   **`this.configure()`**: *FRONTEND ONLY*. Programmatically opens the custom widget configuration dialog (Bound Module Selection).
*   **`this.showSettings()`**: *FRONTEND ONLY*. Programmatically opens the module settings dialog.
*   **`this.translate(key)`**: Returns an Observable from `translate.get('WIDGETS.' + key)`. Subscribe to receive the translated string; do not assign the Observable to a text field. Track and unsubscribe any subscription created directly by your controller in `onDispose()`.
*   **`this.init, this.create, this.dispose, this.update, this.log`**: Are RESERVED methods NEVER OVERWRITE them.

### 2.4 State Management Strategy (CRITICAL)
When handling multiple fields, avoid passing values as arguments to a central update function.
*   **BAD:** `updateUI(level, color)` -> Prone to type mismatch errors.
*   **GOOD (Store-then-Render):**
    1.  **Store:** In the callback, parse and save to `this.state`.
    2.  **Render:** Call a parameter-less `this.refreshUI()` method which evaluates `this.state` and pushes changes to `this.model()`.

### 2.5 Component Interoperability (CRITICAL)
Components MUST be designed to interact seamlessly with their host environment and other components.
*   **`this.expose(name, functionOrValue)`**: Use this inside `onCreate()` to define a **Public API** for your widget. It automatically attaches methods or properties directly to the host HTML element.
    *   *Why:* Allows the parent page/component to control the widget (e.g., `document.querySelector('my-widget').reset()`).
    *   *Example:* `this.expose('reset', () => this.resetState());`
*   **`this.trigger(eventName, eventData)`**: Use this to notify the outside world about internal state changes, user interactions, or completed actions. It dispatches a standard DOM `CustomEvent`.
    *   *Why:* Allows parent components to listen to events using zuix directives like `(my-event)="handler(event)"` or vanilla `addEventListener`.
    *   *Example:* `this.trigger('value-changed', { newValue: this.state.level });`
*   **Golden Rule:** If a component receives user input (e.g., a button, a slider, a toggle), it **MUST** use `this.trigger` to emit its new value. If it has a controllable state (e.g., a timer, a media player), it **MUST** use `this.expose` to allow external control.

### 2.6 Visual Programming (Directives)
Directives operate on the context exposed by `this.declare()`.
*   **`@hide-if="methodName()"`**: **PREFERRED for VISIBILITY.** Hides element if true.
*   **`@if="methodName()"`**: Evaluates logic. **MANDATORY:** Must be paired with `@then` or `@else`. It does **NOT** hide elements by itself.
*   **`@then="..."` / `@else="..."`**: Executed based on `@if`. Inside strings, `$this` is the element wrapper (ZxQuery object), while `$` is the component view (ZxQuery object).
*   **`@active`**: **FLAG ONLY**. Keeps directives actively evaluated even if the element might be considered off-screen.
*   **`@set="js_expression"`**: Direct DOM execution.
*   **`@translate="key"`**: Replaces content with translation from `assets/i18n`.
*   **`z-load="path"`**: Loads a component. **Use `z-` prefix, NOT `data-ui-`**.

### 2.7 Data Parsing & Standards
*   **Field Values:** These can vary in type. You **must** always use `utils.format.fieldValue(f)` to obtain a formatted string that respects the user's unit settings and display preferences.
*   **Colors (HSB):** `Status.ColorHsb` format is `H,S,B,[Duration]`.
    *   **H, S, B** are floats `0.0 - 1.0`. Check `parts.length >= 3`.
    *   **STATE:** Check `Status.Level` for ON/OFF.
*   **Icons:** Use standard *Google's Material Symbols & Icons* (e.g., `lightbulb`, `light_off`). No `_fill` suffix.


### Material Symbols in widgets

Material Symbols are available from local YoT assets. Apply these instructions to ordinary widgets, composite widgets and chat widgets with `widget.mode="presentation"`.

When using icons, put this import at the beginning of the widget CSS, before style rules, even when the widget has its own nonempty CSS. Styles outside the widget Shadow DOM are not sufficient:

```css
@import 'assets/styles/widget-ui.css';
```

Use the icon name as the text content of a plain HTML span:

```html
<!-- Outlined -->
<span class="material-symbols-outlined" aria-hidden="true">lightbulb</span>
<!-- Filled -->
<span class="material-symbols" aria-hidden="true">lightbulb</span>
```

The shared CSS sets `FILL` to 0 for `material-symbols-outlined` and 1 for `material-symbols`. Use standard names such as `lightbulb`, `light_off`, `check_small` or `close_small`; do not invent a `_fill` suffix. Custom widgets use zuix.js rather than Angular, so do not use `<mat-icon>`. Do not load icon fonts or styles from Google Fonts or another external CDN.

For icons alongside text, use an inline-flex container with `align-items: center` and a suitable gap; set the icon's `line-height: 1`. Keep a visible label or accessible name on icon-only controls; `aria-hidden="true"` is appropriate for decorative icons when the control already has its own name.

### Local graphics and animation libraries

YoT copies these npm assets during build/serve. Load them on demand from the SAME frontend origin and base path, in ordinary, composite and presentation widgets. Do not replace them with a CDN, bare package imports, Angular components or another version.

| Library | Local entry point | Browser API |
| --- | --- | --- |
| Chart.js 4.5.1 | `assets/js/chartjs/chart.module.js` | Native ESM bundle; use its default `Chart` export. Independent of Monaco/AMD. |
| Anime.js 4.5.0 | `assets/js/animejs/anime.esm.min.js` | ESM named exports, e.g. `animate`, `stagger`, `createTimeline`. Use v4 APIs, not the v3 `anime(...)` API. |
| Three.js 0.186.1 | `assets/js/three/three.module.js` | ESM namespace. Its sibling `three.core.js` is copied too. |

Resolve paths against `document.baseURI`, not the controller's dynamic script URL or the backend adapter URL:

```javascript
// Inside an async controller method called from onCreate():
const { animate } = await import(new URL('assets/js/animejs/anime.esm.min.js', document.baseURI).href);
// Equivalent entries for Three.js and Chart.js:
// const THREE = await import(new URL('assets/js/three/three.module.js', document.baseURI).href);
// const { default: Chart } = await import(new URL('assets/js/chartjs/chart.module.js', document.baseURI).href);
if (this.disposed) return; // Set your own per-instance flag in onDispose().
```

Do not use top-level static `import` in a widget controller. Start asynchronous loading after the view exists, catch loading/rendering errors in the widget, and check disposal after each await before allocating resources. Module imports are cached by the browser; do not add per-widget cache-busting URLs. Do not insert scripts or import maps into the shared document. Three.js addons (OrbitControls, GLTFLoader, etc.) are NOT bundled: do not invent local addon paths or mix versions.

Prefer HTML/CSS/SVG for simple graphics; Chart.js for ordinary data charts; Anime.js for coordinated HTML/SVG animation; Three.js for useful 3D scenes. Avoid a library when native browser features are sufficient.

- Use actual DOM elements from `this.field(...).get()` or instance-scoped queries as animation targets. Global string selectors do not reach widget Shadow DOM reliably. Never change global Chart defaults or the shared Anime engine to control a single widget.
- Chart.js: give the canvas a dedicated positioned parent with a defined height. Use responsive mode or resize the instance to the measured container. Stop animation when inactive; destroy the chart in onDispose(). Static charts can use `animation: false`. Respect the UI locale in chart options.
- Anime.js: construct with `autoplay: false`, resume/pause the owned animations through the normal viewport/page lifecycle, and call `revert()` on disposal. Do not confuse Anime's exported `utils` with the host's `utils`; alias it if needed.
- Three.js: use the actual container size for renderer size and camera aspect; cap pixel ratio, e.g. at 2. Own one cancellable rendering chain. Rebase timestamps on resume. Dispose owned geometries, materials, textures, controls and renderer; release an owned WebGL context when permanently removing it. Show a useful fallback if WebGL is unavailable.
- Respect `prefers-reduced-motion`: start decorative scenes static or paused, while allowing deliberate user activation. All recurring work must follow section 2.12; asynchronous loading must never restart a disposed widget.
- zuix allocates its main active-refresh loop when the view contains an active `@` directive or a refresh script. If an otherwise static HTML view relies on `refresh:active`/`refresh:inactive`, add a neutral `@set="void 0"` directive (optionally `@delay="500"`) to its visible root container. `#` fields and `(click)` bindings alone do not enable that loop. Do not add `@active`, which would bypass normal visibility suspension.

Compact source examples are available as `examples/chart-pulse`, `examples/anime-orbit`, and `examples/three-knot` under the widgets directory. They are unbound, use sample/procedural content only and do not send device commands.

### Fluid width in chat previews

For a free-size widget that should occupy the available width in the AI chat preview, declare `settings = { defaultSize: '', sizeOptions: [], fillWidth: true };` in its controller. The optional `fillWidth` flag makes the outer preview widget grow AND shrink with the chat; `width: 100%` inside Shadow DOM alone cannot expand a content-sized outer host.

The flag takes effect only when the current widget has no small/medium/big size. Omit it (or set false) for widgets that should retain their intrinsic width. Inner content may still use its own max-width and centering; the flag fills the outer host, not every inner element. Keep chart wrappers responsive and give them a defined height. This option currently applies ONLY to chat previews, including ordinary previews and presentation mode; it does not change dashboard sizing or add dashboard resizing behavior.

### Practical widget conventions

- When using FLA `layout`/`self` attributes, also put `@import 'assets/styles/flex-layout-attribute.min.css';` at the beginning of the widget CSS, before rules. Imports outside Shadow DOM do not style its contents.
- Render current field values immediately, then subscribe to changes. `this.subscribe(field, handler)` does not replay the current value:
  ```javascript
  const field = this.boundModule?.field('Sensor.Temperature');
  if (field) {
    const render = () => this.model().temperature = utils.format.fieldValue(field);
    render();
    this.subscribe(field, render);
  }
  ```
- `Module.control(...)` dispatches immediately through WebSocket; its HTTP fallback returns a cold observable and needs a subscription to dispatch. Subscribe when observing responses/errors or managing pending UI, and retain directly owned subscriptions for cleanup. Do not subscribe twice to the same HTTP command observable. A successful API response is not proof of a physical device state change: display confirmed state from module fields. Do not automatically repeat commands on refresh/resume.
- For continuous controls, update the local visual value immediately, throttle device requests, and flush the final selected value on release. Cancel any pending trailing send before that flush and on disposal; do not let it overwrite the final value. Clean up pointer listeners and distinguish intentional command-completion timers from visual polling.
- Use a field's numeric `decimalValue` for calculations and `utils.format.fieldValue(field)` for presentation. Zero is valid; distinguish it from missing/invalid values. Clamp values and use the documented scale for each command/field: do not assume all levels use either 0..1 or 0..100.
- Keep mutable state, timers, rendering objects and observers on the controller instance. Avoid shared globals and fixed DOM IDs; multiple copies must coexist.
- Reuse existing registered custom elements when composing frontend widgets; use the documented module/options/ready contract of that component. Frontend composition is distinct from response `kind=composite`, which deploys a program and its dependent widget.
- Use `utils.preferences.ui.language || navigator.language` for locale-aware dates/numbers. Respect the configured display units and distinguish locale from an explicitly selected timezone. Do not hardcode the browser's language when the UI language is available.

### 2.8 Strict Coding Constraints
*   **NO HALLUCINATIONS:** Use **ONLY** the methods of the `utils` class explicitly mentioned in this prompt.
*   **DOM Manipulation Strict Rules:**
    *   **NEVER** use `$this.text(...)`. Use `$this.html(...)` only for trusted HTML. For plain text from users or APIs, use the raw element's `textContent` (for example, `this.field('label').get().textContent = value`) rather than inserting it as HTML.
    *   **FATAL ERROR WARNING:** **NEVER** use `.toggleClass(...)`. This method DOES NOT EXIST in zuix.js ZxQuery. You MUST use explicit `.addClass(...)` and `.removeClass(...)` calls.
*   **API SCOPE:** Do not confuse `this.apiCall` (Frontend UI) with `Api.Call` (Backend Program).

### 2.9 SHADOW DOM LAYOUT
* The CustomWidget host controls preset sizes. Do not impose a fixed 400px minimum height on a widget using `small`, `medium`, or `big`.
* For a preset-sized widget with an absolutely positioned root container, anchor that container to a positioned host:
    ```css
    :host {
        display: block;
        position: relative;
        width: 100%;
        height: 100%;
    }
    .container {
        position: absolute;
        inset: 0;
        padding: 12px;
        box-sizing: border-box;
        overflow: hidden;
    }
    ```
* Keep the background transparent to inherit the application's theme. If the requested design needs a background, use theme variables or an explicitly requested color.
* For `defaultSize: ''`, provide a content-driven layout or a suitable explicit/minimum host height. Percentage heights require a parent with a defined height; `min-height` alone does not make descendant percentage heights reliable. Use normal-flow content or an anchored container with a properly sized host.
* Canvas, charts, and games must measure their actual view size and update their dimensions on resize. Dispose any ResizeObserver or custom resize listeners in `onDispose()`.

### 2.10 FLEX SCROLL CONTAINERS (FLA)
When an element inside a flex column container (`layout="column ..."`) needs to scroll vertically while keeping headers or status bars fixed, ALWAYS apply the FLA attribute `self="size-x1"` to that scrolling element, combined with `overflow-y: auto;` and `min-height: 0;`. This constrains the element along the container's main flex axis, preventing layout collapse and guaranteeing smooth, isolated vertical scrolling.

### 2.11 Interactive Previews in AI Chat
* Generated widgets can run directly inside a chat response through the same `CustomWidget` component used on dashboards. The preview uses the same controller APIs, settings menu, module filters, and size presets.
* Module association and size changes in a chat preview are temporary. Showing a preview does not install the widget or add it to a dashboard; the user uses the separate Save action for installation.
* Standalone interactive components, including small games, must work without a module or backend calls when those are unnecessary. Device/API commands must follow the user's requested behavior and must not be issued automatically merely because a preview was loaded.
* The host may recreate or dispose a preview when the chat is closed or its source changes. Initialize state in `onCreate()` and release owned resources in `onDispose()`. Do not depend on the preview DOM remaining alive or on a hard-coded DOM ID outside the widget.

### 2.12 VIEWPORT VISIBILITY & ACTIVE REFRESH (MANDATORY FOR TIMERS/CANVAS/3D)
* Any widget with periodic updates or calculations, `setInterval`, repeating `setTimeout`, `requestAnimationFrame`, Canvas or Three.js rendering MUST implement pause/resume. Register the lifecycle handlers in `onCreate()` before starting recurring work.
* `this.view().on('refresh:inactive', ...)`: mark the viewport inactive and stop owned animation loops and periodic timers. Cancel scheduled handles; do not keep a loop running merely to skip drawing.
* `this.view().on('refresh:active', ...)`: mark the viewport active, synchronize current data and view dimensions, then resume only if the page is visible. Refresh the display immediately; do not wait for the next interval or replay user/device actions.
* Track viewport activity separately from page visibility. Register `document.addEventListener('visibilitychange', handler)` to pause while `document.hidden` is true and resume only when BOTH the viewport is active and the page is visible. Reuse the same pause/resume routines; do not add a second viewport observer or use `@active` to bypass suspension.
* Make resume and pause idempotent: at most one animation chain and one instance of each periodic timer. Rebase animation timestamps on resume to avoid a large first-frame delta. Async callbacks must not recreate loops after pause or disposal.
* `onDispose()`: mark the controller disposed, permanently stop all owned animation/timer handles, remove the document visibility listener, unsubscribe directly owned subscriptions and dispose owned renderers, controls, geometries/materials and observers as applicable. Pause keeps reusable rendering resources; final disposal releases them. Never resume a disposed controller.
* Events registered with `this.view().on(...)` are automatically unregistered on disposal. Cleanup is still required for timers, rendering resources and handlers registered directly on `document`, `window` or other external objects.


---


### Chat presentation mode

The version-1 widget/composite response accepts optional `widget.mode: "presentation"`. The existing contract applies when it is omitted. A presentation is a temporary inline conversational widget: its sources and Save/editor controls are hidden, and the host widget menu is disabled. `explanation` supplies optional surrounding text; it must not repeat the source code. Complete sources and the refresh/disposal lifecycle are still required. Free sizing (`defaultSize: ''`, `sizeOptions: []`) is supported for responsive scenes and controls. The host creates presentation widgets only from final responses, not partial streamed artifacts. Source and mode survive history reload; transient controller state does not.

Presentation widgets can send a user choice back to their original AI conversation:

```javascript
// Call from a user gesture, never create/resume/refresh. Keep eventId stable for this logical choice.
this.apiCall('System.Utility/AiGenie/Chat.Feedback', {
  eventId: 'portal-choice-1', event: 'choice-selected',
  data: {scene: 'portal', choice: 'roots', label: 'Follow the roots'}
}).subscribe({next: res => {
  const receipt = res.response;
  // Disable this choice after Status === 'Ok'; the chat displays the assistant continuation.
  // On busy, let the user try later. On unknown, inspect history before any retry.
}, error: error => { /* Unconfirmed outcome: inspect the original conversation. */ }});
```

The host supplies adapter/provider/source-request correlation; do not supply or invent those IDs. Only completed presentation messages with request metadata in a feedback-capable provider support this route. `eventId` (1–128 characters) and `event` (1–80) use ASCII letters, digits, `_ . : -`; `data` is a JSON object and the complete request must fit within 16 KiB. The event is reference data representing a user selection, never a new policy or system instruction. Existing context/capability settings apply; a changed context profile requires a new presentation.

This continues the original provider conversation and saves only the assistant reply, with the event in its metadata. No USER row is added. Future model context includes the selection. A completed event survives history reload and repeating the same eventId/data returns its saved result; reusing an ID with different data is rejected. After an unconfirmed result, never retry effects automatically. Recovery guards for unconfirmed outcomes are in memory and do not survive a provider/shared-service restart. Deleted presentations and cleared history cannot be continued. Local-only animations/counters need no feedback call. Dispose subscriptions and listeners with the normal widget lifecycle.



### Speech from widgets

- `utils.speech.say(sentence)` speaks a **plain text string** using the same configured speech engine (browser/local), language and voice settings as AI chat, even when the chat is closed. Do not send widget source, HTML, JSON or the whole AI response to this API.
- It returns a Promise resolving to `"completed"`, `"cancelled"` (replaced/stopped), or `"empty"`. Handle rejection (engine unavailable/not ready, synthesis failure or browser autoplay restrictions) in the widget UI. The local engine must be enabled and its model ready; there is no silent fallback to another engine.
- `utils.speech.stop()` cancels the current shared speech channel. A new `say` call replaces prior chat/widget speech; avoid loops and overlapping automatic narration. Prefer an explicit user action such as a Speak button: browsers may require it for playback. The chat automatic-reply speech toggle does not disable an explicit widget call.
- Pass only the sentence to narrate. There is no caption/text-overlay API yet. Do not invent voice/engine options or import external TTS libraries.

```javascript
// In the widget's click handler; show a concise error in your own UI if needed.
utils.speech.say('The living room temperature is 22 degrees.')
  .catch(error => { console.warn('Speech unavailable:', error.message); });
```

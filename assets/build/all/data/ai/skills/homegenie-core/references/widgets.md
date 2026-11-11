#use-kb homegenie-core/references/widget-authoring.md
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
*   **`(click)="toggleLight('light1')"`** calls a function exposed by `this.declare({toggleLight: role => this.toggleLight(role)})`. Call the declared name directly in the template. A `context.` prefix will not resolve that function.
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
    *   **LIGHT STATE (widget JavaScript):** `this.boundModule`, `this.getBoundModule(name)` and `this.getModule(id)` resolve YoT `Module` objects. Read `module?.field('Status.Level')?.decimalValue`: zero is off, a finite positive level is on, and a missing or invalid level is unknown. Render the initial reading and subscribe to `Status.Level` changes. For light brightness, `Status.Level` is a fraction from 0 to 1. Display `Math.round(level * 100)` in a 0–100 slider. Send the slider percentage as a separate argument, for example `module.control('Control.Level', 42)`.
    *   **API boundary:** `IsOn` and `IsOff` are C# automation helpers. In widget JavaScript, use `isOn` or `isOff` only as state derived by your view; the YoT module exposes the field through `module.field(...)`.
*   **Icons:** Use standard *Google's Material Symbols & Icons* (e.g., `lightbulb`, `light_off`). No `_fill` suffix.


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

The bundled `basic/energy-today` and `isometric/isometric-room` widgets show local Chart.js and Three.js imports.

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

### Dashboard container widgets

Use `settings = {defaultSize: 'dashboard', sizeOptions: []};` for a widget intended to replace a dashboard's standard grid. Declare these settings as a literal class field on the controller. The dashboard settings offer Layout → Custom Widget and list these widgets. Selection is manual; saving a widget does not apply it to a dashboard.

In this mode the host supplies the available dashboard width and height below YoT navigation, with scrolling for overflowing content. Dashboard containers have no overlaid settings button: omit the ordinary widget safe zone, including any reserved top-right padding or margin. Keep only spacing required by the dashboard design. Use responsive width:100%, min-height:100% and normal-flow content; avoid viewport units, fixed positioning over the application and an extra outer scrollbar. Compose previously saved child widgets through their actual Web Component tags/imports. `boundModule` and `moduleSelect` are optional: omit them for a self-contained panel and use confirmed API/module IDs where needed.

In chat, the editor and the widget list, the same widget uses a bounded, scrollable preview. Do not implement a separate Dashboard button or request browser fullscreen. The Standard layout and its widgets are retained when the user switches layouts.


### Embedded widgets and YoT context

Import the saved widget's `.module.js` and use its registered custom tag. In YoT the generated loader delegates to `ZxWidget`, providing `apiCall`, `subscribe`, translation and the enclosing widget's context. Outside YoT it loads as an ordinary zUIx Web Component; autonomous widgets remain usable without HomeGenie.

Use native zUIx option attributes (their values are JavaScript expressions evaluated in the parent component):

```html
<!-- No module option: inherit the enclosing widget's boundModule. -->
<examples-sensor></examples-sensor>
<!-- Explicit module, using the enclosing adapter by default. -->
<examples-sensor :module="'HomeAutomation.Demo:1'"></examples-sensor>
<!-- Select a different configured adapter when needed. -->
<examples-sensor :module="'HomeAutomation.Demo:1'" :adapter="'adapter-id'"></examples-sensor>
<!-- Explicitly unbound: do not inherit the parent module. -->
<examples-clock :module="null"></examples-clock>
```

These are syntax examples: use actual saved component tags, confirmed module IDs and configured adapter IDs. `domain:address` identifies a bound module; API calls still use `domain/address/command`. Binding is available before `onCreate()`, including through nested components. An unknown explicit module stays unbound; it never silently uses the parent module. The controller accesses `this.boundModule` as usual. Check its presence and render unavailable readings as unavailable, not static values suggesting live data. A widget that controls several modules can stay unbound and use `this.apiCall`.

The parent layout assigns each embedded widget its dimensions; `defaultSize` does not automatically size a raw custom tag. Use compact, responsive slots unless full-height cards are explicitly requested. Do not stretch every card across the remaining dashboard height by default. Child widgets that use height:100% need a slot with a defined height.


### Optional dashboard automation

Use a HomeGenie automation program when the requested dashboard needs background work, shared options, or periodic data such as news from an external service. Keep fetches in the program with bounded timeouts and a sensible refresh interval. Declare editable options with `Program.AddOption` inside `Program.Setup`. Read them in the program and publish only display-safe values and status through `Program.Emit`, including initial values and later changes. Keep credentials in the program. The widget renders current fields and subscribes to module events, showing loading, stale or unavailable data when appropriate.

For a reusable dashboard, declare a named `dashboardProgram` role so the user can select a compatible program with the Bound Modules button in the dashboard toolbar. In the program's Setup, create a stable marker property for that dashboard's data contract. For example, put this line inside `Program.Setup(() => { ... });`:

```csharp
Program.Parameter("Dashboard.Theme.Cyberpunk").Value = "";
```

In the dashboard controller, filter on the same property name:

```javascript
settings = {
  defaultSize: 'dashboard',
  sizeOptions: [],
  boundModules: {
    dashboardProgram: {
      label: 'Dashboard program',
      typeFilter: 'program',
      fieldFilter: 'Dashboard.Theme.Cyberpunk'
    }
  }
};
```

`fieldFilter` checks for the field name. An empty value is sufficient, and expressions such as `Dashboard.Theme=cyberpunk` are unsupported. Use the same marker in Setup and in the selector, and use distinct markers for dashboards with incompatible data contracts. Resolve the selection with `this.getBoundModule('dashboardProgram')`. The toolbar opens options for the program resolved through this role. To suggest a program, set defaultModule on the role to a confirmed module ID. The default must be online and match the role filters. Set defaultField to require an extra field on the default only. The manual selector still uses the normal role filters. For example, bundled themes require Simulator.Meter.Watts before automatically binding a Hue address used by the demo. The bundled theme programs expose Disable animations through Dashboard.AnimationsDisabled. Read its initial value, subscribe to changes, and honor reduced motion and visibility. A saved selection or explicit disconnection takes precedence. Show a useful state while the role is unbound.

For a dashboard dedicated to a program created by an objective, save the program first and use `this.getModule('HomeAutomation.HomeGenie.Automation:<PID>')` with the real PID from the host receipt. Keep the same association during revisions. When the resolved program has options, a labelled gear button in the dashboard can call `this.showSettings(program.id)` on user click. Omit or disable the button when the program is unavailable. This opens the program's options, which are shared by every dashboard that uses that program. Use a dashboard-local gear for fixed modules that have no dashboardProgram role.

### Configurable named module bindings

For every device or sensor a dashboard or multi-device widget can read or control, add a selectable role in settings.boundModules and use this.getBoundModule(role). Keep the role available when no compatible device is currently assigned. To preselect a device, use a confirmed compatible ID as defaultModule so users can still change it. Hardcode an ID only for an intentionally fixed integration.

Use real readings, names and states from the selected modules. Treat numbers, status indicators and device labels in reference images as visual examples. If a feature has no real data or integration, show it as unavailable or omit it. Disable its controls. Use clearly labeled simulated data only when the user asks for a demo or simulation.

Example:

```javascript
settings = {
  defaultSize: 'medium',
  boundModules: {
    light: {label: 'Light', typeFilter: 'Light,Dimmer,Color'},
    climate: {label: 'Room sensor', fieldFilter: 'Sensor.Temperature,Sensor.Humidity'}
  }
};
// In onCreate(): render current values first, then listen for changes.
const sensor = this.getBoundModule('climate');
const render = () => { this.model().temperature = sensor?.field('Sensor.Temperature')?.value ?? '—'; };
render();
if (sensor) this.subscribe(sensor, render);
```

Role labels should use the user's language; role keys remain stable during revisions. Use `module.name` to display the selected device's name. Use the role label (for example, "Light 1") in settings and as a fallback when no device is assigned. YoT provides one selector per role and persists selections per dashboard/widget instance; editor/preview selections are temporary. A selection change reloads the instance and disposes its managed subscriptions. Do not save bindings with API calls or browser storage, and do not select or command devices automatically during initialization. An unselected role returns `undefined`: show an informational unconfigured state. Configure roles centrally through the container menu; do not repeat association buttons inside child widgets.

A container passes a role to a saved child's primary binding with `:module="bm('light')"`; use the child's actual registered tag and import. `bm` is supplied by YoT and returns the Module object directly: do not redefine or declare `bm`, or reconstruct a reference from `.domain`/`.address` (these are not YoT Module properties). When a string ID is required, use the complete `module.id`. The child continues using `this.boundModule`. Children can also call `this.getBoundModule('light')` to inherit named roles. Local explicit selections override ancestors; explicit null/empty/unavailable selections stop inheritance. `:module="null"` disables only the primary binding. Never substitute another device for a missing role.

Keep `settings.moduleSelect` / `this.boundModule` for ordinary single-device widgets; both contracts may coexist. Named roles use the same type/field filter rules, but with no filters they offer all online modules. Preserve complete IDs, including `MQTT:device-id.domain:address`; do not split IDs on colons or introduce `adapterId@...`. Use named bindings for configurable devices; `this.getModule(id)` remains available for intentionally fixed, confirmed targets.

### Reactive multi-module state and command feedback

Inside YoT, `this.getModule('domain:address', adapterId?)` returns the current frontend `Module` reference without an HTTP request. It is available before `onCreate()` in ordinary and embedded widgets, even when the widget has no bound module. The default adapter is the widget's context (bound module / enclosing widget); an explicit adapter ID selects another configured adapter. Unknown IDs return `undefined`; never silently select the same module ID on a different adapter. Do not invent `utils.getModule`, `utils.yot`, or `this.utils` lookup APIs.

Render current values immediately, then use `this.subscribe(field, handler)` for one field or `this.subscribe(module, handler)` for all property events, including fields that appear later. Both return an unsubscribable subscription and are automatically released when this widget instance unloads. They do not replay an initial value. These are event subscriptions, not HTTP polling.

```javascript
// In onCreate(). Use a confirmed module ID.
const light = this.getModule('HomeAutomation.PhilipsHue:2');
const render = () => {
  const level = light?.field('Status.Level')?.decimalValue;
  const available = Number.isFinite(level) && level >= 0;
  this.model().available = available;
  this.model().level = available ? level : null;
  this.model().isOn = available && level > 0;
};
render();
if (light) {
  this.subscribe(light, field => {
    if (field.key === 'Status.Level') render();
  });
}
```

For a multi-light panel, resolve each confirmed module and keep independent state and subscriptions for each. Commands can use `module.control(command).subscribe(...)` or `this.apiCall` with the documented API path. Track directly owned `control` subscriptions and timers in `onDispose()`. Show sending/accepted/error feedback for the command receipt, but derive ON/OFF/level indicators from authoritative module fields; updates must also appear when another widget/app controls the device. A positive receipt alone is not confirmation of the target state. Use a bounded timeout for absent confirmation if necessary; do not retry the command automatically or fabricate success. Display unavailable/unknown for missing modules or fields. Prefer existing module/field events over recurring `Modules.Get`/`setInterval` polling.


### Flex alignment for embedded widgets

With flex-layout attributes, row alignment is cross-axis then main-axis; column alignment is main-axis then cross-axis. To stretch embedded widget slots vertically in a horizontal panel, use `layout="row stretch-center"`, not `row center-stretch`. Changing only row/column can collapse slots containing absolutely positioned widget content. Preserve nonzero slot dimensions when revising a composite layout.


### Applying a dashboard from an objective

Saving a container alone does not change the dashboard. In a widget objective, when the user explicitly asks to create or apply a dashboard, first save every widget, then use the host action `apply_dashboard` with `name` (the owned container leaf), `dashboardName` (a short human name) and `mode` (`create` or `update`). The host targets the initiating client preset and checks its revision. Do not dispatch generic preset writes or include client configuration in model output. Claim application only after a successful host receipt. A conflict leaves the saved widgets intact. Revisions preserve the dashboard name, preset and container ID; same-container bindings and Standard widgets are retained. The host provides an Open dashboard action in the chat.

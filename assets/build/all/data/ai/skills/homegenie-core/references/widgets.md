# SECTION 2: CUSTOM WIDGET DEVELOPMENT (ZUIX.JS FRONTEND)

Widgets are dynamically **loadable Web Components** running in Shadow DOM. They consist of **HTML (View)**, **CSS (Style)**, and **JavaScript (Controller)**.

### 2.1 Critical Rules (VIOLATION = BROKEN WIDGET)
1.  **ASSET MANAGEMENT:**
    * Prefer local assets for UI and styling: Do NOT use external CDNs for Fonts, Icons, or CSS. Assume these are hosted locally.
    * EXCEPTION: You MAY use CDNs ONLY for complex third-party JavaScript libraries (e.g., Three.js, GSAP) if they are strictly necessary for the requested functionality.
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
*   **`onDispose()`**: **MANDATORY FOR MEMORY SAFETY**. You MUST always generate this method in the controller class. It is strictly required to clear interval/timeout handles with `clearInterval`/`clearTimeout`, cancel animation frames with `cancelAnimationFrame`, unsubscribe from subscriptions created directly by the widget, and remove custom event handlers to prevent memory leaks. Even if your code doesn't actively use timers yet, generate an empty `onDispose()` method.
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

---


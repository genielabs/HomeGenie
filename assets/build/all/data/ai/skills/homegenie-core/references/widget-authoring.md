# SECTION 2: CUSTOM WIDGET DEVELOPMENT (ZUIX.JS FRONTEND)

A widget has an HTML view, scoped CSS and a JavaScript controller. It runs in Shadow DOM. Use local assets and query the widget view instead of using global DOM selectors.

Write the complete controller as a class declaration, e.g. `class MyWidget extends ControllerInstance { ... }`. The host registers it. End after the closing brace. Do not add `export default`, any other `export`, or static `import` statements. Use dynamic `import()` inside a method if a local library is needed.

### 2.1 Connect the click, state and visible output

Use the same name in the view and the model. The following fragments combine patterns from the shipped temperature display and toggle button. Put the shown `onCreate()` method inside the controller class.

```html
<button type="button" (click)="activate()"><span #status_text></span></button>
```

```javascript
onCreate() {
  const model = this.model();
  model.status_text = 'Ready';
  this.declare({activate: () => { model.status_text = 'Active'; }});
}
```

- `this.model()` returns the reactive model. Set `model.status_text` to update the text in `#status_text`. An `id="status_text"` alone does not bind the text.
- `(click)="activate()"` calls the declared function directly. Use arrow callbacks to capture the controller or the model. Use `event` in the directive when passing the event.
- Use snake_case for `#` field names. `this.field('status_text')` and `this.view().find('.selector')` return ZxQuery wrappers. Use `.css({...})` for styling or `.get()` for the native element.
- In a view expression, `this` is the HTML element, `$this` is its ZxQuery wrapper and `context` is the ComponentContext. In controller methods, `this` is the controller. Use `@set` for work on view refresh and `(click)` for interaction. Never use Mustache interpolation. `@if` needs `@then` or `@else` and does not hide the element. Use `@hide-if` for visibility. Use the bare `@active` flag for directives that must run while hidden.

### 2.2 Style and host settings

- Put CSS imports before style rules. When HTML uses `layout` or `self`, import `assets/styles/flex-layout-attribute.min.css`. When it uses Material Symbols, import `assets/styles/widget-ui.css`. Import both if both are used. Without layout attributes, define the layout in CSS.
- Use `<span class="material-symbols-outlined" aria-hidden="true">lightbulb</span>` for an outlined icon or `class="material-symbols"` for filled. Use standard icon names without a `_fill` suffix. Give icon-only controls an accessible name. Shared CSS hides spans inside disabled buttons, so override that rule locally if a disabled button must retain its icon.
- Valid layout examples include `row center-left`, `row center-justify`, `row center-right`, `column top-stretch` and `column center-center`. Set sizing separately with `self="stretch size-x1"`.
- Define `settings` with `defaultSize` (`small`, `medium`, `big` or `''` for CSS-driven sizing). Include the default in `sizeOptions` when resizing is enabled. Use `sizeOptions: []` to disable resizing. Size charts and canvases to the actual view.
- Use `:host { display: block; }` and explicit root layout. Let the host supply the card background and configuration menu. Reserve header space on ordinary widgets with `padding-right: var(--widget-menu-space, 0px)`. Dashboard containers need no fixed menu space.
- Load CSS, fonts, icons and bundled libraries locally. Chart.js, Anime.js and Three.js are available locally. Load other external JavaScript libraries only when explicitly requested.

### 2.3 Bind real devices only when needed

When a widget reads or controls a device, make the device selectable with `settings.moduleSelect` and use `this.boundModule`. Use real values and show missing data as unavailable. Simulate only when requested. Widgets with no device can omit the selector. An embedded widget leaves configuration to its host. An ordinary widget can open its settings with `this.configure()`.

Example from the shipped temperature display, inside `onCreate()`:

```javascript
const field = this.boundModule?.field('Sensor.Temperature');
const model = this.model();
if (field) {
  const render = () => { model.temperature = utils.format.fieldValue(field); };
  render();
  this.subscribe(field, render);
}
```

Pair it with `<span #temperature></span>`. For a control, capture the resolved module with `const module = this.boundModule;`, then declare the click handler with `this.declare({toggle: () => module?.control('Control.Toggle')});`. Render device state from the initial field value and subscription updates.

- Use comma-separated filter values without spaces. Types within `typeFilter` and fields within `fieldFilter` are OR lists. With both filters set, both must match. The selector lists online modules and needs at least one active filter. Valid types are `dimmer,color,doorlock,doorwindow,generic,light,mediareceiver,mediatransmitter,program,sensor,switch,shutter,motor,thermostat,videoinput`.
- For a level display, use `fieldFilter: 'Status.Level'`. For lighting, include `Color` if RGB lights are supported. Read brightness as `module?.field('Status.Level')?.decimalValue` on a 0–1 scale. Treat zero as off, positive values as on and missing or invalid values as unknown. Display it as a percentage and send percentage commands separately, e.g. `module.control('Control.Level', 42)`.
- For a power meter, use `fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts'` and `includeDomains: 'HomeAutomation.EnergyMonitor'`. Prefer `EnergyMonitor.WattLoad` when present for both live readings and history. `module.control('Statistics.Field.GetData', options)` takes Unix milliseconds for `rangeStart` and `rangeEnd` and emits `{x, y}[]` directly. Sort samples oldest first and merge real live readings. Show an empty state if no data is available.
- `this.apiCall(path, payload)` returns an Observable of `{code, response}`. Read `.response` for the payload. `this.showSettings(moduleId?)` opens module options. `this.translate(key)` returns an Observable for the `WIDGETS.` translation key. Subscribe to get its text.

Use `onInit()` for preloading and `onCreate()` for view setup. Include `onDispose()` to clean up timers, animation frames, manually owned subscriptions and listeners registered outside the widget view. It may be empty for a simple widget. Leave the reserved methods `init()`, `create()`, `dispose()`, `update()` and `log()` to the framework.

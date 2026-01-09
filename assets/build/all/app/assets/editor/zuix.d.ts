/**
 * @description Options object supplied when loading a component.
 */
declare class ContextOptions {
  /**
   * The context ID. HTML attribute equivalent: *z-context*. If not specified it will be randomly generated.
   */
  contextId?: any;
  /**
   * The container element where the component's view will be attached.
   */
  container?: Element;
  /**
   * The initial data model. HTML attribute equivalent: *z-model*.
   */
  model?: JSON;
  /**
   * The view element (or template) to use.
   */
  view?: Element;
  /**
   * The controller handler function or class.
   */
  controller?: ContextControllerHandler;
  /**
   * Additional methods/properties to add to the context controller instance.
   */
  controllerMembers: any;
  /**
   * A map of event handlers for standard and component's events. HTML attribute equivalent: *z-on* or *:on-event*.
   */
  on?: { [k: string]: EventCallback };
  /**
   * A map of event handlers for behaviors. HTML attribute equivalent: *z-behavior* or *:behavior-event*.
   */
  behavior?: { [k: string]: EventCallback };
  /**
   * Custom stylesheet to apply to the component's view, or a boolean to enable/disable CSS loading.
   */
  css?: Element | String | Boolean;
  /**
   * Whether to use style encapsulation (e.g., wrap CSS rules with a component-specific selector).
   */
  encapsulation?: Boolean;
  /**
   * Whether to reset view style to prevent inheriting from parent containers.
   */
  resetCss?: Boolean;
  /**
   * Extension to append when loading the HTML view file (default is *.html*).
   */
  cext?: String;
  /**
   * Can be set to `false` to disable HTML template loading, or to a string containing the inline HTML template code.
   */
  html?: Boolean;
  /**
   * Enables or disables lazy-loading. HTML attribute equivalent: *z-lazy*.
   */
  lazyLoad?: Boolean;
  /**
   * Loading priority (lower number loads first). HTML attribute equivalent: *z-priority*.
   */
  priority?: Number;
  /**
   * The loaded callback, triggered once the component is successfully loaded.
   */
  loaded?: ContextLoadedCallback;
  /**
   * The ready callback, triggered once all component's dependencies have been loaded.
   */
  ready?: ContextReadyCallback;
  /**
   * The error callback, triggered when an error occurs during loading.
   */
  error?: ContextErrorCallback;
}

/**
 * @description Callback function triggered if an error occurs when loading a component.
 */
declare interface ContextErrorCallback {
  /**
   * @param error The error object.
   * @param ctx The component context object (same as `this`).
   */
  (error: any, ctx: ComponentContext): void;
}

/**
 * @description Callback function triggered when a component has been successfully loaded.
 */
declare interface ContextLoadedCallback {
  /**
   * @param ctx The component context object (same as `this`).
   */
  (ctx: ComponentContext): void;
}

/**
 * @description Callback function triggered when a component is created, after all of its dependencies have been loaded and it's fully ready.
 */
declare interface ContextReadyCallback {
  /**
   * @param ctx The component context object (same as `this`).
   */
  (ctx: ComponentContext): void;
}

/**
 * @description The function called after the component is loaded to initialize its controller.
 */
declare interface ContextControllerHandler {
  /**
   * @param cp The component controller object.
   */
  (cp: ContextController): void;
}

/**
 * @description Callback function triggered when an event registered with the `on` method occurs.
 */
declare interface EventCallback {
  /**
   * @param event Event name.
   * @param data Event data.
   * @param $el ZxQuery wrapped element that sourced the event (same as `this`).
   */
  (event: String, data: any, $el: ZxQuery): void;
}

/**
 * @description The component context object represents the component instance itself.
 */
declare class ComponentContext {
  /**
   * Gets/Sets the container element of the component.
   * @param container The container element.
   * @returns The current container element or the ComponentContext itself for chaining.
   */
  container(container?: Element): ComponentContext | Element;
  /**
   * Gets/Sets the view element of the component.
   * @param view The HTML string or element of the view.
   * @returns The current view element or the ComponentContext itself for chaining.
   */
  view(view?: Element | String): ComponentContext | Element;
  /**
   * Gets/Sets the style of the component's view.
   * @param css The CSS string or style element.
   * @returns The current style element or the ComponentContext itself for chaining.
   */
  style(css?: String | Element): ComponentContext | Element;
  /**
   * Gets/Sets the data model of the component. The returned object is an observable-wrapped instance.
   * @param model The model object.
   * @returns The observable model object or the ComponentContext itself for chaining.
   */
  model(model?: any): ComponentContext | any;
  /**
   * Gets/Sets the component's controller handler.
   * @param controller The controller's handler function.
   * @returns The current controller handler or the ComponentContext itself for chaining.
   */
  controller(controller?: ContextControllerHandler): ComponentContext | ContextControllerHandler;
  /**
   * Gets/Sets the component's options.
   * @param options The JSON options object.
   * @returns The current options object or the ComponentContext itself for chaining.
   */
  options(options?: ContextOptions): ComponentContext | any;
  /**
   * Listens for a component event.
   * @param eventPath The event path or object with event name/handler pairs.
   * @param eventHandler The event handler function.
   * @returns The ComponentContext object itself for chaining.
   */
  on(eventPath: String, eventHandler: EventCallback): ComponentContext;
  /**
   * Loads the `.css` file and replace the view style of the component.
   * @param options The options object, typically containing `path`, `success`, `error`, and `then` callbacks.
   * @param enableCaching If true, enables caching (deprecated/internal).
   * @returns The ComponentContext object itself for chaining.
   */
  loadCss(options?: any, enableCaching?: Boolean): ComponentContext;
  /**
   * Loads the `.html` file and replace the view markup code of the component.
   * @param options The options object, typically containing `path`, `success`, `error`, and `then` callbacks.
   * @param enableCaching If true, enables caching (deprecated/internal).
   * @returns The ComponentContext object itself for chaining.
   */
  loadHtml(options?: any, enableCaching?: Boolean): ComponentContext;
  /**
   * Creates the data model out of all elements with the `#<field_name>` (or `z-field="name"`) attribute in the view.
   * @returns The ComponentContext object itself for chaining.
   */
  viewToModel(): ComponentContext;
  /**
   * Triggers the update of all elements in the view that are bound to the model's fields.
   * @returns The ComponentContext object itself for chaining.
   */
  modelToView(): ComponentContext;
  /**
   * Gets the CSS identifier used for style encapsulation of this component's view.
   * @returns The css-id attribute of this component.
   */
  getCssId(): String;
}

/**
 * @description Function called each time the data model is updated.
 */
declare interface ContextControllerUpdateCallback {
  /**
   * @param target The target object.
   * @param key The name of the property.
   * @param value The value of the property.
   * @param path The full property path (dotted notation).
   * @param old The target object before the update.
   */
  (target: any, key: String, value: any, path: String, old: any): void;
}

/**
 * @description Function that gets called after loading and before the component is created.
 */
declare interface ContextControllerInitCallback {
  (): void;
}

/**
 * @description Function that gets called after loading, when the component is actually created and ready.
 */
declare interface ContextControllerCreateCallback {
  (): void;
}

/**
 * @description Function called when the component is about to be disposed.
 */
declare interface ContextControllerDisposeCallback {
  (): void;
}

/**
 * @description The controller object associated with a component instance.
 */
declare class ContextController {
  /**
   * If set, this function gets called before component is created and before applying context options.
   */
  init: ContextControllerInitCallback;
  /**
   * If set, this function gets called after loading, when the component is created and its view (if provided) is loaded.
   */
  create: ContextControllerCreateCallback;
  /**
   * If set, this function gets called each time the data model is updated.
   */
  update: ContextControllerUpdateCallback;
  /**
   * If set, this function gets called when the component is about to be disposed.
   */
  dispose: ContextControllerDisposeCallback;
  /**
   * Gets elements having the `#<field_name>` (or `z-field="name"`) attribute matching the given value within the component view.
   * @param fieldName The name of the field attribute of the element(s) to get.
   * @returns A ZxQuery object wrapping the matching element(s).
   */
  field(fieldName: String): ZxQuery;
  /**
   * Gets the component view or, if a filter argument is passed, gets the view elements matching the given filter (shorthand for `cp.view().find(filter)`).
   * @param filter A valid DOM query selector string.
   * @returns The component view as a ZxQuery object.
   */
  view(filter?: String): ZxQuery;
  /**
   * Gets/Sets the data model of the component.
   * @param model The model object.
   * @returns The observable model object or the ContextController itself for chaining.
   */
  model(model?: any): ContextController | any;
  /**
   * Gets the component options.
   * @returns The component options object.
   */
  options(): any;
  /**
   * Triggers the component event `eventPath` with the given `eventData` object.
   * @param eventPath The event path.
   * @param eventData The event data.
   * @param isHook Trigger as global hook event.
   * @returns The ContextController object itself for chaining.
   */
  trigger(eventPath: String, eventData: any, isHook?: Boolean): ContextController;

  /**
   * Declare fields that are available as public members of the component context object.
   *
   * @param nameValuePairs A map of name/value pairs to expose.
   * @returns The ContextController object itself for chaining.
   */
  expose(nameValuePairs: { [key: string]: any | PropertyDescriptor }): ContextController;
  /**
   * Declare fields that are available as public members of the component context object.
   *
   * @param name Name of the exposed method/property.
   * @param handler Function, property value, or property descriptor.
   * @returns The ContextController object itself for chaining.
   */
  expose(name: string, handler?: any | Function | PropertyDescriptor): ContextController;

  /**
   * Declare fields that are available in the view's scripting scope.
   *
   * @param nameValuePairs A JSON object (map) of name/value pairs to declare.
   * @returns The ContextController object itself for chaining.
   */
  declare(nameValuePairs: { [key: string]: any | PropertyDescriptor }): ContextController;
  /**
   * Declare fields that are available in the view's scripting scope.
   *
   * @param name Name of the declared method/property.
   * @param handler Function, property value, or property descriptor (per getter/setter).
   * @returns The ContextController object itself for chaining.
   */
  declare(name: string, handler?: any | Function | PropertyDescriptor): ContextController;

  /**
   * Loads the `.css` file and replace the current view style of the component.
   * @param options The options object.
   * @returns The ContextController object itself for chaining.
   */
  loadCss(options?: any): ContextController;
  /**
   * Loads the `.html` file and replace the view markup of the component.
   * @param options The options object.
   * @returns The ContextController object itself for chaining.
   */
  loadHtml(options?: any): ContextController;
  /**
   * The component's built-in logger.
   */
  log: Logger;
  /**
   * Registers this one as the default controller for the given component type.
   * @param componentId Component identifier.
   * @returns The ContextController object itself for chaining.
   */
  for(componentId: String): ContextController;
}

/**
 * @description Base class for controllers defined using the ES6 class syntax (ControllerInstance extends ContextController).
 */
declare class ControllerInstance extends ContextController {
  /**
   * Called before component is created and before applying context options.
   */
  onInit(): void;
  /**
   * Called after loading, when the component is created and its view is loaded.
   */
  onCreate(): void;
  /**
   * Called each time the data model is updated.
   * @param target The target object.
   * @param key The name of the property.
   * @param value The value of the property.
   * @param path The full property path.
   * @param old The target object before the update.
   */
  onUpdate(target: any, key: String, value: any, path: String, old: any): void;
  /**
   * Called when the component is about to be disposed.
   */
  onDispose(): void;
  /**
   * @description Represents the currently bound HomeGenie module, providing module metadata, field accessors, and control functions.
   */
  boundModule: Module;
  /**
   * Subscribes a callback to the events of a ModuleField, receiving notifications on value changes.
   * @param field The ModuleField to subscribe to.
   * @param callback The function to call when the field's value changes.
   * @returns The subscription object for unsubscribing.
   */
  subscribe(field: ModuleField, callback: (f: ModuleField) => void): any;
  /**
   * Executes an API request to a HomeGenie service or program from the widget.
   * @param methodEndPoint The API endpoint path including the method (e.g., 'domain/address/method').
   * @param payload The data to send with the request (e.g., a natural language string or a JSON object).
   * @returns An observable Subject that emits the ApiResponse.
   */
  apiCall(methodEndPoint: String, payload: any): any;
  /**
   * Programmatically opens the widget's configuration and settings user interface.
   */
  showSettings(): void;
  /**
   * Retrieves the localized string for a specific key from the project's translation files.
   * @param key The translation key defined in the i18n assets.
   * @returns An observable Subject that emits the localized value.
   */
  translate(key: String): any;
}

/**
 * @description Component cache object used by the zUIx bundle system.
 */
declare class ComponentCache {
  /** The id of the cached component. */
  componentId: String;
  /** The view element. */
  view: Element;
  /** The CSS style text. */
  css: String;
  /** Whether the CSS style has been applied to the view or not (internal). */
  css_applied: Boolean;
  /** The controller handler function. */
  controller: ContextControllerHandler;
  /** The url/path if this is a resource loaded with `zuix.using(..)` method. */
  using: String;
}

/**
 * @description Bundle item object used for in-memory component caching.
 */
declare class BundleItem {
  /** The view element. */
  view: Element;
  /** The CSS style text. */
  css: String;
  /** The controller handler function. */
  controller: ContextControllerHandler;
}







/**
 * @description Global utility class for component-based development.
 */
declare class Zuix {
  /**
   * Searches the document or inside the given `container` for elements
   * having the `#<field_name>` (or `z-field="name"`) attribute matching the given value.
   * @param fieldName The name of the field attribute of the element(s) to get.
   * @param container Starting DOM element for this search (default: document).
   * @returns A ZxQuery object wrapping the matching element(s).
   */
  field(fieldName: String, container?: Element): ZxQuery;
  /**
   * Loads a component given its ID and optional configuration. This is the programmatic equivalent of the `z-load` attribute.
   * @param componentId The identifier name of the component to be loaded.
   * @param options Options used to initialize the loaded component.
   * @returns The component context object.
   */
  load(componentId: String, options?: ContextOptions): ComponentContext;
  /**
   * Unloads the given component context(s), releasing all allocated resources, including nested components.
   * @param context The instance of the component to be unloaded, a ZxQuery selection, or the component's host element.
   * @returns The Zuix object itself for chaining.
   */
  unload(context: ComponentContext | Element): Zuix;
  /**
   * Allocates a component's controller handler. The provided `handler` function will be called to initialize the component's controller instance.
   * @param handler Function called to initialize the component controller, or a string containing the controller's JavaScript code.
   * @param callback Optional controller options / callback, including a componentId for global registration.
   * @returns The allocated controller handler function.
   */
  controller(handler: ContextControllerHandler | string, callback?: {error?: Function, componentId?: string}): ContextControllerHandler;
  /**
   * Gets a ComponentContext object, given its `contextId` or its host element.
   * @param contextId The context ID, component's host element, or ZxQuery selection.
   * @param callback A callback function that will pass the component's context object once loaded and ready.
   * @returns The matching component's context or `null` if the context does not exist or is not yet loaded.
   */
  context(contextId: Element | ZxQuery | any, callback?: Function): ComponentContext;
  /**
   * Creates a new ComponentContext object with the specified options without immediately loading resources.
   * @param componentId The identifier name of the component.
   * @param options Options used to initialize the component context.
   * @returns The new component context object.
   */
  createComponent(componentId: String, options?: ContextOptions): ComponentContext;
  /**
   * Loads a component, given the target host element(s). If the target is already a component, it will be unloaded and replaced by the new one.
   * @param elements The target host element(s) or component context(s).
   * @param componentId The ID of the component to load (path/component_name).
   * @param type The component type ('view' or 'ctrl').
   * @param options The component options.
   */
  loadComponent(elements: Element | ZxQuery, componentId: string, type?: 'view' | 'ctrl', options?: ContextOptions);
  /**
   * Triggers the event specified by `eventPath`.
   * @param context The context object (this) passed to handler functions listening for this event.
   * @param eventPath The path of the event to fire.
   * @param eventData The data object of the event.
   * @returns The Zuix object itself for chaining.
   */
  trigger(context: any, eventPath: String, eventData?: any): Zuix;
  /**
   * Sets a callback for a global hook event. There can be only one callback per event path.
   * @param eventPath The event path (e.g., 'html:parse', 'load:end').
   * @param eventHandler The handler function. Pass null to unset a previous callback.
   * @returns The Zuix object itself for chaining.
   */
  hook(eventPath: String, eventHandler: Function): Zuix;
  /**
   * Loads a CSS, script or a singleton component. Resources loaded are available in the global scope.
   * @param resourceType Either 'style', 'script', or 'component'.
   * @param resourcePath Relative or absolute resource URL path.
   * @param callback Callback function to call once the resource is loaded.
   * @param context The target component context. Mandatory when loading resources for a component with ShadowDOM.
   */
  using(resourceType: String, resourcePath: String, callback?: Function, context?: ComponentContext): void;
  /**
   * Enables/Disables lazy-loading or gets the current setting.
   * @param enable Enable or disable lazy loading.
   * @param threshold Load-ahead threshold in pixels or as a ratio of the viewport size.
   * @returns The Zuix object itself for chaining, or a boolean if no argument is provided.
   */
  lazyLoad(enable?: Boolean, threshold?: Number): Zuix | Boolean;
  /**
   * Enables/Disables or gets the current HTTP resource caching setting.
   * @param enable Enable or disable HTTP caching.
   * @returns The Zuix object itself for chaining, or a boolean if no argument is provided.
   */
  httpCaching(enable?: Boolean): Zuix | Boolean;
  /**
   * Searches the document, or inside the given `element`, for elements with `z-load` attribute, and loads the requested components.
   * @param element Container to use as starting element for the search (default: document). Pass boolean to globally enable/disable the componentizer.
   * @returns The Zuix object itself for chaining.
   */
  componentize(element?: Element | ZxQuery): Zuix;
  /**
   * Gets/Sets a global store entry.
   * @param name Entry name.
   * @param value Entry value.
   * @returns The entry value.
   */
  store(name: String, value: any): any;
  /**
   * Gets the full, resolved path of a loadable resource.
   * @param path Loadable resource ID.
   * @returns The resource's full path.
   */
  getResourcePath(path: String): String;
  /**
   * Gets an observable instance of the given object, based on the browser's built-in Proxy object.
   * @param obj Object to observe.
   * @returns The observable object.
   */
  observable(obj: any): ObservableObject;
  /**
   * Gets/Sets the application's data bundle (all components and scripts used in the page packed into a single object).
   * @param bundleData A bundle object holding in memory all components' data (cache), or 'true' to compile the current state.
   * @param callback Called once the bundle compilation ends (only if `bundleData` is true).
   * @returns The Zuix object itself for chaining, or the array of bundle items if no argument is provided.
   */
  bundle(bundleData: BundleItem[], callback?: Function): Zuix | BundleItem[];
  /**
   * Helper function/static class for querying and manipulating the DOM.
   */
  $: ZxQueryStatic;
  /**
   * Dumps allocated component's contexts. Mainly for debugging purposes.
   * @returns An array of all active ComponentContext objects.
   */
  dumpContexts(): ComponentContext[];
  /**
   * Dumps content of the components cache. Mainly for debugging purposes.
   * @returns An array of all cached components.
   */
  dumpCache(): ComponentCache[];
  /**
   * Sets the global components cache.
   * @param cache An array of ComponentCache objects.
   */
  setComponentCache(cache: ComponentCache[]): void;
  /**
   * Runs a script in the scripting context of the given view element.
   *
   * @param scriptCode Scriptlet Js code.
   * @param $el Target ZxQuery-wrapped element.
   * @param $view Component's view (ZxQuery).
   * @param data Custom data.
   * @return The result of the script execution.
   */
  runScriptlet(scriptCode: string, $el: ZxQuery, $view: ZxQuery, data?: any): any;
}

/**
 * @description Callback function used with the `each(..)` method for iterating over elements.
 */
declare interface ElementsIterationCallback {
  /**
   * @param count Iteration count.
   * @param item Current element.
   * @param $item ZxQuery wrapped element (same as 'this').
   */
  (count: Number, item: Element, $item: ZxQuery): void;
}

/**
 * @description Relative position object.
 */
declare class Position {
  /** X-axis coordinate delta. */
  dx: Number;
  /** Y-axis coordinate delta. */
  dy: Number;
}

/**
 * @description The ElementPosition object returned by the `position()` method.
 */
declare class ElementPosition {
  /** X coordinate of the element in the viewport. */
  x: Number;
  /** Y coordinate of the element in the viewport. */
  y: Number;
  /** Position of the element relative to the viewport. */
  frame: Position;
  /** Current state change event description ('enter', 'exit', 'scroll', 'off-scroll'). */
  event: String;
  /** Boolean value indicating whether the element is visible in the viewport. */
  visible: Boolean;
}

/**
 * @description The `IterationCallback` function used with the static `zuix.$.each(..)` method.
 */
declare interface IterationCallback {
  /**
   * @param i Iteration count / item key.
   * @param item Current element (same as `this`).
   */
  (i: Number, item: any): void;
}

/**
 * @description The `ZxQueryHttpBeforeSendCallback` function.
 */
declare interface ZxQueryHttpBeforeSendCallback {
  /**
   * @param xhr The XMLHttpRequest object before sending the request.
   */
  (xhr: XMLHttpRequest): void;
}

/**
 * @description The `ZxQueryHttpSuccessCallback` function.
 */
declare interface ZxQueryHttpSuccessCallback {
  /**
   * @param responseText The text response from the server.
   */
  (responseText: String): void;
}

/**
 * @description The `ZxQueryHttpErrorCallback` function.
 */
declare interface ZxQueryHttpErrorCallback {
  /**
   * @param xhr The XMLHttpRequest object.
   * @param statusText The HTTP status text.
   * @param statusCode The HTTP status code.
   */
  (xhr: XMLHttpRequest, statusText: String, statusCode: Number): void;
}

/**
 * @description The `ZxQueryHttpThenCallback` function.
 */
declare interface ZxQueryHttpThenCallback {
  /**
   * @param xhr The XMLHttpRequest object after the request has completed.
   */
  (xhr: XMLHttpRequest): void;
}

/**
 * @description zuix.$.http options object.
 */
declare class ZxQueryHttpOptions {
  /** The URL for the request. */
  url: String;
  /** Optional callback function to call before sending the request. */
  beforeSend?: ZxQueryHttpBeforeSendCallback;
  /** Optional callback function to call on successful response. */
  success?: ZxQueryHttpSuccessCallback;
  /** Optional callback function to call on request error. */
  error?: ZxQueryHttpErrorCallback;
  /** Optional callback function to call after the request has completed (success or error). */
  then?: ZxQueryHttpThenCallback;
}

/**
 * @description ZxQuery class for manipulating a set of DOM elements.
 */
declare class ZxQuery {
  /**
   * Gets the number of elements in the ZxQuery object.
   * @returns Number of DOM elements.
   */
  length(): Number;
  /**
   * Gets the closest parent matching the given selector filter. This only applies to the first element in the ZxQuery object.
   * @param filter A valid DOM query selector filter (default: first parent).
   * @returns A new ZxQuery object containing the matching parent element.
   */
  parent(filter?: String): ZxQuery;
  /**
   * Gets the children matching the given selector filter. This only applies to the first element in the ZxQuery object.
   * @param filter A valid DOM query selector filter (default: all children).
   * @returns A new ZxQuery object containing the selected children.
   */
  children(filter?: String): ZxQuery;
  /**
   * Reverses order of the elements in the current set.
   * @returns The ZxQuery object itself for chaining.
   */
  reverse(): ZxQuery;
  /**
   * Gets the DOM Element located at the given position in the ZxQuery object.
   * @param i Position of element (default: 0).
   * @returns The DOM element.
   */
  get(i?: Number): Node | Element;
  /**
   * Gets a new ZxQuery object containing the element located at the given position in the current ZxQuery object.
   * @param i Position of element.
   * @returns A new ZxQuery object containing the selected element.
   */
  eq(i: Number): ZxQuery;
  /**
   * Selects all descendants matching the given DOM query selector filter. This only applies to the first element.
   * @param selector A valid DOM query selector.
   * @returns A new ZxQuery object containing the selected elements.
   */
  find(selector: String): ZxQuery;
  /**
   * Iterates through all DOM elements in the selection.
   * @param iterationCallback The callback function to call for each element.
   * @returns The ZxQuery object itself for chaining.
   */
  each(iterationCallback: ElementsIterationCallback): ZxQuery;
  /**
   * Gets the value of an attribute for the first element, or sets one or more attributes for all elements.
   * @param attr The attribute name or JSON object with attribute/value pairs.
   * @param val The attribute value.
   * @returns The attribute value when no `val` is specified, otherwise the ZxQuery object itself.
   */
  attr(attr: String | JSON, val?: String): String | ZxQuery;
  /**
   * Triggers the given event for all elements in the ZxQuery object.
   * @param eventPath Path of the event to trigger.
   * @param eventData Value of the event.
   * @returns The ZxQuery object itself for chaining.
   */
  trigger(eventPath: String, eventData: any): ZxQuery;
  /**
   * Listens once to the given event for all elements in the ZxQuery object.
   * @param eventPath Event path or object with event/handler pairs.
   * @param eventHandler Event handler.
   * @returns The ZxQuery object itself for chaining.
   */
  one(eventPath: String, eventHandler: Function): ZxQuery;
  /**
   * Listens to the given event for all elements in the ZxQuery object.
   * @param eventPath Event path or object with event/handler pairs.
   * @param eventHandler Event handler.
   * @returns The ZxQuery object itself for chaining.
   */
  on(eventPath: String, eventHandler: Function): ZxQuery;
  /**
   * Stops listening for the given event.
   * @param eventPath Event path or object with event/handler pairs.
   * @param eventHandler Event handler.
   * @returns The ZxQuery object itself for chaining.
   */
  off(eventPath: String, eventHandler: Function): ZxQuery;
  /**
   * De-registers all event handlers of all elements in the ZxQuery object.
   * @returns The ZxQuery object itself for chaining.
   */
  reset(): ZxQuery;
  /**
   * Returns true if the first element markup code is empty.
   * @returns True if the element is empty, false otherwise.
   */
  isEmpty(): Boolean;
  /**
   * Gets coordinates and visibility status of the first element.
   * @returns The ElementPosition object.
   */
  position(): ElementPosition;
  /**
   * Gets the value of a CSS property for the first element, or sets one or more CSS properties for all elements.
   * @param prop The CSS property name or JSON list of property/value pairs.
   * @param val The CSS property value.
   * @returns The CSS property value when no `val` specified, otherwise the ZxQuery object itself.
   */
  css(prop: String | JSON, val?: String): String | ZxQuery;
  /**
   * Adds the given CSS class to the class list of all elements.
   * @param className The CSS class name.
   * @returns The ZxQuery object itself for chaining.
   */
  addClass(className: String): ZxQuery;
  /**
   * Returns true if the first element contains the given CSS class.
   * @param className The CSS class name.
   * @returns True if the element contains the given CSS class, false otherwise.
   */
  hasClass(className: String): Boolean;
  /**
   * Removes the given CSS class from all elements.
   * @param className The CSS class name.
   * @returns The ZxQuery object itself for chaining.
   */
  removeClass(className: String): ZxQuery;
  /**
   * Moves to the previous sibling in the DOM.
   * @returns A new ZxQuery object containing the previous sibling element.
   */
  prev(): ZxQuery;
  /**
   * Moves to the next sibling in the DOM.
   * @returns A new ZxQuery object containing the next sibling element.
   */
  next(): ZxQuery;
  /**
   * Gets the HTML string of the first element, or sets the HTML string for all elements.
   * @param htmlText HTML text.
   * @returns The ZxQuery object itself or the HTML string.
   */
  html(htmlText?: String): ZxQuery | String;
  /**
   * Gets the `checked` attribute of the first element, or sets the `checked` attribute value for all elements.
   * @param check Value to assign to the 'checked' attribute.
   * @returns The ZxQuery object itself or the boolean checked state.
   */
  checked(check?: Boolean): ZxQuery | Boolean;
  /**
   * Gets the `value` attribute of the first element, or sets the `value` attribute value for all elements.
   * @param value Value to assign to the 'value' attribute.
   * @returns The ZxQuery object itself or the value string.
   */
  value(value?: String): ZxQuery | String;
  /**
   * Appends the given element or HTML string to the first element in the ZxQuery object.
   * @param el Element or HTML to append.
   * @returns The ZxQuery object itself for chaining.
   */
  append(el: ZxQuery | Node[] | Node | NodeList | String | any): ZxQuery;
  /**
   * Inserts the given child element before the one located at the specified index to the first element.
   * @param index Position where to insert `el` Element.
   * @param el Element to insert.
   * @returns The ZxQuery object itself for chaining.
   */
  insert(index: Number, el: ZxQuery | Node[] | Node | NodeList | any): ZxQuery;
  /**
   * Prepends the given element or HTML string to the first element in the ZxQuery object.
   * @param el Element to prepend.
   * @returns The ZxQuery object itself for chaining.
   */
  prepend(el: ZxQuery | Node[] | Node | NodeList | String | any): ZxQuery;
  /**
   * Detaches from its parent the first element in the ZxQuery object.
   * @returns The ZxQuery object itself for chaining.
   */
  detach(): ZxQuery;
  /**
   * Re-attaches to its parent the first element in the ZxQuery object (if previously detached).
   * @returns The ZxQuery object itself for chaining.
   */
  attach(): ZxQuery;
  /**
   * Gets the CSS `display` property of the first element, or sets the `display` property value for all elements.
   * @param mode The display value.
   * @returns The display value when no `mode` specified, otherwise the ZxQuery object itself.
   */
  display(mode?: String): String | ZxQuery;
  /**
   * Gets the CSS `visibility` property of the first element, or sets the `visibility` property value for all elements.
   * @param mode The visibility value.
   * @returns The visibility value when no `mode` specified, otherwise the ZxQuery object itself.
   */
  visibility(mode?: String): String | ZxQuery;
  /**
   * Sets the CSS `display` property to '' if no argument value is provided, otherwise set it to the given value.
   * @param mode Set the display mode to be used to show element (e.g. 'block', 'inline', etc..).
   * @returns The ZxQuery object itself for chaining.
   */
  show(mode?: String): ZxQuery;
  /**
   * Sets the CSS `display` property to 'none'.
   * @returns The ZxQuery object itself for chaining.
   */
  hide(): ZxQuery;
  /**
   * Plays the transition effect specified by the given transition class list.
   * @param options A list of classes (string array), a string with whitespace-separated class names, or a PlayFxConfig object.
   */
  playAnimation(options: string[] | string | PlayFxConfig);
  /**
   * Plays the animation effect specified by the given animation class list.
   * @param options A list of classes (string array), a string with whitespace-separated class names, or a PlayFxConfig object.
   */
  playTransition(options: string[] | string | PlayFxConfig);
}

/**
 * @description Static methods for creating ZxQuery objects and general DOM/utility operations.
 */
declare class ZxQueryStatic {
  /**
   * The constructor takes one optional argument that can be a DOM element, a node list or a valid DOM query selector string.
   * @param what Query target (Element, NodeList, selector string, etc.).
   */
  constructor(what?: ZxQuery | Node[] | Node | NodeList | String | any);
  /**
   * Selects document elements matching the given DOM query selector.
   * @param selector A valid DOM query selector.
   * @returns A new ZxQuery object containing the selected elements.
   */
  find(selector: String): ZxQuery;
  /**
   * Iterates through all objects in the given `items` collection.
   * @param items Enumerable objects collection (Array or JSON/Object).
   * @param iterationCallback The callback function to call at each iteration.
   * @returns The ZxQuery object itself for chaining.
   */
  each(items: JSON | any[], iterationCallback: IterationCallback): ZxQuery;
  /**
   * Performs an HTTP request with the given options.
   * @param options The ZxQueryHttpOptions object.
   * @returns The ZxQueryStatic object itself for chaining.
   */
  http(options: ZxQueryHttpOptions): ZxQueryStatic;
  /**
   * Checks if an element has got the specified CSS class.
   * @param el The element to check.
   * @param className The CSS class name.
   * @returns True if the element has the class, false otherwise.
   */
  hasClass(el: Element, className: String): Boolean;
  /**
   * Checks if a class exists by searching for it in all document stylesheets.
   * @param className The CSS class name.
   * @returns True if the class exists, false otherwise.
   */
  classExists(className: String): Boolean;
  /**
   * Wraps an Element inside a container specified by a given tag name.
   * @param containerTag Container element tag name.
   * @param element The element to wrap.
   * @returns The new wrapped element.
   */
  wrapElement(containerTag: String, element: Element): Element;
  /**
   * Appends a new stylesheet, or replaces an existing one, to the document.
   * @param css Stylesheet text.
   * @param target Existing style element to replace.
   * @param cssId ID to assign to the stylesheet.
   * @returns The new style element created out of the given css text.
   */
  appendCss(css: String, target: Element, cssId: String): Element;
  /**
   * Parses variables enclosed in single or double braces and calls the given callback for each parsed variable name.
   * @param html The source HTML template.
   * @param callback A callback function with one argument (the currently parsed variable name).
   * @returns The new HTML code with variables replaced with values or null if no variable was replaced.
   */
  replaceBraces(html: String, callback: Function): String;
  /**
   * Gets the closest parent matching the given query selector.
   * @param elem The starting element.
   * @param selector A valid DOM query selector string expression.
   * @returns The closest matching parent element or null.
   */
  getClosest(elem: Element, selector: String): Element;
  /**
   * Gets the position of an element.
   * @param el The element.
   * @param tolerance Distance in pixels from viewport's boundaries for the element to be considered 'visible'.
   * @returns The ElementPosition object.
   */
  getPosition(el: Element, tolerance?: Number): ElementPosition;
}

/**
 * @description Configuration object for `playFx`, `playTransition`, `playAnimation` utility methods.
 */
declare class PlayFxConfig {
  /** The type of effect to play ('transition' or 'animation'). */
  type: 'transition' | 'animation';
  /** Target element. */
  target: Element | ZxQuery;
  /** List of transition or animation classes to play. */
  classes: string[] | string;
  /** Transition/animation options ('delay', 'duration', etc.). */
  options?: any;
  /** Hold last transition/animation class. */
  holdState?: boolean;
  /** Called after each pair of transition/animation ended. */
  onStep?: PlayFxCallback;
  /** Called when all transitions/animations ended. */
  onEnd?: PlayFxCallback;
}

/**
 * @description Callback function used with the `onStep` and `onEnd` properties of PlayFxConfig.
 */
declare interface PlayFxCallback {
  /**
   * @param $element Target element (same as 'this').
   * @param classQueue Transition/animation class queue left to play, null if the animation ended.
   */
  ($element: ZxQuery, classQueue: string[]): void
}


/**
 * @description Provides functionality to create an observable proxy from a standard JavaScript object.
 */
declare class ObjectObserver {
  /**
   * Creates an ObservableObject wrapper around a standard JavaScript object.
   * This allows the object's properties to be monitored for changes.
   * @param obj The standard JavaScript object to make observable.
   * @returns An ObservableObject proxy.
   */
  observable(obj: any): ObservableObject;
}

/**
 * @description A proxy object that monitors a target object for property changes.
 */
declare class ObservableObject {
  /**
   * Subscribes a listener to receive notifications when the object's properties are accessed or modified.
   * @param observableListener The listener object with 'get' and 'set' methods.
   */
  subscribe(observableListener: ObservableListener): void;
  /**
   * Unsubscribes a listener, stopping notifications of property access or modification.
   * @param observableListener The listener object to unsubscribe.
   */
  unsubscribe(observableListener: ObservableListener): void;
}

/**
 * @description Defines the interface for a listener that receives notifications of property access and modification on an ObservableObject.
 */
declare class ObservableListener {
  /**
   * Called when a property of the ObservableObject is accessed.
   * @param target The original object being observed.
   * @param key The name of the property being accessed.
   * @param value The value of the property being accessed.
   * @param path The full property path (e.g., 'user.address.city').
   */
  get(target: any, key: String, value: any, path: String): void;
  /**
   * Called when a property of the ObservableObject is set (modified).
   * @param target The original object being observed.
   * @param key The name of the property being set.
   * @param value The new value being assigned to the property.
   * @param path The full property path (e.g., 'user.address.city').
   * @param old The old value of the property.
   */
  set(target: any, key: String, value: any, path: String, old: any): void;
}

/**
 * @description Interface for a callback function used to monitor log events globally.
 */
declare interface LoggerMonitorCallback {
  /**
   * @param ctx Context object associated with the log entry.
   * @param level The log level (e.g., 'info', 'warn', 'error').
   * @param args The arguments passed to the logging function.
   */
  (ctx: any, level: String, ...args: any[]): void;
}

/**
 * @description Provides a logging utility with different severity levels and monitoring capabilities.
 */
declare class Logger {
  /**
   * Registers a global callback function to intercept and monitor all log events.
   * @param callback The function to be called for every log message.
   */
  monitor(callback: LoggerMonitorCallback): void;
  /**
   * Enables or disables logging output to the browser console.
   * @param enable If true, console output is enabled; otherwise, it is disabled.
   */
  console(enable: Boolean): void;
  /**
   * Logs a message with the 'info' level.
   * @param args The data or message parts to log.
   * @returns The Logger instance for method chaining.
   */
  info(...args: any[]): Logger;
  /**
   * Logs a message with the 'warn' level.
   * @param args The data or message parts to log.
   * @returns The Logger instance for method chaining.
   */
  warn(...args: any[]): Logger;
  /**
   * Logs a message with the 'error' level.
   * @param args The data or message parts to log.
   * @returns The Logger instance for method chaining.
   */
  error(...args: any[]): Logger;
  /**
   * Logs a message with the 'debug' level.
   * @param args The data or message parts to log.
   * @returns The Logger instance for method chaining.
   */
  debug(...args: any[]): Logger;
  /**
   * Logs a message with the 'trace' level.
   * @param args The data or message parts to log.
   * @returns The Logger instance for method chaining.
   */
  trace(...args: any[]): Logger;
}

/**
 * @description zuix.js library for component based web development
 * @link https://zuixjs.org HomePage
 * @link https://github.com/zuixjs Repository
 */
// @ts-ignore
declare const zuix: Zuix;

/**
 * @description Global utility class. Contains functions for formatting, filtering,
 *              accessing user preferences, and notification/conversion capabilities.
 */
declare const utils: {
  /**
   * Namespace for utility functions related to formatting module field values and names.
   */
  format: {
    /**
     * Formats the value of a module field into a readable string.
     * @param field The ModuleField object to format.
     * @returns The formatted value as a string.
     */
    fieldValue(field: ModuleField): string;
    /**
     * Formats the key (name) of a module field for display purposes.
     * @param field The ModuleField object whose name is to be formatted.
     * @returns The formatted field name as a string.
     */
    fieldName(field: ModuleField): string;
  },
  /**
   * Namespace for utility functions related to content filtering.
   */
  filter: {
    /**
     * Removes HTML tags from a string.
     * @param s The string from which to remove HTML.
     * @returns The string with HTML tags stripped out.
     */
    stripHTML(s: string): string;
  },
  /**
   * Namespace for accessing and manipulating user preference settings.
   */
  preferences: {
    /**
     * Settings related to the user interface appearance and behavior.
     */
    ui: {
      /**
       * The current user interface theme ('light' or 'dark').
       */
      theme: 'light' | 'dark',
      drawer: {
        /**
         * The open state of the side panel (drawer).
         */
        open: boolean
      },
      /**
       * The current user interface language (ISO code, e.g., 'it', 'en').
       */
      language: string
    },
    /**
     * Default unit settings for various measurement categories.
     */
    units: {
      /**
       * The default unit of measure for general use.
       */
      current: string,
      /**
       * The default unit of measure for digital data (e.g., 'bits', 'bytes').
       */
      digital: string,
      /**
       * The default unit of measure for energy (e.g., 'J', 'kWh').
       */
      energy: string,
      /**
       * The default unit of measure for illuminance (e.g., 'lx').
       */
      illuminance: string,
      /**
       * The default unit of measure for power (e.g., 'W', 'kW').
       */
      power: string,
      /**
       * The default unit of measure for pressure (e.g., 'Pa', 'bar').
       */
      pressure: string,
      /**
       * The default unit of measure for speed (es. 'm/s', 'km/h').
       */
      speed: string,
      /**
       * The default unit of measure for temperature (es. '°C', '°F').
       */
      temperature: string,
      /**
       * The default unit of measure for voltage (es. 'V', 'mV').
       */
      voltage: string
    }
  },
  /**
   * Namespace for functions that interact with the user interface, such as notifications and tooltips.
   */
  ui: {
    /**
     * Displays a notification message
     *
     * @param title Title of the notification popup
     * @param message Message to display
     * @param options E.g. `{verticalPosition: 'bottom', horizontalPosition: 'left', duration: 5000}`
     */
    notify(title: string, message: string, options?: any): void;
    /**
     * Displays a tooltip message
     *
     * @param message Message to display
     * @param options E.g. `{hideDelay: 2000, showDelay: 100}`
     */
    tooltip(message: string, options?: any): void;
  },
  /**
   * Namespace for functions to convert various units of measure.
   * @link https://github.com/nosferatoy/units-converter#readme Units-Converter docs
   * @example `utils.convert.voltage(5).from('V').to('mV').value`
   */
  convert: {
    acceleration: ConversionMethod;
    angle: ConversionMethod;
    apparentPower: ConversionMethod;
    area: ConversionMethod;
    charge: ConversionMethod;
    current: ConversionMethod;
    digital: ConversionMethod;
    /**
     * A utility conversion method that performs no conversion (useful for chaining).
     */
    each: ConversionMethod;
    energy: ConversionMethod;
    force: ConversionMethod;
    frequency: ConversionMethod;
    illuminance: ConversionMethod;
    length: ConversionMethod;
    mass: ConversionMethod;
    pace: ConversionMethod;
    partsPer: ConversionMethod;
    power: ConversionMethod;
    pressure: ConversionMethod;
    reactiveEnergy: ConversionMethod;
    reactivePower: ConversionMethod;
    speed: ConversionMethod;
    temperature: ConversionMethod;
    time: ConversionMethod;
    voltage: ConversionMethod;
    volume: ConversionMethod;
    volumeFlowRate: ConversionMethod;
  };
};

declare class Module {
  /**
   * The ID of the adapter/interface that manages this module (e.g., 'ZWave.ZWaveInterface').
   */
  adapterId: string;
  /**
   * The full unique identifier of the module (id ::= '<domain>:<address>').
   */
  id: string; // id ::= '<domain>:<address>';
  /**
   * The device type of the module (e.g., 'Light', 'Sensor', 'Program').
   */
  type: string;
  /**
   * The user-friendly name of the module.
   */
  name: string;
  /**
   * A detailed description of the module.
   */
  description: string;
  /**
   * Retrieves a specific field (parameter) of the module.
   * @param key The field key identifier (e.g., 'Status.Level').
   * @returns The ModuleField object.
   */
  field: (key: string) => ModuleField;
  /**
   * A list of all fields (parameters) available for this module.
   */
  fields: ModuleField[];
  /**
   * A Subject that emits the ModuleField object whenever one of the module's fields changes.
   */
  events: Subject<ModuleField>;
  /**
   * A Subject that emits error events related to module operations.
   */
  error: Subject<any>;
  /**
   * Sends a command to the module's controller.
   * @param command The command string (e.g., 'Control.Level').
   * @param options Optional command options (e.g., '50', or an object).
   * @returns A Subject that may emit the command result or status.
   */
  control(command: any, options?: any): Subject<any>;
  /**
   * Gets the module data matching the given key or sets it if the 'data' parameter was passed.
   * This typically refers to persistent/auxiliary data associated with the module, not its live parameters.
   * @param key The field key identifier.
   * @param data The data to set (optional).
   */
  data(key: string, data?: any): any;
  /**
   * Retrieves all custom widget data associated with this module instance.
   * @returns A dynamic object containing widget-specific data.
   */
  getWidgetData(): any;
  /**
   * Retrieves the path to the icon associated with the module's current state or type.
   * @returns The icon URL/path as a string.
   */
  getIcon(): string;
  /**
   * Indicates whether the underlying device/interface for the module is currently online and responsive.
   * @returns `true` if the module is online; otherwise, `false`.
   */
  get isOnline(): boolean;
  /**
   * Retrieves field configurations relevant for statistical monitoring or trending.
   * @returns A dynamic object containing field metadata for stats tracking.
   */
  getStatsFields(): any;
}

declare class ModuleField {
  /**
   * The field name
   */
  key: string;
  /**
   * Update timestamp.
   */
  timestamp: number;
  /**
   * The current value of the field. The type can vary (string, number, boolean, etc.).
   */
  value: any;
  /**
   * Returns a Subject that emits the ModuleField object whenever its value changes.
   * @returns A subscribable Subject for ModuleField changes.
   */
  events: () => Subject<ModuleField>;
}

/**
 * A Subject is a special type of Observable that allows values to be
 * multicasted to many Observers. Subjects are like EventEmitters.
 *
 * Every Subject is an Observable and an Observer. You can subscribe to a
 * Subject, and you can call next to feed values as well as error and complete.
 */
declare class Subject<T> {
  /**
   * Indicates whether this Subject has been closed (it will emit no more values).
   */
  closed: boolean;
  constructor();
  /**
   * Sends the next value to all listening observers.
   * @param value The value to be emitted.
   */
  next(value: T): void;
  /**
   * Sends an error to all observers and closes the Subject.
   * @param err The error to be emitted.
   */
  error(err: any): void;
  /**
   * Notifies all observers that the Subject has completed the emission of values.
   */
  complete(): void;
  /**
   * Subscribes a function (observer) to receive values emitted by this Subject.
   * @param observer The callback function to be executed on each emission.
   */
  subscribe(observer: (p: T) => void): void;
  /**
   * Unsubscribes all observers and closes the Subject.
   */
  unsubscribe(): void;
  /**
   * Indicates whether there are any observers currently subscribed to this Subject.
   */
  get observed(): boolean;
}




// Convert

/**
 * Interface representing the chainable API returned by all conversion methods (e.g., utils.convert.voltage(5)).
 */
interface ConverterChain {
  /**
   * Specifies the unit to convert from.
   * @param unit The unit string (e.g., 'V', 'C', 'm/s').
   * @returns The chainable converter object.
   */
  from(unit: string): this;
  /**
   * Specifies the unit to convert to.
   * @param unit The target unit string (e.g., 'mV', 'kJ', 'km/h').
   * @returns The chainable converter object.
   */
  to(unit: string): this;
  /**
   * The final converted numeric value.
   */
  value: number;
}

/**
 * Type for a single conversion method, which takes a numeric value and returns a chainable API.
 */
type ConversionMethod = (value: number) => ConverterChain;

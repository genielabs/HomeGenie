const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const sourcePath = fs.existsSync(path.join(__dirname, '../../assets/build/all/data/widgets/isometric/isometric-room.js'))
  ? path.join(__dirname, '../../assets/build/all/data/widgets/isometric/isometric-room.js') : path.join(__dirname, 'isometric-room.js');
const source = fs.readFileSync(sourcePath, 'utf8');
const cards = ['Light', 'Climate', 'Power'].map(name => [
  name, fs.readFileSync(path.join(path.dirname(sourcePath), '../isometric', name.toLowerCase() + '.js'), 'utf8')
]);

function node() {
  return {attrs: {}, classes: new Set(), hidden: false, listeners: new Map(), style: {}, disabled: false,
    classList: {toggle() {}, remove() {}}, setAttribute(name, value) {this.attrs[name] = value;},
    toggleAttribute(name, value) {this.attrs[name] = value;},
    addEventListener(name, callback) {this.listeners.set(name, callback);}, removeEventListener(name) {this.listeners.delete(name);},
    dispatchEvent(event) {this.listeners.get(event.type)?.(event);},
    getBoundingClientRect: () => ({width: 800, height: 480}), remove() {this.removed = true;}};
}
function moduleValue(values = {}) {
  return {name: 'Demo device', isOnline: true, listeners: [], requests: [],
    field(key) {const value = values[key];return value == null ? undefined : {key, value,
      decimalValue: typeof value === 'string' ? parseFloat(value.replace(',', '.')) : value};},
    emit(key, value) {values[key] = value;for (const listener of this.listeners) listener({key, value});},
    control(command) {const request = {command, closed: false};this.requests.push(request);return {subscribe(handlers) {
      request.handlers = handlers;return {unsubscribe() {request.closed = true;}};
    }};}};
}
function harness(roles = {}, reduced = false) {
  const nodes = new Map(), timers = new Map(), frames = new Map(), viewEvents = new Map();let nextId = 0;
  const doc = node(), media = node();doc.hidden = false;media.matches = reduced;
  class ControllerInstance {
    constructor() {this.data = {};}
    model() {return this.data;}
    declare(handlers) {this.declared = handlers;}
    field(key) {if (!nodes.has(key)) nodes.set(key, node());return {get: () => nodes.get(key)};}
    getBoundModule(key) {return roles[key];}
    view() {return {
      on: (name, fn) => viewEvents.set(name, fn),
      get: () => ({closest: () => nodes.get('light_widget')})
    };}
    subscribe(module, listener) {module.listeners.push(listener);return {unsubscribe() {}};}
  }
  const context = vm.createContext({ControllerInstance, console, URL, Event, document: doc, navigator: {language: 'en'},
    window: {devicePixelRatio: 1}, utils: {preferences: {ui: {language: 'en'}}, format: {fieldValue: f => String(f.value)}},
    matchMedia: () => media, requestAnimationFrame: fn => {const id = ++nextId;frames.set(id, fn);return id;},
    cancelAnimationFrame: id => frames.delete(id), setTimeout: fn => {const id = ++nextId;timers.set(id, fn);return id;},
    clearTimeout: id => timers.delete(id)});
  vm.runInContext(source + '\nglobalThis.Widget = IsometricRoom;', context);
  const widget = new context.Widget();widget.loadScene = async () => {};widget.onInit();widget.onCreate();
  widget.field('light_widget');
  const controllers = {};
  for (const [name, code] of cards) {
    vm.runInContext(code + '\nglobalThis.Card = Isometric' + name + ';', context);
    const card = new context.Card();
    if (name === 'Power') card.loadHistory = () => {};
    card.onInit?.();
    card.onCreate();
    controllers[name.toLowerCase()] = card;
  }
  const dispose = widget.onDispose.bind(widget);
  widget.onDispose = () => {
    for (const card of Object.values(controllers)) card.onDispose();
    dispose();
  };
  return {widget, ...controllers, nodes, timers, frames, doc, media, viewEvents};
}
function rendering(h) {
  const w = h.widget;let renders = 0, released = 0;
  w.renderer = {domElement: node(), render() {renders++;}, dispose() {released++;}, forceContextLoss() {}};
  w.width = 800;w.scene = {};w.camera = {};w.sensorMarker = {position: {}, rotation: {}};w.windowMaterial = {};
  w.resize = () => w.renderOnce();
  return {renders: () => renders, released: () => released};
}

test('unbound widgets expose unavailable data and send no device commands', () => {
  const h = harness();assert.equal(h.climate.data.temperature, '—');assert.equal(h.power.data.power, '—');
  assert.equal(h.nodes.get('light_toggle').disabled, true);h.widget.toggleLight();assert.equal(h.timers.size, 0);h.widget.onDispose();
});
test('zero is a real reading and module events update the state immediately', () => {
  const light = moduleValue({'Status.Level': 0}), climate = moduleValue({'Sensor.Temperature': 0, 'Sensor.Humidity': 0});
  const h = harness({light, climate, energy: moduleValue({'Meter.Watts': 0})});
  assert.equal(h.climate.data.temperature, '0');assert.equal(h.climate.data.humidity, '0');assert.equal(h.power.data.power, '0');
  assert.equal(h.widget.isOn, false);light.emit('Status.Level', 1);assert.equal(h.widget.isOn, true);assert.equal(light.requests.length, 0);h.widget.onDispose();
});
test('offline or malformed values do not masquerade as zero', () => {
  const light = moduleValue({'Status.Level': 1});light.isOnline = false;
  const h = harness({light, climate: moduleValue({'Sensor.Temperature': '', 'Sensor.Humidity': 'NaN'})});
  assert.equal(h.widget.isOn, null);assert.equal(h.climate.data.temperature, '—');assert.equal(h.climate.data.humidity, '—');h.widget.onDispose();
});
test('a positive command receipt does not fabricate the device state', () => {
  const light = moduleValue({'Status.Level': 0}), h = harness({light});h.widget.toggleLight();
  assert.equal(light.requests[0].command, 'Control.On');light.requests[0].handlers.next({code: 200, response: {Status: 'Ok'}});
  assert.equal(h.light.pending, true);assert.equal(h.widget.isOn, false);h.widget.toggleLight();assert.equal(light.requests.length, 1);
  light.emit('Status.Level', 1);assert.equal(h.light.pending, false);assert.equal(h.widget.isOn, true);assert.equal(h.timers.size, 0);
  assert.equal(light.requests[0].closed, true);h.widget.onDispose();
});
test('failed commands leave the real state intact and allow a deliberate retry', () => {
  const light = moduleValue({'Status.Level': 1}), h = harness({light});h.widget.toggleLight();
  assert.equal(light.requests[0].command, 'Control.Off');light.requests[0].handlers.error(new Error('offline'));
  assert.equal(h.light.pending, false);assert.equal(h.widget.isOn, true);assert.equal(h.timers.size, 0);
  h.widget.toggleLight();assert.equal(light.requests.length, 2);h.widget.onDispose();
});
test('missing confirmation times out without retrying the command', () => {
  const light = moduleValue({'Status.Level': 0}), h = harness({light});h.widget.toggleLight();
  [...h.timers.values()][0]();assert.equal(h.light.pending, false);assert.match(h.light.data.command_feedback, /No confirmation/);
  assert.equal(light.requests.length, 1);h.widget.onDispose();
});
test('late responses from an earlier command cannot change a later pending request', () => {
  const light = moduleValue({'Status.Level': 0}), h = harness({light});h.widget.toggleLight();
  [...h.timers.values()][0]();h.widget.toggleLight();const text = h.light.data.command_feedback;
  light.requests[0].handlers.error(new Error('late'));assert.equal(h.light.pending, true);assert.equal(h.light.data.command_feedback, text);h.widget.onDispose();
});
test('visibility pause cancels the sole frame chain and resets elapsed time', () => {
  const h = harness(), gpu = rendering(h);h.widget.active = true;h.widget.sync();h.widget.sync();assert.equal(h.frames.size, 1);
  const [id, frame] = [...h.frames.entries()][0];h.frames.delete(id);frame(100);assert.equal(h.frames.size, 1);
  h.doc.hidden = true;h.doc.listeners.get('visibilitychange')();assert.equal(h.frames.size, 0);assert.equal(h.widget.lastTime, null);
  const count = gpu.renders();h.widget.sync();assert.equal(gpu.renders(), count);
  h.doc.hidden = false;h.doc.listeners.get('visibilitychange')();assert.equal(h.frames.size, 1);h.widget.onDispose();assert.equal(h.frames.size, 0);
});
test('viewport inactivity and offscreen intersection stop rendering', () => {
  const h = harness();rendering(h);h.viewEvents.get('refresh:active')();assert.equal(h.frames.size, 1);
  h.viewEvents.get('refresh:inactive')();assert.equal(h.frames.size, 0);
  h.widget.active = true;h.widget.intersecting = false;h.widget.sync();assert.equal(h.frames.size, 0);h.widget.onDispose();
});
test('reduced motion starts static and supports intentional playback', () => {
  const h = harness({}, true);rendering(h);h.widget.active = true;h.widget.sync();assert.equal(h.frames.size, 0);
  h.widget.toggleMotion();assert.equal(h.frames.size, 1);h.media.listeners.get('change')({matches: true});assert.equal(h.frames.size, 0);h.widget.onDispose();
});
test('disposal releases GPU objects, commands, observers and page listeners', () => {
  const light = moduleValue({'Status.Level': 0}), h = harness({light}), gpu = rendering(h);let cleanup = 0;
  h.widget.geometries.add({dispose() {cleanup++;}});h.widget.materials.add({dispose() {cleanup++;}});
  h.widget.resizeObserver = {disconnect() {cleanup++;}};h.widget.intersectionObserver = {disconnect() {cleanup++;}};
  h.widget.active = true;h.widget.sync();h.widget.toggleLight();h.widget.onDispose();
  assert.equal(h.timers.size, 0);assert.equal(h.frames.size, 0);assert.equal(h.doc.listeners.size, 0);assert.equal(h.media.listeners.size, 0);
  assert.equal(light.requests[0].closed, true);assert.equal(gpu.released(), 1);assert.equal(cleanup, 4);
  light.emit('Status.Level', 1);assert.equal(h.frames.size, 0);assert.equal(h.widget.renderer, null);
});

test('the declared motion button pauses visible scene changes and resumes one frame chain', () => {
  const h = harness(), gpu = rendering(h), w = h.widget;
  const leaf = {mesh: {rotation: {x: 0, z: 0}}, x: 0, z: 0, phase: 0};w.plantLeaves.push(leaf);
  const next = time => {const [id, callback] = [...h.frames.entries()][0];h.frames.delete(id);callback(time);};
  w.active = true;w.sync();next(0);next(50);next(100);
  const moving = {y: w.sensorMarker.position.y, rotation: leaf.mesh.rotation.x, phase: w.phase, frames: gpu.renders()};
  assert.ok(moving.phase > 0);assert.ok(moving.rotation > 0);
  w.declared.toggleMotion();assert.equal(h.frames.size, 0);assert.equal(w.data.motion_icon, 'play_arrow');
  w.draw(200);assert.equal(w.phase, moving.phase);assert.equal(w.sensorMarker.position.y, moving.y);assert.equal(leaf.mesh.rotation.x, moving.rotation);
  w.declared.toggleMotion();assert.equal(h.frames.size, 1);assert.equal(w.data.motion_icon, 'pause');
  next(300);next(350);assert.ok(w.phase > moving.phase);assert.notEqual(w.sensorMarker.position.y, moving.y);assert.ok(gpu.renders() > moving.frames);
  assert.equal(h.frames.size, 1);w.onDispose();
});

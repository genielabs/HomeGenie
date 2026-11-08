# Isometric room dashboard

`isometric/isometric-room` is a first interactive Three.js dashboard prototype. The room is built from procedural geometry; no external images, models, CDN or additional packages are needed.

## Try it

Select **Custom Widget** in dashboard settings and choose `isometric/isometric-room`. Use **Bound Modules** in the dashboard toolbar to select the room devices:

- **Light / Luce**: a Light, Dimmer or Color device exposing `Status.Level` and `Control.On` / `Control.Off`.
- **Climate / Clima**: a sensor exposing temperature and/or humidity.
- **Power / Potenza**: a device exposing `Meter.Watts`.

On Vulkan, the separate `three-room-demo` client preset contains **Stanza 3D**, preconfigured with simulated Light 2, Multi Sensor and Light 2 power. Select this preset from Preferences to try the prototype. Existing presets were not replaced. The preset is a local test fixture, not a bundled installation default. Without an explicit selection, the widget uses compatible defaults from the demo. The setup wizard installs the `homegenie-themes` package, which enables the program gear. Isometric Room (652) is selected by default when available. Its **Disable animations** option applies to all dashboards that use that program.

Click the standing lamp or the light button to switch the associated device. State changes come from module events, not from assuming the command succeeded. A missing confirmation produces a bounded warning without automatically resending the command. Missing readings remain unavailable instead of becoming zero.

The motion button pauses/resumes the floating/rotating marker above the sideboard, plant-leaf sway and gentle window-light variation. Motion amplitudes were increased after the initial version proved too faint to recognize. Reduced-motion preferences start it paused. Temperature and power use the client's formatting/unit preferences. Labels are available in English, Italian and French.

## Rendering and lifecycle

The outer container fills the dashboard host area, which already excludes the HomeGenie toolbar. The scene uses the remaining height rather than the viewport width. The outer edges are square; stacked/narrow layouts retain vertical scrolling.

Three.js is loaded from YoT's existing local `assets/js/three/three.module.js`. Deploy the matching frontend assets through the normal build workflow. For the first Vulkan trial, use the Angular frontend on port 4200; the currently installed older bundled UI on port 8080 does not yet serve this asset.

The scene uses a single animation loop, capped at 30 frames per second, pixel ratio at most 1.5 and approximately 1.5 million drawing-buffer pixels. It has no shadow maps, texture downloads or postprocessing. Rendering pauses when the widget is inactive, the page is hidden or the scene is offscreen. Geometry, materials, renderer, observers and listeners are released on disposal. An import/WebGL failure leaves the HTML controls available.

These limits reduce unnecessary work; they are not a measured guarantee of GPU consumption on every device.

## Verification

Run from the HomeGenie repository:

```sh
node --check assets/build/all/data/widgets/isometric/isometric-room.js
node --test tests/widgets/isometric-room.test.cjs
```

The 12 focused tests cover missing/zero values, reactive state, confirmed/failed/timed-out commands, stale responses, visibility and reduced-motion behavior, and resource cleanup. They use a host/renderer harness; they do not replace a real WebGL interaction test.

Initial Vulkan browser verification confirmed the room, named bindings, reactive changes following a simulated API command, reload persistence and narrow-screen layout. Pointer automation cannot click inside the widget's closed shadow root; the motion button was exercised through normal keyboard navigation. Maintainer acceptance remains to check the light button and lamp hit target, motion control, scrolling, hidden-tab resume and perceived performance on the intended devices.

References: [Three.js responsive rendering](https://threejs.org/manual/pages/responsive.html) and [resource cleanup](https://threejs.org/manual/pages/cleanup.html).

The **Isometric** collection also includes reusable Light, Climate and Power widgets. The room dashboard embeds those widgets and passes its module selections down to them. When you add a card by itself, select its module in the card settings.

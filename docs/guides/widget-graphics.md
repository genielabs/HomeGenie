# Local widget graphics

YoT serves Chart.js 4.5.1, Anime.js 4.5.0 and Three.js 0.186.1 below `assets/js/`. YoT generates the Chart.js ESM bundle with `npm run build:widget-assets`; the generated file is committed so direct `ng serve` also works. npm start/build/test regenerate it automatically. Angular asset rules copy the selected distributions and licenses; no CDN or startup loading is needed. Restart the development server after installing dependencies/changing angular.json. Deploy the frontend build and HG data assets through the usual workflow before using the examples.

## Examples

The following three HTML/CSS/controller triplets live in `assets/build/all/data/widgets/examples/` and appear alongside the other example widgets:

| Widget | Library | Interaction |
| --- | --- | --- |
| `examples/chart-pulse` | Chart.js | A pastel bar chart with a Compare button switching between two sample datasets. |
| `examples/anime-orbit` | Anime.js v4 | Five gently floating shapes with a Play/Pause control. |
| `examples/three-knot` | Three.js | A procedural multicoloured 3D knot with a Play/Pause control. |

All work without a bound module and without backend/device calls. They use free sizing, scoped elements, local styles, reduced-motion preferences, viewport/page pause and final disposal. The chart disables animation and resizes explicitly. The 3D scene caps pixel ratio at 2 and reports WebGL errors in its own status area. Labels/sample datasets are illustrative English text, not new application translation keys.

Open an example in the widget editor to preview it, or ask the Genie to use the example as reference for a `widget.mode="presentation"` response. Normal examples remain reusable widgets: presentation mode belongs to the AI response envelope, not to controller settings.

## Import contract

Inside an async method called from `onCreate()`:

```js
const { default: Chart } = await import(new URL('assets/js/chartjs/chart.module.js', document.baseURI).href);
const { animate } = await import(new URL('assets/js/animejs/anime.esm.min.js', document.baseURI).href);
const THREE = await import(new URL('assets/js/three/three.module.js', document.baseURI).href);
```

Load only the library needed. Chart.js is a self-contained ESM bundle with a default Chart export. This avoids UMD detection of the Monaco AMD loader in the widget editor. Anime uses v4, not the v3 `anime(...)` API. Three's relative core import must remain alongside its module; addons such as OrbitControls/GLTFLoader are not shipped. Use `document.baseURI` rather than the backend origin, a route-relative string, a hardcoded leading slash or a dynamic controller URL.

After awaiting imports, check a per-instance disposal flag before touching the view or creating resources. Module downloads can finish after the widget has closed. Handle rejected imports/rendering errors without changing the host dialog layout.

Stop owned work on `refresh:inactive` and page hiding, resume only while both visible and active, and never restart a disposed widget. Disconnect observers and document listeners; destroy charts, revert animations, and dispose geometries/materials/textures/renderers as appropriate. Do not stop a shared library engine or change global defaults to manage one widget.

The shared AI widget reference and the legacy/unified widget prompts document these paths and practical widget conventions together. Local library additions do not change backend or device-control contracts.

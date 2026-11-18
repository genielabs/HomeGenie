import {
  FullScreenQuad,
  Pass
} from "./chunk-JTJIKJED.js";

// node_modules/three/examples/jsm/postprocessing/OutputPass.js
import {
  ColorManagement,
  RawShaderMaterial,
  UniformsUtils,
  LinearToneMapping,
  ReinhardToneMapping,
  CineonToneMapping,
  AgXToneMapping,
  ACESFilmicToneMapping,
  NeutralToneMapping,
  CustomToneMapping,
  SRGBTransfer
} from "../../three.module.js";

// node_modules/three/examples/jsm/shaders/OutputShader.js
var OutputShader = {
  name: "OutputShader",
  uniforms: {
    "tDiffuse": { value: null },
    "toneMappingExposure": { value: 1 }
  },
  vertexShader: `
		precision highp float;

		uniform mat4 modelViewMatrix;
		uniform mat4 projectionMatrix;

		attribute vec3 position;
		attribute vec2 uv;

		varying vec2 vUv;

		void main() {

			vUv = uv;
			gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );

		}`,
  fragmentShader: `

		precision highp float;

		uniform sampler2D tDiffuse;

		#include <tonemapping_pars_fragment>
		#include <colorspace_pars_fragment>

		varying vec2 vUv;

		void main() {

			gl_FragColor = texture2D( tDiffuse, vUv );

			// tone mapping

			#ifdef LINEAR_TONE_MAPPING

				gl_FragColor.rgb = LinearToneMapping( gl_FragColor.rgb );

			#elif defined( REINHARD_TONE_MAPPING )

				gl_FragColor.rgb = ReinhardToneMapping( gl_FragColor.rgb );

			#elif defined( CINEON_TONE_MAPPING )

				gl_FragColor.rgb = CineonToneMapping( gl_FragColor.rgb );

			#elif defined( ACES_FILMIC_TONE_MAPPING )

				gl_FragColor.rgb = ACESFilmicToneMapping( gl_FragColor.rgb );

			#elif defined( AGX_TONE_MAPPING )

				gl_FragColor.rgb = AgXToneMapping( gl_FragColor.rgb );

			#elif defined( NEUTRAL_TONE_MAPPING )

				gl_FragColor.rgb = NeutralToneMapping( gl_FragColor.rgb );

			#elif defined( CUSTOM_TONE_MAPPING )

				gl_FragColor.rgb = CustomToneMapping( gl_FragColor.rgb );

			#endif

			// color space

			#ifdef SRGB_TRANSFER

				gl_FragColor = sRGBTransferOETF( gl_FragColor );

			#endif

		}`
};

// node_modules/three/examples/jsm/postprocessing/OutputPass.js
var OutputPass = class extends Pass {
  constructor() {
    super();
    this.isOutputPass = true;
    this.uniforms = UniformsUtils.clone(OutputShader.uniforms);
    this.material = new RawShaderMaterial({
      name: OutputShader.name,
      uniforms: this.uniforms,
      vertexShader: OutputShader.vertexShader,
      fragmentShader: OutputShader.fragmentShader
    });
    this._fsQuad = new FullScreenQuad(this.material);
    this._outputColorSpace = null;
    this._toneMapping = null;
  }
  render(renderer, writeBuffer, readBuffer) {
    this.uniforms["tDiffuse"].value = readBuffer.texture;
    this.uniforms["toneMappingExposure"].value = renderer.toneMappingExposure;
    if (this._outputColorSpace !== renderer.outputColorSpace || this._toneMapping !== renderer.toneMapping) {
      this._outputColorSpace = renderer.outputColorSpace;
      this._toneMapping = renderer.toneMapping;
      this.material.defines = {};
      if (ColorManagement.getTransfer(this._outputColorSpace) === SRGBTransfer)
        this.material.defines.SRGB_TRANSFER = "";
      if (this._toneMapping === LinearToneMapping)
        this.material.defines.LINEAR_TONE_MAPPING = "";
      else if (this._toneMapping === ReinhardToneMapping)
        this.material.defines.REINHARD_TONE_MAPPING = "";
      else if (this._toneMapping === CineonToneMapping)
        this.material.defines.CINEON_TONE_MAPPING = "";
      else if (this._toneMapping === ACESFilmicToneMapping)
        this.material.defines.ACES_FILMIC_TONE_MAPPING = "";
      else if (this._toneMapping === AgXToneMapping)
        this.material.defines.AGX_TONE_MAPPING = "";
      else if (this._toneMapping === NeutralToneMapping)
        this.material.defines.NEUTRAL_TONE_MAPPING = "";
      else if (this._toneMapping === CustomToneMapping)
        this.material.defines.CUSTOM_TONE_MAPPING = "";
      this.material.needsUpdate = true;
    }
    if (this.renderToScreen === true) {
      renderer.setRenderTarget(null);
      this._fsQuad.render(renderer);
    } else {
      renderer.setRenderTarget(writeBuffer);
      if (this.clear)
        renderer.clear(renderer.autoClearColor, renderer.autoClearDepth, renderer.autoClearStencil);
      this._fsQuad.render(renderer);
    }
  }
  dispose() {
    this.material.dispose();
    this._fsQuad.dispose();
  }
};
export {
  OutputPass
};

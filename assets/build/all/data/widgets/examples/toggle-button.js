/* globals zuix */
'use strict';

/**
 * ToggleButton example class.
 *
 * @constructor
 * @this {ContextController}
 */
function ToggleButton() {
  this.create = onCreate;

  /** @type Module */
  let module = null;

  function onCreate() {
    // the bound module
    module = this.options().module;
    this.model().title = module ? module.name : '';
    const statusLevel = module ? module.field('Status.Level') : null;
    this.expose({
      level: () => statusLevel ? +statusLevel.value : 0,
      toggle: () => module ? module.control('Control.Toggle') : null,
      module
    });
  }
}

module.exports = ToggleButton;


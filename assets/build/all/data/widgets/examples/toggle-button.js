class ToggleButton extends ControllerInstance {


  onCreate() {

    // the bound module selected by the user
    const bm = this.boundModule;

    // sets the content of the #title field
    this.model().title = bm ? bm.name : '';

    // store a reference to the module level (on/off status)
    const statusLevel = bm ? bm.field('Status.Level') : null;

    // declare fields visible in the view template scripting scope
    this.declare({
      // toggles the bound module on/off
      toggle: () => bm?.control('Control.Toggle'),
      // adds button "pressed" class if level is > 0
      buttonState: ($el) => (statusLevel?.value > 0)
          ? $el.addClass('pressed')
          : $el.removeClass('pressed'),
      // is bound module selected?
      bound: bm != null,
    });
//zuix.componentize(this.view());
  }

}

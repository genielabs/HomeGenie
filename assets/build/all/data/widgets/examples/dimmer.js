/**
 * Controller for the Dimmer Widget.
 * This class manages a dimmer control, syncing its level (0-100%) with a bound module
 * that likely expects a normalized value (0.0-1.0). It also handles command throttling
 * to prevent overwhelming the target device.
 */
class Dimmer extends ControllerInstance {

  /**
   * A flag to prevent sending multiple commands simultaneously.
   * When `true`, new commands are queued or ignored.
   * @type {boolean}
   */
  busy = false;

  /**
   * A flag to prevent sending multiple commands simultaneously.
   * When `true`, new commands are queued or ignored.
   * @type {boolean}
   */
  levelRetry = 0;

  /**
   * `onInit` lifecycle hook, called before the component's view is created.
   * Used here to expose methods and set up data subscriptions.
   */
  onInit() {

    // Expose the `setLevel` method so it can be called from the component's view (HTML),
    // for example, by a slider's change event.
    this.declare({
      setLevel: this.setLevel
    });

    // Exit if no module is bound to this widget.
    if (!this.boundModule) {
      return;
    }

    // Find the field that reports the dimmer's current level.
    const levelField = this.boundModule.field('Status.Level');
    if (levelField) {
      // Subscribe to future changes of the level field.
      // When the module reports a new level, update the model.
      this.subscribe(levelField, (field) => {
        // The module's value is expected to be normalized (0.0 to 1.0).
        // Convert it to a percentage for the UI.
        this.model().level_percent = Math.round(field.value * 100);
      });
    }
  }

  /**
   * `onCreate` lifecycle hook, called after the view is created.
   * Used to set the initial state of the widget.
   */
  onCreate() {
    if (!this.boundModule) {
      this.model().name = 'No module bound';
      return;
    }

    // The view and the model now exist.
    // Sync the initial state from the bound module to the component's model.
    const levelField = this.boundModule.field('Status.Level');
    // Get the initial value from the field, defaulting to 0 if not found.
    const initialValue = levelField ? levelField.value : 0;

    // Set the initial model properties.
    this.model().name = this.boundModule.name;
    this.model().level_percent = Math.round(initialValue * 100);
  }

  /**
   * Sets the dimmer level. This method is exposed to the view.
   * @param {number} percentValue The new level, as a percentage (0-100).
   */
  setLevel(percentValue) {
    if (!this.boundModule) return;

    // If a command is already in flight, don't send another one.
    // Instead, schedule a retry with the latest value after a 100ms delay.
    // This effectively "debounces" rapid input from a slider.
    if (this.busy) {
      clearTimeout(this.levelRetry);
      this.levelRetry = setTimeout(() => this.setLevel(percentValue), 100);
      return;
    }

    // Immediately update the model to provide instant visual feedback to the user.
    this.model().level_percent = percentValue;

    // Lock the control to prevent further commands until this one is acknowledged.
    this.busy = true;

    // Send the 'Control.Level' command to the bound module.
    // The value is normalized back to the 0.0-1.0 range, assuming the control expects it.
    // NOTE: The current code sends `percentValue` (0-100). If the module expects
    // a normalized value, this should be `percentValue / 100`.
    this.boundModule.control('Control.Level', percentValue).subscribe(() => {
      // The command has been acknowledged by the module.
      // Unlock the control to allow new commands.
      this.busy = false;
    });
  }
}

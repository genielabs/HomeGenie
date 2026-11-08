// The component controller
// @see https://zuixjs.org/pages/documentation/controller/

class TimeClock extends ControllerInstance {
  settings = {
    defaultSize: 'small'
  }

  // Life-cycle method
  // @see https://zuixjs.org/pages/documentation/controller/#onInit
  onInit() {

    // get day/month names for current locale
    this.locale = new Intl.DateTimeFormat().resolvedOptions().locale;
    this.timeZone = this.options().timeZone || new Intl.DateTimeFormat().resolvedOptions().timeZone;

    // Set the data model (a binding adapter in this case).
    // Read more about various data binding methods here:
    // @see http://zuixjs.org/pages/documentation/view/
    this.model(($el, field, $view, refreshCallback) =>
      this.refreshFn($el, field, $view, refreshCallback));

  }

  /**
   * Model<->View binding adapter
   * @see https://zuixjs.org/pages/documentation/view/#bindingadapters
   *
   * @param {ZxQuery} $el - The target element.
   * @param {string} field - Name of the updated field.
   * @param {ZxQuery} $view - The component view
   * @param {function} refreshCallback - Callback to request a new model-to-view refresh
   */
  refreshFn($el, field, $view, refreshCallback) {
    const timeZone = this.timeZone;
    const now = new Date();

    // update requested view #field
    switch (field) {

      case 'time':
        $el.html(now.toLocaleTimeString(this.locale, {timeZone}));
        break;

      case 'day':
        const day = new Intl.DateTimeFormat(this.locale, {
          timeZone, weekday: 'long'
        }).format(now);
        $el.html(day);
        break;

      case 'date':
        const date = new Intl.DateTimeFormat(this.locale, {
          timeZone, dateStyle: 'long'
        }).format(now);
        $el.html(date);
        break;

      case 'timezone':
        $el.html(timeZone
            .replace('_', ' ')
            .replace('/', ' / '));
        break;

    }
    refreshCallback(1000);
  }
}

// Static fields

/** @param {number} angle */
const rotate = (angle) => ({
  transition: (angle === 6) ? 'none' : null,
  transform: `rotate(${(angle || 360) + 180}deg)`
});
/** @param {ZxQuery} $el */
const secondsCss = ($el) => {
  const seconds = new Date().getSeconds();
  const angle = (seconds * 6);
  $el.css(rotate(angle));
};
/** @param {ZxQuery} $el */
const minutesCss = ($el) => {
  const decimals = new Date().getSeconds() / 60;
  const angle = ((new Date().getMinutes() + decimals) * 6);
  $el.css(rotate(angle));
};
/** @param {ZxQuery} $el */
const hoursCss = ($el) => {
  const decimals = new Date().getMinutes() / 60;
  const angle = ((new Date().getHours() % 12 + decimals) * 30);
  $el.css(rotate(angle));
};

// Component implementation

class AnalogClock extends ControllerInstance {
  onInit() {
    // declare methods that can be used
    // in the scripting scope of the view
    this.declare({
      secondsCss,
      minutesCss,
      hoursCss
    });
  }
}

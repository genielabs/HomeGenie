/**
 * Controller for the Weather Widget.
 * This class is responsible for fetching weather data from a bound module
 * and updating the component's model to reflect the latest conditions and forecast.
 */
class WeatherWidget extends ControllerInstance {

    /**
     * The base path where weather SVG icons are located.
     * @type {string}
     */
    svgBasePath = 'assets/widgets/weather/images/fill/';

    /**
     * The `onCreate` lifecycle hook, called when the component is created and its view is ready.
     * It sets up initial data and subscribes to data changes.
     */
    onCreate() {
      // Exit early if no module is bound to this widget instance.
      if (!this.boundModule) return;

      // Subscribe to changes in the 'Sensor.Temperature' field.
      // When the temperature changes, trigger a model update after a short delay.
      // The delay (500ms) can help batch multiple rapid updates if they occur.
      const temperatureField = this.boundModule.field('Sensor.Temperature');
      if (!temperatureField) return;
      this.subscribe(temperatureField, () => {
        setTimeout(() => this.updateModel(), 500);
      });

      // Perform an initial update to populate the widget with current data.
      this.updateModel();
    }

    /**
     * Fetches all required data from the bound module's fields and updates the component's model.
     * The model properties are directly tied to the view, so any changes here will be
     * reflected in the HTML.
     */
    updateModel() {
       // Guard clause to prevent errors if the module is not available.
        if (!this.boundModule) return;

        // Get a reference to the component's reactive model.
        const m = this.model();
        // Cache the module's fields for easier access.
        const moduleFields = this.boundModule.fields;

        /**
         * A helper function to safely retrieve a field's value by its key.
         * @param {string} key The key of the field to find (e.g., 'Sensor.Temperature').
         * @returns {any|null} The value of the field, or null if not found.
         */
        const getFieldValue = (key) => {
            const field = moduleFields.find(f => f.key === key);
            return field ? utils.format.fieldValue(field) : null;
        };

        // --- Update Current Conditions ---
        m.city = getFieldValue('Conditions.City') || 'Somewhere';
        m.temperature = getFieldValue('Sensor.Temperature') || '--';
        m.description = getFieldValue('Conditions.Description') || 'Not Configured';

        const currentIconCode = getFieldValue('Conditions.IconType');
        m.weather_icon_img = this.getSvgIconPath(currentIconCode);

        // --- Update 3-Day Forecast ---

        // Day 1
        m.forecast_1_day = getFieldValue('Conditions.Forecast.1.Weekday') || '--';
        const forecast1IconCode = getFieldValue('Conditions.Forecast.1.IconType');
        m.forecast_1_icon_img = this.getSvgIconPath(forecast1IconCode);
        m.forecast_1_temp_max = getFieldValue('Conditions.Forecast.1.Temperature.Max') || '--';
        m.forecast_1_temp_min = getFieldValue('Conditions.Forecast.1.Temperature.Min') || '--';

        // Day 2
        m.forecast_2_day = getFieldValue('Conditions.Forecast.2.Weekday') || '--';
        const forecast2IconCode = getFieldValue('Conditions.Forecast.2.IconType');
        m.forecast_2_icon_img = this.getSvgIconPath(forecast2IconCode);
        m.forecast_2_temp_max = getFieldValue('Conditions.Forecast.2.Temperature.Max') || '--';
        m.forecast_2_temp_min = getFieldValue('Conditions.Forecast.2.Temperature.Min') || '--';

        // Day 3
        m.forecast_3_day = getFieldValue('Conditions.Forecast.3.Weekday') || '--';
        const forecast3IconCode = getFieldValue('Conditions.Forecast.3.IconType');
        m.forecast_3_icon_img = this.getSvgIconPath(forecast3IconCode);
        m.forecast_3_temp_max = getFieldValue('Conditions.Forecast.3.Temperature.Max') || '--';
        m.forecast_3_temp_min = getFieldValue('Conditions.Forecast.3.Temperature.Min') || '--';
    }

    /**
     * Constructs the full path to an SVG weather icon based on its code.
     * @param {string} iconCode The code for the weather icon (e.g., 'sunny', 'cloudy').
     * @returns {string} The full, usable path to the SVG file.
     */
    getSvgIconPath(iconCode) {
        iconCode = iconCode || '01d';
        return `${this.svgBasePath}${iconCode}.svg`;
    }
}

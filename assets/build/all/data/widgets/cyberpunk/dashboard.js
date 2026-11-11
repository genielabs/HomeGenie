class NeonDistrict extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard',
    sizeOptions: [],
    boundModules: {
      dashboardProgram: {
        label: 'Dashboard program', labelKey: 'bind_dashboard_program',
        typeFilter: 'program', fieldFilter: 'Dashboard.Theme.Cyberpunk',
        defaultModule: 'HomeAutomation.HomeGenie.Automation:651'
      },
      light1: {label: 'light_1', typeFilter: 'Light,Dimmer,Color', defaultModule: 'HomeAutomation.PhilipsHue:1', defaultField: 'Simulator.Meter.Watts'},
      light2: {label: 'light_2', typeFilter: 'Light,Dimmer,Color', defaultModule: 'HomeAutomation.PhilipsHue:2', defaultField: 'Simulator.Meter.Watts'},
      light3: {label: 'light_3', typeFilter: 'Light,Dimmer,Color', defaultModule: 'HomeAutomation.PhilipsHue:4', defaultField: 'Simulator.Meter.Watts'},
      climate: {label: 'Climate sensor', labelKey: 'bind_climate', fieldFilter: 'Sensor.Temperature,Sensor.Humidity', defaultModule: 'HomeAutomation.Demo:1'},
      energy: {
        label: 'Power meter', labelKey: 'bind_energy',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor',
        defaultModule: 'HomeAutomation.EnergyMonitor:1'
      }
    }
  };

  labels = {
    console_label: 'Home operations / 01',
    scene_label: 'Residential district',
    headline: 'The city after dark.',
    headline_day: 'The city in daylight.',
    subtitle: 'Your lights, climate and power on one console.',
    footer_hint: 'Connect your devices in dashboard settings.',
    no_lights: 'No lights linked',
    unknown: 'Light status unavailable',
    all_off: 'All lights off',
    one_on: '1 light on',
    lights_on: '{count} lights on',

    weather_title: 'Outside conditions',
    weather_unavailable: 'Weather data unavailable',
    weather_settings: 'Weather settings',

    district_message: 'District message',
    weather_updated: 'Observed {time}',
    forecast_day: '{day} at 12:00 UTC',
    clear: 'Clear',
    few_clouds: 'Partly cloudy',
    scattered_clouds: 'Scattered clouds',
    overcast: 'Cloudy',
    showers: 'Showers',
    rain: 'Rain',
    thunder: 'Thunderstorms',
    snow: 'Snow',
    mist: 'Mist',
    wind: 'Wind',
    pressure: 'Pressure',
    rain_hour: 'Rain, last hour',
    snow_hour: 'Snow, last hour',
    sunrise: 'Sunrise',
    sunset: 'Sunset'
  };
  weatherId = 'HomeAutomation.HomeGenie.Automation:34';
  weatherTypes = {
    '01': 'clear', '02': 'few_clouds', '03': 'scattered_clouds', '04': 'overcast',
    '09': 'showers', '10': 'rain', '11': 'thunder', '13': 'snow', '50': 'mist'
  };
  active = true;
  disposed = false;
  clockTimer = null;

  onInit() {
    this.declare({

      openWeatherSettings: () => this.showSettings?.(this.weatherId)
    });
  }

  onCreate() {
    Promise.all([
      import('/widgets/cyberpunk/lighting.module.js'),
      import('/widgets/cyberpunk/telemetry.module.js')
    ]).catch(error => console.error('Could not load dashboard widgets', error));
    this.lights = [...new Set(['light1', 'light2', 'light3'].map(key => this.getBoundModule(key)).filter(Boolean))];
    for (const light of this.lights) this.subscribe(light, () => this.renderLights());

    this.reduce = matchMedia('(prefers-reduced-motion: reduce)');
    this.reducedMotionChanged = () => this.sync();
    this.reduce.addEventListener?.('change', this.reducedMotionChanged);
    this.visibilityChanged = () => this.sync();
    document.addEventListener('visibilitychange', this.visibilityChanged);
    this.view().on('refresh:active', () => {
      this.active = true;
      this.renderLights();
      this.bindDashboardProgram();
      this.bindWeather();
      this.sync();
    });
    this.view().on('refresh:inactive', () => { this.active = false; this.sync(); });

    const language = utils.preferences?.ui?.language;
    let locale = language && language !== 'auto' ? language : navigator.language;
    const time = {hour: '2-digit', minute: '2-digit', second: '2-digit'};
    const date = {weekday: 'short', month: 'short', day: 'numeric'};
    try {
      this.clockFormat = new Intl.DateTimeFormat(locale, time);
      this.dateFormat = new Intl.DateTimeFormat(locale, date);
    } catch {
      locale = navigator.language;
      this.clockFormat = new Intl.DateTimeFormat(locale, time);
      this.dateFormat = new Intl.DateTimeFormat(locale, date);
    }
    this.weatherDate = new Intl.DateTimeFormat(locale, {...date, timeZone: 'UTC'});
    this.weatherWeekday = new Intl.DateTimeFormat(locale, {weekday: 'short', timeZone: 'UTC'});
    this.weatherTime = new Intl.DateTimeFormat(locale, {month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'});
    this.weatherNumber = new Intl.NumberFormat(locale, {maximumFractionDigits: 1});
    this.bindDashboardProgram();
    this.bindWeather();
    this.renderLabels();
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.renderLabels();
      });
    }
  }

  renderLabels() {
    const model = this.model();
    for (const key of ['console_label', 'scene_label', 'footer_hint', 'weather_title'])
      model[key] = this.labels[key];
    this.renderWeather();
    this.renderLights();
    this.sync();
  }

  bindDashboardProgram() {
    const program = this.getBoundModule('dashboardProgram');
    if (program === this.dashboardProgram) return;
    this.dashboardProgramSubscription?.unsubscribe();
    this.dashboardProgram = program;
    this.dashboardProgramSubscription = program && this.subscribe(program, field => {
      if (field?.key?.startsWith('Dashboard.NeonDistrict.')) this.renderPresentation();
      if (field?.key === 'Dashboard.AnimationsDisabled') this.sync();
    });
    this.renderPresentation();
    this.sync();
  }

  renderPresentation() {
    if (this.disposed) return;
    const program = this.dashboardProgram;
    const read = key => String(program?.field('Dashboard.NeonDistrict.' + key)?.value ?? '').trim();
    this.field('headline').get().textContent = read('Title')
      || (this.daylight ? this.labels.headline_day : this.labels.headline);
    this.field('subtitle').get().textContent = read('Subtitle') || this.labels.subtitle;

    const message = read('Message');
    this.field('district_message').get().hidden = !message;
    this.field('district_message_label').get().textContent = this.labels.district_message;
    this.field('district_message_text').get().textContent = message;

    this.field('city_panel').get().setAttribute('data-road-speed', read('RoadSpeed'));

  }

  renderLights() {
    const levels = this.lights.map(light => {
      const field = light.field('Status.Level');
      if (light.isOnline === false || !field || field.value == null || field.value === '') return NaN;
      return field.decimalValue;
    });
    const on = levels.filter(level => Number.isFinite(level) && level > 0).length;
    const labels = this.labels;
    const summary = !levels.length ? labels.no_lights
      : on ? (on === 1 ? labels.one_on : labels.lights_on.replace('{count}', on))
      : levels.every(Number.isFinite) ? labels.all_off : labels.unknown;
    this.model().light_summary = summary;
    this.field('city_panel').get().classList.toggle('lit', on > 0);
  }

  bindWeather() {
    const weather = this.getModule?.(this.weatherId);
    if (weather !== this.weather) {
      this.weatherSubscription?.unsubscribe();
      this.weather = weather;
      if (weather) this.weatherSubscription = this.subscribe(weather, field => {
        if (!/^(Conditions|Astronomy|Sensor)\./.test(field?.key)) return;
        clearTimeout(this.weatherTimer);
        this.weatherTimer = setTimeout(() => this.renderWeather(), 120);
      });
    }
    this.renderWeather();
  }

  weatherValue(key) { return this.weather?.field(key)?.value; }

  weatherNumeric(key) {
    const field = this.weather?.field(key);
    if (field?.value == null || field.value === '') return null;
    return Number.isFinite(field.decimalValue) ? field.decimalValue : null;
  }

  weatherIcon(key) {
    const code = String(this.weatherValue(key) ?? '');
    return /^(01|02|03|04|09|10|11|13|50)[dn]$/.test(code) ? code : '';
  }

  setWeatherIcon(image, code) {
    image.hidden = !code;
    if (code) image.src = 'assets/widgets/weather/images/line/' + code + '.svg';
  }

  weatherDescription(code, key) {
    return this.labels[this.weatherTypes[code.slice(0, 2)]] || this.weatherValue(key) || '';
  }

  temperature(key) {
    const value = this.weatherNumeric(key);
    if (value === null) return '';
    const unit = utils.preferences?.units?.temperature || 'C';
    return utils.format.fieldValue({key: 'Sensor.Temperature', value}) + (unit === 'K' ? ' K' : ' °' + unit);
  }

  renderWeather() {
    if (this.disposed) return;
    const labels = this.labels;
    const model = this.model();
    const code = this.weatherIcon('Conditions.IconType');
    const mood = this.weatherTypes[code.slice(0, 2)] || 'clear';
    const day = code.endsWith('d');
    const city = this.field('city_panel').get();
    city.setAttribute('data-weather', mood);
    city.classList.toggle('daylight', day);
    this.daylight = day;
    this.renderPresentation();

    this.field('weather_panel').get().hidden = !this.weather;
    if (!this.weather) return;
    const settings = this.field('weather_settings').get();
    settings.hidden = typeof this.showSettings !== 'function';
    settings.setAttribute('aria-label', labels.weather_settings);
    settings.title = labels.weather_settings;
    this.setWeatherIcon(this.field('weather_icon').get(), code);
    model.weather_location = this.weatherValue('Conditions.DisplayLocation') || this.weatherValue('Conditions.City') || '';
    model.weather_temperature = this.temperature('Sensor.Temperature');
    model.weather_description = this.weatherDescription(code, 'Conditions.Description') || labels.weather_unavailable;
    const timestamp = this.weatherNumeric('Conditions.Timestamp');
    model.weather_updated = timestamp > 0 && timestamp <= 8.64e15
      ? labels.weather_updated.replace('{time}', this.weatherTime.format(timestamp)) : '';

    let hasForecast = false;
    const now = new Date();
    const today = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
    for (const index of [1, 2, 3]) {
      const prefix = 'Conditions.Forecast.' + index + '.';
      const date = this.weatherNumeric(prefix + 'Timestamp');
      const temp = this.temperature(prefix + 'Temperature');
      const item = this.field('forecast' + index).get();
      item.hidden = !(date >= today && date <= 8.64e15 && temp);
      if (item.hidden) continue;
      hasForecast = true;
      const forecastCode = this.weatherIcon(prefix + 'IconType');
      const weekday = this.weatherDate.format(date);
      item.title = labels.forecast_day.replace('{day}', weekday);
      item.querySelector('[data-part="day"]').textContent = this.weatherWeekday.format(date);
      item.querySelector('[data-part="temp"]').textContent = temp;
      item.querySelector('[data-part="description"]').textContent = this.weatherDescription(forecastCode, prefix + 'Description');
      this.setWeatherIcon(item.querySelector('img'), forecastCode);
    }
    this.field('weather_forecast').get().hidden = !hasForecast;
    const details = this.field('weather_details').get();
    const wind = this.weatherNumeric('Sensor.Wind.Speed');
    const bearing = this.weatherNumeric('Sensor.Wind.Direction');
    const pressure = this.weatherNumeric('Sensor.Pressure');
    const rain = this.weatherNumeric('Sensor.Precipitation.Rain');
    const snow = this.weatherNumeric('Sensor.Precipitation.Snow');
    const sunrise = this.weatherValue('Astronomy.Sunrise');
    const sunset = this.weatherValue('Astronomy.Sunset');
    const format = this.weatherNumber;
    details.textContent = [
      wind !== null && labels.wind + ' ' + format.format(wind) + ' km/h' +
        (bearing !== null ? ' (' + format.format(bearing) + '°)' : ''),
      pressure !== null && labels.pressure + ' ' + format.format(pressure) + ' hPa',
      rain > 0 && labels.rain_hour + ' ' + format.format(rain) + ' mm',
      snow > 0 && labels.snow_hour + ' ' + format.format(snow) + ' mm',
      sunrise && labels.sunrise + ' ' + sunrise,
      sunset && labels.sunset + ' ' + sunset
    ].filter(Boolean).join(' · ');
    details.hidden = !details.textContent;
  }

  updateClock() {
    const now = new Date();
    const model = this.model();
    model.clock = this.clockFormat.format(now);
    model.date_label = this.dateFormat.format(now);
  }

  sync() {
    if (this.disposed) return;
    const visible = this.active && !document.hidden;
    const disabled = /^(on|true|1)$/i.test(String(this.dashboardProgram?.field('Dashboard.AnimationsDisabled')?.value ?? ''));
    const city = this.field('city_panel').get();
    city.classList.toggle('motion-active', visible && !this.reduce?.matches && !disabled);
    city.classList.toggle('motion-disabled', disabled);
    if (visible && this.clockTimer === null) {
      this.updateClock();
      this.clockTimer = setInterval(() => this.updateClock(), 1000);
    } else if (!visible && this.clockTimer !== null) {
      clearInterval(this.clockTimer);
      this.clockTimer = null;
    }
  }

  onDispose() {
    this.disposed = true;
    this.dashboardProgramSubscription?.unsubscribe();
    clearInterval(this.clockTimer);
    clearTimeout(this.weatherTimer);
    document.removeEventListener('visibilitychange', this.visibilityChanged);
    this.reduce?.removeEventListener?.('change', this.reducedMotionChanged);
  }
}

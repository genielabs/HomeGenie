const defaultDuration = 24 * 60 * 60 * 1000;
const chartWidth = 180;
const maxSamples = chartWidth * 4;

// Backfill once with the same provider as Energy Monitor, then merge module events.
export class PowerHistory {
  constructor(redraw, duration = defaultDuration) {
    this.redraw = redraw;
    this.duration = duration;
  }

  update(module, key, reading) {
    if (!this.series || this.series.module !== module || this.series.key !== key) {
      this.dispose();
      this.series = {module, key, end: Date.now(), samples: new Map()};
      if (module && key) this.load(this.series);
    }
    const series = this.series;
    const timestamp = Math.trunc(Number(reading?.timestamp));
    const value = reading?.decimalValue;
    if (!Number.isFinite(timestamp) || !Number.isFinite(value) || timestamp < series.end - this.duration) return;
    if (series.samples.get(timestamp) === value) return;

    series.end = Math.max(series.end, timestamp);
    series.samples.set(timestamp, value);
    series.points = undefined;
    for (const time of series.samples.keys()) {
      if (time < series.end - this.duration) series.samples.delete(time);
    }
    if (series.samples.size > maxSamples) {
      const times = [...series.samples.keys()].sort((a, b) => a - b);
      // Reduce density without dropping the oldest part of the chart.
      for (let index = 1; index < times.length - 1; index += 2) series.samples.delete(times[index]);
    }
  }

  load(series) {
    const {module, key, end} = series;
    this.request = module.control?.('Statistics.Field.GetData', {
      dataApi: 'DataProcessing.Filters/Series', dataFilter: 'MovingAverage',
      field: key, rangeStart: end - this.duration, rangeEnd: end, chartWidth
    })?.subscribe({
      next: points => {
        if (this.series !== series || !Array.isArray(points)) return;
        for (const {x, y} of points) {
          const time = Math.trunc(x);
          if (Number.isFinite(time) && Number.isFinite(y) && time >= series.end - this.duration && time <= end && !series.samples.has(time)) {
            series.samples.set(time, y);
          }
        }
        series.points = undefined;
        this.redraw();
      },
      error: () => {
        // History is optional. The same series continues to receive live readings.
      }
    });
  }

  get points() {
    const series = this.series;
    if (!series || series.samples.size < 2) return '';
    if (series.points !== undefined) return series.points;
    const sorted = [...series.samples].sort(([left], [right]) => left - right);
    const values = sorted.map(([, value]) => value);
    const min = Math.min(...values), span = Math.max(...values) - min;
    const start = series.end - this.duration;
    series.points = sorted.map(([time, value]) => {
      const x = (time - start) * chartWidth / this.duration;
      const y = span ? 37 - 32 * (value - min) / span : 21;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    }).join(' ');
    return series.points;
  }

  dispose() {
    this.series = undefined;
    this.request?.unsubscribe();
    this.request = undefined;
  }
}

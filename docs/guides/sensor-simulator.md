# Sensor Simulator

Program 410 (`sensor-simulator`, demo package), version 1.0.7, generates bounded indoor demo readings from the server's local date/time. Its helper lives in the program's ScriptContext, not in the HomeGenie core:

```csharp
GetSensorValue(DateTime localTime, string sensorType, string sensorId = "")
// Example from the program:
GetSensorValue(DateTime.Now, "Sensor.Temperature", "HomeAutomation.Demo:1")
```

Supported sensor types are `Sensor.Temperature`, `Sensor.Humidity` and `Sensor.Luminance`, or their short names `temperature`, `humidity`, `luminance` (case insensitive). An unknown type throws an argument error. The optional sensor ID gives each module a small stable offset rather than identical readings.

| Reading | Bounds | Profile |
| --- | --- | --- |
| Temperature | 16–29 °C | Warmer in summer and during the afternoon; small gradual variation. |
| Humidity | 35–65% | Seasonal/daytime variation broadly inverse to temperature. |
| Luminance | 0–1000 lux | Indoor daylight with longer days in summer. At night the demo reading varies from 0 to 100 lux over 48 minutes. |

This is a generic temperate northern-hemisphere demo, not local weather, astronomical sunrise/sunset or a simulation of a particular building. It does not infer artificial lighting from other modules. Minute/second resolution and continuous variation avoid abrupt month/hour boundaries. Values are calculated directly from time, not accumulated from previous readings, so emitting more often cannot cause drift or push the humidity towards 95%.

The existing feature switches and `Simulator.Sensor.DataFrequency` remain authoritative. Meter power is separate: `GetPowerValue(maximumWatts, level)` uses the configured maximum and device level (bounded to 0–1), with no random jitter; it never produces negative power. Off samples continue to be emitted as zero at the configured frequency, preserving historical averages.

The first sample after an update replaces the old accumulated reading. The script does not rewrite module configuration.

## Verification

`SensorSimulatorTest` compiles the distributed setup, source and context, checks XML/manifest checksums and versions, exercises ordinary/leap-year ranges, season/daylight behavior, continuity, repeatability, invalid types, power and feature/frequency gating.

Run through the normal HomeGenie test project with the `SensorSimulatorTest` filter. Seven isolated scenarios also passed while preparing 1.0.5. Vulkan updated only 410 using the revisioned program-update flow with backup; compilation and running state were confirmed.

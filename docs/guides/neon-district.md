# Neon District

Neon District is a cyberpunk dashboard for lights, indoor climate and power. Its illustrated city lights up when a connected light turns on. The clock uses the browser's time zone.

In dashboard settings, choose **Custom Widget** and select `cyberpunk/dashboard`. Use the dashboard toolbar's **Bound Modules** button to choose up to three lights, a climate sensor and a power meter. The lighting panel waits for the device to report its new state. The power trace shows recent changes while the dashboard is open. Missing readings appear as a dash.

The setup wizard installs **Neon District** from the `homegenie-themes` package. The dashboard uses the compatible program at ID 651 by default. Use **Bound Modules** to choose another compatible program or disconnect it. The regular gear in the dashboard toolbar opens the active program's options. Use **Disable animations** to pause the road and weather animations. Leave the title or subtitle blank to use the translated theme text. The road offers Very slow, Slow and Normal speeds. A district message appears only when it has text. Changes appear in each dashboard that uses the program.

The program declares `Dashboard.Theme.Cyberpunk` as a compatibility marker and emits `Dashboard.NeonDistrict.*` and `Dashboard.AnimationsDisabled` when options change. Compatible demo lights, the multisensor and the energy monitor are selected by default. Use **Bound Modules** to replace or disconnect any of them.

The **Cyberpunk** collection also includes `cyberpunk/lighting` and `cyberpunk/telemetry`. Use them as standalone widgets or embed them in a dashboard with matching bound module roles.

The weather panel uses HomeGenie's OpenWeatherMap program (34). Open its settings with the gear icon in the weather panel. The scene follows the reported conditions and switches between a sun and moon using the weather icon's day or night state. Forecast temperatures are for 12:00 UTC on each day.

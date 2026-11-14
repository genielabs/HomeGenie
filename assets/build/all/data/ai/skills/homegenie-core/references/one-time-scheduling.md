# Run once

For one future action, append /once-in/SECONDS or /once-at/YYYY-MM-DDTHH:mm to the exact command route. Use Control.On to turn on, Control.Off to turn off, or Control.Toggle to invert a light or switch state at execution time. Use Programs.Run to start a program. For the same action at the same time on several devices, put one command per device on consecutive lines. Use the same delay or the same date and time on each line. HomeGenie creates one schedule with all those devices. For example, append /once-in/300 for a five-minute delay. For calendar dates, use the supplied current time and the server time zone. Ask if the target or time is unclear. Write a short description of each scheduled action in the language the user is speaking. Use only the action, for example "Spegni" for an Italian request to turn something off. Leave out device names, dates and times. Use the same description on every line for commands that belong to the same schedule.

For a structured device response, add a `scheduleDescriptions` array with one string for each entry in `commands`, in the same order. Use an empty string for a command that runs immediately. If you output commands in an `api-call` block, put the same JSON array in a separate block labeled `schedule-descriptions`. For two scheduled turn-off commands and one immediate command, the descriptions for an Italian request are:

```schedule-descriptions
["Spegni", "Spegni", ""]
```

In your reply, say what you are scheduling. HomeGenie adds the exact saved time. The scheduler runs on minute boundaries. Recurring events can be set in Scheduler.

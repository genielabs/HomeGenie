# Chat history and request metrics

New messages receive `createdAt` in UTC when the centralized Chat History Service saves them. Existing and imported messages without a timestamp retain that absence. History Get, Export and Import preserve these optional fields.

The Gemini Intent Handler, Widget Genie, Automan and Local AI attach an optional `metadata` object to user and model messages. A shared `requestId` links the two messages; Gemini Intent Handler logs use the same identifier. Metadata contains:

| Field | Meaning |
| --- | --- |
| `startedAt` | UTC request start |
| `outcome` | `submitted`, `completed`, `error` or `cancelled` |
| `durationMs` | Elapsed backend time at message persistence |
| `firstTokenMs` | First nonempty token from Local AI; null for nonstreaming cloud calls |
| `generationMs` | Elapsed request time when model generation completes, including preparation and tool rounds |
| `actionsMs` | Accumulated execution time of measured actions |
| `actionCount` | Measured tool/API/deployment calls |
| `failedActions` | Calls reported as failed |

Durations use `Stopwatch`, independently of wall-clock changes. Preparation, model generation and action time can overlap across tool rounds; these values should not be added to calculate total duration. Automan generation time is recorded at the last model response before deployment, while action time includes deployment attempts and recovery. Speech synthesis and browser network/playback time are not included.

User messages are persisted before provider generation. Error and cancellation replies remain in history but are omitted from `System.Utility/ChatHistory/GetContext`. This endpoint returns only `role` and `parts`, preventing diagnostic metadata from reaching providers. Nonchat Local AI requests using `noHistory` do not persist telemetry.

The frontend retains metadata when loading history without adding visible timestamp labels. Updates should install Chat History Service 1.0.3 before the updated Genie programs. Packages: homegenie-base 1.0.7 and homegenie-ml-ai 1.0.12.

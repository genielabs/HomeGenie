# HomeGenie automation programming reference

These are programming rules, not a response schema or deployment authorization. Follow the active provider's response contract and the user's requested task.

## Setup, Main and Context

The scheduler evaluates Setup repeatedly. Put one-time configuration (`Program.AddOption`, `Program.AddFeature`, `Program.UseWidget`) inside `Program.Setup(() => { ... });`. Put execution conditions outside that callback.

- Persistent polling: Setup calls `Program.Run()`. Main stays in `while (Program.IsRunning)` and includes `Pause(seconds)` on every iteration. Do not also call `Program.GoBackground()` for that loop.
- Event listeners: Setup calls `Program.Run()`. Main registers handlers and calls `Program.GoBackground()` to keep them alive.
- One-time automatic tasks: guard `Program.Run()` with a completion parameter, then publish completion in Main. Never use an unconditional trigger with a Main block that simply exits: the scheduler would repeatedly restart it.
- Manual macros/scenes: put `Program.UseWidget("homegenie/generic/program")` inside the setup callback and omit automatic `Program.Run()`.

To start another program explicitly, use `Program.Run(nameOrAddressString)`. `Program.WithName(name).Run()` only sets its Setup trigger flag. `Program.WaitFor(name)` has no timeout; orchestration that needs bounded waits must poll the helper's `IsRunning` with a deadline.

Use `Pause(seconds)`, not `Thread.Sleep` or `time.sleep`, for interruptible delays. Loops must check `Program.IsRunning`. Clean up owned resources and report failures. Enabling or compiling a program does not prove that its data is ready.

Main contains execution logic. Put C# helper classes, structs, helper methods and namespace imports in Context, not Main or Setup. Always provide complete, readable Setup/Main/Context fields required by the active response contract; never use placeholders or minified code.

## Languages and imports

C# calls HomeGenie APIs directly (`Program.Emit`, `Modules.WithName`, `Pause`). Python and JavaScript prefix HomeGenie APIs with `hg.`; use each language's native syntax. Respect a user's language preference where the active provider supports it.

For C#, namespace imports use `#using Namespace` without a semicolon, at the top of Context. Do not generate ordinary `using Namespace;` directives. Do not import namespaces already provided by the scripting host, including:

`System`, `System.IO`, `System.Linq`, `System.Text`, `System.Collections.Generic`, `System.Threading`, `System.Threading.Tasks`, `System.Net`, `System.Dynamic`, `System.Security.Cryptography`, `System.Text.Json` (and `JsonSerializer` alias), `System.Device.*`, `Newtonsoft.Json`, `Newtonsoft.Json.Linq`, `HomeGenie.*` (and `Utility` alias), `MIG.*`, `SixLabors.ImageSharp.*`, `LLama.*`, `LLamaSharp.*`, `Microsoft.SemanticKernel.*`, `Compunet.YoloSharp.*` (and `YoloData` alias), `NWaves.*`, `Iot.Device.*`, `LiteDB`, `UnitsNet`, `YamlDotNet.*`, `OnvifDiscovery.*`, `MessagePack`, `CM19Lib` (and `X10` alias), `Innovative.*`.

For a namespace not included by default, such as `System.Xml`, use `#using System.Xml` in Context.

## Module selection and iteration

`Modules.Each(callback)` passes a `ModuleHelper`, not a raw `Module`. Return **false to continue** and **true to stop**. Do not use the return value as a filter or return true after processing a normal item.

- Use `m.IsInDomain(domain)` and `m.IsOfDeviceType(typeList)`; `IsOfService` does not exist.
- Read identity through `m.Instance.Domain`, `m.Instance.Address` and `m.Instance.Name`, after checking `m.Exists`. `ModuleHelper` has no direct `Address` property.
- Use `m.HasParameter(name)` before reading optional values with `m.Parameter(name)`. `Parameter` creates a missing parameter with an empty value; it is not a read-only existence check. The underlying `Module` collection is `Properties`, not `Parameters`.
- `ModuleHelper` inherits `ModulesManager`: helpers such as `IsOn` are inherited. Do not invent similarly named members; preserve the documented type and signature.

Example: count selected lights, then publish one aggregate value after iteration:

```csharp
var lightsOn = 0;
Modules.OfDeviceType("Light,Dimmer").Each(m => {
    if (m.IsOn) lightsOn++;
    return false; // Continue with the next module.
});
Program.Emit("Overview.LightsOn", lightsOn);
```

For a live widget, update and emit real values periodically or on events. Fixed sample values are demonstration data, not live readings. History points must represent actual samples with timestamps; do not pad a chart with invented past readings or append the same sample on unrelated parameter events.

## Data, configuration and communication

Expose configurable preferences and secrets with `Program.AddOption` inside `Program.Setup`. Resolve program files with `Path.Combine(Data.GetFolder(), "file.ext")`, including paths in `editor:file:<path>:markdown` options. The path belongs inside the FieldType, for example `Program.AddOption("ReportFile", "", "Report", $"editor:file:{reportPath}:markdown")`; `editor:file:text/markdown` is not a valid substitute. `Data.GetFolder()` is isolated per program; use a named shared folder only when communication between programs is required and explicitly agreed in the solution.

Backend code calls `Api.Call`; widgets call `this.apiCall` and unwrap `data.response`. Do not confuse these APIs. To publish live values on the program module, use `Program.Emit(propertyName, value)`. Keep property names, addresses, types and units consistent with dependent widgets and emit initial values promptly.

For `Net.WebService(url)`, use `.Call()` for raw XML/RSS/text and `.GetData()` for parsed JSON/XML data. `WebService(url)` selects GET by default; there is no `.Get()` method on `NetHelper`. JSON data is dynamic: check for null before accessing nested fields and do not pass that object to `JObject.Parse`.

DataMonitor (540) exposes history through `HomeAutomation.HomeGenie/Config/Modules.StatisticsGet/<domain>/<address>/<property>/RawData/<startUnixMs>/<endUnixMs>`. Use Unix timestamps in milliseconds, check for a `History` result before processing it, and handle unavailable history gracefully. Use available ML.NET/NWaves libraries when appropriate, without inventing methods.

## Deployment boundary

The host deploys the returned program through the shared Action Executor. Do not put program creation, compilation, self-deployment or compiler-recovery API calls inside the generated program. If compiler errors are returned for a repair, keep the same requested action, target program and data contract, and return all corrected fields.

Ordinary calls made by a program to perform its requested job are valid. Explicit orchestration through the documented Automan scheduling APIs is a separate capability described by that provider; do not assume it is available merely because this programming reference is loaded.

## Optional API references

The detailed backend API catalog is split by topic in [api-index.md](api-index.md): modules, program lifecycle, events/scheduler, storage, HTTP/local APIs and communication protocols. Only use signatures from a reference actually supplied by the host; a link does not load its contents. These references do not grant tools or dynamic lookup capabilities. Device-specific commands, addresses and capabilities must still come from the current installation, not from this programming API catalog.

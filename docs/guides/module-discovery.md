# Module schema and program features

These read-only views expose existing module/ProgramFeature metadata without returning every program's source or any current field values. They do not initialize missing parameters or grant write permissions.

## Routes

The HTTP prefix is `/api/HomeAutomation.HomeGenie/`. Internal Api.Call commands omit `/api/`.

| Route | Purpose |
| --- | --- |
| `Config/Modules.Get/{domain}/{address}/Schema` | Module identity, properties and applicable features, without values/timestamps/sources |
| `Automation/Programs.List/Features` | Paged searchable feature catalog across programs |
| `Automation/Programs.Get/{programId}/Features` | Feature definitions declared by one program, including disabled owners |
| `Automation/Programs.Get/{programId}/Features/{feature}` | One exact feature definition, identified by its property name |
| `Config/Modules.ParameterGet/{domain}/{address}/{property}/Context` | Compact current-value read with sensitive-field and size checks |

Selectors are case-sensitive: Schema, Features and Context. The replaced standalone discovery commands have no aliases. Ordinary Modules.Get, Programs.Get, Programs.List and ParameterGet retain their existing output. Modules.FeaturesGet retains its UI role and missing-field initialization; schema reads never initialize fields.

## Module schema

Schema contains version=1, module identity, total, offset, limit, nextOffset and two arrays:

- properties: fields whose effective FieldType is absent/empty.
- features: fields whose effective FieldType is nonempty.

The effective type uses stored module metadata when present, otherwise the applicable declared feature metadata. Each key appears in only one array. There is no nested features array inside properties. Both rows use key, description, fieldType, present and sensitive. programs identifies declaring programs by id/name/enabled and retains each declaration's fieldType, so multiple owners can be represented without duplicating the key or losing constraints. A locally typed field can be a feature without any declaring owner; that alone does not authorize an AI write.

Applicable definitions include active owners only, respecting the existing domain/type/property/value and virtual-module matching rules. Declared but absent fields have present=false; their values are not created. Inactive definitions can still be inspected through program feature views. Neither current values, timestamps, source code nor program Data enter the schema.

Schema accepts an optional JSON body containing offset and limit only. Pagination applies to the combined ordered unique key set, then splits that page into properties/features. Limits are 1–200, default 100; nextOffset indicates omitted rows in either array. Gemini currently receives the first page only and must not infer that omitted keys do not exist.

```http
GET /api/HomeAutomation.HomeGenie/Config/Modules.Get/Example.Lights/1/Schema
```

## Program feature views

The catalog accepts optional JSON filters programId, query, offset, limit and includeDisabled. The default catalog includes enabled owners; includeDisabled=true includes inactive ones. A program-specific view includes its inactive definitions, independently of runtime applicability. An unknown program/feature is rejected. Conflicting route/body program IDs and unknown filters are rejected.

Results use version, total, offset, limit, nextOffset and items. Items contain property, description, fieldType, program and appliesTo. Definitions with empty FieldType are properties and are excluded from these feature views. Feature names are exact property keys, not human descriptions. Default limit is 50, maximum 200.

Example body for Automation/Programs.List/Features:

```json
{"query":"timeout","offset":0,"limit":20}
```

## Current values and AI integration

ParameterGet/.../Context returns only version, domain, address, key, present and value. Absent fields stay absent. Password types, recognizable credential names and values above 4096 UTF-8 bytes are rejected. The sensitivity filter is conservative, not a complete classification of arbitrary custom properties. Existing HTTP authorization applies.

Gemini's kind=context resources.moduleIds (domain/address) requests the schema; parameterIds (domain/address/property) requests selected Context values. Both require Devices context. Source requests via programIds still require Automation Programs context. Each ID array is bounded at 16 and existing byte/lookup-round limits remain. Schema/value response shapes and target identities are checked so an older backend's full ordinary Get response cannot be inserted as context. History/import/rendering never executes lookups or actions.

## Validated setting changes

The Action Executor program validates an explicit setting-change request against schema.features and all declaring programs' field types. Existing active applicable checkbox and bounded slider settings are supported; arbitrary properties and sensitive fields are rejected. Gemini additionally requires deviceControl and Devices context.

Writes use the unchanged API:

```text
HomeAutomation.HomeGenie/Config/Modules.ParameterSet/HomeAutomation.PhilipsHue/3/HomeGenie.SmartLights.SwitchOffTimeout/60
```

The executor reads the prior value, rechecks it before writing, calls ParameterSet, checks its response and independently rereads the setting. This is optimistic checking, not an atomic compare-and-set across other clients. Standard ParameterSet owns saving; its response plus readback does not independently prove a durable disk flush. The action ledger reserves identities before side effects and records prior/requested/observed values. Validation errors fail before writing; uncertain post-dispatch outcomes are not automatically retried or rolled back.

Smart Device (145) remains the pilot. Timeout=0 means its 30-second default; disabling SmartLights.Enable disables the whole feature, not just auto-off. Do not infer such semantics from field type alone.

## Deployment and verification

Rebuild/restart the backend and update AI Action Executor 1.2.5 and Gemini Genie 1.0.9. Deploy shared homegenie-genie skill 1.0.5. Ordinary chat stores and current settings remain unchanged. No runtime inventory is rewritten during source development.

The preceding standard-API live test passed on Vulkan (60 -> 30 -> 60 seconds, completed/verified receipts and independent reads). The Schema/Features/Context migration also passed live on Vulkan on 2026-10-16: catalog and single-feature reads, disjoint property/feature classification (14 properties, 20 features for Porch Light), unchanged ordinary Get responses, and a Gemini batch changing 60 -> 30 -> 60 through standard ParameterSet with two completed/verified receipts and independent final readback. Automated tests cover classification, owner aggregation, no values/missing-field initialization, filters/paging/inactive owners, contextual read guards and compiled shipped consumers.

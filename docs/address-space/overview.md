---
eyebrow: 'Docs · Address space'
lede:    'The root structure of the ~300-node test address space. Where things live, how the namespaces are split, what each top-level folder contains.'

see_also:
  - { href: './browse-paths-and-access-levels.md',     meta: '4 min' }
  - { href: '../data-features/scalar-types.md',         meta: '5 min' }
  - { href: '../runtime-features/methods.md',           meta: '5 min' }

prev: { label: 'Certificate authentication',  href: '../authentication/certificate-authentication.md' }
next: { label: 'Browse paths and access levels', href: './browse-paths-and-access-levels.md' }
---

# Address space overview

All ten classic servers expose the same application address
space — around 200 custom nodes rooted under one folder, plus
the framework nodes that every UA-.NETStandard server provides
in `ns=0`.

## Namespaces

`TestNodeManager` registers exactly three URIs at construction
time:

| Order | URI                                  | What lives here                                                        |
| ----- | ------------------------------------ | ---------------------------------------------------------------------- |
| 1     | `urn:opcua:testserver:nodes`         | All custom test nodes (typically resolves to `ns=1`)                   |
| 2     | `http://opcfoundation.org/UA/DI/`    | Reserved for Device Integration types — currently empty                |
| 3     | `urn:opcua:test-server:custom-types` | Extension-object DataType + encoding nodes (typically `ns=3`)          |

`http://opcfoundation.org/UA/` is always present in any UA
server as `ns=0`. The actual numeric index for each of the
custom URIs above is assigned by the SDK at startup and should
be read out of `Server.NamespaceArray` rather than hardcoded —
the indices above are the **typical** allocations and have been
stable across builds, but they are not pinned.

Almost everything tests reach for is in `ns=1`. Extension-object
type definitions and their encoding nodes are in
`urn:opcua:test-server:custom-types` (typically `ns=3`); see
[Structures and extension
objects](../data-features/structures-and-extension-objects.md)
for the encoding NodeIds.

## NodeId format

NodeIds in `ns=1` use **string identifiers** wherever stable
navigation matters:

```text
ns=1;s=TestServer/DataTypes/Scalar/BooleanValue
ns=1;s=TestServer/Methods/Add
ns=1;s=TestServer/Dynamic/Counter
```

This is by convention — the suite generates string NodeIds for
every node it creates, so tests can address nodes by stable name
across server restarts.

Numeric NodeIds are used by some auto-generated nodes (the
standard ones in `ns=0`). For `ns=1` test nodes, prefer string
NodeIds.

## ApplicationUri

```text
urn:opcua:testserver:nodes
```

Same for **every** classic server — they all advertise the same
application URI. This is convenient (one trust entry) but means
"distinguishing servers by URI" doesn't work — distinguish by
endpoint URL or by port instead.

## Root structure

```text
Objects (ns=0;i=85)
└── TestServer (ns=1;s=TestServer)
    ├── DataTypes          # Scalars, arrays, matrices, analog
    ├── Methods            # 12 callable methods
    ├── Dynamic            # 13 time-varying variables
    ├── Events             # Single emitter object, three periodic BaseEventState timers
    ├── Alarms             # 3 alarms + 2 source variables
    ├── Historical         # 4 historized variables
    ├── Structures         # Nested objects, deep nesting
    ├── ExtensionObjects   # PointValue, RangeValue
    ├── AccessControl      # 50 access-mode test variables
    └── SecurityKeyService # Only on opcua-sks
```

Each top-level folder maps to one builder class in
`src/TestServer/AddressSpace/`. They're independent — disabling
one (via `OPCUA_ENABLE_*` env vars) doesn't affect the others.

## Views

```text
Views (ns=0;i=87)
├── OperatorView      → Dynamic, Methods, Alarms
├── EngineeringView   → Everything (TestServer root)
├── HistoricalView    → Historical
└── DataView          → DataTypes, Structures, ExtensionObjects
```

Views are filtered perspectives of the address space — see
[Views](../special-features/views.md).

## Folder-level browse

A single browse from `TestServer` returns the 9 (or 10 on SKS)
top-level folders:

| Folder              | NodeId (string)                                |
| ------------------- | ---------------------------------------------- |
| `DataTypes`         | `ns=1;s=TestServer/DataTypes`                  |
| `Methods`           | `ns=1;s=TestServer/Methods`                    |
| `Dynamic`           | `ns=1;s=TestServer/Dynamic`                    |
| `Events`            | `ns=1;s=TestServer/Events`                     |
| `Alarms`            | `ns=1;s=TestServer/Alarms`                     |
| `Historical`        | `ns=1;s=TestServer/Historical`                 |
| `Structures`        | `ns=1;s=TestServer/Structures`                 |
| `ExtensionObjects`  | `ns=1;s=TestServer/ExtensionObjects`           |
| `AccessControl`     | `ns=1;s=TestServer/AccessControl`              |
| `SecurityKeyService` | `ns=1;s=TestServer/SecurityKeyService` (only on opcua-sks) |

## Node-count summary

| Category                          | Count        |
| --------------------------------- | ------------ |
| Scalar variables (RW)             | 21           |
| Scalar variables (RO)             | 21           |
| Array variables (RW)              | 20           |
| Array variables (RO)              | 6            |
| Empty arrays                      | 14           |
| Multi-dimensional matrices        | 3            |
| Analog items (with EURange)       | 3            |
| Methods                           | 12           |
| Dynamic variables                 | 13           |
| Event types (custom)              | **0** — the suite emits standard `BaseEventState` events from one emitter object; no custom event types are registered |
| Alarms                            | 3 + 2 source |
| Historical variables              | 4            |
| Structure objects                 | 4 + 5 collection + 10 deep |
| Extension objects                 | 2            |
| Access-control variables          | 50           |
| Views                             | 4            |
| **Total custom variables/objects** | **~200**    |

Plus framework nodes (`Server`, `Aliases`, `Namespaces`) in
`ns=0` that every OPC UA server has.

## Feature toggles

Each section is opt-out via `OPCUA_ENABLE_*` env vars:

| Folder           | Disabled by                       | Default |
| ---------------- | --------------------------------- | ------- |
| `DataTypes`      | *(always on)*                     | —       |
| `Methods`        | `OPCUA_ENABLE_METHODS=false`      | on      |
| `Dynamic`        | `OPCUA_ENABLE_DYNAMIC=false`      | on      |
| `Events`         | `OPCUA_ENABLE_EVENTS=false`       | on      |
| `Alarms`         | `OPCUA_ENABLE_EVENTS=false`       | on      |
| `Historical`     | `OPCUA_ENABLE_HISTORICAL=false`   | on      |
| `Structures`     | `OPCUA_ENABLE_STRUCTURES=false`   | on      |
| `ExtensionObjects` | *(always on)*                   | —       |
| `AccessControl`  | *(always on)*                     | —       |
| `Views`          | `OPCUA_ENABLE_VIEWS=false`        | on      |

The 10 classic servers all run with the full set on. A leaner
fork might disable several.

## Where to read next

- [Browse paths and access levels](./browse-paths-and-access-levels.md) —
  navigation conventions and access-level legend.
- The per-feature pages under **Data features** and **Runtime
  features**.

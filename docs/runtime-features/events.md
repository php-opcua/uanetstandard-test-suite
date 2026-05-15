---
eyebrow: 'Docs · Runtime features'
lede:    'The three custom event types the suite emits, plus on-demand event generation. The test surface for event subscriptions and event filtering.'

see_also:
  - { href: './alarms.md',                                        meta: '4 min' }
  - { href: './methods.md',                                       meta: '5 min' }
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }

prev: { label: 'Dynamic variables',  href: './dynamic-variables.md' }
next: { label: 'Alarms',             href: './alarms.md' }
---

# Events

Path: `TestServer / Events`

The suite defines **three custom event types** in `ns=1` and
emits them periodically. Plus the `GenerateEvent` method
([Methods](./methods.md)) raises events on demand.

## The event emitter

| BrowsePath            | Type    | EventNotifier            |
| --------------------- | ------- | ------------------------ |
| `Events/EventEmitter` | Object  | `1` (SubscribeToEvents)   |

This Object is registered as an event source on the standard
`Server` node. You can subscribe to events on **either** the
`EventEmitter` node or the `Server` node and receive the same
event stream.

## Custom event types

### SimpleEventType

Inherits from `BaseEventType`.

| Extra property | Type    |
| -------------- | ------- |
| `EventPayload` | String  |

Plus inherited fields: `EventId`, `EventType`, `SourceNode`,
`SourceName`, `Time`, `ReceiveTime`, `Message`, `Severity`.

### ComplexEventType

Inherits from `BaseEventType`.

| Extra property | Type    |
| -------------- | ------- |
| `Source`       | String  |
| `Category`     | String  |
| `NumericValue` | Double  |

### SystemStatusEventType

Inherits from `BaseEventType`.

| Extra property  | Type    | Values                                       |
| --------------- | ------- | -------------------------------------------- |
| `SystemState`   | String  | `Running`, `Idle`, `Busy`, `Maintenance`     |
| `CpuUsage`      | Double  | `0–100` (simulated)                          |
| `MemoryUsage`   | Double  | `40–90` (simulated)                          |

## Periodic emission

| Type                       | Interval | Severity | Message                          |
| -------------------------- | -------- | -------- | -------------------------------- |
| `SimpleEventType`          | 2 s      | 200      | `"Periodic event #N"`            |
| `ComplexEventType`         | 5 s      | 300      | `"Complex event #N"`             |
| `SystemStatusEventType`    | 10 s     | 100 or 600 | `"System status: <state>"`     |

`SystemStatusEventType` raises severity to `600` when the
simulated state cycles to `Maintenance`. All others use a fixed
severity.

## On-demand events

Call `Methods/GenerateEvent(message, severity)` — see
[Methods](./methods.md#generateevent). Raises a `BaseEventType`
event with the given message and severity. No fixed cadence —
controlled by your test.

## Subscribing

### Basic subscription

```text
subscribe(publishingInterval=1000)
monitor(Server, MonitoredItemEventsFilter(
    selectClause=[
        EventId,
        EventType,
        SourceName,
        Time,
        Message,
        Severity,
    ],
))
```

Wait 2 seconds — your first `SimpleEventType` arrives.

### Type filter

```text
filter = ContentFilter(OfType, SimpleEventType)
→ only SimpleEventType events
```

### Severity filter

```text
filter = ContentFilter(Severity >= 500)
→ only SystemStatusEventType in Maintenance state (severity 600)
```

### Custom-property select

For `SimpleEventType`:

```text
selectClause = [Message, Severity, EventPayload]
```

`EventPayload` is the custom field. It contains
`"payload-N"` where `N` matches the event counter.

## Browse the type definitions

The custom types are reachable via the standard ObjectTypes
folder:

```text
ObjectTypes (ns=0;i=88)
└── BaseEventType
    └── SimpleEventType        (ns=1;s=SimpleEventType)
    └── ComplexEventType       (ns=1;s=ComplexEventType)
    └── SystemStatusEventType  (ns=1;s=SystemStatusEventType)
```

Reading the type's `DataTypeDefinition` returns the field list
— useful for type-aware decoders.

## Test patterns

### Basic — does anything arrive?

Subscribe with the select clause above. Wait 2 s. Assert at
least one event arrived.

### Counter increments

The `Message` field for `SimpleEventType` is
`"Periodic event #N"` where `N` increments. Track `N` across
notifications, verify it increments by 1 each time.

### Type filter precision

| Filter                       | Expected (over 10 s)            |
| ---------------------------- | ------------------------------- |
| No filter                    | ~5 Simple + ~2 Complex + ~1 SystemStatus |
| `OfType(SimpleEventType)`    | ~5 events                       |
| `OfType(SystemStatusEventType)` | ~1 event                     |

### Severity threshold

Subscribe with `Severity >= 250` and observe the mix:

| Type                       | Severity | Passes? |
| -------------------------- | -------- | ------- |
| `SimpleEventType`          | 200      | No       |
| `ComplexEventType`         | 300      | Yes      |
| `SystemStatusEventType` (normal) | 100 | No   |
| `SystemStatusEventType` (Maintenance) | 600 | Yes |

### On-demand verification

```text
subscribe (filter = OfType(BaseEventType), severity >= 700)
call Methods/GenerateEvent("test", 750)
→ exactly one event arrives with Message="test", Severity=750
```

## How the source node interacts

Events have a `SourceNode` field. For the suite's events:

| Type                       | SourceNode value          |
| -------------------------- | ------------------------- |
| `SimpleEventType`          | `Events/EventEmitter`     |
| `ComplexEventType`         | `Events/EventEmitter`     |
| `SystemStatusEventType`    | `Server` node             |
| `GenerateEvent`-raised     | `Server` node             |

You can subscribe on `Server` (catches everything) or on
`EventEmitter` (catches only the first two types). Tests
typically subscribe on `Server` for simplicity.

## Where to read next

- [Alarms](./alarms.md) — alarm-style events with state machines.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  the recipes.

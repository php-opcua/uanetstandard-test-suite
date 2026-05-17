---
eyebrow: 'Docs · Runtime features'
lede:    'Three periodic standard events fire from a single emitter object. The test surface for event subscriptions and basic event filtering — no custom event types.'

see_also:
  - { href: './alarms.md',                                        meta: '4 min' }
  - { href: './methods.md',                                       meta: '5 min' }
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }

prev: { label: 'Dynamic variables',  href: './dynamic-variables.md' }
next: { label: 'Alarms',             href: './alarms.md' }
---

# Events

Path: `TestServer / Events`

The suite does **not** define any custom event types. It emits
three standard events on a fixed cadence from a single emitter
Object. All three are constructed as `BaseEventState` instances
and stamped with one of the standard `ObjectTypeIds` values; no
`ns=1;s=SimpleEventType` (or similar) node is registered in the
address space.

This page describes what the server actually reports — anything
not mentioned here is not implemented. The behaviour comes
straight from `src/TestServer/AddressSpace/EventsAlarmsBuilder.cs`.

## The event emitter

| BrowsePath            | Type    | EventNotifier            |
| --------------------- | ------- | ------------------------ |
| `Events/EventEmitter` | Object  | `1` (SubscribeToEvents)   |

This Object is registered as the only event source for the
periodic stream. Subscribing to events on the `Server` node will
also forward these events via the standard event-bubbling path,
because `EventEmitter` is a descendant of `Objects`.

## Periodic emission

Three timers fire on independent intervals. Each one constructs a
`BaseEventState`, stamps it with the fields below, and reports it
through `EventEmitter.ReportEvent`.

| Timer | Interval | `EventType` (stamped)              | `Severity` | `SourceName`     | `Message` template                                        |
| ----- | -------- | ----------------------------------- | ---------- | ---------------- | --------------------------------------------------------- |
| #1    | 2 s      | `ObjectTypeIds.BaseEventType`       | `200`      | `"EventEmitter"` | `"Simple event #{counter}"`                               |
| #2    | 5 s      | `ObjectTypeIds.BaseEventType`       | `500`      | `"EventEmitter"` | `"Complex event: category=ProcessAlert, value={F3}"`      |
| #3    | 10 s     | `ObjectTypeIds.SystemEventType`     | `100`      | `"SystemMonitor"`| `"System status: CPU={n}%, Memory={n}%"`                  |

Notes:

- `counter` in timer #1 increments by 1 per emission and is
  shared only within that timer's closure.
- `{F3}` in timer #2 is a fresh `Random().NextDouble()` formatted
  to three decimals (a fresh `Random` is instantiated **per
  event**, so values are typically very close to each other
  within the same wall-clock second).
- The CPU and memory numbers in timer #3 come from
  `rng.Next(5, 95)` / `rng.Next(30, 80)` respectively. The
  severity is always `100` — there is no Maintenance state, no
  state machine and no severity bump.
- Every event also stamps `EventId` (fresh GUID), `Time` and
  `ReceiveTime` (both `DateTime.UtcNow`) and a `SourceNode`
  pointing at `EventEmitter.NodeId`.

That is the complete list of selectable fields beyond the
`BaseEventType` defaults. There are no `EventPayload`,
`Category`, `NumericValue`, `SystemState`, `CpuUsage` or
`MemoryUsage` properties on these events — the values are baked
into the `Message` localized text.

## On-demand events

`Methods/GenerateEvent(message, severity)` exists in the address
space (see [Methods](./methods.md#generateevent)) but the
implementation only writes a line to the server's `Console.Out`.
It does **not** call `ReportEvent` or otherwise raise an OPC UA
event, so subscribers will never see anything from this method.
If your test relies on triggering an event on demand, use one of
the three periodic timers above or modify the source.

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

Wait 2 seconds — the first message-from-timer-#1 event arrives.

### Type filter

```text
filter = ContentFilter(OfType, BaseEventType)
→ events from timers #1 and #2 (both stamped BaseEventType)

filter = ContentFilter(OfType, SystemEventType)
→ events from timer #3
```

There are no custom event types to filter on. `OfType` against a
non-existent `ns=1;s=...` type will not match anything.

### Severity filter

```text
filter = ContentFilter(Severity >= 300)
→ only timer #2 events (Severity 500)
```

`SystemEventType` always uses `Severity = 100`, so a `>= 300`
filter excludes it.

## Test patterns

### Basic — does anything arrive?

Subscribe with the select clause above. Wait 2 s. Assert at
least one event arrived. After ~10 s you should have seen events
from all three timers.

### Counter increments

The `Message` field for timer-#1 events is
`"Simple event #N"` where `N` increments by 1. Parse it out of
the `LocalizedText.Text`, track `N` across notifications, and
verify monotonicity. Note `N` resets to 1 every time the
container restarts.

### Type filter precision

Over a 10 s window after the first emission:

| Filter                          | Expected (~10 s)            |
| ------------------------------- | --------------------------- |
| No filter                       | ~5 from #1, ~2 from #2, ~1 from #3 |
| `OfType(BaseEventType)`         | ~7 (timers #1 and #2)       |
| `OfType(SystemEventType)`       | ~1 (timer #3)               |

### Severity threshold

| Timer                       | Severity | Passes `Severity >= 300`? |
| --------------------------- | -------- | -------------------------- |
| #1 (`BaseEventType`)        | 200      | No                          |
| #2 (`BaseEventType`)        | 500      | Yes                         |
| #3 (`SystemEventType`)      | 100      | No                          |

## How the source node interacts

| Timer | `SourceNode` value          | `SourceName`     |
| ----- | --------------------------- | ----------------|
| #1    | `Events/EventEmitter`       | `EventEmitter`  |
| #2    | `Events/EventEmitter`       | `EventEmitter`  |
| #3    | `Events/EventEmitter`       | `SystemMonitor` |

All three events list `EventEmitter` as their `SourceNode`. The
`SourceName` for timer #3 is the string `"SystemMonitor"` but
the source node id is still the `EventEmitter` object — there is
no separate `SystemMonitor` node in the address space.

## Where to read next

- [Alarms](./alarms.md) — the `Alarms` folder, which uses real
  `ExclusiveLimitAlarmState` / `NonExclusiveLimitAlarmState` /
  `OffNormalAlarmState` nodes instead of standalone events.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  end-to-end test recipes.

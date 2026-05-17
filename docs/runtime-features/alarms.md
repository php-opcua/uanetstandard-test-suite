---
eyebrow: 'Docs · Runtime features'
lede:    'Three alarm instances — ExclusiveLimit, NonExclusiveLimit, OffNormal — with source variables your tests can manipulate. Plus Acknowledge / Confirm / AddComment paths.'

see_also:
  - { href: './events.md',                                  meta: '4 min' }
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }

prev: { label: 'Events',           href: './events.md' }
next: { label: 'Historical data', href: './historical-data.md' }
---

# Alarms

Path: `TestServer / Alarms`

The suite ships three alarm types from the OPC UA spec, plus
two **source variables** that drive their state. You can let
the variables oscillate naturally or write to them to force a
specific alarm state.

## Source variables

| BrowsePath                | Type    | Access | Initial value | Auto-update?             |
| ------------------------- | ------- | ------ | ------------- | ------------------------ |
| `Alarms/AlarmSourceValue` | Double  | RW     | `50.0`        | **No** — clients must write |
| `Alarms/OffNormalSource`  | Boolean | RW     | `false`       | **No** — clients must write |

Neither source is driven by a timer. Both stay at their initial
value until a client writes a new value, at which point the
alarm-evaluation timer (which fires every second) picks up the
new reading and updates the alarm state on the next tick.

This is a deliberate test design: it lets you assert exact
transitions deterministically, without racing against an
internal oscillator.

## HighTemperatureAlarm (ExclusiveLimitAlarm)

| Property        | Value                         |
| --------------- | ----------------------------- |
| Type            | `ExclusiveLimitAlarmType`     |
| Source          | `Alarms/AlarmSourceValue`     |
| LowLow limit    | 5                             |
| Low limit       | 20                            |
| High limit      | 80                            |
| HighHigh limit  | 95                            |

The server-side update logic only checks the broader `value > 80
|| value < 20` predicate and sets `Severity = 800` / `ActiveState
= "Active"` for it, otherwise `Severity = 100` / `ActiveState =
"Inactive"`. It does not split between High vs. HighHigh in the
limit-state machine — the alarm just flips between two severity
levels around the outer limits. The `LowLow` and `HighHigh`
properties are still exposed on the alarm node for clients that
read them, but they do not gate a distinct state in the current
implementation.

## LevelAlarm (NonExclusiveLimitAlarm)

| Property        | Value                         |
| --------------- | ----------------------------- |
| Type            | `NonExclusiveLimitAlarmType`  |
| Source          | `Alarms/AlarmSourceValue`     |
| LowLow limit    | 10                            |
| Low limit       | 25                            |
| High limit      | 75                            |
| HighHigh limit  | 90                            |

The limit properties are exposed on the node, but the
evaluation timer in the current implementation only updates the
`HighTemperatureAlarm` and `OffNormalAlarm`. `LevelAlarm` keeps
its `EnabledState = "Enabled"`, `ActiveState = "Inactive"` and
`Severity = Low` set by `InitializeNonExclusiveAlarm`; it does
not actively transition based on `AlarmSourceValue`. If you need
multi-bit state transitions, drive the assertions off
`HighTemperatureAlarm` instead.

## OffNormalAlarm

| Property        | Value                         |
| --------------- | ----------------------------- |
| Type            | `OffNormalAlarmType`          |
| Source          | `Alarms/OffNormalSource`      |
| Normal state    | `false`                       |

When `OffNormalSource` deviates from its declared normal
(`false`), the alarm activates. Writing `true` triggers the
alarm; writing `false` clears it.

## Operations supported

All three alarms support the standard OPC UA condition
operations:

| Method        | Effect                                              |
| ------------- | --------------------------------------------------- |
| `Acknowledge` | Mark the alarm as acknowledged by the operator      |
| `Confirm`     | Confirm after acknowledgement                       |
| `AddComment`  | Attach a comment to the alarm                       |

Each takes the alarm's current `EventId` + an optional comment.

## Test patterns

### Drive transitions explicitly

There is no automatic oscillator. Each transition you want
must be triggered with a write to `AlarmSourceValue`. The
evaluation timer reacts within ~1 s.

| Write to `AlarmSourceValue` | `HighTemperatureAlarm.ActiveState` | `Severity` |
| --------------------------- | ----------------------------------- | ---------- |
| `50.0` (initial)             | `Inactive`                          | `100`      |
| `100.0`                      | `Active`                            | `800`      |
| `15.0`                       | `Active`                            | `800`      |
| `30.0`                       | `Inactive`                          | `100`      |
| `-5.0`                       | `Active`                            | `800`      |

`LevelAlarm` does not currently transition with these writes
(see the note above). If your client needs a non-exclusive limit
flow with active state changes, treat that as a TODO against the
suite rather than an observable behaviour.

### OffNormal toggle

```text
write(Alarms/OffNormalSource, true)
→ OffNormalAlarm becomes active

write(Alarms/OffNormalSource, false)
→ OffNormalAlarm clears
```

### Acknowledgement round-trip

1. Force an alarm to active (write `100.0`).
2. Wait for the alarm event to arrive at your subscription.
3. Extract `EventId` from the event.
4. Call `Acknowledge(eventId, comment="test ack")`.
5. Read the alarm's `AckedState` — expect `true`.
6. Optionally call `Confirm` and verify `ConfirmedState`.

### Comment

```text
call AddComment(eventId, "investigating")
→ next condition refresh shows the comment in Comment property
```

## Subscribing

For full alarm state change notifications, monitor events on
**`Server`** with a select clause covering the alarm-specific
fields:

```text
selectClause = [
    EventId,
    EventType,
    SourceName,
    Time,
    Message,
    Severity,
    ActiveState/Id,
    AckedState/Id,
    ConfirmedState/Id,
    Limits/HighLimit,     # for ExclusiveLimit / NonExclusiveLimit
    Limits/LowLimit,
]
```

The `Limits/*` browse path inside the select clause picks up
the threshold properties for limit-based alarms.

### Conditions refresh

OPC UA condition events use `ConditionRefresh` for resyncing.
Some clients call it on subscription start to get the current
state. The suite supports it; if your client uses it, the
initial event burst includes the **current** alarm state for
all three alarms.

## Where to read next

- [Historical data](./historical-data.md) — recorded values
  rather than live events.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) — full
  recipes (alarm flows included).

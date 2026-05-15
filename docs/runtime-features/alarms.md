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

| BrowsePath              | Type      | Access | Behaviour                                |
| ----------------------- | --------- | ------ | ---------------------------------------- |
| `Alarms/AlarmSourceValue` | Double  | RW     | `50 + 60 · sin(t)` every 1 s, range ≈ `-10 .. 110` |
| `Alarms/OffNormalSource` | Boolean  | RW     | Toggles every 20 s                       |

Writes update them immediately and the alarms transition.

## HighTemperatureAlarm (ExclusiveLimitAlarm)

| Property        | Value                         |
| --------------- | ----------------------------- |
| Type            | `ExclusiveLimitAlarmType`     |
| Source          | `Alarms/AlarmSourceValue`     |
| LowLow limit    | 5                             |
| Low limit       | 20                            |
| High limit      | 70                            |
| HighHigh limit  | 90                            |

States (mutually exclusive):

| `AlarmSourceValue`  | Active state  |
| ------------------- | ------------- |
| `> 90`              | HighHigh      |
| `70 < v ≤ 90`       | High          |
| `20 ≤ v ≤ 70`       | Normal (inactive) |
| `5 ≤ v < 20`        | Low           |
| `v < 5`             | LowLow        |

## LevelAlarm (NonExclusiveLimitAlarm)

| Property        | Value                         |
| --------------- | ----------------------------- |
| Type            | `NonExclusiveLimitAlarmType`  |
| Source          | `Alarms/AlarmSourceValue`     |
| LowLow limit    | 0                             |
| Low limit       | 15                            |
| High limit      | 75                            |
| HighHigh limit  | 95                            |

Multiple states can be active at once. If the value is 96, both
`High` and `HighHigh` are active.

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

### Watch transitions naturally

`AlarmSourceValue` oscillates as `50 + 60·sin(t)` — over one
period (~`2π` seconds), it visits every threshold:

| Time (approx) | Value     | HighTemp state | Level state           |
| ------------- | --------- | -------------- | --------------------- |
| 0 s           | 50         | Normal         | Normal                |
| ~2 s          | 100        | HighHigh       | High + HighHigh        |
| ~3 s          | 50         | Normal         | Normal                |
| ~5 s          | -10        | LowLow         | Low + LowLow           |
| ~6 s          | 50         | Normal         | Normal                |

Subscribe to events on the `Alarms` folder (or `Server`) and
verify you see the transitions.

### Force a specific state

```text
write(Alarms/AlarmSourceValue, 100.0)
→ HighTemp: HighHigh active
→ Level:    High + HighHigh active

write(Alarms/AlarmSourceValue, 50.0)
→ both back to Normal

write(Alarms/AlarmSourceValue, -5.0)
→ HighTemp: LowLow
→ Level:    Low + LowLow
```

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

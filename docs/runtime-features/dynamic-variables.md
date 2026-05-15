---
eyebrow: 'Docs · Runtime features'
lede:    'Thirteen variables that change over time — counters, waveforms, random values, status cycling. The test surface for subscriptions and monitored items.'

see_also:
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }
  - { href: './historical-data.md',                                 meta: '4 min' }

prev: { label: 'Methods',  href: './methods.md' }
next: { label: 'Events',   href: './events.md' }
---

# Dynamic variables

Path: `TestServer / Dynamic`

Thirteen read-only variables whose values change over time. The
canonical test surface for **subscriptions** and **monitored
items**.

All `accessLevel = CurrentRead`. Writes are rejected.

## Counters

| BrowseName     | Type    | Update interval | Behaviour                                  |
| -------------- | ------- | --------------- | ------------------------------------------ |
| `Counter`       | UInt32 | 1 s             | Increments by 1, starts at 0               |
| `FastCounter`   | UInt32 | 100 ms          | Increments by 1, starts at 0               |
| `SlowCounter`   | UInt32 | 10 s            | Increments by 1, starts at 0               |

Use cases:

- `Counter` — basic subscription verification (1 notification/sec).
- `FastCounter` — sampling-interval / publishing-interval ratio
  tests. With `publishingInterval=200ms`, expect every-other
  update.
- `SlowCounter` — keep-alive and long-publishing-interval tests.

## Random values

| BrowseName       | Type    | Update interval | Range                              |
| ---------------- | ------- | --------------- | ---------------------------------- |
| `Random`         | Double  | 500 ms          | `[0.0, 1.0)`                        |
| `RandomInt`      | Int32   | 1 s             | `[-1000, 1000]`                     |
| `RandomString`   | String  | 2 s             | 8–24 alphanumeric chars            |

Useful for:

- Verifying every notification carries a different value
  (deadband-vs-no-deadband tests).
- Stressing the encoder with frequent String allocations
  (`RandomString`).

## Waveforms

| BrowseName     | Type    | Period | Formula                                    |
| -------------- | ------- | ------ | ------------------------------------------ |
| `SineWave`     | Double  | 10 s   | `sin(2π · t / 10)`, range `[-1, 1]`         |
| `SawTooth`     | Double  | 5 s    | `(t % 5) / 5`, range `[0, 1)`               |
| `Square`        | Boolean | 2 s    | `true` for 1 s, `false` for 1 s            |
| `TriangleWave` | Double  | 8 s    | Linear ramp `-1 → 1 → -1`                  |

Waveforms are **computed on read** (no timer-driven update).
Each read returns the current value at that instant. That makes
them ideal for deadband tests (subscribe with deadband ε, count
notifications, compare to expected crossings).

## Timestamps

| BrowseName    | Type      | Behaviour                              |
| ------------- | --------- | -------------------------------------- |
| `Timestamp`   | DateTime  | Current server time on every read      |

`Timestamp` always returns a fresh value. Useful to verify that:

- Your client gets a new value on every poll.
- `sourceTimestamp` in the DataValue is current (the server
  populates it).

## Status cycling

| BrowseName       | Type        | Update interval | Cycle                                          |
| ---------------- | ----------- | --------------- | ---------------------------------------------- |
| `StatusVariable` | StatusCode  | 3 s             | `Good` → `Bad_CommunicationError` → `Uncertain_LastUsableValue` → repeat |

Use this to verify your client interprets:

- `Good` (`0x00xxxxxx`) — normal value
- `Bad` (`0x80xxxxxx`) — error
- `Uncertain` (`0x40xxxxxx`) — value may be stale / unreliable

Useful for testing data-change triggers with
`DataChangeTrigger = StatusValue` vs `Status` vs `Value` —
each filter produces a different notification count.

## Nullable value

| BrowseName        | Type    | Update interval | Behaviour                                          |
| ----------------- | ------- | --------------- | -------------------------------------------------- |
| `NullableDouble`  | Double  | 4 s             | Alternates `Good` (random in `[0, 100)`) ↔ `Bad_NoData` |

Tests how your client handles values that intermittently have
**no data**. When status is `Bad_NoData`, the value field may
be 0 or default — your client should treat it as absent rather
than as a real `0`.

## Subscription test recipes

### Basic 1-Hz subscription

```text
subscribe(publishingInterval=1000)
monitor(Dynamic/Counter, samplingInterval=1000)
→ expect 1 notification/sec, value incrementing
```

### Fast subscription

```text
subscribe(publishingInterval=200)
monitor(Dynamic/FastCounter, samplingInterval=100)
→ expect ~5 notifications/sec
```

### Deadband

```text
subscribe(publishingInterval=500)
monitor(Dynamic/SineWave, samplingInterval=200,
        filter = DataChangeFilter(deadband=Absolute, value=0.5))
→ notifications only when |Δvalue| > 0.5
→ over a 10 s period (one cycle), expect ~4 notifications
```

### Status-only trigger

```text
monitor(Dynamic/StatusVariable, filter = DataChangeFilter(trigger=Status))
→ notification only when status code changes (~every 3 s)
```

Compare to `trigger=StatusValue` (every value or status change)
and `trigger=Value` (only value changes — fewer notifications).

## Note on update intervals

The `FastCounter` (100 ms) is the most aggressive cadence in the
suite. For higher rates, fork and adjust `DynamicBuilder.cs` —
see [Customization](../customization/forking-and-adding-nodes.md).

## Where to read next

- [Events](./events.md) — event-style notifications.
- [Historical data](./historical-data.md) — recorded time-series.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  full test recipes.

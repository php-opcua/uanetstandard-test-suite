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

| BrowseName     | Type    | Update interval | Behaviour                                                            |
| -------------- | ------- | --------------- | -------------------------------------------------------------------- |
| `Counter`       | UInt32 | 1 s             | Initial value `0`; first observed value is `1` (incremented before publish) |
| `FastCounter`   | UInt32 | 100 ms          | Initial value `0`; first observed value is `1`                       |
| `SlowCounter`   | UInt32 | 10 s            | Initial value `0`; first observed value is `1`                       |

Use cases:

- `Counter` — basic subscription verification (1 notification/sec).
- `FastCounter` — sampling-interval / publishing-interval ratio
  tests. With `publishingInterval=200ms`, expect every-other
  update.
- `SlowCounter` — keep-alive and long-publishing-interval tests.

## Random values

| BrowseName       | Type    | Update interval | Range / values                                                  |
| ---------------- | ------- | --------------- | --------------------------------------------------------------- |
| `Random`         | Double  | 500 ms          | `[0.0, 1.0)`                                                    |
| `RandomInt`      | Int32   | 1 s             | `[-1000, 1000]` (inclusive)                                     |
| `RandomString`   | String  | 1 s             | One of `["alpha", "bravo", "charlie", "delta", "echo", "foxtrot"]` |

Useful for:

- Verifying every notification carries a different value
  (deadband-vs-no-deadband tests).
- Stressing the encoder with frequent String allocations
  (`RandomString`).

## Waveforms

| BrowseName     | Type    | Period | Formula                                                |
| -------------- | ------- | ------ | ------------------------------------------------------ |
| `SineWave`     | Double  | 10 s   | `sin(2π · t / 10)`, range `[-1, 1]`                    |
| `SawTooth`     | Double  | 10 s   | `(t % 10) / 10`, range `[0, 1)`                        |
| `Square`        | Boolean | 10 s   | `(int)(t / 5) % 2 == 0` — flips every 5 s              |
| `TriangleWave` | Double  | 10 s   | Linear ramp `0 → 1 → 0` over the 10 s period           |

Waveforms are written to the address space by the **500 ms timer**
in `DynamicBuilder.cs`; they are not recomputed on read. Each
read returns whatever value the last tick wrote. The deterministic
period for every waveform is 10 s — the timer ticks twice per
second, sampling the formula and pushing the new value into the
node, so subscriptions with shorter `samplingInterval` than 500 ms
will still only see two distinct values per second.

## Timestamps

| BrowseName    | Type      | Update interval | Behaviour                                       |
| ------------- | --------- | --------------- | ----------------------------------------------- |
| `Timestamp`   | DateTime  | 1 s             | The 1 s timer writes `DateTime.UtcNow` into the value |

`Timestamp` is not recomputed per read — it is refreshed by the
1 s timer like the counters. Two reads less than ~1 s apart will
typically see the same value. Useful to verify that:

- `sourceTimestamp` in the DataValue tracks the value (the server
  sets it).
- The value visibly progresses across the second boundary.

## Status cycling

| BrowseName       | Type        | Update interval | Behaviour                                                       |
| ---------------- | ----------- | --------------- | ---------------------------------------------------------------- |
| `StatusVariable` | StatusCode  | 1 s             | Picks one of `Good`, `Uncertain`, `Bad` uniformly at random per tick |

`StatusVariable` is **not** a deterministic three-state cycle — every
tick draws independently from the three top-level status codes.
Across a long enough window you will see all three, but not in a
fixed order. Use it to verify your client interprets:

- `Good` (`0x00xxxxxx`) — normal value
- `Bad` (`0x80xxxxxx`) — error
- `Uncertain` (`0x40xxxxxx`) — value may be stale / unreliable

Useful for testing data-change triggers with
`DataChangeTrigger = StatusValue` vs `Status` vs `Value` —
each filter produces a different notification count.

## Nullable value

| BrowseName        | Type    | Update interval | Behaviour                                                  |
| ----------------- | ------- | --------------- | ---------------------------------------------------------- |
| `NullableDouble`  | Double  | 500 ms          | ~20% of ticks set `StatusCode = Bad_NoData`; otherwise `Good` with value `rng.NextDouble() * 100` |

Tests how your client handles values that intermittently have
**no data**. The choice is probabilistic, not deterministic
alternation — over a long enough sample you should see ~1
`Bad_NoData` per 5 updates. When status is `Bad_NoData` the value
field is left at whatever the previous tick wrote — your client
should treat it as absent rather than as a real number.

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

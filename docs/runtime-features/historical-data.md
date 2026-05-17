---
eyebrow: 'Docs · Runtime features'
lede:    'Four historized variables, a 1-second recording interval, and HistoryRead support for raw, processed, and at-time queries. The history-test surface.'

see_also:
  - { href: './dynamic-variables.md',                       meta: '4 min' }
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }

prev: { label: 'Alarms',  href: './alarms.md' }
next: { label: 'Views',   href: '../special-features/views.md' }
---

# Historical data

Path: `TestServer / Historical`

Four variables with `accessLevel = CurrentRead + HistoryRead`.
The server records samples in-memory at 1 second intervals,
buffering up to 10 000 samples per variable (rolling).

## The variables

| BrowseName              | Type     | Pattern                                                                |
| ----------------------- | -------- | ---------------------------------------------------------------------- |
| `HistoricalTemperature` | Double   | `25 + 10·sin(t / 30) + (rand·2 - 1)` — roughly `[14, 36]`              |
| `HistoricalPressure`    | Double   | `1013 + 20·cos(t / 45) + (rand·3 - 1.5)` — roughly `[992, 1034]`       |
| `HistoricalCounter`     | UInt32   | Increments by 1 every second; the **first** historized value is `1`     |
| `HistoricalBoolean`     | Boolean  | Toggles every ~5 s — deterministic, follows `(int)(t / 5) % 2 == 0`     |

`t` is the elapsed seconds component of `DateTime.UtcNow.TimeOfDay`.
All four are read-only (`R + HR`). `HistoricalCounter` initialises
to `0` but is incremented before the first sample is written to
history, so the first historized value is `1`, not `0`.

## Recording

| Setting           | Value     |
| ----------------- | --------- |
| Sample interval   | 1 000 ms   |
| Buffer size       | 10 000 samples / variable |
| Storage           | In-memory  |
| Persistence       | None — buffer resets on restart |

After ~2.8 hours of uptime (`10000 / 3600`), the oldest samples
start being overwritten.

## HistoryRead operations supported

| Operation                  | Status in this server                                       |
| -------------------------- | ----------------------------------------------------------- |
| `ReadRawModifiedDetails`   | **Implemented** by `TestNodeManager.HistoryReadRawModified`. |
| `ReadProcessedDetails`     | **Not implemented.** Falls through to the UA-.NETStandard base class, which returns `Bad_HistoryOperationUnsupported`. |
| `ReadAtTimeDetails`        | **Not implemented.** Same as above — `Bad_HistoryOperationUnsupported`. |
| `ReadEventDetails`         | **Not implemented.** No event history is recorded.           |

Only the raw read path is exercised end-to-end. If your test
matrix expects aggregates or at-time interpolation, the suite is
not the right surface for it today.

## ReadRawModifiedDetails

Inputs:

| Param               | Notes                                              |
| ------------------- | -------------------------------------------------- |
| `StartTime`         | Beginning of time range                            |
| `EndTime`           | End of time range                                  |
| `NumValuesPerNode`  | Cap on returned values; `0` = no cap                |
| `ReturnBounds`      | If true, include bounding values just outside range |
| `IsReadModified`    | Not applicable (suite has no modify history)       |

Returns: array of `DataValue` with `Value`, `StatusCode`,
`SourceTimestamp`, `ServerTimestamp`.

### Continuation

If `NumValuesPerNode` is smaller than the result set, the
response includes a `ContinuationPoint`. Repeated calls with
the continuation point fetch successive pages.

## ReadProcessedDetails

Not implemented in this server. Calling it returns
`Bad_HistoryOperationUnsupported`. The aggregates listed in
`AggregateFunctions` (Average, Minimum, Maximum, Count, ...) are
the standard NodeIds advertised by the stack, not invocable
endpoints in this build.

## ReadAtTimeDetails

Not implemented. Calling it returns
`Bad_HistoryOperationUnsupported`. There is no server-side
interpolation between recorded samples.

## Test patterns

### Basic raw read

1. Wait at least 10 s after server start (for history to
   accumulate).
2. `ReadRawModifiedDetails(HistoricalTemperature, t-10s, t, NumValuesPerNode=100)`.
3. Expect ~10 samples, each 1 s apart.
4. Each value is `22 + ~8·sin(...) + noise` — broadly in the
   range `[10, 35]`.

### Counter monotonicity

`HistoricalCounter` starts at `0` in the live value but the
first sample written to the history store is `1` (the counter is
incremented before each insert). A 30-second range over a
freshly started server returns `[1, 2, …, 30]`; over a long-
running server it returns `[k, k+1, …, k+29]` for some `k ≥ 1`.

### Boolean history

`HistoricalBoolean` toggles every ~5 s on a deterministic
schedule (`(int)(t/5) % 2 == 0`). A 30-s read returns ~30
samples grouped in runs of 5 identical values, not a random mix.

### Aggregates (not supported)

Calling `ReadProcessedDetails` returns
`Bad_HistoryOperationUnsupported` — see the operations table
above. If your client needs average/min/max, compute them from
the raw samples after a `ReadRawModifiedDetails` call.

### Continuation

```text
ReadRawModifiedDetails(HistoricalCounter, ..., NumValuesPerNode = 5)
→ 5 values + a ContinuationPoint

ReadRawModifiedDetails(..., ContinuationPoint=<from above>)
→ next 5 values
```

Repeat until the continuation point is `null`.

### Time-range filtering

```text
read t to t+10s → 10 values
read t to t+100s → 100 values
```

Sanity-check that range bounds are honoured exactly.

## Bounding values

The current `HistoryReadRawModified` implementation just filters
the in-memory list by `SourceTimestamp` between `StartTime` and
`EndTime` (inclusive on both sides) and sorts ascending. It does
**not** specifically inject bounding values when
`ReturnBounds=true` — the request flag is ignored by the custom
handler. If you need bounding values for plotting, pad the
requested time range by one sample-interval on each side.

## Recording behaviour

The recording loop runs **server-side, every 1 000 ms**, sampling
the current value of each historical variable. The buffer is a
ring — at sample 10 001, sample 1 is discarded.

The 10 000-sample cap is the `maxHistorySize` constant inside
`HistoricalBuilder.cs`. It is **not** environment-driven —
there is no `OPCUA_HISTORY_MAX_SAMPLES` variable. Changing the
cap requires editing the source.

## Where to read next

- [Views](../special-features/views.md) — filtered browses.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  recipes covering historical reads.

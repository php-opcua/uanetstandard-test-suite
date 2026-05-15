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

| BrowseName              | Type     | Pattern                                   |
| ----------------------- | -------- | ----------------------------------------- |
| `HistoricalTemperature` | Double   | `22 + 8·sin(t/60) + noise`                |
| `HistoricalPressure`    | Double   | `101.325 + 5·cos(t/120) + noise`          |
| `HistoricalCounter`     | UInt32   | Increments by 1 every second              |
| `HistoricalBoolean`     | Boolean  | Random `true`/`false`                     |

All four are read-only (`R+HR`).

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

| Operation                  | Use                                            |
| -------------------------- | ---------------------------------------------- |
| `ReadRawModifiedDetails`   | Raw samples within a time range                |
| `ReadProcessedDetails`     | Aggregates (average, min, max, count) over intervals |
| `ReadAtTimeDetails`        | Interpolated value at specific timestamps      |

`ReadEventDetails` (for event history) is **not** implemented —
no event history is recorded.

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

Aggregates server-side. Inputs:

| Param                 | Notes                                       |
| --------------------- | ------------------------------------------- |
| `StartTime` / `EndTime` | Time range                                |
| `ProcessingInterval`  | Bucket size in ms                          |
| `AggregateType`       | NodeId of the aggregate function           |

Supported aggregate functions (subset of standard):

- `Average`
- `Minimum`
- `Maximum`
- `Count`

Each `processingInterval`-sized bucket gets one returned value.

## ReadAtTimeDetails

Inputs:

| Param            | Notes                                        |
| ---------------- | -------------------------------------------- |
| `ReqTimes`       | Array of timestamps                          |
| `UseSimpleBounds` | Interpolation behaviour                      |

Returns interpolated values at the requested timestamps. For
in-between-sample times, the server linearly interpolates (or
returns the simple-bound depending on the flag).

## Test patterns

### Basic raw read

1. Wait at least 10 s after server start (for history to
   accumulate).
2. `ReadRawModifiedDetails(HistoricalTemperature, t-10s, t, NumValuesPerNode=100)`.
3. Expect ~10 samples, each 1 s apart.
4. Each value is `22 + ~8·sin(...) + noise` — broadly in the
   range `[10, 35]`.

### Counter monotonicity

`HistoricalCounter` starts at 0 and ticks up by 1 each second.
A 30-second range should return values `[k, k+1, ..., k+29]` for
some starting `k`.

### Boolean history

`HistoricalBoolean` flips randomly. A 30-s read returns ~30
samples with random `true`/`false`.

### Aggregates

```text
ReadProcessedDetails(
  HistoricalTemperature,
  startTime = t - 5 min,
  endTime   = t,
  processingInterval = 60000,    # 1 min buckets
  aggregateType = Average,
)
→ 5 returned values, one per minute, each ≈ 22 (with small variance)
```

Compare `Minimum` and `Maximum` for the same range — they should
straddle the average.

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

With `ReturnBounds=true`:

- The first returned value has `sourceTimestamp < StartTime`
  (the last value before the range).
- The last has `sourceTimestamp > EndTime` (the first value
  after the range).

Useful for plotting — you get the values that bound the chart
window for proper interpolation at the edges.

## Recording behaviour

The recording loop runs **server-side, every 1 000 ms**, sampling
the current value of each historical variable. The buffer is a
ring — at sample 10 001, sample 1 is discarded.

The 10 000-sample cap is `OPCUA_HISTORY_MAX_SAMPLES` (default
10 000) — adjustable via env var if you need more or less.

## Where to read next

- [Views](../special-features/views.md) — filtered browses.
- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  recipes covering historical reads.

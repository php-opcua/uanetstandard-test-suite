# Historical Data

Path: `Objects > TestServer > Historical`

4 variables with historical data access enabled. The server records value changes in memory (up to 10,000 samples per variable) and serves them via the OPC UA HistoryRead service.

## Historical Variables

| BrowseName | DataType | Access | Update Rate | Value Pattern |
|---|---|---|---|---|
| `HistoricalTemperature` | Double | R + HR | 1 second | `22 + 8 * sin(t/60) + noise` -- simulated temperature oscillation |
| `HistoricalPressure` | Double | R + HR | 1 second | `101.325 + 5 * cos(t/120) + noise` -- simulated pressure |
| `HistoricalCounter` | UInt32 | R + HR | 1 second | Increments by 1 every second |
| `HistoricalBoolean` | Boolean | R + HR | 1 second | Random `true`/`false` every second |

### Recording Interval

Historical data is recorded at **1000ms** (1 second) intervals.

### Access Levels

These variables have:
- `accessLevel = CurrentRead | HistoryRead`
- `userAccessLevel = CurrentRead | HistoryRead`

The `HistoryRead` flag indicates that historical data is available.

### Storage

- **Maximum samples:** 10,000 per variable (circular buffer -- oldest samples are discarded)
- **Storage type:** In-memory (not persisted across restarts)
- **Sample rate:** 1 sample/second for all variables (1000ms recording interval)

After the server has been running for ~2.8 hours, the buffer is full and oldest samples start being overwritten.

## Supported HistoryRead Operations

### ReadRawModifiedDetails

Read raw historical values within a time range.

**Parameters:**
- `StartTime` -- beginning of the time range
- `EndTime` -- end of the time range
- `NumValuesPerNode` -- maximum number of values to return (0 = no limit)
- `ReturnBounds` -- whether to include bounding values
- `IsReadModified` -- read modified values (not applicable here)

**Example request:**
```
StartTime: 5 minutes ago
EndTime: now
NumValuesPerNode: 100
```
Returns up to 100 raw samples from the last 5 minutes, each with timestamp and value.

### ReadProcessedDetails

Read aggregated historical values (e.g., average, min, max over intervals).

**Parameters:**
- `StartTime` / `EndTime` -- time range
- `ProcessingInterval` -- aggregation interval in milliseconds
- `AggregateType` -- NodeId of the aggregate function

**Common aggregates:**
- Average
- Minimum
- Maximum
- Count

### ReadAtTimeDetails

Read the interpolated value at specific timestamps.

**Parameters:**
- `ReqTimes` -- array of timestamps to read values at
- `UseSimpleBounds` -- interpolation method

## Testing Historical Data

### Basic history read

1. Connect to the server and wait at least 10 seconds (so history accumulates)
2. Read `HistoricalTemperature` with `ReadRawModifiedDetails`:
   - `StartTime` = 10 seconds ago
   - `EndTime` = now
3. Expect ~10 samples, each approximately 1 second apart
4. Values should be around 22 with +/-8 oscillation

### Pagination

1. Set `NumValuesPerNode = 5`
2. Read history -- should return 5 values plus a `ContinuationPoint`
3. Call HistoryRead again with the `ContinuationPoint` to get the next batch
4. Repeat until `ContinuationPoint` is null

### Time range filtering

1. Read `HistoricalCounter` from startup to now
2. Values should be sequential: 0, 1, 2, 3, ...
3. Read a sub-range and verify the values match the expected range

### Boolean history

1. Read `HistoricalBoolean` history
2. Values should be a random sequence of `true`/`false`
3. Each sample should have a unique timestamp ~1 second apart

### Aggregates (if supported by your client)

1. Read `HistoricalTemperature` with `ReadProcessedDetails`:
   - `ProcessingInterval = 60000` (1 minute)
   - `AggregateType = Average`
2. Each returned value should be the average temperature over that minute
3. Compare with `Minimum` and `Maximum` aggregates

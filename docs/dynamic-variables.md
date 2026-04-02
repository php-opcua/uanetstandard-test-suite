# Dynamic Variables

Path: `Objects > TestServer > Dynamic`

13 variables whose values change over time. These are essential for testing subscriptions, monitored items, and data change notifications.

All dynamic variables are **read-only**.

## Variable Reference

### Counters

| BrowseName | DataType | Update Interval | Behavior |
|---|---|---|---|
| `Counter` | UInt32 | 1 second | Increments by 1 every second. Starts at 0. |
| `FastCounter` | UInt32 | 100 ms | Increments by 1 every 100ms. Starts at 0. |
| `SlowCounter` | UInt32 | 10 seconds | Increments by 1 every 10 seconds. Starts at 0. |

**Testing notes:**
- `FastCounter` is ideal for testing subscription sampling intervals
- Create a subscription with 200ms publishing interval, monitor `FastCounter` -- you should see every other update
- `SlowCounter` is useful for testing longer publishing intervals and keep-alive

### Random Values

| BrowseName | DataType | Update Interval | Behavior |
|---|---|---|---|
| `Random` | Double | 500 ms | Random value in `[0.0, 1.0)` |
| `RandomInt` | Int32 | 1 second | Random integer in `[-1000, 1000]` |
| `RandomString` | String | 2 seconds | Random alphanumeric string, 8-24 characters |

**Testing notes:**
- Every data change notification should have a different value
- Useful for testing deadband filtering (set a deadband on `Random` and verify fewer notifications)

### Waveforms

| BrowseName | DataType | Period | Formula |
|---|---|---|---|
| `SineWave` | Double | 10 seconds | `sin(2 * PI * t / 10)` -- smooth oscillation between -1 and 1 |
| `SawTooth` | Double | 5 seconds | `(t % 5) / 5` -- linear ramp from 0 to 1, then resets |
| `Square` | Boolean | 2 seconds | `true` for 1s, `false` for 1s, repeating |
| `TriangleWave` | Double | 8 seconds | Linear ramp from -1 to 1 to -1, period 8s |

**Testing notes:**
- Waveforms are computed on every read (no timer), so they are always current
- `SineWave` is good for testing analog deadband filtering
- `Square` is good for testing boolean data change detection

### Timestamps

| BrowseName | DataType | Behavior |
|---|---|---|
| `Timestamp` | DateTime | Returns the current server time on every read |

**Testing notes:**
- The value changes on every single read, making it ideal for verifying that your client receives fresh timestamps
- Source timestamp in the DataValue should closely match the value itself

### Status Cycling

| BrowseName | DataType | Update Interval | Behavior |
|---|---|---|---|
| `StatusVariable` | StatusCode | 3 seconds | Cycles through: `Good` -> `BadCommunicationError` -> `UncertainLastUsableValue` -> repeat |

**Testing notes:**
- Verify your client correctly interprets different status code classes:
  - `Good` (0x00xxxxxx)
  - `Bad` (0x80xxxxxx) -- indicates an error
  - `Uncertain` (0x40xxxxxx) -- value may not be reliable
- Test data change filtering with status change triggers

### Nullable Value

| BrowseName | DataType | Update Interval | Behavior |
|---|---|---|---|
| `NullableDouble` | Double | 4 seconds | Alternates between a valid Double value (`StatusCode = Good`) and `StatusCode = BadNoData` |

**Testing notes:**
- When status is `Good`: value is a random double in `[0, 100)`
- When status is `BadNoData`: value is `0` but should be treated as unavailable
- Tests how your client handles variables that intermittently have no data

## Subscription Testing Guide

### Basic subscription

1. Create a subscription with `publishingInterval = 1000` (1 second)
2. Add `Counter` as a monitored item with `samplingInterval = 1000`
3. Expect one data change notification per second, values incrementing by 1

### Fast sampling

1. Create a subscription with `publishingInterval = 200`
2. Monitor `FastCounter` with `samplingInterval = 100`
3. Expect ~5 notifications per second

### Deadband filtering

1. Monitor `SineWave` with an absolute deadband of `0.5`
2. Notifications should only arrive when the value changes by more than 0.5
3. With a period of 10s and deadband of 0.5, expect ~4 notifications per period

### Status change trigger

1. Monitor `StatusVariable` with `DataChangeTrigger = StatusValue` (trigger on status or value change)
2. Expect notification every 3 seconds as the status cycles
3. Change to `DataChangeTrigger = Status` to only trigger on status changes

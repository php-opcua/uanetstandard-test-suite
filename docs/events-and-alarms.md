# Events & Alarms

## Events

Path: `Objects > TestServer > Events`

### Custom Event Types

The server defines 3 custom event types in namespace `ns=1`:

#### SimpleEventType

Inherits from: `BaseEventType`

| Property | DataType | Description |
|---|---|---|
| `EventPayload` | String | Custom payload string |

Plus all inherited fields: `EventId`, `EventType`, `SourceNode`, `SourceName`, `Time`, `ReceiveTime`, `Message`, `Severity`.

#### ComplexEventType

Inherits from: `BaseEventType`

| Property | DataType | Description |
|---|---|---|
| `Source` | String | Source identifier |
| `Category` | String | Event category |
| `NumericValue` | Double | Associated numeric value |

#### SystemStatusEventType

Inherits from: `BaseEventType`

| Property | DataType | Description |
|---|---|---|
| `SystemState` | String | One of: `"Running"`, `"Idle"`, `"Busy"`, `"Maintenance"` |
| `CpuUsage` | Double | Simulated CPU usage (0-100) |
| `MemoryUsage` | Double | Simulated memory usage (40-90) |

### Event Emitter

| BrowsePath | Type | EventNotifier |
|---|---|---|
| `Events/EventEmitter` | Object | `1` (SubscribeToEvents) |

The `EventEmitter` object is linked to the `Server` object as an event source. To receive events, subscribe to events on either the `EventEmitter` node or the `Server` node.

### Automatic Events

The server generates events automatically at fixed intervals:

| Event Type | Interval | Severity | Message Pattern |
|---|---|---|---|
| `SimpleEventType` | Every 2 seconds | 200 | `"Periodic event #N"` (N increments) |
| `ComplexEventType` | Every 5 seconds | 300 | `"Complex event #N"` |
| `SystemStatusEventType` | Every 10 seconds | 100 (600 for Maintenance) | `"System status: <state>"` |

### On-Demand Events

Call the `Methods/GenerateEvent` method to raise a `BaseEventType` event with custom message and severity. See [Methods](methods.md).

### Testing Events

#### Basic event subscription

1. Create a subscription on the `Server` node (or `EventEmitter`)
2. Add an event monitored item with a `SimpleAttributeOperand` select clause:
   - `EventId` (ByteString)
   - `EventType` (NodeId)
   - `SourceName` (String)
   - `Time` (DateTime)
   - `Message` (LocalizedText)
   - `Severity` (UInt16)
3. Wait 2 seconds -- you should receive a `SimpleEventType` event

#### Filtering events by type

Use a `ContentFilter` with `OfType` operator:
- Filter for `SimpleEventType` -> receive events every 2s
- Filter for `SystemStatusEventType` -> receive events every 10s

#### Filtering by severity

Use a `ContentFilter` where `Severity >= 500`:
- Only `SystemStatusEventType` events during `Maintenance` state (severity 600) should pass

#### Reading custom properties

For `SimpleEventType`, include `EventPayload` in the select clause:
```
SelectClause: [Message, Severity, EventPayload]
```
The `EventPayload` field contains `"payload-N"` where N matches the event counter.

---

## Alarms

Path: `Objects > TestServer > Alarms`

### Alarm Source Variables

Two variables drive the alarms:

| BrowsePath | DataType | Access | Behavior |
|---|---|---|---|
| `Alarms/AlarmSourceValue` | Double | RW | Oscillates as `50 + 60 * sin(t)` every 1 second. Range: approximately -10 to 110. |
| `Alarms/OffNormalSource` | Boolean | RW | Toggles every 20 seconds |

You can write to these variables to manually trigger alarm states.

### Alarm Instances

#### HighTemperatureAlarm (ExclusiveLimitAlarm)

| Property | Value |
|---|---|
| Type | `ExclusiveLimitAlarmType` |
| Source | `AlarmSourceValue` |
| LowLow Limit | 5 |
| Low Limit | 20 |
| High Limit | 70 |
| HighHigh Limit | 90 |

**States based on AlarmSourceValue:**

| Value Range | Active State |
|---|---|
| value > 90 | HighHigh (critical) |
| 70 < value <= 90 | High |
| 20 <= value <= 70 | Normal (inactive) |
| 5 <= value < 20 | Low |
| value < 5 | LowLow (critical) |

Only one state is active at a time (exclusive).

#### LevelAlarm (NonExclusiveLimitAlarm)

| Property | Value |
|---|---|
| Type | `NonExclusiveLimitAlarmType` |
| Source | `AlarmSourceValue` |
| LowLow Limit | 0 |
| Low Limit | 15 |
| High Limit | 75 |
| HighHigh Limit | 95 |

Multiple states can be active simultaneously (non-exclusive). For example, if the value is 96, both `High` and `HighHigh` states are active.

#### OffNormalAlarm

| Property | Value |
|---|---|
| Type | `OffNormalAlarmType` |
| Source | `OffNormalSource` |
| Normal State | matches `OffNormalSource` |

Active when `OffNormalSource` deviates from its normal state.

### Alarm Operations

All three alarms support:

| Operation | Description |
|---|---|
| **Acknowledge** | Client acknowledges the alarm condition |
| **Confirm** | Client confirms the alarm (after acknowledge) |
| **AddComment** | Client adds a comment to the alarm |

### Testing Alarms

#### Monitoring alarm state changes

1. Subscribe to events on the `Alarms` folder or `Server` node
2. Wait for `AlarmSourceValue` to oscillate through the limit thresholds
3. Verify you receive `ConditionType` events with changing states

#### Manually triggering alarms

1. Write `100.0` to `Alarms/AlarmSourceValue` -> triggers HighHigh on both alarms
2. Write `50.0` -> returns to normal
3. Write `-5.0` -> triggers LowLow on HighTemperatureAlarm, Low + LowLow on LevelAlarm

#### Acknowledging an alarm

1. Wait for an alarm to become active
2. Read the alarm's `EventId`
3. Call the `Acknowledge` method with the `EventId` and a comment
4. Verify the `AckedState` changes to `true`

#### Testing OffNormalAlarm

1. Write `true` to `Alarms/OffNormalSource`
2. The alarm should activate
3. Write `false` -> alarm deactivates

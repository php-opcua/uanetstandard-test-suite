---
eyebrow: 'Docs · Testing patterns'
lede:    'Subscription, monitored-item, and method-call test recipes. Including the event + alarm flows that exercise the publish loop.'

see_also:
  - { href: '../runtime-features/dynamic-variables.md',  meta: '4 min' }
  - { href: '../runtime-features/methods.md',            meta: '5 min' }
  - { href: '../runtime-features/events.md',             meta: '4 min' }

prev: { label: 'Basic tests',  href: './basic-tests.md' }
next: { label: 'Security tests', href: './security-tests.md' }
---

# Subscription and method tests

The recipes for the publish/subscribe surface and the method-call
service.

## Subscription tests

### Basic 1-Hz subscription

```text
create_subscription(publishingInterval=1000)
monitor(Dynamic/Counter, samplingInterval=1000)

→ one DataChange notification per second
→ values strictly increasing by 1
```

Run for ~5 seconds, verify 4-5 notifications with sequential
values.

### Fast subscription

```text
create_subscription(publishingInterval=200)
monitor(Dynamic/FastCounter, samplingInterval=100)

→ ~5 notifications per second
```

The publishing interval is the upper bound on notification rate
— a 200 ms `publishingInterval` means at most 5/sec.

### Multiple monitored items

```text
create_subscription(publishingInterval=1000)
monitor(Dynamic/Counter)
monitor(Dynamic/SineWave)
monitor(Dynamic/StatusVariable)

→ notifications for all three, interleaved in each publish
```

Each publish response carries the latest value for every
monitored item that changed.

### Deadband filtering

```text
monitor(Dynamic/SineWave, filter=DataChangeFilter(
    trigger = StatusValue,
    deadband: { type: Absolute, value: 0.5 },
))

→ notifications only when the value changes by > 0.5
→ over one sine period (~10s), expect ~4 notifications
```

### Status-change trigger

```text
monitor(Dynamic/StatusVariable, filter=DataChangeFilter(
    trigger = Status,  # not StatusValue, not Value
))

→ notification only when status code changes
→ at 3-second cadence (the cycle interval)
```

### Subscription with publishing disabled

```text
create_subscription(publishingInterval=1000, publishingEnabled=false)
monitor(Dynamic/Counter)

→ no notifications

publish_enabled(subscriptionId, true)
→ notifications resume
```

Useful to test publishing-state machinery in your client.

### Subscription teardown

```text
delete_subscription(subscriptionId)

→ DeleteSubscriptionsResponse status: Good
→ further publish responses for this id should not arrive
```

## Method-call tests

### Simple call

```text
call(
  objectId = ns=1;s=TestServer/Methods,
  methodId = ns=1;s=TestServer/Methods/Add,
  inputs = [2.5, 3.5],
)

→ status = Good
→ outputs = [6.0]
```

### Method with no inputs

```text
call(TestServer/Methods, TestServer/Methods/GetServerTime, inputs=[])

→ outputs = [DateTime close to now]
```

### Failing method

```text
call(TestServer/Methods, TestServer/Methods/Failing, inputs=[])

→ status = Bad_InternalError
```

The OPC UA call wire-completes successfully; the **result
status** is Bad. Your client should distinguish wire failure
(transport / encoding error) from result-status failure.

### Method timeout

```text
client.timeout = 5000  # 5 seconds
call(TestServer/Methods/LongRunning, inputs=[10000])  # 10s

→ client raises timeout
```

Useful to test cancellation / abort flows.

### Array argument

```text
call(TestServer/Methods/ArraySum, inputs=[[1.0, 2.0, 3.0, 4.0]])

→ outputs = [10.0]
```

### Echo round-trip

For every OPC UA type:

```text
sample = make_test_value(T)
call(TestServer/Methods/Echo, inputs=[sample])
→ outputs = [sample]
```

Tests that Variant encoding round-trips correctly for every type.

### Invalid argument

```text
call(TestServer/Methods/MatrixTranspose, inputs=[[1,2,3], 2, 2])
  # 3 elements but 2*2 = 4 expected

→ status = Bad_InternalError
```

The handler does not validate `matrix.Length == rows * cols`,
so the mismatched length raises `IndexOutOfRangeException`
inside the method body. The generic wrapper in
`TestNodeManager.CreateMethod` catches every exception and
returns `Bad_InternalError` — not `Bad_InvalidArgument`.

## Event subscription tests

### Subscribe to periodic events

```text
create_subscription(publishingInterval=1000)
monitor(Server, EventFilter(
    selectClause = [EventId, EventType, Message, Severity, Time],
))

→ wait 3s → at least one event from the 2 s timer arrives
  (Message = "Simple event #N", EventType = BaseEventType)
```

### Filter by event type

The suite does not register any custom event types — all three
periodic timers emit `BaseEventState` instances stamped with
either `ObjectTypeIds.BaseEventType` or
`ObjectTypeIds.SystemEventType`. Use those standard NodeIds:

```text
filter = EventFilter(
    selectClause = [...],
    whereClause  = OfType(BaseEventType),
)
→ events from the 2 s and 5 s timers (both stamped BaseEventType)

filter = EventFilter(
    selectClause = [...],
    whereClause  = OfType(SystemEventType),
)
→ events from the 10 s "System status" timer
```

### Severity filter

```text
filter = EventFilter(
    selectClause = [...],
    whereClause  = Severity >= 300,
)

→ only the 5 s "Complex event" timer (Severity = 500); the
  2 s timer (Severity = 200) and 10 s timer (Severity = 100)
  are filtered out
```

### On-demand event via method (no-op stub)

```text
# Setup subscription
subscribe(Server, EventFilter(...))

# Call the stub
call(TestServer/Methods/GenerateEvent, inputs=["test event", 750])

→ method returns Good
→ NO event arrives. The handler only writes a console line on
  the server (`Console.WriteLine`) — it does not call
  ReportEvent. There is no on-demand event-emission path in
  the current build.
```

### Custom property in selectClause

There are no custom event-type properties in this server. A
`selectClause` like
`[Message, Severity, EventId, SourceNode, ReceiveTime, …]`
returns only the standard `BaseEventType` fields. Trying to
select a non-existent path such as `EventPayload` yields a null
value for that field.

## Alarm subscription tests

### Watch transitions

```text
subscribe(Server, EventFilter(
    selectClause = [
        EventId, SourceName, Severity, Time,
        ActiveState/Id, AckedState/Id,
        ConditionType
    ],
))

# AlarmSourceValue oscillates ~50 + 60·sin(t)
# Over a 10s window, alarms transition naturally

→ multiple ConditionType events with transitioning states
```

### Force a specific state

```text
write(Alarms/AlarmSourceValue, 100.0)
→ within publishingInterval, alarm event arrives with
  ActiveState/Id=true, severity reflecting HighHigh
```

### Acknowledge round-trip

```text
# 1. Force alarm
write(Alarms/AlarmSourceValue, 100.0)
# 2. Wait for event, capture EventId
event_id = ...
# 3. Acknowledge
call(<alarm>, Acknowledge, inputs=[event_id, "investigating"])
→ status = Good
# 4. Refresh — AckedState now true
```

### OffNormal toggle

```text
write(Alarms/OffNormalSource, true)
→ event: OffNormalAlarm ActiveState=true

write(Alarms/OffNormalSource, false)
→ event: OffNormalAlarm ActiveState=false
```

## Historical data tests

### Raw read

```text
# Wait at least 10s after server start
read_history_raw(
    Historical/HistoricalTemperature,
    startTime = now - 10s,
    endTime   = now,
    maxValues = 100,
)

→ ~10 samples, ~1s apart, values around 22 ± 8
```

### Continuation

```text
read_history_raw(
    Historical/HistoricalCounter,
    startTime = now - 30s,
    endTime   = now,
    maxValues = 5,
)

→ 5 values + ContinuationPoint

read_history_raw(..., continuation=<from above>)
→ next 5 values
```

### Aggregate

```text
read_history_processed(
    Historical/HistoricalTemperature,
    startTime    = now - 5min,
    endTime      = now,
    interval     = 60_000,  # 1 minute
    aggregateId  = AggregateFunction.Average,
)

→ 5 buckets, each ≈ 22 (with small variance)
```

## Where to read next

- [Security tests](./security-tests.md) — cert and auth recipes.
- The per-feature pages under **Runtime features** for deeper
  reference.

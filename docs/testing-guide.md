# Testing Guide

Practical test scenarios organized by OPC UA feature. Each scenario lists the server to use, what to do, and what to expect.

## 1. Connection & Session

### Basic connection (no security)

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Connect to `opc.tcp://localhost:4840/UA/TestServer`
  2. Create a session (anonymous)
  3. Browse root -> Objects -> TestServer
- **Expect:** Session created, browse returns TestServer with sub-folders

### Encrypted connection

- **Server:** `opcua-userpass` (4841)
- **Steps:**
  1. Discover endpoints via `GetEndpoints()`
  2. Select the Basic256Sha256/SignAndEncrypt endpoint
  3. Present client certificate and key
  4. Create session with username `admin` / `admin123`
- **Expect:** Encrypted session, full access

### Connection rejection

- **Server:** `opcua-userpass` (4841)
- **Steps:**
  1. Try connecting without credentials (anonymous)
- **Expect:** `BadIdentityTokenRejected`

---

## 2. Reading & Writing

### Read all data types

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Read all 21 variables under `DataTypes/Scalar`
  2. Verify each value matches the documented initial value
  3. Verify each `DataType` attribute matches the expected type
- **Expect:** All reads succeed with `Good` status

### Write and read-back

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Write `false` to `DataTypes/Scalar/BooleanValue`
  2. Read it back
  3. Verify value is `false`
  4. Repeat for each data type with an appropriate test value
- **Expect:** All writes succeed, read-back matches written value

### Write to read-only variable

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Write `42` to `DataTypes/ReadOnly/Int32_RO`
- **Expect:** `BadNotWritable` status code

### Batch read

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Build a read request with all 21 scalar variables
  2. Send as a single Read service call
- **Expect:** 21 results, all `Good`

### Array operations

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Read `DataTypes/Array/Int32Array` -> `[-100000, -1, 0, 1, 100000]`
  2. Write `[1, 2, 3]` (shorter array)
  3. Read back -> `[1, 2, 3]`
  4. Read an empty array -> `[]`
  5. Write elements to an empty array, read back

---

## 3. Subscriptions & Monitored Items

### Basic data change subscription

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Create subscription (`publishingInterval = 1000`)
  2. Add `Dynamic/Counter` as monitored item
  3. Wait for notifications
- **Expect:** One notification per second, value incrementing by 1

### Fast data changes

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Create subscription (`publishingInterval = 200`)
  2. Monitor `Dynamic/FastCounter` with `samplingInterval = 100`
- **Expect:** ~5 notifications/second

### Deadband filtering

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Monitor `Dynamic/SineWave` with absolute deadband of `0.5`
  2. Count notifications over 30 seconds
- **Expect:** Fewer notifications than without deadband (~12 instead of ~30)

### Multiple monitored items

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Create one subscription
  2. Add 5 dynamic variables as monitored items
  3. Verify notifications arrive for all 5
- **Expect:** Interleaved notifications from all monitored items

---

## 4. Method Calls

### Simple method

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Call `Methods/Add` with `a=2.5, b=3.5`
- **Expect:** Output `result = 6.0`

### Method with no input

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Call `Methods/GetServerTime` with empty input arguments
- **Expect:** Output `time` is a DateTime close to current time

### Method failure

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Call `Methods/Failing`
- **Expect:** StatusCode `BadInternalError`

### Method timeout

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Call `Methods/LongRunning` with `durationMs = 10000`
  2. Set client timeout to 5000ms
- **Expect:** Client-side timeout before method completes

### Array input method

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Call `Methods/ArraySum` with `values = [1.0, 2.0, 3.0, 4.0]`
- **Expect:** Output `sum = 10.0`

---

## 5. Events

### Subscribe to server events

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Create subscription on the `Server` node
  2. Add event monitored item with select: `[EventType, Message, Severity, Time]`
  3. Wait 2 seconds
- **Expect:** Receive `SimpleEventType` event with `"Periodic event #N"`

### Trigger event via method

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Subscribe to events
  2. Call `Methods/GenerateEvent("Test event", 750)`
- **Expect:** Receive event with message `"Test event"` and severity `750`

### Filter events

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Subscribe to events with `ContentFilter` where `Severity >= 500`
  2. Wait for `SystemStatusEventType` in Maintenance state
- **Expect:** Only receive events with severity 600 (Maintenance)

---

## 6. Alarms

### Monitor alarm transitions

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Subscribe to events on `Alarms` folder
  2. Wait for `AlarmSourceValue` to oscillate
- **Expect:** Receive alarm condition events as value crosses thresholds

### Manual alarm trigger

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Write `100.0` to `Alarms/AlarmSourceValue`
  2. Check `HighTemperatureAlarm` state
- **Expect:** Alarm is active in `HighHigh` state

### Acknowledge alarm

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Trigger an alarm (write extreme value)
  2. Read alarm's `EventId`
  3. Call `Acknowledge` method
  4. Verify `AckedState` is `true`

---

## 7. Historical Data

### Read raw history

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Wait 30 seconds after server start
  2. HistoryRead `Historical/HistoricalTemperature` with `ReadRawModifiedDetails`
  3. Time range: last 30 seconds
- **Expect:** ~30 samples (recorded at 1000ms intervals), values oscillating around 22

### Paginated history read

- **Steps:**
  1. Set `NumValuesPerNode = 10`
  2. Read history
  3. Use `ContinuationPoint` to fetch next page
- **Expect:** First page has 10 values, continuation returns more

---

## 8. Security

### All security policies

- **Server:** `opcua-all-security` (4843)
- **Steps:**
  1. Call `GetEndpoints()`
  2. Iterate through returned endpoints
  3. Connect with each policy/mode combination
- **Expect:** Successful connections for all valid combinations

### Certificate rejection

- **Server:** `opcua-certificate` (4842)
- **Steps:**
  1. Connect with `certs/self-signed/cert.pem`
- **Expect:** `BadCertificateUntrusted`

### Expired certificate

- **Server:** `opcua-certificate` (4842)
- **Steps:**
  1. Connect with `certs/expired/cert.pem`
- **Expect:** `BadCertificateTimeInvalid`

### Legacy policies

- **Server:** `opcua-legacy` (4847)
- **Steps:**
  1. Connect with `Basic128Rsa15 / SignAndEncrypt`
  2. Connect with `Basic256 / Sign`
- **Expect:** Both succeed (but your client should ideally warn about deprecated policies)

---

## 9. Extension Objects

### Read custom structured type

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Browse to `ExtensionObjects/PointValue`
  2. Read the value
- **Expect:** ExtensionObject with TypeId `ns=3;i=3010`, binary body containing `{X: 1.5, Y: 2.5, Z: 3.5}`

### Write custom structured type

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Write a new `TestPointXYZ` value `{X: 10.0, Y: 20.0, Z: 30.0}` to `ExtensionObjects/PointValue`
  2. Read it back
- **Expect:** Value matches `{X: 10.0, Y: 20.0, Z: 30.0}`

### Read-only extension object

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Read `ExtensionObjects/RangeValue`
  2. Attempt to write a new value
- **Expect:** Read returns TypeId `ns=3;i=3011` with `{Min: 0.0, Max: 100.0, Value: 42.5}`, write returns `BadNotWritable`

---

## 10. Operation Limits

### Read with node limit

- **Server:** `opcua-no-security` (4840) -- configured with `OPCUA_MAX_NODES_PER_READ=5`
- **Steps:**
  1. Build a Read request with 10 node IDs
  2. Send the request
- **Expect:** Server rejects or limits the request (max 5 nodes per read)

### Write with node limit

- **Server:** `opcua-no-security` (4840) -- configured with `OPCUA_MAX_NODES_PER_WRITE=5`
- **Steps:**
  1. Build a Write request with 10 node IDs
  2. Send the request
- **Expect:** Server rejects or limits the request (max 5 nodes per write)

---

## 11. Browsing & Navigation

### Recursive browse

- **Server:** `opcua-no-security` (4840)
- **Steps:**
  1. Start at `Objects/TestServer`
  2. Recursively browse all nodes
- **Expect:** ~300 total nodes discovered

### Deep nesting

- **Steps:**
  1. Browse `Structures/DeepNesting/Level_1`
  2. Continue browsing deeper: `Level_2`, `Level_3`, ..., `Level_10`
  3. Read `Depth` at each level
- **Expect:** 10 levels, `Depth` values 1-10

### Browse with view

- **Steps:**
  1. Get NodeId of `OperatorView` from `Views` folder
  2. Browse `TestServer` with that view
- **Expect:** Only `Dynamic`, `Methods`, `Alarms` visible

---

## 12. Access Control

### Access level attributes

- **Steps:**
  1. Read `accessLevel` attribute (ID=17) of `AccessControl/AccessLevels/CurrentRead_Only`
  2. Read `userAccessLevel` attribute (ID=18)
- **Expect:** `accessLevel = CurrentRead`, `userAccessLevel = CurrentRead`

### Role-based write protection

- **Server:** `opcua-userpass` (4841)
- **Steps:**
  1. Connect as `viewer` (viewer123)
  2. Try to write to `AccessControl/OperatorLevel/Setpoint`
- **Expect:** `BadUserAccessDenied`
  3. Connect as `operator` (operator123)
  4. Write to the same variable
- **Expect:** `Good`

### All type/access combinations

- **Steps:**
  1. For each type in `AccessControl/AllCombinations`:
     - Read `_RO` variant -> `Good`
     - Write `_RO` variant -> `BadNotWritable`
     - Read `_WO` variant -> `BadNotReadable`
     - Write `_WO` variant -> `Good`
     - Read and write `_RW` variant -> both `Good`
- **Expect:** 32 variables, all behave as documented

---

## 13. Discovery

### Find servers

- **Server:** `opcua-discovery` (4844)
- **Steps:**
  1. Call `FindServers()` on `opc.tcp://localhost:4844`
- **Expect:** List of registered servers (if any have registered)

### Get endpoints

- **Server:** any server
- **Steps:**
  1. Call `GetEndpoints()` on the server endpoint
  2. Parse the response
- **Expect:** List of endpoint descriptions with security policy, mode, and auth tokens

---
eyebrow: 'Docs · Testing patterns'
lede:    'The smoke-test recipes — connect, browse, read, write — every OPC UA client library should pass. Quick checks before deeper feature tests.'

see_also:
  - { href: './subscription-and-method-tests.md',     meta: '5 min' }
  - { href: './security-tests.md',                    meta: '5 min' }
  - { href: '../data-features/scalar-types.md',       meta: '5 min' }

prev: { label: 'Docker Compose and other CI', href: '../ci-integration/docker-compose-and-other-ci.md' }
next: { label: 'Subscription and method tests', href: './subscription-and-method-tests.md' }
---

# Basic tests

The connection, browse, and read/write recipes most client
libraries run first. All target `opcua-no-security` (port 4840)
unless noted.

## Connection tests

### Basic connect

```text
1. Open opc.tcp://localhost:4840/UA/TestServer
2. Create anonymous session
3. Browse root → expect to find "Objects"
4. Browse Objects → expect to find "TestServer"
5. Close session
```

Pass: every step succeeds without error.

### GetEndpoints

```text
GetEndpoints("opc.tcp://localhost:4840/UA/TestServer")
→ returns exactly 1 EndpointDescription:
    securityPolicy:  http://opcfoundation.org/UA/SecurityPolicy#None
    securityMode:    None
    userIdentityTokens: [Anonymous]
```

For `opcua-all-security` (4843), expect 11 endpoints. For
`opcua-certificate` (4842), 6.

### Connection refused on unsupported policy

Connect to `opcua-userpass` (4841) requesting `None / None` →
expect failure (the server doesn't offer it).

## Read tests

### Single scalar read

```text
read(ns=1;s=TestServer/DataTypes/Scalar/BooleanValue)
→ DataValue { value=true, statusCode=Good }
```

Repeat for every scalar type — see
[Scalar types](../data-features/scalar-types.md) for the full
list of initial values.

### Batch read

```text
read([
  ns=1;s=TestServer/DataTypes/Scalar/BooleanValue,
  ns=1;s=TestServer/DataTypes/Scalar/Int32Value,
  ns=1;s=TestServer/DataTypes/Scalar/StringValue,
])
→ 3 DataValues, each Good
```

For `opcua-no-security`, the server caps `MaxNodesPerRead` at
**5**. Asking for 6 nodes returns `BadTooManyOperations`. Use
this to test the limit error path.

### Read non-Value attribute

```text
read(ns=1;s=TestServer/DataTypes/Scalar/BooleanValue, attribute=DisplayName)
→ "BooleanValue" (LocalizedText)

read(..., attribute=AccessLevel)
→ 3 (CurrentRead | CurrentWrite)
```

## Write tests

### Single scalar write

```text
write(ns=1;s=TestServer/DataTypes/Scalar/BooleanValue, value=false)
→ Good
read(ns=1;s=TestServer/DataTypes/Scalar/BooleanValue)
→ value=false
```

### Round-trip every type

```text
for each scalar type T:
    sample = make_test_value(T)
    write(ns=1;s=TestServer/DataTypes/Scalar/{T}Value, value=sample)
    assert read(...).value == sample
```

Strong assertion that your client wires up every encoder.

### Read-only rejection

```text
write(ns=1;s=TestServer/DataTypes/ReadOnly/Int32_RO, value=42)
→ Bad_NotWritable
```

### Type mismatch

```text
write(ns=1;s=TestServer/DataTypes/Scalar/Int32Value, value="not a number")
→ Bad_TypeMismatch (or your library coerces silently — check)
```

## Browse tests

### Browse Objects → find TestServer

```text
browse(ns=0;i=85)  # Objects folder
→ result contains a reference to ns=1;s=TestServer
```

### Browse TestServer → 9 folders

```text
browse(ns=1;s=TestServer)
→ 9 references (or 10 on opcua-sks)
   [DataTypes, Methods, Dynamic, Events, Alarms,
    Historical, Structures, ExtensionObjects, AccessControl]
```

### Recursive browse

```text
recursive_browse(ns=1;s=TestServer, maxDepth=20)
→ approximately 300 nodes
```

Exact count varies by server config (toggled features) but is
stable across restarts.

### Browse with filter

```text
browse(ns=1;s=TestServer, nodeClassFilter=Variable)
→ only variable children (none directly — they're in subfolders)

browse(ns=1;s=TestServer/DataTypes/Scalar, nodeClassFilter=Variable)
→ 21 variables
```

## Endpoint discovery on each server

For each of the 10 classic servers, call `GetEndpoints` and
verify the **count** matches:

| Server                  | Expected endpoint count |
| ----------------------- | ----------------------- |
| `opcua-no-security`     | 1                       |
| `opcua-userpass`        | 1                       |
| `opcua-certificate`     | 6                       |
| `opcua-all-security`    | 11                      |
| `opcua-discovery`       | varies (registered servers) |
| `opcua-auto-accept`     | 1                       |
| `opcua-sign-only`       | 1                       |
| `opcua-legacy`          | 4                       |
| `opcua-ecc-nist`        | 4                       |
| `opcua-ecc-brainpool`   | 4                       |

## Multi-server checklist

For a comprehensive smoke test:

| Server                | Connect | Browse  | Read Int32 |
| --------------------- | ------- | ------- | ---------- |
| `opcua-no-security`    | ✓       | ✓       | ✓          |
| `opcua-userpass`       | ✓ (with creds) | ✓ | ✓        |
| `opcua-certificate`    | ✓ (with cert)  | ✓ | ✓        |
| `opcua-all-security`   | ✓ (any combo)  | ✓ | ✓        |

A test matrix that runs these three operations against each
server flushes out 80% of integration bugs.

## What "Good" means

OPC UA `Good = 0x00000000`. A successful read has:

```text
statusCode = Good
sourceTimestamp = a recent DateTime
serverTimestamp = a recent DateTime (often equal to sourceTimestamp)
value           = the actual value
```

Any other status code in `0x80xxxxxx` is a **bad** read and
should fail the test. `0x40xxxxxx` is **uncertain** — the value
may be present but flagged as unreliable; treat per your
library's policy.

## Where to read next

- [Subscription and method tests](./subscription-and-method-tests.md) —
  the next layer.
- [Security tests](./security-tests.md) — cert and auth recipes.

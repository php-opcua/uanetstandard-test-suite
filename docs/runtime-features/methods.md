---
eyebrow: 'Docs · Runtime features'
lede:    'Twelve callable methods covering arithmetic, strings, arrays, async, error handling, and event generation. The reference for method-call tests.'

see_also:
  - { href: './dynamic-variables.md',                     meta: '4 min' }
  - { href: '../testing-patterns/subscription-and-method-tests.md', meta: '5 min' }

prev: { label: 'Access control',  href: '../data-features/access-control.md' }
next: { label: 'Dynamic variables', href: './dynamic-variables.md' }
---

# Methods

Path: `TestServer / Methods`

Twelve methods covering the major scenarios your client's
method-call surface needs to handle. Each has a stable BrowsePath
and InputArguments / OutputArguments properties readable via
standard browse.

## The methods

### Add

Adds two doubles.

| Direction | Name     | Type    | Notes                  |
| --------- | -------- | ------- | ---------------------- |
| Input     | `a`      | Double  |                        |
| Input     | `b`      | Double  |                        |
| Output    | `result` | Double  | `a + b`                |

### Multiply

| Direction | Name     | Type    |
| --------- | -------- | ------- |
| Input     | `a`      | Double  |
| Input     | `b`      | Double  |
| Output    | `result` | Double  |

### Concatenate

| Direction | Name     | Type    | Notes        |
| --------- | -------- | ------- | ------------ |
| Input     | `a`      | String  |              |
| Input     | `b`      | String  |              |
| Output    | `result` | String  | `a + b`      |

### Reverse

| Direction | Name     | Type    | Notes                          |
| --------- | -------- | ------- | ------------------------------ |
| Input     | `input`  | String  |                                |
| Output    | `result` | String  | characters reversed             |

### GetServerTime

No inputs.

| Direction | Name   | Type      | Notes                       |
| --------- | ------ | --------- | --------------------------- |
| Output    | `time` | DateTime  | Current server UTC time     |

### Echo

Returns its input unchanged. Useful for Variant round-trip
tests.

| Direction | Name     | Type    | Notes                                  |
| --------- | -------- | ------- | -------------------------------------- |
| Input     | `input`  | Variant | Any OPC UA type                        |
| Output    | `output` | Variant | The same value                         |

### GenerateEvent

A no-op stub. The method is registered in the address space and
will be called successfully, but the handler **only writes a
console line on the server** (`Console.WriteLine("GenerateEvent
called: …")`) — it does not call `ReportEvent` or otherwise
publish an OPC UA event. Subscribers will not see anything.

| Direction | Name       | Type     | Notes                                          |
| --------- | ---------- | -------- | ---------------------------------------------- |
| Input     | `message`  | String   | Echoed to the server's stdout                  |
| Input     | `severity` | UInt16   | Echoed to the server's stdout                  |

No outputs. Treat this method as a placeholder for future event
generation work — do not rely on it to trigger event tests today.

### LongRunning

Blocks server-side for the specified duration. Capped at 30 s.

| Direction | Name         | Type    | Notes                                  |
| --------- | ------------ | ------- | -------------------------------------- |
| Input     | `durationMs` | UInt32  | Milliseconds. Server caps at 30 000.   |
| Output    | `completed`  | Boolean | Always `true` on return                 |

Use to test client-side timeout handling.

### Failing

No inputs, no outputs. Always returns `Bad_InternalError`
(`0x80020000`).

The OPC UA method-call **succeeds** at the transport level —
your client receives a response. The response's status code is
`Bad_InternalError`. Exercise the difference between "call
failed" and "call succeeded but result status is Bad".

### ArraySum

| Direction | Name      | Type     | Notes                              |
| --------- | --------- | -------- | ---------------------------------- |
| Input     | `values`  | Double[] | Array of doubles                   |
| Output    | `sum`     | Double   | Sum                                |

Tests array-argument passing. Try empty, single-element, and
large arrays.

### MatrixTranspose

| Direction | Name       | Type     | Notes                              |
| --------- | ---------- | -------- | ---------------------------------- |
| Input     | `matrix`   | Double[] | Flat data (row-major)               |
| Input     | `rows`     | UInt32   | Row count                          |
| Input     | `cols`     | UInt32   | Column count                       |
| Output    | `result`   | Double[] | Transposed (flat, row-major)        |

The handler does not validate `matrix.Length == rows * cols`.
With a mismatched length the indexing `matrix[r * cols + c]`
goes out of bounds and an `IndexOutOfRangeException` is thrown;
the generic method wrapper in `TestNodeManager.CreateMethod`
catches every exception and returns the status code
`Bad_InternalError`. Tests that expect `Bad_InvalidArgument` on
shape mismatch will see `Bad_InternalError` instead.

Example:

```text
matrix=[1,2,3,4,5,6], rows=2, cols=3
       represents:  [[1,2,3], [4,5,6]]

result=[1,4,2,5,3,6]
       represents:  [[1,4], [2,5], [3,6]]
```

### MultiOutput

No inputs.

| Direction | Name           | Type     | Value      |
| --------- | -------------- | -------- | ---------- |
| Output    | `intValue`     | Int32    | `42`       |
| Output    | `stringValue`  | String   | `"hello"`  |
| Output    | `boolValue`    | Boolean  | `true`     |

Tests multiple typed outputs in one call.

## Discovering method signatures

Each method exposes the standard `InputArguments` /
`OutputArguments` property nodes — but **only when that side has
at least one argument**. The wrapper in `TestNodeManager.CreateMethod`
skips an empty side rather than creating an empty array property.
Concretely:

| Method            | Has `InputArguments`? | Has `OutputArguments`? |
| ----------------- | --------------------- | ---------------------- |
| `Add`             | Yes                   | Yes                    |
| `Multiply`        | Yes                   | Yes                    |
| `Concatenate`     | Yes                   | Yes                    |
| `Reverse`         | Yes                   | Yes                    |
| `GetServerTime`   | No                    | Yes                    |
| `Echo`            | Yes                   | Yes                    |
| `GenerateEvent`   | Yes                   | No                     |
| `LongRunning`     | Yes                   | Yes                    |
| `Failing`         | No                    | No                     |
| `ArraySum`        | Yes                   | Yes                    |
| `MatrixTranspose` | Yes                   | Yes                    |
| `MultiOutput`     | No                    | Yes                    |

When present, each property returns an array of `Argument`
records with `Name`, `DataType`, `ValueRank`, `Description`. The
NodeId is `TestServer/Methods/<MethodName>/InputArguments` (or
`/OutputArguments`) — string-based, in the `ns=1` namespace.

## Test checklist

- [ ] `Add(2.5, 3.5)` → `6.0`
- [ ] `Multiply(3.0, 4.0)` → `12.0`
- [ ] `Concatenate("Hello", " World")` → `"Hello World"`
- [ ] `Reverse("abcde")` → `"edcba"`
- [ ] `GetServerTime()` → DateTime close to now
- [ ] `Echo(value)` for various types — round-trip identity
- [ ] `GenerateEvent("test", 500)` → returns Good but **no event is emitted** (no-op stub)
- [ ] `LongRunning(2000)` → `true` after ~2 seconds
- [ ] `LongRunning(60000)` → still returns; duration capped to 30 s
- [ ] `Failing()` → `Bad_InternalError` in the call result
- [ ] `ArraySum([1.0, 2.0, 3.0])` → `6.0`
- [ ] `ArraySum([])` → `0.0`
- [ ] `MatrixTranspose([1,2,3,4], 2, 2)` → `[1,3,2,4]`
- [ ] `MatrixTranspose([1,2,3], 2, 2)` → `Bad_InternalError` (no shape validation; out-of-range index is caught and reported as `Bad_InternalError`)
- [ ] `MultiOutput()` → `(42, "hello", true)`

## Method-call mechanics

OPC UA method calls have a hierarchical structure:

```text
call(
  objectId  = TestServer/Methods,        # the parent
  methodId  = TestServer/Methods/Add,    # the method itself
  inputs    = [2.5, 3.5],
)
```

Some libraries hide this and just take the method NodeId; others
require both. Both styles work against this suite.

## Where to read next

- [Subscription and method tests](../testing-patterns/subscription-and-method-tests.md) —
  test recipes for the method-call surface.
- [Dynamic variables](./dynamic-variables.md) — the time-varying
  test surface.

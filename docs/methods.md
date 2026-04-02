# Methods

Path: `Objects > TestServer > Methods`

12 callable methods covering arithmetic, string operations, arrays, async operations, error handling, and event generation.

## Method Reference

### Add

Adds two doubles.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `a` | Double | First operand |
| **Input** | `b` | Double | Second operand |
| **Output** | `result` | Double | `a + b` |

### Multiply

Multiplies two doubles.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `a` | Double | First operand |
| **Input** | `b` | Double | Second operand |
| **Output** | `result` | Double | `a * b` |

### Concatenate

Concatenates two strings.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `a` | String | First string |
| **Input** | `b` | String | Second string |
| **Output** | `result` | String | `a + b` |

### Reverse

Reverses a string.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `input` | String | Input string |
| **Output** | `result` | String | Characters in reverse order |

### GetServerTime

Returns the current server time. No input arguments.

| | Name | DataType | Description |
|---|---|---|---|
| **Output** | `time` | DateTime | Current server UTC time |

### Echo

Echoes back any value. Accepts a Variant (any OPC UA type) and returns it unchanged. Useful for testing type serialization round-trips.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `input` | Variant | Any value |
| **Output** | `output` | Variant | Same value echoed back |

### GenerateEvent

Raises a `BaseEventType` event on the server object. Useful for testing event subscriptions on demand.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `message` | String | Event message text |
| **Input** | `severity` | UInt16 | Event severity (0-1000) |

No output. The event is raised asynchronously and can be received by any client subscribed to events on the server object.

### LongRunning

Simulates a long-running operation. The method blocks for the specified duration, then returns.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `durationMs` | UInt32 | Duration in milliseconds (capped at 30,000) |
| **Output** | `completed` | Boolean | Always `true` on completion |

**Testing notes:**
- The server caps the duration at 30 seconds to prevent abuse
- Use this to test method call timeouts in your client
- If your client has a 5-second timeout, call with `durationMs = 10000` to trigger it

### Failing

Always returns an error. No inputs, no outputs.

| | Name | DataType | Description |
|---|---|---|---|
| *(none)* | | | |

**Returns:** `BadInternalError` (0x80020000)

**Testing notes:**
- Use this to verify your client correctly handles method call failures
- The method call itself succeeds at the OPC UA transport level, but the result status code is `BadInternalError`

### ArraySum

Sums all elements of a Double array.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `values` | Double[] | Array of doubles |
| **Output** | `sum` | Double | Sum of all values |

**Testing notes:**
- Tests passing array arguments to methods
- Try with empty array, single element, large arrays

### MatrixTranspose

Transposes a flat matrix representation.

| | Name | DataType | Description |
|---|---|---|---|
| **Input** | `matrix` | Double[] | Flat matrix data (row-major) |
| **Input** | `rows` | UInt32 | Number of rows |
| **Input** | `cols` | UInt32 | Number of columns |
| **Output** | `result` | Double[] | Transposed matrix (flat, row-major) |

If `matrix.length != rows * cols`, returns `BadInvalidArgument`.

**Example:**
```
Input:  matrix=[1,2,3,4,5,6], rows=2, cols=3
        Represents: [[1,2,3],[4,5,6]]
Output: result=[1,4,2,5,3,6]
        Represents: [[1,4],[2,5],[3,6]]
```

### MultiOutput

Returns multiple output values of different types. No inputs.

| | Name | DataType | Description |
|---|---|---|---|
| **Output** | `intValue` | Int32 | Always `42` |
| **Output** | `stringValue` | String | Always `"hello"` |
| **Output** | `boolValue` | Boolean | Always `true` |

**Testing notes:**
- Verifies your client can handle multiple output arguments
- Verifies correct type mapping for each output

## Testing Checklist

- [ ] Call `Add(2.5, 3.5)` -> expect `6.0`
- [ ] Call `Multiply(3.0, 4.0)` -> expect `12.0`
- [ ] Call `Concatenate("Hello", " World")` -> expect `"Hello World"`
- [ ] Call `Reverse("abcde")` -> expect `"edcba"`
- [ ] Call `GetServerTime()` -> expect a DateTime close to current time
- [ ] Call `Echo` with various types (Int32, String, Boolean, Array) -> expect same value back
- [ ] Call `GenerateEvent("test", 500)` -> verify event received on subscription
- [ ] Call `LongRunning(2000)` -> expect `true` after ~2 seconds
- [ ] Call `LongRunning(60000)` -> duration is capped to 30s
- [ ] Call `Failing()` -> expect `BadInternalError`
- [ ] Call `ArraySum([1.0, 2.0, 3.0])` -> expect `6.0`
- [ ] Call `MatrixTranspose([1,2,3,4], 2, 2)` -> expect `[1,3,2,4]`
- [ ] Call `MatrixTranspose([1,2,3], 2, 2)` -> expect `BadInvalidArgument`
- [ ] Call `MultiOutput()` -> expect `(42, "hello", true)`

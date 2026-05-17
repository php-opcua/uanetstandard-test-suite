---
eyebrow: 'Docs · Data features'
lede:    'Arrays (1D), empty arrays, and 2D/3D matrices. Every shape your client''s array handling needs to survive.'

see_also:
  - { href: './scalar-types.md',     meta: '5 min' }
  - { href: './structures-and-extension-objects.md',     meta: '4 min' }

prev: { label: 'Scalar types',  href: './scalar-types.md' }
next: { label: 'Structures and extension objects', href: './structures-and-extension-objects.md' }
---

# Arrays and matrices

Path: `TestServer / DataTypes / Array` and `MultiDimensional`.

## 1D arrays (read-write)

Path: `TestServer / DataTypes / Array`

20 arrays with `valueRank = 1`. All RW.

Initial values come straight from
`src/TestServer/AddressSpace/DataTypesBuilder.cs`. Most arrays are
intentionally short (3 elements) — the suite optimises for
boundary coverage and round-trip checks, not for stress data.

| BrowseName            | ElementType     | Initial value                                                   |
| --------------------- | --------------- | --------------------------------------------------------------- |
| `BooleanArray`        | Boolean[]       | `[true, false, true]`                                           |
| `SByteArray`          | SByte[]         | `[-1, 0, 1]`                                                    |
| `ByteArray`           | Byte[]          | `[1, 2, 3]`                                                     |
| `Int16Array`          | Int16[]         | `[-100, 0, 100]`                                                |
| `UInt16Array`         | UInt16[]        | `[100, 200, 300]`                                               |
| `Int32Array`          | Int32[]         | `[-1000, 0, 1000]`                                              |
| `UInt32Array`         | UInt32[]        | `[1000, 2000, 3000]`                                            |
| `Int64Array`          | Int64[]         | `[-100000, 0, 100000]`                                          |
| `UInt64Array`         | UInt64[]        | `[100000, 200000, 300000]`                                      |
| `FloatArray`          | Float[]         | `[1.1, 2.2, 3.3]`                                               |
| `DoubleArray`         | Double[]        | `[1.11, 2.22, 3.33]`                                            |
| `StringArray`         | String[]        | `["one", "two", "three"]`                                       |
| `DateTimeArray`       | DateTime[]      | `[now, now + 1h, now + 2h]` (captured at server startup)        |
| `GuidArray`           | Guid[]          | Three fresh `Guid.NewGuid()` values per server start            |
| `ByteStringArray`     | ByteString[]    | `[ [0x01], [0x02], [0x03] ]` (three single-byte byte strings)   |
| `XmlElementArray`     | XmlElement[]    | `Array.Empty<XmlElement>()` (length 0)                          |
| `NodeIdArray`         | NodeId[]        | `[ns=0;i=1, ns=0;i=2]`                                          |
| `StatusCodeArray`     | StatusCode[]    | `[Good, Bad]`                                                   |
| `QualifiedNameArray`  | QualifiedName[] | `[1:a, 1:b]`                                                    |
| `LocalizedTextArray`  | LocalizedText[] | `[en:one, en:two]`                                              |

## 1D arrays (read-only)

Path: `TestServer / DataTypes / Array / ReadOnly`

Six arrays with `accessLevel = CurrentRead`. Writes return
`Bad_NotWritable`.

| BrowseName            | Value                            |
| --------------------- | -------------------------------- |
| `BooleanArray_RO`     | `[true, false]`                  |
| `Int32Array_RO`       | `[10, 20, 30]`                   |
| `DoubleArray_RO`      | `[1.0, 2.0, 3.0]`                |
| `StringArray_RO`      | `["read", "only"]` (2 elements)  |
| `ByteArray_RO`        | `[0xAA, 0xBB]` (2 elements)      |
| `DateTimeArray_RO`    | `[DateTime.UtcNow]` (1 element, captured at startup) |

## Empty arrays

Path: `TestServer / DataTypes / Array / Empty`

14 variables initialised to `[]`. Read-write. Used to verify
your library handles **zero-length** arrays correctly — many
encoders surprise themselves at this boundary.

| BrowseName               | ElementType    |
| ------------------------ | -------------- |
| `EmptyBooleanArray`      | Boolean[]      |
| `EmptySByteArray`        | SByte[]        |
| `EmptyByteArray`         | Byte[]         |
| `EmptyInt16Array`        | Int16[]        |
| `EmptyUInt16Array`       | UInt16[]       |
| `EmptyInt32Array`        | Int32[]        |
| `EmptyUInt32Array`       | UInt32[]       |
| `EmptyInt64Array`        | Int64[]        |
| `EmptyUInt64Array`       | UInt64[]       |
| `EmptyFloatArray`        | Float[]        |
| `EmptyDoubleArray`       | Double[]       |
| `EmptyStringArray`       | String[]       |
| `EmptyDateTimeArray`     | DateTime[]     |
| `EmptyByteStringArray`   | ByteString[]   |

Read should return an array of length 0 (not `null`).

## Multi-dimensional matrices

Path: `TestServer / DataTypes / MultiDimensional`

Three matrices with `valueRank > 1`. Stored as a flat array
with `arrayDimensions` metadata, **row-major** ordering.

| BrowseName         | Type      | Dimensions | Initial value (row-major)                              |
| ------------------ | --------- | ---------- | ------------------------------------------------------ |
| `Matrix2D_Double`  | Double    | 3 × 3      | `[[1,2,3], [4,5,6], [7,8,9]]`                          |
| `Matrix2D_Int32`   | Int32     | 2 × 4      | `[[1,2,3,4], [5,6,7,8]]`                               |
| `Cube3D_Byte`      | Byte      | 2 × 3 × 4 | `new byte[2,3,4]` — all 24 elements default to `0`     |

To address element `[i,j]` of a 2D matrix in flat storage:

```text
flat_index = i * cols + j
```

For 3D `[i,j,k]` with dimensions `[dim0, dim1, dim2]`:

```text
flat_index = i * (dim1 * dim2) + j * dim2 + k
```

Reading the variable returns:

- `value`: flat array
- `arrayDimensions`: the shape (`[3,3]`, `[2,4]`, or `[2,3,4]`)
- `valueRank`: 2 or 3

Your client should expose both pieces — the flat data and the
dimensions — so callers can reconstruct the matrix.

## Test patterns

### Round-trip 1D

For each RW 1D array:

```text
write(<node>, <new-array>)
assert read(<node>) == <new-array>
```

Try different lengths than the initial value to exercise
allocation behaviour.

### Boundary values

| Array               | Boundary check                                |
| ------------------- | --------------------------------------------- |
| `SByteArray`        | `[Int8.MIN, Int8.MAX]`                        |
| `Int16Array`        | `[Int16.MIN, Int16.MAX]`                      |
| `UInt32Array`       | Includes `4294967295` (`UInt32.MAX`)           |
| `UInt64Array`       | Try with `Int64.MAX + 1`                       |

### Empty round-trip

For each empty array:

```text
write(<node>, []) → expect Good
read(<node>)      → expect []
```

Then:

```text
write(<node>, [a, b, c]) → expect Good
read(<node>)              → [a, b, c]
write(<node>, [])         → expect Good
read(<node>)              → []
```

Exercises the encoder's array-length handling at both extremes.

### Matrix shape

For each multi-D matrix:

1. Read once → get flat data + dimensions.
2. Reconstruct the matrix locally.
3. Write the reconstructed flat array back.
4. Read again — bytes should match.

For `Matrix2D_Double`, also try writing a **wrong-length** flat
array (8 instead of 9 elements). The server doesn't enforce
length consistency, so the write succeeds — but reading back
shows the inconsistency. Your client should ideally validate
locally before writing.

## Where to read next

- [Structures and extension objects](./structures-and-extension-objects.md) —
  structured types beyond primitive arrays.
- [Basic tests](../testing-patterns/basic-tests.md) —
  recipes covering arrays.

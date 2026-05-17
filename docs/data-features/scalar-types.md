---
eyebrow: 'Docs · Data features'
lede:    'The 21 scalar variables under TestServer/DataTypes — one per OPC UA built-in type, in RW and RO variants. The reference your read/write round-trip tests will exercise first.'

see_also:
  - { href: './arrays-and-matrices.md',     meta: '4 min' }
  - { href: '../testing-patterns/basic-tests.md', meta: '5 min' }

prev: { label: 'Browse paths and access levels', href: '../address-space/browse-paths-and-access-levels.md' }
next: { label: 'Arrays and matrices',     href: './arrays-and-matrices.md' }
---

# Scalar types

Path: `TestServer / DataTypes / Scalar` (RW) and `Scalar/ReadOnly` (read-only siblings).

21 variables per group, one per OPC UA built-in scalar type.

## Read-write scalars

Values come from `src/TestServer/AddressSpace/DataTypesBuilder.cs`.
Boundary-value tests should use your own data — the initial set
is deliberately mild so that round-trip tests have a known
neutral starting point.

| BrowseName            | Type             | Initial value                              |
| --------------------- | ---------------- | ------------------------------------------ |
| `BooleanValue`        | Boolean          | `true`                                     |
| `SByteValue`          | SByte            | `-42`                                      |
| `ByteValue`           | Byte             | `42`                                       |
| `Int16Value`          | Int16            | `-1000`                                    |
| `UInt16Value`         | UInt16           | `1000`                                     |
| `Int32Value`          | Int32            | `-100000`                                  |
| `UInt32Value`         | UInt32           | `100000`                                   |
| `Int64Value`          | Int64            | `-1000000`                                 |
| `UInt64Value`         | UInt64           | `1000000`                                  |
| `FloatValue`          | Float            | `3.14`                                     |
| `DoubleValue`         | Double           | `2.71828`                                  |
| `StringValue`         | String           | `"Hello OPC UA"`                           |
| `DateTimeValue`       | DateTime         | `DateTime.UtcNow` captured at server start |
| `GuidValue`           | Guid             | Fresh `Guid.NewGuid()` per server start (non-deterministic) |
| `ByteStringValue`     | ByteString       | `[0x01, 0x02, 0x03, 0x04]`                 |
| `XmlElementValue`     | XmlElement       | `<test>Hello OPC UA</test>`                |
| `NodeIdValue`         | NodeId           | `ns=0;i=1234` (numeric, server namespace)  |
| `ExpandedNodeIdValue` | ExpandedNodeId   | `ns=0;i=5678`                              |
| `StatusCodeValue`     | StatusCode       | `Good` (`0x00000000`)                      |
| `QualifiedNameValue`  | QualifiedName    | `1:TestName`                               |
| `LocalizedTextValue`  | LocalizedText    | `en: "Test Text"`                          |

All RW: write the new value, read it back, expect equality.

## Read-only scalars

Path: `TestServer / DataTypes / ReadOnly`

Same 21 types as above, suffixed `_RO`. `accessLevel =
CurrentRead`. Writes return `Bad_NotWritable`.

| BrowseName              | Type            | Value                                            |
| ----------------------- | --------------- | ------------------------------------------------ |
| `Boolean_RO`            | Boolean         | `true`                                           |
| `SByte_RO`              | SByte           | `-10`                                            |
| `Byte_RO`               | Byte            | `10`                                             |
| `Int16_RO`              | Int16           | `-500`                                           |
| `UInt16_RO`             | UInt16          | `500`                                            |
| `Int32_RO`              | Int32           | `-50000`                                         |
| `UInt32_RO`             | UInt32          | `50000`                                          |
| `Int64_RO`              | Int64           | `-500000`                                        |
| `UInt64_RO`             | UInt64          | `500000`                                         |
| `Float_RO`              | Float           | `1.618`                                          |
| `Double_RO`             | Double          | `1.41421`                                        |
| `String_RO`             | String          | `"ReadOnly String"`                              |
| `DateTime_RO`           | DateTime        | `DateTime.UtcNow` captured at server start       |
| `Guid_RO`               | Guid            | `12345678-1234-1234-1234-123456789abc` (constant)|
| `ByteString_RO`         | ByteString      | `[0xDE, 0xAD, 0xBE, 0xEF]`                       |
| `XmlElement_RO`         | XmlElement      | `default(XmlElement)` — server returns the value as `null` |
| `NodeId_RO`             | NodeId          | `ns=0;i=9999`                                    |
| `ExpandedNodeId_RO`     | ExpandedNodeId  | `ns=0;i=8888`                                    |
| `StatusCode_RO`         | StatusCode      | `Good`                                           |
| `QualifiedName_RO`      | QualifiedName   | `1:ReadOnly`                                     |
| `LocalizedText_RO`      | LocalizedText   | `en: "ReadOnly Text"`                            |

## Test patterns

### Round-trip every type

For each RW variable:

```text
write(<node>, <new-value>)
assert read(<node>) == <new-value>
```

Covers wire encoding + decoding for the full type set.

### Read-only rejection

For each RO variable:

```text
status = write(<node>, <some-value>)
assert status == Bad_NotWritable
```

### Type-specific notes

- **`Int64` / `UInt64`** — make sure your library doesn't downcast
  to 32-bit. The initial values are well within 32-bit range, but
  try writing `2^63` or `2^64 - 1` to verify.
- **`ByteString`** — opaque bytes, not a UTF-8 string. Some
  libraries silently convert; test with bytes that aren't valid
  UTF-8 (e.g., `0xff, 0xfe`).
- **`Guid`** — standard `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`
  format. Endian handling differs between libraries; test with
  a non-symmetric GUID.
- **`DateTime`** — OPC UA uses 100-nanosecond ticks from epoch
  1601-01-01 UTC. Many libraries convert to native datetime
  types; verify sub-second precision survives.
- **`StatusCode`** — a UInt32 wrapping a status. Don't accidentally
  treat it as a number — your client should expose it as a
  StatusCode type with helpers.
- **`LocalizedText`** — `{ locale, text }` pair. Some servers
  store only `text`; the suite stores both.
- **`QualifiedName`** — `{ namespaceIndex, name }`. The namespace
  index in the value is independent of the namespace of the
  variable holding it.
- **`NodeId` / `ExpandedNodeId`** — when written, the server
  doesn't validate that the NodeId points anywhere real. They're
  raw values.

## Why exhaustive coverage matters

A client that reads/writes `Int32` correctly but mishandles
`UInt64` will fail on real PLCs that use both. This 21-variable
sweep is the cheapest test for "did we wire all the encoders".

For a single client-side test that fans across all 21:

```text
for type, node in scalars:
    write(node, sample_value_for(type))
    assert read(node).value == sample_value_for(type)
```

Sample values are your call. Boundary values (`Int16.MAX`,
`UInt32.MAX`, empty string, `DateTime.MinValue`) flush out
overflow bugs your library may have.

## Where to read next

- [Arrays and matrices](./arrays-and-matrices.md) — array variants
  of these types plus 2D/3D matrices.
- [Testing patterns · Basic tests](../testing-patterns/basic-tests.md) —
  recipes for round-trip tests.

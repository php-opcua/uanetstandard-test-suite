# Extension Objects

Path: `Objects > TestServer > ExtensionObjects`

Custom structured data types with binary-encoded `ExtensionObject` values. Unlike the `Structures` module (which uses plain OPC UA Object nodes with child variables), extension objects use proper OPC UA `ExtensionObject` encoding -- the structured value is serialized as a single typed binary blob in the OPC UA binary protocol.

## Custom Types Namespace

- **Namespace Index:** `ns=3`
- **Namespace URI:** `urn:opcua:test-server:custom-types`
- **Encoding:** Binary (Default Binary Encoding)

The extension object types and their encoding nodes are defined in namespace 3. This is separate from the main test node namespace (`ns=1`).

## TestPointXYZ

A 3D point with three Double fields.

| Field | DataType | Description |
|---|---|---|
| `X` | Double | X coordinate |
| `Y` | Double | Y coordinate |
| `Z` | Double | Z coordinate |

**Type definition:** DataType node with Default Binary Encoding at `ns=3;i=3010` (TypeId).

## TestRangeStruct

A range with min, max, and current value.

| Field | DataType | Description |
|---|---|---|
| `Min` | Double | Minimum value |
| `Max` | Double | Maximum value |
| `Value` | Double | Current value |

**Type definition:** DataType node with Default Binary Encoding at `ns=3;i=3011` (TypeId).

## Variables

| BrowseName | DataType | Access | Initial Value |
|---|---|---|---|
| `PointValue` | TestPointXYZ | RW | `{X: 1.5, Y: 2.5, Z: 3.5}` |
| `RangeValue` | TestRangeStruct | R | `{Min: 0.0, Max: 100.0, Value: 42.5}` |

## Binary Encoding Details

Both extension object variables use binary-encoded `ExtensionObject` values. The binary body for each type consists of 3 consecutive IEEE 754 double-precision (8 bytes each) values:

**PointValue binary body (24 bytes):**
```
X = 1.5 (8 bytes) | Y = 2.5 (8 bytes) | Z = 3.5 (8 bytes)
TypeId: ns=3;i=3010
```

**RangeValue binary body (24 bytes):**
```
Min = 0.0 (8 bytes) | Max = 100.0 (8 bytes) | Value = 42.5 (8 bytes)
TypeId: ns=3;i=3011
```

## Testing Notes

- **Read `PointValue`:** Returns an `ExtensionObject` with `TypeId` pointing to `ns=3;i=3010`. Your client must decode the binary encoding to extract the X, Y, Z fields.
- **Write `PointValue`:** Encode a `TestPointXYZ` as an `ExtensionObject` with the correct TypeId and binary body, then write it. Read it back to verify round-trip correctness.
- **Read `RangeValue`:** Returns a read-only `TestRangeStruct` with TypeId `ns=3;i=3011`. Writing should return `BadNotWritable`.
- **Type discovery:** Browse the DataType nodes in namespace 3 to discover the field structure.
- **Binary encoding:** Both types have `Default Binary` encoding nodes in namespace 3. Clients that support automatic type dictionaries should be able to decode them without manual configuration.

## Difference from Structures Module

| Feature | Structures | Extension Objects |
|---|---|---|
| Encoding | Object + child variables | Single ExtensionObject value (binary) |
| Read | One read per field | One read returns all fields |
| Write | One write per field | One write sets all fields |
| Browse | Browse children to find fields | Browse DataType for Definition |
| Namespace | ns=1 | Types in ns=3, variables in ns=1 |
| Use case | Hierarchical browsing tests | Structured type encoding tests |

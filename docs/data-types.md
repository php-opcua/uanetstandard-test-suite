# Data Types

Path: `Objects > TestServer > DataTypes`

This section provides variables for every OPC UA built-in data type, in read/write, read-only, array, empty array, multi-dimensional, and analog (range) variants.

## 1. Scalar Variables (Read/Write)

Path: `TestServer > DataTypes > Scalar`

21 variables, one for each OPC UA data type. All are readable and writable.

| BrowseName | DataType | Access | Initial Value |
|---|---|---|---|
| `BooleanValue` | Boolean | RW | `true` |
| `SByteValue` | SByte | RW | `-42` |
| `ByteValue` | Byte | RW | `42` |
| `Int16Value` | Int16 | RW | `-1000` |
| `UInt16Value` | UInt16 | RW | `1000` |
| `Int32Value` | Int32 | RW | `-100000` |
| `UInt32Value` | UInt32 | RW | `100000` |
| `Int64Value` | Int64 | RW | `-1000000` |
| `UInt64Value` | UInt64 | RW | `1000000` |
| `FloatValue` | Float | RW | `3.14` |
| `DoubleValue` | Double | RW | `3.141592653589793` |
| `StringValue` | String | RW | `"Hello OPC UA"` |
| `DateTimeValue` | DateTime | RW | Current time at startup |
| `GuidValue` | Guid | RW | `72962B91-FA75-4AE6-8D28-B404DC7DAF63` |
| `ByteStringValue` | ByteString | RW | `"OPC UA Test Data"` (as bytes) |
| `XmlElementValue` | XmlElement | RW | `<test><value>42</value></test>` |
| `NodeIdValue` | NodeId | RW | `ns=1;s=test-nodeid` |
| `ExpandedNodeIdValue` | ExpandedNodeId | RW | `ns=1;i=1234` |
| `StatusCodeValue` | StatusCode | RW | `Good (0x00000000)` |
| `QualifiedNameValue` | QualifiedName | RW | `1:TestQualifiedName` |
| `LocalizedTextValue` | LocalizedText | RW | `en: "Test Localized Text"` |

**Testing notes:**
- Write a new value, then read it back to verify round-trip correctness
- Int64/UInt64 are native 64-bit values in .NET (no array encoding needed)
- ByteString is raw binary data, not a text string
- Guid must be in standard format: `XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`

## 2. Read-Only Scalars

Path: `TestServer > DataTypes > ReadOnly`

Same 21 data types but with `accessLevel = CurrentRead` only. Writing to these should return `BadNotWritable`.

| BrowseName | DataType | Access | Value |
|---|---|---|---|
| `Boolean_RO` | Boolean | R | `true` |
| `SByte_RO` | SByte | R | `-42` |
| `Byte_RO` | Byte | R | `42` |
| `Int16_RO` | Int16 | R | `-1000` |
| `UInt16_RO` | UInt16 | R | `1000` |
| `Int32_RO` | Int32 | R | `-100000` |
| `UInt32_RO` | UInt32 | R | `100000` |
| `Int64_RO` | Int64 | R | `-1000000` |
| `UInt64_RO` | UInt64 | R | `1000000` |
| `Float_RO` | Float | R | `3.14` |
| `Double_RO` | Double | R | `3.141592653589793` |
| `String_RO` | String | R | `"Read Only String"` |
| `DateTime_RO` | DateTime | R | `2024-01-01T00:00:00Z` |
| `Guid_RO` | Guid | R | `72962B91-FA75-4AE6-8D28-B404DC7DAF63` |
| `ByteString_RO` | ByteString | R | `"ReadOnly"` (as bytes) |
| `XmlElement_RO` | XmlElement | R | `<readonly/>` |
| `NodeId_RO` | NodeId | R | `ns=1;s=readonly-nodeid` |
| `ExpandedNodeId_RO` | ExpandedNodeId | R | `ns=1;i=9999` |
| `StatusCode_RO` | StatusCode | R | `Good` |
| `QualifiedName_RO` | QualifiedName | R | `1:ReadOnly` |
| `LocalizedText_RO` | LocalizedText | R | `en: "Read Only"` |

## 3. Arrays (Read/Write)

Path: `TestServer > DataTypes > Array`

20 array variables with `valueRank = 1`. All are readable and writable.

| BrowseName | Element Type | Access | Initial Value |
|---|---|---|---|
| `BooleanArray` | Boolean[] | RW | `[true, false, true, false]` |
| `SByteArray` | SByte[] | RW | `[-128, -1, 0, 1, 127]` |
| `ByteArray` | Byte[] | RW | `[0, 42, 128, 255]` |
| `Int16Array` | Int16[] | RW | `[-32768, -1, 0, 1, 32767]` |
| `UInt16Array` | UInt16[] | RW | `[0, 1000, 50000, 65535]` |
| `Int32Array` | Int32[] | RW | `[-100000, -1, 0, 1, 100000]` |
| `UInt32Array` | UInt32[] | RW | `[0, 100000, 1000000, 4294967295]` |
| `Int64Array` | Int64[] | RW | `[0, -1000000, 1000000]` |
| `UInt64Array` | UInt64[] | RW | `[0, 1000000, 999999999]` |
| `FloatArray` | Float[] | RW | `[1.1, 2.2, 3.3, 4.4]` |
| `DoubleArray` | Double[] | RW | `[1.11, 2.22, 3.33, 4.44, 5.55]` |
| `StringArray` | String[] | RW | `["Hello", "OPC", "UA", "World"]` |
| `DateTimeArray` | DateTime[] | RW | `[2024-01-01, 2024-06-15, now]` |
| `GuidArray` | Guid[] | RW | `[72962B91-..., A1B2C3D4-...]` |
| `ByteStringArray` | ByteString[] | RW | `["First", "Second", "Third"]` |
| `XmlElementArray` | XmlElement[] | RW | `["<a/>", "<b/>", "<c/>"]` |
| `NodeIdArray` | NodeId[] | RW | `[ns=1;s=arr-0, ns=1;s=arr-1]` |
| `StatusCodeArray` | StatusCode[] | RW | `[Good, BadInternalError, UncertainLastUsableValue]` |
| `QualifiedNameArray` | QualifiedName[] | RW | `[1:QN_A, 1:QN_B]` |
| `LocalizedTextArray` | LocalizedText[] | RW | `[en:Hello, it:Ciao, de:Hallo]` |

**Testing notes:**
- Write a different-length array, read it back
- Test boundary values (min/max for numeric types)
- UInt32Array includes `4294967295` (max UInt32)

## 4. Read-Only Arrays

Path: `TestServer > DataTypes > Array > ReadOnly`

6 read-only arrays for testing write rejection on array types.

| BrowseName | Element Type | Access | Value |
|---|---|---|---|
| `BooleanArray_RO` | Boolean[] | R | `[true, false]` |
| `Int32Array_RO` | Int32[] | R | `[1, 2, 3]` |
| `DoubleArray_RO` | Double[] | R | `[1.1, 2.2, 3.3]` |
| `StringArray_RO` | String[] | R | `["A", "B", "C"]` |
| `ByteArray_RO` | Byte[] | R | `[0, 127, 255]` |
| `DateTimeArray_RO` | DateTime[] | R | `[2024-01-01, 2024-06-15]` |

## 5. Empty Arrays

Path: `TestServer > DataTypes > Array > Empty`

14 arrays initialized to `[]` (empty). All are read/write.

| BrowseName | Element Type |
|---|---|
| `EmptyBooleanArray` | Boolean[] |
| `EmptySByteArray` | SByte[] |
| `EmptyByteArray` | Byte[] |
| `EmptyInt16Array` | Int16[] |
| `EmptyUInt16Array` | UInt16[] |
| `EmptyInt32Array` | Int32[] |
| `EmptyUInt32Array` | UInt32[] |
| `EmptyInt64Array` | Int64[] |
| `EmptyUInt64Array` | UInt64[] |
| `EmptyFloatArray` | Float[] |
| `EmptyDoubleArray` | Double[] |
| `EmptyStringArray` | String[] |
| `EmptyDateTimeArray` | DateTime[] |
| `EmptyByteStringArray` | ByteString[] |

**Testing notes:**
- Read should return an array with 0 elements
- Write some elements, then read back to verify
- Test that your client handles empty arrays without errors

## 6. Multi-Dimensional Arrays

Path: `TestServer > DataTypes > MultiDimensional`

3 matrices with `valueRank > 1`. Data is stored as a flat array with `arrayDimensions` metadata.

| BrowseName | DataType | Dimensions | Access | Description |
|---|---|---|---|---|
| `Matrix2D_Double` | Double | 3x3 | RW | `[[1,2,3],[4,5,6],[7,8,9]]` stored as flat array (9 elements) |
| `Matrix2D_Int32` | Int32 | 2x4 | RW | `[[1,2,3,4],[5,6,7,8]]` stored as flat array |
| `Cube3D_Byte` | Byte | 2x3x4 | RW | Values `0..23` stored as flat array |

**Testing notes:**
- The `arrayDimensions` attribute tells you the shape
- `valueRank = 2` means 2D matrix, `valueRank = 3` means 3D
- Data is row-major: for a 3x3 matrix, indices go `[0,0], [0,1], [0,2], [1,0], ...`

## 7. Analog Data Items (Variables with Range)

Path: `TestServer > DataTypes > WithRange`

Variables using the `AnalogDataItem` type, which includes `EURange` and `InstrumentRange` properties.

| BrowseName | DataType | Access | Value | EURange | InstrumentRange |
|---|---|---|---|---|---|
| `Temperature` | Double | RW | `~22.5` | [-40, 120] | [-50, 150] |
| `Pressure` | Double | RW | `~101.325` | [0, 500] | [0, 600] |
| `ReadOnlyValue` | Double | R | `42.0` | -- | -- |

**Properties of AnalogDataItem:**
- **EURange** (Engineering Units Range): the normal operating range. Values outside this range may indicate an abnormal condition.
- **InstrumentRange**: the absolute physical limits of the sensor/instrument. Values outside this range are impossible.

**Testing notes:**
- Browse `Temperature` node and read its `EURange` property
- `EURange` has two fields: `Low` and `High`
- Write a value outside EURange (e.g., `200.0`) -- the server accepts it (no enforcement), but your client may want to flag it

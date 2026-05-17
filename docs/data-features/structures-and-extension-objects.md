---
eyebrow: 'Docs · Data features'
lede:    'Two ways the suite exposes structured data: as plain Object nodes with child variables (Structures) and as binary-encoded ExtensionObjects (ExtensionObjects). Same shapes, different encodings, different test surfaces.'

see_also:
  - { href: './scalar-types.md',     meta: '5 min' }
  - { href: './arrays-and-matrices.md', meta: '4 min' }
  - { href: '../runtime-features/methods.md',  meta: '5 min' }

prev: { label: 'Arrays and matrices', href: './arrays-and-matrices.md' }
next: { label: 'Analog items',        href: './access-control.md' }
---

# Structures and extension objects

The suite exposes structured data in **two distinct styles**.
Both end up describing similar shapes (3D points, ranges,
records) but exercise different parts of an OPC UA client.

| Approach           | Encoding                                | Browse                  | Folder                              |
| ------------------ | --------------------------------------- | ----------------------- | ----------------------------------- |
| Structures         | Object node + child variables            | Multi-step browse        | `TestServer/Structures`             |
| Extension objects  | Single binary `ExtensionObject` value     | Browse type definition   | `TestServer/ExtensionObjects`       |

## Structures (Object + child variables)

Path: `TestServer / Structures`

Plain OPC UA pattern — an Object node with named children. Each
field is a separate Variable node that you read independently.

### TestPoint

```text
Structures/TestPoint/
├── X  (Double, RW, 1.0)
├── Y  (Double, RW, 2.0)
└── Z  (Double, RW, 3.0)
```

### TestRange

```text
Structures/TestRange/
├── Min    (Double, RW, 0.0)
├── Max    (Double, RW, 100.0)
└── Value  (Double, RW, 50.0)
```

### TestPerson

```text
Structures/TestPerson/
├── Name    (String, RW, "John Doe")
├── Age     (UInt32, RW, 30)
└── Active  (Boolean, RW, true)
```

### TestNested

A structure containing another structure (1-level nesting):

```text
Structures/TestNested/
├── Label       (String, RW, "origin")
├── Timestamp   (DateTime, RW, startup time)
└── Point       (Object)
    ├── X       (Double, RW, 10.0)
    ├── Y       (Double, RW, 20.0)
    └── Z       (Double, RW, 30.0)
```

`Point` is reached via the `HasComponent` reference. The browse
needs two hops from `TestNested` to read `X`/`Y`/`Z`.

### PointCollection

Five identically-shaped `Point_N` objects. The initial coordinate
values are seeded with `new Random(42)` and assigned by repeated
`rng.NextDouble() * 100` calls in `StructuresBuilder.cs`. Because
the seed is fixed, the values are deterministic across server
restarts, but they are **not** the round multiples sometimes
documented in test specs — they fall somewhere in
`[0, 100)` for each coordinate. Read them to discover the actual
values for your environment, or treat them as opaque test data
and only assert structural shape:

```text
Structures/PointCollection/
├── Point_0 → (X, Y, Z each a Double in [0, 100))
├── Point_1 → (X, Y, Z each a Double in [0, 100))
├── Point_2 → (X, Y, Z each a Double in [0, 100))
├── Point_3 → (X, Y, Z each a Double in [0, 100))
└── Point_4 → (X, Y, Z each a Double in [0, 100))
```

Useful for testing "browse all children, then read all
descendants" patterns.

### DeepNesting

A chain of 10 nested objects:

```text
Structures/DeepNesting/
└── Level_1 (Depth=1, Name="Level 1")
    └── Level_2 (Depth=2, Name="Level 2")
        └── Level_3 (Depth=3, Name="Level 3")
            └── …
                └── Level_10 (Depth=10, Name="Level 10")
```

Each level has a `Depth` (UInt32) and `Name` (String).

| Use                                  | Why                                    |
| ------------------------------------ | -------------------------------------- |
| Browse-depth limits                  | Reaches `Level_10` is 11+ hops from `Objects` |
| Recursive browse safety              | Verify no infinite-loop on deep trees  |
| Continuation-point handling          | Tune `MaxBrowseRefsPerNode` lower; observe |

## Extension objects (binary-encoded)

Path: `TestServer / ExtensionObjects`

Two variables whose Value attribute is a **binary `ExtensionObject`**
— a single typed blob with all fields encoded together. The types
are defined in the third namespace
(`urn:opcua:test-server:custom-types`); resolve its index from
`Server.NamespaceArray` at runtime — it has historically been
`ns=3` but is not pinned by the SDK.

In the table below `<ct>` is that resolved index. The DataType
node uses one numeric id and the `Default Binary` encoding node
uses a different one — `ExtensionObject` values carry the
**encoding** NodeId in their `typeId` field.

| Variable      | DataType (TypeDefinition) | `Default Binary` encoding NodeId | Initial                         | Access |
| ------------- | ------------------------- | --------------------------------- | -------------------------------- | ------ |
| `PointValue`  | `ns=<ct>;i=3000` (`TestPointXYZ`)    | `ns=<ct>;i=3010`        | `{X:1.5, Y:2.5, Z:3.5}`          | RW     |
| `RangeValue`  | `ns=<ct>;i=3001` (`TestRangeStruct`) | `ns=<ct>;i=3011`        | `{Min:0.0, Max:100.0, Value:42.5}` | R    |

### Binary body

Both types are 3 × 8 bytes (3 IEEE-754 doubles, little-endian):

```text
PointValue body (24 bytes):
  X (8 bytes) | Y (8 bytes) | Z (8 bytes)
  TypeId (encoding):  ns=<ct>;i=3010

RangeValue body (24 bytes):
  Min (8 bytes) | Max (8 bytes) | Value (8 bytes)
  TypeId (encoding):  ns=<ct>;i=3011
```

### Reading

```text
ev = read(node)
assert ev.typeId == NodeId(custom_types_ns, 3010)
fields = decode(ev.body, TestPointXYZ)
assert fields.X == 1.5
```

### Writing

```text
body = encode({X: 10.0, Y: 20.0, Z: 30.0}, TestPointXYZ)
write(node, ExtensionObject(typeId=NodeId(custom_types_ns, 3010), body=body))
```

### Type discovery

The DataType node is reachable via the custom-types namespace:

```text
Browse the custom-types namespace → find TestPointXYZ DataType node (i=3000)
                                 → child encoding `Default Binary` (i=3010)
```

The server registers `DataTypeState` nodes with
`SuperTypeId = DataTypeIds.Structure`. There is no
`DataTypeDefinition` attribute populated, so type-dictionary-only
clients will need to know the layout (`3 × Double`, little-endian)
out of band.

### Read-only RangeValue

`RangeValue` is `R` only. Writes return `Bad_NotWritable`.

## Difference recap

| Feature              | Structures                       | Extension objects                  |
| -------------------- | -------------------------------- | ---------------------------------- |
| Storage              | One Variable per field           | One Variable holding all fields    |
| Read                 | One read per field               | One read returns all fields        |
| Write                | One write per field              | One write sets all fields          |
| Encoding             | Plain typed Values                | Binary-encoded ExtensionObject     |
| Browse target        | Children of an Object node       | DataType definition in ns=3        |
| Atomic update?       | No (field-by-field)              | Yes (one value)                    |
| Test goal            | Browse hierarchies                | Binary encoding round-trip         |

## Test patterns

### Structures

- Browse `TestNested` → expect 2 vars + 1 object.
- Browse `PointCollection` → expect 5 objects with same shape.
- Recursive browse from `DeepNesting` → reach `Level_10`.

### Extension objects

- Read `PointValue` → verify the `typeId` matches the resolved
  encoding NodeId (`i=3010` in the custom-types namespace) and
  decode the body.
- Write a new value → read back → verify round-trip.
- Read `RangeValue` → assert `Bad_NotWritable` on write.
- Browse the custom-types namespace → discover `TestPointXYZ`
  (`i=3000`) and `TestRangeStruct` (`i=3001`) as DataType nodes.

## Where to read next

- [Access control](./access-control.md) — the 50 access-mode test
  variables.
- [Methods](../runtime-features/methods.md) — methods that act on
  structures and arrays.

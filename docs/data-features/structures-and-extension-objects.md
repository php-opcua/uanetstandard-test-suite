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
    ├── X       (Double, RW, 0.0)
    ├── Y       (Double, RW, 0.0)
    └── Z       (Double, RW, 0.0)
```

`Point` is reached via the `HasComponent` reference. The browse
needs two hops from `TestNested` to read `X`/`Y`/`Z`.

### PointCollection

Five identically-shaped `Point_N` objects:

```text
Structures/PointCollection/
├── Point_0 → (X=0,   Y=0,   Z=0  )
├── Point_1 → (X=10,  Y=20,  Z=30 )
├── Point_2 → (X=20,  Y=40,  Z=60 )
├── Point_3 → (X=30,  Y=60,  Z=90 )
└── Point_4 → (X=40,  Y=80,  Z=120)
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
— a single typed blob with all fields encoded together. Defined in
the `ns=3` namespace.

| Variable      | TypeId          | Encoding TypeId | Initial               | Access |
| ------------- | --------------- | --------------- | --------------------- | ------ |
| `PointValue`  | `ns=3;i=3010` (TestPointXYZ) | `ns=3;i=...` | `{X:1.5, Y:2.5, Z:3.5}` | RW    |
| `RangeValue`  | `ns=3;i=3011` (TestRangeStruct) | `ns=3;i=...` | `{Min:0.0, Max:100.0, Value:42.5}` | R |

### Binary body

Both types are 3 × 8 bytes (3 IEEE-754 doubles, little-endian):

```text
PointValue body (24 bytes):
  X (8 bytes) | Y (8 bytes) | Z (8 bytes)
  TypeId:  ns=3;i=3010

RangeValue body (24 bytes):
  Min (8 bytes) | Max (8 bytes) | Value (8 bytes)
  TypeId:  ns=3;i=3011
```

### Reading

```text
ev = read(node)
assert ev.typeId == NodeId("ns=3;i=3010")
fields = decode(ev.body, TestPointXYZ)
assert fields.X == 1.5
```

### Writing

```text
body = encode({X: 10.0, Y: 20.0, Z: 30.0}, TestPointXYZ)
write(node, ExtensionObject(typeId="ns=3;i=3010", body=body))
```

### Type discovery

The type's structure is reachable via the namespace:

```text
Browse ns=3 → find TestPointXYZ DataType node
Read DataTypeDefinition attribute → field list (X, Y, Z, all Double)
```

A type-dictionary-aware client should be able to decode the
binary without manual configuration.

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

- Read `PointValue` → verify `typeId` and decode body.
- Write a new value → read back → verify round-trip.
- Read `RangeValue` → assert `Bad_NotWritable` on write.
- Browse `ns=3` → discover `TestPointXYZ` and `TestRangeStruct`.

## Where to read next

- [Access control](./access-control.md) — the 50 access-mode test
  variables.
- [Methods](../runtime-features/methods.md) — methods that act on
  structures and arrays.

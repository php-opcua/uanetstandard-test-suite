# Structures

Path: `Objects > TestServer > Structures`

Structured objects represented as OPC UA Object nodes with child Variable nodes. These test browsing, hierarchical navigation, and nested object handling.

## TestPoint

Path: `Structures/TestPoint`

A simple 3D point with X, Y, Z coordinates.

| BrowsePath | DataType | Access | Initial Value |
|---|---|---|---|
| `TestPoint/X` | Double | RW | `1.0` |
| `TestPoint/Y` | Double | RW | `2.0` |
| `TestPoint/Z` | Double | RW | `3.0` |

## TestRange

Path: `Structures/TestRange`

A range definition with min, max, and current value.

| BrowsePath | DataType | Access | Initial Value |
|---|---|---|---|
| `TestRange/Min` | Double | RW | `0.0` |
| `TestRange/Max` | Double | RW | `100.0` |
| `TestRange/Value` | Double | RW | `50.0` |

## TestPerson

Path: `Structures/TestPerson`

A person record with mixed data types.

| BrowsePath | DataType | Access | Initial Value |
|---|---|---|---|
| `TestPerson/Name` | String | RW | `"John Doe"` |
| `TestPerson/Age` | UInt32 | RW | `30` |
| `TestPerson/Active` | Boolean | RW | `true` |

## TestNested

Path: `Structures/TestNested`

A structure containing another structure (nested object). Tests hierarchical browsing.

```
TestNested
├── Label      (String)    = "origin"
├── Timestamp  (DateTime)  = startup time
└── Point      (Object)
    ├── X      (Double)    = 0.0
    ├── Y      (Double)    = 0.0
    └── Z      (Double)    = 0.0
```

| BrowsePath | DataType | Access | Initial Value |
|---|---|---|---|
| `TestNested/Label` | String | RW | `"origin"` |
| `TestNested/Timestamp` | DateTime | RW | Server startup time |
| `TestNested/Point/X` | Double | RW | `0.0` |
| `TestNested/Point/Y` | Double | RW | `0.0` |
| `TestNested/Point/Z` | Double | RW | `0.0` |

**Testing notes:**
- Browse `TestNested` -- should find 2 variables (`Label`, `Timestamp`) and 1 object (`Point`)
- Browse `Point` -- should find 3 variables (`X`, `Y`, `Z`)
- The `Point` node uses `HasComponent` reference type

## PointCollection (5 Objects)

Path: `Structures/PointCollection`

5 point objects with the same structure, useful for testing enumeration and batch operations.

| Object | X | Y | Z |
|---|---|---|---|
| `Point_0` | 0.0 | 0.0 | 0.0 |
| `Point_1` | 10.0 | 20.0 | 30.0 |
| `Point_2` | 20.0 | 40.0 | 60.0 |
| `Point_3` | 30.0 | 60.0 | 90.0 |
| `Point_4` | 40.0 | 80.0 | 120.0 |

Each point is an Object node with 3 Double variables (`X`, `Y`, `Z`).

**Testing notes:**
- Browse `PointCollection` -- should find 5 objects
- All objects have the same structure (same child variable names and types)
- Useful for testing: "browse all children, then read all variables of each child"

## DeepNesting (10 Levels)

Path: `Structures/DeepNesting`

A chain of 10 nested objects, each containing the next one. Tests recursive browsing and depth limits.

```
DeepNesting
└── Level_1         (Depth=1, Name="Level 1")
    └── Level_2     (Depth=2, Name="Level 2")
        └── Level_3 (Depth=3, Name="Level 3")
            └── ...
                └── Level_10 (Depth=10, Name="Level 10")
```

Each level has:
- `Depth` (UInt32) -- the nesting level (1-10)
- `Name` (String) -- `"Level N"`

**Testing notes:**
- Total nodes in the chain: 10 objects + 20 variables = 30 nodes
- To reach `Level_10`, you need to browse 10 levels deep from `DeepNesting`
- Tests browse depth limits and recursive algorithms
- Verify your client doesn't stack overflow or infinite-loop on deep hierarchies
- The `Organizes` reference type is used between levels

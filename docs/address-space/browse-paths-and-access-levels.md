---
eyebrow: 'Docs · Address space'
lede:    'How to write a stable BrowsePath in your tests, and the legend for the access-level abbreviations used across the rest of the docs.'

see_also:
  - { href: './overview.md',                          meta: '4 min' }
  - { href: '../data-features/access-control.md',     meta: '4 min' }
  - { href: '../runtime-features/historical-data.md', meta: '5 min' }

prev: { label: 'Overview',                  href: './overview.md' }
next: { label: 'Scalar types',              href: '../data-features/scalar-types.md' }
---

# Browse paths and access levels

## BrowsePaths over NodeIds

NodeIds in `ns=1` use string identifiers — those are stable
across restarts:

```text
ns=1;s=TestServer/DataTypes/Scalar/BooleanValue
```

But many tests are more readable using **BrowsePaths**:

```text
[
  { ReferenceTypeId: HierarchicalReferences, TargetName: "TestServer" },
  { ReferenceTypeId: HierarchicalReferences, TargetName: "DataTypes" },
  { ReferenceTypeId: HierarchicalReferences, TargetName: "Scalar" },
  { ReferenceTypeId: HierarchicalReferences, TargetName: "BooleanValue" },
]
```

The `TranslateBrowsePathsToNodeIds` service converts these to
NodeIds at runtime. Both work; pick the style your library
expresses cleanly.

For the rest of the docs, paths are shown in shorthand:

```text
TestServer/DataTypes/Scalar/BooleanValue
```

…meaning the BrowsePath starting from `Objects`, going through
each named child. Translating that to a NodeId in `ns=1` adds the
`ns=1;s=` prefix.

## Reference types used

| ReferenceType        | Where it appears                                       |
| -------------------- | ------------------------------------------------------ |
| `Organizes`          | Folder → folder, folder → leaf                          |
| `HasComponent`       | Object → child variable / child object                  |
| `HasProperty`        | Variable → properties (EURange, InputArguments, …)      |
| `HasTypeDefinition`  | Instance → its TypeDefinition                           |
| `GeneratesEvent`     | Object → event type (EventEmitter)                      |
| `HasSubtype`         | Type hierarchy (in `ns=0`)                              |
| `HasEncoding`        | DataType → Default Binary encoding (ExtensionObjects)   |

For deep-nested structures (`Structures/DeepNesting`), the
`Organizes` reference is used between levels.

## Access-level legend

OPC UA's `accessLevel` attribute (id 17) is a byte bitmask:

| Bit | Name              | Meaning                                |
| --- | ----------------- | -------------------------------------- |
| 0   | `CurrentRead`     | Read attribute Value                   |
| 1   | `CurrentWrite`    | Write attribute Value                  |
| 2   | `HistoryRead`     | HistoryRead service supported          |
| 3   | `HistoryWrite`    | HistoryWrite service supported (rare)   |
| 4   | `SemanticChange`  | Value changes meaning over time         |
| 5   | `StatusWrite`     | Status code can be written             |
| 6   | `TimestampWrite`  | Source timestamp can be written         |

`userAccessLevel` (id 18) is the same byte but **scoped to the
calling user** — may be more restrictive than `accessLevel`.

## Shorthand used in the docs

| Shorthand | Maps to                                            |
| --------- | -------------------------------------------------- |
| `R`       | `CurrentRead`                                      |
| `W`       | `CurrentWrite`                                     |
| `RW`      | `CurrentRead + CurrentWrite`                       |
| `WO`      | `CurrentWrite` (at userAccessLevel; readable at accessLevel) |
| `HR`      | `HistoryRead`                                      |
| `R+HR`    | `CurrentRead + HistoryRead`                        |
| `RW+HR`   | `CurrentRead + CurrentWrite + HistoryRead`         |

These are used throughout the **Data features** and **Runtime
features** sections.

## How to read the access-level attribute

Two attributes, two reads:

```text
read(node, AttributeId.AccessLevel)     → byte bitmask
read(node, AttributeId.UserAccessLevel) → byte bitmask
```

Comparing them tells you whether the **user** has the same
access as the **node** affords. Useful when writing role-aware
client logic.

For systematic verification, see [Access control](../data-features/access-control.md) —
the suite has 50 variables that cover every combination, ready
to exercise this attribute pair.

## Browse depth

The deepest natural path is in `Structures/DeepNesting`, where
each level recursively contains the next:

```text
TestServer / Structures / DeepNesting /
  Level_1 / Level_2 / Level_3 / Level_4 / Level_5 /
  Level_6 / Level_7 / Level_8 / Level_9 / Level_10
```

That's 13 hops from `Objects`. Useful to test browse-depth limits
in your client.

## Hierarchical vs aggregating browses

A "hierarchical" reference is `Organizes`, `HasComponent`, or
`HasChild`. Most browses use `BrowseDirection.Forward` with
`ReferenceTypeId = HierarchicalReferences` and `IncludeSubtypes = true`.

For type-aware browses (e.g., "find all `BaseDataVariableType`
instances"), you'd use `ReferenceTypeId = HasTypeDefinition` and
`BrowseDirection.Inverse`. The suite's test surface doesn't
require this — most tests stick with the default hierarchical
forward browse.

## ContinuationPoints

For very wide browses (large collections), some servers return
results in chunks with a `ContinuationPoint`. The suite's largest
collections are:

| Collection                            | Approx node count |
| ------------------------------------- | ----------------- |
| `DataTypes/Scalar` children            | 21               |
| `DataTypes/Array` children             | 20               |
| `AccessControl/AllCombinations`        | 32               |
| `Structures/DeepNesting/Level_1` chain | 10 deep          |

None of these are large enough to trigger continuation in
UA-.NETStandard's default config. For continuation-handling
tests, set `MaxBrowseReferencesPerNode` low (via config), or
test against the largest known collection.

## Where to read next

- The per-feature pages under **Data features** and **Runtime
  features** for what's inside each top-level folder.

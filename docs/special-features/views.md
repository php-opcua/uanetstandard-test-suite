---
eyebrow: 'Docs · Special features'
lede:    'Four OPC UA views, each a filtered perspective of the address space. Useful for exercising view-aware browse logic in your client.'

see_also:
  - { href: '../address-space/overview.md',             meta: '5 min' }
  - { href: '../address-space/browse-paths-and-access-levels.md', meta: '4 min' }

prev: { label: 'Historical data',         href: '../runtime-features/historical-data.md' }
next: { label: 'Security Key Service',    href: './security-key-service.md' }
---

# Views

Path: `Views` (standard OPC UA folder, `ns=0;i=87`)

Four views are defined. Each one is a NodeId you can pass in
the `View` parameter of a `Browse` or `BrowseNext` request — the
server then filters the browse to only that view's members.

## The views

| BrowseName        | Contains (target folders)                                | Use case                                |
| ----------------- | -------------------------------------------------------- | --------------------------------------- |
| `OperatorView`    | `TestServer/Dynamic`, `TestServer/Methods`, `TestServer/Alarms` | Operator dashboards                   |
| `EngineeringView` | `TestServer` (the whole subtree)                         | Full engineering / debug access         |
| `HistoricalView`  | `TestServer/Historical`                                  | Trend / history clients                 |
| `DataView`        | `TestServer/DataTypes`, `TestServer/Structures`, `TestServer/ExtensionObjects` | Data inspection / configuration |

## Why views

Views are how OPC UA servers offer **role-specific** or
**purpose-specific** subsets of their address space. An operator
UI doesn't need to see `AccessControl/AllCombinations`; an
engineering tool wants everything.

The test suite includes views so your client can exercise the
view-passing path of the Browse service.

## How to use a view

When calling `Browse` / `BrowseNext`, pass the view's NodeId in
the `View` field of the `BrowseDescription`:

```text
BrowseDescription {
    nodeId:       <starting node>,
    view: {
        viewId:    <OperatorView NodeId>,
        timestamp: null,
        viewVersion: 0,
    },
    referenceTypeId: HierarchicalReferences,
    includeSubtypes: true,
    browseDirection: Forward,
    nodeClassMask:  0,
    resultMask:     0x3F,
}
```

The server returns only references **that belong to** the
selected view.

## Finding the view NodeIds

```text
Browse(nodeId=Views (ns=0;i=87)) → 4 child nodes
```

Each child is one of the views. Read its `BrowseName` to
identify it:

| BrowseName        | NodeId pattern        |
| ----------------- | --------------------- |
| `OperatorView`    | `ns=1;s=Views/OperatorView`     |
| `EngineeringView` | `ns=1;s=Views/EngineeringView`  |
| `HistoricalView`  | `ns=1;s=Views/HistoricalView`   |
| `DataView`        | `ns=1;s=Views/DataView`         |

(The exact string-id may differ in different builds — read the
`BrowseName` rather than hard-coding.)

## Test patterns

### View-vs-no-view comparison

Browse `TestServer` **without** a view:

```text
result = [DataTypes, Methods, Dynamic, Events, Alarms,
          Historical, Structures, ExtensionObjects, AccessControl]
```

Browse `TestServer` with `OperatorView`:

```text
result = [Dynamic, Methods, Alarms]
```

The view filters the result.

### View isolation

Browse with `HistoricalView`:

- From `TestServer` → only `Historical` shows up.
- From `TestServer/Historical` → all 4 historical variables show up.
- Navigating to `DataTypes` with this view: the browse won't
  return it.

This exercises the "view restricts visibility" path.

### View completeness

Browse with `EngineeringView` recursively from `TestServer`.
Result should match a non-view recursive browse — `EngineeringView`
contains everything.

This catches regressions where a view-aware browse accidentally
misses nodes.

### View per role

The combination of views + user roles is a useful
defense-in-depth pattern. The suite doesn't enforce
"viewer-can-only-browse-OperatorView" — that's up to your
client. But you can test the **combination** by:

1. Connect as `viewer` to `opcua-userpass`.
2. Browse with `OperatorView`.
3. Verify the read scope is the intersection (read-only access
   to operator-relevant nodes).

## View versions

The `viewVersion` field in `BrowseDescription` lets a server
signal that a view has been updated and old cached results are
stale. The suite uses a static `viewVersion = 0` — views never
update. Real servers may increment this.

## Where to read next

- [Security Key Service](./security-key-service.md) — the SKS
  feature.
- [Address space · Overview](../address-space/overview.md) — what
  the views are filtering.

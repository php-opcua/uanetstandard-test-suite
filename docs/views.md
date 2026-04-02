# Views

Path: `Views` (standard OPC UA Views folder, `ns=0;i=87`)

4 OPC UA views that provide filtered perspectives of the address space. Views allow clients to browse only a subset of nodes relevant to a specific use case.

## Available Views

| View BrowseName | Contents | Use Case |
|---|---|---|
| `OperatorView` | Dynamic, Methods, Alarms | Day-to-day operations monitoring |
| `EngineeringView` | Everything (TestServer root) | Full engineering access |
| `HistoricalView` | Historical | Trend analysis and data review |
| `DataView` | DataTypes, Structures | Data inspection and configuration |

## OperatorView

Contains the nodes most relevant to a plant operator:
- **Dynamic** -- live process values (counters, waveforms, status)
- **Methods** -- callable actions
- **Alarms** -- active alarm conditions

Excludes: raw data types, structures, historical data, access control test nodes.

## EngineeringView

Contains the entire `TestServer` folder. Equivalent to browsing without a view, but structured as a view for testing view-based browsing.

## HistoricalView

Contains only the `Historical` folder with the 4 historical variables. Useful for clients that need to focus on trend data.

## DataView

Contains:
- **DataTypes** -- all scalar, array, and matrix variables
- **Structures** -- all structured objects

Excludes: dynamic variables, methods, events, alarms.

## How to Use Views

### Browsing with a view

When calling the `Browse` or `BrowseNext` service, pass the view's NodeId in the `View` parameter of the `BrowseDescription`:

```
BrowseDescription {
    NodeId: <starting node>,
    View: {
        ViewId: <OperatorView NodeId>,
        Timestamp: null,
        ViewVersion: 0
    },
    ...
}
```

Only nodes that belong to the specified view will be returned.

### Finding view NodeIds

1. Browse the `Views` folder (`ns=0;i=87`)
2. The child nodes are the 4 views
3. Each view has a NodeId (auto-generated, `ns=1;i=XXXX`)
4. Use that NodeId in subsequent browse calls

## Testing Views

### Basic view browsing

1. Browse the `Views` folder -> find 4 view nodes
2. Read each view's `BrowseName` attribute to identify them
3. Browse from `Objects` using `OperatorView` -> should only see `Dynamic`, `Methods`, `Alarms` under `TestServer`

### View vs no-view comparison

1. Browse `TestServer` without a view -> see all 8+ sub-folders
2. Browse `TestServer` with `OperatorView` -> see only 3 sub-folders
3. Browse `TestServer` with `DataView` -> see only 2 sub-folders

### Verifying view isolation

1. Browse with `HistoricalView` starting from `TestServer`
2. Try to navigate to `DataTypes` -> should not be reachable
3. Navigate to `Historical` -> should work, with all 4 variables visible

### View completeness

1. Browse with `EngineeringView` from `TestServer`
2. Recursively browse all nodes -> should match the result of browsing without any view

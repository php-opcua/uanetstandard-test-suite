---
eyebrow: 'Docs · Customization'
lede:    'Fork the suite and add your own variables, methods, dynamic counters, or whole new builders. The code patterns the existing builders use.'

see_also:
  - { href: './adding-server-instances.md',     meta: '4 min' }
  - { href: './simulation-recipes.md',          meta: '5 min' }

prev: { label: 'Security tests',           href: '../testing-patterns/security-tests.md' }
next: { label: 'Adding server instances',  href: './adding-server-instances.md' }
---

# Forking and adding nodes

The suite is designed to be forked. Each feature group lives in
its own C# class under `src/TestServer/AddressSpace/` — you can
modify one without touching the others.

## Project layout

```text
src/TestServer/
├── Program.cs                  # Entry point
├── TestServer.csproj           # NuGet deps
├── Server/
│   ├── TestServerApp.cs        # Server bootstrap
│   └── TestNodeManager.cs      # Calls every builder
├── Configuration/
│   └── ServerConfig.cs         # Env-var → config
├── UserManagement/
│   └── UserManager.cs          # Username/password auth
└── AddressSpace/
    ├── DataTypesBuilder.cs     # Scalars + arrays
    ├── MethodsBuilder.cs       # 12 methods
    ├── DynamicBuilder.cs       # Time-varying variables
    ├── EventsAlarmsBuilder.cs  # Events + alarms
    ├── HistoricalBuilder.cs    # History recording
    ├── StructuresBuilder.cs    # Object hierarchies
    ├── ExtensionObjectsBuilder.cs  # Binary-encoded structs
    ├── AccessControlBuilder.cs # Access-mode test variables
    ├── ViewsBuilder.cs         # Views
    └── SecurityKeyServiceBuilder.cs  # SKS (opt-in)
```

Every builder follows the same shape:

```text
class FooBuilder {
    FooBuilder(IServerInternal server, ushort namespaceIndex);
    FolderState Build(FolderState rootFolder, ServerSystemContext context);
    void Stop();   // dispose timers
}
```

## Adding a single variable

The pattern any builder uses:

<!-- @code-block language="text" label="adding a variable" -->
```text
using Opc.Ua;

// inside a builder, given a parent folder:
var variable = new BaseDataVariableState<double>(parentFolder)
{
    NodeId       = new NodeId("MyVariable", NamespaceIndex),
    BrowseName   = new QualifiedName("MyVariable", NamespaceIndex),
    DisplayName  = "MyVariable",
    DataType     = DataTypeIds.Double,
    ValueRank    = ValueRanks.Scalar,
    AccessLevel  = AccessLevels.CurrentReadOrWrite,
    UserAccessLevel = AccessLevels.CurrentReadOrWrite,
    Value        = 42.0,
};

parentFolder.AddChild(variable);
AddPredefinedNode(SystemContext, variable);
```
<!-- @endcode-block -->

For read-only:

```text
AccessLevel = AccessLevels.CurrentRead;
UserAccessLevel = AccessLevels.CurrentRead;
```

## Adding a method

<!-- @code-block language="text" label="adding a method" -->
```text
var method = new MethodState(parentFolder)
{
    NodeId       = new NodeId("MyMethod", NamespaceIndex),
    BrowseName   = new QualifiedName("MyMethod", NamespaceIndex),
    DisplayName  = "MyMethod",
    Executable   = true,
    UserExecutable = true,
};

method.InputArguments = new PropertyState<Argument[]>(method)
{
    NodeId     = new NodeId("MyMethod_InputArgs", NamespaceIndex),
    BrowseName = BrowseNames.InputArguments,
    Value = new Argument[]
    {
        new Argument {
            Name = "input",
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
        },
    },
};

method.OutputArguments = new PropertyState<Argument[]>(method)
{
    NodeId     = new NodeId("MyMethod_OutputArgs", NamespaceIndex),
    BrowseName = BrowseNames.OutputArguments,
    Value = new Argument[]
    {
        new Argument {
            Name = "result",
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
        },
    },
};

method.OnCallMethod = (context, objectId, inputArgs, outputArgs) =>
{
    var input = (string)inputArgs[0].Value;
    outputArgs[0] = new Variant(input.ToUpper());
    return ServiceResult.Good;
};

parentFolder.AddChild(method);
AddPredefinedNode(SystemContext, method);
```
<!-- @endcode-block -->

## Adding a dynamic (timer-driven) variable

<!-- @code-block language="text" label="dynamic variable" -->
```text
private Timer _myTimer;
private uint _counter;

private void AddMyCounter(FolderState parent)
{
    var v = new BaseDataVariableState<uint>(parent)
    {
        NodeId       = new NodeId("MyCounter", NamespaceIndex),
        BrowseName   = new QualifiedName("MyCounter", NamespaceIndex),
        DisplayName  = "MyCounter",
        DataType     = DataTypeIds.UInt32,
        ValueRank    = ValueRanks.Scalar,
        AccessLevel  = AccessLevels.CurrentRead,
        UserAccessLevel = AccessLevels.CurrentRead,
        Value        = _counter,
    };

    parent.AddChild(v);
    AddPredefinedNode(SystemContext, v);

    _myTimer = new Timer(_ =>
    {
        _counter++;
        v.Value = _counter;
        v.Timestamp = DateTime.UtcNow;
        v.ClearChangeMasks(SystemContext, false);
    }, null, 1000, 1000);
}

public void Stop()
{
    _myTimer?.Dispose();
}
```
<!-- @endcode-block -->

Track every timer in a list — disposed in `Stop()`. Without
this, the daemon won't shut down cleanly.

## Adding a custom event type

<!-- @code-block language="text" label="custom event type" -->
```text
var motorFaultType = new BaseObjectTypeState()
{
    NodeId       = new NodeId("MotorFaultEventType", NamespaceIndex),
    BrowseName   = new QualifiedName("MotorFaultEventType", NamespaceIndex),
    DisplayName  = "MotorFaultEventType",
    SuperTypeId  = ObjectTypeIds.BaseEventType,
    IsAbstract   = false,
};

var motorIdProp = new PropertyState<string>(motorFaultType)
{
    NodeId       = new NodeId("MotorFaultEventType_MotorId", NamespaceIndex),
    BrowseName   = new QualifiedName("MotorId", NamespaceIndex),
    DataType     = DataTypeIds.String,
    ValueRank    = ValueRanks.Scalar,
};
motorFaultType.AddChild(motorIdProp);
```
<!-- @endcode-block -->

Then raise it the same way `EventsAlarmsBuilder` raises its
`SimpleEventType` — see that file for the pattern.

## Adding a user account

Edit `config/users.json`:

<!-- @code-block language="text" label="config/users.json" -->
```text
[
  { "username": "admin",     "password": "admin123",     "role": "admin"    },
  { "username": "operator",  "password": "operator123",  "role": "operator" },
  { "username": "viewer",    "password": "viewer123",    "role": "viewer"   },
  { "username": "test",      "password": "test",         "role": "admin"    },
  { "username": "plc_svc",   "password": "s3cure!",      "role": "operator" }
]
```
<!-- @endcode-block -->

Restart the server:

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose restart opcua-userpass
```
<!-- @endcode-block -->

For a non-default role, extend
`UserManager.GetUserRoles()` in
`src/TestServer/UserManagement/UserManager.cs`:

<!-- @code-block language="text" label="UserManager.cs" -->
```text
case "engineer":
    return new[] { "AuthenticatedUser", "Operator", "Engineer" };
case "supervisor":
    return new[] { "AuthenticatedUser", "Operator", "ConfigureAdmin" };
```
<!-- @endcode-block -->

## Creating a new address-space builder

Step-by-step for a whole new feature group.

### 1. Create the builder class

`src/TestServer/AddressSpace/MyModuleBuilder.cs`:

<!-- @code-block language="text" label="MyModuleBuilder.cs" -->
```text
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.AddressSpace;

public class MyModuleBuilder
{
    private readonly IServerInternal _server;
    private readonly ushort _namespaceIndex;
    private readonly List<Timer> _timers = new();

    public MyModuleBuilder(IServerInternal server, ushort namespaceIndex)
    {
        _server = server;
        _namespaceIndex = namespaceIndex;
    }

    public FolderState Build(FolderState root, ServerSystemContext context)
    {
        var folder = new FolderState(root)
        {
            NodeId       = new NodeId("MyModule", _namespaceIndex),
            BrowseName   = new QualifiedName("MyModule", _namespaceIndex),
            DisplayName  = "MyModule",
            TypeDefinitionId = ObjectTypeIds.FolderType,
        };
        root.AddChild(folder);

        // add your nodes here

        return folder;
    }

    public void Stop()
    {
        foreach (var t in _timers) t.Dispose();
        _timers.Clear();
    }
}
```
<!-- @endcode-block -->

### 2. Add a feature toggle (optional)

`src/TestServer/Configuration/ServerConfig.cs`:

<!-- @code-block language="text" label="ServerConfig.cs" -->
```text
public bool EnableMyModule { get; set; }
    = GetBoolEnv("OPCUA_ENABLE_MY_MODULE", true);
```
<!-- @endcode-block -->

### 3. Wire it into the node manager

`src/TestServer/Server/TestNodeManager.cs` — inside
`CreateAddressSpace()`:

<!-- @code-block language="text" label="TestNodeManager.cs" -->
```text
if (_config.EnableMyModule)
{
    Console.WriteLine("[AddressSpace] Building MyModule…");
    var builder = new MyModuleBuilder(Server, _namespaceIndex);
    builder.Build(rootFolder, SystemContext);
}
```
<!-- @endcode-block -->

### 4. Build and run

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose build
docker compose up -d
docker compose logs -f opcua-no-security
```
<!-- @endcode-block -->

You should see the `[AddressSpace] Building MyModule…` line in
the startup log.

## Tips

- **Keep builders independent.** Each one should only depend on
  the namespace index and the parent folder. Don't reference
  other builders' nodes directly.
- **Dispose every timer.** Track them in a list, dispose in
  `Stop()`. The daemon's shutdown depends on this.
- **Use `Console.WriteLine` with `[Tag]` prefix.** Easier to
  grep the logs.
- **Test against a single server first.** `docker compose up -d
  opcua-no-security` is faster than the whole suite.
- **Check the UA-.NETStandard samples.** The [OPC Foundation
  UA-.NETStandard repo](https://github.com/OPCFoundation/UA-.NETStandard)
  has reference code for every node type.

## Where to read next

- [Adding server instances](./adding-server-instances.md) — new
  Docker services with custom env vars.
- [Simulation recipes](./simulation-recipes.md) — example
  builders for PLC / HVAC / energy meter scenarios.

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

Every builder follows roughly this shape (compare with the
existing builders in the repo — exact signatures vary):

```csharp
class FooBuilder {
    // Constructor: pass the TestNodeManager (which exposes the helpers
    // CreateFolder / CreateVariable / CreateVariableUntyped /
    // CreateMethod and the NamespaceIndex), the root folder, and the
    // ISystemContext.
    public FooBuilder(TestNodeManager mgr, FolderState root, ISystemContext context);

    // Builders without timers expose a parameterless Build():
    public void Build();

    // Builders that drive timers take the manager's timer list so it
    // can dispose them on shutdown:
    public void Build(List<IDisposable> timers);
}
```

There is no `Stop()` method on builders today. Timers added to
`TestNodeManager._timers` (via `Build(timers)`) are disposed in
`TestNodeManager.Dispose(bool)`.

## Adding a single variable

The simplest pattern uses the helper that every existing builder
calls — `TestNodeManager.CreateVariable<T>(...)`:

<!-- @code-block language="csharp" label="adding a variable" -->
```csharp
// _mgr is the TestNodeManager passed to your builder's constructor.
var variable = _mgr.CreateVariable<double>(
    parentFolder,
    "TestServer/MyModule/MyVariable",     // path string used as the NodeId.s
    "MyVariable",                         // BrowseName / DisplayName
    DataTypeIds.Double,
    ValueRanks.Scalar,
    42.0);                                // initial value
```
<!-- @endcode-block -->

`CreateVariable<T>` defaults `AccessLevel` and `UserAccessLevel`
to `CurrentReadOrWrite`. To make the variable read-only, pass an
explicit `accessLevel`:

```csharp
_mgr.CreateVariable<double>(parent, path, name, DataTypeIds.Double,
    ValueRanks.Scalar, 42.0, AccessLevels.CurrentRead);
```

If you prefer to construct the `BaseDataVariableState` by hand
(for example to set extra fields like `Historizing`), call
`AddPredefinedNode(SystemContext, node)` via the
`TestNodeManager.AddNode` helper — `AddPredefinedNode` itself is
protected on the base class.

## Adding a method

The existing `MethodsBuilder` uses
`TestNodeManager.CreateMethod(parent, path, name, handler, inArgs, outArgs)`.
The handler receives raw `IList<object>` (no `Argument` wrapper);
index into them with the same casts the production methods use.

<!-- @code-block language="csharp" label="adding a method" -->
```csharp
_mgr.CreateMethod(
    parentFolder,
    "TestServer/MyModule/Uppercase",
    "Uppercase",
    (input, output) =>
    {
        var s = (string)input[0];
        output[0] = s.ToUpperInvariant();
    },
    new[]
    {
        new Argument
        {
            Name = "input",
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
            Description = new LocalizedText("en", "Input string"),
        },
    },
    new[]
    {
        new Argument
        {
            Name = "result",
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
            Description = new LocalizedText("en", "Upper-cased string"),
        },
    });
```
<!-- @endcode-block -->

`CreateMethod` wraps the handler in a try/catch that returns
`Bad_InternalError` on any uncaught exception. If you need a
specific `StatusCode`, throw `ServiceResultException(StatusCodes.Xxx, ...)`
**before** `CreateMethod` swallows the exception, or call
`output[0]` only after validating inputs and short-circuit by
throwing.

## Adding a dynamic (timer-driven) variable

<!-- @code-block language="csharp" label="dynamic variable" -->
```csharp
public void Build(List<IDisposable> timers)
{
    var v = _mgr.CreateVariable<uint>(
        parentFolder,
        "TestServer/MyModule/MyCounter",
        "MyCounter",
        DataTypeIds.UInt32,
        ValueRanks.Scalar,
        0u,
        AccessLevels.CurrentRead);

    uint counter = 0;

    timers.Add(new Timer(_ =>
    {
        counter++;
        v.Value = counter;
        v.Timestamp = DateTime.UtcNow;
        v.ClearChangeMasks(_context, false);
    }, null, 1000, 1000));
}
```
<!-- @endcode-block -->

Add the timer to the `List<IDisposable> timers` passed in by
`TestNodeManager`. The node manager keeps the list and disposes
every entry in `TestNodeManager.Dispose(bool)` — you do not need
your own `Stop()` method.

## Adding a custom event type

The shipped `EventsAlarmsBuilder` does **not** register any
custom event types — it constructs plain `BaseEventState`
instances and stamps them with `ObjectTypeIds.BaseEventType`
or `ObjectTypeIds.SystemEventType`. If you need a custom event
type, register a `BaseObjectTypeState` of your own, then point
your reported event's `EventType.Value` at its NodeId:

<!-- @code-block language="csharp" label="custom event type" -->
```csharp
var motorFaultType = new BaseObjectTypeState
{
    SymbolicName = "MotorFaultEventType",
    NodeId       = new NodeId("MotorFaultEventType", _mgr.NamespaceIndex),
    BrowseName   = new QualifiedName("MotorFaultEventType", _mgr.NamespaceIndex),
    DisplayName  = new LocalizedText("en", "MotorFaultEventType"),
    SuperTypeId  = ObjectTypeIds.BaseEventType,
    IsAbstract   = false,
};

var motorIdProp = new PropertyState<string>(motorFaultType)
{
    SymbolicName = "MotorId",
    NodeId       = new NodeId("MotorFaultEventType/MotorId", _mgr.NamespaceIndex),
    BrowseName   = new QualifiedName("MotorId", _mgr.NamespaceIndex),
    DataType     = DataTypeIds.String,
    ValueRank    = ValueRanks.Scalar,
};
motorFaultType.AddChild(motorIdProp);
_mgr.AddNode(_context, motorFaultType);

// Later, when raising an event from a timer:
// var e = new BaseEventState(emitter);
// e.Initialize(_context, emitter, EventSeverity.High, new LocalizedText("..."));
// e.EventType.Value = motorFaultType.NodeId;
// emitter.ReportEvent(_context, e);
```
<!-- @endcode-block -->

Note that selecting the `MotorId` field from a subscription's
`selectClause` requires emitting an event instance that actually
carries that field; `BaseEventState` will not — you would need a
generated subclass (e.g. via Opc.Ua model compiler) to expose
properties beyond the standard `BaseEventType` set.

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

For a non-default role, you have two extension points — neither
is `GetUserRoles()` (no such method exists):

1. Extend `UserManager` itself (`src/TestServer/UserManagement/UserManager.cs`)
   with a helper like `IsEngineer(username)` mirroring the
   existing `IsAdmin` / `IsOperator` predicates.
2. Wire the new role into the `switch` in
   `AccessControlBuilder.CreateRoleProtectedVariable` so write
   hooks know how to evaluate `minimumRole = "engineer"`.

<!-- @code-block language="csharp" label="UserManager.cs additions" -->
```csharp
public bool IsEngineer(string username)
    => GetRole(username) is "admin" or "engineer";

public bool IsSupervisor(string username)
    => GetRole(username) is "admin" or "supervisor";
```
<!-- @endcode-block -->

`AccessControlBuilder.cs`:

```csharp
var hasAccess = minimumRole switch
{
    "admin"      => _userManager.IsAdmin(username),
    "operator"   => _userManager.IsOperator(username),
    "engineer"   => _userManager.IsEngineer(username),
    "supervisor" => _userManager.IsSupervisor(username),
    _            => true,
};
```

The `permissions` array in `users.json` is exposed via
`UserManager.HasPermission(username, permission)` for tests that
want to gate on individual permission strings rather than on a
role bucket.

## Creating a new address-space builder

Step-by-step for a whole new feature group.

### 1. Create the builder class

`src/TestServer/AddressSpace/MyModuleBuilder.cs`:

<!-- @code-block language="csharp" label="MyModuleBuilder.cs" -->
```csharp
using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class MyModuleBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public MyModuleBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    // Use Build(List<IDisposable> timers) instead if you need timers.
    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/MyModule", "MyModule");

        // Example: one read-write Double variable.
        _mgr.CreateVariable<double>(folder,
            "TestServer/MyModule/MyValue", "MyValue",
            DataTypeIds.Double, ValueRanks.Scalar, 0.0);
    }
}
```
<!-- @endcode-block -->

### 2. Add a feature toggle (optional)

`src/TestServer/Configuration/ServerConfig.cs`. Add the field
with its default, then read it from the environment in
`FromEnvironment()`:

<!-- @code-block language="csharp" label="ServerConfig.cs" -->
```csharp
public bool EnableMyModule { get; set; } = true;

// inside FromEnvironment(), alongside the other GetEnvBool calls:
config.EnableMyModule = GetEnvBool("OPCUA_ENABLE_MY_MODULE", config.EnableMyModule);
```
<!-- @endcode-block -->

### 3. Wire it into the node manager

`src/TestServer/Server/TestNodeManager.cs` — inside
`CreateAddressSpace()`, follow the same pattern the existing
builders use:

<!-- @code-block language="csharp" label="TestNodeManager.cs" -->
```csharp
if (_config.EnableMyModule)
{
    var myModule = new MyModuleBuilder(this, root, SystemContext);
    myModule.Build();
    Console.WriteLine("  [+] MyModule address space built");
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

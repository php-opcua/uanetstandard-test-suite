# Customization Guide

How to fork this repository and build your own OPC UA test environment tailored to your specific needs.

## Getting Started

```bash
# Fork on GitHub, then clone your fork
git clone https://github.com/php-opcua/uanetstandard-test-suite.git
cd uanetstandard-test-suite
```

All source code lives in `src/TestServer/AddressSpace/`. Each file is an independent builder class that you can modify, replace, or use as a template for new builders.

## Project Structure

```
src/TestServer/
├── Program.cs                 Entry point — creates the server, handles shutdown
├── TestServer.csproj          Project file (NuGet dependencies)
├── Server/
│   ├── TestServerApp.cs       Server application setup
│   └── TestNodeManager.cs     Node manager — calls all builders, controls what gets built
├── Configuration/
│   └── ServerConfig.cs        Environment variable parsing
├── UserManagement/
│   └── UserManager.cs         Username/password authentication
└── AddressSpace/
    ├── DataTypesBuilder.cs    Scalar and array variables
    ├── MethodsBuilder.cs      Callable methods
    ├── DynamicBuilder.cs      Time-varying variables (timers)
    ├── EventsAlarmsBuilder.cs Event types and alarm instances
    ├── HistoricalBuilder.cs   Variables with history recording
    ├── StructuresBuilder.cs   Nested object hierarchies
    ├── ExtensionObjectsBuilder.cs  Custom structured types with binary encoding
    ├── AccessControlBuilder.cs     Access level and role-based variables
    └── ViewsBuilder.cs        OPC UA views
```

Every builder class contains a `Build()` method that creates its section of the address space. The `TestNodeManager` calls them based on config flags during `CreateAddressSpace()`.

## Common Tasks

### Adding a New Variable

Open any existing builder or create a new one. The pattern is always the same:

```csharp
using Opc.Ua;

// Inside your builder class
private void AddMyVariable(FolderState parentFolder)
{
    var variable = new BaseDataVariableState<double>(parentFolder)
    {
        NodeId = new NodeId("MyVariable", NamespaceIndex),
        BrowseName = new QualifiedName("MyVariable", NamespaceIndex),
        DisplayName = "MyVariable",
        DataType = DataTypeIds.Double,
        ValueRank = ValueRanks.Scalar,
        AccessLevel = AccessLevels.CurrentReadOrWrite,
        UserAccessLevel = AccessLevels.CurrentReadOrWrite,
        Value = 42.0
    };

    parentFolder.AddChild(variable);
    AddPredefinedNode(SystemContext, variable);
}
```

For a read-only variable, change `AccessLevel` and `UserAccessLevel` to `AccessLevels.CurrentRead`.

### Adding a New Method

```csharp
private void AddMyMethod(FolderState parentFolder)
{
    var method = new MethodState(parentFolder)
    {
        NodeId = new NodeId("MyMethod", NamespaceIndex),
        BrowseName = new QualifiedName("MyMethod", NamespaceIndex),
        DisplayName = "MyMethod",
        Executable = true,
        UserExecutable = true
    };

    method.InputArguments = new PropertyState<Argument[]>(method)
    {
        NodeId = new NodeId("MyMethod_InputArgs", NamespaceIndex),
        BrowseName = BrowseNames.InputArguments,
        Value = new Argument[]
        {
            new Argument { Name = "input", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
        }
    };

    method.OutputArguments = new PropertyState<Argument[]>(method)
    {
        NodeId = new NodeId("MyMethod_OutputArgs", NamespaceIndex),
        BrowseName = BrowseNames.OutputArguments,
        Value = new Argument[]
        {
            new Argument { Name = "result", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
        }
    };

    method.OnCallMethod = (context, objectId, inputArgs, outputArgs) =>
    {
        var input = (string)inputArgs[0].Value;
        outputArgs[0] = new Variant(input.ToUpper());
        return ServiceResult.Good;
    };

    parentFolder.AddChild(method);
    AddPredefinedNode(SystemContext, method);
}
```

### Adding a Dynamic Variable (Timer-Based)

Variables that change over time use `System.Threading.Timer`. Keep a reference so it can be stopped on shutdown.

```csharp
private Timer _counterTimer;
private uint _counterValue = 0;

private void AddMyCounter(FolderState parentFolder)
{
    var variable = new BaseDataVariableState<uint>(parentFolder)
    {
        NodeId = new NodeId("MyCounter", NamespaceIndex),
        BrowseName = new QualifiedName("MyCounter", NamespaceIndex),
        DisplayName = "MyCounter",
        DataType = DataTypeIds.UInt32,
        ValueRank = ValueRanks.Scalar,
        AccessLevel = AccessLevels.CurrentRead,
        UserAccessLevel = AccessLevels.CurrentRead,
        Value = _counterValue
    };

    parentFolder.AddChild(variable);
    AddPredefinedNode(SystemContext, variable);

    _counterTimer = new Timer(_ =>
    {
        _counterValue++;
        variable.Value = _counterValue;
        variable.Timestamp = DateTime.UtcNow;
        variable.ClearChangeMasks(SystemContext, false);
    }, null, 1000, 1000);
}

public void StopTimers()
{
    _counterTimer?.Dispose();
}
```

For variables computed on every read (no timer), override `OnReadValue` or use a getter pattern.

### Adding a Custom Event Type

```csharp
// Define the event type
var motorFaultType = new BaseObjectTypeState()
{
    NodeId = new NodeId("MotorFaultEventType", NamespaceIndex),
    BrowseName = new QualifiedName("MotorFaultEventType", NamespaceIndex),
    DisplayName = "MotorFaultEventType",
    SuperTypeId = ObjectTypeIds.BaseEventType,
    IsAbstract = false
};

// Add custom properties
var motorIdProp = new PropertyState<string>(motorFaultType)
{
    NodeId = new NodeId("MotorFaultEventType_MotorId", NamespaceIndex),
    BrowseName = new QualifiedName("MotorId", NamespaceIndex),
    DataType = DataTypeIds.String,
    ValueRank = ValueRanks.Scalar
};
motorFaultType.AddChild(motorIdProp);
```

### Adding a New User

Edit `config/users.json`:

```json
{
  "users": [
    { "username": "admin", "password": "admin123", "role": "admin" },
    { "username": "operator", "password": "operator123", "role": "operator" },
    { "username": "viewer", "password": "viewer123", "role": "viewer" },
    { "username": "plc_service", "password": "s3cure!Pass", "role": "operator" }
  ]
}
```

To add a new role, update the `GetUserRoles` method in `src/TestServer/UserManagement/UserManager.cs`:

```csharp
case "engineer":
    return new[] { "AuthenticatedUser", "Operator", "Engineer" };
case "supervisor":
    return new[] { "AuthenticatedUser", "Operator", "ConfigureAdmin" };
```

---

## Creating a New Address Space Builder

Step by step guide for adding an entirely new section to the address space.

### 1. Create the builder class

Create `src/TestServer/AddressSpace/MyModuleBuilder.cs`:

```csharp
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.AddressSpace
{
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

        public FolderState Build(FolderState rootFolder, ServerSystemContext context)
        {
            var folder = new FolderState(rootFolder)
            {
                NodeId = new NodeId("MyModule", _namespaceIndex),
                BrowseName = new QualifiedName("MyModule", _namespaceIndex),
                DisplayName = "MyModule",
                TypeDefinitionId = ObjectTypeIds.FolderType
            };
            rootFolder.AddChild(folder);

            // Add your variables, methods, events here

            return folder;
        }

        public void Stop()
        {
            foreach (var timer in _timers)
                timer.Dispose();
            _timers.Clear();
        }
    }
}
```

### 2. Add a feature toggle (optional)

In `src/TestServer/Configuration/ServerConfig.cs`, add a new toggle:

```csharp
public bool EnableMyModule { get; set; } = GetBoolEnv("OPCUA_ENABLE_MY_MODULE", true);
```

### 3. Wire it into the node manager

In `src/TestServer/Server/TestNodeManager.cs`, inside `CreateAddressSpace()`:

```csharp
if (_config.EnableMyModule)
{
    Console.WriteLine("[AddressSpace] Building my module...");
    var builder = new MyModuleBuilder(Server, _namespaceIndex);
    builder.Build(rootFolder, SystemContext);
}
```

### 4. Test it

```bash
docker compose build && docker compose up -d
docker compose logs -f opcua-no-security
```

---

## Adding a New Server Instance

To create a 9th server with a different configuration, add a new service in `docker-compose.yml`:

```yaml
opcua-my-scenario:
  build: .
  ports:
    - "4848:4848"
  volumes:
    - ./certs:/app/certs
    - ./config:/app/config:ro
  environment:
    OPCUA_PORT: "4848"
    OPCUA_SERVER_NAME: "MyScenarioServer"
    OPCUA_SECURITY_POLICIES: "Basic256Sha256"
    OPCUA_SECURITY_MODES: "SignAndEncrypt"
    OPCUA_ALLOW_ANONYMOUS: "false"
    OPCUA_AUTH_USERS: "true"
    OPCUA_AUTH_CERTIFICATE: "false"
    OPCUA_ENABLE_HISTORICAL: "false"
    OPCUA_ENABLE_EVENTS: "false"
  depends_on:
    certs-generator:
      condition: service_completed_successfully
  restart: unless-stopped
```

You can disable features you don't need via the `OPCUA_ENABLE_*` variables to create a leaner server.

If your new server needs to be recognized by the certificates, add its hostname to the SAN list in `scripts/generate-certs.sh`:

```
DNS.10 = opcua-my-scenario
```

Then regenerate certificates:

```bash
rm -rf ./certs
docker compose up -d
```

---

## Simulation Examples

Here are some ideas for custom simulations you could build.

### Industrial PLC

Simulate a PLC with registers, coils, and process data:

```csharp
public FolderState BuildPLC(FolderState rootFolder, ServerSystemContext context)
{
    var plc = CreateFolder(rootFolder, "PLC_001");
    var registers = CreateFolder(plc, "HoldingRegisters");

    for (int i = 0; i < 100; i++)
    {
        var variable = new BaseDataVariableState<ushort>(registers)
        {
            NodeId = new NodeId($"HR_{i:D4}", _namespaceIndex),
            BrowseName = new QualifiedName($"HR_{i:D4}", _namespaceIndex),
            DisplayName = $"HR_{i:D4}",
            DataType = DataTypeIds.UInt16,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = (ushort)0
        };
        registers.AddChild(variable);
    }

    return plc;
}
```

### HVAC System

```csharp
public FolderState BuildHVAC(FolderState rootFolder, ServerSystemContext context)
{
    var hvac = CreateFolder(rootFolder, "HVAC");
    var zones = new[] { "LobbyZone", "OfficeZone", "ServerRoom", "Warehouse" };

    foreach (var zoneName in zones)
    {
        var zone = CreateFolder(hvac, zoneName);
        double setpoint = zoneName == "ServerRoom" ? 18.0 : 22.0;

        AddAnalogVariable(zone, "Setpoint", setpoint, AccessLevels.CurrentReadOrWrite);
        AddAnalogVariable(zone, "CurrentTemp", setpoint + Random.Shared.NextDouble() * 2 - 1, AccessLevels.CurrentRead);
        AddAnalogVariable(zone, "FanSpeed", 50.0, AccessLevels.CurrentReadOrWrite);
    }

    return hvac;
}
```

### Energy Meter

```csharp
public FolderState BuildEnergyMeter(FolderState rootFolder, ServerSystemContext context)
{
    var meter = CreateFolder(rootFolder, "EnergyMeter");
    var startTime = DateTime.UtcNow;

    var power = new BaseDataVariableState<double>(meter)
    {
        NodeId = new NodeId("ActivePower_kW", _namespaceIndex),
        BrowseName = new QualifiedName("ActivePower_kW", _namespaceIndex),
        DataType = DataTypeIds.Double,
        AccessLevel = AccessLevels.CurrentRead | AccessLevels.HistoryRead,
        Value = 150.0
    };
    meter.AddChild(power);

    // Timer to simulate varying power
    _timers.Add(new Timer(_ =>
    {
        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        var basePower = 150 + 50 * Math.Sin(elapsed / 3600 * Math.PI);
        var noise = (Random.Shared.NextDouble() - 0.5) * 10;
        power.Value = basePower + noise;
        power.Timestamp = DateTime.UtcNow;
        power.ClearChangeMasks(context, false);
    }, null, 1000, 1000));

    return meter;
}
```

---

## Tips

- **Keep builders independent.** Each builder should only use the namespace index and parent folder parameters. Don't reference other builders' variables directly.
- **Always dispose timers.** Every `System.Threading.Timer` must be tracked and disposed in the `Stop()` method, otherwise the server won't shut down cleanly.
- **Use `Console.WriteLine` with tags.** Follow the existing pattern: `Console.WriteLine("[MyModule] Something happened")` for easy log filtering.
- **Test locally first.** Run `docker compose build && docker compose up opcua-no-security` to test with a single server before starting all 10.
- **Check UA-.NETStandard docs.** The [OPC Foundation UA-.NETStandard repository](https://github.com/OPCFoundation/UA-.NETStandard) contains samples and API documentation for all available node types, data types, and advanced features.

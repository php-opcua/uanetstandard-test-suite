---
eyebrow: 'Docs · Customization'
lede:    'Sketches for common industrial simulations — PLC registers, HVAC zones, energy meter, fleet of devices. Starting points for forking the suite into something domain-specific.'

see_also:
  - { href: './forking-and-adding-nodes.md',  meta: '6 min' }
  - { href: './adding-server-instances.md',   meta: '4 min' }

prev: { label: 'Adding server instances',  href: './adding-server-instances.md' }
next: { label: 'Environment variables',    href: '../reference/environment-variables.md' }
---

# Simulation recipes

The suite ships **generic** test data. Realistic plant
simulations are out of scope for a base test suite — but the
forking surface makes them straightforward.

A few starter sketches.

## Industrial PLC

A modbus-style PLC with holding registers, coils, and process
data.

<!-- @code-block language="text" label="PlcBuilder.cs (sketch)" -->
```text
public FolderState BuildPLC(FolderState root, ServerSystemContext ctx)
{
    var plc = CreateFolder(root, "PLC_001");

    // Holding registers (100 of them)
    var hr = CreateFolder(plc, "HoldingRegisters");
    for (int i = 0; i < 100; i++)
    {
        var v = new BaseDataVariableState<ushort>(hr)
        {
            NodeId       = new NodeId($"HR_{i:D4}", _namespaceIndex),
            BrowseName   = new QualifiedName($"HR_{i:D4}", _namespaceIndex),
            DataType     = DataTypeIds.UInt16,
            AccessLevel  = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value        = (ushort)0,
        };
        hr.AddChild(v);
    }

    // Input coils (50)
    var coils = CreateFolder(plc, "InputCoils");
    for (int i = 0; i < 50; i++)
    {
        var v = new BaseDataVariableState<bool>(coils)
        {
            NodeId       = new NodeId($"IC_{i:D4}", _namespaceIndex),
            BrowseName   = new QualifiedName($"IC_{i:D4}", _namespaceIndex),
            DataType     = DataTypeIds.Boolean,
            AccessLevel  = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value        = false,
        };
        coils.AddChild(v);
    }

    // Process values (cycling)
    var proc = CreateFolder(plc, "Process");
    AddDynamicSineVariable(proc, "Temperature", min: 18, max: 25, period: 60);
    AddDynamicSineVariable(proc, "Pressure",    min: 0.9, max: 1.1, period: 30);

    return plc;
}
```
<!-- @endcode-block -->

Then wire into `TestNodeManager.CreateAddressSpace()` (see
[Forking and adding nodes](./forking-and-adding-nodes.md)).

## HVAC system

A multi-zone HVAC simulation:

<!-- @code-block language="text" label="HvacBuilder.cs (sketch)" -->
```text
public FolderState BuildHVAC(FolderState root, ServerSystemContext ctx)
{
    var hvac = CreateFolder(root, "HVAC");
    var zones = new[] { "LobbyZone", "OfficeZone", "ServerRoom", "Warehouse" };

    foreach (var name in zones)
    {
        var zone = CreateFolder(hvac, name);
        double setpoint = name == "ServerRoom" ? 18.0 : 22.0;

        AddAnalogVariable(zone, "Setpoint", setpoint,
            AccessLevels.CurrentReadOrWrite);
        AddAnalogVariable(zone, "CurrentTemp",
            setpoint + Random.Shared.NextDouble() * 2 - 1,
            AccessLevels.CurrentRead);
        AddAnalogVariable(zone, "FanSpeed", 50.0,
            AccessLevels.CurrentReadOrWrite);
        AddDigitalVariable(zone, "HeatingActive", false,
            AccessLevels.CurrentRead);
        AddDigitalVariable(zone, "CoolingActive", false,
            AccessLevels.CurrentRead);
    }

    // Plant-wide
    AddDigitalVariable(hvac, "EmergencyShutdown", false,
        AccessLevels.CurrentReadOrWrite);
    AddAnalogVariable(hvac, "PowerConsumption_kW", 15.0,
        AccessLevels.CurrentRead);

    return hvac;
}
```
<!-- @endcode-block -->

Each zone has 5 variables; 4 zones = 20 variables plus 2
plant-wide.

## Energy meter

A power monitor with timer-driven simulated load:

<!-- @code-block language="text" label="EnergyMeterBuilder.cs (sketch)" -->
```text
public FolderState BuildEnergyMeter(FolderState root, ServerSystemContext ctx)
{
    var meter = CreateFolder(root, "EnergyMeter");
    var startTime = DateTime.UtcNow;

    var power = new BaseDataVariableState<double>(meter)
    {
        NodeId       = new NodeId("ActivePower_kW", _namespaceIndex),
        BrowseName   = new QualifiedName("ActivePower_kW", _namespaceIndex),
        DataType     = DataTypeIds.Double,
        AccessLevel  = AccessLevels.CurrentRead | AccessLevels.HistoryRead,
        UserAccessLevel = AccessLevels.CurrentRead | AccessLevels.HistoryRead,
        Value        = 150.0,
    };
    meter.AddChild(power);

    _timers.Add(new Timer(_ =>
    {
        var t = (DateTime.UtcNow - startTime).TotalSeconds;
        var basePower = 150 + 50 * Math.Sin(t / 3600 * Math.PI); // hourly cycle
        var noise = (Random.Shared.NextDouble() - 0.5) * 10;
        power.Value = basePower + noise;
        power.Timestamp = DateTime.UtcNow;
        power.ClearChangeMasks(ctx, false);
    }, null, 1000, 1000));

    // Cumulative energy
    var energy = new BaseDataVariableState<double>(meter) { /* ... */ };
    // Voltage / current / frequency similarly
    // Total harmonic distortion etc.

    return meter;
}
```
<!-- @endcode-block -->

Couple with `HistoricalBuilder` to record 24 hours of power
consumption — your test reads back the history and verifies the
sine pattern.

## Fleet of devices

A grid of identical devices (e.g., 100 PLCs in a factory floor):

<!-- @code-block language="text" label="DeviceFleetBuilder.cs (sketch)" -->
```text
public FolderState BuildFleet(FolderState root, ServerSystemContext ctx)
{
    var fleet = CreateFolder(root, "DeviceFleet");

    for (int i = 0; i < 100; i++)
    {
        var device = CreateFolder(fleet, $"Device_{i:D3}");
        AddAnalogVariable(device, "Status",      Random.Shared.NextDouble(),
            AccessLevels.CurrentRead);
        AddAnalogVariable(device, "ErrorCount",  0.0,
            AccessLevels.CurrentReadOrWrite);
        AddAnalogVariable(device, "LastSeen",    /* timestamp */,
            AccessLevels.CurrentRead);

        // Per-device dynamic signal
        _timers.Add(new Timer(_ =>
        {
            var v = (BaseDataVariableState<double>)
                device.FindChildBySymbolicName(ctx, "Status").Variable;
            v.Value = Random.Shared.NextDouble();
            v.Timestamp = DateTime.UtcNow;
            v.ClearChangeMasks(ctx, false);
        }, null, 5000 + i*50, 5000));   // staggered ticks
    }

    return fleet;
}
```
<!-- @endcode-block -->

Each device gets 3 variables; 100 devices = 300 variables. Each
device has a slow random walk on `Status`. Useful for testing
**fan-out** in subscription / fleet-reading scenarios.

## Recipe machine

A batch process machine with recipe selection, alarm states,
and method-driven operations:

<!-- @code-block language="text" label="RecipeBuilder.cs (sketch)" -->
```text
public FolderState BuildRecipeMachine(FolderState root, ServerSystemContext ctx)
{
    var rm = CreateFolder(root, "RecipeMachine");

    AddStringVariable(rm, "CurrentRecipe", "Recipe_A",
        AccessLevels.CurrentReadOrWrite);
    AddAnalogVariable(rm, "BatchProgress", 0.0,
        AccessLevels.CurrentRead);  // 0..100
    AddDigitalVariable(rm, "InProgress", false,
        AccessLevels.CurrentRead);

    AddMethod(rm, "StartBatch", new[]
    {
        new Argument { Name = "recipeName", DataType = DataTypeIds.String },
        new Argument { Name = "batchSize",  DataType = DataTypeIds.UInt32 },
    }, /* outputs */ new[]
    {
        new Argument { Name = "batchId", DataType = DataTypeIds.String },
    }, (ctx, _, inputs, outputs) =>
    {
        // start a coroutine that ticks BatchProgress 0 → 100 over 60s,
        // sets InProgress = true while running,
        // returns a generated batchId
        var batchId = Guid.NewGuid().ToString();
        outputs[0] = new Variant(batchId);
        return ServiceResult.Good;
    });

    AddMethod(rm, "AbortBatch", /* ... */);

    return rm;
}
```
<!-- @endcode-block -->

This gives subscribers a "long-running operation" to monitor via
`BatchProgress` data changes.

## Wiring multiple builders

Inside `TestNodeManager.CreateAddressSpace()`:

<!-- @code-block language="text" label="TestNodeManager.cs" -->
```text
if (_config.EnableMyDomain)
{
    new PlcBuilder(Server, _namespaceIndex)
        .Build(rootFolder, SystemContext);

    new HvacBuilder(Server, _namespaceIndex)
        .Build(rootFolder, SystemContext);

    new EnergyMeterBuilder(Server, _namespaceIndex)
        .Build(rootFolder, SystemContext);
}
```
<!-- @endcode-block -->

…then a `OPCUA_ENABLE_MY_DOMAIN` env var toggles the whole set
on/off.

## Where to read next

- [Reference · Environment variables](../reference/environment-variables.md) —
  the full env-var reference.
- [UA-.NETStandard samples](https://github.com/OPCFoundation/UA-.NETStandard) —
  upstream code for every node type.

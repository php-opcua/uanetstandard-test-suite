using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class EventsAlarmsBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public EventsAlarmsBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build(List<IDisposable> timers)
    {
        BuildEvents(timers);
        BuildAlarms(timers);
    }

    private void BuildEvents(List<IDisposable> timers)
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Events", "Events");

        // Event emitter object
        var emitter = new BaseObjectState(folder)
        {
            SymbolicName = "EventEmitter",
            NodeId = new NodeId("TestServer/Events/EventEmitter", _mgr.NamespaceIndex),
            BrowseName = new QualifiedName("EventEmitter", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "EventEmitter"),
            TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            EventNotifier = EventNotifiers.SubscribeToEvents
        };
        folder.AddChild(emitter);
        _mgr.AddNode(_context, emitter);

        var counter = 0;

        // Simple event every 2 seconds
        timers.Add(new Timer(_ =>
        {
            try
            {
                var e = new BaseEventState(emitter);
                e.Initialize(_context, emitter, EventSeverity.Low, new LocalizedText(""));
                e.EventId.Value = Guid.NewGuid().ToByteArray();
                e.EventType.Value = ObjectTypeIds.BaseEventType;
                e.SourceNode.Value = emitter.NodeId;
                e.SourceName.Value = "EventEmitter";
                e.Time.Value = DateTime.UtcNow;
                e.ReceiveTime.Value = DateTime.UtcNow;
                e.Severity.Value = 200;
                e.Message.Value = new LocalizedText("en", $"Simple event #{++counter}");

                emitter.ReportEvent(_context, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error emitting simple event: {ex.Message}");
            }
        }, null, 2000, 2000));

        // Complex event every 5 seconds
        timers.Add(new Timer(_ =>
        {
            try
            {
                var e = new BaseEventState(emitter);
                e.Initialize(_context, emitter, EventSeverity.Low, new LocalizedText(""));
                e.EventId.Value = Guid.NewGuid().ToByteArray();
                e.EventType.Value = ObjectTypeIds.BaseEventType;
                e.SourceNode.Value = emitter.NodeId;
                e.SourceName.Value = "EventEmitter";
                e.Time.Value = DateTime.UtcNow;
                e.ReceiveTime.Value = DateTime.UtcNow;
                e.Severity.Value = 500;
                e.Message.Value = new LocalizedText("en", $"Complex event: category=ProcessAlert, value={new Random().NextDouble():F3}");

                emitter.ReportEvent(_context, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error emitting complex event: {ex.Message}");
            }
        }, null, 5000, 5000));

        // System status event every 10 seconds
        timers.Add(new Timer(_ =>
        {
            try
            {
                var rng = new Random();
                var e = new BaseEventState(emitter);
                e.Initialize(_context, emitter, EventSeverity.Low, new LocalizedText(""));
                e.EventId.Value = Guid.NewGuid().ToByteArray();
                e.EventType.Value = ObjectTypeIds.SystemEventType;
                e.SourceNode.Value = emitter.NodeId;
                e.SourceName.Value = "SystemMonitor";
                e.Time.Value = DateTime.UtcNow;
                e.ReceiveTime.Value = DateTime.UtcNow;
                e.Severity.Value = 100;
                e.Message.Value = new LocalizedText("en", $"System status: CPU={rng.Next(5, 95)}%, Memory={rng.Next(30, 80)}%");

                emitter.ReportEvent(_context, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error emitting system event: {ex.Message}");
            }
        }, null, 10000, 10000));
    }

    private void BuildAlarms(List<IDisposable> timers)
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Alarms", "Alarms");
        var p = "TestServer/Alarms";

        // Alarm source variable (writable - clients can trigger alarms)
        var alarmSource = _mgr.CreateVariable<double>(folder, $"{p}/AlarmSourceValue", "AlarmSourceValue",
            DataTypeIds.Double, ValueRanks.Scalar, 50.0);

        // Off-normal source
        var offNormalSource = _mgr.CreateVariable<bool>(folder, $"{p}/OffNormalSource", "OffNormalSource",
            DataTypeIds.Boolean, ValueRanks.Scalar, false);

        // Exclusive Limit Alarm (High > 80, HighHigh > 95, Low < 20, LowLow < 5)
        var exclusiveAlarm = new ExclusiveLimitAlarmState(folder);
        InitializeAlarm(exclusiveAlarm, folder, $"{p}/HighTemperatureAlarm", "HighTemperatureAlarm",
            alarmSource.NodeId, 80.0, 95.0, 20.0, 5.0);
        _mgr.AddNode(_context, exclusiveAlarm);

        // Non-Exclusive Limit Alarm
        var nonExclusiveAlarm = new NonExclusiveLimitAlarmState(folder);
        InitializeNonExclusiveAlarm(nonExclusiveAlarm, folder, $"{p}/LevelAlarm", "LevelAlarm",
            alarmSource.NodeId, 75.0, 90.0, 25.0, 10.0);
        _mgr.AddNode(_context, nonExclusiveAlarm);

        // Off-Normal Alarm
        var offNormalAlarm = new OffNormalAlarmState(folder);
        InitializeOffNormalAlarm(offNormalAlarm, folder, $"{p}/OffNormalAlarm", "OffNormalAlarm",
            offNormalSource.NodeId);
        _mgr.AddNode(_context, offNormalAlarm);

        // Timer to evaluate alarm conditions
        timers.Add(new Timer(_ =>
        {
            try
            {
                var value = (double)alarmSource.Value;

                // Update exclusive limit alarm
                var isActive = value > 80 || value < 20;
                exclusiveAlarm.ActiveState.Value = new LocalizedText("en", isActive ? "Active" : "Inactive");
                exclusiveAlarm.Severity.Value = isActive ? (ushort)800 : (ushort)100;
                exclusiveAlarm.Message.Value = new LocalizedText("en", $"Temperature alarm: value={value:F1}");
                exclusiveAlarm.ClearChangeMasks(_context, true);

                // Off-normal alarm
                var offNormalActive = (bool)offNormalSource.Value;
                offNormalAlarm.ActiveState.Value = new LocalizedText("en", offNormalActive ? "Active" : "Inactive");
                offNormalAlarm.ClearChangeMasks(_context, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating alarms: {ex.Message}");
            }
        }, null, 1000, 1000));
    }

    private void InitializeAlarm(ExclusiveLimitAlarmState alarm, NodeState parent, string path, string name,
        NodeId sourceNode, double high, double highHigh, double low, double lowLow)
    {
        alarm.Create(_context, new NodeId(path, _mgr.NamespaceIndex),
            new QualifiedName(name, _mgr.NamespaceIndex),
            new LocalizedText("en", name), true);
        alarm.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        alarm.InputNode.Value = sourceNode;
        alarm.HighHighLimit.Value = highHigh;
        alarm.HighLimit.Value = high;
        alarm.LowLimit.Value = low;
        alarm.LowLowLimit.Value = lowLow;
        alarm.EnabledState.Value = new LocalizedText("en", "Enabled");
        alarm.ActiveState.Value = new LocalizedText("en", "Inactive");
        alarm.AckedState.Value = new LocalizedText("en", "Acknowledged");
        alarm.Severity.Value = (ushort)EventSeverity.Low;

        parent.AddChild(alarm);
    }

    private void InitializeNonExclusiveAlarm(NonExclusiveLimitAlarmState alarm, NodeState parent, string path, string name,
        NodeId sourceNode, double high, double highHigh, double low, double lowLow)
    {
        alarm.Create(_context, new NodeId(path, _mgr.NamespaceIndex),
            new QualifiedName(name, _mgr.NamespaceIndex),
            new LocalizedText("en", name), true);
        alarm.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        alarm.InputNode.Value = sourceNode;
        alarm.HighHighLimit.Value = highHigh;
        alarm.HighLimit.Value = high;
        alarm.LowLimit.Value = low;
        alarm.LowLowLimit.Value = lowLow;
        alarm.EnabledState.Value = new LocalizedText("en", "Enabled");
        alarm.ActiveState.Value = new LocalizedText("en", "Inactive");
        alarm.AckedState.Value = new LocalizedText("en", "Acknowledged");
        alarm.Severity.Value = (ushort)EventSeverity.Low;

        parent.AddChild(alarm);
    }

    private void InitializeOffNormalAlarm(OffNormalAlarmState alarm, NodeState parent, string path, string name,
        NodeId sourceNode)
    {
        alarm.Create(_context, new NodeId(path, _mgr.NamespaceIndex),
            new QualifiedName(name, _mgr.NamespaceIndex),
            new LocalizedText("en", name), true);
        alarm.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        alarm.InputNode.Value = sourceNode;
        alarm.EnabledState.Value = new LocalizedText("en", "Enabled");
        alarm.ActiveState.Value = new LocalizedText("en", "Inactive");
        alarm.AckedState.Value = new LocalizedText("en", "Acknowledged");
        alarm.Severity.Value = (ushort)EventSeverity.Medium;

        parent.AddChild(alarm);
    }
}

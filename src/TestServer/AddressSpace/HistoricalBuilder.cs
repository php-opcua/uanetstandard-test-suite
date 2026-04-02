using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class HistoricalBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public HistoricalBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build(List<IDisposable> timers)
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Historical", "Historical");
        var p = "TestServer/Historical";

        var historyReadAccess = (byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead);

        var histTemp = _mgr.CreateVariable<double>(folder, $"{p}/HistoricalTemperature", "HistoricalTemperature",
            DataTypeIds.Double, ValueRanks.Scalar, 20.0, historyReadAccess);
        histTemp.Historizing = true;

        var histPressure = _mgr.CreateVariable<double>(folder, $"{p}/HistoricalPressure", "HistoricalPressure",
            DataTypeIds.Double, ValueRanks.Scalar, 1013.0, historyReadAccess);
        histPressure.Historizing = true;

        var histCounter = _mgr.CreateVariable<uint>(folder, $"{p}/HistoricalCounter", "HistoricalCounter",
            DataTypeIds.UInt32, ValueRanks.Scalar, 0u, historyReadAccess);
        histCounter.Historizing = true;

        var histBool = _mgr.CreateVariable<bool>(folder, $"{p}/HistoricalBoolean", "HistoricalBoolean",
            DataTypeIds.Boolean, ValueRanks.Scalar, false, historyReadAccess);
        histBool.Historizing = true;

        // Register history store in the node manager so HistoryRead can access it
        var historyStore = _mgr.HistoryStore;
        historyStore[histTemp.NodeId] = new List<DataValue>();
        historyStore[histPressure.NodeId] = new List<DataValue>();
        historyStore[histCounter.NodeId] = new List<DataValue>();
        historyStore[histBool.NodeId] = new List<DataValue>();

        var rng = new Random();
        uint counterVal = 0;
        const int maxHistorySize = 10000;

        // Record historical values every 1000ms (matching node-opcua test server)
        timers.Add(new Timer(_ =>
        {
            var now = DateTime.UtcNow;

            // Temperature: random walk around 20-30
            var temp = 25.0 + 10.0 * Math.Sin(now.TimeOfDay.TotalSeconds / 30.0) + rng.NextDouble() * 2 - 1;
            histTemp.Value = temp;
            histTemp.Timestamp = now;
            histTemp.ClearChangeMasks(_context, false);
            AddHistoryValue(historyStore, histTemp.NodeId, temp, now, maxHistorySize);

            // Pressure: around 1013
            var pressure = 1013.0 + 20.0 * Math.Cos(now.TimeOfDay.TotalSeconds / 45.0) + rng.NextDouble() * 3 - 1.5;
            histPressure.Value = pressure;
            histPressure.Timestamp = now;
            histPressure.ClearChangeMasks(_context, false);
            AddHistoryValue(historyStore, histPressure.NodeId, pressure, now, maxHistorySize);

            // Counter
            counterVal++;
            histCounter.Value = counterVal;
            histCounter.Timestamp = now;
            histCounter.ClearChangeMasks(_context, false);
            AddHistoryValue(historyStore, histCounter.NodeId, counterVal, now, maxHistorySize);

            // Boolean: toggle every ~5 seconds
            var boolVal = (int)(now.TimeOfDay.TotalSeconds / 5) % 2 == 0;
            histBool.Value = boolVal;
            histBool.Timestamp = now;
            histBool.ClearChangeMasks(_context, false);
            AddHistoryValue(historyStore, histBool.NodeId, boolVal, now, maxHistorySize);
        }, null, 1000, 1000));
    }

    private static void AddHistoryValue(Dictionary<NodeId, List<DataValue>> store, NodeId nodeId, object value, DateTime timestamp, int maxSize)
    {
        var list = store[nodeId];
        lock (list)
        {
            list.Add(new DataValue
            {
                Value = value,
                StatusCode = StatusCodes.Good,
                SourceTimestamp = timestamp,
                ServerTimestamp = timestamp
            });

            if (list.Count > maxSize)
            {
                list.RemoveRange(0, list.Count - maxSize);
            }
        }
    }
}

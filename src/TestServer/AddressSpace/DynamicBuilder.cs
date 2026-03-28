using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class DynamicBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public DynamicBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build(List<IDisposable> timers)
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Dynamic", "Dynamic");
        var p = "TestServer/Dynamic";
        var ro = AccessLevels.CurrentRead;

        var counter = _mgr.CreateVariable<uint>(folder, $"{p}/Counter", "Counter", DataTypeIds.UInt32, ValueRanks.Scalar, 0u, ro);
        var fastCounter = _mgr.CreateVariable<uint>(folder, $"{p}/FastCounter", "FastCounter", DataTypeIds.UInt32, ValueRanks.Scalar, 0u, ro);
        var slowCounter = _mgr.CreateVariable<uint>(folder, $"{p}/SlowCounter", "SlowCounter", DataTypeIds.UInt32, ValueRanks.Scalar, 0u, ro);
        var random = _mgr.CreateVariable<double>(folder, $"{p}/Random", "Random", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);
        var randomInt = _mgr.CreateVariable<int>(folder, $"{p}/RandomInt", "RandomInt", DataTypeIds.Int32, ValueRanks.Scalar, 0, ro);
        var sineWave = _mgr.CreateVariable<double>(folder, $"{p}/SineWave", "SineWave", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);
        var sawTooth = _mgr.CreateVariable<double>(folder, $"{p}/SawTooth", "SawTooth", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);
        var square = _mgr.CreateVariable<bool>(folder, $"{p}/Square", "Square", DataTypeIds.Boolean, ValueRanks.Scalar, false, ro);
        var timestamp = _mgr.CreateVariable<DateTime>(folder, $"{p}/Timestamp", "Timestamp", DataTypeIds.DateTime, ValueRanks.Scalar, DateTime.UtcNow, ro);
        var randomString = _mgr.CreateVariable<string>(folder, $"{p}/RandomString", "RandomString", DataTypeIds.String, ValueRanks.Scalar, "", ro);
        var statusVariable = _mgr.CreateVariable<StatusCode>(folder, $"{p}/StatusVariable", "StatusVariable", DataTypeIds.StatusCode, ValueRanks.Scalar, StatusCodes.Good, ro);
        var nullableDouble = _mgr.CreateVariable<double>(folder, $"{p}/NullableDouble", "NullableDouble", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);
        var triangleWave = _mgr.CreateVariable<double>(folder, $"{p}/TriangleWave", "TriangleWave", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);

        var rng = new Random();
        var statusCodes = new[] { StatusCodes.Good, StatusCodes.Uncertain, StatusCodes.Bad };
        var strings = new[] { "alpha", "bravo", "charlie", "delta", "echo", "foxtrot" };
        uint counterVal = 0, fastVal = 0, slowVal = 0;
        int slowTick = 0;

        // Fast counter: 100ms
        timers.Add(new Timer(_ =>
        {
            fastCounter.Value = ++fastVal;
            fastCounter.Timestamp = DateTime.UtcNow;
            fastCounter.ClearChangeMasks(_context, false);
        }, null, 100, 100));

        // Normal counter + other 1-second updates
        timers.Add(new Timer(_ =>
        {
            var now = DateTime.UtcNow;
            var t = now.TimeOfDay.TotalSeconds;

            counter.Value = ++counterVal;
            counter.Timestamp = now;
            counter.ClearChangeMasks(_context, false);

            randomInt.Value = rng.Next(-1000, 1001);
            randomInt.Timestamp = now;
            randomInt.ClearChangeMasks(_context, false);

            timestamp.Value = now;
            timestamp.Timestamp = now;
            timestamp.ClearChangeMasks(_context, false);

            randomString.Value = strings[rng.Next(strings.Length)];
            randomString.Timestamp = now;
            randomString.ClearChangeMasks(_context, false);

            statusVariable.Value = statusCodes[rng.Next(statusCodes.Length)];
            statusVariable.Timestamp = now;
            statusVariable.ClearChangeMasks(_context, false);

            // Square wave: 10 second period
            square.Value = (int)(t / 5) % 2 == 0;
            square.Timestamp = now;
            square.ClearChangeMasks(_context, false);
        }, null, 1000, 1000));

        // Random + waves: 500ms
        timers.Add(new Timer(_ =>
        {
            var now = DateTime.UtcNow;
            var t = now.TimeOfDay.TotalSeconds;

            random.Value = rng.NextDouble();
            random.Timestamp = now;
            random.ClearChangeMasks(_context, false);

            // Sine wave: 10 second period
            sineWave.Value = Math.Sin(2 * Math.PI * t / 10.0);
            sineWave.Timestamp = now;
            sineWave.ClearChangeMasks(_context, false);

            // Sawtooth: 10 second period
            sawTooth.Value = (t % 10.0) / 10.0;
            sawTooth.Timestamp = now;
            sawTooth.ClearChangeMasks(_context, false);

            // Triangle wave: 10 second period
            var phase = (t % 10.0) / 10.0;
            triangleWave.Value = phase < 0.5 ? phase * 2.0 : 2.0 - phase * 2.0;
            triangleWave.Timestamp = now;
            triangleWave.ClearChangeMasks(_context, false);

            // Nullable double: sometimes null
            if (rng.NextDouble() < 0.2)
            {
                nullableDouble.StatusCode = StatusCodes.BadNoData;
            }
            else
            {
                nullableDouble.Value = rng.NextDouble() * 100;
                nullableDouble.StatusCode = StatusCodes.Good;
            }
            nullableDouble.Timestamp = now;
            nullableDouble.ClearChangeMasks(_context, false);
        }, null, 500, 500));

        // Slow counter: 10s
        timers.Add(new Timer(_ =>
        {
            slowCounter.Value = ++slowVal;
            slowCounter.Timestamp = DateTime.UtcNow;
            slowCounter.ClearChangeMasks(_context, false);
        }, null, 10000, 10000));
    }
}

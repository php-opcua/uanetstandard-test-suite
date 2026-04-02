using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class MethodsBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public MethodsBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Methods", "Methods");
        var p = "TestServer/Methods";

        // Add(a: Double, b: Double) → result: Double
        _mgr.CreateMethod(folder, $"{p}/Add", "Add",
            (input, output) =>
            {
                var a = (double)input[0];
                var b = (double)input[1];
                output[0] = a + b;
            },
            new[] { Arg("a", DataTypeIds.Double, "First operand"), Arg("b", DataTypeIds.Double, "Second operand") },
            new[] { Arg("result", DataTypeIds.Double, "Sum") });

        // Multiply(a: Double, b: Double) → result: Double
        _mgr.CreateMethod(folder, $"{p}/Multiply", "Multiply",
            (input, output) =>
            {
                var a = (double)input[0];
                var b = (double)input[1];
                output[0] = a * b;
            },
            new[] { Arg("a", DataTypeIds.Double, "First operand"), Arg("b", DataTypeIds.Double, "Second operand") },
            new[] { Arg("result", DataTypeIds.Double, "Product") });

        // Concatenate(a: String, b: String) → result: String
        _mgr.CreateMethod(folder, $"{p}/Concatenate", "Concatenate",
            (input, output) =>
            {
                var a = (string)input[0];
                var b = (string)input[1];
                output[0] = a + b;
            },
            new[] { Arg("a", DataTypeIds.String, "First string"), Arg("b", DataTypeIds.String, "Second string") },
            new[] { Arg("result", DataTypeIds.String, "Concatenated string") });

        // Reverse(input: String) → result: String
        _mgr.CreateMethod(folder, $"{p}/Reverse", "Reverse",
            (input, output) =>
            {
                var s = (string)input[0];
                output[0] = new string(s.Reverse().ToArray());
            },
            new[] { Arg("input", DataTypeIds.String, "Input string") },
            new[] { Arg("result", DataTypeIds.String, "Reversed string") });

        // GetServerTime() → time: DateTime
        _mgr.CreateMethod(folder, $"{p}/GetServerTime", "GetServerTime",
            (input, output) =>
            {
                output[0] = DateTime.UtcNow;
            },
            Array.Empty<Argument>(),
            new[] { Arg("time", DataTypeIds.DateTime, "Current server time") });

        // Echo(input: Variant) → output: Variant
        _mgr.CreateMethod(folder, $"{p}/Echo", "Echo",
            (input, output) =>
            {
                output[0] = input[0];
            },
            new[] { Arg("input", DataTypeIds.BaseDataType, "Value to echo") },
            new[] { Arg("output", DataTypeIds.BaseDataType, "Echoed value") });

        // GenerateEvent(message: String, severity: UInt16) → void
        _mgr.CreateMethod(folder, $"{p}/GenerateEvent", "GenerateEvent",
            (input, output) =>
            {
                var message = (string)input[0];
                var severity = (ushort)input[1];
                Console.WriteLine($"GenerateEvent called: message='{message}', severity={severity}");
            },
            new[] { Arg("message", DataTypeIds.String, "Event message"), Arg("severity", DataTypeIds.UInt16, "Event severity") },
            Array.Empty<Argument>());

        // LongRunning(durationMs: UInt32) → completed: Boolean
        _mgr.CreateMethod(folder, $"{p}/LongRunning", "LongRunning",
            (input, output) =>
            {
                var duration = (uint)input[0];
                Thread.Sleep((int)Math.Min(duration, 30000));
                output[0] = true;
            },
            new[] { Arg("durationMs", DataTypeIds.UInt32, "Duration in milliseconds") },
            new[] { Arg("completed", DataTypeIds.Boolean, "Completion status") });

        // Failing() → always throws
        _mgr.CreateMethod(folder, $"{p}/Failing", "Failing",
            (input, output) =>
            {
                throw new InvalidOperationException("This method always fails");
            },
            Array.Empty<Argument>(),
            Array.Empty<Argument>());

        // ArraySum(values: Double[]) → sum: Double
        _mgr.CreateMethod(folder, $"{p}/ArraySum", "ArraySum",
            (input, output) =>
            {
                var values = (double[])input[0];
                output[0] = values.Sum();
            },
            new[] { ArgArray("values", DataTypeIds.Double, "Array of doubles") },
            new[] { Arg("sum", DataTypeIds.Double, "Sum of values") });

        // MatrixTranspose(matrix: Double[], rows: UInt32, cols: UInt32) → result: Double[]
        _mgr.CreateMethod(folder, $"{p}/MatrixTranspose", "MatrixTranspose",
            (input, output) =>
            {
                var matrix = (double[])input[0];
                var rows = (uint)input[1];
                var cols = (uint)input[2];
                var result = new double[matrix.Length];
                for (var r = 0; r < rows; r++)
                    for (var c = 0; c < cols; c++)
                        result[c * rows + r] = matrix[r * cols + c];
                output[0] = result;
            },
            new[] { ArgArray("matrix", DataTypeIds.Double, "Flat matrix"), Arg("rows", DataTypeIds.UInt32, "Row count"), Arg("cols", DataTypeIds.UInt32, "Column count") },
            new[] { ArgArray("result", DataTypeIds.Double, "Transposed matrix") });

        // MultiOutput() → intValue: Int32, stringValue: String, boolValue: Boolean
        _mgr.CreateMethod(folder, $"{p}/MultiOutput", "MultiOutput",
            (input, output) =>
            {
                output[0] = 42;
                output[1] = "hello";
                output[2] = true;
            },
            Array.Empty<Argument>(),
            new[]
            {
                Arg("intValue", DataTypeIds.Int32, "Integer value"),
                Arg("stringValue", DataTypeIds.String, "String value"),
                Arg("boolValue", DataTypeIds.Boolean, "Boolean value")
            });
    }

    private static Argument Arg(string name, NodeId dataType, string description)
    {
        return new Argument
        {
            Name = name,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            Description = new LocalizedText("en", description)
        };
    }

    private static Argument ArgArray(string name, NodeId dataType, string description)
    {
        return new Argument
        {
            Name = name,
            DataType = dataType,
            ValueRank = ValueRanks.OneDimension,
            Description = new LocalizedText("en", description)
        };
    }
}

using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

/// <summary>
/// Exposes two Method nodes that let a (regular, non-reverse) client trigger
/// a Reverse Connect attempt from the server side, against a client-supplied
/// host:port target.
///
/// Used by the php-opcua client integration tests:
///   1. The PHP test opens a ReverseConnect listener on an ephemeral port.
///   2. It connects to this server normally and calls StartReverseConnect
///      with the listener's host/port.
///   3. The server enqueues the reverse connection target via
///      ReverseConnectServer.AddReverseConnection().
///   4. UA-.NETStandard begins TCP-connecting outward to the listener and
///      sends a ReverseHello (RHE) per OPC UA Part 6 §7.1.2.3.
///   5. The PHP test accepts the inbound connection and validates the RHE.
///
/// StopReverseConnect tears the target down at the end of the test.
///
/// Method paths under the server address space:
///   - ns=2;s=TestServer/ReverseConnect/StartReverseConnect
///   - ns=2;s=TestServer/ReverseConnect/StopReverseConnect
/// </summary>
public class ReverseConnectMethodsBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;
    private readonly Action<Uri> _addReverseConnection;
    private readonly Func<Uri, bool> _removeReverseConnection;

    public ReverseConnectMethodsBuilder(
        TestNodeManager mgr,
        FolderState root,
        ISystemContext context,
        Action<Uri> addReverseConnection,
        Func<Uri, bool> removeReverseConnection)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
        _addReverseConnection = addReverseConnection;
        _removeReverseConnection = removeReverseConnection;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/ReverseConnect", "ReverseConnect");
        var p = "TestServer/ReverseConnect";

        // StartReverseConnect(host: String, port: UInt16) → status: StatusCode
        _mgr.CreateMethod(folder, $"{p}/StartReverseConnect", "StartReverseConnect",
            (input, output) =>
            {
                var host = (string)input[0];
                var port = (ushort)input[1];

                if (string.IsNullOrWhiteSpace(host))
                {
                    output[0] = (uint)StatusCodes.BadInvalidArgument;
                    return;
                }
                if (port == 0)
                {
                    output[0] = (uint)StatusCodes.BadInvalidArgument;
                    return;
                }

                var uri = new Uri($"opc.tcp://{host}:{port}");

                try
                {
                    _addReverseConnection(uri);
                    Console.WriteLine($"[ReverseConnect] AddReverseConnection({uri}) queued");
                    output[0] = (uint)StatusCodes.Good;
                }
                catch (ArgumentException)
                {
                    // Already configured for this URL: idempotent for the caller.
                    output[0] = (uint)StatusCodes.Good;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReverseConnect] AddReverseConnection failed: {ex.Message}");
                    output[0] = (uint)StatusCodes.BadInternalError;
                }
            },
            new[]
            {
                Arg("host", DataTypeIds.String, "Target host (the client's reverse-connect listener)"),
                Arg("port", DataTypeIds.UInt16, "Target TCP port of the reverse-connect listener")
            },
            new[] { Arg("status", DataTypeIds.StatusCode, "StatusCodes.Good on success") });

        // StopReverseConnect(host: String, port: UInt16) → status: StatusCode
        _mgr.CreateMethod(folder, $"{p}/StopReverseConnect", "StopReverseConnect",
            (input, output) =>
            {
                var host = (string)input[0];
                var port = (ushort)input[1];

                if (string.IsNullOrWhiteSpace(host) || port == 0)
                {
                    output[0] = (uint)StatusCodes.BadInvalidArgument;
                    return;
                }

                var uri = new Uri($"opc.tcp://{host}:{port}");

                try
                {
                    var removed = _removeReverseConnection(uri);
                    Console.WriteLine($"[ReverseConnect] RemoveReverseConnection({uri}) removed={removed}");
                    output[0] = (uint)(removed ? StatusCodes.Good : StatusCodes.BadNotFound);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReverseConnect] RemoveReverseConnection failed: {ex.Message}");
                    output[0] = (uint)StatusCodes.BadInternalError;
                }
            },
            new[]
            {
                Arg("host", DataTypeIds.String, "Target host that was passed to StartReverseConnect"),
                Arg("port", DataTypeIds.UInt16, "Target TCP port that was passed to StartReverseConnect")
            },
            new[] { Arg("status", DataTypeIds.StatusCode, "StatusCodes.Good if removed, BadNotFound otherwise") });
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
}

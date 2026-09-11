using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

/// <summary>
/// Read-only variables whose StatusCode carries fixed DataValue InfoBits
/// (StatusCode InfoType = DataValue, LimitBits set), so clients can verify
/// they decode them. No stock node produces LimitBits. Values and status
/// codes never change: a single Read is enough to test against.
/// </summary>
public class InfoBitsBuilder
{
    private const uint InfoTypeDataValue = 0x00000400;
    private const uint LimitLow = 0x00000100;
    private const uint LimitHigh = 0x00000200;
    private const uint LimitConstant = 0x00000300;

    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;

    public InfoBitsBuilder(TestNodeManager mgr, FolderState root)
    {
        _mgr = mgr;
        _root = root;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/InfoBits", "InfoBits");
        var p = "TestServer/InfoBits";

        Create(folder, $"{p}/NoLimit", "NoLimit", 50.0, StatusCodes.Good);
        Create(folder, $"{p}/LimitLow", "LimitLow", 0.0, InfoTypeDataValue | LimitLow);
        Create(folder, $"{p}/LimitHigh", "LimitHigh", 100.0, InfoTypeDataValue | LimitHigh);
        Create(folder, $"{p}/LimitConstant", "LimitConstant", 42.0, InfoTypeDataValue | LimitConstant);
    }

    private void Create(FolderState folder, string path, string name, double value, uint statusCode)
    {
        var variable = _mgr.CreateVariable<double>(folder, path, name, DataTypeIds.Double, ValueRanks.Scalar, value, AccessLevels.CurrentRead);
        variable.StatusCode = statusCode;
    }
}

using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class ExtensionObjectsBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public ExtensionObjectsBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/ExtensionObjects", "ExtensionObjects");
        var p = "TestServer/ExtensionObjects";
        var ctNs = _mgr.CustomTypesNamespaceIndex;

        // Create custom DataType nodes for TestPointXYZ in custom types namespace
        var pointTypeId = new NodeId(3000, ctNs);
        var pointType = new DataTypeState
        {
            SymbolicName = "TestPointXYZ",
            NodeId = pointTypeId,
            BrowseName = new QualifiedName("TestPointXYZ", ctNs),
            DisplayName = new LocalizedText("en", "TestPointXYZ"),
            IsAbstract = false,
            SuperTypeId = DataTypeIds.Structure
        };
        _mgr.AddNode(_context, pointType);

        // Binary encoding node for TestPointXYZ
        var pointEncodingId = new NodeId(3010, ctNs);
        var pointEncoding = new BaseObjectState(null)
        {
            SymbolicName = "TestPointXYZ_DefaultBinary",
            NodeId = pointEncodingId,
            BrowseName = new QualifiedName("Default Binary", 0),
            DisplayName = new LocalizedText("en", "Default Binary"),
            TypeDefinitionId = ObjectTypeIds.DataTypeEncodingType
        };
        pointType.AddChild(pointEncoding);
        _mgr.AddNode(_context, pointEncoding);

        // Create custom DataType node for TestRangeStruct
        var rangeTypeId = new NodeId(3001, ctNs);
        var rangeType = new DataTypeState
        {
            SymbolicName = "TestRangeStruct",
            NodeId = rangeTypeId,
            BrowseName = new QualifiedName("TestRangeStruct", ctNs),
            DisplayName = new LocalizedText("en", "TestRangeStruct"),
            IsAbstract = false,
            SuperTypeId = DataTypeIds.Structure
        };
        _mgr.AddNode(_context, rangeType);

        // Binary encoding node for TestRangeStruct
        var rangeEncodingId = new NodeId(3011, ctNs);
        var rangeEncoding = new BaseObjectState(null)
        {
            SymbolicName = "TestRangeStruct_DefaultBinary",
            NodeId = rangeEncodingId,
            BrowseName = new QualifiedName("Default Binary", 0),
            DisplayName = new LocalizedText("en", "Default Binary"),
            TypeDefinitionId = ObjectTypeIds.DataTypeEncodingType
        };
        rangeType.AddChild(rangeEncoding);
        _mgr.AddNode(_context, rangeEncoding);

        // PointValue variable - contains binary-encoded ExtensionObject (3 doubles: 1.5, 2.5, 3.5)
        var pointBody = EncodeDoubles(1.5, 2.5, 3.5);
        var pointExtObj = new ExtensionObject(pointEncodingId, pointBody);

        var pointVar = _mgr.CreateVariableUntyped(folder, $"{p}/PointValue", "PointValue",
            DataTypeIds.Structure, ValueRanks.Scalar, pointExtObj);

        // RangeValue variable (RO) - contains binary-encoded ExtensionObject (3 doubles: 0.0, 100.0, 42.0)
        var rangeBody = EncodeDoubles(0.0, 100.0, 42.5);
        var rangeExtObj = new ExtensionObject(rangeEncodingId, rangeBody);

        var rangeVar = _mgr.CreateVariableUntyped(folder, $"{p}/RangeValue", "RangeValue",
            DataTypeIds.Structure, ValueRanks.Scalar, rangeExtObj, AccessLevels.CurrentRead);
    }

    private static byte[] EncodeDoubles(params double[] values)
    {
        var bytes = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, i * 8, 8);
        }
        return bytes;
    }
}

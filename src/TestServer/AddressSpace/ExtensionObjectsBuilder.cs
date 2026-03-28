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

        // Create custom DataType nodes for TestPointXYZ
        var pointTypeId = new NodeId("TestPointXYZ", _mgr.NamespaceIndex);
        var pointType = new DataTypeState
        {
            SymbolicName = "TestPointXYZ",
            NodeId = pointTypeId,
            BrowseName = new QualifiedName("TestPointXYZ", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "TestPointXYZ"),
            IsAbstract = false,
            SuperTypeId = DataTypeIds.Structure
        };
        _mgr.AddNode(_context, pointType);

        // Create custom DataType node for TestRangeStruct
        var rangeTypeId = new NodeId("TestRangeStruct", _mgr.NamespaceIndex);
        var rangeType = new DataTypeState
        {
            SymbolicName = "TestRangeStruct",
            NodeId = rangeTypeId,
            BrowseName = new QualifiedName("TestRangeStruct", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "TestRangeStruct"),
            IsAbstract = false,
            SuperTypeId = DataTypeIds.Structure
        };
        _mgr.AddNode(_context, rangeType);

        // PointValue variable (RW) - uses a structured object with X, Y, Z as child variables
        var pointObj = new BaseObjectState(folder)
        {
            SymbolicName = "PointValue",
            NodeId = new NodeId($"{p}/PointValue", _mgr.NamespaceIndex),
            BrowseName = new QualifiedName("PointValue", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "PointValue"),
            TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            ReferenceTypeId = ReferenceTypeIds.HasComponent
        };
        folder.AddChild(pointObj);
        _mgr.AddNode(_context, pointObj);

        _mgr.CreateVariable<double>(pointObj, $"{p}/PointValue/X", "X", DataTypeIds.Double, ValueRanks.Scalar, 1.0);
        _mgr.CreateVariable<double>(pointObj, $"{p}/PointValue/Y", "Y", DataTypeIds.Double, ValueRanks.Scalar, 2.0);
        _mgr.CreateVariable<double>(pointObj, $"{p}/PointValue/Z", "Z", DataTypeIds.Double, ValueRanks.Scalar, 3.0);

        // RangeValue variable (RO)
        var rangeObj = new BaseObjectState(folder)
        {
            SymbolicName = "RangeValue",
            NodeId = new NodeId($"{p}/RangeValue", _mgr.NamespaceIndex),
            BrowseName = new QualifiedName("RangeValue", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "RangeValue"),
            TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            ReferenceTypeId = ReferenceTypeIds.HasComponent
        };
        folder.AddChild(rangeObj);
        _mgr.AddNode(_context, rangeObj);

        var ro = AccessLevels.CurrentRead;
        _mgr.CreateVariable<double>(rangeObj, $"{p}/RangeValue/Min", "Min", DataTypeIds.Double, ValueRanks.Scalar, 0.0, ro);
        _mgr.CreateVariable<double>(rangeObj, $"{p}/RangeValue/Max", "Max", DataTypeIds.Double, ValueRanks.Scalar, 100.0, ro);
        _mgr.CreateVariable<double>(rangeObj, $"{p}/RangeValue/Value", "Value", DataTypeIds.Double, ValueRanks.Scalar, 42.0, ro);
    }
}

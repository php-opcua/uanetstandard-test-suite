using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class StructuresBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public StructuresBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Structures", "Structures");
        var p = "TestServer/Structures";

        BuildTestPoint(folder, p);
        BuildTestRange(folder, p);
        BuildTestPerson(folder, p);
        BuildTestNested(folder, p);
        BuildPointCollection(folder, p);
        BuildDeepNesting(folder, p);
    }

    private void BuildTestPoint(FolderState parent, string basePath)
    {
        var obj = CreateObject(parent, $"{basePath}/TestPoint", "TestPoint");
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestPoint/X", "X", DataTypeIds.Double, ValueRanks.Scalar, 1.0);
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestPoint/Y", "Y", DataTypeIds.Double, ValueRanks.Scalar, 2.0);
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestPoint/Z", "Z", DataTypeIds.Double, ValueRanks.Scalar, 3.0);
    }

    private void BuildTestRange(FolderState parent, string basePath)
    {
        var obj = CreateObject(parent, $"{basePath}/TestRange", "TestRange");
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestRange/Min", "Min", DataTypeIds.Double, ValueRanks.Scalar, 0.0);
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestRange/Max", "Max", DataTypeIds.Double, ValueRanks.Scalar, 100.0);
        _mgr.CreateVariable<double>(obj, $"{basePath}/TestRange/Value", "Value", DataTypeIds.Double, ValueRanks.Scalar, 50.0);
    }

    private void BuildTestPerson(FolderState parent, string basePath)
    {
        var obj = CreateObject(parent, $"{basePath}/TestPerson", "TestPerson");
        _mgr.CreateVariable<string>(obj, $"{basePath}/TestPerson/Name", "Name", DataTypeIds.String, ValueRanks.Scalar, "John Doe");
        _mgr.CreateVariable<uint>(obj, $"{basePath}/TestPerson/Age", "Age", DataTypeIds.UInt32, ValueRanks.Scalar, 30u);
        _mgr.CreateVariable<bool>(obj, $"{basePath}/TestPerson/Active", "Active", DataTypeIds.Boolean, ValueRanks.Scalar, true);
    }

    private void BuildTestNested(FolderState parent, string basePath)
    {
        var obj = CreateObject(parent, $"{basePath}/TestNested", "TestNested");
        _mgr.CreateVariable<string>(obj, $"{basePath}/TestNested/Label", "Label", DataTypeIds.String, ValueRanks.Scalar, "origin");
        _mgr.CreateVariable<DateTime>(obj, $"{basePath}/TestNested/Timestamp", "Timestamp", DataTypeIds.DateTime, ValueRanks.Scalar, DateTime.UtcNow);

        var point = CreateObject(obj, $"{basePath}/TestNested/Point", "Point");
        _mgr.CreateVariable<double>(point, $"{basePath}/TestNested/Point/X", "X", DataTypeIds.Double, ValueRanks.Scalar, 10.0);
        _mgr.CreateVariable<double>(point, $"{basePath}/TestNested/Point/Y", "Y", DataTypeIds.Double, ValueRanks.Scalar, 20.0);
        _mgr.CreateVariable<double>(point, $"{basePath}/TestNested/Point/Z", "Z", DataTypeIds.Double, ValueRanks.Scalar, 30.0);
    }

    private void BuildPointCollection(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/PointCollection", "PointCollection");
        var rng = new Random(42);

        for (var i = 0; i < 5; i++)
        {
            var pointObj = CreateObject(folder, $"{basePath}/PointCollection/Point_{i}", $"Point_{i}");
            _mgr.CreateVariable<double>(pointObj, $"{basePath}/PointCollection/Point_{i}/X", "X", DataTypeIds.Double, ValueRanks.Scalar, rng.NextDouble() * 100);
            _mgr.CreateVariable<double>(pointObj, $"{basePath}/PointCollection/Point_{i}/Y", "Y", DataTypeIds.Double, ValueRanks.Scalar, rng.NextDouble() * 100);
            _mgr.CreateVariable<double>(pointObj, $"{basePath}/PointCollection/Point_{i}/Z", "Z", DataTypeIds.Double, ValueRanks.Scalar, rng.NextDouble() * 100);
        }
    }

    private void BuildDeepNesting(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/DeepNesting", "DeepNesting");

        NodeState currentParent = folder;
        var currentPath = $"{basePath}/DeepNesting";

        for (var i = 1; i <= 10; i++)
        {
            var levelName = $"Level_{i}";
            var levelPath = $"{currentPath}/{levelName}";

            var levelObj = CreateObject(currentParent, levelPath, levelName);
            _mgr.CreateVariable<uint>(levelObj, $"{levelPath}/Depth", "Depth", DataTypeIds.UInt32, ValueRanks.Scalar, (uint)i);
            _mgr.CreateVariable<string>(levelObj, $"{levelPath}/Name", "Name", DataTypeIds.String, ValueRanks.Scalar, $"Level {i}");

            currentParent = levelObj;
            currentPath = levelPath;
        }
    }

    private BaseObjectState CreateObject(NodeState parent, string path, string name)
    {
        var obj = new BaseObjectState(parent)
        {
            SymbolicName = name,
            NodeId = new NodeId(path, _mgr.NamespaceIndex),
            BrowseName = new QualifiedName(name, _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            ReferenceTypeId = ReferenceTypeIds.HasComponent
        };

        parent.AddChild(obj);
        _mgr.AddNode(_context, obj);
        return obj;
    }
}

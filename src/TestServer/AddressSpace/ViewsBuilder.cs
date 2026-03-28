using Opc.Ua;
using TestServer.Server;

namespace TestServer.AddressSpace;

public class ViewsBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public ViewsBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        // Create views that reference different parts of the address space
        CreateView("OperatorView", "Operator view with Dynamic, Methods, and Alarms",
            new[] { "TestServer/Dynamic", "TestServer/Methods", "TestServer/Alarms" },
            externalReferences);

        CreateView("EngineeringView", "Full engineering access to all nodes",
            new[] { "TestServer" },
            externalReferences);

        CreateView("HistoricalView", "Historical data analysis view",
            new[] { "TestServer/Historical" },
            externalReferences);

        CreateView("DataView", "Data types and structures browsing",
            new[] { "TestServer/DataTypes", "TestServer/Structures", "TestServer/ExtensionObjects" },
            externalReferences);
    }

    private void CreateView(string name, string description, string[] targetPaths,
        IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        var view = new ViewState
        {
            SymbolicName = name,
            NodeId = new NodeId($"Views/{name}", _mgr.NamespaceIndex),
            BrowseName = new QualifiedName(name, _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            Description = new LocalizedText("en", description),
            ContainsNoLoops = true,
            EventNotifier = EventNotifiers.None
        };

        // Add references to target folders
        foreach (var path in targetPaths)
        {
            var targetNodeId = new NodeId(path, _mgr.NamespaceIndex);
            view.AddReference(ReferenceTypeIds.Organizes, false, targetNodeId);
        }

        // Register view in the Views folder
        view.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ViewsFolder);

        if (!externalReferences.TryGetValue(ObjectIds.ViewsFolder, out var refs))
        {
            refs = new List<IReference>();
            externalReferences[ObjectIds.ViewsFolder] = refs;
        }
        refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, view.NodeId));

        _mgr.AddNode(_context, view);
    }
}

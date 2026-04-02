using Opc.Ua;
using Opc.Ua.Server;
using TestServer.AddressSpace;
using TestServer.Configuration;
using TestServer.UserManagement;

namespace TestServer.Server;

public class TestNodeManager : CustomNodeManager2
{
    private readonly ServerConfig _config;
    private readonly UserManager _userManager;
    private readonly List<IDisposable> _timers = new();

    /// <summary>
    /// In-memory historical data store, populated by HistoricalBuilder, queried by HistoryReadRawModified.
    /// </summary>
    public Dictionary<NodeId, List<DataValue>> HistoryStore { get; } = new();

    public TestNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration,
        ServerConfig config,
        UserManager userManager)
        : base(server, configuration, new[] { "urn:opcua:testserver:nodes", "http://opcfoundation.org/UA/DI/", "urn:opcua:test-server:custom-types" })
    {
        _config = config;
        _userManager = userManager;
    }

    /// <summary>
    /// Namespace index for custom extension object types (urn:opcua:test-server:custom-types).
    /// Expected to be ns=3 to match test expectations.
    /// </summary>
    public ushort CustomTypesNamespaceIndex => NamespaceIndexes.Count > 2 ? NamespaceIndexes[2] : NamespaceIndex;

    /// <summary>
    /// Exposes AddPredefinedNode to address space builders (it's protected in base class).
    /// </summary>
    public void AddNode(ISystemContext context, NodeState node)
    {
        AddPredefinedNode(context, node);
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            base.CreateAddressSpace(externalReferences);

            var root = CreateFolder(null, "TestServer", "TestServer");
            root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
            AddExternalReference(ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, false, root.NodeId, externalReferences);

            // Build all address space modules
            var dataTypes = new DataTypesBuilder(this, root, SystemContext);
            dataTypes.Build();
            Console.WriteLine("  [+] DataTypes address space built");

            if (_config.EnableMethods)
            {
                var methods = new MethodsBuilder(this, root, SystemContext);
                methods.Build();
                Console.WriteLine("  [+] Methods address space built");
            }

            if (_config.EnableDynamic)
            {
                var dynamic_ = new DynamicBuilder(this, root, SystemContext);
                dynamic_.Build(_timers);
                Console.WriteLine("  [+] Dynamic address space built");
            }

            if (_config.EnableEvents)
            {
                var events = new EventsAlarmsBuilder(this, root, SystemContext);
                events.Build(_timers);
                Console.WriteLine("  [+] Events & Alarms address space built");
            }

            if (_config.EnableHistorical)
            {
                var historical = new HistoricalBuilder(this, root, SystemContext);
                historical.Build(_timers);
                Console.WriteLine("  [+] Historical address space built");
            }

            if (_config.EnableStructures)
            {
                var structures = new StructuresBuilder(this, root, SystemContext);
                structures.Build();
                Console.WriteLine("  [+] Structures address space built");

                var extObjects = new ExtensionObjectsBuilder(this, root, SystemContext);
                extObjects.Build();
                Console.WriteLine("  [+] ExtensionObjects address space built");
            }

            var accessControl = new AccessControlBuilder(this, root, SystemContext, _userManager);
            accessControl.Build();
            Console.WriteLine("  [+] AccessControl address space built");

            if (_config.EnableViews)
            {
                var views = new ViewsBuilder(this, root, SystemContext);
                views.Build(externalReferences);
                Console.WriteLine("  [+] Views address space built");
            }

            Console.WriteLine("Address space created successfully");

            // Apply operation limits to standard server capability nodes
            ApplyOperationLimits();
        }
    }

    private void ApplyOperationLimits()
    {
        try
        {
            // MaxNodesPerRead (i=11705)
            SetStandardNodeValue(new NodeId(11705, 0), (uint)_config.MaxNodesPerRead);
            // MaxNodesPerWrite (i=11707)
            SetStandardNodeValue(new NodeId(11707, 0), (uint)_config.MaxNodesPerWrite);
            // MaxNodesPerBrowse (i=11710)
            SetStandardNodeValue(new NodeId(11710, 0), (uint)_config.MaxNodesPerBrowse);

            Console.WriteLine($"  [+] Operation limits: Read={_config.MaxNodesPerRead}, Write={_config.MaxNodesPerWrite}, Browse={_config.MaxNodesPerBrowse}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [!] Could not set operation limits: {ex.Message}");
        }
    }

    private void SetStandardNodeValue(NodeId nodeId, object value)
    {
        // Find node through the server's DiagnosticsNodeManager which manages standard server nodes
        var node = Server.DiagnosticsNodeManager.FindPredefinedNode(nodeId, typeof(BaseVariableState)) as BaseVariableState;
        if (node != null)
        {
            node.Value = value;
            node.ClearChangeMasks(SystemContext, false);
        }
    }

    public FolderState CreateFolder(NodeState? parent, string path, string name)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None
        };

        parent?.AddChild(folder);
        AddPredefinedNode(SystemContext, folder);
        return folder;
    }

    public BaseDataVariableState<T> CreateVariable<T>(
        NodeState parent, string path, string name,
        NodeId dataType, int valueRank, T defaultValue,
        byte accessLevel = AccessLevels.CurrentReadOrWrite)
    {
        var variable = new BaseDataVariableState<T>(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            DataType = dataType,
            ValueRank = valueRank,
            AccessLevel = accessLevel,
            UserAccessLevel = accessLevel,
            Historizing = false,
            Value = defaultValue,
            StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow
        };

        parent.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);
        return variable;
    }

    public BaseDataVariableState CreateVariableUntyped(
        NodeState parent, string path, string name,
        NodeId dataType, int valueRank, object? defaultValue,
        byte accessLevel = AccessLevels.CurrentReadOrWrite)
    {
        var variable = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            DataType = dataType,
            ValueRank = valueRank,
            AccessLevel = accessLevel,
            UserAccessLevel = accessLevel,
            Historizing = false,
            Value = defaultValue,
            StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow
        };

        parent.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);
        return variable;
    }

    public MethodState CreateMethod(
        NodeState parent, string path, string name,
        Action<IList<object>, IList<object>> handler,
        params Argument[][] args)
    {
        var method = new MethodState(parent)
        {
            SymbolicName = name,
            NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            UserExecutable = true,
            Executable = true
        };

        if (args.Length > 0 && args[0].Length > 0)
        {
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId($"{path}/InputArguments", NamespaceIndex),
                BrowseName = BrowseNames.InputArguments,
                DisplayName = new LocalizedText("en", "InputArguments"),
                TypeDefinitionId = VariableTypeIds.PropertyType,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                DataType = DataTypeIds.Argument,
                ValueRank = ValueRanks.OneDimension,
                Value = args[0]
            };
        }

        if (args.Length > 1 && args[1].Length > 0)
        {
            method.OutputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId($"{path}/OutputArguments", NamespaceIndex),
                BrowseName = BrowseNames.OutputArguments,
                DisplayName = new LocalizedText("en", "OutputArguments"),
                TypeDefinitionId = VariableTypeIds.PropertyType,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                DataType = DataTypeIds.Argument,
                ValueRank = ValueRanks.OneDimension,
                Value = args[1]
            };
        }

        method.OnCallMethod = (ISystemContext context, MethodState methodNode, IList<object> inputArgs, IList<object> outputArgs) =>
        {
            try
            {
                handler(inputArgs, outputArgs);
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
            }
        };

        parent.AddChild(method);
        AddPredefinedNode(SystemContext, method);
        return method;
    }

    protected override void HistoryReadRawModified(
        ServerSystemContext context,
        ReadRawModifiedDetails details,
        TimestampsToReturn timestampsToReturn,
        IList<HistoryReadValueId> nodesToRead,
        IList<HistoryReadResult> results,
        IList<ServiceResult> errors,
        List<NodeHandle> nodesToProcess,
        IDictionary<NodeId, NodeState> cache)
    {
        for (var i = 0; i < nodesToProcess.Count; i++)
        {
            var handle = nodesToProcess[i];
            var nodeToRead = nodesToRead[handle.Index];

            if (!HistoryStore.TryGetValue(handle.NodeId, out var history))
            {
                errors[handle.Index] = StatusCodes.BadHistoryOperationUnsupported;
                continue;
            }

            List<DataValue> filtered;
            lock (history)
            {
                filtered = history
                    .Where(dv =>
                        (details.StartTime == DateTime.MinValue || dv.SourceTimestamp >= details.StartTime) &&
                        (details.EndTime == DateTime.MinValue || dv.SourceTimestamp <= details.EndTime))
                    .OrderBy(dv => dv.SourceTimestamp)
                    .ToList();
            }

            if (details.NumValuesPerNode > 0 && filtered.Count > (int)details.NumValuesPerNode)
            {
                filtered = filtered.Take((int)details.NumValuesPerNode).ToList();
            }

            var historyData = new HistoryData();
            historyData.DataValues.AddRange(filtered);

            results[handle.Index] = new HistoryReadResult
            {
                HistoryData = new ExtensionObject(historyData),
                StatusCode = StatusCodes.Good
            };
            errors[handle.Index] = StatusCodes.Good;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var timer in _timers)
            {
                timer.Dispose();
            }
            _timers.Clear();
        }
        base.Dispose(disposing);
    }
}

using System.Security.Cryptography;
using Opc.Ua;
using Opc.Ua.Server;
using TestServer.Server;
using TestServer.UserManagement;

namespace TestServer.AddressSpace;

/// <summary>
/// Builds File Transfer (Part 5) fixtures in <c>TestServer/Files/</c>.
///
/// Backing storage is per-process <see cref="MemoryStream"/> — every container
/// restart resets all files to their initial seed. No disk I/O, no volumes,
/// no cleanup between test runs.
///
/// Scope v1 (this build): four <c>FileType</c> nodes covering the read,
/// write, chunked-read, and round-trip scenarios most client implementations
/// need to exercise. <c>FileDirectoryType</c> + <c>AppendableFile</c> are
/// deferred to v1.1.
/// </summary>
public class FileTransferBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;
    private readonly UserManager _userManager;

    private const int LargeFileSize = 262144;

    public FileTransferBuilder(TestNodeManager mgr, FolderState root, ISystemContext context, UserManager userManager)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
        _userManager = userManager;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/Files", "Files");

        BuildFile(folder, "ReadOnlyFile",   writable: false, GetReadOnlySeed(),     mimeType: "application/octet-stream");
        BuildFile(folder, "EmptyFile",      writable: false, Array.Empty<byte>(),   mimeType: "text/plain");
        BuildFile(folder, "LargeFile",      writable: false, GetLargeFileSeed(),    mimeType: "application/octet-stream");
        BuildFile(folder, "WritableFile",   writable: true,  Array.Empty<byte>(),   mimeType: "application/octet-stream");
        BuildFile(folder, "AppendableFile", writable: true,  GetAppendableSeed(),   mimeType: "text/plain");

        BuildProtectedWritableFile(folder);

        BuildRootDir(folder);
    }

    // ---------------------------------------------------------------------
    // ProtectedWritableFile — per-user UserWritable (admin-only on Write).
    // ---------------------------------------------------------------------

    /// <summary>
    /// A FileType whose <c>Writable</c> property is <c>true</c> (advertising
    /// the capability) but whose <c>Open(Write)</c> callback enforces a
    /// per-session role check: only <c>admin</c> users may obtain a write
    /// handle. <c>anonymous</c>, <c>operator</c>, and <c>viewer</c> sessions
    /// receive <c>Bad_UserAccessDenied</c> on Open(Write); Open(Read) is
    /// allowed for every session.
    /// </summary>
    private void BuildProtectedWritableFile(FolderState parent)
    {
        var name = "ProtectedWritableFile";
        var path = $"TestServer/Files/{name}";
        var backend = new InMemoryFileBackend(Array.Empty<byte>(), writable: true);
        var file = BuildFileNode(parent, name, path, backend, mimeType: "application/octet-stream");

        // Override Open to add the role check before delegating to the backend.
        file.Open.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, byte mode, ref uint fileHandle) =>
        {
            try
            {
                var wantsWrite = (mode & 0x02) != 0;
                if (wantsWrite && ! IsAdminSession(ctx))
                {
                    return new ServiceResult(StatusCodes.BadUserAccessDenied, "ProtectedWritableFile: Write requires the 'admin' role");
                }

                fileHandle = backend.Open(mode);
                file.OpenCount.Value = backend.OpenCount;
                file.OpenCount.ClearChangeMasks(ctx, false);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };
    }

    /// <summary>
    /// Inspect the session identity attached to {@param ctx}. UA-.NETStandard
    /// exposes the active <see cref="IUserIdentity"/> via
    /// <see cref="ISystemContext.UserIdentity"/>. We look up the role through
    /// the suite's <see cref="UserManager"/> by the identity's display name.
    /// </summary>
    private bool IsAdminSession(ISystemContext ctx)
    {
        if (ctx is not ServerSystemContext serverCtx || serverCtx.UserIdentity is null)
        {
            return false;
        }
        var displayName = serverCtx.UserIdentity.DisplayName;
        return ! string.IsNullOrEmpty(displayName) && _userManager.IsAdmin(displayName);
    }

    // ---------------------------------------------------------------------
    // Seed generators
    // ---------------------------------------------------------------------

    /// <summary>1024 bytes — MD5("readonly-seed") repeated 64 times. Deterministic across boots.</summary>
    private static byte[] GetReadOnlySeed()
    {
        var block = MD5.HashData(System.Text.Encoding.ASCII.GetBytes("readonly-seed"));
        var buffer = new byte[1024];
        for (var i = 0; i < 64; i++)
            Buffer.BlockCopy(block, 0, buffer, i * 16, 16);
        return buffer;
    }

    /// <summary>262144 bytes — sequence 0,1,2,…,255 repeated 1024 times.</summary>
    private static byte[] GetLargeFileSeed()
    {
        var buffer = new byte[LargeFileSize];
        for (var i = 0; i < LargeFileSize; i++)
            buffer[i] = (byte)(i & 0xFF);
        return buffer;
    }

    /// <summary>13 bytes — ASCII "seed-content\n" — predictable starting offset for Append-mode tests.</summary>
    private static byte[] GetAppendableSeed()
    {
        return System.Text.Encoding.ASCII.GetBytes("seed-content\n");
    }

    // ---------------------------------------------------------------------
    // Backend
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // FileDirectoryType — RootDir with the standard 4 management methods.
    // ---------------------------------------------------------------------

    private void BuildRootDir(FolderState parent)
    {
        var path = "TestServer/Files/RootDir";
        var dir = new FileDirectoryState(parent)
        {
            SymbolicName = "RootDir",
            NodeId = new NodeId(path, _mgr.NamespaceIndex),
            BrowseName = new QualifiedName("RootDir", _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", "RootDir"),
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FileDirectoryType,
            EventNotifier = EventNotifiers.None,
        };
        dir.Create(_context, dir.NodeId, dir.BrowseName, null, true);

        WireDirectoryCallbacks(dir, parentBrowsePath: path);

        parent.AddChild(dir);
        _mgr.AddNode(_context, dir);
    }

    /// <summary>
    /// Track all (file-or-directory) state instances created at runtime via
    /// CreateFile/CreateDirectory so DeleteFileSystemObject and MoveOrCopy
    /// can find them by NodeId.
    /// </summary>
    private readonly Dictionary<NodeId, NodeState> _dynamicNodes = new();

    /// <summary>
    /// File backends for runtime-created FileType nodes. Keyed by NodeId so
    /// CreateFile can return the live backend ref while DeleteFileSystemObject
    /// can dispose handles when the node disappears.
    /// </summary>
    private readonly Dictionary<NodeId, InMemoryFileBackend> _dynamicBackends = new();

    private void WireDirectoryCallbacks(FileDirectoryState dir, string parentBrowsePath)
    {
        dir.CreateDirectory.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, string directoryName, ref NodeId directoryNodeId) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryName))
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "directoryName must not be empty");
                var childPath = $"{parentBrowsePath}/{directoryName}";

                var child = new FileDirectoryState(dir)
                {
                    SymbolicName = directoryName,
                    NodeId = new NodeId(childPath, _mgr.NamespaceIndex),
                    BrowseName = new QualifiedName(directoryName, _mgr.NamespaceIndex),
                    DisplayName = new LocalizedText("en", directoryName),
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    TypeDefinitionId = ObjectTypeIds.FileDirectoryType,
                };
                child.Create(_context, child.NodeId, child.BrowseName, null, true);
                WireDirectoryCallbacks(child, parentBrowsePath: childPath);

                dir.AddChild(child);
                _mgr.AddNode(_context, child);
                _dynamicNodes[child.NodeId] = child;

                directoryNodeId = child.NodeId;
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        dir.CreateFile.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, string fileName, bool requestFileOpen, ref NodeId fileNodeId, ref uint fileHandle) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "fileName must not be empty");
                var childPath = $"{parentBrowsePath}/{fileName}";
                var backend = new InMemoryFileBackend(Array.Empty<byte>(), writable: true);

                var child = BuildFileNode(dir, fileName, childPath, backend, mimeType: "application/octet-stream");

                _dynamicNodes[child.NodeId] = child;
                _dynamicBackends[child.NodeId] = backend;

                fileNodeId = child.NodeId;
                fileHandle = requestFileOpen ? backend.Open(mode: 3 /* Read | Write */) : 0u;

                if (requestFileOpen)
                {
                    child.OpenCount.Value = backend.OpenCount;
                    child.OpenCount.ClearChangeMasks(ctx, false);
                }
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        dir.DeleteFileSystemObject.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, NodeId objectToDelete) =>
        {
            try
            {
                if (objectToDelete == null || objectToDelete.IsNullNodeId)
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "objectToDelete must not be null");
                if (!_dynamicNodes.TryGetValue(objectToDelete, out var node))
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown dynamic node");

                _mgr.DeleteDynamicNode(objectToDelete);
                _dynamicNodes.Remove(objectToDelete);
                _dynamicBackends.Remove(objectToDelete);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        dir.MoveOrCopy.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, NodeId objectToMoveOrCopy, NodeId targetDirectoryId, bool createCopy, string newName, ref NodeId newNodeId) =>
        {
            try
            {
                if (!_dynamicNodes.TryGetValue(objectToMoveOrCopy, out var sourceNode))
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown source node");
                if (!_dynamicNodes.TryGetValue(targetDirectoryId, out var targetNode) && targetDirectoryId != dir.NodeId)
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown target directory");
                var targetDir = targetNode as FileDirectoryState ?? dir;
                var resolvedNewName = string.IsNullOrWhiteSpace(newName)
                    ? sourceNode.BrowseName.Name
                    : newName;
                var targetParentPath = targetDirectoryId == dir.NodeId
                    ? parentBrowsePath
                    : (targetDirectoryId.Identifier as string ?? parentBrowsePath);
                var destPath = $"{targetParentPath}/{resolvedNewName}";
                var destNodeId = new NodeId(destPath, _mgr.NamespaceIndex);

                if (sourceNode is FileState srcFile && _dynamicBackends.TryGetValue(sourceNode.NodeId, out var srcBackend))
                {
                    var clonedBackend = new InMemoryFileBackend(srcBackend.Snapshot(), writable: true);
                    var dest = BuildFileNode(targetDir, resolvedNewName, destPath, clonedBackend, mimeType: "application/octet-stream");
                    _dynamicNodes[dest.NodeId] = dest;
                    _dynamicBackends[dest.NodeId] = clonedBackend;
                    newNodeId = dest.NodeId;
                }
                else if (sourceNode is FileDirectoryState)
                {
                    // Directories: shallow copy (empty new dir at destination) is the v1 contract.
                    var destDir = new FileDirectoryState(targetDir)
                    {
                        SymbolicName = resolvedNewName,
                        NodeId = destNodeId,
                        BrowseName = new QualifiedName(resolvedNewName, _mgr.NamespaceIndex),
                        DisplayName = new LocalizedText("en", resolvedNewName),
                        ReferenceTypeId = ReferenceTypeIds.Organizes,
                        TypeDefinitionId = ObjectTypeIds.FileDirectoryType,
                    };
                    destDir.Create(_context, destDir.NodeId, destDir.BrowseName, null, true);
                    WireDirectoryCallbacks(destDir, parentBrowsePath: destPath);
                    targetDir.AddChild(destDir);
                    _mgr.AddNode(_context, destDir);
                    _dynamicNodes[destDir.NodeId] = destDir;
                    newNodeId = destDir.NodeId;
                }
                else
                {
                    throw new ServiceResultException(StatusCodes.BadServiceUnsupported, "Unsupported source type");
                }

                if (!createCopy)
                {
                    _mgr.DeleteDynamicNode(sourceNode.NodeId);
                    _dynamicNodes.Remove(sourceNode.NodeId);
                    _dynamicBackends.Remove(sourceNode.NodeId);
                }
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };
    }

    /// <summary>
    /// Shared helper that builds a FileState node + property set, wires callbacks,
    /// and adds it to the address space under {@param parent}. Returns the new
    /// FileState so the caller can finalize it (e.g. open a handle on the same
    /// backend).
    /// </summary>
    private FileState BuildFileNode(NodeState parent, string name, string path, InMemoryFileBackend backend, string mimeType)
    {
        var file = new FileState(parent)
        {
            SymbolicName = name,
            NodeId = new NodeId(path, _mgr.NamespaceIndex),
            BrowseName = new QualifiedName(name, _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FileType,
            EventNotifier = EventNotifiers.None,
        };
        file.Create(_context, file.NodeId, file.BrowseName, null, true);

        file.Size = new PropertyState<ulong>(file)
        {
            NodeId = new NodeId($"{path}/Size", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.Size,
            DisplayName = BrowseNames.Size,
            DataType = DataTypeIds.UInt64,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = (ulong)backend.Length,
        };
        file.Writable = MakeBoolProperty(file, $"{path}/Writable", BrowseNames.Writable, true);
        file.UserWritable = MakeBoolProperty(file, $"{path}/UserWritable", BrowseNames.UserWritable, true);
        file.OpenCount = new PropertyState<ushort>(file)
        {
            NodeId = new NodeId($"{path}/OpenCount", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.OpenCount,
            DisplayName = BrowseNames.OpenCount,
            DataType = DataTypeIds.UInt16,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = (ushort)0,
        };
        file.MimeType = new PropertyState<string>(file)
        {
            NodeId = new NodeId($"{path}/MimeType", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.MimeType,
            DisplayName = BrowseNames.MimeType,
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = mimeType,
        };

        WireCallbacks(file, backend);

        parent.AddChild(file);
        _mgr.AddNode(_context, file);
        return file;
    }

    private void BuildFile(FolderState parent, string name, bool writable, byte[] seed, string mimeType)
    {
        var path = $"TestServer/Files/{name}";
        var backend = new InMemoryFileBackend(seed, writable);

        var file = new FileState(parent)
        {
            SymbolicName = name,
            NodeId = new NodeId(path, _mgr.NamespaceIndex),
            BrowseName = new QualifiedName(name, _mgr.NamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FileType,
            EventNotifier = EventNotifiers.None,
        };

        file.Create(_context, file.NodeId, file.BrowseName, null, true);

        file.Size = new PropertyState<ulong>(file)
        {
            NodeId = new NodeId($"{path}/Size", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.Size,
            DisplayName = BrowseNames.Size,
            DataType = DataTypeIds.UInt64,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = (ulong)seed.LongLength,
        };
        file.Writable = MakeBoolProperty(file, $"{path}/Writable", BrowseNames.Writable, writable);
        file.UserWritable = MakeBoolProperty(file, $"{path}/UserWritable", BrowseNames.UserWritable, writable);
        file.OpenCount = new PropertyState<ushort>(file)
        {
            NodeId = new NodeId($"{path}/OpenCount", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.OpenCount,
            DisplayName = BrowseNames.OpenCount,
            DataType = DataTypeIds.UInt16,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = (ushort)0,
        };
        file.MimeType = new PropertyState<string>(file)
        {
            NodeId = new NodeId($"{path}/MimeType", _mgr.NamespaceIndex),
            BrowseName = BrowseNames.MimeType,
            DisplayName = BrowseNames.MimeType,
            DataType = DataTypeIds.String,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = mimeType,
        };

        WireCallbacks(file, backend);

        parent.AddChild(file);
        _mgr.AddNode(_context, file);
    }

    private PropertyState<bool> MakeBoolProperty(FileState parent, string path, string browseName, bool value)
    {
        return new PropertyState<bool>(parent)
        {
            NodeId = new NodeId(path, _mgr.NamespaceIndex),
            BrowseName = browseName,
            DisplayName = browseName,
            DataType = DataTypeIds.Boolean,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = value,
        };
    }

    private static void WireCallbacks(FileState file, InMemoryFileBackend backend)
    {
        file.Open.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, byte mode, ref uint fileHandle) =>
        {
            try
            {
                fileHandle = backend.Open(mode);
                file.OpenCount.Value = backend.OpenCount;
                file.OpenCount.ClearChangeMasks(ctx, false);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        file.Close.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, uint fileHandle) =>
        {
            try
            {
                backend.Close(fileHandle);
                file.OpenCount.Value = backend.OpenCount;
                file.OpenCount.ClearChangeMasks(ctx, false);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        file.Read.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, uint fileHandle, int length, ref byte[] data) =>
        {
            try { data = backend.Read(fileHandle, length); return ServiceResult.Good; }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        file.Write.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, uint fileHandle, byte[] data) =>
        {
            try
            {
                backend.Write(fileHandle, data);
                file.Size.Value = (ulong)backend.Length;
                file.Size.ClearChangeMasks(ctx, false);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        file.GetPosition.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, uint fileHandle, ref ulong position) =>
        {
            try { position = backend.GetPosition(fileHandle); return ServiceResult.Good; }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };

        file.SetPosition.OnCall = (ISystemContext ctx, MethodState m, NodeId objectId, uint fileHandle, ulong position) =>
        {
            try { backend.SetPosition(fileHandle, position); return ServiceResult.Good; }
            catch (ServiceResultException ex) { return new ServiceResult(ex); }
        };
    }
}

/// <summary>
/// MemoryStream-backed implementation of the FileType behaviour.
/// One backend instance per file; multiple handles per backend.
/// Thread-safe via a single lock — UA-.NETStandard dispatches OnCall from
/// its session worker pool, so concurrent handle access is real.
/// </summary>
internal sealed class InMemoryFileBackend
{
    private readonly object _lock = new();
    private readonly bool _writable;
    private MemoryStream _stream;
    private readonly Dictionary<uint, HandleState> _handles = new();
    private uint _nextHandle = 1;

    public int Length { get { lock (_lock) return (int)_stream.Length; } }
    public ushort OpenCount { get { lock (_lock) return (ushort)_handles.Count; } }

    /// <summary>Return a copy of the current contents — used by MoveOrCopy(createCopy=true).</summary>
    public byte[] Snapshot()
    {
        lock (_lock)
        {
            return _stream.ToArray();
        }
    }

    public InMemoryFileBackend(byte[] seed, bool writable)
    {
        _writable = writable;
        _stream = new MemoryStream();
        if (seed.Length > 0)
            _stream.Write(seed, 0, seed.Length);
        _stream.Position = 0;
    }

    public uint Open(byte mode)
    {
        // Part 5 §C.2.1 — mode is a bit field:
        //   1 = Read, 2 = Write, 4 = EraseExisting (only with Write), 8 = Append
        var read = (mode & 0x01) != 0;
        var write = (mode & 0x02) != 0;
        var erase = (mode & 0x04) != 0;
        var append = (mode & 0x08) != 0;

        if (!read && !write)
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Open mode must include Read or Write");
        if (write && !_writable)
            throw new ServiceResultException(StatusCodes.BadNotWritable, "File is read-only");
        if (erase && !write)
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "EraseExisting requires Write");

        lock (_lock)
        {
            // EraseExisting truncates atomically at open time.
            if (erase)
            {
                _stream.Dispose();
                _stream = new MemoryStream();
            }

            var handle = _nextHandle++;
            _handles[handle] = new HandleState
            {
                Mode = mode,
                Position = append ? (ulong)_stream.Length : 0,
            };
            return handle;
        }
    }

    public void Close(uint handle)
    {
        lock (_lock)
        {
            if (!_handles.Remove(handle))
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Unknown file handle");
        }
    }

    public byte[] Read(uint handle, int length)
    {
        if (length < 0)
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Length must be non-negative");

        lock (_lock)
        {
            var state = LookupForRead(handle);
            _stream.Position = (long)state.Position;
            var buffer = new byte[length];
            var read = _stream.Read(buffer, 0, length);
            state.Position = (ulong)_stream.Position;
            if (read == length) return buffer;
            // Short read at EOF — trim.
            var trimmed = new byte[read];
            if (read > 0) Buffer.BlockCopy(buffer, 0, trimmed, 0, read);
            return trimmed;
        }
    }

    public void Write(uint handle, byte[] data)
    {
        lock (_lock)
        {
            var state = LookupForWrite(handle);
            _stream.Position = (long)state.Position;
            _stream.Write(data, 0, data.Length);
            state.Position = (ulong)_stream.Position;
        }
    }

    public ulong GetPosition(uint handle)
    {
        lock (_lock)
        {
            if (!_handles.TryGetValue(handle, out var state))
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Unknown file handle");
            return state.Position;
        }
    }

    public void SetPosition(uint handle, ulong position)
    {
        lock (_lock)
        {
            if (!_handles.TryGetValue(handle, out var state))
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Unknown file handle");
            state.Position = position;
        }
    }

    private HandleState LookupForRead(uint handle)
    {
        if (!_handles.TryGetValue(handle, out var state))
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Unknown file handle");
        if ((state.Mode & 0x01) == 0)
            throw new ServiceResultException(StatusCodes.BadInvalidState, "Handle was not opened for Read");
        return state;
    }

    private HandleState LookupForWrite(uint handle)
    {
        if (!_handles.TryGetValue(handle, out var state))
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Unknown file handle");
        if ((state.Mode & 0x02) == 0)
            throw new ServiceResultException(StatusCodes.BadInvalidState, "Handle was not opened for Write");
        return state;
    }

    private sealed class HandleState
    {
        public byte Mode;
        public ulong Position;
    }
}

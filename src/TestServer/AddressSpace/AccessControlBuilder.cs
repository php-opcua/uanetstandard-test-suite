using Opc.Ua;
using Opc.Ua.Server;
using TestServer.Server;
using TestServer.UserManagement;

namespace TestServer.AddressSpace;

public class AccessControlBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;
    private readonly UserManager _userManager;

    public AccessControlBuilder(TestNodeManager mgr, FolderState root, ISystemContext context, UserManager userManager)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
        _userManager = userManager;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/AccessControl", "AccessControl");
        var p = "TestServer/AccessControl";

        BuildAccessLevels(folder, p);
        BuildAdminOnly(folder, p);
        BuildOperatorLevel(folder, p);
        BuildViewerLevel(folder, p);
        BuildAllCombinations(folder, p);
    }

    private void BuildAccessLevels(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/AccessLevels", "AccessLevels");
        var p = $"{basePath}/AccessLevels";

        _mgr.CreateVariable<int>(folder, $"{p}/CurrentRead_Only", "CurrentRead_Only",
            DataTypeIds.Int32, ValueRanks.Scalar, 42, AccessLevels.CurrentRead);

        _mgr.CreateVariable<int>(folder, $"{p}/CurrentWrite_Only", "CurrentWrite_Only",
            DataTypeIds.Int32, ValueRanks.Scalar, 0, AccessLevels.CurrentWrite);

        _mgr.CreateVariable<int>(folder, $"{p}/ReadWrite", "ReadWrite",
            DataTypeIds.Int32, ValueRanks.Scalar, 100, AccessLevels.CurrentReadOrWrite);

        _mgr.CreateVariable<int>(folder, $"{p}/HistoryRead_Only", "HistoryRead_Only",
            DataTypeIds.Int32, ValueRanks.Scalar, 200, (byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));

        _mgr.CreateVariable<int>(folder, $"{p}/FullAccess", "FullAccess",
            DataTypeIds.Int32, ValueRanks.Scalar, 300,
            (byte)(AccessLevels.CurrentReadOrWrite | AccessLevels.HistoryRead));
    }

    private void BuildAdminOnly(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/AdminOnly", "AdminOnly");
        var p = $"{basePath}/AdminOnly";

        _mgr.CreateVariable<string>(folder, $"{p}/SecretConfig", "SecretConfig",
            DataTypeIds.String, ValueRanks.Scalar, "secret-value-123");
        _mgr.CreateVariable<int>(folder, $"{p}/SystemParameter", "SystemParameter",
            DataTypeIds.Int32, ValueRanks.Scalar, 9999);
        _mgr.CreateVariable<double>(folder, $"{p}/CalibrationFactor", "CalibrationFactor",
            DataTypeIds.Double, ValueRanks.Scalar, 1.0);
        _mgr.CreateVariable<bool>(folder, $"{p}/MaintenanceMode", "MaintenanceMode",
            DataTypeIds.Boolean, ValueRanks.Scalar, false);
    }

    private void BuildOperatorLevel(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/OperatorLevel", "OperatorLevel");
        var p = $"{basePath}/OperatorLevel";

        CreateRoleProtectedVariable<double>(folder, $"{p}/Setpoint", "Setpoint",
            DataTypeIds.Double, ValueRanks.Scalar, 50.0, "operator");
        CreateRoleProtectedVariable<int>(folder, $"{p}/MotorSpeed", "MotorSpeed",
            DataTypeIds.Int32, ValueRanks.Scalar, 1500, "operator");
        CreateRoleProtectedVariable<bool>(folder, $"{p}/ProcessEnabled", "ProcessEnabled",
            DataTypeIds.Boolean, ValueRanks.Scalar, true, "operator");
        CreateRoleProtectedVariable<string>(folder, $"{p}/RecipeName", "RecipeName",
            DataTypeIds.String, ValueRanks.Scalar, "Recipe_A", "operator");
    }

    private void CreateRoleProtectedVariable<T>(
        NodeState parent, string path, string name,
        NodeId dataType, int valueRank, T defaultValue,
        string minimumRole)
    {
        var variable = _mgr.CreateVariable<T>(parent, path, name, dataType, valueRank, defaultValue);

        variable.OnWriteValue = (ISystemContext context, NodeState node, NumericRange indexRange,
            QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp) =>
        {
            var username = GetUsername(context);
            if (username == null)
            {
                // Anonymous user - deny write
                return StatusCodes.BadUserAccessDenied;
            }

            var hasAccess = minimumRole switch
            {
                "admin" => _userManager.IsAdmin(username),
                "operator" => _userManager.IsOperator(username),
                _ => true
            };

            if (!hasAccess)
            {
                return StatusCodes.BadUserAccessDenied;
            }

            return ServiceResult.Good;
        };
    }

    private static string? GetUsername(ISystemContext context)
    {
        if (context is ISessionSystemContext sessionContext)
        {
            var identity = sessionContext.UserIdentity;
            if (identity != null)
            {
                return identity.DisplayName;
            }
        }

        return null;
    }

    private void BuildViewerLevel(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/ViewerLevel", "ViewerLevel");
        var p = $"{basePath}/ViewerLevel";
        var ro = AccessLevels.CurrentRead;

        _mgr.CreateVariable<uint>(folder, $"{p}/ProductionCount", "ProductionCount",
            DataTypeIds.UInt32, ValueRanks.Scalar, 12345u, ro);
        _mgr.CreateVariable<string>(folder, $"{p}/MachineName", "MachineName",
            DataTypeIds.String, ValueRanks.Scalar, "Machine-001", ro);
        _mgr.CreateVariable<bool>(folder, $"{p}/IsRunning", "IsRunning",
            DataTypeIds.Boolean, ValueRanks.Scalar, true, ro);
        _mgr.CreateVariable<double>(folder, $"{p}/CurrentTemperature", "CurrentTemperature",
            DataTypeIds.Double, ValueRanks.Scalar, 45.2, ro);
        _mgr.CreateVariable<uint>(folder, $"{p}/UptimeSeconds", "UptimeSeconds",
            DataTypeIds.UInt32, ValueRanks.Scalar, 86400u, ro);
    }

    private void BuildAllCombinations(FolderState parent, string basePath)
    {
        var folder = _mgr.CreateFolder(parent, $"{basePath}/AllCombinations", "AllCombinations");
        var p = $"{basePath}/AllCombinations";

        var types = new (string Name, NodeId DataType, object RoValue, object RwValue, object WoValue, object HrValue)[]
        {
            ("Boolean", DataTypeIds.Boolean, true, false, false, true),
            ("Int32", DataTypeIds.Int32, 42, 0, 0, 100),
            ("UInt32", DataTypeIds.UInt32, 42u, 0u, 0u, 100u),
            ("Double", DataTypeIds.Double, 3.14, 0.0, 0.0, 2.71),
            ("String", DataTypeIds.String, "readonly", "readwrite", "writeonly", "history"),
            ("DateTime", DataTypeIds.DateTime, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow),
            ("Byte", DataTypeIds.Byte, (byte)0xFF, (byte)0, (byte)0, (byte)0xAB),
            ("Float", DataTypeIds.Float, 1.5f, 0f, 0f, 2.5f),
        };

        foreach (var (name, dataType, roValue, rwValue, woValue, hrValue) in types)
        {
            _mgr.CreateVariableUntyped(folder, $"{p}/{name}_RO", $"{name}_RO",
                dataType, ValueRanks.Scalar, roValue, AccessLevels.CurrentRead);

            _mgr.CreateVariableUntyped(folder, $"{p}/{name}_RW", $"{name}_RW",
                dataType, ValueRanks.Scalar, rwValue, AccessLevels.CurrentReadOrWrite);

            _mgr.CreateVariableUntyped(folder, $"{p}/{name}_WO", $"{name}_WO",
                dataType, ValueRanks.Scalar, woValue, AccessLevels.CurrentWrite);

            _mgr.CreateVariableUntyped(folder, $"{p}/{name}_HR", $"{name}_HR",
                dataType, ValueRanks.Scalar, hrValue, (byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));
        }
    }
}

using System.Xml;
using Opc.Ua;
using TestServer.Server;
using Range = Opc.Ua.Range;

namespace TestServer.AddressSpace;

public class DataTypesBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;

    public DataTypesBuilder(TestNodeManager mgr, FolderState root, ISystemContext context)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/DataTypes", "DataTypes");
        BuildScalar(folder);
        BuildReadOnly(folder);
        BuildArrays(folder);
        BuildMultiDimensional(folder);
        BuildWithRange(folder);
    }

    private void BuildScalar(FolderState parent)
    {
        var folder = _mgr.CreateFolder(parent, "TestServer/DataTypes/Scalar", "Scalar");
        var p = "TestServer/DataTypes/Scalar";

        _mgr.CreateVariable<bool>(folder, $"{p}/BooleanValue", "BooleanValue", DataTypeIds.Boolean, ValueRanks.Scalar, true);
        _mgr.CreateVariable<sbyte>(folder, $"{p}/SByteValue", "SByteValue", DataTypeIds.SByte, ValueRanks.Scalar, (sbyte)-42);
        _mgr.CreateVariable<byte>(folder, $"{p}/ByteValue", "ByteValue", DataTypeIds.Byte, ValueRanks.Scalar, (byte)42);
        _mgr.CreateVariable<short>(folder, $"{p}/Int16Value", "Int16Value", DataTypeIds.Int16, ValueRanks.Scalar, (short)-1000);
        _mgr.CreateVariable<ushort>(folder, $"{p}/UInt16Value", "UInt16Value", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)1000);
        _mgr.CreateVariable<int>(folder, $"{p}/Int32Value", "Int32Value", DataTypeIds.Int32, ValueRanks.Scalar, -100000);
        _mgr.CreateVariable<uint>(folder, $"{p}/UInt32Value", "UInt32Value", DataTypeIds.UInt32, ValueRanks.Scalar, 100000u);
        _mgr.CreateVariable<long>(folder, $"{p}/Int64Value", "Int64Value", DataTypeIds.Int64, ValueRanks.Scalar, -1000000L);
        _mgr.CreateVariable<ulong>(folder, $"{p}/UInt64Value", "UInt64Value", DataTypeIds.UInt64, ValueRanks.Scalar, 1000000UL);
        _mgr.CreateVariable<float>(folder, $"{p}/FloatValue", "FloatValue", DataTypeIds.Float, ValueRanks.Scalar, 3.14f);
        _mgr.CreateVariable<double>(folder, $"{p}/DoubleValue", "DoubleValue", DataTypeIds.Double, ValueRanks.Scalar, 2.71828);
        _mgr.CreateVariable<string>(folder, $"{p}/StringValue", "StringValue", DataTypeIds.String, ValueRanks.Scalar, "Hello OPC UA");
        _mgr.CreateVariable<DateTime>(folder, $"{p}/DateTimeValue", "DateTimeValue", DataTypeIds.DateTime, ValueRanks.Scalar, DateTime.UtcNow);
        _mgr.CreateVariable<Guid>(folder, $"{p}/GuidValue", "GuidValue", DataTypeIds.Guid, ValueRanks.Scalar, Guid.NewGuid());
        _mgr.CreateVariable<byte[]>(folder, $"{p}/ByteStringValue", "ByteStringValue", DataTypeIds.ByteString, ValueRanks.Scalar, new byte[] { 0x01, 0x02, 0x03, 0x04 });
        _mgr.CreateVariable<XmlElement>(folder, $"{p}/XmlElementValue", "XmlElementValue", DataTypeIds.XmlElement, ValueRanks.Scalar, default(XmlElement)!);
        _mgr.CreateVariable<NodeId>(folder, $"{p}/NodeIdValue", "NodeIdValue", DataTypeIds.NodeId, ValueRanks.Scalar, new NodeId(1234, 0));
        _mgr.CreateVariable<ExpandedNodeId>(folder, $"{p}/ExpandedNodeIdValue", "ExpandedNodeIdValue", DataTypeIds.ExpandedNodeId, ValueRanks.Scalar, new ExpandedNodeId(5678, 0));
        _mgr.CreateVariable<StatusCode>(folder, $"{p}/StatusCodeValue", "StatusCodeValue", DataTypeIds.StatusCode, ValueRanks.Scalar, StatusCodes.Good);
        _mgr.CreateVariable<QualifiedName>(folder, $"{p}/QualifiedNameValue", "QualifiedNameValue", DataTypeIds.QualifiedName, ValueRanks.Scalar, new QualifiedName("TestName", 1));
        _mgr.CreateVariable<LocalizedText>(folder, $"{p}/LocalizedTextValue", "LocalizedTextValue", DataTypeIds.LocalizedText, ValueRanks.Scalar, new LocalizedText("en", "Test Text"));
    }

    private void BuildReadOnly(FolderState parent)
    {
        var folder = _mgr.CreateFolder(parent, "TestServer/DataTypes/ReadOnly", "ReadOnly");
        var p = "TestServer/DataTypes/ReadOnly";
        var ro = AccessLevels.CurrentRead;

        _mgr.CreateVariable<bool>(folder, $"{p}/Boolean_RO", "Boolean_RO", DataTypeIds.Boolean, ValueRanks.Scalar, true, ro);
        _mgr.CreateVariable<sbyte>(folder, $"{p}/SByte_RO", "SByte_RO", DataTypeIds.SByte, ValueRanks.Scalar, (sbyte)-10, ro);
        _mgr.CreateVariable<byte>(folder, $"{p}/Byte_RO", "Byte_RO", DataTypeIds.Byte, ValueRanks.Scalar, (byte)10, ro);
        _mgr.CreateVariable<short>(folder, $"{p}/Int16_RO", "Int16_RO", DataTypeIds.Int16, ValueRanks.Scalar, (short)-500, ro);
        _mgr.CreateVariable<ushort>(folder, $"{p}/UInt16_RO", "UInt16_RO", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)500, ro);
        _mgr.CreateVariable<int>(folder, $"{p}/Int32_RO", "Int32_RO", DataTypeIds.Int32, ValueRanks.Scalar, -50000, ro);
        _mgr.CreateVariable<uint>(folder, $"{p}/UInt32_RO", "UInt32_RO", DataTypeIds.UInt32, ValueRanks.Scalar, 50000u, ro);
        _mgr.CreateVariable<long>(folder, $"{p}/Int64_RO", "Int64_RO", DataTypeIds.Int64, ValueRanks.Scalar, -500000L, ro);
        _mgr.CreateVariable<ulong>(folder, $"{p}/UInt64_RO", "UInt64_RO", DataTypeIds.UInt64, ValueRanks.Scalar, 500000UL, ro);
        _mgr.CreateVariable<float>(folder, $"{p}/Float_RO", "Float_RO", DataTypeIds.Float, ValueRanks.Scalar, 1.618f, ro);
        _mgr.CreateVariable<double>(folder, $"{p}/Double_RO", "Double_RO", DataTypeIds.Double, ValueRanks.Scalar, 1.41421, ro);
        _mgr.CreateVariable<string>(folder, $"{p}/String_RO", "String_RO", DataTypeIds.String, ValueRanks.Scalar, "ReadOnly String", ro);
        _mgr.CreateVariable<DateTime>(folder, $"{p}/DateTime_RO", "DateTime_RO", DataTypeIds.DateTime, ValueRanks.Scalar, DateTime.UtcNow, ro);
        _mgr.CreateVariable<Guid>(folder, $"{p}/Guid_RO", "Guid_RO", DataTypeIds.Guid, ValueRanks.Scalar, Guid.Parse("12345678-1234-1234-1234-123456789abc"), ro);
        _mgr.CreateVariable<byte[]>(folder, $"{p}/ByteString_RO", "ByteString_RO", DataTypeIds.ByteString, ValueRanks.Scalar, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, ro);
        _mgr.CreateVariable<XmlElement>(folder, $"{p}/XmlElement_RO", "XmlElement_RO", DataTypeIds.XmlElement, ValueRanks.Scalar, default(XmlElement)!, ro);
        _mgr.CreateVariable<NodeId>(folder, $"{p}/NodeId_RO", "NodeId_RO", DataTypeIds.NodeId, ValueRanks.Scalar, new NodeId(9999, 0), ro);
        _mgr.CreateVariable<ExpandedNodeId>(folder, $"{p}/ExpandedNodeId_RO", "ExpandedNodeId_RO", DataTypeIds.ExpandedNodeId, ValueRanks.Scalar, new ExpandedNodeId(8888, 0), ro);
        _mgr.CreateVariable<StatusCode>(folder, $"{p}/StatusCode_RO", "StatusCode_RO", DataTypeIds.StatusCode, ValueRanks.Scalar, StatusCodes.Good, ro);
        _mgr.CreateVariable<QualifiedName>(folder, $"{p}/QualifiedName_RO", "QualifiedName_RO", DataTypeIds.QualifiedName, ValueRanks.Scalar, new QualifiedName("ReadOnly", 1), ro);
        _mgr.CreateVariable<LocalizedText>(folder, $"{p}/LocalizedText_RO", "LocalizedText_RO", DataTypeIds.LocalizedText, ValueRanks.Scalar, new LocalizedText("en", "ReadOnly Text"), ro);
    }

    private void BuildArrays(FolderState parent)
    {
        var folder = _mgr.CreateFolder(parent, "TestServer/DataTypes/Array", "Array");
        var p = "TestServer/DataTypes/Array";

        _mgr.CreateVariable<bool[]>(folder, $"{p}/BooleanArray", "BooleanArray", DataTypeIds.Boolean, ValueRanks.OneDimension, new[] { true, false, true });
        _mgr.CreateVariable<sbyte[]>(folder, $"{p}/SByteArray", "SByteArray", DataTypeIds.SByte, ValueRanks.OneDimension, new sbyte[] { -1, 0, 1 });
        _mgr.CreateVariable<byte[]>(folder, $"{p}/ByteArray", "ByteArray", DataTypeIds.Byte, ValueRanks.OneDimension, new byte[] { 1, 2, 3 });
        _mgr.CreateVariable<short[]>(folder, $"{p}/Int16Array", "Int16Array", DataTypeIds.Int16, ValueRanks.OneDimension, new short[] { -100, 0, 100 });
        _mgr.CreateVariable<ushort[]>(folder, $"{p}/UInt16Array", "UInt16Array", DataTypeIds.UInt16, ValueRanks.OneDimension, new ushort[] { 100, 200, 300 });
        _mgr.CreateVariable<int[]>(folder, $"{p}/Int32Array", "Int32Array", DataTypeIds.Int32, ValueRanks.OneDimension, new[] { -1000, 0, 1000 });
        _mgr.CreateVariable<uint[]>(folder, $"{p}/UInt32Array", "UInt32Array", DataTypeIds.UInt32, ValueRanks.OneDimension, new uint[] { 1000, 2000, 3000 });
        _mgr.CreateVariable<long[]>(folder, $"{p}/Int64Array", "Int64Array", DataTypeIds.Int64, ValueRanks.OneDimension, new long[] { -100000, 0, 100000 });
        _mgr.CreateVariable<ulong[]>(folder, $"{p}/UInt64Array", "UInt64Array", DataTypeIds.UInt64, ValueRanks.OneDimension, new ulong[] { 100000, 200000, 300000 });
        _mgr.CreateVariable<float[]>(folder, $"{p}/FloatArray", "FloatArray", DataTypeIds.Float, ValueRanks.OneDimension, new[] { 1.1f, 2.2f, 3.3f });
        _mgr.CreateVariable<double[]>(folder, $"{p}/DoubleArray", "DoubleArray", DataTypeIds.Double, ValueRanks.OneDimension, new[] { 1.11, 2.22, 3.33 });
        _mgr.CreateVariable<string[]>(folder, $"{p}/StringArray", "StringArray", DataTypeIds.String, ValueRanks.OneDimension, new[] { "one", "two", "three" });
        _mgr.CreateVariable<DateTime[]>(folder, $"{p}/DateTimeArray", "DateTimeArray", DataTypeIds.DateTime, ValueRanks.OneDimension, new[] { DateTime.UtcNow, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2) });
        _mgr.CreateVariable<Guid[]>(folder, $"{p}/GuidArray", "GuidArray", DataTypeIds.Guid, ValueRanks.OneDimension, new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
        _mgr.CreateVariable<byte[][]>(folder, $"{p}/ByteStringArray", "ByteStringArray", DataTypeIds.ByteString, ValueRanks.OneDimension, new[] { new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 } });
        _mgr.CreateVariable<XmlElement[]>(folder, $"{p}/XmlElementArray", "XmlElementArray", DataTypeIds.XmlElement, ValueRanks.OneDimension, Array.Empty<XmlElement>());
        _mgr.CreateVariable<NodeId[]>(folder, $"{p}/NodeIdArray", "NodeIdArray", DataTypeIds.NodeId, ValueRanks.OneDimension, new[] { new NodeId(1, 0), new NodeId(2, 0) });
        _mgr.CreateVariable<StatusCode[]>(folder, $"{p}/StatusCodeArray", "StatusCodeArray", DataTypeIds.StatusCode, ValueRanks.OneDimension, new StatusCode[] { StatusCodes.Good, StatusCodes.Bad });
        _mgr.CreateVariable<QualifiedName[]>(folder, $"{p}/QualifiedNameArray", "QualifiedNameArray", DataTypeIds.QualifiedName, ValueRanks.OneDimension, new[] { new QualifiedName("a", 1), new QualifiedName("b", 1) });
        _mgr.CreateVariable<LocalizedText[]>(folder, $"{p}/LocalizedTextArray", "LocalizedTextArray", DataTypeIds.LocalizedText, ValueRanks.OneDimension, new[] { new LocalizedText("en", "one"), new LocalizedText("en", "two") });

        // Read-only arrays
        var roFolder = _mgr.CreateFolder(folder, $"{p}/ReadOnly", "ReadOnly");
        var rop = $"{p}/ReadOnly";
        var ro = AccessLevels.CurrentRead;

        _mgr.CreateVariable<bool[]>(roFolder, $"{rop}/BooleanArray_RO", "BooleanArray_RO", DataTypeIds.Boolean, ValueRanks.OneDimension, new[] { true, false }, ro);
        _mgr.CreateVariable<int[]>(roFolder, $"{rop}/Int32Array_RO", "Int32Array_RO", DataTypeIds.Int32, ValueRanks.OneDimension, new[] { 10, 20, 30 }, ro);
        _mgr.CreateVariable<double[]>(roFolder, $"{rop}/DoubleArray_RO", "DoubleArray_RO", DataTypeIds.Double, ValueRanks.OneDimension, new[] { 1.0, 2.0, 3.0 }, ro);
        _mgr.CreateVariable<string[]>(roFolder, $"{rop}/StringArray_RO", "StringArray_RO", DataTypeIds.String, ValueRanks.OneDimension, new[] { "read", "only" }, ro);
        _mgr.CreateVariable<byte[]>(roFolder, $"{rop}/ByteArray_RO", "ByteArray_RO", DataTypeIds.Byte, ValueRanks.OneDimension, new byte[] { 0xAA, 0xBB }, ro);
        _mgr.CreateVariable<DateTime[]>(roFolder, $"{rop}/DateTimeArray_RO", "DateTimeArray_RO", DataTypeIds.DateTime, ValueRanks.OneDimension, new[] { DateTime.UtcNow }, ro);

        // Empty arrays
        var emptyFolder = _mgr.CreateFolder(folder, $"{p}/Empty", "Empty");
        var ep = $"{p}/Empty";

        _mgr.CreateVariable<bool[]>(emptyFolder, $"{ep}/EmptyBooleanArray", "EmptyBooleanArray", DataTypeIds.Boolean, ValueRanks.OneDimension, Array.Empty<bool>());
        _mgr.CreateVariable<sbyte[]>(emptyFolder, $"{ep}/EmptySByteArray", "EmptySByteArray", DataTypeIds.SByte, ValueRanks.OneDimension, Array.Empty<sbyte>());
        _mgr.CreateVariable<byte[]>(emptyFolder, $"{ep}/EmptyByteArray", "EmptyByteArray", DataTypeIds.Byte, ValueRanks.OneDimension, Array.Empty<byte>());
        _mgr.CreateVariable<short[]>(emptyFolder, $"{ep}/EmptyInt16Array", "EmptyInt16Array", DataTypeIds.Int16, ValueRanks.OneDimension, Array.Empty<short>());
        _mgr.CreateVariable<ushort[]>(emptyFolder, $"{ep}/EmptyUInt16Array", "EmptyUInt16Array", DataTypeIds.UInt16, ValueRanks.OneDimension, Array.Empty<ushort>());
        _mgr.CreateVariable<int[]>(emptyFolder, $"{ep}/EmptyInt32Array", "EmptyInt32Array", DataTypeIds.Int32, ValueRanks.OneDimension, Array.Empty<int>());
        _mgr.CreateVariable<uint[]>(emptyFolder, $"{ep}/EmptyUInt32Array", "EmptyUInt32Array", DataTypeIds.UInt32, ValueRanks.OneDimension, Array.Empty<uint>());
        _mgr.CreateVariable<long[]>(emptyFolder, $"{ep}/EmptyInt64Array", "EmptyInt64Array", DataTypeIds.Int64, ValueRanks.OneDimension, Array.Empty<long>());
        _mgr.CreateVariable<ulong[]>(emptyFolder, $"{ep}/EmptyUInt64Array", "EmptyUInt64Array", DataTypeIds.UInt64, ValueRanks.OneDimension, Array.Empty<ulong>());
        _mgr.CreateVariable<float[]>(emptyFolder, $"{ep}/EmptyFloatArray", "EmptyFloatArray", DataTypeIds.Float, ValueRanks.OneDimension, Array.Empty<float>());
        _mgr.CreateVariable<double[]>(emptyFolder, $"{ep}/EmptyDoubleArray", "EmptyDoubleArray", DataTypeIds.Double, ValueRanks.OneDimension, Array.Empty<double>());
        _mgr.CreateVariable<string[]>(emptyFolder, $"{ep}/EmptyStringArray", "EmptyStringArray", DataTypeIds.String, ValueRanks.OneDimension, Array.Empty<string>());
        _mgr.CreateVariable<DateTime[]>(emptyFolder, $"{ep}/EmptyDateTimeArray", "EmptyDateTimeArray", DataTypeIds.DateTime, ValueRanks.OneDimension, Array.Empty<DateTime>());
        _mgr.CreateVariable<byte[][]>(emptyFolder, $"{ep}/EmptyByteStringArray", "EmptyByteStringArray", DataTypeIds.ByteString, ValueRanks.OneDimension, Array.Empty<byte[]>());
    }

    private void BuildMultiDimensional(FolderState parent)
    {
        var folder = _mgr.CreateFolder(parent, "TestServer/DataTypes/MultiDimensional", "MultiDimensional");
        var p = "TestServer/DataTypes/MultiDimensional";

        // 2x3 Double matrix
        var matrix2d = _mgr.CreateVariableUntyped(folder, $"{p}/Matrix2D_Double", "Matrix2D_Double",
            DataTypeIds.Double, ValueRanks.TwoDimensions,
            new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } });
        matrix2d.ArrayDimensions = new ReadOnlyList<uint>(new uint[] { 2, 3 });

        // 2x4 Int32 matrix
        var matrix2i = _mgr.CreateVariableUntyped(folder, $"{p}/Matrix2D_Int32", "Matrix2D_Int32",
            DataTypeIds.Int32, ValueRanks.TwoDimensions,
            new int[,] { { 1, 2, 3, 4 }, { 5, 6, 7, 8 } });
        matrix2i.ArrayDimensions = new ReadOnlyList<uint>(new uint[] { 2, 4 });

        // 2x3x4 Byte cube
        var cube = _mgr.CreateVariableUntyped(folder, $"{p}/Cube3D_Byte", "Cube3D_Byte",
            DataTypeIds.Byte, 3,
            new byte[2, 3, 4]);
        cube.ArrayDimensions = new ReadOnlyList<uint>(new uint[] { 2, 3, 4 });
    }

    private void BuildWithRange(FolderState parent)
    {
        var folder = _mgr.CreateFolder(parent, "TestServer/DataTypes/WithRange", "WithRange");
        var p = "TestServer/DataTypes/WithRange";

        // Use Create() to properly initialize AnalogItemState
        var temp = new AnalogItemState<double>(folder);
        temp.Create(_context, new NodeId($"{p}/Temperature", _mgr.NamespaceIndex),
            new QualifiedName("Temperature", _mgr.NamespaceIndex),
            new LocalizedText("en", "Temperature"), true);
        temp.DataType = DataTypeIds.Double;
        temp.ValueRank = ValueRanks.Scalar;
        temp.AccessLevel = AccessLevels.CurrentReadOrWrite;
        temp.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        temp.Value = 25.0;
        temp.StatusCode = StatusCodes.Good;
        temp.Timestamp = DateTime.UtcNow;
        temp.InstrumentRange.Value = new Range(-40.0, 120.0);
        temp.EURange.Value = new Range(0.0, 100.0);
        temp.EngineeringUnits.Value = new EUInformation("°C", "degree Celsius", "http://www.opcfoundation.org/UA/units/un/cefact");
        _mgr.AddNode(_context, temp);

        var press = new AnalogItemState<double>(folder);
        press.Create(_context, new NodeId($"{p}/Pressure", _mgr.NamespaceIndex),
            new QualifiedName("Pressure", _mgr.NamespaceIndex),
            new LocalizedText("en", "Pressure"), true);
        press.DataType = DataTypeIds.Double;
        press.ValueRank = ValueRanks.Scalar;
        press.AccessLevel = AccessLevels.CurrentReadOrWrite;
        press.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        press.Value = 1013.25;
        press.StatusCode = StatusCodes.Good;
        press.Timestamp = DateTime.UtcNow;
        press.InstrumentRange.Value = new Range(0.0, 2000.0);
        press.EURange.Value = new Range(800.0, 1200.0);
        press.EngineeringUnits.Value = new EUInformation("hPa", "hectopascal", "http://www.opcfoundation.org/UA/units/un/cefact");
        _mgr.AddNode(_context, press);

        var roVal = new AnalogItemState<double>(folder);
        roVal.Create(_context, new NodeId($"{p}/ReadOnlyValue", _mgr.NamespaceIndex),
            new QualifiedName("ReadOnlyValue", _mgr.NamespaceIndex),
            new LocalizedText("en", "ReadOnlyValue"), true);
        roVal.DataType = DataTypeIds.Double;
        roVal.ValueRank = ValueRanks.Scalar;
        roVal.AccessLevel = AccessLevels.CurrentRead;
        roVal.UserAccessLevel = AccessLevels.CurrentRead;
        roVal.Value = 42.0;
        roVal.StatusCode = StatusCodes.Good;
        roVal.Timestamp = DateTime.UtcNow;
        roVal.InstrumentRange.Value = new Range(0.0, 100.0);
        roVal.EURange.Value = new Range(0.0, 100.0);
        _mgr.AddNode(_context, roVal);
    }
}

# AI Reference — UA-.NETStandard Test Server Suite

Single-file reference optimized for AI consumption. Contains every fact needed to generate client code, write tests, debug connections, or modify this server.

## Project Identity

- **Name:** `uanetstandard-test-suite`
- **GitHub:** `php-opcua/uanetstandard-test-suite`
- **Runtime:** .NET 8.0 (Alpine)
- **OPC UA Stack:** OPCFoundation.NetStandard.Opc.Ua.Server 1.5.x
- **Language:** C#
- **Deployment:** Docker Compose (8 containers from 1 codebase)
- **Entry point:** `src/TestServer/Program.cs`
- **Docker image:** `ghcr.io/php-opcua/uanetstandard-test-suite`
- **ApplicationUri:** `urn:opcua:testserver:nodes`

## Connection Strings

```
opc.tcp://localhost:4840/UA/TestServer   # no security, anonymous
opc.tcp://localhost:4841/UA/TestServer   # Basic256Sha256+SignAndEncrypt, username/password
opc.tcp://localhost:4842/UA/TestServer   # Basic256Sha256+Aes128+Aes256, certificate auth
opc.tcp://localhost:4843/UA/TestServer   # all policies, all modes, all auth
opc.tcp://localhost:4844                 # discovery server (no /UA/TestServer)
opc.tcp://localhost:4845/UA/TestServer   # Basic256Sha256+SignAndEncrypt, auto-accept certs
opc.tcp://localhost:4846/UA/TestServer   # Basic256Sha256+Sign only, anonymous+username
opc.tcp://localhost:4847/UA/TestServer   # Basic128Rsa15+Basic256, legacy deprecated
```

## Server Matrix

```
PORT  SERVICE               POLICIES                                          MODES                 ANON  USER  CERT  AUTO-ACCEPT  LIMITS
4840  opcua-no-security     None                                              None                  yes   no    no    yes          MaxRead=5,MaxWrite=5
4841  opcua-userpass        Basic256Sha256                                    SignAndEncrypt         no    yes   no    yes
4842  opcua-certificate     Basic256Sha256,Aes128_Sha256_RsaOaep,Aes256_Sha256_RsaPss  Sign,SignAndEncrypt  no    no    yes   no
4843  opcua-all-security    None,Basic128Rsa15,Basic256,Basic256Sha256,Aes128_Sha256_RsaOaep,Aes256_Sha256_RsaPss  None,Sign,SignAndEncrypt  yes  yes  yes  yes
4844  opcua-discovery       None,Basic256Sha256                               None,SignAndEncrypt    yes   no    no    yes
4845  opcua-auto-accept     Basic256Sha256                                    SignAndEncrypt         yes   yes   yes   yes
4846  opcua-sign-only       Basic256Sha256                                    Sign                   yes   yes   no    yes
4847  opcua-legacy          Basic128Rsa15,Basic256                            Sign,SignAndEncrypt    yes   yes   no    yes
```

## Credentials

```
USERNAME    PASSWORD      ROLE      OPC_UA_ROLES
admin       admin123      admin     AuthenticatedUser, ConfigureAdmin, SecurityAdmin, Operator, Engineer
operator    operator123   operator  AuthenticatedUser, Operator
viewer      viewer123     viewer    AuthenticatedUser
test        test          admin     AuthenticatedUser, ConfigureAdmin, SecurityAdmin, Operator, Engineer
```

## Certificate Files

```
certs/ca/ca-cert.pem              CA root certificate (PEM)
certs/ca/ca-key.pem               CA private key
certs/server/cert.pem             Server cert signed by CA (PEM)
certs/server/key.pem              Server private key (PEM)
certs/server/cert.der             Server cert (DER)
certs/server/key.der              Server key (DER)
certs/client/cert.pem             Trusted client cert signed by CA (PEM) — use this for testing
certs/client/key.pem              Client private key (PEM)
certs/client/cert.der             Client cert (DER)
certs/client/key.der              Client key (DER)
certs/self-signed/cert.pem        Untrusted self-signed cert — expect rejection
certs/self-signed/key.pem         Self-signed key
certs/expired/cert.pem            Expired cert — expect BadCertificateTimeInvalid
certs/expired/key.pem             Expired key
certs/trusted/                    Dir of trusted client certs
certs/rejected/                   Dir of rejected certs
```

Server SAN: `localhost`, all container names, `127.0.0.1`, `0.0.0.0`, `urn:opcua:testserver:nodes`

Server auto-generates its own application certificate via `CheckApplicationInstanceCertificates()`.

## Namespaces

```
ns=0  http://opcfoundation.org/UA/                  OPC UA standard namespace
ns=1  urn:opcua:testserver:nodes                     All custom test nodes (NodeIds: ns=1;i=XXXX)
ns=2  http://opcfoundation.org/UA/DI/                Device Integration
ns=3  urn:opcua:test-server:custom-types             Extension object types and encodings
ns=4  http://opcfoundation.org/UA/Diagnostics        Diagnostics
```

Use BrowsePaths for stable references (NodeIds are auto-generated).

## Complete Address Space

Root: `Objects/TestServer` (ns=1)

### DataTypes/Scalar — 21 RW variables

```
PATH                                    TYPE            INITIAL
DataTypes/Scalar/BooleanValue           Boolean         true
DataTypes/Scalar/SByteValue             SByte           -42
DataTypes/Scalar/ByteValue              Byte            42
DataTypes/Scalar/Int16Value             Int16           -1000
DataTypes/Scalar/UInt16Value            UInt16          1000
DataTypes/Scalar/Int32Value             Int32           -100000
DataTypes/Scalar/UInt32Value            UInt32          100000
DataTypes/Scalar/Int64Value             Int64           -1000000
DataTypes/Scalar/UInt64Value            UInt64          1000000
DataTypes/Scalar/FloatValue             Float           3.14
DataTypes/Scalar/DoubleValue            Double          3.141592653589793
DataTypes/Scalar/StringValue            String          "Hello OPC UA"
DataTypes/Scalar/DateTimeValue          DateTime        (startup time)
DataTypes/Scalar/GuidValue              Guid            72962B91-FA75-4AE6-8D28-B404DC7DAF63
DataTypes/Scalar/ByteStringValue        ByteString      "OPC UA Test Data"
DataTypes/Scalar/XmlElementValue        XmlElement      <test><value>42</value></test>
DataTypes/Scalar/NodeIdValue            NodeId          ns=1;s=test-nodeid
DataTypes/Scalar/ExpandedNodeIdValue    ExpandedNodeId  ns=1;i=1234
DataTypes/Scalar/StatusCodeValue        StatusCode      Good
DataTypes/Scalar/QualifiedNameValue     QualifiedName   1:TestQualifiedName
DataTypes/Scalar/LocalizedTextValue     LocalizedText   en:"Test Localized Text"
```

### DataTypes/ReadOnly — 21 R variables

```
PATH                                    TYPE            VALUE
DataTypes/ReadOnly/Boolean_RO           Boolean         true
DataTypes/ReadOnly/SByte_RO             SByte           -42
DataTypes/ReadOnly/Byte_RO              Byte            42
DataTypes/ReadOnly/Int16_RO             Int16           -1000
DataTypes/ReadOnly/UInt16_RO            UInt16          1000
DataTypes/ReadOnly/Int32_RO             Int32           -100000
DataTypes/ReadOnly/UInt32_RO            UInt32          100000
DataTypes/ReadOnly/Int64_RO             Int64           -1000000
DataTypes/ReadOnly/UInt64_RO            UInt64          1000000
DataTypes/ReadOnly/Float_RO             Float           3.14
DataTypes/ReadOnly/Double_RO            Double          3.141592653589793
DataTypes/ReadOnly/String_RO            String          "Read Only String"
DataTypes/ReadOnly/DateTime_RO          DateTime        2024-01-01T00:00:00Z
DataTypes/ReadOnly/Guid_RO              Guid            72962B91-FA75-4AE6-8D28-B404DC7DAF63
DataTypes/ReadOnly/ByteString_RO        ByteString      "ReadOnly"
DataTypes/ReadOnly/XmlElement_RO        XmlElement      <readonly/>
DataTypes/ReadOnly/NodeId_RO            NodeId          ns=1;s=readonly-nodeid
DataTypes/ReadOnly/ExpandedNodeId_RO    ExpandedNodeId  ns=1;i=9999
DataTypes/ReadOnly/StatusCode_RO        StatusCode      Good
DataTypes/ReadOnly/QualifiedName_RO     QualifiedName   1:ReadOnly
DataTypes/ReadOnly/LocalizedText_RO     LocalizedText   en:"Read Only"
```

Write to any `_RO` -> `BadNotWritable`

### DataTypes/Array — 20 RW arrays (valueRank=1)

```
PATH                                    ELEMENT_TYPE    INITIAL
DataTypes/Array/BooleanArray            Boolean[]       [true,false,true,false]
DataTypes/Array/SByteArray              SByte[]         [-128,-1,0,1,127]
DataTypes/Array/ByteArray               Byte[]          [0,42,128,255]
DataTypes/Array/Int16Array              Int16[]         [-32768,-1,0,1,32767]
DataTypes/Array/UInt16Array             UInt16[]        [0,1000,50000,65535]
DataTypes/Array/Int32Array              Int32[]         [-100000,-1,0,1,100000]
DataTypes/Array/UInt32Array             UInt32[]        [0,100000,1000000,4294967295]
DataTypes/Array/Int64Array              Int64[]         [0,-1000000,1000000]
DataTypes/Array/UInt64Array             UInt64[]        [0,1000000,999999999]
DataTypes/Array/FloatArray              Float[]         [1.1,2.2,3.3,4.4]
DataTypes/Array/DoubleArray             Double[]        [1.11,2.22,3.33,4.44,5.55]
DataTypes/Array/StringArray             String[]        ["Hello","OPC","UA","World"]
DataTypes/Array/DateTimeArray           DateTime[]      [2024-01-01,2024-06-15,now]
DataTypes/Array/GuidArray               Guid[]          [72962B91-...,A1B2C3D4-...]
DataTypes/Array/ByteStringArray         ByteString[]    ["First","Second","Third"]
DataTypes/Array/XmlElementArray         XmlElement[]    ["<a/>","<b/>","<c/>"]
DataTypes/Array/NodeIdArray             NodeId[]        [ns=1;s=arr-0,ns=1;s=arr-1]
DataTypes/Array/StatusCodeArray         StatusCode[]    [Good,BadInternalError,UncertainLastUsableValue]
DataTypes/Array/QualifiedNameArray      QualifiedName[] [1:QN_A,1:QN_B]
DataTypes/Array/LocalizedTextArray      LocalizedText[] [en:Hello,it:Ciao,de:Hallo]
```

### DataTypes/Array/ReadOnly — 6 R arrays

```
DataTypes/Array/ReadOnly/BooleanArray_RO    Boolean[]   [true,false]
DataTypes/Array/ReadOnly/Int32Array_RO      Int32[]     [1,2,3]
DataTypes/Array/ReadOnly/DoubleArray_RO     Double[]    [1.1,2.2,3.3]
DataTypes/Array/ReadOnly/StringArray_RO     String[]    ["A","B","C"]
DataTypes/Array/ReadOnly/ByteArray_RO       Byte[]      [0,127,255]
DataTypes/Array/ReadOnly/DateTimeArray_RO   DateTime[]  [2024-01-01,2024-06-15]
```

### DataTypes/Array/Empty — 14 RW arrays (initial=[])

```
DataTypes/Array/Empty/EmptyBooleanArray      Boolean[]
DataTypes/Array/Empty/EmptySByteArray        SByte[]
DataTypes/Array/Empty/EmptyByteArray         Byte[]
DataTypes/Array/Empty/EmptyInt16Array        Int16[]
DataTypes/Array/Empty/EmptyUInt16Array       UInt16[]
DataTypes/Array/Empty/EmptyInt32Array        Int32[]
DataTypes/Array/Empty/EmptyUInt32Array       UInt32[]
DataTypes/Array/Empty/EmptyInt64Array        Int64[]
DataTypes/Array/Empty/EmptyUInt64Array       UInt64[]
DataTypes/Array/Empty/EmptyFloatArray        Float[]
DataTypes/Array/Empty/EmptyDoubleArray       Double[]
DataTypes/Array/Empty/EmptyStringArray       String[]
DataTypes/Array/Empty/EmptyDateTimeArray     DateTime[]
DataTypes/Array/Empty/EmptyByteStringArray   ByteString[]
```

### DataTypes/MultiDimensional — 3 RW matrices

```
PATH                                         TYPE    DIMENSIONS  INITIAL
DataTypes/MultiDimensional/Matrix2D_Double   Double  [3,3]       [1,2,3,4,5,6,7,8,9]
DataTypes/MultiDimensional/Matrix2D_Int32    Int32   [2,4]       [1,2,3,4,5,6,7,8]
DataTypes/MultiDimensional/Cube3D_Byte       Byte    [2,3,4]     [0..23]
```

valueRank=2 for 2D, valueRank=3 for 3D. Data is flat row-major.

### DataTypes/WithRange — 3 variables (2 AnalogDataItem + 1 plain)

```
PATH                                TYPE    ACCESS  VALUE    EU_RANGE      INSTRUMENT_RANGE
DataTypes/WithRange/Temperature     Double  RW      ~22.5    [-40,120]     [-50,150]
DataTypes/WithRange/Pressure        Double  RW      ~101.325 [0,500]       [0,600]
DataTypes/WithRange/ReadOnlyValue   Double  R       42.0     -             -
```

Temperature and Pressure are `AnalogDataItem` nodes with `EURange` and `InstrumentRange` properties as children.

### Methods — 12 methods

```
PATH                    INPUT                                           OUTPUT                                          BEHAVIOR
Methods/Add             a:Double, b:Double                              result:Double                                   a+b
Methods/Multiply        a:Double, b:Double                              result:Double                                   a*b
Methods/Concatenate     a:String, b:String                              result:String                                   a+b
Methods/Reverse         input:String                                    result:String                                   reverse(input)
Methods/GetServerTime   (none)                                          time:DateTime                                   DateTime.UtcNow
Methods/Echo            input:Variant                                   output:Variant                                  identity
Methods/GenerateEvent   message:String, severity:UInt16                 (none)                                          raises BaseEventType on server
Methods/LongRunning     durationMs:UInt32                               completed:Boolean                               Task.Delay(min(dur,30000)), returns true
Methods/Failing         (none)                                          (none)                                          always BadInternalError
Methods/ArraySum        values:Double[] (valueRank=1)                   sum:Double                                      Sum()
Methods/MatrixTranspose matrix:Double[],rows:UInt32,cols:UInt32         result:Double[]                                 transpose; BadInvalidArgument if len!=rows*cols
Methods/MultiOutput     (none)                                          intValue:Int32,stringValue:String,boolValue:Boolean   42,"hello",true
```

### Dynamic — 13 R variables

```
PATH                     TYPE        UPDATE       FORMULA/BEHAVIOR
Dynamic/Counter          UInt32      1s timer     ++counter (starts 0)
Dynamic/FastCounter      UInt32      100ms timer  ++counter (starts 0)
Dynamic/SlowCounter      UInt32      10s timer    ++counter (starts 0)
Dynamic/Random           Double      500ms timer  Random.Shared.NextDouble() -> [0,1)
Dynamic/RandomInt        Int32       1s timer     Random.Shared.Next(-1000,1001) -> [-1000,1000]
Dynamic/SineWave         Double      on-read      sin(2*PI*elapsed/10), period=10s
Dynamic/SawTooth         Double      on-read      (elapsed%5)/5 -> [0,1), period=5s
Dynamic/Square           Boolean     on-read      (elapsed%2)<1, period=2s
Dynamic/TriangleWave     Double      on-read      triangle [-1,1], period=8s
Dynamic/Timestamp        DateTime    on-read      DateTime.UtcNow
Dynamic/RandomString     String      2s timer     random alphanumeric 8-24 chars
Dynamic/StatusVariable   StatusCode  3s timer     cycles: Good -> BadCommunicationError -> UncertainLastUsableValue
Dynamic/NullableDouble   Double      4s timer     alternates Good(random 0-100) / BadNoData
```

### Events — 3 custom types + 1 emitter

```
EVENT TYPE               PARENT          CUSTOM PROPERTIES
SimpleEventType          BaseEventType   EventPayload:String
ComplexEventType         BaseEventType   Source:String, Category:String, NumericValue:Double
SystemStatusEventType    BaseEventType   SystemState:String, CpuUsage:Double, MemoryUsage:Double
```

```
EMITTER: Events/EventEmitter (Object, EventNotifier=1, eventSourceOf Server)
```

```
PERIODIC EVENTS:
SimpleEventType          every 2s    severity=200    message="Periodic event #N"    payload="payload-N"
ComplexEventType         every 5s    severity=300    message="Complex event #N"
SystemStatusEventType    every 10s   severity=100/600  states=[Running,Idle,Busy,Maintenance]  cpu=random  mem=40+random*50
```

### Alarms — 2 sources + 3 alarms

```
SOURCE VARIABLES:
Alarms/AlarmSourceValue    Double   RW    oscillates 50+60*sin(t) every 1s (range ~-10 to ~110)
Alarms/OffNormalSource     Boolean  RW    toggles every 20s

ALARMS:
Alarms/HighTemperatureAlarm   ExclusiveLimitAlarmType    source=AlarmSourceValue   LoLo=5 Lo=20 Hi=70 HiHi=90
Alarms/LevelAlarm             NonExclusiveLimitAlarmType source=AlarmSourceValue   LoLo=0 Lo=15 Hi=75 HiHi=95
Alarms/OffNormalAlarm         OffNormalAlarmType         source=OffNormalSource

All support: Acknowledge, Confirm, AddComment
Exclusive: only one limit state active at a time
NonExclusive: multiple limit states can be active simultaneously
```

### Historical — 4 R+HR variables

```
PATH                                TYPE     RATE    MAX_SAMPLES  FORMULA
Historical/HistoricalTemperature    Double   1000ms  10000        22+8*sin(t/60)+(random-0.5)*2
Historical/HistoricalPressure       Double   1000ms  10000        101.325+5*cos(t/120)+(random-0.5)
Historical/HistoricalCounter        UInt32   1000ms  10000        ++counter
Historical/HistoricalBoolean        Boolean  1000ms  10000        random()>0.5
```

Recording interval: 1000ms (1 second). accessLevel = CurrentRead | HistoryRead. In-memory storage, not persisted across restarts.
Supports: ReadRawModifiedDetails, ReadProcessedDetails, ReadAtTimeDetails.

### Structures — objects with child variables

```
OBJECT                                    CHILDREN (all RW)
Structures/TestPoint                      X:Double=1.0, Y:Double=2.0, Z:Double=3.0
Structures/TestRange                      Min:Double=0.0, Max:Double=100.0, Value:Double=50.0
Structures/TestPerson                     Name:String="John Doe", Age:UInt32=30, Active:Boolean=true
Structures/TestNested                     Label:String="origin", Timestamp:DateTime=(startup)
Structures/TestNested/Point               X:Double=0.0, Y:Double=0.0, Z:Double=0.0
Structures/PointCollection/Point_{0..4}   X:Double=i*10, Y:Double=i*20, Z:Double=i*30
Structures/DeepNesting/Level_1/../Level_N Depth:UInt32=N, Name:String="Level N"  (N=1..10, each nested inside previous)
```

### ExtensionObjects — 2 variables with binary-encoded custom structured types

```
CUSTOM TYPES (namespace ns=3, URI: urn:opcua:test-server:custom-types):
TestPointXYZ       fields: X:Double, Y:Double, Z:Double     Encoding TypeId: ns=3;i=3010
TestRangeStruct    fields: Min:Double, Max:Double, Value:Double   Encoding TypeId: ns=3;i=3011

VARIABLES:
ExtensionObjects/PointValue    TestPointXYZ     RW    {X:1.5, Y:2.5, Z:3.5}    binary body: 3 doubles (24 bytes)
ExtensionObjects/RangeValue    TestRangeStruct  R     {Min:0.0, Max:100.0, Value:42.5}   binary body: 3 doubles (24 bytes)
```

PointValue supports read/write of the full binary-encoded ExtensionObject. RangeValue is read-only.

### AccessControl — 50 variables

```
ACCESS LEVELS (5 vars):
AccessControl/AccessLevels/CurrentRead_Only   Int32  accessLevel=R        userAccessLevel=R       value=100
AccessControl/AccessLevels/CurrentWrite_Only  Int32  accessLevel=R|W      userAccessLevel=W       value=0
AccessControl/AccessLevels/ReadWrite          Int32  accessLevel=R|W      userAccessLevel=R|W     value=42
AccessControl/AccessLevels/HistoryRead_Only   Int32  accessLevel=R|HR     userAccessLevel=R|HR    value=55
AccessControl/AccessLevels/FullAccess         Int32  accessLevel=R|W|HR   userAccessLevel=R|W|HR  value=77

ADMIN ONLY (4 vars, all RW):
AccessControl/AdminOnly/SecretConfig          String   "admin-secret-value"
AccessControl/AdminOnly/SystemParameter       Int32    999
AccessControl/AdminOnly/CalibrationFactor     Double   99.99
AccessControl/AdminOnly/MaintenanceMode       Boolean  false

OPERATOR LEVEL (4 vars, all RW, role-based write protection: admin/operator can write, viewer cannot):
AccessControl/OperatorLevel/Setpoint          Double   50.0
AccessControl/OperatorLevel/MotorSpeed        Int32    1500
AccessControl/OperatorLevel/ProcessEnabled    Boolean  true
AccessControl/OperatorLevel/RecipeName        String   "default"

VIEWER LEVEL (5 vars, all R):
AccessControl/ViewerLevel/ProductionCount     UInt32   12345
AccessControl/ViewerLevel/MachineName         String   "Machine-001"
AccessControl/ViewerLevel/IsRunning           Boolean  true
AccessControl/ViewerLevel/CurrentTemperature  Double   ~22.5+/-0.5 (random noise)
AccessControl/ViewerLevel/UptimeSeconds       UInt32   process uptime

ALL COMBINATIONS (32 vars = 8 types x 4 access modes):
Types: Boolean, Int32, UInt32, Double, String, DateTime, Byte, Float
Suffixes: _RO (R), _RW (R|W), _WO (userAccess=W only), _HR (R|HR)
Pattern: AccessControl/AllCombinations/{Type}_{Suffix}
RO values: true, -42, 42, 3.14, "immutable", 2024-01-01, 128, 2.71
RW/WO values: false, 0, 0, 0.0, "", now, 0, 0.0
```

### Views — 4 views under standard Views folder (ns=0;i=87)

```
VIEW                REFERENCES
OperatorView        Dynamic, Methods, Alarms
EngineeringView     TestServer (entire tree)
HistoricalView      Historical
DataView            DataTypes, Structures
```

## Source Code Architecture

```
src/TestServer/
├── Program.cs                        main(): creates server, handles CLI args (--health), graceful shutdown
├── TestServer.csproj                 NuGet dependencies (OPCFoundation.NetStandard.Opc.Ua.Server 1.5.x)
├── Server/
│   ├── TestServerApp.cs              Server application setup, certificate management
│   └── TestNodeManager.cs            Node manager: creates TestServer folder, calls builders based on config
├── Configuration/
│   └── ServerConfig.cs               Environment variable parsing (GetStringEnv, GetBoolEnv, GetIntEnv)
├── UserManagement/
│   └── UserManager.cs                User validation, role mapping from config/users.json
└── AddressSpace/
    ├── DataTypesBuilder.cs           Scalars, arrays, matrices, analog items
    ├── MethodsBuilder.cs             12 method definitions with callbacks
    ├── DynamicBuilder.cs             Timer-based and on-read variables
    ├── EventsAlarmsBuilder.cs        Event types + emitter + alarm instances
    ├── HistoricalBuilder.cs          4 vars + history recording (1000ms interval)
    ├── StructuresBuilder.cs          Objects with child variables, nesting
    ├── ExtensionObjectsBuilder.cs    Binary-encoded custom types (ns=3)
    ├── AccessControlBuilder.cs       Access levels + role folders + AllCombinations
    └── ViewsBuilder.cs               4 views referencing existing folders
```

## Code Patterns

### Adding a new variable

```csharp
var variable = new BaseDataVariableState<double>(parentFolder)
{
    NodeId = new NodeId("Name", namespaceIndex),
    BrowseName = new QualifiedName("Name", namespaceIndex),
    DisplayName = "Name",
    DataType = DataTypeIds.Double,
    ValueRank = ValueRanks.Scalar,
    AccessLevel = AccessLevels.CurrentReadOrWrite,
    UserAccessLevel = AccessLevels.CurrentReadOrWrite,
    Value = 42.0
};
parentFolder.AddChild(variable);
AddPredefinedNode(SystemContext, variable);
```

### Adding a new method

```csharp
var method = new MethodState(parentFolder)
{
    NodeId = new NodeId("Name", namespaceIndex),
    BrowseName = new QualifiedName("Name", namespaceIndex),
    Executable = true, UserExecutable = true
};
method.InputArguments = CreateArguments(...);
method.OutputArguments = CreateArguments(...);
method.OnCallMethod = (context, objectId, inputArgs, outputArgs) =>
{
    outputArgs[0] = new Variant(result);
    return ServiceResult.Good;
};
```

### Timer pattern (dynamic/events)

```csharp
var timer = new Timer(_ =>
{
    variable.Value = newValue;
    variable.Timestamp = DateTime.UtcNow;
    variable.ClearChangeMasks(SystemContext, false);
}, null, intervalMs, intervalMs);
_timers.Add(timer);

// Cleanup in Stop():
foreach (var t in _timers) t.Dispose();
```

### Builder pattern

Each address-space builder is a class with a `Build()` method that receives the parent folder and system context. The `TestNodeManager.CreateAddressSpace()` method calls builders conditionally based on `ServerConfig` flags.

## Environment Variables (Complete)

```
OPCUA_PORT                    int     4840        Server port
OPCUA_SERVER_NAME             string  OPCUATestServer  Display name
OPCUA_HOSTNAME                string  0.0.0.0     Bind address
OPCUA_RESOURCE_PATH           string  /UA/TestServer   Endpoint path
OPCUA_SECURITY_POLICIES       csv     None        Comma-separated policy names
OPCUA_SECURITY_MODES          csv     None        Comma-separated mode names
OPCUA_ALLOW_ANONYMOUS         bool    true        Allow anonymous auth
OPCUA_AUTH_USERS              bool    false       Enable username/password auth
OPCUA_AUTH_CERTIFICATE        bool    false       Enable X.509 cert auth
OPCUA_AUTO_ACCEPT_CERTS       bool    false       Auto-accept unknown client certs
OPCUA_IS_DISCOVERY            bool    false       Run as discovery server
OPCUA_MAX_SESSIONS            int     100
OPCUA_ENABLE_HISTORICAL       bool    true
OPCUA_ENABLE_EVENTS           bool    true
OPCUA_ENABLE_METHODS          bool    true
OPCUA_ENABLE_DYNAMIC          bool    true
OPCUA_ENABLE_STRUCTURES       bool    true
OPCUA_ENABLE_VIEWS            bool    true
OPCUA_MAX_NODES_PER_READ      int     0           Max nodes per Read request (0=unlimited)
OPCUA_MAX_NODES_PER_WRITE     int     0           Max nodes per Write request (0=unlimited)
OPCUA_MAX_NODES_PER_BROWSE    int     0           Max nodes per Browse request (0=unlimited)
```

Bool parsing: `"true"` or `"1"` -> true, anything else -> false.

## Node Count Summary

```
Scalars RW:          21
Scalars RO:          21
Arrays RW:           20
Arrays RO:            6
Arrays Empty:        14
Matrices:             3
Analog (WithRange):   3
Methods:             12
Dynamic:             13
Event Types:          3
Alarms:               3 (+2 sources)
Historical:           4
Structures:          46 vars in 19 objects
Extension Objects:    2
Access Control:      50
Views:                4
Folders:             22
─────────────────────────
TOTAL:              ~300 nodes
```

## Expected Error Codes

```
SCENARIO                                    EXPECTED STATUS CODE
Write to R variable                         BadNotWritable
Read WO variable (user level)               BadNotReadable
Wrong username/password                     BadIdentityTokenRejected
Anonymous on auth-required server           BadIdentityTokenRejected
Self-signed cert on validated server        BadCertificateUntrusted
Expired cert                                BadCertificateTimeInvalid
Methods/Failing call                        BadInternalError
MatrixTranspose with wrong dimensions       BadInvalidArgument
HistoryRead on non-historical variable      BadHistoryOperationUnsupported
Write as viewer role to OperatorLevel       BadUserAccessDenied
NullableDouble during BadNoData phase       BadNoData (in DataValue.statusCode)
Read >5 nodes on no-security server         BadTooManyOperations (or similar)
```

## Docker Commands

```bash
docker compose up -d          # start all 8 servers
docker compose down           # stop all
docker compose build          # rebuild after code changes
docker compose logs -f <svc>  # tail logs for one service
docker compose ps             # check status
```

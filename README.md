# UA-.NETStandard Test Suite

OPC UA test server infrastructure based on the [OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard) library. Provides 8 pre-configured server instances via Docker Compose for comprehensive integration testing of OPC UA client libraries.

## Quick Start

```bash
docker compose up -d
```

This starts 8 OPC UA servers covering all security configurations:

| Port | Server | Security Policy | Auth | Use Case |
|------|--------|----------------|------|----------|
| 4840 | No Security | None | Anonymous | Basic connectivity |
| 4841 | Username/Password | Basic256Sha256 + SignAndEncrypt | User/Pass | Credential auth |
| 4842 | Certificate | Basic256Sha256, Aes128, Aes256 + Sign/SignAndEncrypt | X.509 | Cert auth |
| 4843 | All Security | All 6 policies + All 3 modes | All | Full security matrix |
| 4844 | Discovery | None, Basic256Sha256 | Anonymous | FindServers |
| 4845 | Auto-Accept | Basic256Sha256 + SignAndEncrypt | User + Cert (auto-trust) | Auto cert acceptance |
| 4846 | Sign Only | Basic256Sha256 + Sign | Anonymous + User | Signing without encryption |
| 4847 | Legacy | Basic128Rsa15, Basic256 + Sign/SignAndEncrypt | Anonymous + User | Deprecated policies |

## Endpoints

```
opc.tcp://localhost:4840/UA/TestServer   # No Security
opc.tcp://localhost:4841/UA/TestServer   # Username/Password
opc.tcp://localhost:4842/UA/TestServer   # Certificate
opc.tcp://localhost:4843/UA/TestServer   # All Security
opc.tcp://localhost:4844                 # Discovery
opc.tcp://localhost:4845/UA/TestServer   # Auto-Accept
opc.tcp://localhost:4846/UA/TestServer   # Sign Only
opc.tcp://localhost:4847/UA/TestServer   # Legacy
```

## User Accounts

| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | admin |
| operator | operator123 | operator |
| viewer | viewer123 | viewer |
| test | test | admin |

## Certificates

Certificates are auto-generated on first startup in `certs/`:

- `certs/ca/` - Certificate Authority
- `certs/server/` - Server certificate (PEM, DER, PFX)
- `certs/client/` - Client certificate (PEM, DER, PFX)
- `certs/self-signed/` - Untrusted self-signed (for rejection testing)
- `certs/expired/` - Expired certificate (for expiration testing)

## Address Space (~300 nodes)

All servers share the same rich address space under `TestServer/`:

### DataTypes (`TestServer/DataTypes/`)
- **Scalar/** - 21 read/write scalar types (Boolean, SByte, Byte, Int16..., LocalizedText)
- **ReadOnly/** - 21 read-only scalar types
- **Array/** - 20 read/write arrays, 6 read-only arrays, 14 empty arrays
- **MultiDimensional/** - 2D matrices (Double, Int32) and 3D cube (Byte)
- **WithRange/** - 3 analog items with EURange/InstrumentRange metadata

### Methods (`TestServer/Methods/`)
- **Add**, **Multiply** - Arithmetic
- **Concatenate**, **Reverse** - String operations
- **GetServerTime** - No-input method
- **Echo** - Variant echo
- **GenerateEvent** - Trigger server events
- **LongRunning** - Async/timeout testing
- **Failing** - Always fails (error handling)
- **ArraySum** - Array input
- **MatrixTranspose** - Matrix operations
- **MultiOutput** - Multiple return values

### Dynamic (`TestServer/Dynamic/`)
13 time-varying read-only variables: Counter (1s), FastCounter (100ms), SlowCounter (10s), Random, SineWave, SawTooth, TriangleWave, Square, Timestamp, RandomString, StatusVariable, NullableDouble, RandomInt

### Events & Alarms (`TestServer/Events/`, `TestServer/Alarms/`)
- **EventEmitter** - emits SimpleEvent (2s), ComplexEvent (5s), SystemStatusEvent (10s)
- **HighTemperatureAlarm** - ExclusiveLimit (High >80, HighHigh >95, Low <20, LowLow <5)
- **LevelAlarm** - NonExclusiveLimit
- **OffNormalAlarm** - Boolean off-normal condition
- **AlarmSourceValue** / **OffNormalSource** - writable alarm trigger variables

### Historical (`TestServer/Historical/`)
4 variables with HistoryRead support: HistoricalTemperature, HistoricalPressure, HistoricalCounter, HistoricalBoolean (up to 10,000 values, 100ms recording)

### Structures (`TestServer/Structures/`)
- **TestPoint** (X, Y, Z), **TestRange** (Min, Max, Value), **TestPerson** (Name, Age, Active)
- **TestNested** - nested object with Point sub-object
- **PointCollection** - 5 point objects
- **DeepNesting** - 10 levels deep (Level_1 through Level_10)

### Extension Objects (`TestServer/ExtensionObjects/`)
Custom DataTypes: TestPointXYZ, TestRangeStruct with structured variable instances

### Access Control (`TestServer/AccessControl/`)
- **AccessLevels/** - CurrentRead, CurrentWrite, ReadWrite, HistoryRead, FullAccess
- **AdminOnly/** - 4 admin-restricted variables
- **OperatorLevel/** - 4 operator-level variables
- **ViewerLevel/** - 5 read-only viewer variables
- **AllCombinations/** - 32 variables (8 types x 4 access patterns: RO, RW, WO, HR)

### Views
4 OPC UA views: OperatorView, EngineeringView, HistoricalView, DataView

## CI Integration

```yaml
steps:
  - uses: actions/checkout@v4

  - name: Start OPC UA Test Servers
    run: docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d --wait

  - name: Run client tests
    run: your-test-command
    env:
      OPCUA_CERTS_DIR: ./certs

  - name: Stop servers
    if: always()
    run: docker compose down
```

## Technology

- **Runtime**: .NET 8.0 (Alpine)
- **OPC UA Stack**: [OPCFoundation.NetStandard.Opc.Ua.Server](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua.Server/) 1.5.x
- **Configuration**: Environment variables
- **Certificates**: OpenSSL auto-generation

# Access Control

Path: `Objects > TestServer > AccessControl`

50 variables organized to test different access levels, role-based permissions, and all combinations of data type and access mode.

## 1. Access Levels

Path: `AccessControl > AccessLevels`

5 variables demonstrating different OPC UA access level flags:

| BrowseName | DataType | accessLevel | userAccessLevel | Initial Value | Description |
|---|---|---|---|---|---|
| `CurrentRead_Only` | Int32 | CurrentRead | CurrentRead | `100` | Read succeeds, write returns `BadNotWritable` |
| `CurrentWrite_Only` | Int32 | CurrentRead + CurrentWrite | CurrentWrite | `0` | Write succeeds, read returns `BadNotReadable` (at user level) |
| `ReadWrite` | Int32 | CurrentRead + CurrentWrite | CurrentRead + CurrentWrite | `42` | Both read and write succeed |
| `HistoryRead_Only` | Int32 | CurrentRead + HistoryRead | CurrentRead + HistoryRead | `55` | Read and history read succeed, write fails |
| `FullAccess` | Int32 | CurrentRead + CurrentWrite + HistoryRead | CurrentRead + CurrentWrite + HistoryRead | `77` | All operations succeed |

### accessLevel vs userAccessLevel

OPC UA defines two separate access level attributes:

- **accessLevel**: The access capabilities of the variable itself (what the server supports)
- **userAccessLevel**: The access granted to the current user/session (may be more restrictive)

For `CurrentWrite_Only`:
- `accessLevel` includes `CurrentRead` (server can read internally)
- `userAccessLevel` is only `CurrentWrite` (user cannot read, only write)

**Testing notes:**
- Read `CurrentWrite_Only` -- should return `BadNotReadable` (user has no read permission)
- Write to `CurrentRead_Only` -- should return `BadNotWritable`
- Check `accessLevel` attribute (AttributeId=17) and `userAccessLevel` attribute (AttributeId=18) separately

## 2. AdminOnly

Path: `AccessControl > AdminOnly`

Variables intended for admin-level access only. All are read/write at the OPC UA level (role enforcement depends on the client's user identity).

| BrowseName | DataType | Access | Initial Value | Purpose |
|---|---|---|---|---|
| `SecretConfig` | String | RW | `"admin-secret-value"` | Sensitive configuration string |
| `SystemParameter` | Int32 | RW | `999` | System-level parameter |
| `CalibrationFactor` | Double | RW | `99.99` | Calibration coefficient |
| `MaintenanceMode` | Boolean | RW | `false` | Maintenance flag |

**Testing notes:**
- Connect as `admin` (admin123) -> read and write should succeed
- Connect as `viewer` (viewer123) -> read succeeds, write may be rejected depending on role enforcement

## 3. OperatorLevel

Path: `AccessControl > OperatorLevel`

Variables for operator-level access. Operators and admins can read/write; viewers can only read. These variables have **role-based write protection** via `rolePermissions` -- admin and operator roles can write, but the viewer role cannot.

| BrowseName | DataType | Access | Initial Value | Purpose |
|---|---|---|---|---|
| `Setpoint` | Double | RW | `50.0` | Process setpoint |
| `MotorSpeed` | Int32 | RW | `1500` | Motor speed (RPM) |
| `ProcessEnabled` | Boolean | RW | `true` | Process enable flag |
| `RecipeName` | String | RW | `"default"` | Active recipe name |

**Testing notes:**
- Connect as `admin` or `operator` -> write should succeed
- Connect as `viewer` -> write should return `BadUserAccessDenied`

## 4. ViewerLevel

Path: `AccessControl > ViewerLevel`

Read-only variables accessible to all roles including viewers.

| BrowseName | DataType | Access | Value | Purpose |
|---|---|---|---|---|
| `ProductionCount` | UInt32 | R | `12345` | Static production counter |
| `MachineName` | String | R | `"Machine-001"` | Machine identifier |
| `IsRunning` | Boolean | R | `true` | Machine running status |
| `CurrentTemperature` | Double | R | `~22.5 +/- 0.5` | Temperature with slight random noise |
| `UptimeSeconds` | UInt32 | R | *(process uptime)* | Seconds since server started |

**Testing notes:**
- `CurrentTemperature` has a small random variation on each read
- `UptimeSeconds` increases in real-time

## 5. AllCombinations

Path: `AccessControl > AllCombinations`

Every combination of 8 data types and 4 access modes, producing 32 variables. This is useful for systematic testing of type handling across access levels.

### Access Modes

| Suffix | Access Level | Description |
|---|---|---|
| `_RO` | CurrentRead | Read-only |
| `_RW` | CurrentRead + CurrentWrite | Read and write |
| `_WO` | userAccessLevel = CurrentWrite only | Write-only at user level |
| `_HR` | CurrentRead + HistoryRead | Read + history |

### Data Types

| Type | `_RO` initial | `_RW` initial | `_WO` initial | `_HR` initial |
|---|---|---|---|---|
| `Boolean` | `true` | `false` | `false` | `true` |
| `Int32` | `-42` | `0` | `0` | `-42` |
| `UInt32` | `42` | `0` | `0` | `42` |
| `Double` | `3.14` | `0.0` | `0.0` | `3.14` |
| `String` | `"immutable"` | `""` | `""` | `"immutable"` |
| `DateTime` | `2024-01-01` | current time | current time | `2024-01-01` |
| `Byte` | `128` | `0` | `0` | `128` |
| `Float` | `2.71` | `0.0` | `0.0` | `2.71` |

### Complete Variable List

```
AllCombinations/Boolean_RO     Boolean_RW     Boolean_WO     Boolean_HR
AllCombinations/Int32_RO       Int32_RW       Int32_WO       Int32_HR
AllCombinations/UInt32_RO      UInt32_RW      UInt32_WO      UInt32_HR
AllCombinations/Double_RO      Double_RW      Double_WO      Double_HR
AllCombinations/String_RO      String_RW      String_WO      String_HR
AllCombinations/DateTime_RO    DateTime_RW    DateTime_WO    DateTime_HR
AllCombinations/Byte_RO        Byte_RW        Byte_WO        Byte_HR
AllCombinations/Float_RO       Float_RW       Float_WO       Float_HR
```

**Testing notes:**
- Use these for systematic "for each type, test each access mode" loops
- Write to all `_RO` variables -> expect `BadNotWritable` for each
- Write to all `_RW` variables -> expect `Good` for each
- Read all `_WO` variables -> expect `BadNotReadable` for each
- Write to all `_WO` variables -> expect `Good` for each
- Read all `_HR` variables -> expect `Good` for each, and `HistoryRead` flag should be set in the accessLevel attribute

---
eyebrow: 'Docs · Data features'
lede:    'Fifty variables organised to exercise every combination of access-level flag, user role, and data type. The systematic test surface for access-attribute logic.'

see_also:
  - { href: '../address-space/browse-paths-and-access-levels.md', meta: '4 min' }
  - { href: '../authentication/user-accounts-and-roles.md', meta: '3 min' }

prev: { label: 'Structures and extension objects',  href: './structures-and-extension-objects.md' }
next: { label: 'Methods',                           href: '../runtime-features/methods.md' }
---

# Access control

Path: `TestServer / AccessControl`

A test surface for the OPC UA access-level model. 50 variables
split across 5 groups: access levels, AdminOnly, OperatorLevel,
ViewerLevel, and AllCombinations.

## 1. Access levels

Path: `AccessControl / AccessLevels`

5 variables, one per access-level flavour:

| BrowseName          | accessLevel             | userAccessLevel | Initial | Notes                                      |
| ------------------- | ----------------------- | --------------- | ------- | ------------------------------------------ |
| `CurrentRead_Only`  | `CurrentRead`           | `CurrentRead`   | `100`   | Write → `Bad_NotWritable`                  |
| `CurrentWrite_Only` | `CurrentRead+Write`     | `CurrentWrite`  | `0`     | Read → `Bad_NotReadable` (user level)      |
| `ReadWrite`          | `CurrentRead+Write`    | `CurrentRead+Write` | `42` | Full RW                                    |
| `HistoryRead_Only`  | `CurrentRead+HistoryRead` | same          | `55`    | History-read enabled                       |
| `FullAccess`        | `CurrentRead+Write+HistoryRead` | same    | `77`    | Everything works                            |

### accessLevel vs userAccessLevel

The OPC UA spec gives each variable **two** access-level
attributes:

- `accessLevel` (id 17): server-level capabilities — what the
  variable can do.
- `userAccessLevel` (id 18): per-user capabilities — what the
  current session is allowed to do.

For `CurrentWrite_Only`, the server *could* read (the variable
holds a value internally), but the user *cannot*. `accessLevel`
includes `CurrentRead`; `userAccessLevel` is only `CurrentWrite`.

Reading id 17 vs id 18 separately is the standard
test for "did your client expose both attributes correctly?".

## 2. AdminOnly

Path: `AccessControl / AdminOnly`

Variables intended for admin-only access. OPC-level access is
RW for all; role enforcement is server-side.

| BrowseName          | Type      | Initial                  | Purpose                       |
| ------------------- | --------- | ------------------------ | ----------------------------- |
| `SecretConfig`      | String    | `"admin-secret-value"`   | Sensitive config              |
| `SystemParameter`   | Int32     | `999`                    | System-level parameter        |
| `CalibrationFactor` | Double    | `99.99`                  | Calibration coefficient       |
| `MaintenanceMode`   | Boolean   | `false`                  | Maintenance flag              |

Behaviour:

| Connected as | Read    | Write                       |
| ------------ | ------- | --------------------------- |
| `admin`      | OK      | OK                           |
| `operator`   | OK      | depends on rolePermissions  |
| `viewer`     | OK      | depends on rolePermissions  |

## 3. OperatorLevel

Path: `AccessControl / OperatorLevel`

Variables with **role-based write protection**. `admin` and
`operator` can write; `viewer` cannot.

| BrowseName        | Type      | Initial    | Purpose                |
| ----------------- | --------- | ---------- | ---------------------- |
| `Setpoint`        | Double    | `50.0`     | Process setpoint       |
| `MotorSpeed`      | Int32     | `1500`     | Motor speed (RPM)      |
| `ProcessEnabled`  | Boolean   | `true`     | Process enable flag    |
| `RecipeName`      | String    | `"default"` | Active recipe name    |

Behaviour matrix:

| Connected as | Read    | Write                        |
| ------------ | ------- | ---------------------------- |
| `admin`      | OK      | OK                            |
| `operator`   | OK      | OK                            |
| `viewer`     | OK      | `Bad_UserAccessDenied`       |

This is the canonical test for "role-aware writes" in your
client.

## 4. ViewerLevel

Path: `AccessControl / ViewerLevel`

Read-only by design. All five roles can read; none can write.

| BrowseName            | Type      | Value                  | Notes                              |
| --------------------- | --------- | ---------------------- | ---------------------------------- |
| `ProductionCount`     | UInt32    | `12345`                | Static                              |
| `MachineName`         | String    | `"Machine-001"`        | Static                              |
| `IsRunning`           | Boolean   | `true`                 | Static                              |
| `CurrentTemperature`  | Double    | `~22.5 ± 0.5`          | Slight random noise per read       |
| `UptimeSeconds`       | UInt32    | server uptime           | Increases continuously              |

`UptimeSeconds` and `CurrentTemperature` are good for confirming
"read returns fresh data per call".

## 5. AllCombinations

Path: `AccessControl / AllCombinations`

Every combination of 8 data types × 4 access modes = **32
variables**. Systematic test surface.

### Access mode suffix legend

| Suffix | Access (accessLevel + userAccessLevel)              |
| ------ | --------------------------------------------------- |
| `_RO`  | `CurrentRead`                                       |
| `_RW`  | `CurrentRead + CurrentWrite`                        |
| `_WO`  | `userAccessLevel = CurrentWrite` (server can read)  |
| `_HR`  | `CurrentRead + HistoryRead`                         |

### Type matrix

| Type     | `_RO` value      | `_RW` initial | `_WO` initial | `_HR` initial   |
| -------- | ---------------- | ------------- | ------------- | --------------- |
| Boolean   | `true`          | `false`       | `false`       | `true`          |
| Int32     | `-42`           | `0`           | `0`           | `-42`           |
| UInt32    | `42`            | `0`           | `0`           | `42`            |
| Double    | `3.14`          | `0.0`         | `0.0`         | `3.14`          |
| String    | `"immutable"`   | `""`          | `""`          | `"immutable"`   |
| DateTime  | `2024-01-01`    | now           | now           | `2024-01-01`    |
| Byte      | `128`           | `0`           | `0`           | `128`           |
| Float     | `2.71`          | `0.0`         | `0.0`         | `2.71`          |

### Variable list

```text
AllCombinations/Boolean_RO     Boolean_RW     Boolean_WO     Boolean_HR
AllCombinations/Int32_RO       Int32_RW       Int32_WO       Int32_HR
AllCombinations/UInt32_RO      UInt32_RW      UInt32_WO      UInt32_HR
AllCombinations/Double_RO      Double_RW      Double_WO      Double_HR
AllCombinations/String_RO      String_RW      String_WO      String_HR
AllCombinations/DateTime_RO    DateTime_RW    DateTime_WO    DateTime_HR
AllCombinations/Byte_RO        Byte_RW        Byte_WO        Byte_HR
AllCombinations/Float_RO       Float_RW       Float_WO       Float_HR
```

## Test patterns

### Read-only rejection

```text
for type in [Boolean, Int32, ..., Float]:
    status = write(AllCombinations/{type}_RO, sample_value)
    assert status == Bad_NotWritable
```

### Write-only rejection on read

```text
for type:
    dv = read(AllCombinations/{type}_WO)
    assert dv.statusCode == Bad_NotReadable
```

### Full RW happy path

```text
for type:
    write(AllCombinations/{type}_RW, sample_value)
    assert read(AllCombinations/{type}_RW).value == sample_value
```

### HistoryRead flag

```text
for type:
    access = read_attribute(AllCombinations/{type}_HR, AccessLevel)
    assert (access & HistoryRead) != 0
```

### Role-based write

Connect to `opcua-userpass` (4841) as `viewer`:

```text
status = write(AccessControl/OperatorLevel/Setpoint, 60.0)
assert status == Bad_UserAccessDenied
```

Same write as `operator` → `Good`.

## Where to read next

- [User accounts and roles](../authentication/user-accounts-and-roles.md) —
  the role-to-permission mapping.
- [Browse paths and access levels](../address-space/browse-paths-and-access-levels.md) —
  the access-level legend.

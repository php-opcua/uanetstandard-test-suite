---
eyebrow: 'Docs · Reference'
lede:    'The full URL table — every endpoint, every port, every service name, in one place.'

see_also:
  - { href: '../server-instances/overview.md',   meta: '5 min' }
  - { href: './environment-variables.md',         meta: '4 min' }

prev: { label: 'Environment variables', href: './environment-variables.md' }
next: { label: 'Troubleshooting',       href: './troubleshooting.md' }
---

# Ports and endpoints

The endpoint URLs and Docker service names for every service.

## Endpoint URLs (from the host)

| Service                | Endpoint URL                                | Transport |
| ---------------------- | ------------------------------------------- | --------- |
| `opcua-no-security`     | `opc.tcp://localhost:4840/UA/TestServer`    | TCP       |
| `opcua-userpass`        | `opc.tcp://localhost:4841/UA/TestServer`    | TCP       |
| `opcua-certificate`     | `opc.tcp://localhost:4842/UA/TestServer`    | TCP       |
| `opcua-all-security`    | `opc.tcp://localhost:4843/UA/TestServer`    | TCP       |
| `opcua-discovery`       | `opc.tcp://localhost:4844`                   | TCP (no resource path) |
| `opcua-auto-accept`     | `opc.tcp://localhost:4845/UA/TestServer`    | TCP       |
| `opcua-sign-only`       | `opc.tcp://localhost:4846/UA/TestServer`    | TCP       |
| `opcua-legacy`          | `opc.tcp://localhost:4847/UA/TestServer`    | TCP       |
| `opcua-ecc-nist`        | `opc.tcp://localhost:4848/UA/TestServer`    | TCP       |
| `opcua-ecc-brainpool`   | `opc.tcp://localhost:4849/UA/TestServer`    | TCP       |
| `opcua-sks`             | `opc.tcp://localhost:4851/UA/TestServer`    | TCP       |
| `opcua-pubsub` (via relay) | `opc.udp://127.0.0.1:14850`              | UDP       |

## Endpoint URLs (container-to-container)

For inter-container connections inside the compose network, use
the **service name** instead of `localhost`:

| Service                 | Internal URL                                     |
| ----------------------- | ------------------------------------------------ |
| `opcua-no-security`     | `opc.tcp://opcua-no-security:4840/UA/TestServer` |
| `opcua-userpass`        | `opc.tcp://opcua-userpass:4841/UA/TestServer`    |
| ...                     | …same pattern                                    |
| `opcua-pubsub-relay`    | `opc.udp://opcua-pubsub-relay:4850`              |

Server certs include all the service names in their SAN, so
hostname validation passes either way.

## Ports

Mapped to the host (`docker-compose.yml`):

| Port  | Protocol | Service                  |
| ----- | -------- | ------------------------ |
| 4840  | TCP      | `opcua-no-security`      |
| 4841  | TCP      | `opcua-userpass`         |
| 4842  | TCP      | `opcua-certificate`      |
| 4843  | TCP      | `opcua-all-security`     |
| 4844  | TCP      | `opcua-discovery`        |
| 4845  | TCP      | `opcua-auto-accept`      |
| 4846  | TCP      | `opcua-sign-only`        |
| 4847  | TCP      | `opcua-legacy`           |
| 4848  | TCP      | `opcua-ecc-nist`         |
| 4849  | TCP      | `opcua-ecc-brainpool`    |
| 4851  | TCP      | `opcua-sks`              |
| 14850 | UDP      | `opcua-pubsub-relay` (forwards from `opcua-pubsub`) |

The publisher's container-internal port is `4850/udp`, but
**outside the compose network** you reach it on `14850/udp` on
the host via the relay.

## Application URI

All classic servers report:

```text
urn:opcua:testserver:nodes
```

This appears in:

- Server cert SAN
- `Server.ServerStatus.BuildInfo.ProductUri`
- The cert's `subjectAltName URI`
- Returned from `GetEndpoints()` as `applicationUri`

Servers don't differ in `applicationUri` — they share it. Use
the endpoint **URL** or **port** to distinguish them in tests.

## Resource paths

| Server family                            | Path                  |
| ---------------------------------------- | --------------------- |
| All classic (4840-4843, 4845-4849, 4851)  | `/UA/TestServer`      |
| Discovery (4844)                          | (none — bare URL)     |
| PubSub (UDP 14850)                        | (none — UDP)          |

The discovery server's bare URL is per the OPC UA spec — discovery
endpoints don't need a path because they only serve the
`FindServers` and `GetEndpoints` services.

## NodeId patterns

NodeIds in `ns=1` follow these conventions:

| Pattern                                              | Example                                               |
| ---------------------------------------------------- | ----------------------------------------------------- |
| Top-level folder                                     | `ns=1;s=TestServer/DataTypes`                          |
| Variable (Scalar)                                    | `ns=1;s=TestServer/DataTypes/Scalar/BooleanValue`     |
| Variable (Array)                                     | `ns=1;s=TestServer/DataTypes/Array/Int32Array`        |
| Method                                                | `ns=1;s=TestServer/Methods/Add`                        |
| Dynamic                                              | `ns=1;s=TestServer/Dynamic/Counter`                    |
| Event source                                          | `ns=1;s=TestServer/Events/EventEmitter`                |
| Alarm                                                | `ns=1;s=TestServer/Alarms/HighTemperatureAlarm`        |
| History                                              | `ns=1;s=TestServer/Historical/HistoricalTemperature`   |
| Structure                                            | `ns=1;s=TestServer/Structures/TestPoint`               |
| Extension object                                      | `ns=1;s=TestServer/ExtensionObjects/PointValue`        |
| Access control                                        | `ns=1;s=TestServer/AccessControl/AccessLevels/CurrentRead_Only` |
| View                                                 | `ns=1;s=Views/OperatorView`                            |
| SKS object (only on opcua-sks)                        | `ns=1;s=TestServer/SecurityKeyService`                 |
| SKS method                                            | `ns=1;s=TestServer/SecurityKeyService/GetSecurityKeys` |

These are **stable** across restarts.

## Extension-object types — `ns=3`

| TypeId             | Type name           |
| ------------------ | ------------------- |
| `ns=3;i=3010`      | `TestPointXYZ`      |
| `ns=3;i=3011`      | `TestRangeStruct`   |

Read `PointValue` / `RangeValue` → check `typeId` to identify
which struct you got.

## Standard namespace shortcuts

| BrowseName            | NodeId           | Useful for                             |
| --------------------- | ---------------- | -------------------------------------- |
| `Objects`              | `ns=0;i=85`      | Root browse                            |
| `Views`                | `ns=0;i=87`      | Discover views                         |
| `Server`               | `ns=0;i=2253`    | Event subscription target              |
| `Server.ServerStatus`  | `ns=0;i=2256`    | Server state read                      |
| `Server.NamespaceArray` | `ns=0;i=2255`   | Discover namespaces                    |

## Where to read next

- [Troubleshooting](./troubleshooting.md) — common port/URL
  mistakes.
- [Address space · Overview](../address-space/overview.md) — the
  node layout in detail.

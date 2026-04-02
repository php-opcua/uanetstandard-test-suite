# Changelog

## v1.0.0 — 2026-04-02

### Initial Release

Complete rewrite of the OPC UA test server suite from Node.js/node-opcua to .NET 8.0/UA-.NETStandard.

### Server Infrastructure

- **8 pre-configured server instances** via Docker Compose covering all major OPC UA security policies, modes, and authentication methods (ports 4840-4847).
- **.NET 8.0 runtime** (Alpine) with OPCFoundation.NetStandard.Opc.Ua.Server 1.5.x.
- **Auto-generated certificates** via OpenSSL (CA, server, client, self-signed, expired) on first startup.
- **Server auto-generates its own application certificate** via `CheckApplicationInstanceCertificates()`.
- **ApplicationUri**: `urn:opcua:testserver:nodes` for all server instances.
- **Health check** support via `--health` CLI argument.

### Address Space (~300 nodes)

- **21 scalar data types** (Boolean through LocalizedText) in read/write and read-only variants.
- **20 array types** + 14 empty arrays + 6 read-only arrays.
- **3 multi-dimensional matrices** (3x3 Double, 2x4 Int32, 2x3x4 Byte).
- **12 methods** — arithmetic, string ops, arrays, async, error handling, event generation.
- **13 dynamic variables** — counters, sine/sawtooth/triangle waves, random values, status cycling.
- **3 custom event types** with periodic emission.
- **3 alarm types** — ExclusiveLimit, NonExclusiveLimit, OffNormal.
- **4 historical variables** with HistoryRead support (1000ms recording interval, up to 10,000 samples).
- **Structured objects** with nesting up to 10 levels deep.
- **2 extension object variables** with binary-encoded ExtensionObject values (namespace 3: `urn:opcua:test-server:custom-types`).
- **50 access control variables** covering every combination of type and access level.
- **4 OPC UA Views** for filtered browsing.

### Namespaces

- `ns=0`: `http://opcfoundation.org/UA/` (standard)
- `ns=1`: `urn:opcua:testserver:nodes` (all custom nodes)
- `ns=2`: `http://opcfoundation.org/UA/DI/`
- `ns=3`: `urn:opcua:test-server:custom-types` (extension object types and encodings)
- `ns=4`: `http://opcfoundation.org/UA/Diagnostics`

### Authentication & Access Control

- **4 user accounts** with role-based permissions (admin, operator, viewer, test).
- **Role-based write protection** on OperatorLevel variables (admin/operator can write, viewer cannot).
- **Operation limits** on no-security server: MaxNodesPerRead=5, MaxNodesPerWrite=5.

### Security

- All 6 OPC UA security policies: None, Basic128Rsa15, Basic256, Basic256Sha256, Aes128_Sha256_RsaOaep, Aes256_Sha256_RsaPss.
- All 3 security modes: None, Sign, SignAndEncrypt.
- Certificate authentication with CA trust chain.
- Auto-accept mode for quick encrypted testing.

### CI/CD

- **GitHub Actions composite action** (`action.yml`) for one-step CI integration.
- **Docker image** published to `ghcr.io/php-opcua/uanetstandard-test-suite`.
- **CI-optimized compose file** (`docker-compose.ci.yml`) with no-restart policy.

### Documentation

- Full documentation suite in `docs/` covering setup, servers, authentication, security, address space, data types, methods, dynamic variables, events, alarms, historical data, structures, extension objects, access control, views, testing guide, CI integration, customization, and AI reference.

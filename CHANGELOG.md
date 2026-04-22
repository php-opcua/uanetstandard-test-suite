# Changelog

## Unreleased

### Added — Security Key Service (service 11, port 4851)

- **`opcua-sks` service** — new classic OPC UA server instance that boots the shared `TestServer` image with `OPCUA_ENABLE_SKS=true`. Exposes the OPC UA Part 14 §8.4.2 `GetSecurityKeys` method under `ns=1;s=TestServer/SecurityKeyService`, letting PubSub subscriber-side clients (e.g. `php-opcua/opcua-client-ext-pubsub`'s `SksGroupKeyProvider`) exercise the real RPC path against a live server instead of only `MockClient`.
- **`SecurityKeyServiceBuilder`** under `src/TestServer/AddressSpace/` — mirrors the existing builder pattern, self-contained, opt-in via `EnableSks` config flag so the other 10 server instances are unaffected.
- **Env-driven config** — `OPCUA_ENABLE_SKS`, `OPCUA_SKS_GROUP_ID`, `OPCUA_SKS_POLICY_URI`, `OPCUA_SKS_TOKEN_ID`, `OPCUA_SKS_SIGNING_KEY_HEX`, `OPCUA_SKS_ENCRYPTING_KEY_HEX`, `OPCUA_SKS_KEY_NONCE_HEX`, `OPCUA_SKS_TIME_TO_NEXT_KEY_MS`, `OPCUA_SKS_KEY_LIFETIME_MS`. All off by default on the other services; on only for the dedicated `opcua-sks` service.
- **Action update** — `sks` is a new option for the `servers` CSV input in `action.yml`.
- **Docs** — new section "11. Security Key Service" in `docs/servers.md`, new row in the endpoint table of `docs/ci-integration.md`, new row in `README.md` "What's Inside".
- **Test-only scope** — hardcoded keys, no caller authentication, no rotation scheduling, no revocation. Real SKS deployments are expected to do all of the above.

### Added — PubSub publisher (service 12, port 4850)

- **`opcua-pubsub` service** — new UA-.NETStandard UDP+UADP publisher built from `src/TestPublisher/` (separate Dockerfile `Dockerfile.publisher`, separate `.csproj` against the `OPCFoundation.NetStandard.Opc.Ua.PubSub` NuGet package). Broadcasts a deterministic `DataSet` (counter / timestamp / sine-value) every 500 ms, bringing real UADP interop coverage for Part 14 subscriber-side clients (e.g. `php-opcua/opcua-client-ext-pubsub`).
- **Env-driven configuration** — same `OPCUA_*` prefix convention as the TestServer services. `OPCUA_URL`, `OPCUA_NETWORK_INTERFACE`, `OPCUA_PUBLISHER_ID`, `OPCUA_WRITER_GROUP_ID`, `OPCUA_DATASET_WRITER_ID`, `OPCUA_DATASET_NAME`, `OPCUA_PUBLISH_INTERVAL_MS`, `OPCUA_TICK_INTERVAL_MS`, `OPCUA_LOG_LEVEL` — one image, reconfigure via compose env.
- **Networking** — publisher + `opcua-pubsub-relay` sidecar pair. Publisher sends unicast UADP to the relay on a shared compose bridge (`pubsub-net`); the relay forwards each datagram to `host.docker.internal:14850`. Subscribers on the physical host listen on `127.0.0.1:14850` (or `0.0.0.0:14850`). Works identically on Docker Engine bare-metal (GitHub Actions runners, CI) and Docker Desktop (Linux / macOS / Windows) — multicast over the VM boundary is sidestepped entirely.
- **Security** — unsecured (mode `None`). For signed and encrypted PubSub streams subscribers pair this with the `opcua-sks` service (service 11) — full end-to-end secured publisher + SKS is planned follow-up work.
- **Action update** — `pubsub` is a new option for the `servers` CSV input in `action.yml`.
- **Docs** — new section "12. PubSub Publisher" in `docs/servers.md`, new row in the endpoint table of `docs/ci-integration.md`, new row in `README.md` "What's Inside".

### Changed

- **Pinned UA-.NETStandard NuGet version to `1.5.378.134`** (previously `1.5.*`). The wildcard would auto-upgrade on every Docker build, which defeats the purpose of a stable interop counterpart: any upstream change to protocol semantics would silently break every client test run until someone noticed. Pinning makes NuGet upgrades an explicit decision.
  - **Why 1.5.378.134 specifically:** it is the latest stable (released 2026-03-26) that predates the "Secure channel enhancements 2025 11" rework in UA-.NETStandard master (commit [`d188383`](https://github.com/OPCFoundation/UA-.NETStandard/commit/d188383), merged 2026-04-16). That rework turns on strict OPC UA 1.05.4 ECC behaviour — first sequence number for ECC policies MUST be 0, with wrap at `UInt32.MaxValue` — and adds `_AesGcm` / `_ChaChaPoly` policy variants. A client speaking 1.05.3 ECC against a strict server would fail at the first message.
  - **When to bump:** once a client in the ecosystem (e.g. `php-opcua/opcua-client`) ships the 1.05.4 ECC fix, coordinate a bump here and in the client's integration tests in the same release train.

## v1.1.0 — 2026-04-10

### ECC Security Policies

- **2 new server instances** for Elliptic Curve Cryptography (ECC) security policies (ports 4848-4849).
- **Server 9 — ECC NIST** (`opcua-ecc-nist`, port 4848): ECC_nistP256 and ECC_nistP384 policies with NIST P-256/P-384 curves.
- **Server 10 — ECC Brainpool** (`opcua-ecc-brainpool`, port 4849): ECC_brainpoolP256r1 and ECC_brainpoolP384r1 policies (European BSI standard).
- **ECC certificates auto-generated** by UA-.NETStandard SDK via `ApplicationCertificates` collection with `CertificateType` mapping.
- Updated GitHub Actions composite action (`action.yml`) with `ecc-nist`, `ecc-brainpool` server options.
- Updated CI compose overrides (`docker-compose.ci.yml`) for the 2 new services.
- **Upgraded to .NET 10.0** runtime and SDK.

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

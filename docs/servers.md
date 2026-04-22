# Server Instances

The suite runs 10 client/server OPC UA instances plus 2 PubSub-related services (a Security Key Service and a UDP+UADP publisher). All server-side instances share the same codebase and expose the same address space — only security, authentication, and network settings differ.

## Overview

| # | Service Name | Port | Security Policy | Security Mode | Authentication |
|---|---|---|---|---|---|
| 1 | `opcua-no-security` | 4840 | None | None | Anonymous only |
| 2 | `opcua-userpass` | 4841 | Basic256Sha256 | SignAndEncrypt | Username/Password |
| 3 | `opcua-certificate` | 4842 | Basic256Sha256, Aes128, Aes256 | Sign, SignAndEncrypt | X.509 Certificate |
| 4 | `opcua-all-security` | 4843 | All 6 RSA policies | All 3 modes | Anonymous + Username + Certificate |
| 5 | `opcua-discovery` | 4844 | None, Basic256Sha256 | None, SignAndEncrypt | Anonymous |
| 6 | `opcua-auto-accept` | 4845 | Basic256Sha256 | SignAndEncrypt | Anonymous + Username + Certificate |
| 7 | `opcua-sign-only` | 4846 | Basic256Sha256 | Sign | Anonymous + Username |
| 8 | `opcua-legacy` | 4847 | Basic128Rsa15, Basic256 | Sign, SignAndEncrypt | Anonymous + Username |
| 9 | `opcua-ecc-nist` | 4848 | ECC_nistP256, ECC_nistP384 | Sign, SignAndEncrypt | Anonymous + Username + Certificate |
| 10 | `opcua-ecc-brainpool` | 4849 | ECC_brainpoolP256r1, ECC_brainpoolP384r1 | Sign, SignAndEncrypt | Anonymous + Username + Certificate |
| 11 | `opcua-sks` | 4851 | None | None | Anonymous only |
| 12 | `opcua-pubsub` + `opcua-pubsub-relay` | 14850 (UDP, host side) | N/A (PubSub §8 group keys) | None (unsecured) | N/A (PubSub is stateless) |

---

## 1. No Security (`opcua-no-security` -- port 4840)

**Endpoint:** `opc.tcp://localhost:4840/UA/TestServer`

The simplest possible configuration. No encryption, no signing, anonymous access only.

**Operation limits:** `MaxNodesPerRead=5`, `MaxNodesPerWrite=5`

**Use for testing:**
- Basic connectivity
- Browse, read, write operations without security overhead
- Subscription and monitored items
- Method calls
- Initial client development
- Operation limit handling

---

## 2. Username/Password (`opcua-userpass` -- port 4841)

**Endpoint:** `opc.tcp://localhost:4841/UA/TestServer`

Encrypted channel with username/password authentication. Uses `Basic256Sha256` policy with `SignAndEncrypt` mode.

**Use for testing:**
- Username/password authentication flow
- Encrypted communication
- Role-based access (admin, operator, viewer users)
- Authentication failure handling (wrong credentials)

**Credentials:** See [Authentication & Roles](authentication.md)

---

## 3. Certificate Authentication (`opcua-certificate` -- port 4842)

**Endpoint:** `opc.tcp://localhost:4842/UA/TestServer`

Multiple security policies with X.509 certificate-based client authentication. No anonymous access, no username/password.

**Policies:** Basic256Sha256, Aes128_Sha256_RsaOaep, Aes256_Sha256_RsaPss
**Modes:** Sign, SignAndEncrypt

**Use for testing:**
- X.509 certificate authentication
- Client certificate validation
- Multiple security policy negotiation
- Certificate rejection (self-signed, expired)

**Client certificates:** See [Security & Certificates](security.md)

---

## 4. All Security (`opcua-all-security` -- port 4843)

**Endpoint:** `opc.tcp://localhost:4843/UA/TestServer`

Every security policy, every security mode, and every authentication method enabled simultaneously.

**Policies:** None, Basic128Rsa15, Basic256, Basic256Sha256, Aes128_Sha256_RsaOaep, Aes256_Sha256_RsaPss
**Modes:** None, Sign, SignAndEncrypt
**Auth:** Anonymous, Username/Password, X.509 Certificate

**Use for testing:**
- Endpoint discovery and selection logic
- Security policy negotiation
- Fallback behavior between security levels
- GetEndpoints response parsing with many endpoints

---

## 5. Discovery Server (`opcua-discovery` -- port 4844)

**Endpoint:** `opc.tcp://localhost:4844`

A dedicated OPC UA Discovery Server (not a regular server). Other servers in the suite can register with it.

**Use for testing:**
- FindServers request
- Server registration and discovery
- Discovery service integration

> Note: This server does NOT expose the test address space. It only provides discovery services.

---

## 6. Auto-Accept Certificates (`opcua-auto-accept` -- port 4845)

**Endpoint:** `opc.tcp://localhost:4845/UA/TestServer`

Like `opcua-userpass` but automatically accepts any client certificate, even unknown ones. Also allows anonymous access (`OPCUA_ALLOW_ANONYMOUS=true`).

**Use for testing:**
- Quick encrypted testing without certificate setup
- Client certificate provisioning flows
- Auto-trust behavior

---

## 7. Sign Only (`opcua-sign-only` -- port 4846)

**Endpoint:** `opc.tcp://localhost:4846/UA/TestServer`

Uses `Basic256Sha256` with `Sign` mode only (no encryption, just message signing).

**Use for testing:**
- Sign-only security mode
- Verifying that messages are signed but not encrypted
- Distinguishing Sign from SignAndEncrypt behavior

---

## 8. Legacy Security (`opcua-legacy` -- port 4847)

**Endpoint:** `opc.tcp://localhost:4847/UA/TestServer`

Uses deprecated security policies: `Basic128Rsa15` and `Basic256`. These are considered insecure but still found in older OPC UA deployments.

**Use for testing:**
- Backward compatibility with legacy servers
- Deprecated policy support
- Security policy upgrade/migration scenarios
- Warning/logging when using deprecated policies

---

## 9. ECC NIST (`opcua-ecc-nist` -- port 4848)

**Endpoint:** `opc.tcp://localhost:4848/UA/TestServer`

ECC security using NIST curves. Uses pre-generated ECC P-256 server certificate.

**Policies:** ECC_nistP256, ECC_nistP384
**Modes:** Sign, SignAndEncrypt
**Auth:** Anonymous, Username/Password, X.509 Certificate

**Use for testing:**
- ECC-based security with NIST standard curves
- ECDSA signatures (SHA-256 / SHA-384)
- Ephemeral ECDH key agreement
- ECC certificate validation
- Modern security (~128-bit / ~192-bit equivalent)

**ECC Client certificates:** Available at `certs/ecc-nist/client/`

---

## 10. ECC Brainpool (`opcua-ecc-brainpool` -- port 4849)

**Endpoint:** `opc.tcp://localhost:4849/UA/TestServer`

ECC security using Brainpool curves (European BSI standard). Uses pre-generated ECC brainpoolP256r1 server certificate.

**Policies:** ECC_brainpoolP256r1, ECC_brainpoolP384r1
**Modes:** Sign, SignAndEncrypt
**Auth:** Anonymous, Username/Password, X.509 Certificate

**Use for testing:**
- ECC-based security with Brainpool curves
- European regulatory compliance (BSI TR-03116)
- Alternative to NIST curves with verifiably random parameters
- ECC certificate validation with non-NIST curves

---

## 11. Security Key Service (`opcua-sks` -- port 4851)

**Endpoint:** `opc.tcp://localhost:4851/UA/TestServer`

Classic OPC UA server that exposes the OPC UA Part 14 §8.4.2 `GetSecurityKeys` method under `ns=1;s=TestServer/SecurityKeyService`. Subscriber-side PubSub clients can exercise the real `Client::call()` path against a live server instead of only mocking it. Uses UA-.NETStandard's standard server stack — the SKS method itself is a minimal test-only implementation that returns pre-seeded, hardcoded key material.

**Method signature:**

```
GetSecurityKeys(
    securityGroupId: String,
    startingTokenId: UInt32,
    requestedKeyCount: UInt32,
) returns (
    securityPolicyUri: String,
    firstTokenId: UInt32,
    keys: ByteString[],
    timeToNextKey: Duration,
    keyLifetime: Duration,
)
```

**Well-known NodeIds:**

- Object: `ns=1;s=TestServer/SecurityKeyService`
- Method: `ns=1;s=TestServer/SecurityKeyService/GetSecurityKeys`

**Default key layout (for `PubSub-Aes256-CTR`):**

| Component | Bytes | Default (hex) |
|---|---|---|
| Signing key (HMAC-SHA256) | 32 | `01` repeated 32 times |
| Encrypting key (AES-256)  | 32 | `02` repeated 32 times |
| Key nonce                 | 4  | `03 03 03 03` |

Together they form the 68-byte `Keys[0]` ByteString returned by `GetSecurityKeys`.

**Environment variables (all optional):**

| Variable | Default | Purpose |
|---|---|---|
| `OPCUA_ENABLE_SKS` | `true` on this service, `false` elsewhere | Mount the SecurityKeyService builder |
| `OPCUA_SKS_GROUP_ID` | `test-group` | Security group id accepted by `GetSecurityKeys` |
| `OPCUA_SKS_POLICY_URI` | `http://opcfoundation.org/UA/SecurityPolicy#PubSub-Aes256-CTR` | Security policy URI returned |
| `OPCUA_SKS_TOKEN_ID` | `7` | UInt32 token id of the current key |
| `OPCUA_SKS_SIGNING_KEY_HEX` | 32 bytes of `0x01` | HMAC-SHA256 signing key (hex) |
| `OPCUA_SKS_ENCRYPTING_KEY_HEX` | 32 bytes of `0x02` | AES-256 encrypting key (hex) |
| `OPCUA_SKS_KEY_NONCE_HEX` | `03030303` | 4-byte key nonce (hex) |
| `OPCUA_SKS_TIME_TO_NEXT_KEY_MS` | `300000` | Milliseconds until the next key rotates in |
| `OPCUA_SKS_KEY_LIFETIME_MS` | `600000` | Total lifetime of one key (ms) |

**Use for testing:**

- Subscriber-side `SksGroupKeyProvider` (or equivalent) against a real OPC UA method call
- Error paths: unknown `securityGroupId` returns `BadNotFound`
- Key-layout consumers that split the returned `ByteString` per policy (32 + 32 + 4 bytes for `PubSub-Aes256-CTR`)

> **Note:** this is a **test-only SKS**. Real deployments authenticate the caller, scope keys per security group, rotate on schedule, and revoke compromised tokens — none of that is done here. Do not copy this into production.

---

## 12. PubSub Publisher (`opcua-pubsub` + `opcua-pubsub-relay` -- host port 14850)

**Host endpoint:** `opc.udp://127.0.0.1:14850` (reachable from the machine running the subscriber tests)

A [UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard) PubSub publisher that emits UADP (OPC UA Binary over UDP) `NetworkMessages` every 500 ms — see [OPC UA Part 14](https://reference.opcfoundation.org/Core/Part14/v105/docs/) for the spec.

### Why two services?

PubSub natively uses multicast, which does not traverse the Docker Desktop VM boundary reliably across platforms. To give subscribers a single cross-platform endpoint, the publisher sends **unicast** to a companion `opcua-pubsub-relay` sidecar (a stateless `socat` forwarder) on a shared `pubsub-net` compose bridge; the relay re-sends each packet to `host.docker.internal:14850`, which Docker Desktop and Docker Engine both bridge to the physical host.

```
opcua-pubsub ── pubsub-net ──► opcua-pubsub-relay ──host.docker.internal──► 127.0.0.1:14850 on the host
```

This works identically on:
- **Linux + Docker Engine** (GitHub Actions runners, bare-metal servers)
- **Docker Desktop** (Linux / macOS / Windows)

**DataSet shape (every message):**

| Field | Type | Value |
|---|---|---|
| `counter` | `UInt32` | Monotonically increasing from 0 |
| `timestamp` | `DateTime` | UTC publish time |
| `value` | `Double` | `sin(counter × π / 20)` sine wave between -1.0 and 1.0 |

**NetworkMessage headers:**

- `PublisherId` (UInt16) — default `100`
- `WriterGroupId` — default `1`
- `DataSetWriterId` — default `1`
- `PublishingInterval` — default `500` ms

**Environment variables (all optional):**

| Variable | Default | Purpose |
|---|---|---|
| `OPCUA_URL` | `opc.udp://opcua-pubsub-relay:4850` | PubSub endpoint URL — resolved on the compose bridge to the relay sidecar |
| `OPCUA_PUBSUB_HOST_PORT` | `14850` | UDP port on the physical host where the relay forwards packets |
| `OPCUA_NETWORK_INTERFACE` | `` (all NICs) | NIC name to bind on (empty = all; UA-.NETStandard skips loopback by design, never pass `lo`) |
| `OPCUA_PUBLISHER_ID` | `100` | UInt16 publisher id |
| `OPCUA_WRITER_GROUP_ID` | `1` | UInt16 writer group id |
| `OPCUA_DATASET_WRITER_ID` | `1` | UInt16 DataSet writer id |
| `OPCUA_DATASET_NAME` | `Simple` | DataSet name reported in metadata |
| `OPCUA_PUBLISH_INTERVAL_MS` | `500` | Publishing interval (ms) |
| `OPCUA_TICK_INTERVAL_MS` | `250` | Internal value-simulator tick rate (ms) |
| `OPCUA_LOG_LEVEL` | `Information` | `Debug` \| `Information` \| `Warning` \| `Error` |

**Use for testing:**

- Subscriber libraries that decode UADP (`php-opcua/opcua-client-ext-pubsub`, equivalents in other languages)
- UDP unicast and multicast subscriber wiring
- PublisherId / WriterGroupId / DataSetWriterId demuxing
- Sequence number continuity checks across messages
- Field name / type interpretation against a known `DataSetMetaData`

> **Note:** this publisher emits **unsecured** PubSub (mode `None`). A companion [Security Key Service](#11-security-key-service-opcua-sks----port-4851) is available on port 4851 for clients that need to exercise the SKS RPC path; joining the two (a secured publisher fed by the SKS) is planned follow-up work.


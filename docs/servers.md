# Server Instances

The suite runs 8 independent OPC UA server instances, each configured for a specific testing scenario. All servers share the same codebase and expose the same address space — only security, authentication, and network settings differ.

## Overview

| # | Service Name | Port | Security Policy | Security Mode | Authentication |
|---|---|---|---|---|---|
| 1 | `opcua-no-security` | 4840 | None | None | Anonymous only |
| 2 | `opcua-userpass` | 4841 | Basic256Sha256 | SignAndEncrypt | Username/Password |
| 3 | `opcua-certificate` | 4842 | Basic256Sha256, Aes128, Aes256 | Sign, SignAndEncrypt | X.509 Certificate |
| 4 | `opcua-all-security` | 4843 | All 6 policies | All 3 modes | Anonymous + Username + Certificate |
| 5 | `opcua-discovery` | 4844 | None, Basic256Sha256 | None, SignAndEncrypt | Anonymous |
| 6 | `opcua-auto-accept` | 4845 | Basic256Sha256 | SignAndEncrypt | Anonymous + Username + Certificate |
| 7 | `opcua-sign-only` | 4846 | Basic256Sha256 | Sign | Anonymous + Username |
| 8 | `opcua-legacy` | 4847 | Basic128Rsa15, Basic256 | Sign, SignAndEncrypt | Anonymous + Username |

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

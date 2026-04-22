# UA-.NETStandard Test Server Suite — Documentation

A comprehensive OPC UA server suite designed for testing OPC UA client libraries. Built on `OPCFoundation.NetStandard.Opc.Ua.Server` 1.5.x (.NET 10.0), deployed via Docker Compose as 10 client/server instances (ports 4840-4849) plus a PubSub UDP+UADP publisher (port 4850) and a Security Key Service (port 4851).

## Table of Contents

1. [Setup & Installation](setup.md) — Docker setup, build, run, and teardown
2. [Server Instances](servers.md) — The 10 client/server configurations + PubSub publisher + Security Key Service and when to use each
3. [Authentication & Roles](authentication.md) — Users, passwords, roles, permissions, and certificate-based auth
4. [Security & Certificates](security.md) — Security policies, modes, certificate structure, and trust chain
5. [Address Space Overview](address-space.md) — Top-level structure and navigation
6. [Data Types](data-types.md) — All 21 OPC UA scalar types, arrays, matrices, and analog items
7. [Methods](methods.md) — 12 callable methods with signatures and examples
8. [Dynamic Variables](dynamic-variables.md) — 13 time-varying variables (counters, waves, random)
9. [Events & Alarms](events-and-alarms.md) — Custom event types, periodic events, and 3 alarm types
10. [Historical Data](historical-data.md) — 4 variables with history access
11. [Structures](structures.md) — Nested objects, collections, and deep nesting for browse tests
12. [Extension Objects](extension-objects.md) — Custom structured types with binary encoding (PointValue, RangeValue)
13. [Access Control](access-control.md) — Access levels, role-based folders, and all type/access combinations
14. [Views](views.md) — 4 OPC UA views for filtered browsing
15. [Testing Guide](testing-guide.md) — Practical scenarios and how to test each feature
16. [CI Integration](ci-integration.md) — GitHub Actions, GitLab CI, Docker Compose usage
17. [Customization](customization.md) — How to fork and build your own OPC UA simulations
18. [AI Reference](AI_REFERENCE.md) — Single-file machine-readable reference for AI tools

## Quick Start

```bash
# Start the whole suite (10 client/server + PubSub publisher + SKS)
docker compose up -d

# Verify they're running
docker compose ps

# Connect to the simplest server (no security)
# Endpoint: opc.tcp://localhost:4840/UA/TestServer

# Listen to the PubSub publisher (UADP via relay sidecar)
# Endpoint: opc.udp://127.0.0.1:14850

# Fetch PubSub group keys from the SKS
# Endpoint: opc.tcp://localhost:4851/UA/TestServer
# Method:   ns=1;s=TestServer/SecurityKeyService/GetSecurityKeys

# Stop everything
docker compose down
```

## Architecture

The main codebase (`src/TestServer/`) is instantiated 11 times via `docker-compose.yml` — 10 client/server instances covering every security/auth scenario, plus a dedicated Security Key Service instance (SKS) that enables a Part 14 §8.4.2 `GetSecurityKeys` method builder. A separate codebase (`src/TestPublisher/`) ships the PubSub publisher; a companion `opcua-pubsub-relay` sidecar (`alpine/socat`) bridges the UADP frames to the physical host on UDP port 14850.

```
Client ──► opcua-no-security    (4840)  No security, anonymous only
       ──► opcua-userpass       (4841)  Encrypted + username/password
       ──► opcua-certificate    (4842)  Multi-policy + certificate auth
       ──► opcua-all-security   (4843)  All policies, all modes, all auth
       ──► opcua-discovery      (4844)  Discovery server
       ──► opcua-auto-accept    (4845)  Auto-accepts any client cert
       ──► opcua-sign-only      (4846)  Sign mode only (no encryption)
       ──► opcua-legacy         (4847)  Deprecated policies (Basic128, Basic256)
       ──► opcua-ecc-nist       (4848)  ECC NIST (P-256, P-384)
       ──► opcua-ecc-brainpool  (4849)  ECC Brainpool (P-256r1, P-384r1)
       ──► opcua-sks            (4851)  Security Key Service (GetSecurityKeys)

UDP 14850 ◄─ opcua-pubsub + opcua-pubsub-relay    PubSub UDP+UADP publisher (Part 14), relayed via `host.docker.internal`
```

## Node Count Summary

| Category | Count |
|---|---|
| Scalar variables (RW) | 21 |
| Scalar variables (RO) | 21 |
| Array variables (RW) | 20 |
| Array variables (RO) | 6 |
| Empty arrays | 14 |
| Multi-dimensional arrays | 3 |
| Analog items with range | 3 |
| Methods | 12 |
| Dynamic variables | 13 |
| Event types | 3 |
| Alarms | 3 (+2 source variables) |
| Historical variables | 4 |
| Structure objects | 4 + 5 collection + 10 deep |
| Extension objects | 2 |
| Access control variables | 50 |
| Views | 4 |
| **Total nodes** | **~300** |

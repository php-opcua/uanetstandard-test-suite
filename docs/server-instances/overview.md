---
eyebrow: 'Docs · Server instances'
lede:    'Twelve services run from one codebase, each shaped by environment variables. The map of which port does what, and how to pick the right one.'

see_also:
  - { href: './classic-rsa-and-ecc.md',         meta: '6 min' }
  - { href: './special-purpose.md',             meta: '5 min' }
  - { href: '../reference/ports-and-endpoints.md', meta: '3 min' }

prev: { label: 'First connection',   href: '../getting-started/first-connection.md' }
next: { label: 'Classic RSA and ECC servers', href: './classic-rsa-and-ecc.md' }
---

# Server instances overview

The suite is **one C# codebase**, instantiated 11 times via
`docker-compose.yml` with different environment variables. Each
classic server exposes the same ~200-node application address
space — only the security wrapping and the enabled
authentication methods differ. Two additional services (the
PubSub publisher and its UDP relay sidecar) run from a second
codebase.

## The full map

| # | Service                | Port (host) | Codebase     | Group         |
| - | ---------------------- | ----------- | ------------ | ------------- |
| 1 | `opcua-no-security`    | 4840        | TestServer   | classic RSA   |
| 2 | `opcua-userpass`       | 4841        | TestServer   | classic RSA   |
| 3 | `opcua-certificate`    | 4842        | TestServer   | classic RSA   |
| 4 | `opcua-all-security`   | 4843        | TestServer   | classic RSA   |
| 5 | `opcua-discovery`      | 4844        | TestServer   | special       |
| 6 | `opcua-auto-accept`    | 4845        | TestServer   | classic RSA   |
| 7 | `opcua-sign-only`      | 4846        | TestServer   | classic RSA   |
| 8 | `opcua-legacy`         | 4847        | TestServer   | classic RSA   |
| 9 | `opcua-ecc-nist`       | 4848        | TestServer   | classic ECC   |
| 10 | `opcua-ecc-brainpool` | 4849        | TestServer   | classic ECC   |
| 11 | `opcua-sks`            | 4851        | TestServer   | special (SKS) |
| 12 | `opcua-pubsub` + `opcua-pubsub-relay` | UDP 14850 | TestPublisher | PubSub |

## Which server, when?

| You're testing…                                                  | Use                             |
| ---------------------------------------------------------------- | ------------------------------- |
| Connectivity, encoding, browse                                   | `opcua-no-security` (4840)      |
| Username/password identity                                       | `opcua-userpass` (4841)         |
| X.509 client-cert identity                                       | `opcua-certificate` (4842)      |
| Endpoint negotiation (pick best policy)                          | `opcua-all-security` (4843)     |
| `FindServers` / discovery                                        | `opcua-discovery` (4844)        |
| TOFU mode (auto-accept unknown certs)                            | `opcua-auto-accept` (4845)      |
| Sign-only mode (no encryption)                                   | `opcua-sign-only` (4846)        |
| Backwards-compat with `Basic128Rsa15` / `Basic256`               | `opcua-legacy` (4847)           |
| ECC `nistP256` / `nistP384`                                      | `opcua-ecc-nist` (4848)         |
| ECC Brainpool curves                                             | `opcua-ecc-brainpool` (4849)    |
| `GetSecurityKeys` RPC (PubSub key distribution)                  | `opcua-sks` (4851)              |
| UADP subscriber decode                                           | `opcua-pubsub` (UDP 14850)      |

For all of these, the address space is the **same** — pick the
server that gives you the right security/auth wrapper, not the
right data.

## Three "groups" — three pages

The 12 services fall into three natural groups:

- **Classic RSA servers** (4840-4847) — the bread-and-butter
  RSA policies and auth combinations. Covered in
  [Classic RSA and ECC servers](./classic-rsa-and-ecc.md).
- **Classic ECC servers** (4848-4849) — modern elliptic-curve
  policies. Same page as above.
- **Special-purpose servers** (4844 discovery, 4851 SKS, UDP
  14850 PubSub) — distinct protocols / features. Covered in
  [Special-purpose servers](./special-purpose.md).

## Shared characteristics

All 10 classic servers share:

| Property                 | Value                                              |
| ------------------------ | -------------------------------------------------- |
| Endpoint resource path   | `/UA/TestServer`                                   |
| Application URI          | `urn:opcua:testserver:nodes`                       |
| Hostname (in cert SAN)   | localhost, plus every Docker service name          |
| Default max sessions     | 100                                                |
| Default max nodes/read   | 1000 — except `opcua-no-security` (5)              |
| Default max nodes/write  | 1000 — except `opcua-no-security` (5)              |
| Address space            | ~200 nodes (sum of all builders)                   |
| Health probe binary      | `dotnet TestServer.dll --health` (returns exit 0)  |

The `--health` binary is implemented in `src/TestServer/Program.cs`
and is available in every container. However, only
`opcua-no-security` actually wires it into a Docker `healthcheck`
block in `docker-compose.yml`; the other 9 classic services do
not declare a healthcheck, so `docker compose ps` reports their
state as just `running` rather than `healthy`. The probe can
still be invoked manually (`docker compose exec <service> dotnet
TestServer.dll --health`).

## Why the address space is identical

The codebase wires up the address space **once**, regardless of
security flavour. This is intentional — your tests can reuse the
same node-id assertions across every server. If a test passes on
`opcua-no-security`, it should also pass on `opcua-userpass`
modulo the authentication step.

## Toggling features per-instance

Each instance can disable feature groups via environment
variables:

| Variable                    | Disables                                  |
| --------------------------- | ----------------------------------------- |
| `OPCUA_ENABLE_HISTORICAL=false` | History recording + nodes                |
| `OPCUA_ENABLE_EVENTS=false`     | Periodic event timers + alarms folder    |
| `OPCUA_ENABLE_METHODS=false`    | Methods folder                            |
| `OPCUA_ENABLE_DYNAMIC=false`    | Dynamic variables                         |
| `OPCUA_ENABLE_STRUCTURES=false` | Structures folder                         |
| `OPCUA_ENABLE_VIEWS=false`      | Views                                     |

All default to `true`. The 10 classic servers run with the full
set enabled. A leaner server would disable the parts irrelevant
to a particular test.

See [Environment variables](../reference/environment-variables.md)
for the full list.

## Where to read next

- [Classic RSA and ECC servers](./classic-rsa-and-ecc.md) —
  per-server detail for the 10 classics.
- [Special-purpose servers](./special-purpose.md) — discovery,
  SKS, PubSub.

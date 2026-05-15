---
eyebrow: 'Docs · Reference'
lede:    'Every OPCUA_* environment variable the suite reads. Grouped by purpose, with defaults and where each one applies.'

see_also:
  - { href: './ports-and-endpoints.md',         meta: '3 min' }
  - { href: '../server-instances/overview.md',  meta: '5 min' }

prev: { label: 'Simulation recipes',   href: '../customization/simulation-recipes.md' }
next: { label: 'Ports and endpoints',  href: './ports-and-endpoints.md' }
---

# Environment variables

All configuration is via env vars. No XML config files. The same
image, shaped by env, becomes any of the 12 services.

## Network

| Variable                | Default                  | Effect                                      |
| ----------------------- | ------------------------ | ------------------------------------------- |
| `OPCUA_PORT`            | `4840`                   | TCP port to bind                            |
| `OPCUA_HOSTNAME`        | `0.0.0.0`                | Bind address                                |
| `OPCUA_RESOURCE_PATH`   | `/UA/TestServer`         | URL resource path                           |
| `OPCUA_SERVER_NAME`     | `OPCUATestServer`        | Display name in `ServerStatus`               |

## Security

| Variable                | Default        | Effect                                                       |
| ----------------------- | -------------- | ------------------------------------------------------------ |
| `OPCUA_SECURITY_POLICIES` | `None`        | Comma-separated policy list — see [Policies and modes](../security/policies-and-modes.md) |
| `OPCUA_SECURITY_MODES`  | `None`         | Comma-separated mode list                                    |
| `OPCUA_AUTO_ACCEPT_CERTS` | `false`      | Auto-trust unknown client certs (TOFU)                       |

## Authentication

| Variable                | Default | Effect                                          |
| ----------------------- | ------- | ----------------------------------------------- |
| `OPCUA_ALLOW_ANONYMOUS` | `true`  | Accept Anonymous identity tokens                |
| `OPCUA_AUTH_USERS`      | `false` | Validate username/password from `users.json`    |
| `OPCUA_AUTH_CERTIFICATE` | `false` | Accept X.509 user-identity tokens              |

## Discovery / role

| Variable               | Default | Effect                                                   |
| ---------------------- | ------- | -------------------------------------------------------- |
| `OPCUA_IS_DISCOVERY`   | `false` | Run as a Discovery Server (no application address space) |

## Operation limits

| Variable                       | Default     | Effect                                          |
| ------------------------------ | ----------- | ----------------------------------------------- |
| `OPCUA_MAX_SESSIONS`           | `100`       | Max concurrent sessions                         |
| `OPCUA_MAX_NODES_PER_READ`     | `0` (∞)     | Max NodeIds in one Read request                 |
| `OPCUA_MAX_NODES_PER_WRITE`    | `0` (∞)     | Max NodeIds in one Write request                |
| `OPCUA_MAX_NODES_PER_BROWSE`   | `0` (∞)     | Max NodeIds in one Browse request               |

`opcua-no-security` sets `MaxNodesPerRead=5` and
`MaxNodesPerWrite=5` to exercise the limit error path.

## Feature toggles (address-space)

| Variable                  | Default | Disables                                  |
| ------------------------- | ------- | ----------------------------------------- |
| `OPCUA_ENABLE_HISTORICAL` | `true`  | Historical builder + 4 historized vars    |
| `OPCUA_ENABLE_EVENTS`     | `true`  | Events + alarms builder                   |
| `OPCUA_ENABLE_METHODS`    | `true`  | Methods builder (12 methods)              |
| `OPCUA_ENABLE_DYNAMIC`    | `true`  | Dynamic builder (13 time-varying vars)    |
| `OPCUA_ENABLE_STRUCTURES` | `true`  | Structures builder (objects, nested)      |
| `OPCUA_ENABLE_VIEWS`      | `true`  | Views builder                             |
| `OPCUA_ENABLE_SKS`        | `false` | Security Key Service builder              |

`OPCUA_ENABLE_SKS` is `true` only on the dedicated `opcua-sks`
service.

## Security Key Service (when enabled)

| Variable                          | Default                                              |
| --------------------------------- | ---------------------------------------------------- |
| `OPCUA_SKS_GROUP_ID`              | `test-group`                                         |
| `OPCUA_SKS_POLICY_URI`            | `http://opcfoundation.org/UA/SecurityPolicy#PubSub-Aes256-CTR` |
| `OPCUA_SKS_TOKEN_ID`              | `7`                                                  |
| `OPCUA_SKS_SIGNING_KEY_HEX`       | `01` × 32                                            |
| `OPCUA_SKS_ENCRYPTING_KEY_HEX`    | `02` × 32                                            |
| `OPCUA_SKS_KEY_NONCE_HEX`         | `03030303`                                           |
| `OPCUA_SKS_TIME_TO_NEXT_KEY_MS`   | `300000`                                             |
| `OPCUA_SKS_KEY_LIFETIME_MS`       | `600000`                                             |

## PubSub publisher (when running TestPublisher image)

| Variable                    | Default                                  |
| --------------------------- | ---------------------------------------- |
| `OPCUA_URL`                 | `opc.udp://opcua-pubsub-relay:4850`      |
| `OPCUA_PUBSUB_HOST_PORT`    | `14850`                                  |
| `OPCUA_NETWORK_INTERFACE`   | empty (all NICs)                          |
| `OPCUA_PUBLISHER_ID`        | `100`                                    |
| `OPCUA_WRITER_GROUP_ID`     | `1`                                      |
| `OPCUA_DATASET_WRITER_ID`   | `1`                                      |
| `OPCUA_DATASET_NAME`        | `Simple`                                 |
| `OPCUA_PUBLISH_INTERVAL_MS` | `500`                                    |
| `OPCUA_TICK_INTERVAL_MS`    | `250`                                    |
| `OPCUA_LOG_LEVEL`           | `Information`                            |

## Compose-only

These shape the **compose** invocation but aren't read by the
server image:

| Variable             | Default         | Effect                              |
| -------------------- | --------------- | ----------------------------------- |
| `OPCUA_SERVER_IMAGE` | (build locally) | Image to use in `docker-compose.ci.yml` |
| `FORCE_REGEN`        | (unset)         | Forces `certs-generator` to re-create certs |

## A complete `.env` for the suite

The suite itself doesn't ship an `.env` — settings live in
`docker-compose.yml` per service. But for a custom compose
override, you can centralise variables:

<!-- @code-block language="bash" label=".env.suite (custom)" -->
```bash
# Common
OPCUA_HOSTNAME=0.0.0.0
OPCUA_RESOURCE_PATH=/UA/TestServer

# Feature toggles (all on)
OPCUA_ENABLE_HISTORICAL=true
OPCUA_ENABLE_EVENTS=true
OPCUA_ENABLE_METHODS=true
OPCUA_ENABLE_DYNAMIC=true
OPCUA_ENABLE_STRUCTURES=true
OPCUA_ENABLE_VIEWS=true

# Op limits
OPCUA_MAX_SESSIONS=100
OPCUA_MAX_NODES_PER_READ=0
OPCUA_MAX_NODES_PER_WRITE=0
```
<!-- @endcode-block -->

## Per-server settings

Each service in `docker-compose.yml` declares its own
`environment:` block. For the canonical settings of each, see
`docker-compose.yml` in the repo or
[Classic RSA and ECC servers](../server-instances/classic-rsa-and-ecc.md).

## Where to read next

- [Ports and endpoints](./ports-and-endpoints.md) — the URL
  reference.
- [Troubleshooting](./troubleshooting.md) — common config
  mistakes.

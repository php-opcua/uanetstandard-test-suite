---
eyebrow: 'Docs · Server instances'
lede:    'Three services don''t fit the "classic OPC UA server with the test address space" mold: the discovery server, the Security Key Service, and the PubSub publisher. Each is the only target for its specific tests.'

see_also:
  - { href: './overview.md',                         meta: '4 min' }
  - { href: '../special-features/security-key-service.md', meta: '5 min' }
  - { href: '../special-features/pubsub-publisher.md',     meta: '5 min' }

prev: { label: 'Classic RSA and ECC servers',  href: './classic-rsa-and-ecc.md' }
next: { label: 'Security · Policies and modes', href: '../security/policies-and-modes.md' }
---

# Special-purpose servers

These don't expose the standard test address space. Each one
exists for a specific protocol feature you'd otherwise have to
mock.

## Discovery server (`opcua-discovery`, port 4844)

```text
endpoint:  opc.tcp://localhost:4844
            (note: no resource path)
policy:    None, Basic256Sha256
mode:      None, SignAndEncrypt
auth:      Anonymous
```

A dedicated OPC UA Discovery Server. **Not** a regular server —
it does not expose `TestServer` and has no application
address space.

### What it serves

| Service        | Use case                                            |
| -------------- | --------------------------------------------------- |
| `FindServers`  | List servers that have registered with discovery    |
| `GetEndpoints` | Get endpoints of this discovery server itself       |

### How tests use it

The classic test servers **register** with this discovery server
on start (via the discovery URL `opc.tcp://opcua-discovery:4844`
on the compose network).

A discovery test typically:

1. Connects to `opc.tcp://localhost:4844` anonymously.
2. Calls `FindServers()` with no filter.
3. Asserts the result includes the expected registered servers.
4. Optionally filters by `serverUri` or `serverCapabilities`.

Note the **lack of resource path** — discovery endpoints are
served at the bare URL, no `/UA/TestServer` suffix.

## Security Key Service (`opcua-sks`, port 4851)

```text
endpoint:  opc.tcp://localhost:4851/UA/TestServer
policy:    None
mode:      None
auth:      Anonymous
extra:     OPCUA_ENABLE_SKS=true
```

A classic OPC UA server with one extra feature: the
**`GetSecurityKeys` method** from OPC UA Part 14 §8.4.2. Used by
PubSub subscribers to obtain group keys.

### The method

NodeIds:

- Object: `ns=1;s=TestServer/SecurityKeyService`
- Method: `ns=1;s=TestServer/SecurityKeyService/GetSecurityKeys`

Signature:

<!-- @code-block language="text" label="signature" -->
```text
GetSecurityKeys(
    securityGroupId:    String,
    startingTokenId:    UInt32,
    requestedKeyCount:  UInt32,
) returns (
    securityPolicyUri:  String,
    firstTokenId:       UInt32,
    keys:               ByteString[],
    timeToNextKey:      Duration,
    keyLifetime:        Duration,
)
```
<!-- @endcode-block -->

### Default key layout

For policy `PubSub-Aes256-CTR`, each `keys[i]` ByteString is 68
bytes:

| Bytes | Contents                  | Default                 |
| ----- | ------------------------- | ----------------------- |
| 0-31  | HMAC-SHA256 signing key   | 32 × `0x01`             |
| 32-63 | AES-256 encrypting key    | 32 × `0x02`             |
| 64-67 | 4-byte key nonce          | `0x03 0x03 0x03 0x03`   |

All values overridable via `OPCUA_SKS_*` environment variables —
see [Security Key Service](../special-features/security-key-service.md).

### Limits — test-only

- Single hardcoded group (`OPCUA_SKS_GROUP_ID`, default
  `test-group`).
- Unknown `securityGroupId` returns `BadNotFound`.
- **No caller authentication** — anyone reachable on 4851 can
  fetch keys.
- **No rotation scheduling** — keys are static for the lifetime
  of the process.

Real-world SKS deployments do **all** of the above. This service
exists to give subscriber-side code a real round-trip target, not
to be a reference SKS.

## PubSub publisher (`opcua-pubsub`, UDP 14850 host)

```text
endpoint:  opc.udp://127.0.0.1:14850
codebase:  src/TestPublisher/ (separate)
transport: UADP over UDP
mode:      None (unsecured)
```

A UA-.NETStandard PubSub publisher. Emits a deterministic
`DataSet` every 500 ms over UDP UADP.

### DataSet shape

| Field       | Type     | Value                                  |
| ----------- | -------- | -------------------------------------- |
| `counter`   | UInt32   | Monotonic counter, starts at 0         |
| `timestamp` | DateTime | UTC publish time                       |
| `value`     | Double   | `sin(counter × π / 20)`                |

### Headers

| Header           | Default |
| ---------------- | ------- |
| `PublisherId`    | 100     |
| `WriterGroupId`  | 1       |
| `DataSetWriterId`| 1       |
| Publish interval | 500 ms  |

All defaults overridable via `OPCUA_*` env vars — see
[PubSub publisher](../special-features/pubsub-publisher.md).

### Why two services?

PubSub natively uses multicast — which doesn't traverse the
Docker Desktop VM boundary reliably across platforms. The
publisher sends **unicast** to a `socat` relay on a shared
compose bridge; the relay re-emits each packet to
`host.docker.internal:14850`, which Docker Desktop and Docker
Engine both bridge to the physical host.

```text
opcua-pubsub ── pubsub-net ──► opcua-pubsub-relay ── host.docker.internal:14850 ──► subscriber
```

Subscribers on the host listen on `127.0.0.1:14850` and don't
know the relay exists.

### How tests use it

A subscriber-side test:

1. Opens a UDP socket on `127.0.0.1:14850`.
2. Receives UADP NetworkMessages.
3. Decodes them per `OPCFoundation.NetStandard.Opc.Ua.PubSub`
   wire format.
4. Asserts the `counter` increments, the `value` is a sine
   wave, the timestamps are monotonic.

The published headers (`PublisherId`, `WriterGroupId`,
`DataSetWriterId`) are also part of the assertion surface — they
exercise the subscriber's demux logic.

## Where to read next

- [Security Key Service](../special-features/security-key-service.md) —
  the SKS in detail.
- [PubSub publisher](../special-features/pubsub-publisher.md) —
  the publisher in detail.

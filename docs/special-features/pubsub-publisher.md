---
eyebrow: 'Docs · Special features'
lede:    'A UA-.NETStandard PubSub publisher emitting UADP NetworkMessages over UDP every 500 ms. The test target for subscriber libraries that decode Part 14 traffic.'

see_also:
  - { href: './security-key-service.md',                  meta: '5 min' }
  - { href: '../server-instances/special-purpose.md',     meta: '5 min' }

prev: { label: 'Security Key Service',  href: './security-key-service.md' }
next: { label: 'GitHub Action',         href: '../ci-integration/github-action.md' }
---

# PubSub publisher

The `opcua-pubsub` service emits OPC UA Part 14 **UADP**
(OPC UA Binary over UDP) `NetworkMessages` every 500 ms. Built
on the UA-.NETStandard PubSub stack. Subscribers on the host
machine listen on `127.0.0.1:14850`.

## How to reach it

| Property        | Value                                  |
| --------------- | -------------------------------------- |
| Subscriber URL  | `opc.udp://127.0.0.1:14850`            |
| Transport       | UDP UADP                                |
| Security mode   | None                                    |
| Publishing rate | 500 ms (default)                        |

A single UDP socket — no OPC UA session, no handshake. The
subscriber opens a UDP listener and decodes incoming datagrams.

## Why two services

PubSub natively uses **UDP multicast**. Multicast doesn't
reliably traverse the Docker Desktop VM boundary across
Linux/macOS/Windows. To make the publisher work the same
everywhere, the suite splits it:

```text
opcua-pubsub  ── pubsub-net ──►  opcua-pubsub-relay  ── host.docker.internal:14850 ──►  subscriber
   (publisher)                       (socat sidecar)                                       (your test)
```

The publisher sends **unicast** to the relay (a stateless
`socat` forwarder) on a shared compose bridge. The relay
re-emits each packet to `host.docker.internal:14850`, which
Docker Desktop and Docker Engine both bridge to the physical
host's `127.0.0.1:14850`.

This works on:

- **Linux + Docker Engine** (bare metal, GitHub Actions runners)
- **Docker Desktop** on Linux, macOS, Windows

Subscribers don't know the relay exists — they listen on
`127.0.0.1:14850` directly.

## DataSet shape

Every NetworkMessage carries one DataSet with three fields:

| Field       | Type      | Value                                                                 |
| ----------- | --------- | --------------------------------------------------------------------- |
| `counter`   | UInt32    | Monotonic counter. Internal state starts at `0`, but the simulator increments before publishing — the **first wire value is `1`**, the second is `2`, and so on. |
| `timestamp` | DateTime  | UTC publish time                                                      |
| `value`     | Double    | `sin(counter × π / 20)` (range `[-1, 1]`)                             |

## NetworkMessage headers

| Header              | Default |
| ------------------- | ------- |
| `PublisherId`       | 100 (UInt16) |
| `WriterGroupId`     | 1 (UInt16) |
| `DataSetWriterId`   | 1 (UInt16) |
| Publishing interval | 500 ms  |

Headers are written into each UADP frame's chunk encoding —
your subscriber's demux uses them to route to the right
DataSetReader.

## Configuration via env vars

| Variable                    | Default                                                | Effect                              |
| --------------------------- | ------------------------------------------------------ | ----------------------------------- |
| `OPCUA_URL`                 | `opc.udp://opcua-pubsub-relay:4850`                    | Where the publisher unicasts        |
| `OPCUA_PUBSUB_HOST_PORT`    | `14850`                                                | Host-side UDP port                  |
| `OPCUA_NETWORK_INTERFACE`   | empty (all NICs)                                       | NIC to bind on (never pass `lo`)    |
| `OPCUA_PUBLISHER_ID`        | `100`                                                  | UInt16 publisher id                 |
| `OPCUA_WRITER_GROUP_ID`     | `1`                                                    | UInt16 writer group id              |
| `OPCUA_DATASET_WRITER_ID`   | `1`                                                    | UInt16 dataset writer id            |
| `OPCUA_DATASET_NAME`        | `Simple`                                               | DataSet name in metadata            |
| `OPCUA_PUBLISH_INTERVAL_MS` | `500`                                                  | How often a NetworkMessage is sent   |
| `OPCUA_TICK_INTERVAL_MS`    | `250`                                                  | Internal value-simulator tick rate  |
| `OPCUA_LOG_LEVEL`           | `Information`                                          | `Debug` \| `Information` \| `Warning` \| `Error` |

## Test patterns

### Receive first message

A subscriber bound to `127.0.0.1:14850`:

```text
socket = bind UDP on 0.0.0.0:14850
data, addr = socket.recv()
# Decode UADP NetworkMessage
```

Expected: a NetworkMessage with one DataSet, three fields,
PublisherId=100, WriterGroupId=1, DataSetWriterId=1.

### Counter increments

Receive 10 messages over ~5 seconds. The `counter` field is
strictly increasing, but **not necessarily by 1 per message**.
The simulator increments `_counter` every `OPCUA_TICK_INTERVAL_MS`
(default 250 ms) but the publisher emits a `NetworkMessage` only
every `OPCUA_PUBLISH_INTERVAL_MS` (default 500 ms). With the
defaults you should see `counter` advance by `2` between
consecutive frames. To get a strict +1 step, set both intervals
to the same value (e.g. `OPCUA_TICK_INTERVAL_MS=500`).

If you miss a message (UDP isn't reliable), `counter` jumps by
more than the expected step. That's by design — UDP losses are
part of the test surface.

### Value sequence

The `value` field follows `sin(counter × π / 20)`. So:

| `counter` | `value`              |
| --------- | -------------------- |
| 1         | `sin(π/20) ≈ 0.1564` |
| 5         | `sin(π/4) ≈ 0.7071`  |
| 10        | `sin(π/2) = 1.0`     |
| 20        | `sin(π) ≈ 0`         |

(`counter` is never `0` on the wire — see the note in the
DataSet shape table above.)

Verify the formula holds for any received message.

### Filter by PublisherId

A subscriber that exercises filtering:

```text
config.PublisherIdToAccept = 100
→ all messages pass (only one PublisherId here)

config.PublisherIdToAccept = 999
→ no messages pass
```

The publisher only emits with `PublisherId=100` unless
reconfigured.

### Multiple writers

The default config has one DataSetWriter. To test multi-writer
demux, fork and add a second `OPCUA_DATASET_WRITER_ID` in
`src/TestPublisher/Program.cs`.

## Decoding the wire

UADP frame structure (high-level):

```text
NetworkMessage Header
  PublisherId, WriterGroupId, ...
  DataSet Message Header (per DataSet)
    DataSetWriterId, Sequence number, Timestamp
    Field Encoding
      Field 0 (counter)  — UInt32, little-endian
      Field 1 (timestamp) — DateTime
      Field 2 (value)     — Double, IEEE-754
```

The exact byte-level wire format is defined in
[OPC UA Part 14 §7.2](https://reference.opcfoundation.org/Core/Part14/v105/docs/7.2).

UA-.NETStandard's `OPCFoundation.NetStandard.Opc.Ua.PubSub`
NuGet package implements this fully on the .NET side — what the
suite emits matches what that library expects to decode.

## Sequence numbers

The wire includes a per-message sequence number that increments
independently of the `counter` field:

```text
SequenceNumber: 0, 1, 2, 3, ...
```

Subscribers verify the sequence is contiguous (no skips) to
detect UDP drops. UDP packet loss rates depend heavily on the
host and network: on a quiet loopback it is normally
near-zero, but a busy CI host or a Docker Desktop VM can drop
several percent. Treat any observed rate as environmental
rather than a guaranteed bound.

## Notes

- The publisher is **unsecured** (mode None). For secured
  PubSub testing, pair with the
  [Security Key Service](./security-key-service.md) — full
  end-to-end secured PubSub is on the [roadmap](https://github.com/php-opcua/uanetstandard-test-suite/blob/master/ROADMAP.md).
- The publisher doesn't expose a UA server interface — there's
  no DataSet discovery beyond what's in the wire-level
  metadata.
- The relay sidecar (`opcua-pubsub-relay`) is **stateless** — it
  forwards every UDP packet it receives. No buffering, no
  retransmission.

## Where to read next

- [GitHub Action](../ci-integration/github-action.md) — invoke
  the publisher (or a subset) from CI.
- [Security Key Service](./security-key-service.md) — the
  complementary feature.

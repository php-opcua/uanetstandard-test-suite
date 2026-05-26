---
eyebrow: 'Docs · Runtime features'
lede:    'Two Method nodes that let a client tell the server to dial back via OPC UA Reverse Connect (Part 6 §7.1.2.3) — the test hook for the php-opcua/opcua-client-ext-reverse-connect package.'

see_also:
  - { href: './methods.md',                          meta: '4 min' }
  - { href: '../reference/environment-variables.md', meta: '3 min' }

prev: { label: 'File Transfer', href: './file-transfer.md' }
next: { label: 'Environment variables', href: '../reference/environment-variables.md' }
---

# Reverse Connect

Added in v1.4.0. The suite exposes a folder `TestServer/ReverseConnect`
with two Method nodes that let an ordinary client *trigger* the server
to initiate an outbound TCP connection and emit a ReverseHello (`RHE`)
frame per OPC UA Part 6 §7.1.2.3.

This is the test counterpart of the
[`php-opcua/opcua-client-ext-reverse-connect`](https://github.com/php-opcua/opcua-client-ext-reverse-connect)
package. Without it, end-to-end reverse-connect integration tests would
have to coordinate startup ordering (server up before client listens, or
the other way around) and depend on the UA-.NETStandard retry timer.
With these methods the PHP test is fully in control: open a listener,
call `StartReverseConnect`, accept the inbound RHE.

## Where they live

Folder NodeId: `ns=2;s=TestServer/ReverseConnect`

Method NodeIds:

- `ns=2;s=TestServer/ReverseConnect/StartReverseConnect`
- `ns=2;s=TestServer/ReverseConnect/StopReverseConnect`

The folder is only built when `OPCUA_ENABLE_REVERSE_CONNECT=true`
(default). Disable it on services where Reverse Connect is not
desired.

## StartReverseConnect

Equivalent to `Opc.Ua.Server.ReverseConnectServer.AddReverseConnection(new Uri($"opc.tcp://{host}:{port}"))`.

| Direction | Name     | Type       | Notes                                  |
| --------- | -------- | ---------- | -------------------------------------- |
| Input     | `host`   | String     | Target host (client's listener)        |
| Input     | `port`   | UInt16     | Target TCP port                        |
| Output    | `status` | StatusCode | `Good`, `BadInvalidArgument`, or `BadInternalError` |

Notes:

- `host` empty or `port == 0` → `BadInvalidArgument` (no call into the
  reverse-connect manager).
- Calling `StartReverseConnect` twice with the same `(host, port)` is
  idempotent and returns `Good` the second time. UA-.NETStandard's
  `AddReverseConnection` throws `ArgumentException` for a duplicate; the
  method handler catches it and returns `Good` so the test does not need
  to track local state.
- UA-.NETStandard begins attempting the outbound connection within ~1
  second of the call returning on a healthy host.

## StopReverseConnect

Equivalent to `Opc.Ua.Server.ReverseConnectServer.RemoveReverseConnection(new Uri($"opc.tcp://{host}:{port}"))`.

| Direction | Name     | Type       | Notes                                   |
| --------- | -------- | ---------- | --------------------------------------- |
| Input     | `host`   | String     | Same target that was passed to Start    |
| Input     | `port`   | UInt16     | Same target that was passed to Start    |
| Output    | `status` | StatusCode | `Good` if removed, `BadNotFound` otherwise |

Use it at the end of an integration test to keep the server clean for
the next iteration.

## Docker networking note

The listener typically runs on the docker host while the server runs in
a container. To make `host.docker.internal` resolvable from inside the
container, the `opcua-no-security` service in `docker-compose.yml`
includes:

```yaml
extra_hosts:
  - "host.docker.internal:host-gateway"
```

so the integration test can pass `host.docker.internal` as the `host`
argument of `StartReverseConnect`.

## Disabling

| Variable                       | Default | Effect when `false`                          |
| ------------------------------ | ------- | -------------------------------------------- |
| `OPCUA_ENABLE_REVERSE_CONNECT` | `true`  | `TestServer/ReverseConnect` folder not built |

---
eyebrow: 'Docs · Getting started'
lede:    'A ready-to-use OPC UA suite for integration-testing OPC UA client libraries. 10 classic servers, an SKS, a PubSub publisher — all on top of the OPC Foundation reference implementation.'

see_also:
  - { href: './getting-started/installation.md', meta: '3 min' }
  - { href: './getting-started/quick-start.md',  meta: '3 min' }
  - { href: './server-instances/overview.md',    meta: '5 min' }

next: { label: 'Installation', href: './getting-started/installation.md' }
---

# Overview

This is a Docker-Compose suite of OPC UA test servers built for one
job: **integration testing of OPC UA client libraries**. Whatever
language you're writing the client in — Rust, Python, PHP, Go,
Java, C#, anything that speaks OPC UA — you point it at this suite
and exercise every protocol corner without having to set up a PLC.

## What runs

| Port | Service                  | Purpose                                            |
| ---- | ------------------------ | -------------------------------------------------- |
| 4840 | `opcua-no-security`      | Plain connectivity, anonymous                      |
| 4841 | `opcua-userpass`         | Encrypted + username/password                      |
| 4842 | `opcua-certificate`      | X.509 client-cert authentication                   |
| 4843 | `opcua-all-security`     | Every RSA policy + every mode + every auth        |
| 4844 | `opcua-discovery`        | OPC UA Discovery Server                            |
| 4845 | `opcua-auto-accept`      | Auto-trust mode for quick setup                    |
| 4846 | `opcua-sign-only`        | Sign-only (no encryption)                          |
| 4847 | `opcua-legacy`           | Deprecated `Basic128Rsa15` / `Basic256`            |
| 4848 | `opcua-ecc-nist`         | ECC NIST P-256 / P-384                             |
| 4849 | `opcua-ecc-brainpool`    | ECC Brainpool P-256r1 / P-384r1                    |
| 4851 | `opcua-sks`              | Part 14 §8.4.2 `GetSecurityKeys` RPC               |
| 14850 (UDP) | `opcua-pubsub` + relay | UADP NetworkMessages on UDP                |

All 10 classic instances run the **same code** with different
environment variables — same address space, ~300 nodes, identical
behaviour. Only the security / authentication wrapping differs.

## Why it exists

The original `php-opcua/opcua-test-suite` ran on Node.js +
`node-opcua`. This suite reimplements it on the **OPC Foundation
UA-.NETStandard library** — the reference implementation,
maintained by the same organisation that publishes the OPC UA
specification.

For an integration-test counterpart this matters: protocol
behaviour, encoding, and security are as close to the spec as
possible. A test passing against this suite is a strong signal
your client will interop with real-world servers built on the
same stack (Siemens, Beckhoff, KEPServer, …).

## Address space — what's in it

Each of the 10 classic servers exposes the same ~200-node
custom address space (plus the standard `ns=0` framework nodes
that every UA-.NETStandard server ships):

- 21 scalar data types × {RW, RO} variants
- 20 RW arrays + 14 empty arrays + 6 RO arrays
- 3 multi-dimensional matrices (2D and 3D)
- 12 callable methods (arithmetic, strings, arrays, async, failures)
- 13 dynamic variables (counters, waves, random)
- 3 periodic standard `BaseEventState` event timers (no custom event types)
- 3 alarms (exclusive limit, non-exclusive limit, off-normal)
- 4 historical variables (1-second recording, 10 000-sample buffer)
- Structured objects (nested up to 10 levels)
- 2 extension objects (binary-encoded ExtensionObject)
- 50 access-control variables across every type/access combination
- 4 OPC UA Views for filtered browsing

Detailed breakdowns in [Address space · Overview](./address-space/overview.md)
and the **Data features** / **Runtime features** sections.

## How tests typically use it

A typical CI-integration pattern:

1. Start the suite (`docker compose up -d` or the GitHub Action).
2. Run your client's integration suite against `localhost:4840-4849`.
3. Tear down.

For GitHub Actions this is one step:

<!-- @code-block language="text" label=".github/workflows/test.yml" -->
```text
# Pin to a tag (e.g. @v1.5.0) once you've verified one exists for
# your fork; @master tracks the bleeding edge. There is no
# guarantee a v1.5.0 tag exists in upstream — check
# https://github.com/php-opcua/uanetstandard-test-suite/tags
- uses: php-opcua/uanetstandard-test-suite@master

- run: cargo test  # or pytest, npm test, dotnet test, …
```
<!-- @endcode-block -->

See [CI integration · GitHub Action](./ci-integration/github-action.md).

## Tools that test against this suite

The same suite powers integration tests across several client
libraries:

| Library                                  | Language | Coverage                                       |
| ---------------------------------------- | -------- | ---------------------------------------------- |
| [`php-opcua/opcua-client`](https://github.com/php-opcua/opcua-client)               | PHP      | All 10 servers + SKS                           |
| [`php-opcua/opcua-session-manager`](https://github.com/php-opcua/opcua-session-manager)   | PHP      | Daemon-side IPC + classic servers              |
| [`php-opcua/opcua-client-ext-pubsub`](https://github.com/php-opcua/opcua-client-ext-pubsub) | PHP      | PubSub publisher + SKS                         |

…and any other client that exercises a CI matrix can plug in the
same way.

## Forkable

The codebase is organised as a set of focused builders — one C#
class per feature group (data types, methods, events, etc.).
Adding your own variables, methods, or even whole new server
flavours is a small change.

See [Customization · Forking and adding nodes](./customization/forking-and-adding-nodes.md).

## Where to read next

- [Installation](./getting-started/installation.md) — get it running.
- [Quick start](./getting-started/quick-start.md) — first commands.
- [Server instances · Overview](./server-instances/overview.md) —
  pick the right server for your test.

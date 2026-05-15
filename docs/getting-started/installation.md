---
eyebrow: 'Docs · Getting started'
lede:    'Bring up the whole suite with one command. Prerequisites, what happens on first start, where certificates land.'

see_also:
  - { href: './quick-start.md',                  meta: '3 min' }
  - { href: './first-connection.md',             meta: '4 min' }
  - { href: '../reference/troubleshooting.md',   meta: '5 min' }

prev: { label: 'Overview',     href: '../overview.md' }
next: { label: 'Quick start',  href: './quick-start.md' }
---

# Installation

## Prerequisites

- **Docker** with Compose v2 (`docker compose ...`).
- **Free ports** on the host: 4840-4849 (TCP), 4851 (TCP), 14850
  (UDP). The discovery server uses 4844 with **no** resource path.
- ~500 MB of disk for the image + generated certs.

No language toolchain needed on the host — everything runs in
containers.

## One command

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose up -d
```
<!-- @endcode-block -->

That brings up:

- 1 certificate-generator init container.
- 10 classic test servers (4840-4849).
- 1 Security Key Service (4851).
- 1 PubSub publisher + 1 socat relay (UDP 14850 on the host).

First start takes 30-60 seconds — most of that is the cert
generator producing the CA, server, client, self-signed, and
expired certificate sets. Subsequent starts skip cert generation.

## What lands on disk

The compose file mounts two host directories:

| Host path | What's in it                                            | Mount mode |
| --------- | ------------------------------------------------------- | ---------- |
| `./certs/` | Generated certificates (CA, server, client, self-signed, expired, trust dirs) | rw |
| `./config/` | `users.json` for username/password auth                 | ro         |

After first start you'll see, on the host:

<!-- @code-block language="text" label="filesystem" -->
```text
certs/
├── ca/
│   ├── ca-cert.pem
│   ├── ca-key.pem
│   └── ca-cert.der
├── server/
│   ├── cert.pem  cert.der  key.pem  key.der  server.pfx
├── client/
│   ├── cert.pem  cert.der  key.pem  key.der  client.pfx
├── self-signed/
├── expired/
├── trusted/
└── pki/
    ├── trusted/   ├── issuers/   └── rejected/

config/
└── users.json
```
<!-- @endcode-block -->

The `client/*` and `self-signed/*` files are what your tests
present as a client identity — see
[Authentication · Certificate authentication](../authentication/certificate-authentication.md).

## Verify it's up

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose ps
```
<!-- @endcode-block -->

You should see ~13 services with state `running` (one of them,
`certs-generator`, may be `completed` — that's expected, it
exits after generating).

Quick TCP probe:

<!-- @code-block language="bash" label="terminal — probe" -->
```bash
for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849 4851; do
  nc -z localhost $port && echo "port $port: OK" || echo "port $port: FAIL"
done
```
<!-- @endcode-block -->

If any port fails, see [Troubleshooting](../reference/troubleshooting.md).

## Stop

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose down
```
<!-- @endcode-block -->

Containers stop and are removed. The `certs/` directory stays —
the next start reuses them.

For a clean slate including certificates:

<!-- @code-block language="bash" label="terminal — full reset" -->
```bash
docker compose down
rm -rf ./certs
docker compose up -d
```
<!-- @endcode-block -->

## Rebuild after source changes

If you've forked the suite and edited C# under `src/TestServer/`:

<!-- @code-block language="bash" label="terminal — rebuild" -->
```bash
docker compose build
docker compose up -d
```
<!-- @endcode-block -->

## Image source

The default `docker-compose.yml` builds from the local
`Dockerfile`. For CI where you don't want to build, point at the
published image:

<!-- @code-block language="bash" label=".env (CI)" -->
```bash
OPCUA_SERVER_IMAGE=ghcr.io/php-opcua/uanetstandard-test-suite:latest
```
<!-- @endcode-block -->

The CI compose override (`docker-compose.ci.yml`) does this
plus disables auto-restart — see
[CI integration · Docker Compose and other CI](../ci-integration/docker-compose-and-other-ci.md).

## Resource usage

The 11 server processes combined use roughly:

| Resource | Idle | Under load (typical CI run) |
| -------- | ---- | --------------------------- |
| RAM      | 350-500 MB | 600-800 MB             |
| CPU      | < 1% | 5-15%                       |
| Disk (certs only) | ~150 KB | —                  |

Comfortable on any developer laptop or CI runner.

## Where to read next

- [Quick start](./quick-start.md) — first connection and basic
  commands.
- [First connection](./first-connection.md) — connect from your
  client library.

---
eyebrow: 'Docs · CI integration'
lede:    'The reusable GitHub Composite Action that brings up the full suite in one step. Inputs, outputs, certificate handling, version pinning.'

see_also:
  - { href: './docker-compose-and-other-ci.md',  meta: '4 min' }
  - { href: '../reference/troubleshooting.md',   meta: '4 min' }

prev: { label: 'PubSub publisher',  href: '../special-features/pubsub-publisher.md' }
next: { label: 'Docker Compose and other CI', href: './docker-compose-and-other-ci.md' }
---

# GitHub Action

The suite ships a **GitHub Composite Action**. Add a single step
to your workflow and all selected servers come up on
`localhost`.

## Minimal example

<!-- @code-block language="text" label=".github/workflows/test.yml" -->
```text
name: Integration tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: php-opcua/uanetstandard-test-suite@v1.4.0

      - run: cargo test     # or pytest, npm test, dotnet test, …
```
<!-- @endcode-block -->

When the `uses:` step completes, all selected servers (default:
all) are accepting connections.

## Inputs

| Input          | Default | Effect                                                |
| -------------- | ------- | ----------------------------------------------------- |
| `servers`      | `all`   | Comma-separated list of services to start              |
| `wait-timeout` | `120`   | Seconds passed to `docker compose up --wait --wait-timeout`. Only the `opcua-no-security` service has a healthcheck — the other services are considered "started" as soon as the container is running. |

Valid `servers` values:

`no-security`, `userpass`, `certificate`, `all-security`,
`discovery`, `auto-accept`, `sign-only`, `legacy`,
`ecc-nist`, `ecc-brainpool`, `sks`, `pubsub`, `all`.

## Outputs

| Output      | Description                                              |
| ----------- | -------------------------------------------------------- |
| `certs-dir` | Absolute path to the generated certificates directory     |

Inside `certs-dir`:

```text
ca/                 # CA cert + key
server/             # Server cert + key
client/             # Trusted client cert
self-signed/        # Untrusted (for rejection tests)
expired/            # Expired (for expiry tests)
trusted/            # Trust dir layout
```

See [Certificates](../security/certificates.md) for the full
file map.

## Selecting a subset of servers

Smaller test runs save CI time:

<!-- @code-block language="text" label="subset example" -->
```text
- uses: php-opcua/uanetstandard-test-suite@v1.4.0
  with:
    servers: 'no-security,userpass'
```
<!-- @endcode-block -->

Common subsets:

| Purpose                             | `servers`                                          |
| ----------------------------------- | -------------------------------------------------- |
| Basic connectivity tests             | `no-security`                                      |
| Auth + crypto only                   | `userpass,certificate,all-security`                |
| ECC only                             | `ecc-nist,ecc-brainpool`                           |
| PubSub subscriber tests              | `pubsub`                                           |
| PubSub + SKS                         | `pubsub,sks`                                       |
| Everything                           | `all` (default)                                    |

## Using certificates in tests

Pass `certs-dir` to your test command:

<!-- @code-block language="text" label="certificates passthrough" -->
```text
- id: opcua
  uses: php-opcua/uanetstandard-test-suite@v1.4.0

- run: dotnet test
  env:
    OPCUA_CERTS_DIR:         ${{ steps.opcua.outputs.certs-dir }}
    OPCUA_CLIENT_CERT:       ${{ steps.opcua.outputs.certs-dir }}/client/cert.pem
    OPCUA_CLIENT_KEY:        ${{ steps.opcua.outputs.certs-dir }}/client/key.pem
    OPCUA_CA_CERT:           ${{ steps.opcua.outputs.certs-dir }}/ca/ca-cert.pem
    OPCUA_UNTRUSTED_CERT:    ${{ steps.opcua.outputs.certs-dir }}/self-signed/cert.pem
    OPCUA_EXPIRED_CERT:      ${{ steps.opcua.outputs.certs-dir }}/expired/cert.pem
```
<!-- @endcode-block -->

Your tests read those env vars and present the appropriate cert
to each server.

## Version pinning

| Form                                                           | Recommendation                          |
| -------------------------------------------------------------- | --------------------------------------- |
| `@v1.4.0`                                                      | **Recommended for stability**           |
| `@master`                                                      | Bleeding edge — may break               |
| `@<sha>` (full git SHA)                                        | Maximum reproducibility (CI provenance) |

Pinning to a tag means your tests don't break when the suite
releases changes — the trade-off is you need to manually bump
the version to pick up new servers or fixes.

## What the Action does internally

The composite action runs four steps (see `action.yml`):

1. `actions/checkout` of `php-opcua/uanetstandard-test-suite`
   into `.uanetstandard-test-suite/`.
2. Builds `--scale <svc>=0` arguments for every service not in
   the `servers` input (or no scale args when `servers: all`).
3. Runs `docker compose up -d --build --wait --wait-timeout <input>`
   in `.uanetstandard-test-suite/` with the computed scale args.
   The action does **not** layer `docker-compose.ci.yml` — the
   base file's `restart: unless-stopped` policies and the
   `opcua-no-security` healthcheck apply.
4. Verifies `certs/ca` exists, `chmod -R a+r certs/`, and exposes
   the absolute path via the `certs-dir` output.

Note: `--wait` only blocks until `docker compose` considers each
service "started". Of the 12 services, only `opcua-no-security`
has a healthcheck declared, so `--wait` returns once that one
container reports healthy and the other 11 containers are simply
"running". If you need to be sure a specific other server is
ready, poll its TCP port after the action completes.

See [`action.yml`](https://github.com/php-opcua/uanetstandard-test-suite/blob/master/action.yml)
in the repo for the full file.

## End-to-end example

A realistic workflow that runs three test layers:

<!-- @code-block language="text" label=".github/workflows/test.yml" -->
```text
name: Tests
on: [push, pull_request]

jobs:
  unit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: shivammathur/setup-php@v2
        with: { php-version: '8.4' }
      - run: composer install
      - run: vendor/bin/pest --exclude=Integration

  integration:
    runs-on: ubuntu-latest
    needs: unit
    steps:
      - uses: actions/checkout@v4
      - uses: shivammathur/setup-php@v2
        with: { php-version: '8.4' }

      - id: opcua
        uses: php-opcua/uanetstandard-test-suite@v1.4.0
        with:
          servers: 'no-security,userpass,certificate,all-security,ecc-nist'

      - run: composer install
      - run: vendor/bin/pest tests/Integration
        env:
          OPCUA_CERTS_DIR: ${{ steps.opcua.outputs.certs-dir }}

  ecc-integration:
    runs-on: ubuntu-latest
    needs: unit
    steps:
      - uses: actions/checkout@v4
      - uses: shivammathur/setup-php@v2
        with: { php-version: '8.4' }

      - id: opcua
        uses: php-opcua/uanetstandard-test-suite@v1.4.0
        with:
          servers: 'ecc-nist,ecc-brainpool'

      - run: composer install
      - run: vendor/bin/pest tests/EccIntegration
        env:
          OPCUA_CERTS_DIR: ${{ steps.opcua.outputs.certs-dir }}
```
<!-- @endcode-block -->

Three jobs — unit (no servers), integration (RSA servers), and
ECC integration (ECC servers only). Each gets its own set of
running servers.

## Resource limits on Actions runners

Standard GitHub-hosted runners have:

- 2-4 vCPU
- 7-16 GB RAM
- ~14 GB disk

The full suite (10 classic + SKS + PubSub) uses ~600 MB RAM and
modest CPU. Comfortable on every tier.

## Common failures

| Symptom                              | Cause / fix                                              |
| ------------------------------------ | -------------------------------------------------------- |
| Step exits with timeout                | A service didn't start in `wait-timeout` seconds. Increase to 180. |
| `Bad_CertificateHostNameInvalid`     | Connecting to a Docker service name instead of `localhost`. |
| Port already in use                  | Another job on the same runner is using 4840-4849. Use job isolation. |
| Cert dir empty                       | The `certs-generator` container failed — check the action logs. |

## Where to read next

- [Docker Compose and other CI](./docker-compose-and-other-ci.md) —
  for GitLab, Jenkins, etc.
- [Troubleshooting](../reference/troubleshooting.md) — common
  CI failures and fixes.

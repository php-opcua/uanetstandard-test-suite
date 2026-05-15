---
eyebrow: 'Docs · Customization'
lede:    'Add a new test-server instance with custom security, auth, or feature flags. Compose recipe, certificate SAN update, and verification steps.'

see_also:
  - { href: './forking-and-adding-nodes.md',          meta: '6 min' }
  - { href: '../reference/environment-variables.md',  meta: '3 min' }

prev: { label: 'Forking and adding nodes', href: './forking-and-adding-nodes.md' }
next: { label: 'Simulation recipes',       href: './simulation-recipes.md' }
---

# Adding server instances

The 10 classic servers all run from the **same Docker image**,
shaped by environment variables. Adding an 11th is a small
`docker-compose.yml` edit plus (optionally) an update to the
cert generator.

## A new server — minimal

Add to `docker-compose.yml`:

<!-- @code-block language="text" label="docker-compose.yml" -->
```text
opcua-my-scenario:
  build: .
  ports:
    - "4852:4852"
  volumes:
    - ./certs:/app/certs
    - ./config:/app/config:ro
  environment:
    OPCUA_PORT:              "4852"
    OPCUA_SERVER_NAME:       "MyScenarioServer"
    OPCUA_HOSTNAME:          "0.0.0.0"
    OPCUA_RESOURCE_PATH:     "/UA/TestServer"
    OPCUA_SECURITY_POLICIES: "Basic256Sha256"
    OPCUA_SECURITY_MODES:    "SignAndEncrypt"
    OPCUA_ALLOW_ANONYMOUS:   "false"
    OPCUA_AUTH_USERS:        "true"
    OPCUA_AUTH_CERTIFICATE:  "false"
  depends_on:
    certs-generator:
      condition: service_completed_successfully
  restart: unless-stopped
```
<!-- @endcode-block -->

Restart:

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose up -d
```
<!-- @endcode-block -->

The new server comes up at
`opc.tcp://localhost:4852/UA/TestServer`.

## Adjusting features

Disable feature groups for a leaner server:

<!-- @code-block language="text" label="lean config" -->
```text
environment:
  OPCUA_PORT:                 "4852"
  # ...
  OPCUA_ENABLE_HISTORICAL:    "false"   # no historical buffer
  OPCUA_ENABLE_EVENTS:        "false"   # no events / alarms
  OPCUA_ENABLE_METHODS:       "false"   # no methods
  OPCUA_ENABLE_DYNAMIC:       "false"   # no timers
  OPCUA_ENABLE_STRUCTURES:    "false"   # no structures
  OPCUA_ENABLE_VIEWS:         "false"   # no views
```
<!-- @endcode-block -->

Useful when you want a "pure data types only" server, or when
RAM constraints on the runner matter.

## Custom auth combinations

A few useful presets:

### Cert-only with auto-accept

```text
OPCUA_ALLOW_ANONYMOUS:   "false"
OPCUA_AUTH_USERS:        "false"
OPCUA_AUTH_CERTIFICATE:  "true"
OPCUA_AUTO_ACCEPT_CERTS: "true"
```

### Username-only, no encryption

```text
OPCUA_SECURITY_POLICIES: "None"
OPCUA_SECURITY_MODES:    "None"
OPCUA_AUTH_USERS:        "true"
OPCUA_ALLOW_ANONYMOUS:   "false"
```

This is **bad practice** in production but valid for testing —
some old clients try this combination.

### Strict cert validation, no anon

```text
OPCUA_SECURITY_POLICIES: "Basic256Sha256"
OPCUA_SECURITY_MODES:    "SignAndEncrypt"
OPCUA_AUTH_CERTIFICATE:  "true"
OPCUA_AUTO_ACCEPT_CERTS: "false"
OPCUA_ALLOW_ANONYMOUS:   "false"
OPCUA_AUTH_USERS:        "false"
```

Mirrors a paranoid production server.

## Updating the SAN

If your new server's hostname is **inside** the Docker network
(other containers connect to it by service name), the server
cert needs a matching SAN entry. Edit
`scripts/generate-certs.sh`:

<!-- @code-block language="text" label="generate-certs.sh — SAN update" -->
```text
[alt_names]
DNS.1 = localhost
DNS.2 = opcua-no-security
DNS.3 = opcua-userpass
DNS.4 = opcua-certificate
...
DNS.11 = opcua-my-scenario   # <— add your hostname
IP.1 = 127.0.0.1
IP.2 = 0.0.0.0
```
<!-- @endcode-block -->

Then regenerate:

<!-- @code-block language="bash" label="terminal" -->
```bash
rm -rf ./certs
docker compose up -d
```
<!-- @endcode-block -->

For host-side connections (your test on the host → `localhost:4852`),
the existing `localhost` SAN entry is sufficient. The update is
only needed if **other containers** in the compose network
connect to your new service by name.

## Adding it to the GitHub Action

If you fork the action.yml, add an option to the `servers`
input:

<!-- @code-block language="text" label="action.yml snippet" -->
```text
# In the case statement that maps server-name → docker service:
"my-scenario") echo "opcua-my-scenario" ;;
```
<!-- @endcode-block -->

Then your workflow:

<!-- @code-block language="text" label="workflow" -->
```text
- uses: ./.github/actions/uanetstandard-test-suite
  with:
    servers: 'no-security,my-scenario'
```
<!-- @endcode-block -->

## Adding it to docker-compose.ci.yml

The CI override needs a matching entry so the ci-mode (no
auto-restart, no healthcheck) applies:

<!-- @code-block language="text" label="docker-compose.ci.yml" -->
```text
opcua-my-scenario:
  image: ${OPCUA_SERVER_IMAGE}
  restart: "no"
  healthcheck:
    disable: true
```
<!-- @endcode-block -->

## Verifying

After `docker compose up -d`, check:

<!-- @code-block language="bash" label="terminal — verify" -->
```bash
docker compose ps opcua-my-scenario
nc -z localhost 4852 && echo "OK"
docker compose logs --tail=20 opcua-my-scenario
```
<!-- @endcode-block -->

Look for:

```text
[OPCUA] Server started on port 4852
[OPCUA] Endpoints:
  opc.tcp://0.0.0.0:4852/UA/TestServer  Basic256Sha256/SignAndEncrypt
```

Then test from your client:

<!-- @code-block language="bash" label="terminal — test" -->
```bash
opcua-cli get-endpoints opc.tcp://localhost:4852/UA/TestServer
```
<!-- @endcode-block -->

Should return the endpoint descriptions for the policy/mode you
configured.

## When to add a server vs. fork a builder

| You want to test…                              | Pick                                          |
| ---------------------------------------------- | --------------------------------------------- |
| A different security/auth combination          | New server instance                            |
| A different address space                       | Fork a builder                                |
| A new method or variable on the same address space | Fork a builder                            |
| Both                                            | Fork a builder + add an instance using it     |

## Where to read next

- [Simulation recipes](./simulation-recipes.md) — example
  builders.
- [Reference · Environment variables](../reference/environment-variables.md) —
  the full `OPCUA_*` list.

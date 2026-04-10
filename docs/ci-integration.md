# CI Integration Guide

How to use the UA-.NETStandard Test Server Suite in your project's CI/CD pipeline.

## GitHub Actions (Recommended)

This repository is a reusable **GitHub Composite Action**. Add one step to your workflow and all 10 OPC UA servers are ready on `localhost:4840-4849`.

### Minimal Example

```yaml
name: Integration Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: php-opcua/uanetstandard-test-suite@v1.1.0

      - run: cargo test  # or npm test, pytest, dotnet test, etc.
```

That's it. When the `uses:` step completes, all servers are up and accepting connections.

### Full Example with Options

```yaml
name: Integration Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Start OPC UA test servers
        id: opcua
        uses: php-opcua/uanetstandard-test-suite@v1.1.0
        with:
          # Which servers to start (default: all)
          servers: 'no-security,userpass,certificate'
          # Timeout waiting for servers (default: 120s)
          wait-timeout: '90'

      - name: Run tests
        env:
          OPCUA_TEST_ENDPOINT: opc.tcp://localhost:4840/UA/TestServer
          OPCUA_CERTS_DIR: ${{ steps.opcua.outputs.certs-dir }}
        run: dotnet test

      - name: Use client certificates
        run: |
          # Certificates are available directly on the filesystem
          ls ${{ steps.opcua.outputs.certs-dir }}/client/
          # Use certs/client/cert.pem and certs/client/key.pem
          # for certificate-based authentication tests
```

### Action Inputs

| Input | Default | Description |
|---|---|---|
| `servers` | `all` | Comma-separated list of servers to start. Options: `no-security`, `userpass`, `certificate`, `all-security`, `discovery`, `auto-accept`, `sign-only`, `legacy`, `ecc-nist`, `ecc-brainpool` |
| `wait-timeout` | `120` | Seconds to wait for all servers to be ready before failing |

### Action Outputs

| Output | Description |
|---|---|
| `certs-dir` | Absolute path to certificates on the runner. Contains `ca/`, `server/`, `client/`, `self-signed/`, `expired/` subdirectories |

### Server Selection

Start only the servers you need to save CI time:

```yaml
# Only no-security for basic tests
- uses: php-opcua/uanetstandard-test-suite@v1.1.0
  with:
    servers: 'no-security'

# Security tests only
- uses: php-opcua/uanetstandard-test-suite@v1.1.0
  with:
    servers: 'userpass,certificate,all-security'

# All servers (default)
- uses: php-opcua/uanetstandard-test-suite@v1.1.0
```

### Available Servers and Ports

| Server name (for `servers` input) | Port | Endpoint |
|---|---|---|
| `no-security` | 4840 | `opc.tcp://localhost:4840/UA/TestServer` |
| `userpass` | 4841 | `opc.tcp://localhost:4841/UA/TestServer` |
| `certificate` | 4842 | `opc.tcp://localhost:4842/UA/TestServer` |
| `all-security` | 4843 | `opc.tcp://localhost:4843/UA/TestServer` |
| `discovery` | 4844 | `opc.tcp://localhost:4844` |
| `auto-accept` | 4845 | `opc.tcp://localhost:4845/UA/TestServer` |
| `sign-only` | 4846 | `opc.tcp://localhost:4846/UA/TestServer` |
| `legacy` | 4847 | `opc.tcp://localhost:4847/UA/TestServer` |
| `ecc-nist` | 4848 | `opc.tcp://localhost:4848/UA/TestServer` |
| `ecc-brainpool` | 4849 | `opc.tcp://localhost:4849/UA/TestServer` |

### Using Certificates in Tests

The action makes all generated certificates available on the runner filesystem via the `certs-dir` output:

```yaml
- id: opcua
  uses: php-opcua/uanetstandard-test-suite@v1.1.0

- run: |
    # Trusted client certificate (signed by CA)
    export CLIENT_CERT="${{ steps.opcua.outputs.certs-dir }}/client/cert.pem"
    export CLIENT_KEY="${{ steps.opcua.outputs.certs-dir }}/client/key.pem"

    # CA certificate (for validating server cert)
    export CA_CERT="${{ steps.opcua.outputs.certs-dir }}/ca/ca-cert.pem"

    # Untrusted certificate (for rejection tests)
    export UNTRUSTED_CERT="${{ steps.opcua.outputs.certs-dir }}/self-signed/cert.pem"

    # Expired certificate
    export EXPIRED_CERT="${{ steps.opcua.outputs.certs-dir }}/expired/cert.pem"

    dotnet test
```

### Version Pinning

```yaml
# Pin to a specific release (recommended for stability)
- uses: php-opcua/uanetstandard-test-suite@v1.1.0

# Use latest from main branch
- uses: php-opcua/uanetstandard-test-suite@master

# Pin to a specific commit
- uses: php-opcua/uanetstandard-test-suite@sha-abc1234
```

---

## Docker Compose (Non-GitHub CI)

For GitLab CI, Jenkins, CircleCI, or local testing, use `docker-compose.ci.yml` directly:

```bash
# Set the image
export OPCUA_SERVER_IMAGE=ghcr.io/php-opcua/uanetstandard-test-suite:latest

# Pull and start
docker pull "$OPCUA_SERVER_IMAGE"
docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d

# Wait for servers
for port in 4840 4841 4842 4843 4844 4845 4846 4847 4848 4849; do
  while ! nc -z localhost $port 2>/dev/null; do sleep 1; done
  echo "Port $port ready"
done

# Certificates are available at ./certs/
export OPCUA_CERTS_DIR=./certs

# Run your tests
your-test-command

# Cleanup
docker compose -f docker-compose.yml -f docker-compose.ci.yml down
```

### GitLab CI Example

```yaml
integration-tests:
  image: docker:24
  services:
    - docker:24-dind
  variables:
    OPCUA_SERVER_IMAGE: ghcr.io/php-opcua/uanetstandard-test-suite:latest
  before_script:
    - apk add --no-cache docker-compose
    - docker pull "$OPCUA_SERVER_IMAGE"
    - docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
    - sleep 30  # wait for servers
  script:
    - your-test-command
  after_script:
    - docker compose -f docker-compose.yml -f docker-compose.ci.yml down
```

---

## Local Development

For local testing without Docker image registry:

```bash
# Clone and start (builds from source)
git clone https://github.com/php-opcua/uanetstandard-test-suite.git
cd uanetstandard-test-suite
docker compose up -d

# Ports 4840-4849 are now available on localhost
# Certificates are at ./certs/
```

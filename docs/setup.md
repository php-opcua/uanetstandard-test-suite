# Setup & Installation

## Prerequisites

- Docker and Docker Compose (v2+)
- Ports 4840-4849 available on the host

## Starting the Servers

```bash
# Build and start all 10 servers in detached mode
docker compose up -d

# First run takes longer: the certs-generator container creates
# all certificates before servers start (via depends_on)
```

## Verifying

```bash
# Check all services are running
docker compose ps

# View logs for a specific server
docker compose logs -f opcua-no-security

# View logs for all servers
docker compose logs -f
```

## Stopping

```bash
# Stop all servers
docker compose down

# Stop and remove generated certificates
docker compose down
rm -rf ./certs
```

## Rebuilding

After modifying source code in `src/TestServer/`:

```bash
docker compose build
docker compose up -d
```

## Certificate Regeneration

Certificates are auto-generated on first startup by the `certs-generator` container and stored in `./certs/`. Subsequent restarts skip the generation if the certificates already exist. To force regeneration:

```bash
# Option 1: use FORCE_REGEN
FORCE_REGEN=1 docker compose up -d

# Option 2: remove the certificates directory and restart
rm -rf ./certs
docker compose up -d
```

To generate certificates locally (outside Docker):

```bash
bash scripts/generate-certs.sh

# Force regeneration locally
FORCE_REGEN=1 bash scripts/generate-certs.sh
```

Additionally, each server auto-generates its own application certificate via `CheckApplicationInstanceCertificates()` at startup if one is not already present.

## Network

All servers run on a shared Docker network so they can communicate with each other. The discovery server at port 4844 can be used for server registration.

## Resource Usage

Each server instance is a lightweight .NET 10.0 process. The 10 servers combined typically use:
- ~300-500 MB RAM total
- Minimal CPU (mostly idle, spikes during subscriptions)

## Environment Variables

All configuration is done via environment variables in `docker-compose.yml`. See [Server Instances](servers.md) for per-server configuration.

| Variable | Default | Description |
|---|---|---|
| `OPCUA_PORT` | `4840` | Server listen port |
| `OPCUA_SERVER_NAME` | `OPCUATestServer` | Server display name |
| `OPCUA_HOSTNAME` | `0.0.0.0` | Bind address |
| `OPCUA_RESOURCE_PATH` | `/UA/TestServer` | Endpoint resource path |
| `OPCUA_SECURITY_POLICIES` | `None` | Comma-separated: None, Basic128Rsa15, Basic256, Basic256Sha256, Aes128_Sha256_RsaOaep, Aes256_Sha256_RsaPss |
| `OPCUA_SECURITY_MODES` | `None` | Comma-separated: None, Sign, SignAndEncrypt |
| `OPCUA_ALLOW_ANONYMOUS` | `true` | Allow anonymous connections |
| `OPCUA_AUTH_USERS` | `false` | Enable username/password authentication |
| `OPCUA_AUTH_CERTIFICATE` | `false` | Enable X.509 certificate authentication |
| `OPCUA_AUTO_ACCEPT_CERTS` | `false` | Auto-accept unknown client certificates |
| `OPCUA_IS_DISCOVERY` | `false` | Run as OPC UA Discovery Server |
| `OPCUA_MAX_SESSIONS` | `100` | Maximum concurrent sessions |
| `OPCUA_ENABLE_HISTORICAL` | `true` | Enable historical data nodes |
| `OPCUA_ENABLE_EVENTS` | `true` | Enable events and alarms |
| `OPCUA_ENABLE_METHODS` | `true` | Enable callable methods |
| `OPCUA_ENABLE_DYNAMIC` | `true` | Enable dynamic (time-varying) variables |
| `OPCUA_ENABLE_STRUCTURES` | `true` | Enable structured objects |
| `OPCUA_ENABLE_VIEWS` | `true` | Enable OPC UA views |
| `OPCUA_MAX_NODES_PER_READ` | `0` | Max nodes per Read request (0 = unlimited) |
| `OPCUA_MAX_NODES_PER_WRITE` | `0` | Max nodes per Write request (0 = unlimited) |
| `OPCUA_MAX_NODES_PER_BROWSE` | `0` | Max nodes per Browse request (0 = unlimited) |

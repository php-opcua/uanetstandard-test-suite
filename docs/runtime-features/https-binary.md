---
eyebrow: 'Docs · Runtime features'
lede:    'opc.https:// Binary endpoint (Part 6 §7.4.4) on the same TestServer process — used by the integration tests of php-opcua/opcua-client-ext-transport-https.'

see_also:
  - { href: './reverse-connect.md',                  meta: '4 min' }
  - { href: '../reference/environment-variables.md', meta: '3 min' }

prev: { label: 'Reverse Connect',           href: './reverse-connect.md' }
next: { label: 'Environment variables',     href: '../reference/environment-variables.md' }
---

# HTTPS Binary

Added in v1.5.0. The suite ships an `opcua-https-binary` docker-compose
service that exposes **both** `opc.tcp://` (internally) and
`opc.https://` on the same TestServer process, so a single instance can
serve PHP integration tests for the HTTPS binary mapping defined in
OPC UA Part 6 §7.4.4.

## Endpoints

| URL | Default port (host) | Purpose |
| --- | --- | --- |
| `opc.https://0.0.0.0:4852/UA/TestServer` | `4852` | HTTPS Binary endpoint — `application/octet-stream` |
| `opc.tcp://0.0.0.0:4862/UA/TestServer`   | `4862` | Plain UA-TCP — health-check + parity with the other services |

The HTTPS port is configurable via `OPCUA_HTTPS_PORT` (default `4852`).

## Certificate

UA-.NETStandard's auto-generated application certificate defaults to
1024-bit RSA, which modern TLS 1.2 / 1.3 cipher suites refuse. The
`certs-generator` service therefore pre-generates a stronger HTTPS
certificate that the TestServer installs at start-up:

| Property | Value |
| --- | --- |
| Key | RSA 2048 |
| Subject CN | `HttpsBinaryServer` |
| Issuer | `O=OPC UA Test Suite, CN=OPC UA Test CA` |
| SAN URI | `urn:opcua:testserver:nodes` |
| SAN DNS | `localhost`, `opcua-https-binary`, `host.docker.internal` |
| SAN IP | `127.0.0.1`, `0.0.0.0` |
| EKU | `serverAuth, clientAuth` |
| Validity | 10 years |
| Locations on disk | `certs/https-server/{cert.pem, cert.der, key.pem, key.der, server.pfx}` |

At container start, `Program.cs::InstallPregeneratedHttpsCertificate(config)`
loads `cert.pem` + `key.pem`, round-trips through PFX to bind both
halves on every runtime, and writes them into
`/tmp/pki/own/certs/HttpsBinaryServer [<thumbprint>].der` and
`/tmp/pki/own/private/HttpsBinaryServer [<thumbprint>].pfx` — the file
layout UA-.NETStandard's `CertificateStoreType.Directory` looks up by
`SubjectName`. The thumbprint is computed dynamically from
`X509Certificate2.Thumbprint`.

## Mutual TLS

`HttpsMutualTls = false` in `Program.cs`'s `ServerConfiguration`. The
HTTPS listener accepts plain TLS connections — no client cert required
on `CreateSession`. **Production deployments should set this back to
`true`**; the test endpoint flips it off so the PHP client connects
without configuring a TLS client cert.

## User authentication

`OPCUA_AUTH_USERS=true` is set on the `opcua-https-binary` service.
UA-.NETStandard's `HttpsServiceHost.CreateServiceHost(...)` filters the
**Anonymous** user token policy out of the HTTPS endpoint description
whenever `HttpsMutualTls = false` — so without a non-anonymous policy
the `ActivateSession` call would fail. The HTTPS endpoint therefore
advertises a `UserName` token policy and the integration tests
authenticate with the seeded `admin` / `admin123` user from
`config/users.json`. Anonymous over HTTPS requires mTLS to be on.

## Build & enable

The NuGet `OPCFoundation.NetStandard.Opc.Ua.Bindings.Https`
v1.5.378.134 is referenced from `TestServer.csproj`; without it
UA-.NETStandard ignores any `opc.https://` `BaseAddresses` entry.

At runtime, `BaseAddresses` is built by `BuildBaseAddresses(config)`:
the `opc.https://` URI is appended only when `OPCUA_ENABLE_HTTPS=true`.

## Disabling

`OPCUA_ENABLE_HTTPS=false` (the default on every other service) skips
both the certificate install and the address append.

## Verifying

```bash
docker compose up -d opcua-https-binary
docker logs uanetstandard-test-suite-opcua-https-binary-1 | grep "Installed pre-generated"
# [HTTPS] Installed pre-generated RSA 2048 cert (thumb=...) into /tmp/pki/own/

openssl s_client -connect localhost:4852 -showcerts < /dev/null 2>&1 | grep "Public-Key"
#                 Public-Key: (2048 bit)
```

## Integration test target

`php-opcua/opcua-client-ext-transport-https` v4.4.0 ships the
`BinaryHttpsTransport` that targets this endpoint. The end-to-end
integration test currently sits in a known-pending state — see the
`fase-1-missing.md` debug roadmap in that package.

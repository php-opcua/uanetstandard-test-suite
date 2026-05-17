---
eyebrow: 'Docs · Security'
lede:    'Every certificate the suite ships — what it''s for, where it lives, when each is presented. The reference your tests will reach for.'

see_also:
  - { href: './trust-flow.md',                            meta: '4 min' }
  - { href: '../authentication/certificate-authentication.md', meta: '4 min' }
  - { href: './policies-and-modes.md',                    meta: '5 min' }

prev: { label: 'Policies and modes',  href: './policies-and-modes.md' }
next: { label: 'Trust flow',          href: './trust-flow.md' }
---

# Certificates

All certificates are auto-generated on first start by the
`certs-generator` init container (Alpine + OpenSSL). Output
lands in the host's `./certs/` directory and is mounted into
every server.

## The full set

```text
certs/
├── ca/
│   ├── ca-key.pem        # CA private key (don't ship)
│   ├── ca-cert.pem       # CA certificate
│   ├── ca-cert.der       # Same, DER
│   └── ca-crl.pem        # Empty CRL (for completeness)
├── server/
│   ├── key.pem  key.der  # Server private key (PEM + DER)
│   ├── cert.pem cert.der # Server cert (PEM + DER), CA-signed
│   └── server.pfx        # Server cert + key, PKCS#12, empty password
├── client/
│   ├── key.pem  key.der
│   ├── cert.pem cert.der # Trusted client cert (CA-signed)
│   └── client.pfx        # PKCS#12 bundle
├── self-signed/
│   ├── key.pem  key.der
│   └── cert.pem cert.der # Self-signed (different issuer)
├── expired/
│   ├── key.pem
│   └── cert.pem cert.der # Backdated cert with already-past expiry
├── trusted/              # Server's trust dir (copy of client cert)
└── pki/                  # UA-.NETStandard PKI workspace
    ├── trusted/   issuers/   rejected/
```

## Per-file purpose

| File                              | Used as                                                | When                                              |
| --------------------------------- | ------------------------------------------------------ | ------------------------------------------------- |
| `ca/ca-cert.pem`                  | Trust anchor for your client                           | Validating the server cert                        |
| `server/cert.pem` + `key.pem`     | Pre-generated server cert (not actually loaded — see note) | —                                            |
| `client/cert.pem` + `key.pem`     | Your client's identity                                 | Cert-authenticated tests                          |
| `self-signed/cert.pem` + `key.pem` | A "wrong" cert                                         | Cert-rejection tests                              |
| `expired/cert.pem`                | An expired cert                                        | Expiry-validation tests                           |

### About the server cert

The pre-generated `server/cert.pem` is **not** loaded by the
servers. Each server auto-generates its own application
certificate via UA-.NETStandard's
`CheckApplicationInstanceCertificates()` at startup, stored
under `/tmp/pki/own/` inside each container.

`server/cert.pem` exists in the host filesystem to be human-
readable — it shows what the CA-signed server cert *would*
look like, with the right SAN, so you can inspect it if needed.

## CA cert details

```text
Subject:   CN=OPC UA Test CA
Key:       RSA 4096-bit
Validity:  10 years from generation
Use:       Trust anchor for client + server certs
```

Your client trusts this CA cert to validate the server's
auto-generated cert. The auto-generated server cert is **not**
CA-signed; it's self-issued by each server. Setting your client
to **also** trust by fingerprint (TOFU) or to validate the
chain only when present is the cleanest test pattern — see
[Trust flow](./trust-flow.md).

## Client (trusted) cert details

```text
Subject:           CN=OPC UA Test Client
Key:               RSA 2048-bit
Signed by:         OPC UA Test CA
Validity:          10 years from generation (`-days 3650`)
Subject Alt Names: URI:urn:opcua:testclient
```

This is the cert your tests **present** when authenticating
client-side. Servers that do strict cert validation
(`opcua-certificate`, the non-auto-accept ones) accept it
because:

1. Their `pki/trusted/` directory has been populated with it at
   startup (the server mounts it from `certs/trusted/`).
2. The CA cert is in their issuers list.

## Self-signed cert

```text
Subject:    CN=Self Signed Client
Signed by:  itself
Use:        Trigger Bad_CertificateUntrusted
```

A cert with a different issuer (it signed itself). The CA
doesn't recognise it. Use it in cert-rejection tests:

| Server              | Presenting `self-signed/cert.pem` |
| ------------------- | --------------------------------- |
| `opcua-certificate` | `Bad_CertificateUntrusted`        |
| `opcua-auto-accept` | Accepted (auto-trust mode)        |
| `opcua-no-security` | Irrelevant — server doesn't use cert auth |

## Expired cert

```text
Subject:    CN=Expired Client
Signed by:  OPC UA Test CA
Validity:   1 day from generation (`-days 1`, NOT backdated)
Use:        Will trigger Bad_CertificateTimeInvalid once the day elapses
```

The cert is signed for a 1-day validity window starting at the
moment `scripts/generate-certs.sh` runs. There is no backdating
in the script — for the first ~24 hours after running
`docker compose up -d` (or after deleting `certs/` and bringing
the stack back up) this cert is **still valid**. Tests that
assert `Bad_CertificateTimeInvalid` will see `Good` if executed
in that initial window.

If you need a guaranteed-expired cert on demand, regenerate
the cert tree against a system clock that is set far in the
future (`certs-generator` re-runs only when the `ca/`, `server/`
and `client/` PEM files are all missing — see "Regenerating"
below), or modify the script's `-days 1` to a negative or
already-elapsed value.

## Filename formats

Each cert ships in two formats:

- **PEM** — base-64, RFC 7468 (`-----BEGIN CERTIFICATE-----`).
- **DER** — raw binary.

Some libraries (e.g., .NET) prefer **PFX** (PKCS#12) bundles.
The CA-signed certs (server + client) include a `.pfx` with an
**empty password** — this is how UA-.NETStandard expects them.

## SAN coverage

The server cert's SAN includes every Docker service name plus
loopback:

```text
URI:  urn:opcua:testserver:nodes
DNS:  localhost
      opcua-no-security
      opcua-userpass
      opcua-certificate
      opcua-all-security
      opcua-discovery
      opcua-auto-accept
      opcua-sign-only
      opcua-legacy
      opcua-ecc-nist
      opcua-ecc-brainpool
IP:   127.0.0.1
      0.0.0.0
```

So when your client connects from the **host** via `localhost`,
cert hostname validation passes. When containers on the same
compose network talk to each other by service name, it also
passes.

## Regenerating

To regenerate everything:

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose down
rm -rf ./certs
docker compose up -d
```
<!-- @endcode-block -->

There is no `FORCE_REGEN` knob — `scripts/generate-certs.sh`
checks for the presence of `ca/ca-cert.pem`, `server/cert.pem`
and `client/cert.pem` and exits early ("Certificates already
exist, skipping generation.") when all three are present. The
**only** supported way to regenerate is to delete the relevant
files (or the whole `certs/` directory) before bringing the
stack up.

## Generating locally (outside Docker)

The same script is runnable on the host:

<!-- @code-block language="bash" label="terminal" -->
```bash
bash scripts/generate-certs.sh
```
<!-- @endcode-block -->

Useful for inspecting the cert configuration without running
Docker.

## Where to read next

- [Trust flow](./trust-flow.md) — how trust is established
  end-to-end.
- [Authentication · Certificate authentication](../authentication/certificate-authentication.md) —
  using these certs for user-identity auth.

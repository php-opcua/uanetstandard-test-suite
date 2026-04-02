# Security & Certificates

## Security Policies

OPC UA security policies define the cryptographic algorithms used for signing and encryption.

| Policy | OPC UA URI Suffix | Status | Key Size |
|---|---|---|---|
| `None` | `#None` | Current | N/A |
| `Basic128Rsa15` | `#Basic128Rsa15` | **Deprecated** | 1024-2048 bit |
| `Basic256` | `#Basic256` | **Deprecated** | 1024-2048 bit |
| `Basic256Sha256` | `#Basic256Sha256` | Current | 2048-4096 bit |
| `Aes128_Sha256_RsaOaep` | `#Aes128_Sha256_RsaOaep` | Current | 2048-4096 bit |
| `Aes256_Sha256_RsaPss` | `#Aes256_Sha256_RsaPss` | Current | 2048-4096 bit |

## Security Modes

| Mode | Description |
|---|---|
| `None` | No signing, no encryption. Messages sent in plaintext. |
| `Sign` | Messages are signed (integrity) but not encrypted. Content is readable on the wire. |
| `SignAndEncrypt` | Messages are both signed and encrypted. Full confidentiality and integrity. |

## Policy + Mode Combinations

Not all combinations are valid:

| | None | Sign | SignAndEncrypt |
|---|---|---|---|
| **None** | Valid (no security) | Invalid | Invalid |
| **Basic128Rsa15** | Invalid | Valid | Valid |
| **Basic256** | Invalid | Valid | Valid |
| **Basic256Sha256** | Invalid | Valid | Valid |
| **Aes128_Sha256_RsaOaep** | Invalid | Valid | Valid |
| **Aes256_Sha256_RsaPss** | Invalid | Valid | Valid |

`None/None` is the only valid combination with `None` policy. All other policies require `Sign` or `SignAndEncrypt`.

## Certificate Infrastructure

All certificates are generated automatically by the `certs-generator` Docker container on first startup. They are stored in a bind mount at `./certs/` accessible on the host filesystem.

Additionally, each server instance auto-generates its own application certificate via `CheckApplicationInstanceCertificates()` at startup. The ApplicationUri for all servers is `urn:opcua:testserver:nodes`.

### Certificate Authority (CA)

| File | Description |
|---|---|
| `certs/ca/ca-cert.pem` | Root CA certificate (PEM format) |
| `certs/ca/ca-key.pem` | Root CA private key |

The CA signs the server and client certificates, establishing a trust chain.

### Server Certificate

| File | Description |
|---|---|
| `certs/server/cert.pem` | Server certificate (PEM) -- signed by CA |
| `certs/server/key.pem` | Server private key (PEM) |
| `certs/server/cert.der` | Server certificate (DER binary format) |
| `certs/server/key.der` | Server private key (DER binary format) |

**Subject Alternative Names (SAN):**

```
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
IP:   127.0.0.1
      0.0.0.0
```

The SAN includes all Docker service names so that certificate validation succeeds both from the host (via `localhost`) and between containers (via service names).

### Client Certificate (Trusted)

Use these when testing certificate-based authentication:

| File | Description |
|---|---|
| `certs/client/cert.pem` | Client certificate (PEM) -- signed by CA, trusted by server |
| `certs/client/key.pem` | Client private key (PEM) |
| `certs/client/cert.der` | Client certificate (DER format) |
| `certs/client/key.der` | Client private key (DER format) |

### Self-Signed Certificate (Untrusted)

Use this to test certificate rejection:

| File | Description |
|---|---|
| `certs/self-signed/cert.pem` | Self-signed certificate -- NOT signed by the CA |
| `certs/self-signed/key.pem` | Corresponding private key |

Connecting with this certificate should be rejected by servers that validate certificates (all except `opcua-auto-accept`).

### Expired Certificate

Use this to test expiration validation:

| File | Description |
|---|---|
| `certs/expired/cert.pem` | Certificate with past expiration date |
| `certs/expired/key.pem` | Corresponding private key |

Connecting with this certificate should be rejected with a `BadCertificateTimeInvalid` error.

### Trust Directories

| Path | Description |
|---|---|
| `certs/trusted/` | Client certificates that the server trusts. The CA-signed client cert is copied here at startup. |
| `certs/rejected/` | Certificates that were rejected. Servers may move unknown certs here for manual review. |

## Certificate Trust Flow

```
CA (ca-cert.pem)
├── signs ──► Server cert (server/cert.pem)    Trusted
├── signs ──► Client cert (client/cert.pem)    Trusted
│
├── self-signed/cert.pem                       NOT trusted (different issuer)
└── expired/cert.pem                           NOT trusted (expired)
```

## Testing Security

### Discovering endpoints

```
Connect to any server -> call GetEndpoints()
Each endpoint descriptor contains:
  - endpointUrl
  - securityPolicyUri
  - securityMode
  - userIdentityTokens (list of accepted auth methods)
```

### Testing all policies on one server

Use `opcua-all-security` (port 4843) which has all 6 policies and all 3 modes enabled. This generates 11 endpoint combinations (6 policies x 2 secure modes + 1 None/None).

### Testing deprecated policies

Use `opcua-legacy` (port 4847) which only offers `Basic128Rsa15` and `Basic256`. Your client should ideally log a warning when using these.

### Testing Sign vs SignAndEncrypt

- `opcua-sign-only` (4846): only `Sign` mode -- messages are signed but readable
- `opcua-userpass` (4841): only `SignAndEncrypt` -- messages are fully encrypted
- Compare behavior and verify your client handles both correctly

### Auto-accept behavior

`opcua-auto-accept` (port 4845) has `autoAcceptCerts=true`. Any client certificate is accepted, even unknown ones. Use this for quick testing without certificate setup.

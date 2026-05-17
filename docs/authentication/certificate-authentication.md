---
eyebrow: 'Docs · Authentication'
lede:    'Connecting with an X.509 client certificate as the session identity. The accept paths, the reject paths, and the cert files for each.'

see_also:
  - { href: './user-accounts-and-roles.md',      meta: '3 min' }
  - { href: '../security/certificates.md',       meta: '5 min' }
  - { href: '../security/trust-flow.md',         meta: '4 min' }

prev: { label: 'User accounts and roles',  href: './user-accounts-and-roles.md' }
next: { label: 'Address space · Overview', href: '../address-space/overview.md' }
---

# Certificate authentication

OPC UA distinguishes the **application-level** certificate (for
the secure channel) from the **user-level** certificate (for the
session identity). The suite uses the **same cert files** for
both — but the validation paths are independent.

## Servers that accept cert auth

| Server               | Accepts cert auth | Notes                                                                       |
| -------------------- | ----------------- | --------------------------------------------------------------------------- |
| `opcua-certificate`  | ✓                 | Only cert auth (no anon, no userpass)                                       |
| `opcua-all-security` | ✓                 | Cert auth + anon + userpass                                                 |
| `opcua-auto-accept`  | ✓                 | Auto-trusts any client cert                                                 |
| `opcua-ecc-nist`     | ✓                 | Both anon/userpass/cert; channel policies are ECC (`nistP256`/`nistP384`)   |
| `opcua-ecc-brainpool` | ✓                | Same as above with Brainpool curves                                         |

Other servers either don't enable cert auth or use it only for
the secure channel.

Note: both ECC servers run with `OPCUA_AUTO_ACCEPT_CERTS=true`
in `docker-compose.yml`, so any client cert presented to them
(even self-signed) will be trusted. The user-identity token
itself is honoured if it is an `X509IdentityToken`. The trusted
RSA cert in `certs/client/cert.pem` will satisfy the
user-identity validation, but **the channel handshake** requires
a key matching the ECC policy — clients that only have an RSA
keypair cannot complete the secure channel on these endpoints.

## The cert files

| File                                 | Purpose                                  |
| ------------------------------------ | ---------------------------------------- |
| `certs/client/cert.pem` + `key.pem`  | Trusted client cert (CA-signed)          |
| `certs/self-signed/cert.pem` + `key.pem` | Untrusted self-signed cert            |
| `certs/expired/cert.pem` + `key.pem`  | Expired cert                             |

The trusted client cert is **pre-staged** in each server's PKI
trust dir at startup, so cert-auth servers accept it
immediately.

## Happy path

Connect to `opcua-certificate` with `certs/client/cert.pem`:

| Step                  | Detail                                      |
| --------------------- | ------------------------------------------- |
| Discovery             | `GetEndpoints()` returns cert-auth endpoints |
| Channel cert (yours)  | `client/cert.pem` → validated by server     |
| Channel cert (server) | Auto-generated, your client must trust      |
| Session identity      | `client/cert.pem` → validated by server     |
| Result                | Session created                              |

The same `client/cert.pem` is used **both** for the secure
channel and the session identity. This is the simplest pattern
and what most tests use.

You can also present a **different** cert at each layer (your
library's API permitting) — the suite doesn't constrain this.

## Negative paths

### Self-signed cert

```text
Present:  certs/self-signed/cert.pem
Result:   Bad_CertificateUntrusted (on all strict servers)
          Accepted (on opcua-auto-accept)
```

The self-signed cert has its **own** issuer, not the suite's
CA. Strict servers reject it because it's not in their PKI
trusted dir.

### Expired cert

```text
Present:  certs/expired/cert.pem
Result:   Bad_CertificateTimeInvalid
```

The cert is CA-signed (issuer is valid) but the `NotAfter`
field is in the past. Validates at the time-checking step.

### Wrong key for the cert

If your test mistakenly pairs `client/cert.pem` with
`expired/key.pem`, the channel handshake fails with
`Bad_CertificateInvalid` — the signed proof doesn't verify
against the cert's public key.

### Cert auth attempted against a non-cert server

| Server               | Presenting trusted client cert as identity |
| -------------------- | ------------------------------------------ |
| `opcua-userpass`     | `Bad_IdentityTokenRejected`                |
| `opcua-no-security`  | Rejected — no security-level identity     |

## Application URI matching

The client cert's `subjectAltName URI` field must match the
client's declared `ApplicationUri`. Mismatch yields
`Bad_CertificateUriInvalid`.

The suite's trusted client cert has:

```text
URI:   urn:opcua:testclient
```

Your client's `ApplicationUri` config must be `urn:opcua:testclient`
when presenting this cert, or the server rejects the channel.

For tests where you generate your own cert, ensure your client's
`ApplicationUri` matches the cert's SAN URI.

## ECC certs

The suite's pre-generated `client/cert.pem` is **RSA**. For
ECC-only servers (`opcua-ecc-nist`, `opcua-ecc-brainpool`),
you'd present an ECC client cert.

The suite doesn't ship a pre-generated ECC client cert because
the curves vary. Your client typically generates one at runtime
with the right curve type — UA-.NETStandard does this on the
server side, mirror the approach on the client side.

A test outline:

1. Generate an ECC keypair (`ECC_nistP256` for `opcua-ecc-nist`).
2. Create a self-signed cert with the right SAN URI.
3. Configure your client to use the ECC policy.
4. Use **auto-accept** server-side (or pre-stage the cert).

This is more involved than RSA cert auth — most suite tests stick
with RSA cert auth for the user-identity path and use ECC only
for the channel.

## Per-server expectations

| Server                  | Cert verb of art                             |
| ----------------------- | -------------------------------------------- |
| `opcua-certificate`     | The canonical cert-auth target. Use `client/cert.pem`. |
| `opcua-all-security`    | Same; cert auth is one of three options.     |
| `opcua-auto-accept`     | Auto-trusts any cert. Use to test client cert generation. |
| `opcua-ecc-nist`        | ECC cert required for cert-auth path.        |
| `opcua-ecc-brainpool`   | Brainpool ECC cert required.                 |

## What the server does on accept

When the server accepts your cert and creates a session, the
session's user identity carries:

- `tokenType = X509`
- `policyId = "X509"` (or similar — server-dependent)
- The cert is stored in the session for the lifetime of the
  session (relevant if your client tries to **rotate** identities
  mid-session — not supported here).

There's no "role" attached to a cert-authenticated user (unlike
the username flow). Cert-authenticated sessions effectively get
`AuthenticatedUser` plus whatever the server's default policy
grants.

## Where to read next

- [Address space · Overview](../address-space/overview.md) — what
  you can do with the session once it's established.
- [Security tests](../testing-patterns/security-tests.md) — test
  recipes for the cert-auth path.

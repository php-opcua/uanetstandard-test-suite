---
eyebrow: 'Docs · Security'
lede:    'How trust gets established between your client and the suite — the order of operations, what gets auto-trusted, and what won''t pass.'

see_also:
  - { href: './certificates.md',                     meta: '5 min' }
  - { href: './policies-and-modes.md',               meta: '5 min' }
  - { href: '../authentication/certificate-authentication.md', meta: '4 min' }

prev: { label: 'Certificates',  href: './certificates.md' }
next: { label: 'User accounts and roles', href: '../authentication/user-accounts-and-roles.md' }
---

# Trust flow

A secured OPC UA handshake involves three things that need to
"trust" each other:

1. **Your client's view of the server.** The server presents a
   cert; the client decides whether to accept it.
2. **The server's view of your client.** Your client presents a
   cert; the server decides whether to accept it.
3. **The user identity** (if cert-based). A separate cert
   presented at the session layer.

This page covers #1 and #2. For #3, see
[Certificate authentication](../authentication/certificate-authentication.md).

## The handshake — client perspective

1. Open TCP socket to the server.
2. Server sends its certificate.
3. Your client validates it: is it trusted? Not expired? Hostname
   matches? Not revoked?
4. If valid → proceed with secure-channel setup.
5. If invalid → typically `Bad_CertificateUntrusted`,
   `Bad_CertificateHostNameInvalid`, or
   `Bad_CertificateTimeInvalid`.

## What the suite's servers present

Each server presents a **self-signed cert** that it auto-
generated on first start. UA-.NETStandard does this via
`CheckApplicationInstanceCertificates()`.

The cert lives inside the container at `/tmp/pki/own/certs/`,
**not** in the host's `./certs/` directory. That means:

- The host-side `./certs/server/cert.pem` is **not** what the
  server actually presents.
- Each server's actual cert is **different** per container.
- Across restarts (without `down`), the cert is the same.
- After `docker compose down && rm -rf certs && up -d`, the cert
  changes.

This is deliberate — it mirrors how a real OPC UA server behaves
when generating its application instance certificate.

## How your client should trust the server cert

Three strategies, in order of strictness:

### Strategy A — pin by fingerprint (recommended for tests)

On first connection, capture the cert's SHA-256 fingerprint and
hard-code it in the test. Subsequent runs assert the fingerprint
matches. This is **TOFU** (trust on first use).

Most client libraries support this directly — e.g., a
`trust_policy = fingerprint` config setting.

### Strategy B — auto-accept

Tell your client to accept any server cert it sees. Convenient
for development, dangerous in production. The suite's
`opcua-auto-accept` server is the mirror image — it also
auto-accepts client certs. The two together remove all cert
friction.

### Strategy C — full chain validation

Add `certs/ca/ca-cert.pem` to your client's trust store. This
validates **CA-signed** certs — but, as noted, the actual server
cert is **not** CA-signed. So this strategy alone won't accept
the server.

For full-chain validation, you'd need to either:

- Pre-stage the server's auto-generated cert in your client's
  trust store (after first start), or
- Configure the server to use the pre-generated, CA-signed
  `certs/server/cert.pem` instead of auto-generating (requires a
  fork — see [Customization](../customization/forking-and-adding-nodes.md)).

## What the suite servers trust about your client

When you present a client cert (cert authentication or strict
secure-channel), the server validates it against the contents of
**its trust directory** at `/tmp/pki/trusted/certs/`.

By default, the suite **pre-stages**
`certs/client/cert.pem` into that directory at startup. So:

| Cert your client presents              | Server says               |
| -------------------------------------- | ------------------------- |
| `certs/client/cert.pem` (CA-signed)     | Accepts                   |
| `certs/self-signed/cert.pem`            | `Bad_CertificateUntrusted` |
| `certs/expired/cert.pem`                | `Bad_CertificateTimeInvalid` |
| A cert your own code generates           | `Bad_CertificateUntrusted` (not in trust dir) |
| Any cert, on `opcua-auto-accept`         | Accepts (and writes it into trust dir) |

`opcua-auto-accept` adds the cert to its `pki/trusted/` dir on
first contact, so subsequent connections with the **same** cert
work even after a restart.

## Per-server trust posture

| Server                | Validates client cert? | Notes                                       |
| --------------------- | ---------------------- | ------------------------------------------- |
| `opcua-no-security`    | N/A                    | No cert layer                                |
| `opcua-userpass`       | Server-side only       | Client cert presented for the channel       |
| `opcua-certificate`    | **Strict**             | Cert auth — must be in trust dir            |
| `opcua-all-security`   | Strict                 | Trust dir pre-staged                        |
| `opcua-auto-accept`    | Auto-accept            | Writes new certs to trust dir on first contact |
| `opcua-sign-only`      | Server-side only       | Cert for channel, not for user identity     |
| `opcua-legacy`         | Server-side only       | Same as userpass but with deprecated policies |
| `opcua-ecc-nist`       | Strict                 | ECC client cert needed for cert-auth path   |
| `opcua-ecc-brainpool`  | Strict                 | Same, Brainpool curve                       |

## Common failure modes

| Symptom from your client                  | Typical cause                                          |
| ----------------------------------------- | ------------------------------------------------------ |
| `Bad_CertificateUntrusted` (server cert)   | Your client's trust store doesn't have the server cert |
| `Bad_CertificateUntrusted` (client cert)   | The suite server doesn't have your cert in `pki/trusted/` |
| `Bad_CertificateHostNameInvalid`          | Connecting via a hostname not in the cert's SAN         |
| `Bad_CertificateTimeInvalid`              | Cert expired (correct behaviour with `expired/cert.pem`) |
| `Bad_CertificateSignatureInvalid`         | Wrong signing key — usually a regeneration mid-test    |
| `Bad_CertificateChainIncomplete`          | CA cert missing from your client's trust store          |

## Resetting trust between test runs

If your tests modify the auto-accept server's trust dir, reset
it cleanly between runs:

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose restart opcua-auto-accept
```
<!-- @endcode-block -->

For a stronger reset (re-generate all certs):

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose down
rm -rf ./certs
docker compose up -d
```
<!-- @endcode-block -->

## Where to read next

- [Authentication · User accounts and roles](../authentication/user-accounts-and-roles.md) —
  username/password auth.
- [Authentication · Certificate authentication](../authentication/certificate-authentication.md) —
  the user-identity cert flow.

---
eyebrow: 'Docs · Getting started'
lede:    'Three connection shapes you''ll exercise first: anonymous, username/password, and certificate authentication. The exact endpoints, credentials, and cert files for each.'

see_also:
  - { href: './quick-start.md',                       meta: '3 min' }
  - { href: '../authentication/user-accounts-and-roles.md', meta: '3 min' }
  - { href: '../authentication/certificate-authentication.md', meta: '4 min' }

prev: { label: 'Quick start', href: './quick-start.md' }
next: { label: 'Server instances · Overview', href: '../server-instances/overview.md' }
---

# First connection

Three patterns cover ~95% of integration testing.

## 1 — Anonymous (no security)

Server: `opcua-no-security`, port 4840.

| Setting           | Value                                    |
| ----------------- | ---------------------------------------- |
| Endpoint URL      | `opc.tcp://localhost:4840/UA/TestServer` |
| Security policy   | `None`                                   |
| Security mode     | `None`                                   |
| Identity token    | Anonymous                                |

Connect with whatever your library calls the "no-security
anonymous" path. No certificate handling, no credentials. The
right choice for any test that doesn't care about security.

## 2 — Username and password

Server: `opcua-userpass`, port 4841.

| Setting           | Value                                    |
| ----------------- | ---------------------------------------- |
| Endpoint URL      | `opc.tcp://localhost:4841/UA/TestServer` |
| Security policy   | `Basic256Sha256`                         |
| Security mode     | `SignAndEncrypt`                         |
| Identity token    | Username + password                      |

Credentials live in `config/users.json`:

| Username    | Password       | Role     |
| ----------- | -------------- | -------- |
| `admin`     | `admin123`     | admin    |
| `operator`  | `operator123`  | operator |
| `viewer`    | `viewer123`    | viewer   |
| `test`      | `test`         | admin    |

Test the negative paths too:

| Test                                | Expected                       |
| ----------------------------------- | ------------------------------ |
| Valid username + valid password     | Session created                |
| Valid username + wrong password     | `Bad_UserAccessDenied`         |
| Unknown username                    | `Bad_UserAccessDenied`         |
| Anonymous on this server            | `Bad_IdentityTokenRejected`    |

The server's certificate is **CA-signed**. Your client needs the
CA cert in its trust store, or to set "auto-accept" for testing
purposes (see [Trust flow](../security/trust-flow.md)).

CA cert path:

<!-- @code-block language="text" label="filesystem" -->
```text
certs/ca/ca-cert.pem
```
<!-- @endcode-block -->

## 3 — X.509 certificate authentication

Server: `opcua-certificate`, port 4842.

| Setting           | Value                                                       |
| ----------------- | ----------------------------------------------------------- |
| Endpoint URL      | `opc.tcp://localhost:4842/UA/TestServer`                    |
| Security policy   | `Basic256Sha256` (also `Aes128_Sha256_RsaOaep`, `Aes256_Sha256_RsaPss`) |
| Security mode     | `Sign` or `SignAndEncrypt`                                  |
| Identity token    | X.509 client certificate                                    |

Two certs are involved:

1. **Application certificate** — identifies your client at the
   secure-channel layer. Use `certs/client/cert.pem` +
   `certs/client/key.pem` (CA-signed, trusted by the server).
2. **User-identity certificate** — also `certs/client/cert.pem`
   in the simplest setup (same cert reused for app + user).

The server **does not** accept anonymous or username here — only
the X.509 user-identity flow.

Negative paths:

| Test                                       | Cert presented              | Expected                            |
| ------------------------------------------ | --------------------------- | ----------------------------------- |
| Trusted CA-signed cert                     | `certs/client/cert.pem`     | Session created                     |
| Self-signed cert (unknown issuer)          | `certs/self-signed/cert.pem`| `Bad_CertificateUntrusted`          |
| Expired cert                               | `certs/expired/cert.pem`    | `Bad_CertificateTimeInvalid`        |

## Picking a port — quick chart

| You want to test…                          | Use server                    |
| ------------------------------------------ | ----------------------------- |
| Quick connectivity, no crypto              | `opcua-no-security` (4840)    |
| Username/password flow                     | `opcua-userpass` (4841)       |
| Cert authentication, cert rejection paths   | `opcua-certificate` (4842)   |
| Discover the full endpoint matrix          | `opcua-all-security` (4843)   |
| Auto-accept untrusted certs (TOFU)          | `opcua-auto-accept` (4845)   |
| Sign-only mode (signed, not encrypted)      | `opcua-sign-only` (4846)     |
| Legacy policies (Basic128Rsa15 / Basic256)  | `opcua-legacy` (4847)        |
| Modern ECC (NIST curves)                    | `opcua-ecc-nist` (4848)      |
| Modern ECC (Brainpool curves)               | `opcua-ecc-brainpool` (4849) |
| PubSub SKS (`GetSecurityKeys`)              | `opcua-sks` (4851)           |
| UADP PubSub subscriber tests                | `opcua-pubsub` (UDP 14850)   |

Full breakdown in [Server instances · Overview](../server-instances/overview.md).

## Common first-time pitfalls

| Symptom                                       | Cause                                                  |
| --------------------------------------------- | ------------------------------------------------------ |
| Connection times out                          | Port mapping not exposed — re-check `docker compose ps` |
| `Bad_CertificateHostNameInvalid`              | Connecting via Docker service name instead of `localhost` |
| `Bad_CertificateUntrusted` on server          | CA cert missing from client trust store                |
| `Bad_IdentityTokenRejected` after correct creds | Connecting to server that disallows that auth method (e.g. anonymous on userpass) |
| Discovery server returns no endpoints         | Hitting the discovery endpoint (4844) for an application URL — no resource path |

See [Troubleshooting](../reference/troubleshooting.md) for more.

## Where to read next

- [Server instances · Overview](../server-instances/overview.md) —
  pick the right server.
- [Security · Policies and modes](../security/policies-and-modes.md) —
  the policy/mode matrix.
- [Authentication · Certificate authentication](../authentication/certificate-authentication.md) —
  cert handling deep-dive.

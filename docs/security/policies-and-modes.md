---
eyebrow: 'Docs · Security'
lede:    'Which policy/mode combinations the suite supports, which deprecated ones are still served for compat tests, and the matrix of what each classic server offers.'

see_also:
  - { href: './certificates.md',                  meta: '5 min' }
  - { href: './trust-flow.md',                    meta: '4 min' }
  - { href: '../server-instances/classic-rsa-and-ecc.md', meta: '6 min' }

prev: { label: 'Special-purpose servers',  href: '../server-instances/special-purpose.md' }
next: { label: 'Certificates',             href: './certificates.md' }
---

# Policies and modes

OPC UA security has two orthogonal axes: **policy** (algorithm
suite) and **mode** (sign / encrypt / neither). This page lists
which the suite supports and which server offers what.

## Policies

| Policy                       | Status         | Algorithm summary                              |
| ---------------------------- | -------------- | ---------------------------------------------- |
| `None`                       | Required       | No crypto                                       |
| `Basic128Rsa15`              | **Deprecated** | SHA-1, RSA-PKCS1, AES-128                       |
| `Basic256`                   | **Deprecated** | SHA-1                                           |
| `Basic256Sha256`             | Current        | SHA-256, RSA-PKCS1, AES-256                     |
| `Aes128_Sha256_RsaOaep`      | Current        | SHA-256, RSA-OAEP, AES-128                      |
| `Aes256_Sha256_RsaPss`       | Current        | SHA-256, RSA-PSS, AES-256                       |
| `ECC_nistP256`               | Current (ECC)  | ECDSA-P256, ECDH-P256, AES-128                  |
| `ECC_nistP384`               | Current (ECC)  | ECDSA-P384, ECDH-P384, AES-256                  |
| `ECC_brainpoolP256r1`        | Current (ECC)  | ECDSA-bP256r1, ECDH-bP256r1, AES-128            |
| `ECC_brainpoolP384r1`        | Current (ECC)  | ECDSA-bP384r1, ECDH-bP384r1, AES-256            |

`ECC_curve25519` / `ECC_curve448` are **blocked** upstream — see
[ROADMAP](https://github.com/php-opcua/uanetstandard-test-suite/blob/master/ROADMAP.md).

## Modes

| Mode             | What's protected                                              |
| ---------------- | ------------------------------------------------------------- |
| `None`           | Nothing — cleartext on the wire                               |
| `Sign`           | Integrity + authenticity (HMAC signature)                     |
| `SignAndEncrypt` | Integrity + authenticity + confidentiality (HMAC + AES/ChaCha) |

## Valid combinations

|                          | None        | Sign        | SignAndEncrypt |
| ------------------------ | ----------- | ----------- | -------------- |
| `None`                   | ✅          | ❌          | ❌             |
| `Basic128Rsa15`           | ❌          | ✅          | ✅             |
| `Basic256`               | ❌          | ✅          | ✅             |
| `Basic256Sha256`         | ❌          | ✅          | ✅             |
| `Aes128_Sha256_RsaOaep`   | ❌          | ✅          | ✅             |
| `Aes256_Sha256_RsaPss`    | ❌          | ✅          | ✅             |
| `ECC_nistP256` / P384    | ❌          | ✅          | ✅             |
| `ECC_brainpool*`          | ❌          | ✅          | ✅             |

`None/None` is the only valid combination with policy `None`.
Every other policy requires `Sign` or `SignAndEncrypt`.

## What each server offers

| Server                      | Policies offered                                  | Modes offered      |
| --------------------------- | ------------------------------------------------- | ------------------ |
| `opcua-no-security`          | `None`                                            | `None`             |
| `opcua-userpass`             | `Basic256Sha256`                                  | `SignAndEncrypt`   |
| `opcua-certificate`          | `Basic256Sha256`, `Aes128_Sha256_RsaOaep`, `Aes256_Sha256_RsaPss` | `Sign`, `SignAndEncrypt` |
| `opcua-all-security`         | `None`, `Basic128Rsa15`, `Basic256`, `Basic256Sha256`, `Aes128_Sha256_RsaOaep`, `Aes256_Sha256_RsaPss` | `None`, `Sign`, `SignAndEncrypt` |
| `opcua-discovery`            | `None`, `Basic256Sha256`                          | `None`, `SignAndEncrypt` |
| `opcua-auto-accept`          | `Basic256Sha256`                                  | `SignAndEncrypt`   |
| `opcua-sign-only`            | `Basic256Sha256`                                  | `Sign`             |
| `opcua-legacy`               | `Basic128Rsa15`, `Basic256`                       | `Sign`, `SignAndEncrypt` |
| `opcua-ecc-nist`             | `ECC_nistP256`, `ECC_nistP384`                    | `Sign`, `SignAndEncrypt` |
| `opcua-ecc-brainpool`        | `ECC_brainpoolP256r1`, `ECC_brainpoolP384r1`      | `Sign`, `SignAndEncrypt` |
| `opcua-sks`                  | `None`                                            | `None`             |

## Number of endpoints per server

A server advertises one `EndpointDescription` per
`(policy, mode)` pair that's valid for it. Reading `GetEndpoints()`
returns:

| Server                | Endpoint count |
| --------------------- | -------------- |
| `opcua-no-security`    | 1              |
| `opcua-userpass`       | 1              |
| `opcua-certificate`    | 6              |
| `opcua-all-security`   | 11             |
| `opcua-auto-accept`    | 1              |
| `opcua-sign-only`      | 1              |
| `opcua-legacy`         | 4              |
| `opcua-ecc-nist`       | 4              |
| `opcua-ecc-brainpool`  | 4              |

For testing endpoint-negotiation logic, `opcua-all-security` is
the canonical target.

## Endpoint URIs

Each policy in `GetEndpoints` returns a URI of the form:

```text
http://opcfoundation.org/UA/SecurityPolicy#<PolicyName>
```

Examples:

- `http://opcfoundation.org/UA/SecurityPolicy#None`
- `http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256`
- `http://opcfoundation.org/UA/SecurityPolicy#ECC_nistP256`

The full constant names are in the
[OPC Foundation spec](https://reference.opcfoundation.org/Core/Part2/v105/docs/).

## How to override

Each server's policies and modes are env-var-driven. To change
which policies a server offers, edit `docker-compose.yml`:

<!-- @code-block language="text" label="compose snippet" -->
```text
environment:
  OPCUA_SECURITY_POLICIES: "Basic256Sha256,Aes256_Sha256_RsaPss"
  OPCUA_SECURITY_MODES:    "Sign,SignAndEncrypt"
```
<!-- @endcode-block -->

After editing, `docker compose up -d` re-creates the affected
service.

## Where to read next

- [Certificates](./certificates.md) — the cert files the suite
  ships with.
- [Trust flow](./trust-flow.md) — how trust is established
  between the suite and your client.

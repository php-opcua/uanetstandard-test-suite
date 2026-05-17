---
eyebrow: 'Docs · Testing patterns'
lede:    'Policy negotiation, certificate validation, authentication paths — the recipes for the security layer of your OPC UA client.'

see_also:
  - { href: '../security/policies-and-modes.md',                meta: '5 min' }
  - { href: '../security/certificates.md',                       meta: '5 min' }
  - { href: '../authentication/certificate-authentication.md',   meta: '4 min' }

prev: { label: 'Subscription and method tests',  href: './subscription-and-method-tests.md' }
next: { label: 'Forking and adding nodes', href: '../customization/forking-and-adding-nodes.md' }
---

# Security tests

The recipes that exercise the policy, mode, certificate, and
authentication layers.

## Policy and mode tests

### Discover all policies

```text
GetEndpoints("opc.tcp://localhost:4843/UA/TestServer")  # opcua-all-security
→ 11 EndpointDescriptions covering every (policy, mode) pair
```

For each endpoint:

```text
verify EndpointDescription contains:
  - endpointUrl
  - securityPolicyUri
  - securityMode
  - userIdentityTokens  (list of accepted token types)
  - transportProfileUri
```

### Connect with each policy on opcua-all-security

```text
for each EndpointDescription ed:
    if ed.securityMode == None:
        connect_anonymous(ed)
        → Good
    elif ed.userIdentityTokens contains Anonymous:
        connect_anonymous(ed, with client cert)
        → Good
    elif ed.userIdentityTokens contains UserName:
        connect_username(ed, "admin", "admin123")
        → Good
```

The right test of "my client speaks every policy".

### Negotiate to strongest available

A canonical client behaviour:

```text
endpoints = GetEndpoints(...)

# Filter: drop deprecated, prefer SignAndEncrypt
candidates = [
    ed for ed in endpoints
    if ed.securityPolicy not in DEPRECATED
       and ed.securityMode == SignAndEncrypt
]

# Pick by Strength (server-reported)
best = max(candidates, key=lambda e: e.securityLevel)

connect(best)
→ Good
```

Test against `opcua-all-security` — your client should pick
`Aes256_Sha256_RsaPss + SignAndEncrypt` or similar (strongest
non-deprecated).

### Legacy policies

```text
connect("opc.tcp://localhost:4847", Basic128Rsa15, SignAndEncrypt)
→ Good (server offers it)
→ Your client should ideally log a warning about deprecated policy
```

### ECC negotiation

```text
GetEndpoints("opc.tcp://localhost:4848")
→ 4 endpoints (ECC_nistP256 + ECC_nistP384) × (Sign + SignAndEncrypt)

connect with ECC_nistP256 + SignAndEncrypt
→ Good
```

## Certificate validation tests

### Trusted client cert

```text
connect("opc.tcp://localhost:4842", Basic256Sha256, SignAndEncrypt,
        client_cert = certs/client/cert.pem,
        client_key  = certs/client/key.pem)
→ Good
```

### Self-signed (untrusted) cert

```text
connect("opc.tcp://localhost:4842", ...,
        client_cert = certs/self-signed/cert.pem,
        client_key  = certs/self-signed/key.pem)
→ Bad_CertificateUntrusted
```

### Expired cert

```text
connect("opc.tcp://localhost:4842", ...,
        client_cert = certs/expired/cert.pem,
        client_key  = certs/expired/key.pem)
→ Bad_CertificateTimeInvalid
```

### Auto-accept

```text
connect("opc.tcp://localhost:4845", ...,
        client_cert = certs/self-signed/cert.pem,
        client_key  = certs/self-signed/key.pem)
→ Good  (auto-accept server accepts unknown certs)
```

Subsequent connections with the same self-signed cert also
succeed — the server moves it to its `trusted/` dir on first
contact.

### Application URI mismatch

```text
connect with client_cert that has URI=urn:opcua:something-else
→ Bad_CertificateUriInvalid
```

The cert's `subjectAltName URI` must match the client's
`ApplicationUri`.

## Authentication tests

### Valid username

```text
connect("opc.tcp://localhost:4841", ..., user="admin", pass="admin123")
→ Good, session created
```

Repeat for `operator/operator123`, `viewer/viewer123`, `test/test`.

### Wrong password

```text
connect(..., user="admin", pass="wrongpassword")
→ Bad_UserAccessDenied
```

### Unknown user

```text
connect(..., user="nonexistent", pass="x")
→ Bad_UserAccessDenied
```

### Anonymous on userpass

```text
connect("opc.tcp://localhost:4841", ..., identity=Anonymous)
→ Bad_IdentityTokenRejected
```

Note the distinction: invalid username/password combinations
return `Bad_UserAccessDenied` (the token type was accepted but
the credentials failed). `Bad_IdentityTokenRejected` is only
returned when the token type itself is not allowed on the
endpoint — Anonymous on a server with `AllowAnonymous=false`,
or Certificate on a server with `AuthCertificate=false`.

### Role-based write rejection

```text
# Connect as viewer
session = connect(..., user="viewer", pass="viewer123")
write(session, ns=1;s=TestServer/AccessControl/OperatorLevel/Setpoint, 60.0)
→ Bad_UserAccessDenied
```

Compare with operator (allowed):

```text
session = connect(..., user="operator", pass="operator123")
write(session, ..., 60.0)
→ Good
```

### Certificate auth happy path

```text
connect("opc.tcp://localhost:4842", ...,
        # Channel uses client cert
        client_cert = certs/client/cert.pem,
        # User identity is also a cert
        identity = X509(certs/client/cert.pem, certs/client/key.pem))
→ Good
```

### Certificate auth — wrong identity cert

```text
connect("opc.tcp://localhost:4842", ...,
        client_cert = certs/client/cert.pem,
        identity = X509(certs/self-signed/cert.pem, ...))
→ Bad_CertificateUntrusted (or Bad_IdentityTokenRejected)
```

## Trust-store tests

### Server cert TOFU

A useful pattern for tests that don't want to pre-share certs:

```text
1. Connect to opcua-userpass (4841) for the first time
2. Server presents its auto-generated cert
3. Your client captures fingerprint, stores it
4. Disconnect
5. Re-connect — client trusts the same fingerprint
```

If the cert changes between steps (server restarted with cert
regen), the test should detect the change and fail.

### CA cert validation

```text
# Add certs/ca/ca-cert.pem to your client's trust store
# This validates the CA-signed certs but NOT the server's
# auto-generated cert (which is self-issued, not CA-signed)

connect(opcua-userpass, ...)
→ Bad_CertificateUntrusted (server cert not CA-signed)
```

Pair this with fingerprint pinning for a working setup.

## Discovery tests

### FindServers

```text
client.connect("opc.tcp://localhost:4844")  # discovery, no resource path
client.call_find_servers()
→ list containing (at most) the discovery server itself
```

None of the classic test servers in `docker-compose.yml` set
`OPCUA_DISCOVERY_URL`, and `TestServerApp` does not call
`RegisterServer` / `RegisterServer2` against any discovery
endpoint. So `FindServers` against port 4844 will **not** return
the other suite servers — it returns only whatever the
discovery server has self-registered (effectively itself, or an
empty list, depending on the UA-.NETStandard stack version).
The discovery endpoint is provided primarily as a target for
testing the `FindServers` and `GetEndpoints` calls themselves,
not as a working server registry for the rest of the suite.

### GetEndpoints on discovery server

```text
GetEndpoints("opc.tcp://localhost:4844")
→ EndpointDescriptions for the discovery service itself
→ None/None + Basic256Sha256/SignAndEncrypt
```

## Test matrix summary

| Test                              | Server                   | Expected                          |
| --------------------------------- | ------------------------ | --------------------------------- |
| Connect anonymous                  | 4840                    | Good                              |
| Connect anonymous on userpass      | 4841                    | `Bad_IdentityTokenRejected`        |
| Valid creds                        | 4841                    | Good                              |
| Wrong password                     | 4841                    | `Bad_UserAccessDenied`             |
| Trusted client cert                | 4842                    | Good                              |
| Self-signed cert                   | 4842                    | `Bad_CertificateUntrusted`         |
| Expired cert                       | 4842                    | `Bad_CertificateTimeInvalid`       |
| Any cert (auto-accept)             | 4845                    | Good                              |
| Sign-only channel                  | 4846                    | Good (Sign mode)                  |
| Legacy policy                       | 4847                    | Good (with warning)               |
| ECC NIST                           | 4848                    | Good (ECC negotiation)             |
| ECC Brainpool                      | 4849                    | Good (Brainpool negotiation)       |
| FindServers                        | 4844                    | Non-empty list                    |

This 13-test matrix is a strong "security layer works" battery.

## Where to read next

- [Security](../security/policies-and-modes.md) — the policy /
  mode / cert reference.
- [Authentication](../authentication/user-accounts-and-roles.md) —
  user accounts and cert identity.

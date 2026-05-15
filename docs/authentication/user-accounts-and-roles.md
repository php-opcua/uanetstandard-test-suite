---
eyebrow: 'Docs · Authentication'
lede:    'The four built-in user accounts, their passwords, and the role-based access semantics they exercise. Plus the negative paths your tests should cover.'

see_also:
  - { href: './certificate-authentication.md',         meta: '4 min' }
  - { href: '../security/trust-flow.md',              meta: '4 min' }
  - { href: '../data-features/access-control.md',     meta: '4 min' }

prev: { label: 'Trust flow',                  href: '../security/trust-flow.md' }
next: { label: 'Certificate authentication',  href: './certificate-authentication.md' }
---

# User accounts and roles

When a server has `OPCUA_AUTH_USERS=true`, it validates
username/password credentials against `config/users.json`. The
suite ships four canned accounts covering the three role tiers
plus a convenience admin.

## The four accounts

| Username   | Password      | Role     | Why it exists                                   |
| ---------- | ------------- | -------- | ----------------------------------------------- |
| `admin`    | `admin123`    | admin    | Full-access role tests                          |
| `operator` | `operator123` | operator | Mid-tier read+write tests                       |
| `viewer`   | `viewer123`   | viewer   | Read-only tests, write-rejection tests          |
| `test`     | `test`        | admin    | Convenience login for ad-hoc tests              |

Plaintext passwords are **intentional** — this is a test suite,
not a real auth system. Don't mirror this config into a real
deployment.

## What each role implies

The role maps to a set of OPC UA role identifiers (per the spec's
WellKnownRoles):

### admin

```text
AuthenticatedUser, ConfigureAdmin, SecurityAdmin, Operator, Engineer
```

- Read: every variable.
- Write: every writable variable, including `AccessControl/AdminOnly`
  and `AccessControl/OperatorLevel`.
- Method calls: all.
- History: full read.

### operator

```text
AuthenticatedUser, Operator
```

- Read: every variable.
- Write: `AccessControl/OperatorLevel` variables, regular `_RW`
  variables. Cannot write `AccessControl/AdminOnly`.
- Method calls: all (method-level role checks are not enforced).
- History: read.

### viewer

```text
AuthenticatedUser
```

- Read: every readable variable.
- Write: **none**. Any write returns `Bad_UserAccessDenied`.
- Method calls: yes (consistent with the spec — methods aren't
  role-gated in the test suite).
- History: read.

### anonymous (no user)

- Read: variables marked anonymous-allowed.
- Write: depends on server.
- No role identifiers.

## Which servers accept which auth

| Server                  | Anonymous | Username | Cert auth |
| ----------------------- | --------- | -------- | --------- |
| `opcua-no-security` (4840)  | ✓     |          |           |
| `opcua-userpass` (4841)     |       | ✓        |           |
| `opcua-certificate` (4842)  |       |          | ✓         |
| `opcua-all-security` (4843) | ✓     | ✓        | ✓         |
| `opcua-discovery` (4844)    | ✓     |          |           |
| `opcua-auto-accept` (4845)  | ✓     | ✓        | ✓         |
| `opcua-sign-only` (4846)    | ✓     | ✓        |           |
| `opcua-legacy` (4847)       | ✓     | ✓        |           |
| `opcua-ecc-nist` (4848)     | ✓     | ✓        | ✓         |
| `opcua-ecc-brainpool` (4849) | ✓    | ✓        | ✓         |
| `opcua-sks` (4851)          | ✓     |          |           |

## Test patterns

### Happy path

| Server          | User       | Expected                              |
| --------------- | ---------- | ------------------------------------- |
| `opcua-userpass` | `admin`    | Session created, full RW              |
| `opcua-userpass` | `operator` | Session created, RW on operator nodes |
| `opcua-userpass` | `viewer`   | Session created, RO only              |
| `opcua-userpass` | `test`     | Session created, full RW              |

### Negative path — auth failure

| Server          | Identity                       | Expected                       |
| --------------- | ------------------------------ | ------------------------------ |
| `opcua-userpass` | `admin` + wrong password      | `Bad_IdentityTokenRejected`    |
| `opcua-userpass` | `unknown` + any password      | `Bad_IdentityTokenRejected`    |
| `opcua-userpass` | Anonymous                     | `Bad_IdentityTokenRejected`    |

### Negative path — role-based write rejection

| Server          | User       | Action                                      | Expected               |
| --------------- | ---------- | ------------------------------------------- | ---------------------- |
| `opcua-userpass` | `viewer`  | Write to `AccessControl/OperatorLevel/Setpoint` | `Bad_UserAccessDenied` |
| `opcua-userpass` | `operator` | Same                                       | OK                     |
| `opcua-userpass` | `admin`   | Write to `AccessControl/AdminOnly/SecretConfig` | OK                |
| `opcua-userpass` | `operator` | Same                                       | `Bad_UserAccessDenied` |

## Adding users (fork)

Edit `config/users.json` and restart the server:

<!-- @code-block language="text" label="config/users.json" -->
```text
[
  { "username": "admin",      "password": "admin123",      "role": "admin"    },
  { "username": "operator",   "password": "operator123",   "role": "operator" },
  { "username": "viewer",     "password": "viewer123",     "role": "viewer"   },
  { "username": "test",       "password": "test",          "role": "admin"    },
  { "username": "plc_svc",    "password": "s3cure!",       "role": "operator" }
]
```
<!-- @endcode-block -->

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose restart opcua-userpass
```
<!-- @endcode-block -->

The new user is now valid. For non-default roles, also extend
`UserManager.GetUserRoles()` in `src/TestServer/UserManagement/` —
see [Customization](../customization/forking-and-adding-nodes.md).

## Where to read next

- [Certificate authentication](./certificate-authentication.md) —
  the cert-based identity flow.
- [Data features · Access control](../data-features/access-control.md) —
  the 50 access-control variables that exercise these roles.

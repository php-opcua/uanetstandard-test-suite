# Authentication & Roles

## Authentication Methods

The suite supports three authentication methods, enabled independently per server via environment variables:

| Method | Env Variable | Description |
|---|---|---|
| Anonymous | `OPCUA_ALLOW_ANONYMOUS=true` | No credentials required |
| Username/Password | `OPCUA_AUTH_USERS=true` | Validates against `config/users.json` |
| X.509 Certificate | `OPCUA_AUTH_CERTIFICATE=true` | Client presents a certificate |

## User Accounts

Defined in `config/users.json`. Available on servers with `OPCUA_AUTH_USERS=true`.

| Username | Password | Role | Description |
|---|---|---|---|
| `admin` | `admin123` | admin | Full access to everything |
| `operator` | `operator123` | operator | Read/write on operational variables |
| `viewer` | `viewer123` | viewer | Read-only everywhere |
| `test` | `test` | admin | Convenience account for quick testing |

## Roles and Permissions

Each role maps to a set of OPC UA role identifiers:

### admin

```
AuthenticatedUser, ConfigureAdmin, SecurityAdmin, Operator, Engineer
```

- **Read:** All variables
- **Write:** All writable variables (including AdminOnly, OperatorLevel, AllCombinations_RW)
- **Methods:** All methods
- **Historical:** Full history access

### operator

```
AuthenticatedUser, Operator
```

- **Read:** All variables
- **Write:** OperatorLevel variables, AllCombinations_RW, standard RW variables
- **Methods:** All methods
- **Historical:** History read access

### viewer

```
AuthenticatedUser
```

- **Read:** All readable variables
- **Write:** None (all writes should return `BadUserAccessDenied`)
- **Methods:** Can call methods (method-level access control is not role-restricted)
- **Historical:** History read access

### anonymous

- **Read:** Variables with `allowAnonymous` enabled on the server
- **Write:** Depends on server configuration
- No specific role identifiers

## Which Server Uses Which Auth

| Server | Anonymous | Username | Certificate |
|---|---|---|---|
| `opcua-no-security` (4840) | Yes | No | No |
| `opcua-userpass` (4841) | No | Yes | No |
| `opcua-certificate` (4842) | No | No | Yes |
| `opcua-all-security` (4843) | Yes | Yes | Yes |
| `opcua-discovery` (4844) | Yes | No | No |
| `opcua-auto-accept` (4845) | Yes | Yes | Yes |
| `opcua-sign-only` (4846) | Yes | Yes | No |
| `opcua-legacy` (4847) | Yes | Yes | No |

## Testing Authentication

### Valid login

Connect to `opcua-userpass` (port 4841) with:
- Username: `admin`, Password: `admin123`
- Expect: session created, full read/write access

### Invalid login

Connect to `opcua-userpass` (port 4841) with:
- Username: `admin`, Password: `wrongpassword`
- Expect: `BadIdentityTokenRejected` or `BadUserAccessDenied`

### Anonymous on secured server

Connect to `opcua-userpass` (port 4841) with anonymous token:
- Expect: `BadIdentityTokenRejected` (anonymous is disabled)

### Role-based access

1. Connect as `viewer` to `opcua-userpass` (4841)
2. Try to write to `TestServer > DataTypes > Scalar > BooleanValue`
3. Expect: `BadUserAccessDenied`
4. Reading the same variable should succeed

### Certificate authentication

1. Connect to `opcua-certificate` (port 4842)
2. Present the client certificate from `certs/client/cert.pem` with key `certs/client/key.pem`
3. Expect: session created

### Rejected certificate

1. Connect to `opcua-certificate` (port 4842)
2. Present the self-signed certificate from `certs/self-signed/cert.pem`
3. Expect: `BadCertificateUntrusted` or similar rejection

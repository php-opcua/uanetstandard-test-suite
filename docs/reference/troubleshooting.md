---
eyebrow: 'Docs · Reference'
lede:    'Common symptoms and what causes them — port conflicts, cert issues, auth failures, PubSub silence, daemon startup loops.'

see_also:
  - { href: './environment-variables.md',         meta: '4 min' }
  - { href: './ports-and-endpoints.md',           meta: '3 min' }
  - { href: '../getting-started/installation.md', meta: '3 min' }

prev: { label: 'Ports and endpoints',  href: './ports-and-endpoints.md' }
next: { label: 'Top of docs',          href: '../index.md' }
---

# Troubleshooting

Symptom → cause → fix.

## Suite won't start

### `docker compose up -d` fails

| Cause                                            | Fix                                                  |
| ------------------------------------------------ | ---------------------------------------------------- |
| Port already in use (4840-4849, 4851, 14850)      | `lsof -i :4840` to find owner; kill or change port    |
| Image build failure                              | Check `docker compose build` output, often a NuGet/.NET version mismatch |
| Insufficient disk space                          | `docker system prune -a` to clear old images          |
| `certs-generator` keeps exiting non-zero          | `docker compose logs certs-generator` — usually OpenSSL issue in the script |

### Some servers stay `restarting`

```text
docker compose logs --tail=50 opcua-userpass
```

Common offenders:

| Log line                                             | Cause                                              |
| ---------------------------------------------------- | -------------------------------------------------- |
| `EndpointDescription not found for SecurityPolicy`    | Mismatched `OPCUA_SECURITY_POLICIES` + `MODES`     |
| `Cannot bind to address 0.0.0.0:NNNN`                  | Port collision with another container or host process |
| `Application certificate could not be created`         | PKI dir not writable (check volume mount perms)    |
| `User cert directory missing`                          | `./certs` directory wiped while server running     |

## Connection issues

### `Connection refused` from `localhost:4840`

| Cause                                | Check                                                  |
| ------------------------------------ | ------------------------------------------------------ |
| Service not running                  | `docker compose ps opcua-no-security` — state `running`? |
| Port not exposed on the host         | `docker port opcua-no-security` should show `4840`     |
| Firewall blocking                    | `ufw status` / `iptables -L`                           |
| Connecting from the wrong host       | If you're on a Docker Desktop VM, `localhost` is the VM. Use the actual host. |

### `Bad_CertificateHostNameInvalid`

The server cert's SAN doesn't include the hostname your client
used.

| You connected via                | Fix                                              |
| -------------------------------- | ------------------------------------------------ |
| `localhost` (the typical case)    | Should work — re-regenerate certs                |
| The container service name (`opcua-no-security`) | From a sibling container, this works. From the host, use `localhost`. |
| A different DNS name              | Add to `scripts/generate-certs.sh` SAN; regenerate |

### `Bad_CertificateUntrusted`

Depends which side:

| Side                                   | Cause                                                              |
| -------------------------------------- | ------------------------------------------------------------------ |
| **Client says server is untrusted**    | Server presents an auto-generated cert. Your client needs to TOFU-pin it or be configured to auto-accept. |
| **Server says client is untrusted**    | Strict-validation server doesn't have your client cert in its trust dir. Use `certs/client/cert.pem` (pre-staged) or `opcua-auto-accept`. |

### `Bad_IdentityTokenRejected`

Returned when the **token type itself** is not accepted by the
endpoint. The credentials never reach validation.

| Cause                                       | Fix                                            |
| ------------------------------------------- | ---------------------------------------------- |
| Anonymous on a server with `AllowAnonymous=false` | Provide username/password or a cert     |
| UserName token on a server with `AuthUsers=false` | Use Anonymous (or the cert flow)         |
| Cert auth on a non-cert server              | Use `opcua-certificate` (4842), `opcua-all-security` (4843), `opcua-auto-accept` (4845), `opcua-ecc-nist` (4848), or `opcua-ecc-brainpool` (4849) |

### `Bad_UserAccessDenied`

Returned by the credential-validation step when the token type
was accepted but the username/password combination failed.

| Cause                                       | Fix                                            |
| ------------------------------------------- | ---------------------------------------------- |
| Wrong password                              | Check against `config/users.json`              |
| Unknown username                            | Check against `config/users.json`              |
| Writes from a `viewer`-role session on a role-protected node | Reconnect as `operator` or `admin` |

### `Bad_TooManyOperations` on Read

Only `opcua-no-security` has this set (cap of 5). Either:

- Reduce your batch size.
- Use a different server (no cap on the others).

## PubSub issues

### No UDP packets received on `127.0.0.1:14850`

| Cause                                          | Fix                                                |
| ---------------------------------------------- | -------------------------------------------------- |
| `opcua-pubsub` not running                      | `docker compose ps opcua-pubsub`                  |
| `opcua-pubsub-relay` not running                | `docker compose ps opcua-pubsub-relay`            |
| Firewall blocking UDP                           | `iptables -L | grep 14850`                         |
| Wrong listener address                          | Listen on `0.0.0.0:14850`, not `127.0.0.1:14850`, if running in another container |
| Docker Desktop network mode issue                | Restart Docker Desktop; relay needs `host.docker.internal` |

### Counter sequence jumps

UDP isn't reliable. Small jumps (1-2 missed packets per minute)
are normal. Large jumps mean:

- Publisher restarting (check logs)
- Network buffer overflow (rare on localhost)
- Receiver too slow

## Certificate issues

### `Bad_CertificateTimeInvalid`

The cert is expired or not yet valid. For `certs/expired/*.pem`,
this is the **expected** behaviour — that's what the cert is
for.

For other certs (your own, or the suite's):

- System clock — `date` — out of sync?
- Cert regeneration mid-test? Check `certs/server/cert.pem`
  `NotAfter` (`openssl x509 -in cert.pem -noout -dates`).

### `Bad_CertificateUriInvalid`

The cert's URI SAN doesn't match the client's `ApplicationUri`.

The suite's client cert has URI `urn:opcua:testclient`. Your
client's `ApplicationUri` config must match.

## Build / source issues

### After editing C# code, changes don't show

The Docker image is cached. Force rebuild:

<!-- @code-block language="bash" label="rebuild" -->
```bash
docker compose build --no-cache
docker compose up -d
```
<!-- @endcode-block -->

### `dotnet restore` fails

If you've pinned UA-.NETStandard to a specific version and
NuGet can't find it:

- Check `src/TestServer/TestServer.csproj` — version mismatch?
- Try `dotnet restore --no-cache` to bypass NuGet cache.

## Performance

### Servers slow to start

The cert generator runs serially. First start takes 30-60s. To
speed up subsequent starts, **don't** delete `certs/`.

For CI, pre-generate certs once and cache the directory:

<!-- @code-block language="text" label="cache certs in GH Actions" -->
```text
- name: Cache certs
  uses: actions/cache@v4
  with:
    path: ./certs
    key: opcua-certs-${{ runner.os }}-v1
```
<!-- @endcode-block -->

### Memory pressure

All 11 servers use ~600 MB. If you're constrained, start a
subset:

<!-- @code-block language="bash" label="subset" -->
```bash
docker compose up -d opcua-no-security opcua-userpass
```
<!-- @endcode-block -->

…or use the GitHub Action's `servers:` input.

## Logging

### See per-server logs

```text
docker compose logs -f opcua-no-security
```

`-f` follows; remove for a snapshot.

### See all servers' logs

```text
docker compose logs -f
```

Prefix shows which service each line is from.

### Increase log verbosity

UA-.NETStandard uses standard .NET logging. The `LogLevel`
adjusts what's emitted:

```text
environment:
  Logging__LogLevel__Default: "Debug"
```

This shows every service call, every node access. Useful for
deep protocol diagnosis; noisy for normal operation.

For the PubSub publisher:

```text
environment:
  OPCUA_LOG_LEVEL: "Debug"
```

## Reset to clean state

If everything's confused:

<!-- @code-block language="bash" label="full reset" -->
```bash
docker compose down -v   # remove containers + anonymous volumes
rm -rf ./certs           # wipe certs
docker compose build --no-cache
docker compose up -d
```
<!-- @endcode-block -->

This takes ~5 minutes but always works.

## Where to read next

- [Top of docs](../index.md).
- The [GitHub issue tracker](https://github.com/php-opcua/uanetstandard-test-suite/issues)
  for bugs not covered here.

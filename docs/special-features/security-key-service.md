---
eyebrow: 'Docs · Special features'
lede:    'The OPC UA Part 14 §8.4.2 GetSecurityKeys method, served by opcua-sks on port 4851. The test-only target for subscriber-side PubSub key clients.'

see_also:
  - { href: './pubsub-publisher.md',                         meta: '5 min' }
  - { href: '../server-instances/special-purpose.md',         meta: '5 min' }

prev: { label: 'Views',  href: './views.md' }
next: { label: 'PubSub publisher', href: './pubsub-publisher.md' }
---

# Security Key Service

The `opcua-sks` server (port 4851) is a regular OPC UA server
with one extra feature: it exposes the **`GetSecurityKeys`**
method from OPC UA Part 14 §8.4.2. PubSub subscribers use this
RPC to fetch the symmetric group keys they need to verify and
decrypt secured PubSub messages.

## What it serves

| Property                | Value                                          |
| ----------------------- | ---------------------------------------------- |
| Endpoint                | `opc.tcp://localhost:4851/UA/TestServer`       |
| Policy                  | `None`                                         |
| Mode                    | `None`                                         |
| Auth                    | Anonymous                                      |
| Object NodeId           | `ns=1;s=TestServer/SecurityKeyService`         |
| Method NodeId           | `ns=1;s=TestServer/SecurityKeyService/GetSecurityKeys` |

## Method signature

<!-- @code-block language="text" label="GetSecurityKeys signature" -->
```text
GetSecurityKeys(
    securityGroupId:    String,
    startingTokenId:    UInt32,
    requestedKeyCount:  UInt32,
) returns (
    securityPolicyUri:  String,
    firstTokenId:       UInt32,
    keys:               ByteString[],
    timeToNextKey:      Duration,
    keyLifetime:        Duration,
)
```
<!-- @endcode-block -->

## Default behaviour

For the configured group (`OPCUA_SKS_GROUP_ID`, default
`test-group`):

| Output            | Default                                              |
| ----------------- | ---------------------------------------------------- |
| `securityPolicyUri` | `http://opcfoundation.org/UA/SecurityPolicy#PubSub-Aes256-CTR` |
| `firstTokenId`    | `7`                                                  |
| `keys[0]`         | 68 bytes: 32 × `0x01` + 32 × `0x02` + `03 03 03 03`   |
| `timeToNextKey`   | 300 000 ms (5 minutes)                                |
| `keyLifetime`     | 600 000 ms (10 minutes)                               |

`requestedKeyCount` is honored: with `requestedKeyCount=3`,
returns three identical 68-byte ByteStrings (in this test
implementation the same key repeats — real SKS rotate them).

For unknown `securityGroupId`, the method returns `Bad_NotFound`.

## Key layout — PubSub-Aes256-CTR

Each `keys[i]` ByteString is the concatenation of:

| Bytes    | Field                            | Length |
| -------- | -------------------------------- | ------ |
| `0..31`  | HMAC-SHA256 signing key          | 32     |
| `32..63` | AES-256 encrypting key           | 32     |
| `64..67` | Key nonce                        | 4      |

Total: **68 bytes**.

Subscriber-side code splits the ByteString and feeds each
fragment to the appropriate crypto primitive. The suite's
defaults are deliberately simple (32 × `0x01`, etc.) so failing
crypto operations are easy to debug.

## Environment-driven overrides

| Variable                          | Default                                              | Effect                          |
| --------------------------------- | ---------------------------------------------------- | ------------------------------- |
| `OPCUA_ENABLE_SKS`                | `true` on this service                                | Enables the SKS builder         |
| `OPCUA_SKS_GROUP_ID`              | `test-group`                                         | Accepted group ID                |
| `OPCUA_SKS_POLICY_URI`            | `…#PubSub-Aes256-CTR`                                | Returned policy URI              |
| `OPCUA_SKS_TOKEN_ID`              | `7`                                                  | Token ID of the current key      |
| `OPCUA_SKS_SIGNING_KEY_HEX`       | `01` × 32                                            | HMAC-SHA256 key (hex)            |
| `OPCUA_SKS_ENCRYPTING_KEY_HEX`    | `02` × 32                                            | AES-256 key (hex)                |
| `OPCUA_SKS_KEY_NONCE_HEX`         | `03030303`                                           | Key nonce (hex)                  |
| `OPCUA_SKS_TIME_TO_NEXT_KEY_MS`   | `300000`                                             | Time-to-next-key value           |
| `OPCUA_SKS_KEY_LIFETIME_MS`       | `600000`                                             | Key lifetime                     |

## Test patterns

### Happy path — fetch one key

```text
call(
  TestServer/SecurityKeyService,
  TestServer/SecurityKeyService/GetSecurityKeys,
  inputs = [
    "test-group",   # securityGroupId
    0,              # startingTokenId — ignored by this test impl
    1,              # requestedKeyCount
  ],
)

→ status = Good
→ outputs = [
    securityPolicyUri = "…#PubSub-Aes256-CTR",
    firstTokenId      = 7,
    keys              = [<68 bytes>],
    timeToNextKey     = 300_000.0,
    keyLifetime       = 600_000.0,
  ]
```

Split the 68-byte ByteString:

| Bytes    | Expected (defaults)        |
| -------- | -------------------------- |
| `0..31`  | All `0x01`                 |
| `32..63` | All `0x02`                 |
| `64..67` | `03 03 03 03`              |

### Negative path — unknown group

```text
call(..., inputs=["unknown-group", 0, 1])

→ status = Bad_NotFound
```

### Multiple keys

```text
call(..., inputs=["test-group", 0, 3])

→ keys = [<68 bytes>, <68 bytes>, <68 bytes>]    (3 identical)
```

In a real SKS the keys differ — here they're the same because
no real rotation exists.

## Limits — test-only

This is **not** a reference SKS. Specifically:

- **Single hardcoded group.** Only `OPCUA_SKS_GROUP_ID` is
  accepted. Unknown IDs return `Bad_NotFound`.
- **No caller authentication.** Anyone reachable on 4851 can
  fetch keys. Real SKS enforce that the caller is part of the
  group.
- **No rotation scheduling.** Keys are static — `firstTokenId`
  doesn't advance.
- **No revocation.** A compromised token is permanently
  compromised.
- **`startingTokenId` is ignored.** A real SKS returns keys
  starting at the requested token; this one always returns the
  current one.

If you're building a real SKS, this is fine to test the **wire
protocol** against. Not fine to copy into production.

## Integration with the PubSub publisher

The PubSub publisher (`opcua-pubsub`, UDP 14850) emits
**unsecured** UADP. The SKS provides keys for a hypothetical
**secured** publisher — currently not deployed in the suite. The
two services are complementary but not yet end-to-end joined.

A planned roadmap item is a secured publisher fed by the SKS's
keys. See the [project roadmap](https://github.com/php-opcua/uanetstandard-test-suite/blob/master/ROADMAP.md).

## Where to read next

- [PubSub publisher](./pubsub-publisher.md) — the unsecured UADP
  source.
- [Special-purpose servers](../server-instances/special-purpose.md) —
  context for SKS + discovery + PubSub.

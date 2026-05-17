---
eyebrow: 'Docs · Getting started'
lede:    'A 60-second walkthrough: start the suite, connect, browse, read, stop. The simplest happy path with the simplest server.'

see_also:
  - { href: './installation.md',                 meta: '3 min' }
  - { href: './first-connection.md',             meta: '4 min' }
  - { href: '../server-instances/overview.md',   meta: '5 min' }

prev: { label: 'Installation',  href: './installation.md' }
next: { label: 'First connection', href: './first-connection.md' }
---

# Quick start

End-to-end, ~60 seconds, no security.

## 1. Start

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose up -d
```
<!-- @endcode-block -->

Wait for `docker compose ps` to show all services running.

## 2. Connect

The simplest server has **no security and anonymous access**:

```text
endpoint:        opc.tcp://localhost:4840/UA/TestServer
security policy: None
security mode:   None
identity:        Anonymous
```

Any OPC UA client library can connect with one line. Examples
below.

### Using `opcua-cli` (or any UA inspector)

<!-- @code-block language="bash" label="terminal — quick browse" -->
```bash
opcua-cli browse opc.tcp://localhost:4840/UA/TestServer ns=0;i=85
```
<!-- @endcode-block -->

You should see `TestServer` as a child of the Objects folder.

### Using `php-opcua/opcua-client`

<!-- @code-block language="php" label="example.php" -->
```php
use PhpOpcua\Client\OpcuaClient;

$client = new OpcuaClient('opc.tcp://localhost:4840/UA/TestServer');
$client->connect();

$dv = $client->read('ns=1;s=TestServer/DataTypes/Scalar/BooleanValue');
echo $dv->value;          // true

$client->disconnect();
```
<!-- @endcode-block -->

### Using node-opcua

<!-- @code-block language="text" label="example.js" -->
```text
const { OPCUAClient } = require('node-opcua');

const client = OPCUAClient.create({ endpointMustExist: false });
await client.connect('opc.tcp://localhost:4840/UA/TestServer');
const session = await client.createSession();

const dv = await session.read({ nodeId: 'ns=1;s=TestServer/DataTypes/Scalar/BooleanValue' });
console.log(dv.value.value);

await session.close();
await client.disconnect();
```
<!-- @endcode-block -->

### Using `opcua` (Rust)

<!-- @code-block language="text" label="example.rs (sketch)" -->
```text
let mut client = ClientBuilder::new().application_name("quickstart").client().unwrap();
let session = client.connect_to_endpoint(
    ("opc.tcp://localhost:4840/UA/TestServer", "None"),
    IdentityToken::Anonymous,
)?;

let result = session.read(
    &[NodeId::new(1, "TestServer/DataTypes/Scalar/BooleanValue").into()],
    TimestampsToReturn::Both,
    0.0,
)?;
```
<!-- @endcode-block -->

## 3. Drive the test surface

A few representative reads to confirm everything's wired:

| Browse path                                                | Type    | Notes                          |
| ---------------------------------------------------------- | ------- | ------------------------------ |
| `TestServer/DataTypes/Scalar/BooleanValue`                  | Boolean | RW, initial value `true`      |
| `TestServer/DataTypes/Scalar/Int32Value`                    | Int32   | RW, `-100000`                 |
| `TestServer/DataTypes/Array/Int32Array`                     | Int32[] | `[-1000, 0, 1000]`            |
| `TestServer/Dynamic/Counter`                                | UInt32  | 1 Hz; first observed value is `1` (initial `0` is incremented before publish) |
| `TestServer/Methods/Add`                                    | Method  | Inputs `a, b`, returns `a+b`  |

Try a write and read-back:

<!-- @code-block language="php" label="write / read-back" -->
```php
$node = 'ns=1;s=TestServer/DataTypes/Scalar/Int32Value';
$client->write($node, 42);
echo $client->read($node)->value;   // 42
```
<!-- @endcode-block -->

…or a method call:

<!-- @code-block language="php" label="method call" -->
```php
$result = $client->callMethod(
    'ns=1;s=TestServer/Methods',
    'ns=1;s=TestServer/Methods/Add',
    [2.5, 3.5],
);
// $result[1][0] === 6.0
```
<!-- @endcode-block -->

## 4. Subscribe

A subscription on the `Counter` is the cheapest way to confirm
the publish loop:

<!-- @code-block language="php" label="subscription" -->
```php
$sub = $client->subscribe(publishingInterval: 1000, onData: function ($node, $dv) {
    echo "{$node} = {$dv->value}\n";
});
$sub->monitor('ns=1;s=TestServer/Dynamic/Counter');
$sub->run();   // Ctrl+C to stop
```
<!-- @endcode-block -->

You'll see the counter tick every second.

## 5. Stop

<!-- @code-block language="bash" label="terminal" -->
```bash
docker compose down
```
<!-- @endcode-block -->

## Where to read next

- [First connection](./first-connection.md) — connect with
  username/password and with a client certificate.
- [Server instances · Overview](../server-instances/overview.md) —
  pick the right server for what you want to test.
- [Testing patterns · Basic tests](../testing-patterns/basic-tests.md) —
  the test recipes most libraries reach for first.

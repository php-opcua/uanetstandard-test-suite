---
eyebrow: 'Docs · Runtime features'
lede:    'Six FileType nodes plus one FileDirectoryType under TestServer/Files — covers the full OPC UA Part 5 service surface: read, write, append, role-protected write, directory create/delete/move-or-copy. MemoryStream-backed, reset on container restart.'

see_also:
  - { href: '../reference/ports-and-endpoints.md',          meta: '5 min' }
  - { href: '../testing-patterns/basic-tests.md',           meta: '5 min' }
  - { href: 'https://reference.opcfoundation.org/Core/Part5/v105/docs/C', meta: 'external', label: 'OPC UA Part 5 — File Transfer' }

prev: { label: 'Historical data', href: './historical-data.md' }
next: { label: 'Views',           href: '../special-features/views.md' }
---

# File Transfer (Part 5)

Added in v1.3.0. The suite exposes a folder `TestServer/Files`
containing **six `FileType` nodes** plus one **`FileDirectoryType`**
that exercise the entire OPC UA File Transfer service surface
(`Open`, `Close`, `Read`, `Write`, `GetPosition`, `SetPosition`,
`CreateDirectory`, `CreateFile`, `DeleteFileSystemObject`,
`MoveOrCopy`) plus the standard properties (`Size`, `Writable`,
`UserWritable`, `OpenCount`, `MimeType`).

All files are backed by per-process `MemoryStream`s:
- No disk I/O, no volumes, no mount setup.
- Every `docker compose restart` (or container kill) resets all six
  files to their initial seed and clears every runtime-created child
  of `RootDir`.
- The `OpenCount` reflects live handles only.

## The six files

Path: `TestServer / Files`

| NodeId                                          | Type      | Initial size | Writable | MimeType                   | Notes                                                  |
| ----------------------------------------------- | --------- | ------------ | -------- | -------------------------- | ------------------------------------------------------ |
| `ns=1;s=TestServer/Files/ReadOnlyFile`          | `FileType` | 1024 B       | `false`  | `application/octet-stream` | Deterministic seed (MD5("readonly-seed") × 64)         |
| `ns=1;s=TestServer/Files/EmptyFile`             | `FileType` | 0 B          | `false`  | `text/plain`               | Empty by design — `Read` returns `ByteString(0)`       |
| `ns=1;s=TestServer/Files/LargeFile`             | `FileType` | 256 KB       | `false`  | `application/octet-stream` | Bytes `0,1,2,…,255` repeated 1024 times — forces chunked `Read` |
| `ns=1;s=TestServer/Files/WritableFile`          | `FileType` | 0 B          | `true`   | `application/octet-stream` | Round-trip target: `Open(Write)` → `Write` → `Close` → re-`Open(Read)` |
| `ns=1;s=TestServer/Files/AppendableFile`        | `FileType` | 13 B (`"seed-content\n"`) | `true` | `text/plain`        | Target for `Open(Write \| Append)` — initial cursor at EOF |
| `ns=1;s=TestServer/Files/ProtectedWritableFile` | `FileType` | 0 B          | `true` (advertised) | `application/octet-stream` | `Open(Write)` requires `admin` role; other roles → `Bad_UserAccessDenied` |

Plus the directory:

| NodeId                                  | Type                 | Children                                 |
| --------------------------------------- | -------------------- | ---------------------------------------- |
| `ns=1;s=TestServer/Files/RootDir`       | `FileDirectoryType`  | Initially empty; populated by `CreateDirectory` / `CreateFile` calls |

Namespace `ns=1` resolves to `urn:opcua:testserver:nodes` (the
standard test namespace). Resolve the index at runtime via
`NamespaceArray` — `ns=1` is the typical allocation, not a guarantee.

## Open modes (Part 5 §C.2.1)

The `Open(Byte mode)` method accepts a bit field:

| Bit | Name             | Meaning                                          |
| --- | ---------------- | ------------------------------------------------ |
| `1` | `Read`           | Handle can be passed to `Read`                   |
| `2` | `Write`          | Handle can be passed to `Write`                  |
| `4` | `EraseExisting`  | Truncate the file at open time (requires `Write`) |
| `8` | `Append`         | Initial position = end-of-file                   |

The fixture enforces these invariants:

| Scenario                                                       | Result                                              |
| -------------------------------------------------------------- | --------------------------------------------------- |
| `Open(0)` — neither Read nor Write                             | `Bad_InvalidArgument`                                |
| `Open(2)` on a `Writable=false` file                           | `Bad_NotWritable`                                    |
| `Open(4)` — `EraseExisting` without `Write`                    | `Bad_InvalidArgument`                                |
| `Open(2 \| 4)` on `WritableFile` / `AppendableFile`             | Truncates atomically; new handle starts at position 0 |
| `Open(2 \| 8)` on `AppendableFile`                              | Handle starts at position 13 (end-of-seed)            |
| `Read` on a handle opened without `Read` bit                   | `Bad_InvalidState`                                   |
| `Write` on a handle opened without `Write` bit                 | `Bad_InvalidState`                                   |
| Any operation on an unknown `fileHandle`                       | `Bad_InvalidArgument`                                |
| `Read` past EOF                                                | Short-read; returns fewer bytes (or empty), no error |
| `Open(Write)` on `ProtectedWritableFile`, anonymous session    | `Bad_UserAccessDenied`                               |
| `Open(Write)` on `ProtectedWritableFile`, `admin` userpass     | Good, handle valid                                   |
| `Open(Read)` on `ProtectedWritableFile`, any session           | Good                                                  |

## Round-trip pattern for `WritableFile`

```text
1.  fileHandle1 = Open(WritableFile, mode = Write | EraseExisting)
2.  Write(fileHandle1, payload)
3.  Close(fileHandle1)
4.  fileHandle2 = Open(WritableFile, mode = Read)
5.  assert Read(fileHandle2, length = payload.Length) == payload
6.  Close(fileHandle2)
```

## Append pattern for `AppendableFile`

```text
1.  fileHandle = Open(AppendableFile, mode = Write | Append)
2.  assert GetPosition(fileHandle) == 13          # past the seed
3.  Write(fileHandle, additional_bytes)
4.  Close(fileHandle)
5.  fileHandle2 = Open(AppendableFile, mode = Read)
6.  full = Read(fileHandle2, 1024)
7.  assert full[:13] == b"seed-content\n"
8.  assert full[13:]  == additional_bytes
9.  Close(fileHandle2)
```

## Chunked-read pattern for `LargeFile`

```text
1.  handle = Open(LargeFile, mode = Read)
2.  chunkSize = 8192            # any value smaller than the file
3.  collected = []
4.  loop:
       chunk = Read(handle, chunkSize)
       if chunk is empty: break
       collected.append(chunk)
5.  Close(handle)
6.  assert len(collected) == 32  # 256K / 8K
7.  assert collected.flatten()[i] == i % 256  for every i
```

## Per-session permission pattern for `ProtectedWritableFile`

```text
# Session A — anonymous on opcua-no-security (4840)
1.  client_a.Open(ProtectedWritableFile, mode = Read)   → Good
2.  client_a.Open(ProtectedWritableFile, mode = Write)  → Bad_UserAccessDenied

# Session B — username=admin password=admin123 on opcua-userpass (4841)
3.  client_b.Open(ProtectedWritableFile, mode = Write)  → Good (handle)
4.  client_b.Write(handle, ...)                          → Good
5.  client_b.Close(handle)                               → Good

# Session C — username=viewer password=viewer123 on opcua-userpass (4841)
6.  client_c.Open(ProtectedWritableFile, mode = Write)  → Bad_UserAccessDenied
```

The check happens **server-side inside the Open callback** — the
file's `Writable` property still reads `true` for everyone (that
property advertises the abstract capability, not the per-session
authorization).

## `FileDirectoryType` — `RootDir`

`RootDir` exposes the four standard methods (Part 5 §C.3):

| Method                  | Inputs                                                | Outputs                          |
| ----------------------- | ----------------------------------------------------- | -------------------------------- |
| `CreateDirectory`       | `String directoryName`                                 | `NodeId directoryNodeId`         |
| `CreateFile`            | `String fileName, Boolean requestFileOpen`            | `NodeId fileNodeId, UInt32 fileHandle` |
| `DeleteFileSystemObject`| `NodeId objectToDelete`                                | —                                |
| `MoveOrCopy`            | `NodeId source, NodeId targetDir, Boolean createCopy, String newName` | `NodeId newNodeId` |

Behaviour:

- **Children are anonymous-writable** — `CreateFile` returns a
  FileType node with `Writable=true`. Use `RootDir` for round-trip
  scenarios that don't fit `WritableFile` (multiple files,
  hierarchies, move/copy).
- **`CreateFile` with `requestFileOpen=true`** returns a fileHandle
  in Read+Write mode (mode = 3) alongside the new NodeId — spares
  a separate `Open` round-trip.
- **`MoveOrCopy`** supports both `createCopy=true` (deep-clones
  the file content via the backend's snapshot) and
  `createCopy=false` (relinks: source NodeId removed, destination
  NodeId returned). Sub-directories follow the same semantics;
  their children are a shallow copy when copied.
- **`DeleteFileSystemObject`** removes the node from the address
  space and drops the in-process backend for files. Open handles
  on the deleted file are not auto-closed; subsequent operations
  against them return `Bad_InvalidArgument`.
- **Empty name** in `CreateDirectory` / `CreateFile` →
  `Bad_InvalidArgument`. **Unknown NodeId** in
  `DeleteFileSystemObject` / `MoveOrCopy` → `Bad_NodeIdUnknown`.

### CreateFile + round-trip example

```text
1.  (newNodeId, handle) = RootDir.CreateFile("data.bin", requestFileOpen=true)
2.  Write(handle, payload)
3.  Close(handle)
4.  readHandle = Open(newNodeId, mode = Read)
5.  assert Read(readHandle, len(payload)) == payload
6.  Close(readHandle)
7.  RootDir.DeleteFileSystemObject(newNodeId)
```

### MoveOrCopy with copy

```text
1.  (src, h) = RootDir.CreateFile("src.bin", requestFileOpen=true)
2.  Write(h, b"hello")
3.  Close(h)
4.  dst = RootDir.MoveOrCopy(src, RootDir, createCopy=true, newName="copy.bin")
5.  hr = Open(dst, mode = Read)
6.  assert Read(hr, 5) == b"hello"
7.  Close(hr)
# src still exists at this point — close anyway
```

## Property reads

Standard `FileType` properties on every file:

| Property         | Type     | Initial value                                          |
| ---------------- | -------- | ------------------------------------------------------ |
| `Size`           | `UInt64` | byte count of the current backing stream (updated on every `Write`) |
| `Writable`       | `Boolean`| as in the table above (advertised capability)         |
| `UserWritable`   | `Boolean`| same as `Writable` — the per-user check on `ProtectedWritableFile` is enforced **inside the Open callback**, not via this property |
| `OpenCount`      | `UInt16` | live handle count (updated on every `Open`/`Close`)   |
| `MimeType`       | `String` | per the table — `application/octet-stream` or `text/plain` |

## Investigated and skipped — `FileChange` / `FileAccess` events

UA-.NETStandard `1.5.378.134` does **not** ship the dedicated
`FileChangeEventType` / `FileAccessEventType` classes that Part 5
§8.2 defines. The available file-related event types in that release
are `AuditUpdateEventType` / `AuditUpdateMethodEventType` /
`AuditUpdateStateEventType` — audit semantics, not the data-change
notification semantics §8.2 calls for.

The suite intentionally does **not** emit these events:

- Emitting custom vendor-specific event types would defeat the
  fixture's role (clients would have to know about the custom type).
- Emitting `AuditUpdate*` types as a substitute would conflate
  audit logs with file-change notifications, again forcing
  client-side workarounds.

The right time to revisit this is when a future UA-.NETStandard
release adds the standard `FileChangeEventType` /
`FileAccessEventType` classes.

## Enabling / disabling

On by default on every server instance. Disable with:

```yaml
environment:
  OPCUA_ENABLE_FILE_TRANSFER: "false"
```

Disabling skips the entire `TestServer/Files` folder construction —
none of the seven nodes (six files plus the directory) will exist
in the address space.

## Reset semantics

Each file and the directory tree reset on the events below:

| Event                                          | What resets                                          |
| ---------------------------------------------- | ---------------------------------------------------- |
| `docker compose restart <service>`             | All six files back to initial seed; every runtime-created child of `RootDir` removed; every `OpenCount = 0` |
| Container kill / process crash                 | Same                                                  |
| `Close()` of the last handle                   | **Nothing** — backing stream persists between opens   |
| Session drop with open handles                 | Handles leak until restart (no session-bound cleanup) |
| `Open(Write \| EraseExisting)` on a writable file| That file only, truncated to 0 bytes                |
| `RootDir.DeleteFileSystemObject(child)`        | That child only (node removed; backend dropped)      |
| Session-bound test isolation                   | Test must explicitly truncate / delete, or restart the container between tests |

The three read-only files (`ReadOnlyFile`, `EmptyFile`, `LargeFile`)
cannot be mutated under any condition — their seeds are reasserted
on every container start.

## Implementation reference

The fixture is built by
`src/TestServer/AddressSpace/FileTransferBuilder.cs` (~480 lines C#).
Uses UA-.NETStandard's `FileState` and `FileDirectoryState` as the
type wrappers, exposes one in-process `InMemoryFileBackend` per file
plus a per-directory dictionary of dynamically-created child node IDs
and backends. The backend is thread-safe via a single lock —
UA-.NETStandard dispatches method `OnCall` from its session worker
pool, so concurrent handle access is real but correctly serialised.

`TestNodeManager` was extended with a `DeleteDynamicNode(NodeId)`
helper that wraps the framework's protected `DeleteNode(SystemContext, NodeId)`
so the `DeleteFileSystemObject` / `MoveOrCopy` callbacks can drop
nodes from the address space.

The builder is conditionally wired in
`TestNodeManager.CreateAddressSpace` between the `Methods` and
`Dynamic` builders.

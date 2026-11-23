# UiPath FTP Activities (extended)

`UiPath.FTP.Activities` (extended package). It depends on the base package `UiPath.FTP.Core.Activities`, which contains all the activities and the coded workflow API.

## What this package is

- The activities, the coded workflow API, their properties and XAML are those of `UiPath.FTP.Core.Activities`. They are documented in this folder as well: see the [activity documents](activities/) and [coded-api.md](coded-api.md). Nothing in a workflow changes when you use this package instead of the base one.
- This package adds the SFTP algorithms that the operating system cryptography module does not provide. They are implemented with BouncyCastle (`UiPath.FTP.Activities.Extended.dll`) and are picked up automatically for every SFTP connection; there is no setting.
- It keeps the id `UiPath.FTP.Activities`, so projects that already reference it keep working as before after an upgrade.

## Added SFTP algorithms

| Area | Added |
|------|-------|
| Key exchange | `curve25519-sha256` (and `@libssh.org`), `diffie-hellman-group-exchange-sha256`, `diffie-hellman-group16-sha512`, `diffie-hellman-group14-sha256`, `diffie-hellman-group-exchange-sha1`, `diffie-hellman-group14-sha1`, `diffie-hellman-group1-sha1` |
| Host and client keys | `ssh-ed25519` (host and client key), `ssh-dss` (server host key only) |
| Ciphers | `blowfish-cbc`, `twofish-cbc`, `twofish128/192/256-cbc`, `cast128-cbc`, `arcfour`, `arcfour128`, `arcfour256` |
| MACs | `hmac-md5`, `hmac-md5-96`, `hmac-sha1-96`, `hmac-sha2-256-96`, `hmac-sha2-512-96` (and the `-etm` variants of the MD5 and SHA-1 ones) |
| Private key files | OpenSSH-format keys encrypted with bcrypt, Ed25519 keys |

The operating system algorithms of the base package stay first in the negotiation; these are added after them, legacy last.

## Not supported (also not in this package)

Compared with `UiPath.FTP.Activities` 3.1.3 the following are no longer available, because the underlying SSH library no longer provides them:

- **DSA client keys** (private key files of type `ssh-dss`, PEM or OpenSSH format). A project that authenticates with a DSA key must switch to an RSA, ECDSA or Ed25519 key. DSA host keys of servers (`ssh-dss`) are still verified.
- PEM private keys encrypted with single DES (`DES-CBC`). Re-encrypt the key with AES, for example `ssh-keygen -p -m PEM -f <key>`.
- The packages target `net8.0` and `net8.0-windows` only (3.1.3 also targeted `net6.0`).

## Not limited to operating system cryptography

A connection that negotiates one of the added algorithms uses BouncyCastle, not the operating system module. If only operating system cryptography is allowed, use `UiPath.FTP.Core.Activities` alone; this extended package is not suitable for that environment.

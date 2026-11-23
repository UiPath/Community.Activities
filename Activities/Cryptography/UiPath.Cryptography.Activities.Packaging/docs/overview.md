# UiPath Cryptography Activities: PGP extension

`UiPath.Cryptography.Activities`

This package is the **extension** of `UiPath.Cryptography.Core.Activities`. It adds the OpenPGP implementation (PgpCore on BouncyCastle) that the core package needs for its PGP features. OpenPGP has no operating system equivalent, so this package is not limited to operating system cryptography. Projects that must use operating system cryptography only reference `UiPath.Cryptography.Core.Activities` and have no PGP.

The package contains no activities of its own. It depends on the exact same version of `UiPath.Cryptography.Core.Activities`, so adding or upgrading this package brings the activities, the coded workflow API and their documentation from the core package. Projects that already listed `UiPath.Cryptography.Activities` keep working unchanged after an upgrade, including PGP.

## What the extension enables

| Feature | Where it lives |
|---------|----------------|
| PGP Generate Keys, PGP Sign File, PGP ClearSign File, PGP Verify | Activities of the core package |
| `PGP` algorithm of Encrypt Text, Encrypt File, Decrypt Text, Decrypt File | Activities of the core package |
| PGP methods of the `cryptography` coded workflow service (`PgpPublicKey`, `PgpPrivateKey`, `PgpKeyPair`) | Coded workflow API of the core package |

The activity and API documentation is in the documentation of `UiPath.Cryptography.Core.Activities`.

## Without the extension

With only `UiPath.Cryptography.Core.Activities` in the project, the PGP activities stay visible but fail at run time, and so do PGP calls from coded workflows and the `PGP` algorithm choice, with `InvalidOperationException`: "PGP is not available ... need the UiPath.Cryptography.Activities package (the extended package)". Add this package to fix it.

## How the core package finds the extension

The extension assembly `UiPath.Cryptography.Activities.Extended.dll` declares its provider with the `PgpProvider` assembly attribute. The core package looks for it among the loaded assemblies and then loads it by name; a missing extension is never cached, so installing it takes effect on the next call. If the extension is installed but cannot be loaded (for example a version mismatch), the exception carries the cause as its inner exception.

## Versions

Both packages are built together and the extension pins the exact version of the core package. Upgrade them together.

## Compliance note

`UiPath.Cryptography.Core.Activities` passes the operating system crypto scan. This package contains `PgpCore` and `BouncyCastle.Cryptography` and is flagged by that scan by design; do not use it where only operating system cryptography is allowed.

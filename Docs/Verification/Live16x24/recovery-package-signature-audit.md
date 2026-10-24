# Unity package-signature diagnosis — 2026-10-24

## Finding

The installed/current editor is Unity `6000.3.4f1`. Local cached Package Manager verification records exactly the ten reported packages failing with `ERR_SIGNED_ATTESTATION_VALIDATION_FAILED` and signer-certificate time validity failure. This matches Unity’s documented old-editor signature-verification defect, including certificate expiry handling. It is strong evidence for an editor verifier issue; this audit did not independently verify package tarball cryptography.

## Local evidence

- `ProjectSettings/ProjectVersion.txt`: 6000.3.4f1 (fdd3b8998ec4); only that editor is installed under `/Applications/Unity/Hub/Editor`.
- `Packages/manifest.json` has no scoped registries; all ten locked dependencies use `source: registry` and `https://packages.unity.com`.
- `Library/PackageManager/ProjectCache` contains ten error records. Each reports a signer certificate that is expired or not yet valid. File mtime is 2026-10-16 20:23:37 local; affected extracted packages also predate today’s recovery. This failure was already cached before today’s cleanup.
- Current `~/Library/Logs/Unity/upm.log` has no signature/transfer-failure records. It successfully downloaded official editor metadata on today’s startup. Editor-prev.log documents actual disk-full Burst compiler failures, a separate condition.

| Package | Version | ProjectCache error line |
| --- | --- | ---: |
| `com.unity.inputsystem` | `1.17.0` | 1568 |
| `com.unity.settings-manager` | `2.1.1` | 6584 |
| `com.unity.splines` | `2.8.2` | 6725 |
| `com.unity.collections` | `2.6.2` | 7153 |
| `com.unity.nuget.newtonsoft-json` | `3.2.2` | 7267 |
| `com.unity.burst` | `1.8.27` | 7517 |
| `com.unity.mathematics` | `1.3.3` | 7629 |
| `com.unity.searcher` | `4.9.4` | 7740 |
| `com.unity.nuget.mono-cecil` | `1.11.6` | 7846 |
| `com.unity.test-framework.performance` | `3.2.0` | 8015 |

## Official corroboration

- [Unity6.3 package-signature manual](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/upm-signature-lp/upm-signature) explicitly identifies false invalid-signature reports in builds older than6000.3.5f2, and advises a patched editor.
- [UUM-133005](https://issuetracker.unity3d.com/issues/4648) describes the identical cached error code and certificate-time failure; the search-visible official issue record lists6000.3.7f1 as fixed for6.3. The manual and issue tracker give different earliest patch versions; choose6000.3.7f1 or later within6.3 to cover both, rather than claiming5f2 is independently verified for this precise case.

## Minimal safe remedy

1. Preserve the current working editor/project; use a patched6.3 editor at or after6000.3.7f1 and retain the existing package manifest/lock versions. Avoid an unrelated major-version migration or updating ten dependencies merely to clear the indicator.
2. Let that editor re-resolve/revalidate, then inspect the ten signature results. If stale verification persists, use the supported Package Manager refresh/re-resolution flow before considering narrowly regenerating verification metadata. No blanket PackageCache/global cache deletion is warranted by these records.
3. Only if a patched verifier still reports a content/signature error should an affected package be downloaded again from the official registry and rechecked. Do not disable signature validation or alter the system clock.

## Limits / operations

No Unity, UI, launch arguments, credential stores, cache mutations/deletions, source edits or package changes were made. Root retains editor ownership. A new editor install needs disk space and is not necessary to pretend today’s gameplay capture has already been verified. Current cryptographic package validity was not independently established by this bounded read-only diagnosis.

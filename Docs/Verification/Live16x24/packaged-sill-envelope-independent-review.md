# Sill envelope package — independent integrity review

Passed, 2026-10-27T04:54:34.241819+00:00. Reviewer: `build_comparison_plan`.
Read-only archive audit using separate inline verification code; the packaging
helper was not used to verify the delivered archive. No extraction, executable
launch, Unity, Git, build changes or save changes. Only this receipt was written.

Source asserted by the root build operator: `246bda61e67d58d147e87e781737f32c54d25aa2`.
The matching receipts and package identify that revision; this review does not
independently reproduce source-to-binary provenance.

## Delivered archive

[Linux archive](../../../Builds/SteamDeck/2026-10-27-sill-envelope-246bda61e-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz)

- Size: **128,678,885 bytes**.
- SHA256: `6994a7ebbc92f6521a3ea1192c6cf72c32a22b24cf5b3dac64dbbec7f1a918dc`; external sidecar matches.
- **195 members: 182 regular files and 13 directories**;
  225,392,964 unpacked file bytes. Every file hash, size and mode, plus every
  directory mode, matches the [package receipt](packaged-sill-envelope-linux-archive.json)
  and the current Linux output tree.
- Exactly one `CavesOfOoo-current` top level; no duplicates, traversal, links,
  devices or unexpected entries. Gzip was read fully to EOF, checking CRC,
  uncompressed size and footer as well as all tar payloads.
- Embedded `SHA256SUMS` covers all **181 other files** exactly.
  Launcher and player are0755. Player header is little-endian ELF64 x86-64;
  the executable was not run.
- Six original credit sections match their source files and the combined
  `AUDIO-CREDITS.txt`; all five license texts, 5 audio provenance
  files, starter-spell sources, launcher and Deck README match current sources.
- Embedded build-info matches its external counterpart and archive receipt.
  Embedded Mac/Linux reports match the original report bytes and hashes.

## Build and native evidence

| Receipt | Result | Warnings | Fresh job |
| --- | --- | ---: | --- |
|[Mac](packaged-sill-envelope-mac-build.json)|Succeeded,0 errors|5|`build-649d17a2ee`|
|[Linux](packaged-sill-envelope-linux-build.json)|Succeeded,0 errors|4|`build-ef21d32718`|

Both jobs started after the asserted source-freeze boundary
`2026-10-27T04:46:49Z`; each names the expected fresh output under
`2026-10-27-sill-envelope-246bda61e-current`. Successful build receipts do not
prove execution or absence of runtime errors.

Previously recorded native receipts were parsed and hashed; no tests were
rerun for this package audit. Sill final136/136 and portable equipment final222/222
share52 identities, giving **306 distinct final-suite identities**. The earlier
224/224 regression receipt adds130 more identities:436 in the three-receipt
union,582 execution occurrences. Earlier regression coverage is nonadditive and
is not represented as another final-source run. All recorded cases pass with
zero skips/failures/inconclusive cases.

## Archive-verifier counterchecks

A separate, root-authorized scratch check exercised the packaging helper's
archive-verifier function before delivery. It used only tiny synthetic files;
packaging main was not called and actual build outputs were untouched.

| Synthetic input | Observed result |
| --- | --- |
|Valid archive|Accepted,4 members|
|Same tar payload with one gzip CRC bit flipped|Rejected: `BadGzipFile`|
|Same valid archive with the final4 footer bytes removed|Rejected: `EOFError`|

All3 checks passed. [Retained scratch receipt](packaged-sill-envelope-archive-counterchecks.json)
SHA256: `d729e642065315745af23602cbdacb8bf6d94d61cee71f04f5b6434ab4a102a9`.
Tested helper SHA256: `e3ab1d48d475006a61d22c0517e89d8a07ec89cf9e024e952c0e9592e900013b`.
The delivered archive received its own independent full gzip/tar verification
above; synthetic counterchecks are not substitutes for that audit.

## Limits

No Linux execution or physical Steam Deck testing occurred. No controls,
performance, audio quality, whole-game visual coverage or source reproducibility
claim is made. Mac ordinary-play evidence is a separate root-owned gate. Prior
packages were not modified by this audit; their historical hashes were not
re-audited here. No actionable packaging-integrity finding remains.

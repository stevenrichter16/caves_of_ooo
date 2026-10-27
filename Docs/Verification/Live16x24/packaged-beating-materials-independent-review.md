# Independent Beating package review

Result: **PASS; no concrete package-integrity blocker found.**

- Frozen runtime: `2e5a0ca4cb08d225cbf07b7db0bf28da53d264dc`. Accepted Linux job `build-11e299a6c2` has the correct `CavesOfOoo.x86_64` path. The earlier misnamed job is retained separately and is not the embedded build report. Mac job `build-11c5c0a35a` reports zero errors and five warnings; corrected Linux reports zero errors and zero warnings. Both start after the declared 07:24:30Z boundary.
- Archive: `/Users/steven/caves-of-ooo/Builds/SteamDeck/2026-10-27-beating-materials-2e5a0ca4c-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`. Size **128,681,400 bytes**, SHA256 `09354fd2e13f73cd3e7cf7e33a12c63ac32dd988bb745902dc1fbc3bb12d87e7`. Sidecar matches.
- Independently streamed **191 members: 181 regular files and 10 directories**, totaling **225,171,771 file bytes**. Every file hash, size and permission and every directory mode matches the receipt and current unpacked package. One safe `CavesOfOoo-current` root; no duplicates, links, traversal or special entries. Executable and launcher are 0755. ELF header is 64-bit little-endian x86-64.
- Internal `SHA256SUMS` covers exactly the other **180 files**. Internal, external and receipt build-info agree; embedded build reports match the exact accepted source receipts.
- A separate gzip read drained the wrapper to EOF, validating CRC, ISIZE and footer. Fresh in-memory integrity controls accepted valid gzip and rejected corrupt CRC and truncated footer; the actual archive was never modified.
- Six audio credit sections, five license texts, starter-spell sources and five audio provenance files match repository sources. Prior Sodden and Sill archive bytes and hashes remain unchanged.
- XML evidence re-read only: final Beating **95/95 passed**; earlier related regression **225 total, 222 passed, three allocation probes inconclusive**. No shared test identities: **320 distinct = 317 passed + 3 inconclusive**, zero failed. The regression predates the final road/dune correction and is not described as a new final-source run.

## Actual Mac capture review

Viewed all eight original packaged JPGs and checked each retained hash. The visible sequence corroborates the root's bounded standalone smoke record: normal save-detected startup; SaltPan Continue at31,13 with38/40 HP; ordinary one-cell movement to32,13; native portrait for uncovered sand; saved/reloaded32,13; BrineLens materials at40,12; and PaleSalt quantity1 in inventory. Warm sand, pale crust and teal basins remain legible.

The underfoot sand inspection in02 remains a glyph while the player covers the selected world owner;03 shows the uncovered sand's native portrait. This is limited reader parity, not full examination coverage. The PaleSalt inventory portrait is still a glyph, and camps, wells, containers, plants, actors and hazards retain known identity gaps. This accepts the bounded material smoke evidence, not whole-scene or whole-biome fullness. Screenshots corroborate the recorded route; they do not independently prove process lifecycle or every intermediate action.

## Limits

No files were extracted and no package, build output, Unity state, Git state or gameplay was mutated. Only this scratch review, its JSON companion and an independent scratch audit script were written. No new native tests were run. Linux was not executed, no physical Steam Deck was tested, and performance is not certified. Source/freeze binding is the root operator's assertion plus exact successful receipt provenance; this is not an independent reproducible-build proof.

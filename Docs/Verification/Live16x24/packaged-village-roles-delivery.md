# Shared village roles and sidearms: packaged delivery

Runtime source `0b888da80571bf35ab4f5348ff6e600d289340e7` is verified on origin/main. This batch delivers Farmer, Elder, VillageChild, Dagger, ShortSword and Mace using unchanged approved sprites. It closes bounded body/reader gaps; it does not certify complete regions or area polish.

## Build and native evidence

- One final native run passed **146 distinct cases**, with no failures/skips:24 village roles,24 residents,17 Weaponsmith,19 specialists,39 incidental actors and23 sidearms. The separate earlier sidearm108-case result remains historical, not additive to146.
- Mac job `build-e4e5b6d46c`:88.551s, zero errors/four warnings.
- Linux job `build-039cfbfa21`:86.229s, zero errors/four warnings.
- Build boundary:2026-10-28T09:00:23.035189Z. Both use the real SampleScene and production GameBootstrap.

Mac app: `Builds/Verification/2026-10-28-village-roles-0b888da80-current/CavesOfOoo.app`.

Linux directory: `Builds/SteamDeck/2026-10-28-village-roles-0b888da80-current/CavesOfOoo-current`.

The sibling `CavesOfOoo-SteamDeck-flat16x24-current.tar.gz` is128,763,314bytes, SHA256 `c023ae9399f8ff430ceced5df451e3d7ee9d1cdd97a27b1500307b66ddee96a2`. Every one of182 regular files and13 directory modes was compared; all181 internal checksums and gzip EOF/CRC passed. ELF64 x86-64 header and licenses were inspected. **Linux and physical Steam Deck were not executed.**

## Actual Mac play

Twelve unretouched window captures include the56px title bar. Five independent processes used preserved earned checkpoints, without combining their inventories or injecting gameplay state:

| Process | Earned input | Actual observation |
|---|---|---|
|13739|Village baseline154|All three native NPC bodies and exact readers; six normal steps through a door; F5 checkpoint156 at20,10, HP38, turn2140|
|14187|Packaged156|Fresh process Continue; all564,000 world pixels match saved capture05 against06|
|14491|Mace152|Purchased mace in inventory and F1 reader, HP38, purse6|
|15122|ShortSword153|Purchased short sword in inventory and F1 reader, HP38, purse9|
|15291|Dagger151|Ground dagger at60,3 with player61,3, plus selected exact reader, HP38, purse22|

The first process ended through Command-Q. Querying the closed app with CUA automatically launched process14187; root then pressed Continue normally. This is genuine fresh-process evidence, not a controlled command-line second launch. Its default Player.log was copied as run2 evidence. Subsequent quit actions queried the still-open Unity app to avoid another automatic launch.

Captures01–04 show village identities;05–06 show save/restart;07–10 show mace/short-sword inventory and readers;11–12 show the ground dagger and reader. The restart crop is `(0,96)-(940,696)` in1280×856 window images. The underlying gameplay viewport remains1280×800.

No new packaged combat, trading exchange, Farmer service check or cross-zone journey is claimed. Earlier Editor gameplay remains separately documented. The logs retain7/6/6/6/7 premature-thread notices and one ComputeBuffer-disposal warning per process. No clean-log or sustained-performance claim.

## Preservation and reproducibility

The source-bound snapshot `/private/tmp/coo-village-roles-build-protection-0b888da80` froze52,783 tracked objects and49 preexisting dirty paths. After all player processes stopped and Unity reported Play=false, compiling=false, updating=false and a clean scene, the reviewed restorer backed up/restored eight generated files: four metadata whitespace rewrites and four serialized build settings. All tracked snapshot objects then matched. Normal Editor saves/preferences and prior packages remained identical. The originally absent standalone save root was restored to absence by archiving the owned test tree; original standalone preferences were restored.

The new test checkpoint156 is committed under `Fixtures/shared-village-roles-packaged156`. Separate process branches are archived under `/private/tmp/coo-village-roles-packaged-run{2,3,4}-preserved`; the final branch is in `/private/tmp/coo-village-roles-postbuild-restored/standalone-session-Saves`. No cache or storage cleanup occurred. Free space at restoration was390,114,992,128bytes.

Reproduce from the named earned Quick pair, start the new Mac executable and press Continue. Use normal Look/examine for world owners, or I→Tab→Down→F1 for the two carried sidearms. Do not merge checkpoints152/153/151. The package-audit and restore wrappers pin their existing reviewed helpers, exact source, artifact evidence and narrow metadata allowlist; restoration defaults to a read-only preflight.

Self-review: source and all six approved identities preserved; native regressions passed; independent archive, capture and preservation review retained. Remaining debt includes mixed memory glyphs, sparse/quiet lane composition, cast-scale harmonization and dense modal typography. All55+5 area-polish scopes remain open. Next coverage choice is ordinary surface frogs in Sodden, based on existing disclosed fallback captures rather than a convenient cave queue.

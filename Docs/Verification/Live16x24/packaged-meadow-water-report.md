# Meadow and water packaged delivery — 2026-10-26

The new Mac/Linux packages freeze pushed source
`2a8e71044b36fb6ccdee72afe3473b2094d33abe`. They include the accepted meadow
composition and East water continuity, plus the previous inventory, underfoot,
court, hive and hedge changes. Ordinary Mac water save/return, newly generated
meadow/forge access and a separate clean new game passed. This closes package
parity for those changes, not the complete opening or physical Deck gate.

## Builds and transfer archive

Both non-development builds use normal SampleScene, LZ4 and detailed reports.
Mac job `build-d64a52f2da` succeeded in22.02s; Linux job `build-4565a3256b`
in29.55s. Each reported **zero errors and four warnings**. Receipts are
`packaged-meadow-water-{mac,linux}-build.json`. No native tests were rerun
solely for packaging; the source gates remain meadow244/244 and water229/229.

- Mac: `Builds/Verification/2026-10-26-meadow-water-2a8e71044-current/CavesOfOoo.app`
- Linux folder: `Builds/SteamDeck/2026-10-26-meadow-water-2a8e71044-current/CavesOfOoo-current`
- Transfer archive: `Builds/SteamDeck/2026-10-26-meadow-water-2a8e71044-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`
- Archive:128645547bytes; SHA-256:`e0ddfc99a23e4446db3e2bfbb65f2efb3e7805c95994eb5322cd46fddb6b32b3`.

The [archive receipt](packaged-meadow-water-linux-archive.json) verifies all181
files by hash, size and mode against the unpacked directory, including the
launcher, credits/licenses and build information. No unsafe paths or links.
An independent agent repeated verification, including180SHA256SUMS entries,
ELF64 x86-64 Linux executable and universal x86_64/arm64 Mac executable.
Source metadata is not a claim of reproducible source-to-binary provenance.
All older builds remain; the previous8902d3731archive hash still matches.

## Actual Mac paths

The bundle Saves directory was absent before this test. Both preference domains
and all normal saves were protected. Two unchanged earned fixtures were copied
into the bundle's own temporary save location, one at a time. Only LastGameID
was set; no presentation setting, grants, teleports or gameplay edits. A third
run used an absent Saves directory to check a genuinely fresh start.

| Original capture suffix | Ordinary input and observation |
| --- | --- |
| `01-start`, `02-bank` | Normal launch, C resumes earned East50,20;30/42HP,50drams and native bank. |
| `03-soaked` | Right to51,20;Soaked100%,Drenched36; player remains readable over water. |
| `04-inventory`, `05-item-reader` | I,Tab,F1: damage/defense, far-right[E] and matching equipped sword details. |
| `06-saved`, `07-continued-water` | Escape twice,F5,quit/reopen,C; world/player/native water render before further movement. |
| `08-kitchen-border`, `09-water-return` | Down5 reaches kitchen51,0;Up5 returns to East51,20 with banks,30/42HP,50drams. |
| `10-fresh-glade`, `11-fresh-meadow-entry` | Separate seed64 pre-generation checkpoint;C at glade79,16;Right generates East0,16. New flower spacing is visible. |
| `12-meadow-forge`, `13-forge-actions`, `14-forge-reader` | Right7,Down2 reaches7,18;C,Down,X opens actual forge menu/reader;40/40HP,50drams. No crafting result claimed. |
| `15-new-game-picker`, `16-new-game`, `17-new-game-moved` | Clean third launch offers seven builds;Return begins Duelist at40,12;Right reaches41,12 with visible flat world and updated FOV/log. |

All17files are `packaged-meadow-water-<suffix>.jpg` here. They are unchanged
CUA JPEG bytes,1280×856 including56px macOS chrome and1280×800 game viewport.
The [play summary](packaged-meadow-water-mac-play-summary.json) contains hashes,
actions and log scan. The water fixture intentionally uses its previously
generated meadow; only the separate pre-generation64 replay establishes new
meadow generation. No cached graph was presented as fresh-generation proof.

Actual [inventory](packaged-meadow-water-04-inventory.jpg),
[continued water](packaged-meadow-water-07-continued-water.jpg),
[fresh meadow](packaged-meadow-water-11-fresh-meadow-entry.jpg),
[forge reader](packaged-meadow-water-14-forge-reader.jpg) and
[fresh start](packaged-meadow-water-16-new-game.jpg).
Independent original-image review found no new blocking visual defect in these
paths; see [review](packaged-meadow-water-independent-review.json).

## Preservation and remaining gates

The [player restoration](packaged-meadow-water-player-restored.json) verifies
776normal save files,374preexisting editable/source files, two foreign PNGs and
both logical preference domains. `defaults import` retained only the test-created
LastGameID key; that exact extra key was rolled back and both maps then matched.
All three test Saves directories were moved into the protection archive; no
save or cache was deleted. Bundle Saves is absent again.

[Editor restoration](packaged-meadow-water-editor-restored.json) confirms idle
StandaloneOSX, clean normal SampleScene, null save override and no isolation,
pending art or borrowed view. [Project restoration](packaged-meadow-water-project-restored.json)
compared all47580prebuild tracked file hashes.32reviewed generated changes were
backed up and restored:29sprite metadata whitespace rewrites and3Unity build
serialization files. No source code/art changes occurred during packaging.
About427GiB is free; storage inspection only, no reclamation.

All three player logs complete bootstrap and shutdown without an explicit
exception/error/failed/fatal line in the keyword scan. They retain6/6/7
premature-thread-finalization notices and one ComputeBuffer disposal notice per
log. These predate this delivery and are not fixed or diagnosed here. A clean
keyword scan does not make shutdown warning-free or establish device performance.

Linux execution and physical Deck controls/performance remain unverified. This
is three bounded Mac paths, not a fresh complete six-service run. Whole-opening
art/state acceptance and remaining regional kits stay open. Next review the
six-zone journey as a whole, with a current matched Nella frontage view and its
separate service-loss audit; preserve accepted art until a named defect is shown.

# Current kitchen/hedge packaged delivery — 2026-10-26

Accepted scope: new normal macOS and Linux builds from pushed source
`8902d37316a24e3debd21e161b970f93ef1fd9d7`, actual Mac keyboard replay,
archive integrity, and restoration. This includes the underfoot action fix,
native hive/court and connected hedges. It is not Linux execution, physical
Deck performance or complete opening/regional art acceptance.

## Build and archive

Both non-development builds use `Assets/Scenes/Main/SampleScene.unity`, LZ4
compression and detailed reports. Mac job `build-57f5885e03` succeeded in33.89s;
Linux job `build-b3a962ab53` in31.68s. Each reported **zero errors, four warnings**.
Receipts: `packaged-kitchen-hedge-{mac,linux}-build.json`. Source was committed,
fetched/rebased, pushed and remotely verified before either build. Unity's
transient generated serialization is disclosed below; no reproducible-binary
or physical-device claim follows from the Git SHA.

- Mac: `Builds/Verification/2026-10-26-kitchen-hedge-8902d3731-current/CavesOfOoo.app`
- Linux folder: `Builds/SteamDeck/2026-10-26-kitchen-hedge-8902d3731-current/CavesOfOoo-current`
- Transfer archive: `Builds/SteamDeck/2026-10-26-kitchen-hedge-8902d3731-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`
- Archive bytes:128645260; SHA-256:`80ebb5305d98d564369a9640d4c113d9979dc4d789d10e31cca12946b44c41e2`.

`packaged-kitchen-hedge-linux-archive.json` verifies all181 regular files against
the package by hash, size and mode, no unsafe paths/links, ELF64 x86-64 executable,
executable launch script/UnityPlayer, audio credits and license texts. A separate
agent independently repeated archive/member verification. Existing current
packages were retained. Packaging does not add game source changes.

## Actual Mac replay

The bundle's Saves directory was absent before the run. After fresh logical
exports of both preference domains and a776-file normal-save hash snapshot,
root copied the unmodified, already-earned kitchen checkpoint to the bundle's
own save location. Only temporary LastGameID was set; presentation was not
forced. The original fixture remains unchanged. All subsequent play used normal
CUA keyboard controls; no teleport, grant or gameplay-state edit.

| Capture | Actual observation |
| --- | --- |
| `01-start` | Normal save/Continue prompt and flat opening. |
| `02-continued-court` | C resumes kitchen23,9,30/42HP,50drams,WT47; native court/hives/connected hedges. |
| `03-inventory`, `04-inventory-selected` | I,Tab show Equipment/Inventory, weapon1d8/1d6, armorAV/DV,far-right[E],selected sword portrait/details. |
| `05-item-reader` | F1 opens actual equipped long-sword reader;Escape returns. |
| `06-corner` | Left16/Up5 reaches7,4; continuous actual L-corner and foreground/fog. |
| `07-hedge-reader` | C/Left/X shows hedgerow, approved isolated portrait, Plant,Structure10/10. |
| `08-hive-reader` | Return,Down5,Right6,Down6,Left2 reaches11,15;C/Left/X reads native wild hive at10,15. |
| `09-underfoot-actions` | Return,Left enters10,15;C/period now offers Harvest beneath the player. |
| `10-harvested-saved` | H yields **one Honeycomb**, removes hive;Right to11,15;F5 saves. |
| `11-continued-harvest` | Normal quit/reopen/C, without subsequent gameplay input: player/world render,WT48,former hive absent. |
| `12-no-hive-after-return` | C/Left offers only grass examination at former hive;Escape and normal quit. |

Each filename is `packaged-kitchen-hedge-<name>.jpg` in this directory. All12
are original CUA JPEG bytes at1280×856 including56px macOS chrome; the viewport
is1280×800. No crop, resize or generated content. Their hashes/dimensions and
ordinary action journal are in `packaged-kitchen-hedge-mac-play-summary.json`.
The first quit's Quick metadata retains gameID, kitchen,turn4720 and30/42HP.
This replay yieldedone Honeycomb; the earlier editor replay yieldedtwo. These
are not claimed to be identical RNG histories.

Independent actual-image review accepts inventory statistics/equipment marker,
readers, native hedge/hive identity and continued harvested absence. One reviewer
initially misread a multi-image preview of capture11 as empty; direct review of
the unchanged original JPEG and matching SHA showed the world correctly. That
finding was retracted; no runtime blank-world defect is asserted here.

## Restoration and limits

`packaged-kitchen-hedge-player-restored.json` verifies all776 original save files
and both preference mappings restored. Test saves were moved to the protection
archive, not deleted. Only the test-created LastGameID key was rolled back.
`packaged-kitchen-hedge-editor-restored.json` confirms stopped/idle normal
SampleScene, StandaloneOSX, no isolation/pending art/view borrowing or save-root
override. `packaged-kitchen-hedge-project-restored.json` confirms all24765 tracked
source file hashes match prebuild bytes, plus both foreign PNGs unchanged.

Unity serialized29 importer whitespace-only metadata changes, URP shader
prefilters/runtime-settings list, InputSystem preload and default Standalone
batching. All32 files and their reviewed diff were backed up before restoring
exact prebuild bytes. No broad reset, source deletion or storage reclamation.
About427.7GiB remained free. Older current outputs and user saves are preserved.

Both actual player logs retain shutdown premature-thread-finalization notices
and a ComputeBuffer disposal warning. These predate this build and remain a
separate resource-lifecycle follow-up; this replay establishes neither their
cause nor their performance cost. No fatal gameplay exception was observed.

Can verify: successful builds, archive bytes/modes, exact captures, ordinary
Mac use/return and preservation. Cannot verify: Linux execution, physical Deck
controls/frametime, a new fresh six-service journey, or complete world art.
No native tests were rerun merely for packaging; source gates338/338 hedge,
260/260 hive/court and176/176 underfoot are separately recorded.

Next scene-level work: east meadow's overly uniform flower massing, then
surface-water material continuity outside the glade. Use current matched actual
approach captures before any changes. Preserve accepted sprites, finite budgets,
workpad, functional owners and safe/contested routes. Do not declare the lower-
confidence Nella frontage concern a defect without a fresh matched view.

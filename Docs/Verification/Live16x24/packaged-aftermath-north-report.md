# Current aftermath and North-water packages

Status: bounded Mac gameplay and archive acceptance complete,2026-10-26.
Frozen runtime source: `e49047200f28708d4bfd77fb7007d2c7843ba2ab`, verified on
origin/main before building. No intentional runtime edits during this slice.

## Build and delivery

Normal SampleScene, non-development player, Unity6000.3.4f1, LZ4. Mac build
37.19s/234.04MB; Linux38.28s/214.48MB. Both succeeded with zero errors and
four reported warnings. Source identity is the unique output folder plus
full-SHA build information and receipts, not the unchanged app version1.0.

- Mac: `Builds/Verification/2026-10-26-aftermath-north-e49047200-current/CavesOfOoo.app`
- Linux: `Builds/SteamDeck/2026-10-26-aftermath-north-e49047200-current/CavesOfOoo-current/`
- Transfer archive: `Builds/SteamDeck/2026-10-26-aftermath-north-e49047200-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`

Archive128,667,052bytes; SHA-256
`089a9cc4d06b1d3c20bd2c9052bd606fbeae8d13b97ef25c632edb181ae0e400`.
All181regular files independently verified against packaged and archived
hashes, sizes and modes;180SHA256SUMSentries; no unsafe members or links.
Launcher, readme, credits and licenses included. Earlier builds were retained;
the previous meadow/water archive still matches its recorded hash.

## Actual standalone paths

Fourteen original CUA JPEGs have a1280×800game viewport plus56pixels of native
window chrome. No image editing or generated gameplay imagery. Each capture's
hash and action is in [play summary](packaged-aftermath-north-mac-play-summary.json).
Both paths use unchanged, previously earned checkpoints in the bundle's
separate save directory. No forced presentation, grants or teleportation.

**North:** CContinue at58,16; Down crosses real PeatBog intoSoaked100%; ordinary
I/Tab displays current inventory DMG/DEF and equipped markers. F5, normal quit,
relaunch andC retain58,17water. Up18 reaches garden58,24; Down17 returns to
North58,16 with native bank. FinalF5:HP40,tick2930,42drams. See
[bank](packaged-aftermath-north-02-bank.jpg),
[inventory](packaged-aftermath-north-04-inventory.jpg),
[reopened crossing](packaged-aftermath-north-06-reopened-crossing.jpg),
[returned bank](packaged-aftermath-north-08-returned-bank.jpg).

**Aftermath:** separateCContinue atgarden51,11; Down/G opens an actual seven-item
pile. Enter takes the earned magpie corpse. I/Tab/Down8 selects its native
portrait; F1 opens the matching full reader. Close, moveDown, reopen/select/D
drops it at51,13. StepRight52,13,F5,quit,relaunch,C: the native body remains.
The ordinary nearby Marlback strike leaves35HP; no test health protection.
Finaltick2650. The original red% remains the higher-priority RawMeat pile
marker; text-menu glyphs are existing conventions, not a missing world sprite.
See [inventory](packaged-aftermath-north-11-corpse-inventory.jpg),
[reader](packaged-aftermath-north-12-corpse-reader.jpg),
[dropped body](packaged-aftermath-north-13-dropped-body.jpg),
[reopened body](packaged-aftermath-north-14-reopened-body.jpg).

Independent review of all14captures, binaries, receipts and archive found no
blocking issue. Logs identify Metal/AppleM5 and retain existing premature
thread-finalization and ComputeBuffer disposal notices on shutdown. No fatal
exception keyword was found; this does not diagnose or fix those notices.

## Restoration and self-review

Both test Saves directories are archived outside the repository. Original
bundle Saves absence and logical maps of both preference domains are restored.
All776normal saves,374approved art/source files and two unrelated screenshots
match their protected hashes.33reviewed build-generated files were backed up
and restored:30whitespace-only sprite metadata and3Unity serialization files
(URP prefilters, runtime settings, InputSystem preload/Standalone batching).
All47,731tracked prebuild hashes match. The hash scan overlapped build start
for ProjectSettings only; the prior clean source status established its HEAD
baseline, the correction is explicit in the receipt and original scan retained.
Separate editor observation proves idle normal SampleScene/OSX, no isolation,
null save-root override. No storage reclamation; about425.9GiBfree.

- 🔵 Generated build settings are disclosed and backed up, not shipped as an
  unreviewed source change.
- 🧪 Linux execution, physical Deck controls/performance and complete opening
  acceptance remain separate gates. Build/archive success does not close them.
- 🧪 Viper remains/gland are native-tested but not ordinary encounter-tested in
  these two Mac paths. No all-loot or all-region claim.
- ⚪ No new behavioral tests needed for this packaging-only slice. Its source
  gates were236aftermath and299North native tests, with explicit RED phases.

Next: close the ordinary cellar watered-sootroot growth/revisit gap using the
current playable opening and an earned route, before broader regional kits.
Preserve accepted art unless an actual named visual defect warrants changes.

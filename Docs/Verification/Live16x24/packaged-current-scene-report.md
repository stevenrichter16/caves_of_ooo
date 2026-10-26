# Consolidated current-scene packages

Frozen runtime source: `5a3c7435985f1842e162a7cfbfb7f38bd554eaac`, verified
on origin/main before building on 2026-10-26. This delivery brings the accepted
shrine, Nella access/dialogue/corner, kitchen initial encounter correction and
East wall/gate/peat changes into ordinary SampleScene packages. It preserves
the previous current packages and all approved art.

## Delivered files and verification

- Mac: `Builds/Verification/2026-10-26-current-scene-5a3c74359-current/CavesOfOoo.app`
- Linux folder: `Builds/SteamDeck/2026-10-26-current-scene-5a3c74359-current/CavesOfOoo-current/`
- Transfer archive: `Builds/SteamDeck/2026-10-26-current-scene-5a3c74359-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`

Both non-development builds succeeded with zero errors and four warnings.
Mac: 38.11 seconds,234.06MB. Linux:34.34 seconds,214.49MB. Explicit LZ4 player
data returns to the earlier compressed layout; previous47d678995 packages
used separate uncompressed resource files. This is a packaging choice, not
an established runtime-performance improvement.

Archive:128,671,917bytes;181 regular files,180 checksum entries.
SHA256: `63736cde4c503bb7a055be9edcca3afd7b91916d1773f86150d7b05609f3cd2c`.
Independent streaming verification matches every file against disk and receipt
by hash, size and mode. No unsafe paths, links or duplicate members. The ELF64
x86-64 player, UnityPlayer.so and launcher have executable permissions. Launcher
and README match frozen source; six credit sections and five license texts
are included. See [archive receipt](packaged-current-scene-linux-archive.json).

## Actual standalone gameplay

[Fourteen original CUA screenshots and save metadata](packaged-current-scene-mac-play-summary.json)
record ordinary keyboard input at a1280×800 viewport, plus56pixels of native Mac
window chrome. No image editing, synthetic scene, presentation override or
runtime state mutation was used. The starting earned gate checkpoint is copied
unchanged from the prior ordinary Unity route; this does not replay its entire
history inside the package.

1. Ordinary launch renders the flat opening and offers Continue/New Game.
   C loads East55,19,HP32,tick3500. PageUp twice reaches3×.
2. C/Left/Return closes the adjacent gate. Its closed native frame and occlusion
   update. F5 saves tick3510. Quit/relaunch/C retains location, HP and closed gate;
   zoom correctly resets to automatic1×.
3. PageUp twice and C/Left/x open the matching closed-gate reader, Structure10/10.
   Escape/C/Left/Return reopens it. Left twice crosses to53,19 with HP32.
4. I opens equipment with1d8 weapon damage,1AV+1DV buckler defense and far-right
   [E] markers. Tab shows the selected sword's native portrait and bounded detail
   text. Escape returns to the world; PageDown reaches2×. F5 metadata is tick3540:
   exactly four turn-consuming actions since3500, with no UI turns charged.
5. Quit/relaunch/N opens all seven starting builds.5 then Return creates a fresh
   Warden at40,12,HP40. PageUp and Down reach40,13; F5 saves tick20,HP40.

Representative captures: [closed gate reader](packaged-current-scene-06-gate-examine.jpg),
[open gate](packaged-current-scene-07-gate-reopened.jpg),
[equipment](packaged-current-scene-09-inventory.jpg),
[pack](packaged-current-scene-10-pack-weapon.jpg),
[fresh movement](packaged-current-scene-14-new-game-movement.jpg).

Independent review of all14 captures found no blocking visual defect. The
fresh-start log contains Brinna fighting a Marlback; that one-move observation
does not establish resident safety or kitchen acceptance. Thread-finalization
and ComputeBuffer shutdown notices persist in the raw logs; no fatal or exception
line occurred in the retained sessions. They are not repaired by this delivery.

## Review and boundaries

🔵 Packaging and bounded standalone Mac checks pass. Source gates remain the
recorded shrine31+184, placement104, dialogue99, Nella corner139, kitchen104 and
East249 native runs, with overlap; these must not be summed as unique tests.
Packaging adds no runtime behavior and required no new test suite.

🔵 Integrated cold-eye review by `/root/seed65_route_audit` found no significant
issue in the changes from47d678995 through5a3c74359: catalog ownership/disposal,
exact-speaker handoffs, cold-generation authority, saved-layout preservation and
door/water state semantics remain consistent. Independent package/archive and
actual visual review by `/root/current_build_plan` also found no blocking issue.

🧪 Linux execution, physical Deck, frame-time benchmarks and complete six-zone
art/service/state acceptance remain open. The new executable includes the latest
accepted source; this narrow Mac test does not validate every source feature.
The previous surface/cellar package proof remains historical evidence.

## Preservation and restoration

Fresh protection captured48,289tracked filesystem objects (48,287regular files
and two symlinks), both prior current build trees/archive, normal saves and full
logical preference maps. After play, every tracked object matched that snapshot
before the intentional delivery-record edits. Thirty-two whitespace-only sprite
metas and three reviewed build-serialization files were backed up before their
original bytes were restored. Both foreign dirty screenshots retain their own
prebuild bytes; they were never replaced from Git.

Generated standalone test saves are archived intact outside the repository and
the original Saves absence restored. The complete logical standalone preference
map is restored; the complete Editor map already matched and was not imported.
All776normal save files,374approved source/art files and both foreign screenshots
match their retained baselines. Prior current packages/archive remain unchanged.
Unity is idle on SampleScene/OSX, with no isolation or save-root override. Storage
inspection reports about419GiB free; nothing was reclaimed. See
[project restoration](packaged-current-scene-project-restored.json),
[editor state](packaged-current-scene-editor-restored.json) and
[preservation](packaged-current-scene-preservation.json).

## Next acceptance

Continue a fresh current-package six-site journey through glade/cellar, north
works, Nella, East and the kitchen. Use the recorded ordinary paths and1×/2×
framing, examine and use the services, then save/quit/Continue and return. Retain
real deaths and failures instead of replacing them with forced state. Prioritize
remaining visible owner/state or route defects; the encountered loose Cudgel
still has a deliberate glyph fallback. Use the full actual capture→imagegen→
editable native source loop only when a named artwork gap warrants new pixels.
Do not call this package refresh a complete opening or regional graphics release.

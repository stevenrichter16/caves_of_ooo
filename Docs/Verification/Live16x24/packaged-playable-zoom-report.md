# Playable zoom: current packages

Status: bounded standalone Mac and Linux archive acceptance complete, 2026-10-26.
Frozen runtime source: `47d6789955b6f84a5e9a9168e6317f4ef2086fd4`, verified
on origin/main before building. The runtime change and its RED/GREEN sequence
are in [playable zoom](../../GRAPHICS-G3-PLAYABLE-ZOOM.md).

## Delivered files

- Mac: `Builds/Verification/2026-10-26-playable-zoom-47d678995-current/CavesOfOoo.app`
- Linux folder: `Builds/SteamDeck/2026-10-26-playable-zoom-47d678995-current/CavesOfOoo-current/`
- Transfer archive: `Builds/SteamDeck/2026-10-26-playable-zoom-47d678995-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`

Both ordinary SampleScene builds succeeded with zero errors and four warnings:
Mac 35.09s/343.96MB, Linux 34.46s/324.42MB. These outputs use separate resource
files instead of the prior LZ4 `data.unity3d`; unpacked size is about110MB higher.
No runtime asset additions explain that difference and no LZ4 claim is made for
these builds. The gzip transfer archive remains128,744,597bytes, only77,545bytes
larger than the prior current archive. Compression/performance changes were not
benchmarked. All prior builds and six earlier archives are retained.

Archive SHA256:
`6751aa6177bbe7999939955fa2ba5bfb6b550f2bb812aec4aba4ec267e562262`.
Independent verification compared all188 archived files against disk and receipt
by hash, size and mode, plus187 checksum entries. No unsafe paths or links.
Launcher/README match frozen source; credits and licenses are included.

## Actual standalone gameplay

Twelve original CUA screenshots have a1280×800 game viewport with56pixels of
Mac window chrome. They are actual game output, without image editing. Actions
and hashes are in [play summary](packaged-playable-zoom-mac-play-summary.json).
An unchanged checkpoint generated in the previous current build starts this
same-position comparison: seed224401440, Warden,40,12,40HP,tick10.

Normal C Continue defaults to1×. PageUp gives2×, two more commands reach4×,
three PageDown commands return1×. PageUp then I opens the fullscreen equipment
view; Escape restores2×. F5 metadata remains tick10, establishing that these
commands spent no turns. The weapon damage, armor defense and far-right equipped
markers remain readable. See [2×](packaged-playable-zoom-02-close.jpg) and
[inventory](packaged-playable-zoom-05-inventory.jpg).

Ordinary Down4, Left13, Down3, Right reaches the cellar stairs28,19. This route
uses the open y16 crossing and27,19 approach, avoiding fallen beam26,19,
wall28,17 and solid sign29,18. Shift+. descends to cellar40,12 at2×. PageDown
shows1×; PageUp and Shift+, return to the surface at2×. F5 saves tick240,40HP.
Quit/relaunch/C preserves that world and starts with unchanged automatic1×;
PageUp again reaches2×. Selected zoom is intentionally session state. See
[cellar](packaged-playable-zoom-08-cellar-close.jpg),
[wide cellar](packaged-playable-zoom-09-cellar-wide.jpg),
[returned surface](packaged-playable-zoom-10-returned-surface.jpg).

Independent review of all12 captures found no blocking visual inconsistency.
These prove visible endpoints, not every frame or physical controller feel.
The prior Unity pass additionally verifies Look selection and matching reader.
Existing shutdown thread-finalization and ComputeBuffer disposal notices remain
in raw logs; no fatal/exception keyword appeared. This pass does not fix them.

## Restoration and limits

Test saves are retained outside the repo and the earned surface checkpoint is
versioned for continuation. Original standalone Saves absence and its complete
logical preference map are restored. Editor gameplay preferences match the
original pre-cellar snapshot. Four non-gameplay keys changed through testing:
PT_Run and Unity session ID/count metadata; these are disclosed and preserved.
An initial full-map assertion stopped on exactly those keys; no gameplay values
were silently accepted or overwritten. Native isolation restored LastGameID.

All47,847 tracked prebuild file hashes match after backing up and restoring
33 generated files:30 whitespace-only sprite metas plus3 reviewed build
serialization files (URP prefilters/runtime settings and InputSystem preload/
Standalone batching). Unity is idle on normal SampleScene/OSX, with no isolation
or borrowed view and a null save-root override.776 normal saves,374 approved
art/source files and two unrelated screenshots remain hash-identical. Storage
was inspected only, about425GiB free; nothing was reclaimed.

- 🔵 Packaged output now contains the accepted native zoom fix; default framing
  and approved artwork remain stable.
- 🧪 Linux execution, physical Deck, performance and complete six-zone scene
  acceptance remain open. Build/archive success does not close them.
- 🧪 No standalone Look test or separate3× still in this pass; native tests and
  the earlier actual Unity pass provide the stated narrower evidence.
- ⚪ Packaging-only follow-up adds no runtime behavior and needs no new test
  suite; source gate remains23/23 zoom tests, broader230/233 with the same3
  log-pointer setup failures reproduced on unchanged code.

Next: continue fresh current-generated six-zone scene framing using actual1×/2×
controls, classify the finite encountered owners/states and choose only named
visual defects for the full screenshot→imagegen→native-art production cycle.
The ordinary surface/cellar path is accepted here; remaining connected-site
whole-scene composition and ordinary combat feedback still require review.

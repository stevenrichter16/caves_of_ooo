# G3 — hotbar name spacing

Status, 2026-10-26: bounded native and ordinary Editor acceptance complete; current Mac/Linux packages still predate this source correction.

## Named defect and verified scope

The real Mac capture `Verification/Live16x24/collected-revisit-33-returned-save.jpg`
shows En Garde touching the first card's border. Independent source/visual review
corrected the earlier description of this as clipping: both end letters remain
visible. The problem is crowded spacing, not demonstrated viewport loss.

Before this fix, `HudHotbarPresentation.NameLines` permitted eight characters, the full
card width. `GameplayHotbarLayout` and the camera agree on the 80-column grid;
at 940×120 the cell width is .5875 and the font sprite width is .55. No camera,
card, pointer, font, icon or gameplay change is justified. Keep all approved art.

Implemented minimum: a seven-character name-line budget, wrapping En / Garde.
This leaves the existing Lunge, Follow / Through and Conjure / Rain rows intact.
Seven-character strings still start in column zero under integer centering;
this is not a universal two-sided-gutter guarantee. Original CoO presentation,
with the user's Qud-like readability direction; no copied-art/parity claim.

## Verification plan

1. Apply the reviewed tests to existing HudPolishTests and WardenHotbarIconTests.
   Run native RED before production changes: exact En/Garde rows, normalized
   whitespace, seven/eight/fourteen/fifteen-character limits and null/empty input.
2. Use the actual factory Warden at 940/120 and 1280/120 aspect ratios. Check
   explicit occupied/empty cells, sprite bounds, neighboring names, icon/source,
   READY/header, row clearing and no skill execution. Existing fixtures cover
   cooldown/AIM, unchanged-frame work, source changes and pointer mapping.
3. Implement only the name budget. Run focused and neighboring native GREEN.
4. Inspect ordinary in-game output at 1×/2×, including selection/cooldown where
   reached normally. Native tile bounds are not GPU or physical-Deck evidence.
5. Independently review, preserve normal saves/preferences and unrelated work,
   push exact changes, then refresh the current packages before further journey
   acceptance. Continue the earned resin/still/forge leg afterward.

## Ownership and preservation

The external Editor session recorded in the collected-revisit report ended on
its own. At 21:36–21:38 UTC fresh MCP reads confirm idle Edit mode, no compiling,
no running tests, no native save isolation, and a clean SampleScene. Root did
not stop that session. A new protection baseline in
`/tmp/coo-hotbar-names-protection` preserves its newer preferences/saves and all
34 preexisting dirty tracked files (32 metadata files and two foreign PNGs).
It records 48,645 tracked files and the existing Mac/Linux/archive. Old snapshots
must not be restored over this newer baseline. Storage remains inspection-only.

## Implementation and review log

- Plan and tests prepared before production. Independent reviewer confirmed
  label crowding, sufficient word wrap, and no camera defect.
- Native RED job `1cb4c9fd394d4af9921a0034d5f38b6a`: all 21 cases completed, with eight expected failures before production. The raw tool result has no final pass-count summary; do not infer more than its completed/failure fields.
- Native GREEN job `992b5f3ed74040fd8d6da7c23928907a`: 93/93 passed, zero failed/skipped, across HudPolish, adversarial, Warden icons, hotbar rendering and pointer suites. Boundary cases include whitespace/null, seven/eight/fourteen/fifteen characters and real Warden geometry at 940/120 and 1280/120. Row retirement, neighboring names, icon/header/source and no skill execution are counter-checked.
- Actual ordinary Editor acceptance loaded the unchanged earned `collected-revisit-returned242967504` fixture in owned native isolation. Six original 1280×800 ScreenCapture PNGs show ready names at 1×/2×, key1 activating En Garde (CD11), key2 Lunge AIM, cancel/inventory, and return. Capture03's filename `selected` denotes actual stance activation, not just selection. Independent visual review of 02/03/06 found no material regression.
- Finished the owned isolation and returned to idle, clean SampleScene. The newer external-session baseline was preserved. A read-only comparison checks 48,645 tracked files with exactly three owned source/test deltas; all 34 preexisting dirty tracked paths, complete logical preferences, normal saves and current Mac/Linux/archive remain identical. Receipt: `Verification/Live16x24/hotbar-names-preservation.json`.
- Two Metal memoryless depth load/store messages appeared during capture; no claim of a warning-free engine run. A scratch query's private-property compile error was corrected using reflection and was not a production source error. Source compilation/native tests completed.
- Source/evidence/documentation are committed together; refresh current packages next. No new art, camera, card layout, font, input or skill transaction was needed.
- 🧪 Whole opening, regional, Linux execution, physical Deck and performance
  acceptance remain open. No new artwork is warranted for this text-only repair.

Files intended: this record, HudHotbarPresentation.cs, HudPolishTests.cs,
WardenHotbarIconTests.cs, bounded verification evidence and living continuation.

## Evidence and honesty bounds

- [RED receipt](Verification/Live16x24/hotbar-names-red.json), [GREEN receipt](Verification/Live16x24/hotbar-names-green.json).
- [Actual capture manifest](Verification/Live16x24/hotbar-names-captures.json), [preservation](Verification/Live16x24/hotbar-names-preservation.json).
- Can verify: native line/geometry/countercases, ordinary keyboard actions, actual rendered output in the six frames, owned isolation cleanup and recorded file/preference preservation.
- Cannot verify here: updated-package parity, actual gamepad/physical Deck controls, Linux execution, whole-opening completion or sustained device performance. Existing source behavior remains in the current daa388643 packages until the next build.

## Self-review and continuation

- 🔵 Corrected false wording: original complete letters crowded the border; viewport clipping was not demonstrated. Seven-character labels are not universally inset by integer centering.
- 🔵 Independent source/test and screenshot reviews found no material issue. Kept approved sprites and all neighboring layout/control contracts stable.
- 🧪 Package parity and hardware/continuous feel remain open. Next: package this source, then continue the earned North-still/East-temper route, not another unrelated art packet.
- Hourly report checkpoint: 2026-10-26 21:53 UTC. The user requested itemized work, status and coming-session goals; automation updated. See [full run inventory](GRAPHICS-RUN-CHANGELOG-2026-10-26.md). Intermediate ten-minute storage inspections remain read-only/quiet.

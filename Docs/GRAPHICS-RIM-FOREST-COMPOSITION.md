# RimForest understory composition

2026-11-02 — bounded Editor composition improvement. Source publication pending.
Full-area polish, environmental coverage, shadows and packaged delivery remain separate gates.

## Choice and scope

Actual ordinary Stump RimForest `Overworld.2.3.0`, world seed65, has broad pinkstone
clearings, regularly spaced small trees and disconnected low vegetation. Compared
with already improved Spread field margins and Beating ruin fragments, this was a
useful population gap. Reuse the approved Bush identity around existing trees;
do not disguise TepuiStone as Grass or add blocking trees and alter encounters.
This is placement work through the real generator, with no new art or crop system.

The earned `BeforeEntry` checkpoint is on the world map at32,6, turn1810, HP40,
before entering this forest. Both baseline and changed generation enter through
ordinary Continue and Shift+period. No teleport or inserted world entities.
Evidence lives in `Verification/GraphicsExecution/RimForest/`.

## Requirements and implementation

- Apply only to fresh RimForest generation after original habitat marking.
- Add real passable, destructible Bush owners only on originally empty reserved
  habitat within two cells of existing Trees. Preserve every original owner.
- Keep approach cells and their one-cell margins, tank access and the winding
  central crack clear. Preserve reservation and habitat masks, fauna eligibility,
  pipeline RNG and all other formations. Existing cached/edited saves stay exact.
- Grow five unequal connected groups using stable coordinate ranking. Desired
  sizes21/17/14/11/9; decline a group below five eligible cells rather than relax
  protection. Actual seed65 adds72 bushes: total124, with44 Trees and8 tanks unchanged.
- Keep quiet gaps, readable routes and native world/Examine identity. Counts alone
  do not establish visual acceptance.

`StumpCompositionPlan.ForestUnderstory` freezes eligibility before placement and
does not expand habitat around the added owners. No material, fog, shadow,
collision rule, loot, service, NPC role, encounter table or serialization changes.
Rollback removes the RimForest call; cached real owners remain normal saved entities.

## Verification and actual play

Native corrected RED:20 intended missing-understory failures plus one negative
pass,21 total. Focused GREEN21/21; final bounded Stump regression166/166, including
the focused tests, not an additive total. Frozen original native signatures cover
seeds0/1/17/64/65/12345/int.MaxValue/int.MinValue. Tests prove only `.h` to `Bushh`
changes, exact masks/original owners, actual fauna-placement equivalence, no pipeline
RNG draws, passability, edited/bare save round trips and missing-blueprint preflight.
See `source-corrections.md`; the initial unimported zero-case result is excluded.

Actual matched1× captures: center40,12, west25,12, east48,12, covering both sides
of the chunk; actual2× detail is separate. West travel was unprotected. Temporary
F12 protection was enabled for the east route in both before and after surveys;
ordinary sky-sari/sari-snake actions still ran. Thirty Right inputs reached48,12
because combat consumed actions. Natural encounter balance is not accepted.

The player walked through a Bush at48,12 and opened its native Examine sprite and
6/6 structure identity. Actual route via48,17 to44,18 retained access to the tank
at44,19; its existing draw-water action logged a successful drink. Normal world-map
travel2.3→2.4→2.3 worked. F12 was explicitly OFF before F5 turn2450, HP40, player40,12.
Fresh isolated Continue preserved all221 recorded environmental owner IDs,
locations and structure health. Saved/reloaded world rectangle is identical:
0 changed of639,200 pixels. Normal saves/preferences were restored by the isolated
session tool; Play stopped, override null, scene clean.

Short manual Editor profiles, without execute-code probes inside the windows:
before75.41s / after75.24s. Renderer p95 .104/.097ms, maxima90.75/324.47ms;
Main Thread maxima153.96/3091.28ms. These are not causal or packaged comparisons;
R11 spike attribution remains open. Separate single plan constructions across eight
seeds took .744–1.811ms in this Editor; not full-zone generation or a baseline delta.

## Independent review and remaining work

Independent source review: no new P1/P2; original protection and deterministic
placement are sound within sampled scope. Independent six-view visual review:
modest but real distributed density improvement, readable crossings/owners, no
new P1/P2. Reviewer identified row-like/rectangular group edges, especially northwest
and east. Later shaping should stagger outlines while retaining density and routes.
Tiny tree silhouettes, broad pinkstone planes and weak canopy hierarchy remain;
more bushes alone will not finish forest identity. Stone blocking relief and
black-backed cues remain. The red+ at60,18 was subsequently attributed to a real
AshBed by read-only runtime inspection; investigate its native presentation next.

Area polish remains **in-progress**, population **reviewed** with bounded actual
density evidence. No new sprite coverage or shadow completion. Current Mac/Linux
packages from`fedc68b5f` exclude this placement change; batch after accepted slices.
No physical Deck claim. Preserve approved art and all unrelated work; no cleanup.

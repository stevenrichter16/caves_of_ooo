# Next material coverage after the shallow-cave foundation

2026-10-27. **Source-only recommendation, not a new implemented phase.**
The current shallow-cave work and its named background-readability correction
must finish their own gates first. This audit operated no Unity, generated no
world graphs, changed no runtime/art/tests and earned no deeper-cave acceptance.
Broad world coverage and the later separate biome-by-biome/area-by-area polish
remain open.

## Finite next slice

Prioritize **LimestoneFloor / LimestoneWall at depths3–4**, then
**ShaleFloor / ShaleWall at depths5–6**, carrying the existing shared Rubble,
Stalagmite, StairsDown and StairsUp identities forward only after explicit depth
admission and state checks. Do not extend the shallow sandstone address guard
or orange backdrop palette indiscriminately.

[SolidEarthBuilder.cs:42](../../../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs)
sets sandstone1–2, limestone3–4, shale5–6, slate7–8, quartzite9–10 and obsidian11+.
Depth5 is the first shale level, not the start of a new population tier.

| Exact owner | Source-verified meaning |
| --- | --- |
| [LimestoneWall](../../../Assets/Resources/Content/Blueprints/Objects.json:8466) | `#`, `&w`, background`^K`; inherited solid Wall and Stone/Mineral material/thermal parts; structural HP35/35, hardness3, real Rubble wreckage. Pale stone with lighter chipped edges. |
| [LimestoneFloor](../../../Assets/Resources/Content/Blueprints/Objects.json:8799) | Passable Terrain, `.`, `...,.'` plus backtick variants, `&w`; no authored Material/Thermal/Destructible or creature Hitpoints. Pale dusty stone. |
| [ShaleWall](../../../Assets/Resources/Content/Blueprints/Objects.json:8525) | `#`, `&K`, background`^k`; inherited solid Stone/Mineral wall, structural HP28/28, hardness2, real Rubble wreckage. Thin dark layered stone. |
| [ShaleFloor](../../../Assets/Resources/Content/Blueprints/Objects.json:8837) | Passable Terrain, same floor variants, `&K`; no authored Material/Thermal/Destructible or creature Hitpoints. Dark overlapping flakes. |

Preserve surviving structural damage and actual destruction. Neither the wall
name nor its gray glyph proves a generic StoneFloor/StoneWall or portable ore
identity. Native material coverage does not automatically cover landmarks,
containers, creatures or hazards placed on it.

## Natural versus roomed areas

[UndergroundLayoutPlan.cs:37](../../../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs)
permits the stable25% roomed choice only at depth3+ in ordinary
**Spread, Sodden or Beating** columns. Current POIs and authored reservations
exclude it; wall/floor plus Rubble, Pillar and BrokenColumn content must exist.
A content miss retains natural strata. This selector is cold generation logic,
not current-graph or saved-lair rendering authority.

Current column **0,9 lies under Overwrit** and therefore remains natural at these
depths. It cannot establish roomed coverage. As a source-only counterexample,
the private seed65 hash produces2 at depth4 and11 at depth5—both below25—but the
Overwrit biome gate still refuses rooms. These values are a translation of the
saved hash formula, not observations of generated or visited deeper graphs.

The room geometry uses4–7 rooms with4–10-cell size bounds.
[RuinsBuilder.cs:99](../../../Assets/Scripts/Gameplay/World/Generation/Builders/RuinsBuilder.cs)
adds a Pillar or BrokenColumn at roughly5% of room-floor positions. Those two
extra owners remain explicit coverage candidates, not free acceptance from a
wall/floor kit. Obtain a separate actual eligible roomed baseline later; a
natural cave screenshot cannot substitute for it.

## Existing art: useful references, limited direct reuse

The inspected `Sprites/Environment/limestone_m00`/`m15` and `shale_m00`/`m15`
are opaque16×16 two-color **floor-field slices**, not wall sprites or connectivity
masks. [EnvironmentSpriteRenderer.cs:527](../../../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs)
describes the64×64 macro field, while [565](../../../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs)
and [797](../../../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs)
map and load the16 position-indexed slices per material. Preserve these sources.

Directly centering an opaque16×16 ground slice inside the16×24 cell leaves
vertical gaps; stretching it would break the native-pixel contract. Neither is
an accepted floor solution. No matching limestone/shale native wall set was
found in this bounded source scan. Treat the old floor colors/field character as
references; actual capture and concept critique must determine a small original
16×24 floor/wall kit for each material, with editable native cells and quiet
variation. Frame counts are not fixed by this plan. The accepted warm sandstone
wall must not become a gray geological material by name matching alone.

Shared rubble/stalagmite squares and native stair cells remain direct immutable
reuse candidates. The ordinary room `Pillar` has an existing16×16 binary-alpha
source, but needs its own exact owner/state and actual-readability review.
The opening BrokenColumn art is restricted to its named VisualID; do not widen
that policy casually.

## Encounter, landmark and route limits

[PopulationTable.cs:1102](../../../Assets/Scripts/Data/Tables/PopulationTable.cs)
uses tier=`depth/3+1` (capped8). Depths3–5 are **tier2**: the existing encounter
choice can now include MarlbackStormbinder, CaveBear, IceWight and Rotling; loot
can include LongSword and PaleSaltVein. Tier3 additions such as SkeletalSentry,
CharredHusk, PaleStalker and ChoirIronVein begin at depth6. The nearby comment
that the shale band brings burned/pale creatures is less precise than the code.
The optional field-medicine substitution applies only to ordinary Spread
columns at depths3–5, not current Overwrit0,9.

[LandmarkBuilder.cs:203](../../../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs)
introduces a tier2 Ziggurat with VineWall, PressurePlate, locked chest, IronKey
and living ChoirTendril; the existing MineGallery uses generic Wall, chest and
GlowQuartzVein. CurationGallery begins tier3/depth6. Preserve these distinct
physical owners/services and retain their missing-art/fullness status. The
material kit must not silently reinterpret them as the stratum wall.

The current0,9 entry and disclosed depth1 downstairs at40,12 are real retained
baseline observations. A prior approach killed the isolated HP23 character;
entry83 was preserved. **No depth2–5 descent, safe route or successful return is
claimed by this audit.** Continue with ordinary controls, short observed steps,
actual stair/counterpart checks and earned saves. Record interruption honestly;
do not alter AI, grant health, teleport or regenerate a convenient route.

## Minimum future gates

1. Finish the current shallow-cave correction and delivery first.
2. Capture an actual limestone entry/chamber/edge and stair return; then capture
   the real shale boundary if normal gameplay permits. Keep natural and roomed
   area evidence separate, and account for visibility rather than calling all
   dark space bare ground.
3. Choose reuse versus original native art from those actual views. Keep
   silhouettes, material palettes, ambient canvas, passability and hazards
   consistent without inventing floor owners or extra generation density.
4. Write native RED for exact depth/material/current-graph authority, special
   exclusions, actual structural health/Rubble outcome, real floor/backdrop
   separation, optional groups, disclosure and world/reader parity. Preserve
   established artwork and mechanics.
5. Integrate, compare actual views/controls and save/restart, then verify fresh
   packages. Record remaining actors, landmarks, rooms, hazards, services and
   all deeper bands explicitly. No art count or material delivery accepts a
   whole biome or the separate serious polish phase.

This handoff changes **this document only**. It proposes no production draft,
new art, runtime test result, packaged run or Linux/Deck execution.

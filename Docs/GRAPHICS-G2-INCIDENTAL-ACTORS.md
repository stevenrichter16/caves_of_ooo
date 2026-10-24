# Next court actors — bounded source and capture audit

2026-10-24. Status: source audit complete; plan accepted for the next bounded graphics slice; no implementation or art acceptance yet.

Read-only repository/capture audit by `/root/projection_cold_review`; Farmer source subaudit by `/root/projection_cold_review/look_focus_draft_review`. No Unity, gameplay, tests, source edits, art edits or Git operations. This is a next-step proposal, not an integration/acceptance receipt.

## Decision

There are **two different gaps** worth a small actor packet:

1. **Farmer needs a distinct native frame.** The actual seed65 Farmer7139 is a glyph on the cot. No approved Live16x24 Farmer asset or selector exists. Do not silently reuse the exact Villager frame, the Cook, or the keeper cloak.
2. **Reedback Grazer already has the approved native frame; connect it.** Seed64 ReedbackGrazer5033 visibly remains `g` in the east capture. `Assets/Art/ReedbackGrazer16x24/03-ridge.png` is the user-approved 16×24 image, preserved but absent from the current live flat selectors/resources. Reusing those exact bytes is preferable to new grazer art.

Magpie, Viper, Scrabbler and Cook do not need replacement art for this packet. A separate seed65 Cindercaller is another genuinely missing identity; keep it named as a later gap rather than borrowing Scrabbler/Gleaner art merely because its blueprint inherits Scrabbler.

## Actual evidence, not a fresh census

Read paired owner receipts and inspected these native PNGs: `working-court-64-east.png`, `working-court-64-kitchen-2x.png`, `working-court-65-kitchen-2x.png`, all under `Docs/Verification/Live16x24/`.

| Receipt | Actual owner | Observed/current disposition |
|---|---|---|
| 65 kitchen, tick1130 | Farmer7139 at(53,6), visible | Distinct pale `@` over Bed7197 at the same cell. Cook7190 at(51,6) and Magpie7169 at(58,6) already have native bodies. |
| 64 east, tick410 | ReedbackGrazer5033 at(51,17), visible | `g` visible among the worked enclosure. Approved low quadruped exists, but no flat binding. |
| 64 east | Scrabbler4996 at(6,24), visible | Native squat body visible near the lower left; existing static/motion identity already covers it. |
| 64 east | Farmer4957 at(66,5), not visible; Magpie4995 at(61,7), visible in receipt | Do not claim these are both on-screen: the PNG viewport ends before these world columns. Receipt FOV visibility is not screen-frustum evidence. |
| 64 kitchen, tick1060 | Magpie7159 at(28,19), visible | Native bird visibly stands beside the cot. It is not an unhandled glyph. |
| 64 kitchen | Viper7160 at(55,1), not visible at tick1060; visible at(64,1) in entry receipt | Native mapping exists, but these captures do not establish a close visible Viper appearance beside the court. |
| 64 kitchen | SpreadWaysideCook7183 at(27,17), visible | Approved Cook already connected. |
| 65 east, tick410 | Cindercaller4983 at(67,3), visible in receipt | No current native16×24 exact mapping; outside the inspected east screenshot viewport. No claim of visual acceptance. |

The complete creature identities in the saved seed64 east/kitchen receipts are those seven IDs above, determined from actual blueprint inheritance. HP>0 alone is **not** an actor filter: Hatchet and LeatherBoots also have positive recorded HP. These receipts do not claim a complete future population inventory.

Farmer7139's shared cot location persists through harvest/save/load/return receipts. Root's readback establishes Farmer Physics.Solid=true and Bed7197 Physics.Solid=false; the captured owner schema itself has no Physics field. This is legitimate current occupant obstruction, not bad court geometry and not evidence of a sleeping pose. Do not move/remove the actor, change cot passability, or invent sleep art in a graphics fix.

## Source corrections and exact binding seams

- `Objects.json:24562–24644`: exact Farmer inherits Villager, canonical `@`, `&Y`, layer10, Farmer conversation/trade and boots/gloves loadout. Inherited solid physics at `:11909–11914`. No equipped hoe is promised. `LandmarkBuilder.cs:795–812` can spawn a Farmer from MillStead; `VillagePopulationBuilder.cs:147–150` also places Farmers. The owner receipts do not prove which created this particular one, so avoid a new generation-role requirement.
- `FlatPixelOpeningArtCatalog.cs:587–596`: cast identity table is **exact** Villager/PetDog/Scrabbler/Gleaner. Farmer inheritance is not an alias. `ArtSource/Live16x24/cast/README.md:23–32` identifies frame0 as Villager and prohibits silently assigning civilian/marlback frames to another identity.
- `EnvironmentSpriteRenderer.cs:78–90,987–991,1729–1737` has legacy Farmer sprite support. Its PNG is **16×16**, importer uses cropped12×15 (`Environment/farmer.png.meta:90–96`). `Content/Data/EntityVisuals.json:22–37` also aliases Farmer to an older villager animation sheet. Flat mode intentionally exits first at `EnvironmentSpriteRenderer.cs:1152–1177`; bypassing that boundary to revive an older sheet would mix styles, not connect approved native art.
- `FlatPixelOpeningArtCatalog.cs:372–392`: exact Magpie→north-works5, Viper→6, Patchbearer→7/8, with owned Render/Physics and explicit-reskin/custody guards. `FlatPixelActorMotionCatalog.cs:61–73` accepts only Player/PetDog/Scrabbler/Gleaner. Do not widen this motion policy to suppress a new static actor.
- `Objects.json:40878–41000`: ReedbackGrazer is a squat quadruped with blunt muzzle/reed-like ridge, own SpreadGrazer part and Creature/Brain/Body semantics. `SpreadGrazerPart.cs:108–139` shows finite forage and proximity flight; Fed, Configured, current food/hunt/mood are mutable behavior, not body identity or native art permission.
- Approved ridge file measures **16×24**, SHA256 `29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad`. `ArtSource/Live16x24/PARTS.md:28–40` records its approved identity, low horizontal bounds `(0,9)-(16,21)`, six colors, and real-owner-only placement. Existing use is the recoverable study (`Tools/Clearing16x24/build_unity.cs:9,68`), not live world binding. Its existing sprite pivot is bottom center (`03-ridge.png.meta:49–54`); a runtime Tile needs a full16×24 center-pivot Sprite.Create without cropping, padding, stretching or changing source bytes.
- `Objects.json:41592–41685`: Cindercaller is a narrow-capped/scorched marlback with ranged fire behavior and mutable cudgel/fire-moss loadout. No permanent flaming hands, mandatory held cudgel, or generic Gleaner medical silhouette is truthful. Existing 3D cindercaller assets are not native16×24 approval.
- Runtime entry is `FlatPixelGreyboxCatalog.TryResolvePresentation` (`:688–745`), after `FlatPixelFallbackCatalog` establishes current graph/cell/top-owner/custody/visibility. Subject route is `TryGetPresentationSubjectSprite` (`:750–769`), with external document authorization. Exact six-graph admission is `IsAdmitted` (`:163–179`). Existing companion pattern is `FlatPixelEastKitchenCatalog` (`:20–54`); resources cached/disposable, texture dependencies borrowed, no world scans. Preserve these authorities.

## Smallest implementation/design proposal

Use one small optional **static local-actor companion** with two independent borrowed16×24 textures: a new Farmer frame and a byte-identical Resource copy of approved ridge. Proposed API: `TryResolve(Entity owner, out Tile tile)`, default loading once, injectable `(Texture2D farmer, Texture2D grazer)` with missing/malformed groups independent. Connect at the existing Greybox world and exact-subject native selection seams; no thirteenth positional opening-atlas parameter, second renderer, animation view or per-frame watcher.

Initially preserve existing native-skin admission for the six current local graphs; do not change global flat projection or silently broaden all regional/native actors. A later deliberate portable-static policy can be tested separately if needed. Exact semantic identities are Farmer and ReedbackGrazer only, with owned visible Render/Physics, Creature and non-Item/non-Player/non-custody, canonical `@`/`g`, and no explicit VisualID/VisualVariant/GlyphVariants override. Verify Body/Brain/SpreadGrazer ownership where those source contracts are used; do not gate neutral body art on stock, conversation availability, HP threshold, faction, friendliness, Fed/Configured, or a court role. Keep existing broader/fallback semantics intact.

Farmer authoring follows the current actual capture→imagegen target/parts→deliberate editable native16×24 construction→import→ordinary verification pipeline. Use a civilian farming silhouette distinct from keeper hat, Cook cap/apron, player hood and Warden cap; no baked tool, crop bundle, sleeping posture or guaranteed trade stock. Static art must not be reported as animated. Grazer needs integration and visual review of unchanged pixels, not regeneration.

## Meaningful next RED and acceptance counters

Keep the new fixture bounded and factory-backed. Before production:

1. Actual Farmer and Grazer in current admitted east/kitchen graph must resolve **new/approved native textures** in both world and authorized examine, not merely any non-null fallback. Initial expected failures are missing companion/routes/resource. Pin native rectangle16×24,16PPU/center pivot and exact approved ridge hash separately.
2. Actual Farmer over Bed: submitted foreground is Farmer; ordinary move/removal repaints the old cell to the real bed without manually marking full dirty. Solid Farmer still blocks entry, non-solid bed does not. No service-state/stock/crop mutation.
3. Foreign/custom identity and explicit visual override/canonical glyph change fall back truthfully; inherited Villager, Cook, Cindercaller, corpse and unrelated `g` never receive either new frame. Existing Magpie/Viper/Scrabbler/Cook native results remain exact controls.
4. Hidden/unexplored/memory, foreign current graph/same-ID replacement, stale cell and carried owner cannot leak a body; world/reader visibility and current-subject lifetime agree. Existing lifecycle fixtures can supply harnesses rather than duplicating every global test.
5. Missing/malformed one texture leaves the other available; injected constructors never load Resources; repeated query returns cached Tile/Sprite; disposal releases only owned objects, not borrowed approved textures or peers. Warm selector stays allocation-free.
6. Healthy/lowHP/Calm or changed farmer stock and grazer Fed/Configured still resolve the same neutral body; no invented readiness state. Actual approved importer path must be exercised after a genuine missing-resource RED.

Relevant controls: `FlatPixelEastKitchenTests`, `FlatPixelGlobalPresentationTests`, `FlatPixelOpeningCastTests`, `FlatPixelNorthWorksTests`, `FlatPixelActorMotionTests`/catalog tests and `FlatPixelFallbackCatalogTests`. Final ordinary revisit should show Farmer at/near the real cot, correct exact subject reader and move-away bed restoration; show the existing real Grazer at1x/2x and its reader without spawning substitutes. No physical Deck, new animation, complete incidental bestiary or full six-zone journey acceptance follows from this small packet.

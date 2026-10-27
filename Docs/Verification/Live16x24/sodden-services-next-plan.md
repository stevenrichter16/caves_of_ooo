# Sodden services: concrete static identity source plan

2026-10-27. Planning only, after the ordinary Sodden formation breadth slice.
No art packet, runtime changes, Assets imports, Unity or Git actions are part
of this audit. Root owns capture → imagegen → selection → production approval.

## What this should fix

The actual district now has coherent wet ground, timber approaches and masonry,
but its useful owners remain small glyphs. Render those existing owners before
adding furnishings. The clearest gap is Sella's room:70 selected floor cells,
one line glyph for the bench and one ring glyph for its keeper. The works has a
real merchant and recoverable drying frame, neither recognizable by silhouette.

Primary actual references, all inspected as original1280×800 Game captures:

| Capture under `Docs/Verification/Live16x24/` | Real visible owners and state |
| --- | --- |
|`sodden-final-06-shelter-entry.png/.json`|Stop15,7 at40,12/tick1900. Notice24,11; Sumpsieve47,9 stage2/moisture0 and50,9 stage0/moisture0.|
|`sodden-final-08-shelter-room-matched.png/.json`|Stop15,7 at34,8/tick2000. Unrepaired bench34,6; PeatCutter35,6, displayed as Sella, dressing keeper.|
|`sodden-final-09-peat-works-entry.png/.json`|Works17,7 at40,12/tick2060. Merchant36,8; unharvested SoddenWorksSalvage27,16.|
|`sodden-final-03-east-board.png/.json`|Crossing16,7 at60,3/tick1860. Existing passage guard/post visible as glyphs. The authored initial homes are35,2 and36,2; use the current owner position at capture, not a presumed stationary guard.|

These remain valid composition references while their actual geometry is
unchanged. Closer ordinary approach/Examine captures should establish each
subject before imagegen; do not construct a separate showroom and call it the
playable scene.

## Premises corrected by the source audit

| Tempting assumption | Actual contract and resulting art decision |
| --- | --- |
|Repaired means the service is ready|`SoddenPreparationPart.Context` also needs its exact living, non-hostile, unblocked keeper beside the configured station, plus the player's ingredients/fee. The second bench frame depicts a **braced structure**, not a readiness badge or prepared stock.|
|Repair heals the bench|`RepairablePart` restores a functional fault; it does not heal combat HP. Keep a physical brace change separate from damage, fire, destruction and removal.|
|Sella is a separate blueprint|She is an actual `PeatCutter`; Configure changes the display/conversation and binds her to the bench. Use a shared peat-worker body, not a name-string or copied-coordinate identity test.|
|The cutter must hold the long spade mentioned in Examine|Actual loadout equips LeatherBoots/LeatherGloves and carries a Dagger. There is no authored equipped spade. Use neutral empty hands; do not bake a new wielded tool into the actor or silently add one.|
|The cutbank watcher is an armed soldier|It has an intrinsic1d3 MeleeWeapon and no Loadout in its blueprint. No sword, spear, shield or stock-bearing gear should be invented.|
|Paid passage changes the post|PermitHolder/token state belongs to the real guard/player; warning/grace are actor-relative. One neutral post frame is sufficient. Never show a universal green/open post after one player's purchase.|
|Dry Sumpsieve is wilted/spent|Growth pauses while dry; ripe crops remain ripe. Stages0/1/2 are seed/sprout/ripe. Moisture is independent soil state. No invented wilt/dead stage.|
|Harvest requires a stubble/empty-frame sprite|Both `CropYieldService` and `HarvestablePart` remove the actual source. The crop's prepared bed survives; the drying frame is removed. No permanent empty crop or collapsed-frame remnant is created by these mechanics.|
|The wayfaring merchant is a works worker|`WorldTravellers` spawns the shared `Merchant` blueprint, generates real stock and changes the name/route properties. Use a general travelling merchant identity; no peat-company uniform, fixed weapon, visible quantity of stock or permanent works employment.|
|Existing Sodden art means these sprites are already available|`SoddenNativeArtLibrary` and `SoddenDistrictArtLibrary` are recoverable3D libraries. Their owner descriptions inform the design, but they are not approved flat16×24 sprites and should not be rasterized into this packet by assumption.|
|Borrowing a soil image grants regional soil support|`FlatPixelOpeningGroundCatalog` currently admits specific opening/North/East/Nella contexts. Borrowing its two soil frames requires a separately tested current Sodden prepared-ground route; do not expand the opening admission list or call the hidden-owner-leaking cultivation helper.|

## Smallest useful source packet:10 new native frames

Recommend **one160×24 atlas, ten16×24 frames**, with static props/crops and
shared people declared separately in its manifest. Shared Merchant/PeatCutter
identity does not require a second atlas or another framework. Keep their
mapping independent of the exact bench relationship so the same approved
pixels can later serve normal occurrences after scope/owner validation.

Do not bundle new crop products, locker art, corpse art, animations or additional
furniture into this first source packet. Their coverage stays explicit.

| Index/name | Owner and essential selection | Original visual design |
| --- | --- | --- |
|0 `dressing-bench-split`|Exact intact SoddenDressingBench, sole structural Repairable with `timber-dressing-bench`, `Repaired=false`, Wood composition/material and SoddenPreparation present.|A low, broad wooden bench about13px wide, its useful tabletop/clamp in the lower-middle of the cell. One split/splayed leg and a small gap beneath the top communicate instability. A compact wood press upright and a restrained grey cloth clamp are integral to the already-described bench. A folded pale cloth mark may identify its purpose, but no finished dressing or stocked row of pads.|
|1 `dressing-bench-braced`|Same exact owner with `Repaired=true`; no separate staffed/unattended frame.|Preserve the same tabletop, press and clamp. Add two unmistakable sound timber braces under the top and straighten the unstable leg. The change should be readable without sparkles, green check marks or replacing the whole object. Physical structure only; no promise of available service or restored HP.|
|2 `peat-worker`|Live PeatCutter; includes the configured Sella when actually bound, and can be reused for ordinary peat cutters after validation.|A compact human with a practical headwrap, long smoke-dark work apron/tunic, plain pale cuffs and tall dark trouser/gaiter shapes. Separated feet and empty hands; no giant hat, fixed dagger/spade or held medicinal pad. Distinguish from the existing Farmer by the longer narrow apron, covered crown and cool grey-green clothing. These are identity clothing shapes, not a claim about current removable equipment.|
|3 `cutters-route-notice`|Exact physical SoddenRouteNotice with its current Examinable text.|One weathered timber stake and a small broad board above damp ground, with two or three quiet pale inscription strokes. No legible fake text, quest icon, glowing exclamation mark or baked directional arrow that could contradict the actual route.|
|4 `cutbank-passage-post`|Exact SoddenPassagePost; inherited notice geometry/physics, not the generic notice merely recolored by a name.|Share the notice's material and upright, but use a narrower doubled crosspiece with a short tied pale strip/notch, making the local passage marker distinct at1×. It remains a solid marker, not a gate or a toll box with invented storage. No paid/unpaid frame.|
|5 `sumpsieve-sprout`|SumpsieveCrop stage1; actual harvested product/seed schema and single valid CropPart.|A low crown of two broad oval leaves, roughly7×7px, with an asymmetrical bite-shaped/perforated interior notch. Quiet peat-green, not reed stalks or flowers. Leaf tips remain below the mature silhouette.|
|6 `sumpsieve-ripe`|Same crop stage2 with `HarvestAtMaturity=true`; stays ripe when moisture reaches0.|Three overlapping broad, spoon-like bitter leaves, about12px wide and13–15px high. Two tiny transparent perforations plus a restrained edge/vein imply the sieve leaves. No berries, grain, flowers, sparkle or placed dressing product. The larger harvestable leaf mass is the ripeness cue.|
|7 `cutbank-watcher`|Live SoddenPassageGuard with actual Brain/SpreadTerritory/LocalPassagePermit; neutral body regardless of one actor's permit.|A square-shouldered human in a short dark weathercoat with a muted ochre collar/waist binding, firm separated feet and visible empty hands. A shorter, broader silhouette than the peat-worker and a smaller cloak than the merchant. No fixed weapon, guardhouse or hostile red body tint. Existing warning/hostility/effect feedback must remain authoritative.|
|8 `collapsed-drying-frame`|Exact unharvested SoddenWorksSalvage; Harvestable yields four SalvagedTimber at100%.|Four distinguishable sound, long timber lengths in a low collapsed frame, one diagonal brace and a broken joint. Wood grain matches the accepted boards without becoming paving. Fit roughly14×10px near the bottom; open gaps reveal the real floor. No intact drying rack, attached crop, item-count label or permanent depleted frame.|
|9 `travelling-merchant`|Live shared Merchant with its actual Trader/Inventory/Brain. Region changes and remaining stock do not manufacture new bodily states.|A compact broad travelling cloak with an asymmetric shoulder fold, small hood/cap and a restrained cross-body strap. A closed clothing pouch is acceptable as costume, but do not draw specific sale goods, an open inventory rack, fixed ShortSword or an abundance/fullness indicator. Distinct from Sella's apron and the watcher's short square coat.|

The original source can use the established integer-cell Blender-plane
workflow. Each part/cell stays named and editable; native PNG and contacts
render directly. No tracing, shrinking or repainting approved inputs. Concept
detail that cannot survive the native grid must be pruned before production.

### Three borrowed images, zero new copies

- Sumpsieve stage0 borrows `opening-states` frame6, rect96,0,16,24. Its three
  planted marks/soil divots are deliberately neutral and already reused by
  other crop families. The manifest's original name is `drawgourd-seed`; record
  the borrowed neutral meaning instead of renaming or editing its source.
- Prepared dry/wet ground borrows `opening-ground` frames19/20, rect304/320,0,
  16,24. Keep soil on its own ground layer, read real visible current moisture,
  and preserve it after harvest. An empty prepared bed has no crop-owned water
  reserve; do not invent one.
- Do not clone these three frames into the new atlas. All approved source and
  imported bytes remain unchanged. Their presence in a source-only contact is
  explicitly borrowed context, not new authored coverage.

## Footprint, palette and identity decisions

Every requested owner is currently one native cell and has no authored
SpatialFootprint. Do not enlarge a bench into adjacent walkable cells just to
fill the room. Static tile views use the established centered pivot/16PPU;
actor source baselines should match accepted bodies (feet at native row22,
transparent row23). A renderer needing a bottom-pivot actor view may create
that sprite view without editing/resampling the original pixels. Crop/prop
baselines follow their existing ground contact; shared seed frame6 reaches23.

Palette proposal uses accepted family swatches, pending the captured-scene
concept review: travel wood322E25/504B3B/726C59/88806B; muted metal/bark-like
greys37413B/68756A/8C9380; crop greens435A35/53683E/75834B; linenB5A47E;
actor skinDFA06A and subdued cloth accents from the approved Farmer palette.
Use a small subset per frame, transparent background and binary alpha. Preserve
the darker16×24 terrain's visual priority: pale cloth and faces are local
interaction cues, not a new bright pattern repeated across ground.

Source boundaries are visual meaning, not a mandate to reject all changed
clothing. Do not bake an equipment item into an immutable body. Alive/dead,
custody, hidden/fog, damage/status color overrides, custom variants, removal
and unsupported anatomy should retain the existing truthful behavior/fallback.

There is no need for separate global Merchant/Sella art packets now. The
manifest must distinguish shareable static people from exact local props, and
runtime authorization must remain independent of atlas membership. A new
shared-people module is justified only if the existing dispatch cannot borrow
these frames without duplicating ownership code; do not create one pre-emptively.

## Essential gates before integration

1. **Capture/concept.** Use the retained matched scenes, then approach and
   inspect the actual bench, keeper, plants, post, salvage and merchant using
   ordinary controls. Save exact imagegen inputs/prompt/output and a critique.
   Preserve scene paths, collision, owner positions and the quiet mire. Reject
   extra furniture, weapons, sale stock, exaggerated leafy masses and3D bevels.
2. **Source RED → native production.** Before output, fail on the absent ten
   frames/owner mapping. Produce editable source-cells/manifest/Blend, native
   PNG and direct1×/enlarged contacts. Check unique essential states, palette,
   binary alpha, one-cell footprints, unchanged approved inputs and exact
   source/native pixels. Negative counters must catch a duplicated repaired
   frame, misplaced stage, invented full/empty salvage and borrowed-input drift.
3. **Runtime RED → mapping.** Prove actual current owner/graph/visibility and
   sole owned semantic parts. Repair flag changes only physical bench frame;
   keeper absence/death does not become a false service-ready icon. Unknown
   repair recipes/yields/stages/custom states cannot acquire this art. Do not
   use raw glyphs, display names, saved coordinates or art IDs as authority.
4. **Crop/soil counterchecks.** Stage0/1/2 map correctly while moist and dry;
   ripe dry crops stay ripe. A hidden/foreign crop or soil part cannot change
   visible ground. Ambiguous crops, negative moisture, stale graphs and mixed
   coatings keep truthful fallback/state marks. Do not advance CropTime or
   reconcile simulation in a render query. After harvest/removal, the real
   prepared soil remains, and new products are separately selected.
5. **Actor and finite-source counterchecks.** Shared PeatCutter/Merchant art
   follows their actual owners through ordinary movement; no foreign Sella or
   permanent works residency is inferred. Guard permits remain actor-specific;
   a warning, expired token or personal hostility does not rewrite the post.
   Harvesting salvage removes its sprite with its owner, and a retained spent
   reference cannot masquerade as four fresh timber lengths.
6. **Actual actions and whole-view comparison.** Start from preserved earned
   fixtures. Harvest the real frame at27,16 through the ruined-room entrance,
   then return with two actual timber to brace the bench. Use a real pad, cord
   and two drams to prepare a dressing with the living keeper. Harvest the
   standing crop; plant its returned seed in the real prepared bed and water
   using a carried water supply (existing Water action consumes one unit and
   supplies40 moisture; two stages require48 moist rounds). Top up again when
   needed—do not grant a spell, fabricate inventory, fast-forward growth with
   debug tools or draw a new stage before it genuinely occurs. Inspect/trade
   with the merchant and use the guard's normal passage menu if selected for
   acceptance. Capture before/after/revisit and final full-room views.

Keep source and asset caches bounded: ten cached static sprites/tiles plus
borrowed existing textures, no per-cell GameObjects or per-frame allocation
framework. Profile the actual renderer path after mapping; tests with an
unsupported allocation control must remain inconclusive rather than passes.

## Limits and follow-on coverage

This packet covers eight owner families and their essential depicted states;
it does not finish the district. Actual Bandfrog remains another enemy-art
gap. SoddenWorksLocker, beds/chairs elsewhere, SumpsievePad/Seed and the produced
field dressing need their own existing-art/admission audit; only SalvagedTimber
was found with an existing flat native product mapping in this audit. Do not
claim a full harvest-to-product visual journey before those outputs are checked.

After these real service owners are rendered, re-evaluate the keeper's room and
the work yard at the same native scale. If they still feel bare, make a separate
composition proposal grounded in the lore and existing functional contents.
The first intervention is not to scatter invented drying shelves, hanging
bodies, lights or decorative plants across valid routes.

## Primary references read

- `Assets/Resources/Content/Blueprints/Objects.json`: exact eight blueprints and
  their Creature/PhysicalObject/RepairTimberPile parents; no file reserialization.
- `Assets/Resources/Content/Data/Repairs/Tier1Repairs.json`: two-timber bench brace.
- `Assets/Scripts/Gameplay/World/Generation/SoddenDistrictPlan.cs`: real site
  geometry and coordinates; `Builders/SoddenDistrictBuilder.cs`: native crop
  initialization, prepared soil and keeper assignment.
- `Assets/Scripts/Gameplay/World/SoddenPreparationPart.cs`: configured worker,
  station identity, repaired/staffed service and actual ingredient/fee transfer.
- `Assets/Scripts/Gameplay/Repairs/RepairablePart.cs`: functional repair semantics.
- `Assets/Scripts/Gameplay/Farming/CropPart.cs`, `CropYieldService.cs` and
  `CropWateringService.cs`: stages, moisture, crop removal and real watering.
- `Assets/Scripts/Gameplay/Items/HarvestablePart.cs`: finite source removal.
- `Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.Regional.cs`,
  `SecondExplorationRoutes.cs`, and `Gameplay/AI/SpreadTerritoryPart.cs`: guard/
  post configuration, personal passage and warning scope.
- `Assets/Scripts/Gameplay/World/WorldTravellers.cs`: real travelling Merchant.
- `Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs`:
  existing shared seed and timber product mappings; `FlatPixelOpeningGroundCatalog.cs`:
  prepared soil frames and current limited admission.
- `Assets/Scripts/Presentation/Rendering/SoddenDistrictRecipes.cs`,
  `SoddenDistrictArtLibrary.cs`, `SoddenNativeArtLibrary.cs`:3D provenance only.
- Approved native Farmer image, `states/opening-states-contact.png`,
  `Ground/opening-ground-contact.png`, `Tools/Live16x24/build_incidental_actors.py`
  and `build_north_plot.py`: inspected style, source palette and borrowed geometry.
- `CLAUDE.md`, `Docs/LIVE-16X24-ART-DIRECTION.md` required production pipeline,
  and current-priority sections of `Docs/GRAPHICS-COMPLETION-PLAN.md`.

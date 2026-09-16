# The Deepest Cathedral — native composition

Status: complete and installed. Cathedral native composition passes final targeted and
full-suite checks. SC14 has 537/537 focused passes; SC17 has 12,825 passes and
exactly the 32 unchanged baseline failures, with zero C# errors and no new
failures. All 287 new tests pass. Eighteen native camera previews have complete
model coverage; live input feel and sustained FPS remain outside this evidence.
The aggregate implementation log and review are in
`CATHEDRAL-STILLLEAF-COMPOSITION-PLAN.md`.

## Scope and identity

Exactly `Overworld.5.4.0`, `.1`, and `.2` receive this composition. The
containing world's current sinkhole identity remains authoritative in routing
and rendering. Depth3+ and other Choir cathedrals keep their prior generators.
This is one three-level named place, not three new biomes.

The journey has three distinct spatial roles:

- **Pilgrimage mouth:** four loose green groves frame broad access lanes and
  the native sinkhole landing. The stone center is real walkable ground. It
  does not imply a new abyss, falling simulation or cliff traversal command.
- **Expedition descent:** broad three-cell-deep shelves attach to continuous
  cliff stone. Three native rope anchors, one stocked sack and adjacent bones
  establish a prior expedition. The sack contains an actual torch, dried meat
  and healing tonic, preserving their normal pickup and use mechanics.
- **Memory nave:** paired grown buttresses divide the side chambers into bays.
  They leave the existing cathedral stamp's door columns and processional
  middle clear. The native stamp supplies the node, conversational elders and
  tendril merchants once; the base does not clone these inhabitants.

The user-requested coarse voxel scale and restrained two-color models remain
the art contract. Terrain height is a presentation of native objects, never
an independent collision layer. Shared descent infrastructure may reuse the
Ginmere models; grown vault, node and Choir inhabitants receive distinct art.

## Verified sources and corrections

| Source | Verified consequence |
|---|---|
| `Lore/History/02_Geography.md:82`; `Lore/Factions/01_RotChoir.md:48–49,122,218` | The place is grown, inhabited substrate. The Wedded is distributed and alive; she is not a corpse, scenery statue or newly invented boss. |
| `Lore/Design/V1-DramaticCore.md` | The god's private-name/revelation arc is story-gated. No model, sign or generated inscription grants that revelation. |
| `SinkholeSites.cs:40–49`; `SinkholeArchetypes.cs:47` | The Cathedral is5.4. Olderdeep is4.6 and Stillleaf is2.4. The surface here is Grovelands. |
| `ChoirCathedralBuilder.cs:13–20,115–161` | This shipped builder is the cathedral archetype; the Wedded audience remains W8. Its node is a light fixture, not a Wall-Catching travel gate. Elders and tendrils are native creatures; tendrils have actual Conversation and Trader Parts. |
| `Objects.json`: SubstrateVault, ChoirNode, EncasedElder, ChoirTendril | Existing grown vault and node omit Destructible. Preserve this explicit native exception. Ordinary sandstone walls, trees and bushes remain destructible; presentation does not rewrite either policy. |
| `SinkholeDescentBuilder.cs`; `GinmereCompositionBuilder.cs` | A new authored descent must replace legacy scatter rather than duplicate cache/anchors. Supplies are native nested container items. |
| `OverworldZoneManager.cs:304–348,900–921` | Existing floor encounter/loot tiers remain native. Cathedral floor has depth2 ambient0.22 and its node's own light, not the brighter sima daylight override. Geography calls it Tier4 while the Choir faction document says Tier5; composition does not silently choose a new balance tier. |

No Qud implementation is ported: the layout is CoO-original, grounded in this
repository's canon and native mechanics. Wall-Catching blockers, the earned
Wedded audience, pilgrimage quest/offerings and global endings remain deferred.

## Native architecture and realization

`CathedralCompositionPlan` has no Unity geometry or live-world mutation. Its
finite address check precedes parsing; seed plus address determines the
layout independently of the caller's random stream. `GroundAt`, `ObjectAt`,
`IsApproach`, `Depth`, and `Signature` expose the native arrangement.

`CathedralCompositionBuilder` runs at1000 only on an empty native zone. It
checks all required blueprint names, then stages and validates each actual
owner before committing anything. Checks include visible native glyph,
physics, takeability, terrain semantics, ordinary structural destruction,
material/thermal identity, container capacity and supplies' native use Parts.
No validation prototype is substituted for a later unchecked owner. Failed
staging leaves original owners and reservation membership untouched; a
rejected second build clears its published Plan without regenerating the zone.

The manager integration retains native connectivity, the mouth/floor stamp,
actual stair construction and connector, hazards, populations and containers.
`CathedralArrivalReservationBuilder` runs at3660 and protects the completed
stair cells before late content. It moves no stair, repairs no saved world,
and has no authority outside the three Cathedral levels.

Successful generation emits `worldgen/CathedralCompositionPlanned`; refusal
emits `CathedralCompositionRejected` with address, seed and reason. Arrival
protection emits `CathedralArrivalsReserved`.

All plan allocations occur during generation. Runtime graphics reuse the
established mesh library and current-owner reconciliation; no new per-frame
simulation, per-voxel GameObject or saved presentation field is introduced.

## Verification and implementation log

- 2026-09-15: Read CLAUDE.md, authoritative geography/Choir/lore constraints,
  W6 close-out, native sinkhole routes/blueprints/stamps, and Ginmere's
  completed integration. Scope chosen alongside Stillleaf in
  `CATHEDRAL-STILLLEAF-COMPOSITION-PLAN.md`.
- SC00 baseline (root):12570 total,12538 pass,32 known failures, zero C#
  errors. This task compares exact failures against that baseline.
- Authored23 initial native cases before production. SC02 (root) confirmed
  actual missing CathedralCompositionPlan/Builder RED before implementation.
- Added45 independent adversarial cases before the first compiled native
  feature run. These probe wrong scope, out-of-bounds cells, caller RNG,
  retry preservation, missing/malformed content, real late-pass reachability,
  lower-first return stairs, conversation frontage, trader restocking and
  sandstone destruction. Their first runtime results are recorded under SC05.
- First-pass native plan/builder and late arrival reservation implemented.
  No shared manager/rendering file or blueprint JSON edited by this module.
- SC05 (root):366 targeted cases,354 pass,12 fail, zero C# errors.
  Cathedral's68 new cases passed67 with one meaningful native failure:
  Choir merchants had initial stock but never restocked. The existing
  TraderRestockSystem rejected all non-Villagers factions before consulting
  their authored TraderPart. ChoirStock guarantees an ink vial, so an empty
  renewal is not random chance. After root captured the shared-file baseline,
  the scoped repair retains legacy Villagers eligibility and additionally
  admits a real TraderPart with nonempty StockTable. Purse, interval, factory,
  low-shelf and return-count behavior remain unchanged. Five paired controls
  cover ordinary Choir creatures, forged stock properties, empty Trader parts,
  legacy Villagers, absent purses and missing factories.
- Independent cold-eye review found the planned grown side buttresses ended
  one or two cells before the native nave wall. SC06 confirmed the actual
  continuity RED at seed64 cell9,1. The rule now connects every buttress to
  rows7/17; width and offset remain seed-dependent. No demonstration placement
  was manually adjusted. This adds one regression, bringing Cathedral's
  owned fixtures to74 cases; their successful rerun is recorded under SC08.
- SC07: inspected all nine native Cathedral previews (three roles across
  seeds64,1729,729490642). The shelves are attached and the grown bays now
  visibly join the nave. Mouth/descent circulation reads consistently. Two
  related art findings remain under refinement: very bright vault tops
  dominate the node, and the1.8-unit near wall hides most south-row elder
  faces at the gameplay camera. The proposed correction belongs to shared
  kit rules, not per-seed owner placement or changed native collision.
- SC08: actual XML reports24/24 core cases and50/50 adversarial cases GREEN.
  The broader targeted run passed506/514; remaining rendering/art corrections
  are recorded by the shared plan. The actual Choir restock regression,
  explicit eligibility countercontrols and joined-buttress gate now pass.
- Final shared-integration cold-eye review compared only this feature's
  captured deltas. Exact address and current-site routing agree with the
  per-zone weak authority context, including restored graphs; finite-depth,
  foreign-place and renamed-profile controls remain paired with positives.
  Recipes use actual membership, visibility and door state. Removed native
  floors receive no invented replacement ground, and portable owners keep
  their body variants across the supported stacks. The existing required-art
  rejection path releases owned presentation resources and retains borrowed
  assets. No additional material authority, owner-lifecycle or restock defect
  was found in this bounded review.
- SC12: that review did find one observable elder-pose defect. A real forced
  move updates native VisualFacing; a subsequent nonlethal hit turns the
  unrigged elder toward that action direction. Its unchanged quarter-turn
  recipe previously left it facing into a wall after the gesture expired.
  Ten new cases in the shared `SanctumRenderingAdversarialTests` cover both
  nave rows, real movement and HP loss, independent empty-dirty Refresh and
  LateUpdate expiry, and animated Player facing countercontrols. The run
  produced exactly four elder-expiry failures, 32 passing cases, and zero C#
  errors before the correction.
- The shared presenter now caches authored idle-facing only for the exact
  `cathedral-elder-` model family. Its existing `Play("Idle")` path restores
  that body's current authored quarter-turn before the no-Animator return,
  covering refresh, hit/gesture expiry and movement completion. Ordinary
  actors retain native action-facing. Read-through confirms this changes no
  owner, model variant, movement, collision, timer or resource lifetime.
  SC13 is running; no final GREEN or full-suite outcome is claimed yet.

## Self-review and honesty bounds

⚪ Existing grown-vault indestructibility and rope-anchor non-traversal are
native contracts, not omissions filled by decorative art. The foundational
cathedral is not reported as the W8 god encounter.

🧪 All 74 native gates and nine gameplay-camera views have run, and the shared
elder-expiry regression has an actual RED receipt. Its correction's rerun,
final refinement views and full regression are pending. Static headless
previews can establish composition and model coverage; they cannot establish
input feel, animation quality in motion or sustained gameplay FPS. No
completed verification result is claimed before the actual run.

The same-row displacement tests use native forced movement, but EditMode
intentionally bypasses in-play movement interpolation. The shared idle-path
review is evidence for the correction's coverage of movement completion;
it is not a claim that a live animation look-pass has been performed.

## Files owned by this module

- `Assets/Scripts/Gameplay/World/Generation/CathedralCompositionPlan.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/CathedralCompositionBuilder.cs`
- `Assets/Tests/EditMode/Gameplay/World/CathedralCompositionTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/CathedralCompositionAdversarialTests.cs`
- Unity metadata for those four files and this living document.
- Scoped shared delta in `Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs`,
  captured by root before the authorized native merchant repair.
- Contributed ten regression/control cases to the root-managed
  `Assets/Tests/EditMode/Presentation/Rendering/SanctumRenderingAdversarialTests.cs`;
  the shared presenter correction and execution receipts remain root-owned.

## Final verification

SC17 confirms the exact baseline failure names and messages are unchanged.
SC13 rebuilt all 294 combined artifact/metadata files byte for byte. The final
GUID audit covers 5,735 Unity metadata files and finds no task collision.
`Verification/VoxelWorld/SC16-closeout/regression.json` records the comparison;
`SC10-refined-preview/index.html` contains all eighteen actual native renders.
Earlier pending statements in the implementation log describe those earlier
runs; this final verification supersedes them. New layout rules apply on fresh
native generation; existing cached/saved graphs are not rewritten.

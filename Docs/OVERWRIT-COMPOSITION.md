# Overwrit — the Blank and its Rim

Status: complete and installed. Final targeted gate 396/396. Full suite 12,570 total / 12,538 passed / 32 unchanged baseline failures / zero C# errors. All 307 new feature cases pass; the suite also adds one surrounding equipment countercontrol.

## Scope and verification sweep

Exactly 22 canonical surface addresses are eligible. The Unsaying
(`Overworld.2.11.0`) stays on its existing legacy pipeline; it is reserved in
WorldMapAuthoring comments but is not presently a POI. Named/runtime POIs and
all underground addresses remain outside composition routing.

| Premise | Verified correction / decision |
|---|---|
| Four new busy formations are needed | Canon requires the Blank and the Rim. Four compositions express these two roles: interior Blank, Green Rim beside Grovelands, Dry Rim beside Beating, Pilgrimage Rim beside Spread. Actual neighbor data decides the role; seed changes sparse physical placement. |
| The shipped Overwrit ecology is authoritative | Its W0 placeholder uses DesertBuilder's cactus default, Ruins population/stamps/hazards/containers and anvil scatter. This conflicts with flat, stoneless near-emptiness. Remove these consumers only from eligible ordinary cells. |
| Keeping the Unsaying pipeline solves all circulation | It preserves a possible legacy source, but one random source is less reliable than 23. Sill's actual ArcanistStock already circulates five utility books and three workshop schematics; DryingBreeze was missing. AR04 reproduced that failure through actual restocking; one Chance 30 entry was then added surgically. Renewed stock remains probabilistic, not guaranteed at every visit. |
| Existing Floor or Sand is uniform | Both carry GlyphVariants. OverwritGround derives directly from Terrain, with no variants and no inherited terrain noise. |
| PalimpsestEcho is an underworld ghost | Its actual display name is Recension field scribe; retain the native human and conversation outside this composition. |
| Selecting a rim world address keeps its stairs in the physical margin | A rim chunk still contains a large blank interior. AR05 found stairs in that interior at three seeds. The existing CaveEntranceBuilder receives a scoped cell filter requiring the actual eight-cell rim strip and clear approaches/aprons; default callers retain their previous placement behavior. Underground and excluded routes remain unchanged. |
| A non-null factory result proves valid content | Parts and parameter assignment fail softly. Missing Render/Physics or contradictory fields can produce non-null, invisible or obstructing owners. AR10 confirmed all 27 corruption cases returned false success. Every exact staged owner now receives part/field contract validation before any native owner or reservation is committed. |
| World manager seed 0 is an explicit deterministic zero | Its constructor treats zero as an Environment.TickCount request. AR09 exposed a fixture comparing that actual world against a literal-zero pure plan. Manager-backed cases now use 2048; pure-plan zero coverage remains. No production seed behavior changed. |
| Address eligibility proves a managed graph belongs to this composition | A runtime village/camp at an eligible address owns its own native pipeline. AR10 proved the renderer still claimed it. AreaCompositionScope now attaches each managed graph to its own world map, checks current POI authority and is reattached on restore/cache access without a saved marker. Standalone test graphs retain finite-address authority. |
| Benches promise resting or underreading | New pilgrimage fixtures are physical examinable objects only. No Restable, Harvestable, underreading, conversation, or invented historical inscription. |

Read: CLAUDE.md; PERF-FOUNDATION.md; Lore/10_Bible.md; Lore/Design/THE-OVERWRIT.md;
Lore/Design/UNDERREADING.md; Lore/MYSTERY-LEDGER.md; FELLING-WORLD-DESIGN §3.5;
WorldMapAuthoring/WorldGenerator; FormationSelector; OverworldZoneManager;
DesertBuilder; PopulationTable; LandmarkBuilder; HazardTerrainBuilder;
ContainerPlacementService; HaulablePropBuilder; TraderRestockSystem;
the exact Objects.json and LootTables.json definitions.

## Implementation contract

`OverwritCompositionPlan.Create(id, seed)` produces cold-generation data.
`IsWildernessZone`, `IsRimZone` and `InwardQuarterTurns` use a finite cached
address index, with no plan construction in rendering. Local prefab front +Z
means map north; inward facing follows the true neighboring biome.

All cells receive OverwritGround. New growth is sparse, clumped, walkable,
destructible and flammable, with one fixed visual height. Only the pilgrimage
rim occasionally has one bench and a blank stone waymarker; both have real
native destruction. Three-cell approaches share their portal positions with
adjacent Overwrit chunks and keep fixtures away from arrivals. No floor height
changes, stones, ruins, roads through the interior, bleeds or restored towns.

The builder stages every native entity before placing any, so missing or
malformed content cannot leave a partial zone. Each exact instance must have
the expected identity, visible native glyph/layer, physical and examine
contracts, appropriate destruction/material/thermal behavior and no unintended
hazards or verbs. Checks neither create replacement owners nor reassign entity
IDs. A failure emits OverwritCompositionRejected with the blueprint and failed
contract; successful installation emits OverwritCompositionPlanned. It never
rebuilds a populated zone. No persistent
plan is required: the resulting cells and owners remain native authority.
Ground is the single-appearance exception to art variation; repeated growth
and fixtures receive four coarse variants, with at most two swatches each.
All four ground entries share identical visible top geometry and color; their
buried thickness may vary. Growth has one tip height across every stem and
variant. Furniture front +Z faces inward through the cached address rotation.

### Native content and circulation

OverwritGround is non-solid, non-takeable Terrain with no glyph variants.
OverwritNewGrowth remains walkable, has structural HP 4 and native Plant
material/thermal behavior. The stone waymarker has HP 24, hardness 2 and
native Rubble wreckage. The wood bench has HP 12, hardness 1 and native
flammability. Neither fixture adds a resting, climbing or underreading verb.
Material values follow the nearby shipped vegetation/furniture conventions;
this feature does not claim a newly tuned fire-spread balance.

The actual Sill Arcanist in The Inkwell keeps its native identity and shop
stock table. The new DryingBreeze entry joins the other utility books and
three workshop schematics. Tests empty the real shelf and advance beyond
the native 300-turn restock interval; the exact 300-turn boundary is a
non-renewal countercheck. The Unsaying keeps its original Ruins catalog,
including possible archive/library/workshop and human field-scribe sources.
Reducing ordinary ruin scatter intentionally reduces those incidental
encounters from the prior placeholder distribution; this does not author
the Unsaying or guarantee a field-scribe encounter in every world seed.

## Performance and honesty

Only fresh generation allocates plans and staging collections. Runtime address
and facing checks use cached dictionaries; the managed-graph scope check uses
a weak zone-keyed context and the current map's biome/POI lookup. Existing
dirty-cell ownership and mesh batches handle rendering; no runtime voxel
construction, per-voxel GameObjects or new per-frame generators/allocations.
Static previews establish composition, not live feel, animation or measured
frame rate. No save migration is requested.

## Self-review and deferred work

- 🟡 Corrected after observed RED: ordinary rim-world routing still admitted
  cave stairs into the blank interior; scoped cell filtering now protects the
  physical margin. AR17 verifies the correction.
- 🟡 Corrected after observed RED: removing ordinary library stamps made an
  already missing shop entry material to circulation. Drying Breeze is now
  in ArcanistStock; native renewal passes in AR09 and AR17.
- 🟡 Corrected after observed RED: all 27 malformed non-null native owner
  hypotheses in AR10 falsely succeeded. Exact-instance staging now checks
  semantic contracts before committing terrain or reservations; AR17 GREEN.
- 🟡 Corrected after observed RED: runtime POI graphs at eligible addresses
  could inherit presentation authority. The zone-local map hook now vetoes
  them, releases an already bound surface, and permits a later valid Bind to
  recover without regenerating any native owner. Restore/cache and separate-
  world counterchecks pass in AR17.
- 🔵 Fixture correction: actual world seed 0 and literal pure-plan seed 0 were
  different worlds by existing constructor contract. Native world comparisons
  now use explicit seed 2048, preserving pure-plan zero coverage.
- 🟡 Corrected after independent review and AR16 RED: movable Ginmere family
  variants still included their zone address, rerolling an unchanged torch or
  frog across the descent. The fix removes that component only for movable
  new-family owners. All five cross-depth regressions and static-scenery
  counterchecks pass in AR17; generic single-body aliases remain symmetry pins.
- 🧪 Final full-suite comparison is pending; AR17 covers the targeted feature
  and surrounding manager/sinkhole tests, not the entire game.
- 🔵 Corrected: named-place checks alone miss the Unsaying; old library comments
  overstate exclusive circulation because ArcanistStock now has most entries.
- ⚪ No authored Unsaying, threshold triggers, bleed mask, held bleed, deep
  fragments, prior-world explanation, regional regrowth or Vein Pressure work.
- 🧪 Live input feel and runtime performance need separate live evidence.
- ⚪ The render/source contracts are CoO-original. The review reference for
  the art/composition pass is the game's Overwrit canon, not a Qud parity port.

### Independent final cold-eye review

Re-read current shared scope and movable variant integration after AR17. No
remaining material bug found within the reviewed lifecycle, ownership and
movement surfaces. The scope hook covers new generation, cached access,
SetActiveZone and ReplaceLoadedState. Actual save decoding replaces the map
before attaching restored graphs. No new saved field or gameplay re-creation
is involved. Rejected presentation releases its owned surface, equipment views
and patch meshes while leaving borrowed assets and native owners intact.

Inspected all 21 images in `AR18-final-native-preview` at seeds 64, 1729 and
729490642: four Overwrit compositions and three Ginmere levels. Overwrit stays
flat, stoneless and nearly empty; equal-height shoots cluster in the appropriate
margin. Green/Dry/Pilgrimage are adjacency roles, not permission to invent
different busy ecologies. The three sampled pilgrimage views have no benches;
rare fixture presence, accessible aprons, inward orientation and destruction
are established by separate native/mesh tests rather than claimed from these
screenshots. Ginmere's broad cliffs, connected terrace spaces and irregular
water-bank frog placement read distinctly, with no remaining material visual
regression observed. The hole is still native cell/void presentation, not a new
physical height simulation.

Can verify: native identities, exact scope, targeted tests, current mesh
ownership, static geometry and composition. Cannot verify from these captures:
live movement feel, animation quality, long-session performance, every seed or
authored W7 mysteries that this feature intentionally does not implement.

## Implementation log

- 2026-09-15: Independent canon/native sweep completed. Initial32 native cases
  authored before production. Root confirms isolated AR02 compile RED from
  missing Overwrit types. Baseline AR01:12,262 total,12,230 pass, the32 known
  baseline failures, zero C# errors. Native types/content may now proceed;
  DryingBreeze loot edit remains held for a compiled native restock RED.
- 2026-09-15: Implemented finite scope/facing queries, seeded logical placement,
  native staged builder and four surgical object blueprints. Initial native
  content and layout specifications compiled and exercised actual manager
  routing. Added three physical-rim stair cases and a separate 29-case
  adversarial sweep covering strict address parsing, extreme seeds, content
  failure, native destruction, rare furniture, runtime POIs, all 22 complete
  chunk pipelines and the exact retained Unsaying circulation catalog.
- 2026-09-15 AR04: Actual compiled restock test failed specifically because
  DryingBreezeGrimoire was absent from renewing ArcanistStock. Added only that
  Chance 30 entry after the result; JSON validated. No stock identity, shelf
  policy, restock timing or other loot table was changed.
- 2026-09-15 AR05: All three new rim tests failed at native stairs inside the
  blank interior: seed 0, Overworld.2.9.0 at (16,8); seed 64,
  Overworld.1.9.0 at (5,12); seed 1729, Overworld.2.9.0 at (69,13).
  Root added the scoped native placement filter after that runtime RED.
  This 39-case snapshot was 31 pass / 8 fail / zero C# errors; its other
  failures covered the still-unintegrated shop snapshot and fallback sprites.
- 2026-09-15: Independent art/native review identified the non-null malformed
  owner gap. Added 27 corruption cases before its fix: missing parts, invisible
  or wrong-glyph render, walkability/takeability conflicts, broken destruction,
  lost material/thermal/examine behavior, and unexpected floor hazards. Each
  uses a successful unchanged-content control and demands zero committed owners
  plus unchanged prior reservations and EntityVersion. Current native source
  has 35 regular and 56 adversarial cases; the expanded runtime gate is pending.
- 2026-09-15 AR09/AR10: AR09 was 255 total / 245 pass / 10 fail / zero C#
  errors. Overwrit's native remaining failure was the seed-0 fixture mismatch;
  the corrected shop renewal case passed. Art review exposed unequal individual
  growth tip heights despite equal overall bounds; the art owner is correcting
  the generator after those four actual RED cases. AR10 was 61 total / 29 pass
  / 32 fail / zero C# errors: all 27 malformed native owner cases failed as
  predicted, while the prior 29 Overwrit adversarial cases passed. The other
  five failures were shared runtime-POI presentation scope and Ginmere owner
  coverage, owned by the root integration pass.
- 2026-09-15: After AR10 RED, implemented exact staged-owner contracts and
  blueprint-specific rejection reasons. No factory re-creation, repair of
  existing owners, new save fields or global generation policy was introduced.
  Changed only manager-backed zero seed attributes to 2048. Current code is
  ready for focused GREEN verification; final native previews/full suite remain
  pending.
- 2026-09-15 AR16/AR17: Independent shared integration review found the
  cross-depth movable-body reroll and authored a separate 20-case rendering
  adversarial fixture. AR16 was 307 total / 302 pass / five fail: the five
  cross-depth Ginmere families were actual RED, while within-zone movement,
  shared single-model roles and static coordinate variation remained correct.
  Root then removed the zone component only from movable new-family variant
  keys. AR17 is 350 total / 350 pass / zero fail / zero C# errors, including
  43 surrounding manager/sinkhole cases. Native malformed-content, circulation,
  rim, scope-lifecycle, art and rendering gates are now GREEN.
- 2026-09-15 final review: Inspected the final AR18 image set directly after
  it became available (21 native views), plus current scope and cross-depth
  production. No remaining material finding in this bounded review. Honest
  visual/lifecycle limits are recorded above; full-suite close-out is pending.

## Files owned by this implementation

- NEW OverwritCompositionPlan.cs and Builders/OverwritCompositionBuilder.cs.
- NEW OverwritCompositionTests.cs and OverwritCompositionAdversarialTests.cs,
  with metadata.
- NEW AreaCompositionRenderingAdversarialTests.cs, with metadata, for the
  independent shared-rendering movement review.
- Surgical Objects.json native identities and LootTables.json ArcanistStock
  correction.
- This living document. Root owns manager/rendering integration; the art agent
  owns the reusable voxel kit. No unrelated edits, staging or commits here.

## Final close-out

AR21 targeted:396/396. AR22 full:12,570 total,12,538 pass,32 failures whose
names and messages exactly match AR01; zero new failures and zero C# errors.
All307 new feature cases pass, plus one added surrounding equipment control.
Final AR18 native gallery contains21 views with zero missing meshes and zero
unmodeled visible owners. Main-project art is installed;405 source/art files
match the isolated copy and196 task metadata GUIDs have no collisions. Static
captures do not establish live input/HUD/lighting feel or sustained FPS. See
`OVERWRIT-GINMERE-COMPOSITION-PLAN.md` for the exact mixed-file commit boundary.

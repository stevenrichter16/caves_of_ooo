# The Sodden: floodland composition

Status: complete and installed, 2026-09-12. CoO-original generation and art;
no Qud parity claim. Canon authority: Lore/History/02_Geography.md (Sumphold,
Drowned Ledger), shipped W3 content and its documented mechanical boundaries.

## Design and staged work

The flood never fully drained. Compose broad tea-dark basins, irregular raised
islands, and traces of peat work before placing vegetation. Negative space is
water and low ground; tall growth belongs in coherent stands. Six existing
FormationSelector identities remain authoritative:

- Open mire: three merging basins around connected tussock islands.
- Peat cuts: staggered flooded workings with bank faces and working aprons.
- Reed maze: braided reed stands divided by wide, legible dry channels.
- Drowned copse: bare snag clusters in shallow water, with native maw-toad dens.
- Causeway: one continuous worn board route; mire presses against its flanks.
- Bog face: an exposed stepped peat escarpment, pools at its foot, work breaks.

Seeded waterline, island positions, orientation and working condition vary the
composition, retaining shared three-cell edge approaches and open-pocket reach.
Build the abstract plan first, then native entities, then independent presentation.
Named places, sinkhole mouths and shared POI pipelines retain their authored
content. Ambient hazard, landmark, population and loot passes remain in use.

Milestones: (1) RED and native plan/realization; (2) additive voxel kit and renderer
wiring; (3) multi-seed previews, adversarial native checks, independent review/fix,
full suite comparison and installation receipt. No camera, spawn or reveal change.

## Verification sweep / corrections

| Premise | Verified implementation / decision |
|---|---|
| Wet-looking terrain is interchangeable | MirePool owns LiquidPool bog-mire, damage/fire/gas and TileStateSource. Use native pools; never replace hazards with water paint. |
| Pools need permanent wet stamps | Current source is a renewable four-turn lease. Seed the existing source; destruction permits drying. Only copse shallow water is an intentionally independent coating, as in W3. |
| Reeds conceal, mire sinks, gas explodes | Current W3 ships non-solid reeds without FOV concealment, bog-mire is not Sticky, marsh gas is poison. Do not advertise unimplemented behaviors. |
| Everything is already destructible | PeatBank HP10, DeadTree HP12, Duckboard HP8, MirePool HP200 are destructible. Reeds and ordinary BogTakenBody have no DestructiblePart; retain existing semantics explicitly. |
| Replacing the formation builder is cosmetic | It also guarantees 1–2 copse MawToads and 0–2 ordinary bodies adjacent to surviving cut banks. Preserve these hooks in realization. No pre-Felling witnesses outside their named site. |
| Water is protected from later scenery | Reserve wet cells and approaches through existing GenReservedCells; additionally test actual later pipeline traversal. |
| All Sodden region names use its biome | Sumphold is a Spread POI. Eligibility follows authored biome and place/sinkhole exclusions, not prose proximity. |

References verified: SoddenFormationBuilder, SpreadCompositionPlan/Builder,
OverworldZoneManager, BuilderSpawn, TileStateSourcePart, Objects.json's Sodden
blueprints, SpawnRing3DRecipes/Presenter/Catalog/Library, VoxelWorldPresentation,
SpreadVoxelLibrary/KitBuilder and composition preview harness.

## Content readiness and art

Native content 🟢; biome-exclusive scenery art 🟡 being authored; unsupported
creature art ⚪ keeps existing fallback. Six families × four coarse variants:
quiet bog ground, contiguous mire surface, layered peat bank, forked dead snag,
duckboard, and a prone ordinary bog body. Reuse the established reed family.
Each model uses at most two palette swatches and one combined mesh. Every visible
fixture belongs to its actual native cell and entity, so destruction removes its
own model. No manual edits to a demonstration layout and no new blueprint lore.

## Performance and gates

Finite authored address lookup, bounded generation masks, cached model identities,
prebuilt mesh assets and existing dirty-patch batching. No new per-frame generator
or scene object per voxel. Verify deterministic replay and seed variation, malformed
addresses, shared portals, every open pocket, no duplicate pools, resident/body
siting, nonempty/missing-content rejection, source-water lifetime, removal/visibility
recipes and full native traversal. Compare full suite with SP16's 32 existing
failures. Visual receipts distinguish overview renders from an active play session.

## Review and implementation log

- Pre-change six mixed integration files captured under /tmp/coo-sodden-before;
  SD00-start records baseline status/hashes. User's running editor is preserved;
  batch work uses /tmp/coo-spread-validation.

- SD01/SD02: missing plan/builder and art-library types confirmed RED before
  implementation. SD03: 50/50 composition, art and legacy formation tests pass.
- SD04: 27/37 adversarial checks passed before wiring. Ten expected failures
  proved the old manager routing, obstructed semantic entrances and missing Sodden
  art recipes. Installed the six narrowly scoped manager/presenter integration
  changes, including shared reed registration and per-native-pool water art.
- The preview fixture address was corrected from Spivenor (16,4), a protected
  sinkhole mouth, to ordinary Sodden (16,3). Runtime eligibility always excluded
  Spivenor; no authored mouth was regenerated or given wilderness composition.

- SD05: 588/589 broader checks passed. The sole failure was the existing
  world-edge transition fixture lacking the newly required Sodden identities;
  production correctly rejected the incomplete content pack. Added six minimal
  fixture blueprints, preserving its transition assertion and production guards.
- SD06: eighteen full native-pipeline previews, all six formations at three seeds,
  zero unmapped mesh sources. Visual review found short snag silhouettes and a
  tiled-looking causeway. SD08 then confirmed four explicit art-readability RED
  checks before the generator's source mesh rules were refined.
- SD07: 595/595 targeted checks passed. Independent review included 256 more
  copse layouts across eligible addresses and signed seeds; the hypothesized
  resident-created pocket did not reproduce. Four real-presenter tests confirm
  removed owners disappear, surviving controls remain, and distant batches do not
  rebuild. Shared reeds and all Sodden prebuilt meshes are borrowed unchanged.

- SD10 refined all eighteen native previews: zero unmapped meshes. The repeatable
  art builder now makes 2.78–3.20-cell dead trees, 1.18–1.39-cell cut faces, and
  three crosswise weathered planks per duckboard. Horizontal footprints stay one
  native cell. Broad ground and mire surfaces retain a single constant swatch.
- Review classifications: ten integration gaps confirmed RED→GREEN; one minimal
  fixture gap corrected; four visual/readability rules confirmed RED before art
  refinement. The remaining native adversarial hypotheses pinned correct behavior,
  including removal→drying, residents, scope and batching; no new mechanic bug was
  found in those samples. Extended play balance and every possible seed are not
  inferred from these checks.

## Installed scope and reproduction

Ordinary authored Sodden wilderness is selected by the manager's no-POI arm.
Shared camps/lairs, named sites and sinkholes retain their existing native layout.
The presentation predicate is a finite authored-biome set; optional camps inside
that set can use the additive art without being regenerated. Cached/saved chunks
retain their native state. No save migration or automatic scene reset is introduced.

Run `CavesOfOoo.Editor.SoddenVoxelKitBuilder.Run` in an isolated editor to rebuild
Resources/SoddenVoxel3D. Set SODDEN_PREVIEW_OUT and SODDEN_PREVIEW_NATIVE=1, then run
`CavesOfOoo.Editor.SoddenCompositionPreviewBatch.Run` for the complete eighteen
native-pipeline previews; BuildAndRun also regenerates assets. Source metadata and
asset GUIDs are preserved by regeneration. No demonstration scene is manually edited.

Can verify: native generation, all-open-cell sampled reachability, four-way sampled
final-pipeline approaches, hazard-free reserved approaches, pool source lifetime,
actual presenter removal and stable distant batches, bounds/palette/mesh identity,
and gameplay-pitch rendered overviews. Cannot verify: extended balance/feel, a live
player lightmap/HUD session, all-address/all-seed traversal or frame-time ceilings.
Unmodeled native entities retain ordinary fallback art; this is a six-family scenery
kit with shared reeds, not a claim that every Sodden creature has a new model.

Final receipts and source delta live in Verification/VoxelWorld/SD09-final.
Native preview gallery: [six formations](Verification/VoxelWorld/SD09-final/gallery.html).
The running user session is preserved. New layouts become available on generation
once Unity reloads the updated source; previously visited chunks are not rebuilt.

## Final gate

SD11 full suite: **11,994 total / 11,962 passed / 32 failed**, zero C# compiler
errors. Exact failed-test names match SP16: **zero additional failures**. All
82 new Sodden cases pass. SD07 broader targeted gate: 595/595 before the four final
art-readability cases, which also pass in the full run. Independent cold-eye and
hypothesis sweep complete; no unresolved material findings. GUID audit: 59 new
metadata files, zero collisions. Installed/isolated equivalence: 124 files, zero
mismatches. Six mixed source deltas are captured in implementation.patch, whose
reverse-apply check passes; unrelated foundation changes remain unstaged.

The game's active session has not been restarted. No camera, spawn, visibility,
content blueprint, AI, save format or liquid rule was changed by this feature.

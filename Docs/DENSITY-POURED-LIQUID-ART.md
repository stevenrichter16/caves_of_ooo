# Poured liquid native art — preimplementation sweep

The general carry/pour core is owned separately and remains under its own
transaction, finite volume and actual acquisition gates. This rendering slice
must not change any liquid definition, source, volume, coating or save semantics.

Current verified gaps and seams:

- PouredLiquidPool has no native recipe. The existing three density pool models
  resolve exact natural blueprint names; oil additionally borrows the tar seep.
- There are26 registered liquid IDs, all with glyph ~ and12 distinct native
  color codes. Existing authored colors are not sufficient for all26 identities.
- The shared Stillleaf spring mesh is a24-vertex flat cuboid. It can be borrowed
  without introducing invented rocks, containers or source models.
- Native render surfaces clone only registered material families once per bind.
  Ground batching currently supports world/water/glade families. Rather than
  change that machinery for player-created finite pools, render each poured
  pool as a normal static owned view (nontransient, nonbatched). Twelve cached
  private prefab/material variants borrow one existing flat mesh and the current
  native fog shader. A uniform white texture and native parser color express
  the actual registry color; no per-frame geometry or material copies.
- Recipes are read-only. Require exact PouredLiquidPool identity, actual current
  zone membership, visible canonical glyph/color, no authored VisualID/variant,
  positive LiquidPool volume, initialized known liquid and stationary physical
  ownership. Unknown/empty/malformed or carried/equipped/takeable owners retain
  ordinary native fallback. No unknown-to-water alias.
- A live LiquidId change must select a new cached model after ordinary Refresh;
  positive quantity changes keep the same appearance. Hidden, emptied or removed
  owners relinquish their views. Current terrain coatings never grant a second
  pool owner or fabricate liquid volume.

Before production, run the new native fixture against current code: positive
actual-recipe/visible-owner/color/switch checks must fail and refusal/ordinary
controls must pass. Then use the same actual source/owner cases after import,
including water/oil/acid distinctness, every registered definition, shared mesh
and material preservation, actual fog/removal, no unmapped mesh and scene-owned
material cleanup. The separately authored liquid keyboard audit is the final
acquisition/quantity/save and actual pixel gate; test colors alone are not visual
acceptance. No shared renderer/source writes are authorized by this draft.

## Actual native RED and private implementation

The native125-case selection saved as
`Docs/Verification/DensityCompletion/Integration/native-sixth-poured-renderer-red.*`
ran this fixture's26 cases: both required native representation cases failed,
while all24 refusal/existing-natural-pool controls passed. The broader selection
also contained six independent stale glade measurement pins; those are recorded
in the glade art living document.

The private implementation adds a validated12-color library, exact current-owner
recipe, cached model/catalog lookup and bind-time material registration. Each
prefab uses the identical shipped spring0 mesh. Hex color-code filenames avoid
case-insensitive filesystem collisions. One owned white pixel and twelve owned
materials carry exact existing palette colors; no global atlas or shader change
is needed. The importer preflights the full bounded output set and saves only
its specific assets, never broad scene/source assets.

Offline compilation against Unity references passed with zero errors. Its
expected duplicate private/source type warnings do not establish editor import.
`PouredLiquidArt/private-candidate-manifest.json` names the eight private source
files and builder. Independent review, actual import/native GREEN and gameplay
water/oil/acid screenshots remain pending.

## First native acquisition attempt and visual scope

The bounded importer completed12materials/12prefabs using the one shipped
24-vertex spring mesh; `PouredLiquidArt/native-build.json` and
`native-generated-assets.json` record the actual output set. The first native
acquisition audit `NativeLiquids/e17faf63283a4e36b544a11fdc9ca722` passed17
quantity/acquisition/save checkpoints through oil pouring, then stopped at the
acid source-search precondition. It is not a completed native acceptance.

The water screenshot still frames the previous merchant area while the actual
actor/poured owner are at3,3/4,3, so that image cannot prove water-body visibility.
The oil image shows a small gray flat pool beside the actor. Stronger capture
preflight is being prepared: current actual owner/model claim and on-screen
bounds, with native camera focus rather than a filename-only assertion.

This library integrates with the existing SpawnRing native presenter in its
supported ring/wilderness/town zones. The separately authored Morrowfast3.6
scene is deliberately outside that adapter and retains its current fallback
policy. Native water-model acceptance will carry honestly acquired water to a
supported generated zone; it will not expand special-scene authority merely
to make the audit pass.

Native selection `Integration/native-seventh-liquid-bed-door-material-red` passed25 of26 poured-renderer cases. The sole failure was NUnit exact `Color.Equals` on the imported green swatch. Actual editor values archived in `PouredLiquidArt/native-color-precision.json` are expected0.33 versus actual0.329999983 for red/blue, with green/alpha exactly1; Unity's own color operator already compares these equal. The test now compares every component with1e-6 tolerance, far below a visible8-bit color step. Production, material data, all26 liquid identities,12 distinct caches, shared mesh/white atlas and refusal controls are unchanged. Native rerun remains pending.

The repaired exact-swatch test and all26 poured-renderer cases now pass natively in `Integration/native-seventh-liquid-material-combat-green` (193/193 selected cases). The subsequent real acquisition replay exposed a harness expectation error: authored water is`&c`, so its correct model is`poured-liquid-63`; the earlier hardcoded`&B`/`42` expectation was wrong. The renderer resolved the actual registered definition correctly. The private audit expectation is being changed to derive ModelId from the current definition; no renderer production change is needed. Native capture acceptance and complete acquisition replay remain pending.


### Actual purchased liquid route and visual review

Native run `a513c5b79c7c4c83982ee93ec3db5d0b` passed21/21, zero unexpected errors, with exact restoration. Root directly inspected all three `*-poured-native-body-owner-*.png` frames: water appears cyan, oil muted gray and acid bright green at their selected actual cells; the native side panel identifies each actual puddle and its matching contents. The current flat liquid surface is readable within the existing village presentation. This proves the current three rendered examples and source/owner path, not every liquid or completed C15 biome styling. The matching report records real stock purchases, natural-source fill, conserved transfers and F5/F6 state.

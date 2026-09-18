# Full world art conversion

Status: inventory and verified design, in progress (2026-09-11 local).

The user authorized converting all world sprites and glyph-only world content to
actual Blender-authored 3D assets, continuing the established stylized Felling
direction. This extends the existing village, eight ring chunks, and multi-cell
pilot. It is not complete when FBXs merely exist: each identity must resolve in
the game, preserve current state, and pass the native acceptance gates.

## Scope and readiness

- 🟢 Existing 392-model kits provide the palette, coordinate convention, shaders,
  equipment sockets, native fog mask and 56-degree camera. Preserve approved art.
- 🟡 The initial scan found 454 blueprints and 486 sprite PNGs. These are different
  inventories: an animation sheet, terrain variant, and blueprint are not each
  a distinct creature. Resolve inheritance and record every source separately.
- 🟡 Existing ring catalog binds 73 blueprints in eight zones; town owners and
  pilot components are additional, location-specific coverage. Neither proves
  global coverage, even when a matching FBX exists.
- 🔴 Uncovered identities, stateful fixtures, glyph reskins and zones require
  explicit designs and runtime integration. Never count a generic placeholder
  or unbound FBX as completed conversion.
- ⚪ UI text/icons remain readable UI; world effects use their existing 3D FX
  pipeline. Keep original sprite files as fallback/reference, not flat 3D cards.

## Milestones

1. Finish the current surface-polish acceptance and record its limits.
2. Produce a reproducible coverage ledger: blueprint inheritance, display name,
   description, anatomy/mechanical parts, sprite references, existing bindings,
   missing assets and explicit design families. Audit unknown and base rows.
3. Design anatomy-specific creatures, profession-specific people, tangible item
   shapes, geological/architectural materials and state variants. Model the
   missing families in Blender, with contact sheets at the game camera.
4. Integrate an independently validated world catalog and renderer routing.
   Preserve authored town/pilot precedence, identity/reskin guards, native
   occupancy, visibility, destruction and equipment. No world generation replay.
5. Reviewed asset batches: items/equipment; fixtures/containers; terrain/plants;
   humanoids; animal anatomies; supernatural/endemic creatures. Each batch needs
   FBX round-trip, Unity import, real scene review and coverage receipts.
6. Close the full inventory only after every renderable identity and dynamic
   visual state has a reviewed native representation. List genuine exclusions
   separately, with evidence, rather than hiding them in a coverage percentage.

## Verification sweep / corrections before implementation

| Reference read | Verified consequence |
| --- | --- |
| Objects.json | 454 rows, inheritance-based parts; do not rewrite the blueprint file to author art. |
| EnvironmentSpriteRenderer.cs | Named/creature/fixture tables plus glyph and state routing; table scraping alone is incomplete. |
| EntityVisualCatalog.cs | Explicit VisualID can override blueprint appearance; canonical glyph guards prevent false identities. |
| SpawnRing3DRecipes.cs | Existing resolve rejects zones outside its catalog and unmodeled identities. |
| SpawnRing3DPresenter.cs | Eight-zone hard gate; imported art does not automatically become global presentation. |
| SpawnRing3DLibrary.cs | Strict complete catalog/model/material validation; preserve existing contract. |
| Existing three source manifests | 392 models include variants, grounds and components, not 392 globally covered blueprints. |
| mesh_kit.py | Real volume primitives, palette UVs and corrected static normal export can be reused. |

This is CoO-original presentation work, not a claim of Qud implementation parity.
No save migrations are required by the user. Existing save behavior remains a
useful destruction/ownership regression gate, without adding a migration system.

## Performance and acceptance

Read PERF-FOUNDATION before runtime edits. Resolve catalogs at bind, cache
identity/state lookups, batch stationary geometry, invalidate only affected
cells, and allocate no per-frame collections. Profile 60–90 seconds in real
gameplay; compare max as well as averages. Keep camera position/zoom unchanged.

RED→GREEN tests precede new runtime behavior. Positive/counter pairs cover
identity, state, zone, destruction, visibility and transfer. A dedicated
20–60-case adversarial sweep probes duplicate owners, missing assets, changed
glyphs, empty cells, multi-cell damage/selection, equipment, reload and disposal.
Native scenarios must measure actual gameplay and include screenshots for art
review. Logs distinguish engine/plugin failures from gameplay failures without
discarding either. Cold-eye and player-flow hypothesis reviews precede closeout.

## Implementation log (append-only)

- 2026-09-11: User expanded the polish request to full world-art conversion.
  Read-only sweep established the inventory and global-rendering gaps above.
  No new global rendering or asset completion claim yet.

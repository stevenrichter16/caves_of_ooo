# Working prompt: reconstruct the Felling-Site as components

Act as the game's technical artist and environment implementation engineer. Plan and build a component-based reconstruction of the selected Felling-Site reference, including plausible surfaces currently hidden behind independently changeable objects. Deliver executable preparation tools, actual assets and an interactive removal/reassembly review, not merely a concept sheet or a list of future tasks.

## Inputs and authority

- Exact composition: `ArtSource/FellingSite/reference.png`, 1536×1024. Preserve this source and its hash.
- Existing actor-removal patch, ground-repair study, layout, coordinate rules and previous verification under `ArtSource/FellingSite/`.
- Current canon: `Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, and `Lore/MYSTERY-LEDGER.md`. The cliff is petrified sandstone anatomy; exactly six barren positions; seventh refusal remains a subtle absence. Invent no new symbols, beings, powers or ending mechanics.
- Work remains offline, outside `Assets/`. Respect unrelated working-tree changes. Do not open Unity, change gameplay files, or represent browser behavior as tested Unity behavior.

## Required reasoning before implementation

1. Audit the current artifact and distinguish original pixels, generated inference, runtime clipping and actual independently exported sprites.
2. Inventory visible objects and explain the granularity. Each readable plant/fungus cluster, loose rock and major root section must either receive a stable component ID or an explicit static/decorative classification. Avoid counting indistinct moss grains as fictional harvestable items.
3. Separate appearance from gameplay semantics. Define candidate interaction categories and footprints without inventing balance, drop tables or narrative actions. Monumental boundary geometry and the six/seventh landmarks are protected.
4. Define extraction masks, source rectangles, anchors, depth, contact shadows, overlaps and dependencies. Separate true silhouettes from conservative ground/decal collars; a patch containing ground must not masquerade as a freely movable object.
5. For every removable component, identify the visible surface behind it: soil, moss, path, water, another component or a fixed cliff. Reconstruct that specific surface. The source image does not contain hidden pixels; label their provenance as inferred.
6. Explain how alpha and hidden-surface coverage will be verified. Prior Imagegen transparency attempts returned opaque checkerboards; do not repeat that assumption or bypass the asset gate.
7. Write the detailed implementation plan to disk, with milestones, concrete outputs and acceptance checks, before writing the production pipeline.

## Implementation requirements

- Preserve known source pixels and authored positions. Use Imagegen for genuine missing-surface synthesis. If the user authorizes deterministic image-processing scripts, use them for pixel-preserving extraction, masks, alpha exports, compositing and verification; do not use generative redraws as proof of exact extraction.
- Export real RGBA components with meaningful alpha, per-component metadata and clean backing surfaces. Keep original and generated source images separately for reproducibility.
- Ensure the underlying base contains no baked duplicate of extracted mutable objects. Restoring a sprite must reproduce the intact image; hiding it must expose its backing surface. A perfect intact comparison over an unchanged background is not sufficient evidence.
- Support multiple objects removed simultaneously without stale pixels, unrelated disappearing objects, or one repair patch painting over a surviving neighbor. Protect the six barren areas and tiny seventh absence from extraction/removal.
- Respect physical plausibility: hiding a fixed boundary is an inspection operation, not permission to delete a cliff in the game. Do not fabricate unseen rooms behind it.
- Produce a versioned manifest with stable IDs, source-space masks/rectangles, normalized pivots, world/cell mapping, state permissions, depth/layer order, backing dependencies, alpha metadata, source hashes and readiness findings.
- Build a separate browser review tool with original/reconstructed/base views, component selection, masks, hide/restore, remove-all mutable, solo/exploded inspection, fit/native-size viewing and visible validation results. Review-only actions must be visibly distinguished from candidate gameplay actions.
- Keep the earlier scene movement study usable. Update the Unity handoff to consume these actual exported assets, while leaving Unity adapters/import/runtime testing explicitly unrun.

## Verification and review

Use test-first development for the extraction/state pipeline. Verify bad polygons and paths, invalid IDs, missing underlays, alpha range, bounds, overlaps, protected regions, pivots, deterministic outputs and invalid state transitions. Pair successful operations with counterexamples. Include a separate adversarial review of parser/state/ordering failures.

Measure intact reassembly against the approved actor-free baseline. Inspect individual removals, adjacent removals and all-mutable removal at native size. Check thin stems, roots, contact shadows, moss/path transitions, floating remnants and checkerboard halos. Keep masks editable and fix concrete findings before delivery.

Report separately: code invariants passed, raster measurements passed, visual observations, extraction-quality limits and Unity work remaining. Never call an opaque study a sprite, a conservative patch a precise silhouette, an inferred surface an original recovered surface, or an unexecuted Unity step complete.

## Deliverables and completion

Save this prompt, the implementation plan, source inventory, generated backing sources and prompts, actual component PNGs/masks, cleaned base, manifest, tests, measured reports, interactive viewer and integration notes in the repository. The intended result is a reviewable layered asset package in which removing a component visibly exposes reconstructed content beneath it. Continue through implementation and review; do not stop after writing the plan.

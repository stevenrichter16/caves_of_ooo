# Morrowfast: the southern approach to the Felling

Status: **art, extraction and interactive component preview delivered**, 2026-09-06. Native Unity integration is a separate next stage.

## Intended result

An inhabited chunk immediately south of the Felling at world (3,6), with a legible north–south approach, a small guardian fellowship, houses, shops and lived-in details. Generate a complete high overhead Imagegen scene, preserve it, then build a layered package with independently owned objects and inferred hidden surfaces. The prior Felling and Catacomb assets remain intact.

Morrowfast and the Stillcord are new local design proposals grounded in the root `Lore/` authority chain. They do not change the Felling's origin, the seventh refusal, existing gods or unique faction powers. Detailed inhabitants, services, quirks and quests are maintained in `FELLING-SOUTH-SETTLEMENT-DESIGN.md`.

## Production stages

1. **Lore and layout.** Check the authority chain, world (3,6), adjoining Felling route, existing blueprints and interaction systems. Define the new watch fellowship, five buildings, open lanes, services and optional jobs. Keep admission recoverable and the existing Felling accessible.
2. **Master image.** Use built-in Imagegen for a 1536×1024 high overhead scene. Preserve the exact prompt, generated output and SHA256. Review camera, route clarity, building/prop separation, palette and object inventory. Correct concrete issues before extraction.
3. **Component inventory.** Author stable semantic IDs and reviewed masks for buildings, roofs, doors, gate parts, stalls, furniture, small scenery and inhabitants. Distinguish true silhouettes from ground/contact collars. Indistinct moss grains remain surface texture rather than fictional interactive objects.
4. **Hidden surfaces.** Use Imagegen for a cleared outdoor plate and roof-off interior source. Infer soil/path beneath removable objects and walls/floor/furnishings beneath roofs. Keep generated source images separate; incorporate inference only inside explicit owned masks, retaining known source pixels outside them.
5. **Extraction/export.** Use the user's existing authorization for deterministic scripts. Export real RGBA source components, owned contact contributions, reconstructed base, interior layers, masks, atlas/contact sheets and metadata. Coupled changes must not erase surviving neighbours or leave baked duplicate subjects.
6. **Interactive authoring review.** Provide movement, selection, remove/restore, roof reveal, doors and arch ownership and exposed-surface inspection. Include visible ownership and interaction metadata. Label review operations honestly and keep any browser verification distinct from native Unity gameplay.
7. **Unity handoff and native work.** Map the overhead image explicitly onto the existing 80×25 zone. A 1536×1024 source fits uniformly at 40.96 pixels/cell, spanning 37.5×25 world units, centered at x40. Align the north/south approach at x40; do not reuse the Felling's 224-pixel cliff overhang. Reuse native dialogue, trade, inventory, harvesting, doors and persistence where verified. Mark unimplemented services and quest concepts as proposals.
8. **Verification and polish.** Measure intact source reconstruction, validate alpha/bounds/IDs/support ownership, inspect single/adjacent/all removals and every opened interior, test route/collision/state persistence, and retain failed checks and corrections. If integrated in Unity, compile and run native checks there before describing gameplay as complete.

## Acceptance

- A finished overhead master saved in this repository with the built-in Imagegen prompts and provenance.
- An explicit component manifest with source coordinates, anchors, masks, layer order, actions, state dependencies, physical footprints, and hidden-surface coverage.
- Independently exported alpha components, with no removed object duplicated in the backing.
- Roofs reveal plausible interiors; house doors and the open entrance arch have independent ownership states. Entering an interior does not require deleting a whole opaque building patch.
- Intact reassembly reproduces known source pixels exactly. Inferred content is never described as recovered original pixels.
- The central lane, building thresholds, gate approaches and shop interactions are reachable. Polished presentation keeps passages clear and foreground objects grounded.
- Verification distinguishes extracted art, authoring-preview behavior, proposed gameplay and any actual Unity implementation.

## Performance and integration boundaries

Use one cached manifest, cached alpha/footprints and state-driven redraws. Reuse unchanged Felling extraction helpers where their contracts fit; do not inherit hardcoded source dimensions or overwrite existing production packages. Native implementation must use per-cell invalidation and owned renderer cleanup, avoid new allocations in hot paths, and profile actual gameplay under `Docs/PERF-FOUNDATION.md`. No new runtime lore claim or broad faction rewrite is implied by this local settlement.

## Delivered result

The approved master is `ArtSource/Morrowfast/reference.png`; it uses a high overhead RPG view with shallow front facades. Eight built-in Imagegen passes produced the master, camera correction, outdoor ground, furnished/empty interiors, two foreground occlusion repair plates and a final cistern paving repair. All prompts and generated outputs are preserved with hashes.

Stages 1–6 and the art-preview portion of stage 8 are complete. Stage 7 has a detailed native handoff but no Morrowfast Unity code, assets or zone were installed during this pass. The prior Felling scene remains the existing game implementation.

Delivered: **71 owners**, **145 RGBA layers**, **five independent roofs and original doors**, **21 furnishings**, **35 outdoor owners**, **66 per-owner removal inspections** and an interactive workbench at `http://127.0.0.1:8772/ArtSource/Morrowfast/`. The closed reconstruction differs from the approved source at **zero of 1,572,864 pixels**.

The pipeline/state suite contains **34 passing tests**:9 Python extraction contracts and 25 Node preview contracts. The actual exported manifest validates; the north road, five entries and every component's approach are reachable in the open-room state. Closed doors exclude all five interiors. Bridge support, occupancy-safe restoration and state import validation pass. These are authoring-preview tests, not Unity play-mode results.

Polish corrections included camera adjustment; disjoint foreground masks; separate concealed barrel/counter surfaces; furniture offsets that preserve original entrances; clean continuous indoor floors; a second cistern backing generation without old cast-shadow transfer; and precise stall cords, basket bottoms and planter feet. Current reports and visual review live under `ArtSource/Morrowfast/reports/`.

The [package README](../ArtSource/Morrowfast/README.md) gives controls, asset roles and current limits. The [settlement design](FELLING-SOUTH-SETTLEMENT-DESIGN.md) now matches the actual five-house layout and distinguishes its eight-person narrative cast from the six rendered people. Perimeter fences, rock masses and ground flecks remain fixed texture. Shop transactions, quests, faction reputation, movable-actor animation, native room sorting and Unity collision/persistence remain explicit next-stage work.

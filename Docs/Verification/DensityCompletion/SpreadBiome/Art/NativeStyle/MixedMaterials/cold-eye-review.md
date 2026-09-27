# Mixed native static materials: cold-eye review

Status: reviewed implementation published by parent; actual native868 import in progress. Native GREEN and images are not yet established.

## Q1 — Symmetry and cleanup

- Import plan groups original triangle indices by exact output material while retaining the single flattened vertex/UV stream and original root transform. Semantic grouping preserves World and Water separately. The library validates the same material order, slot count and total geometry; no slot is silently merged during validation.
- Both independent and batched proof iterate all required slots. Independent proof checks the actual mesh's submesh and indexed material/property block. Batch proof checks every committed source submesh and the corresponding current owned World/Water renderer, mesh and palette. Disabling either piece fails the whole proof.
- Original-versus-styled caches, current owner gate and empty-dirty-hint invalidation are unchanged by this repair. Borrowed source meshes/materials are never modified. Temporary palette decodes and preview scenes still dispose in finally; generated outputs remain persistent owned assets.

## Q2 — Cross-feature consistency

- The existing single-slot contracts keep exactly one slot and their original diagnostic names. Parent review caught an accidental renamed palette-failure code; it was restored to `submitted-palette-mismatch`, and empty geometry has its own `submitted-mesh-mismatch` result.
- The evidence API retains original representative fields. `PieceCount`/`GetPiece` expose all static body material-slot observations. Existing equipment evidence remains its previous representative record after the independent equipment routine validates all fitted children; this change does not claim a new equipment enumeration API.
- Native recipe/model/state IDs, water shader/ripple fields, source palette cells and existing World/Water batch lanes are preserved. No semantic color is approximated to the green palette in this repair.

## Q3 — Counters and proof limits

- Actual native importer refusal is the behavior RED: four spray pools contain two source material families. All source/output preparation stops before asset writes.
- Executed missing-API compile RED precedes the new13 fixture: four full original-source geometry/UV/triangle/material comparisons; six current submitted-piece disable/mesh/material/root/indexed override mutations with restoration; current owner hide/remove/portable refusal; foreign original rendering; malformed material-slot metadata.
- Original47 residual cases and12 exact-style regressions are included in the parent's planned native selection. The additional independent two-slot branch has source parity and reference compilation, while the actual current spray-pool runtime path is batched. No assertion that these new native cases passed yet.
- Side-effect-free proof relies on the existing committed batch contribution ledger and exact owned submitted meshes; it does not rerasterize or recompute every combined vertex on each query.

## Q4 — Documentation and remaining gates

- Source definitions are not natural occurrence.868-source adoption includes all explicitly reviewed current static IDs, not only seeded sample identities. Rigged actors/portable equipment remain separate.
- The four pooled sources retain source geometry; this is faithful presentation/material adoption, not a model redesign.
- Staged native preview helper yields four closeups and one full arrangement with exact material/water properties and preservation checks. Nonbackground pixels cannot establish readable water-versus-rock silhouettes; images must be viewed. This is not a FOV, live ripple, generated-source or whole-biome claim.
- Import writes are preflighted and bounded, but an external failure after the first persistent write is not transactionally rolled back. Exactly3475 generated resource/meta paths must be derived from the reviewed868 IDs and checked against the actual dedicated folder after the successful native receipt.

No additional concrete P0–P2 defect was found in this pass. Native assertions, complete coverage and visuals remain active gates.

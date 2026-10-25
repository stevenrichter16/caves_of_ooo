# Concept reduction and native review

## Before authoring

The supplied concept gives useful motifs: a long straight diagonal blade, a
plain crossguard perpendicular to it, a short leather grip and a closed metal
pommel. Its printed 16×24 and 8× claims are false as literal source dimensions.
The large concept has more edge steps, outline/shading detail and apparent
colors than one native cell can carry. Those features were deliberately
reduced, not traced or downsampled.

The exact source contract was written into the verifier first. Its recorded
RED lists the six genuinely absent production artifacts. Only then was the
integer-cell Blender generator added and run.

## Native decisions

- Keep a 12×22 occupied silhouette inside 16×24, with clear transparent margins.
- A two-row diagonal step supports a visibly long blade without enlarging the
  frame. The face, cutting edge and spine use existing muted metal-like colors.
- Keep the guard simple and wider than the blade. Preserve its lower diagonal
  line so the item reads as a sword rather than a spear or narrow tool.
- Use four flat leather pixels within a short dark grip and a compact pale
  pommel. No wrap texture or extra highlight spark is required at this scale.
- Do not add flame, glow, frost, magical particles, damage chips, held hands,
  or a status-dependent hue. The sprite describes neutral exact `LongSword`.

## First render self-review

The direct native and 8× contact were visually inspected after rendering.
The silhouette reads as a long sword; the guard interrupts the blade clearly,
and the grip and pommel form a distinct handle. Relative to the accepted clay
and tonic, the long narrow item occupies more height but does not become a
large solid icon. All six colors are approved-kit constants.

Read-only validation passed: 16×24, bounds `[2,1,14,23]`, 66 occupied pixels,
six opaque colors, binary alpha, zero source/contact/parts pixel mismatches,
runtime bytes identical to source and all 88 prior PNG/meta hashes unchanged.

Independent art/source review on 2026-10-25 UTC by
`/root/projection_cold_review/look_focus_draft_review`: no material art blocker.
The reviewer inspected native/contact/parts pixels and independently confirmed
the native dimensions, bounds, six colors, hard alpha, all source/contact/parts
matches and runtime/source byte equality. The reviewer did not reopen/rerender
the Blend file or inspect the sprite in actual gameplay.

The review also caught a later preservation discrepancy: 22 existing PNG
metadata hashes differ from the pre-authoring snapshot after the initial
88-file GREEN. No preserved PNG differs. `preservation-followup.json` records
the exact files and before/current hashes. This author did not touch those
metadata files, infer a cause, or normalize them; root was notified to resolve
the concurrent metadata changes. The original source-green receipt is a
historical successful check, not a current unchanged-metadata claim.

Root native/contact review: pending at this checkpoint.
Runtime and ordinary gameplay acceptance: belongs to the separate integration
packet and is not claimed here.

## Root reconciliation and actual review — 2026-10-25 UTC

Root confirmed the original snapshot captured 22 Unity metadata files with trailing whitespace. Their canonical Git bytes differ only by per-line trailing whitespace; no texture or semantic importer setting changed. `preservation-reconciled.json` records both hashes and the comparison. Final validation uses the canonical committed baseline, retaining original receipts. Root inspected native/contact geometry and ordinary world/reader captures, including equipped, F5/F6 and cellar roundtrip. Full ordinary temper and journey acceptance remain open.

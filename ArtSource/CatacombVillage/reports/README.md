# Catacomb independent verification

Run from the repository root:

```sh
python3 ArtTools/verify_catacomb_scene.py
```

The runner reads existing exports. It does not regenerate sprites, rewrite the scene, or execute Unity. Its current result is `verification.json`; a required failing or skipped test, malformed asset, stale input, or input/output change during the run produces a nonzero exit and a failed aggregate status. Raw test output and individual JSON check results are preserved beside it.

The eight required gates are extraction/composition/adversarial Python tests; browser Node tests; independent source extraction; current input provenance; strict PNG assets; placed-source derivation; independent reassembly; and exact atlas pixels. Source checks reconstruct the documented alpha threshold and polygon exclusions without invoking the extractor. Placed checks independently resize the catalog sprites with nearest-neighbor sampling. Reassembly draws the complete base and exported layers from disk, then compares the intact and all-mutable-removed states and verifies each individual removal changes no pixels beyond its alpha support.

`sourceAndOutputSnapshot` records SHA-256 hashes of the files observed during that run. The producer's source hashes must match the live source sheet, extraction catalog, scene layout, generated background, and producer script. The source catalog must separately match the live extraction authoring and extractor script. These checks detect stale generation rather than trusting an earlier producer report.

The adversarial test development history is retained as separate logs:

- `adversarial-red.log`: the deliberately missing verifier module failed to import before implementation. This was one loader error, not 32 executed assertion failures.
- `source-derivation-red.log`: six newly added derivation countercases ran against the missing helper before it was implemented.
- `atlas-red.log`: nine newly added atlas countercases ran against the missing helper before it was implemented.
- `placed-derivation-red.log`: six newly added placed-sprite countercases ran against the missing helper before it was implemented.
- `adversarial-green.log`: current complete adversarial result, refreshed at the final verification gate.

One filesystem fixture originally compared a canonical `/private/var` path with its macOS `/var` alias. The expectation now resolves both paths; the confinement implementation was already correct. No production behavior was weakened to satisfy that fixture.

Passing these checks proves the recorded data and exported pixel/state contracts. It does not judge scene composition, silhouette quality, visual readability, animation feel, or Unity integration. Browser interaction and visual inspection are separate evidence recorded in the scene handoff.

## Final verification checkpoint

The run from **2026-09-05 20:39:44 to 20:39:46 UTC** passed all eight gates: **86 Python tests and 54 Node tests, 140 total**, with no skipped tests and no watched file changes during the run. This follows the final exploded-view correction and its actual draw-function regression test.

The current package passes 167 PNG checks for 81 components, including 46 mutable props. All 82 placed source derivatives, including the player, match their recorded nearest-neighbor resize. Intact reconstruction has zero changed pixels; all-mutable removal matches the underlay; all 46 individual removals preserve pixels outside their alpha support. The atlas contains all 81 components exactly. Six authored destinations are reachable.

The browser code SHA-256 for this run is `ee69832b507f1c96765721ab634f84a0f4b25d965e6c4ddad64037b620aef5cc`. The complete input and output hash snapshot is in `verification.json`. Earlier preliminary reports are historical checkpoints rather than the final gate.

# Biome grading across travel and Continue

2026-10-27. Named defect found during actual Overwrit native-material acceptance.
Current pass: genuine native RED, minimum correction,533 native passes and actual
matched travel/Continue replay complete; new packages pending. This is CoO presentation behavior, not a
claim of Caves of Qud parity. No sprite repaint or gameplay generation change
is needed for this defect.

## Reproduction and corrected source assumptions

Actual keyboard travel to Overworld.4.12.0 rendered the new floor tan. F5 at
turn5460, fresh isolated Play, and normal Continue restored the same player at
75,18 and the same damaged bench11/12, but the floor became green. Captures
`Verification/Live16x24/overwrit-after-07-bench-damaged-reader.png` and
`overwrit-after-10-fresh-damaged-reader.png` are actual Unity output, not edits.

| Assumption | Observed source/evidence | Required correction |
| --- | --- | --- |
| The patcher follows the renderer after startup. | Actual renderer4.12 versus patcher11.10; cached renderer null. A non-null startup zone prevents renderer discovery in Update. | Discover the renderer independently of whether a caller seeded CurrentZone. Follow its current graph. |
| The startup manager remains authoritative on Continue. | Actual manager reference differs from WorldLocationContext.For(renderer.CurrentZone). Loading replaces the manager. | Resolve the displayed graph's attached manager without generating a zone or altering saves. |
| Shared profile writes update the rendered profile. | Patcher caches sharedProfile overrides; bootstrap subsequently accesses Volume.profile to disable vignette, which clones the profile. | Write the Volume-owned runtime profile used for rendering; preserve the shared source asset. |
| Initial Cave grading is applied. | Active biome defaults Cave and fade is already complete, so first Cave never requests its target. | Apply the first resolved palette even when the enum equals the default. |

The Unity package's local `Volume.profile` implementation confirms that the
getter creates a unique profile and clones shared components. Existing legacy
presentation keeps the same palette values and fade. The fix changes lifecycle
binding, not the accepted sprites, controls, collision or resource ownership.

## Verification plan

Native tests must first fail against the current implementation for seeded
travel, replacement-manager Continue, runtime-profile binding/shared preservation
and initial Cave. Pair current graph checks with subsequent graph changes and
retain the no-volume/explicit-zone defensive behavior. Then run existing palette,
patcher and relevant rendering regressions. Root owns Unity and actual matched
travel/fresh-Continue captures. Packages remain held until this consistency
defect is fixed and observed. Physical Deck performance remains unverified.

## Self-review and implementation log

- 🟢 Fixed: actual travel/reload color mismatch corrected and replayed; matched world pixels agree.
- Test: cached sprite resolution passed its independent checks, but those tests
  did not exercise global post-processing lifecycle; this actual-play gate caught it.
- No source geometry/pixels changed; source preflight above saved before runtime fix.

Files expected: BiomeColorPatcher.cs; a bounded BiomeColorPatcherTravelTests.cs
fixture; this record and current graphics coverage/continuation records; actual
RED/GREEN/capture evidence. Results will be recorded after execution.

2026-10-27 11:10 UTC: native RED completed9 cases,8 failed and1 passed.
The raw job15e84dc135774b6586ab97600d7f84de had a null final result, but the
matching native TestResults.xml was recovered before the next run overwrote it
and is preserved as Verification/Live16x24/biome-grading-native-red.xml.
Missing-volume behavior was already correct; eight failing cases cover the four
lifecycle defects and their alternate setups. The initial stale-discovery job
selected zero cases and is not counted. Runtime edits followed the genuine RED.

The minimum fix binds Volume.profile, independently discovers the renderer,
adopts WorldLocationContext.For(CurrentZone), and applies the first target even
when Cave equals the initial enum. Palette values, override flags, sprite pixels,
gameplay and save schema remain unchanged. Independent source/fixture review
found no concrete Yellow+ issue.

2026-10-27 11:16 UTC: all533 native cases passed, including501 regional/rendering
cases,9 new lifecycle cases and23 existing palette/patcher cases. The MCP job
reported an initialization timeout even while the native runner proceeded;
the exact completed native XML is retained as biome-grading-related-green.xml,
with zero failures/skips/inconclusive. No extra rerun was used to hide the timeout.

Actual isolated Continue restored the damaged bench11/12 in Overwrit4,12.
Normal keyboard travel reached Grovelands0,8 and returned to4,12; local movement
returned to75,18, F5 saved turn6230, and fresh Launch/Continue restored that state.
The renderer and patcher agreed on the displayed graph and current manager.
The rendered runtime profile changed between regional palettes, while the shared
filter remained[1,.97,.91]. The Overwrit world viewport at(0,40)-(940,640),564000
RGB pixels, was byte-identical before and after fresh Continue. Original full
screenshots, diagnostic values and comparison bounds are in
Verification/Live16x24/biome-grading-actual-replay.json. No screenshots were edited.

Both isolated roots were archived intact before ending Play. Normal Editor and
standalone saves and38 unrelated tracked changes remained unchanged. Storage
inspection was read-only. This fixes a global lifecycle fault, but visual
acceptance is bounded to the observed route; whole-region fullness, fallback
crop/stair sprites, and physical Deck performance remain open.

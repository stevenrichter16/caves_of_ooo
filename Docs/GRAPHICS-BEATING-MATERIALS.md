# Beating static material coverage

Current pass: six actual playable baselines and a native trial across all six
areas were captured. Screenshot reinterpretation, editable source and the
original 92-case native GREEN are complete. Actual gameplay rejected green road
reuse and regular dune stripes. The 34-frame correction now passes 95 cases and
actual gameplay/reload checks. Package refresh is next. This is a material and
flora slice, not acceptance of the Beating or the whole world.

Graphics coverage across every biome remains the priority. Preserve existing
animations and defer nuanced new motion. The Beating's intentional sparse
character must read as wind-shaped land with real inhabitants and useful routes,
not unfinished black ground or uniformly repeated texture.

## Actual baseline

Root used the normal scene and controls with an isolated copy of the earned
seed 65 crossing save. Ordinary map travel and descent reached WindBarrens at
16,13, SaltPan at 15,13, BrineLens at 17,13, RuinField at 19,12, CaravanRoad at
19,13 and DuneBelt at 19,14. All six were observed as current Beating graphs with
a null map POI and no saved lair identity. No forced formation, reveal, teleport,
healing or spawn was used. Each has an actual 1280×800 Game capture and an
observational census of existing paint. See
`Verification/Live16x24/beating-baseline-summary.json` for exact actions, counts,
captures, references, limits and restoration.

Dune play also verifies normal movement from 40,12 → 40,13 → 40,14, collision
against the actual crest at 39,14, and actual sand/crest readers. F5 saved turn
2930 with HP 38/40 in `Fixtures/beating-baseline-dune65`. The full owned session
is archived under `/tmp/coo-beating-baseline-earned-d4b82321`. Unity returned to
a clean SampleScene, outside Play, with a null override. Independent hash
comparisons confirmed that normal saves and all 38 unrelated tracked edits
were unchanged. No storage reclamation occurred.

## Concept and production contract

Two built-in imagegen passes use the actual brine/dune captures and the approved
Sodden style anchor. Exact prompts and untouched outputs are retained under
`ArtSource/Live16x24/beating-materials/concepts`. External NPS Lake Lucero/White
Sands references guide quiet water, irregular mineral patches and low scrub.
These are art references, never integrated game backdrops.

Accept warm muted sand, pale flat salt and quiet dark teal brine. Reject generated
grain carpeting, invented pebble scatter, repeated chevrons and tall cliff faces.
Native art is authored as original integer-cell geometry with editable sources.
The production sheet is not a crop or downsample of the concept.

| Frames | Real owner | Required visual meaning |
| --- | --- | --- |
| 0–3 | Sand | Quiet passable warm substrate |
| 4–7 | SaltCrust | Flat passable salt over actual sand, separate from the mineral node |
| 8–11 | DuneCrest | Solid connected low sand ridge, no invented masonry |
| 12–15 | SandstoneFloor | Quiet warm constructed floor |
| 16–17 | SandstoneWall | Actual solid intact warm masonry |
| 18–19 | Rock | Compact solid stone silhouette |
| 20–23 | BrinePool | Stocked pool with sole live brine coating |
| 24–27 | BrinePool | Depleted but still wet; never implies safe dry ground |
| 28 | PaleSaltVein | Solid harvestable pale mineral deposit |
| 29 | Bones | Nonblocking actual bone harvest |
| 30–31 | Saltbriar | Living salt-rimed forage, distinct from dead dry brush |
| 32–33 | RoadStone | Warm packed gravel, distinct from dune, sand and constructed paving |

Borrow unchanged DryBrush from north vegetation and Signpost from opening
anchors where their real semantics match. The initial RoadStone reuse was
mechanically correct but visibly green in the desert. The revised packet adds
original warm packed-gravel frames 32–33 for the same actual RoadStone owner.
Native frames are 16×24 at 16 PPU, with centered pivots, Point imports and shared
textures. All 34 frames form a 544×24 atlas. Variants are static, with quiet
frame 0 favored.

## Source corrections and runtime boundary

This is CoO-original presentation, with no new Qud-parity claim. The actual
composition is `BeatingCompositionBuilder`, not the legacy cactus desert.
`beating-first-kit-plan.md` and the source preflight record the earlier sweep.
SaltCrust is passable layer 0; SandstoneWall need not have Terrain; Rock lacks
Destructible; and DuneCrest lacks required Material, Thermal and Destructible
parts. The three harvest families must not be rejected as foreign harvest
semantics. Brine stock and its independent wet coating are distinct.

Use the current receiving world's map, POI, saved-lair and cached-graph authority
at every lookup. Preserve hidden, custodied and foreign owners, actual substrate,
custom visuals, essential effects, fog, and mixture/ice fallback. Do not widen
opening admission or reconstruct generation during rendering. Suppress duplicate
brine waves only after the corresponding native pool body is actually submitted.

## Remaining acceptance

Native RED→GREEN, counterchecks, source inspection, six actual after views,
world/reader/harvest continuity, save/restart, a rendering profile and independent
source review passed within the bounds below. Fresh packaged verification is
pending. The baseline shows many remaining creatures, residents, tents, wells,
urns, containers and special owners. Those remain missing native families, along
with settlements, underground/special sites and other biomes. Asset counts and
six sampled screenshots do not complete a region. Current packaged builds remain
the verified Sodden source `eafc02a22`.

## First native trial and saved corrections — 2026-10-27 07:04 UTC

All six real ordinary formation entries were replayed with the same seed 65,
entry ticks and player cells. Existing-paint observations found zero unmatched
receipts. Static materials resolve natively. Dynamic NPC positions and one oil
pool differ and are explicitly listed; no claim of whole-simulation byte identity
is made. `beating-first-trial-summary.json` binds the exact 32-frame revision and
captures. Normal walking, dune collision and actual sand/dune readers worked.
F5 earned turn 2930 with HP 38/40. The full owned save root was archived and the
clean original scene restored. Normal Editor/standalone saves and all 38
unrelated tracked inputs remain unchanged.

The original native RED had 92 failures. The first implementation produced
91 passes and 1 failure. The failed test had silently asked stone to accept
electricity; `Entity.ForceApplyEffect` still respects `ObjectStatusMatrix`.
Changing the setup to conductive brine and asserting every effect application
produced 92/92 GREEN, without weakening runtime behavior. Separate regressions
recorded 222 passes, 0 failures and 3 inconclusive cases because allocation-counter
positive controls could not detect allocation. Those cases are not passing
allocation evidence.

Whole gameplay views identified two visual corrections: green road reuse and
repeating full-width dune stripes. The 34-frame source preserves the first-trial
revision, changes only dunes 8–11 and appends roads 32–33. The native tests first
require that binding, plus missing-resource and submitted-ground counterchecks.
The other 28 prior material frames remain exact. This slice does not justify
changes to generation, content scattering, movement, loot, services or mechanics.

The initial 84.6-second Editor profile recorded renderer mean 0.098 ms, p99
0.190 ms and maximum 68.49 ms; MainThread mean 3.22 ms and maximum 1501 ms; and
maximum draws of 37. It includes zoom, reader opening, ordinary walking, and
tool/idle time. This is not a controlled optimization comparison, a standalone
benchmark or a Deck result. Tool and Editor spikes cannot be attributed to the
adapter alone. At this point, revised art and fresh gameplay acceptance remained
pending.

## Final revised native acceptance — 2026-10-27 07:15 UTC

The revised native RED recorded 87 passes and 8 failures out of 95. The saved
34-frame runtime mapping and imported 544×24 Point atlas now pass 95/95 GREEN.
Actual brine missing-body wave cases and submitted Sand, Crust, actor and removal
cases also pass. Independent source reviews found no remaining blocker. The
source verifier confirms 34 distinct frames, 12 rejected negative checks,
28 unchanged prior frames, 20 preserved first-trial files and 310 preserved
input files. Encoded PNG SHA256
`62fb10110d4d19198ce4189e8f5f64f56774fdd78fed2c187ad9b6d28b1efc9e`
exactly matches source and import. Rebuilt decoded pixels are exact. Blender
metadata caused encoded PNG differences, which are documented rather than
claimed to be byte-identical.

The final actual route through all six areas reached the same entry ticks—2650,
2690, 2750, 2830, 2870 and 2910—and player position 40,12 with HP 38. Warm gravel
now fits the actual RuinField and CaravanRoad. Dune masses retain solid contrast
without repeated stripes. These are bounded material acceptances, not complete
region fullness. See the final-play summary for cells, owner differences, source
identity and all remaining sampled glyph families.

The player returned normally to SaltPan, walked eight cells west and one south
to 32,13, then used C/Left/X to show the native PaleSaltVein portrait. C/Left/H
removed actual source 31736 at 31,13 and produced PaleSalt entity 42025, count 1.
The cleared cell became walkable. Actual inventory confirms the product but
still uses a glyph, explicitly queued for resource coverage across biomes.
F5 saved turn 3140 with HP 38/40. A fresh isolated Continue restored the player
at 31,13, the source's absence and the exact product ID/count. Normal saves were
untouched, full owned roots were archived, and a clean SampleScene and null
SaveRootOverride were restored. No source, build or cache cleanup occurred.

The final 96.6-second Editor sample covered walking, harvesting, zoom, readers
and inventory. Renderer mean was 0.077 ms, p99 0.193 ms and maximum 53.77 ms;
MainThread mean was 2.91 ms and maximum 1304.8 ms; maximum draws/batches were 37.
This includes tool, idle and Editor activity. It cannot isolate latency causes
or imply Deck performance. The shared atlas and cached Tiles remain bounded;
no new per-frame allocations, world-generation calls or state changes are
introduced.

Self-review:

- 🟡 Grassy roads and dune stripes were fixed using actual views.
- 🟡 The false electricity-test premise was fixed in saved source with explicit
  setup assertions.
- 🔵 Current receiving-map, graph, lair, owner and state gates remain intact.
- 🧪 Three existing allocation probes remain inconclusive.
- ⚪ Products, props, actors and all other regions remain open.

This is CoO-original material art, with no Qud-parity claim. Current packaged
Sodden builds are preserved while fresh Beating packaging is prepared after
the source push.

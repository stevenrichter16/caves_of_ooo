# Marrowstye masonry shadow pilot

Status: bounded Editor implementation and actual comparison reviewed; whole-area acceptance remains open. V3 restrained shadows,
with material/composition review kept separate. Stillleaf pilot source f587fd3b3 is
pushed and remotely verified; current packages0a2cc9bae exclude that newer slice.

Choice: current actual Marrowstye12.12 masonry runs meet quiet dry ground without
short cast shade. Sill and ordinary Beating already have bounded shade; Summit's
historical empty center has received later population edits and needs a fresh audit.
Marrowstye is a useful non-cave expansion using existing approved art/layers.

Earned baseline: kitchen224401440 fixture, seed224401440, normal C/Continue,
world map north/up command, one south step, ordinary entry12.12 at39,11HP40.
F5turn3670 retained in Verification/GraphicsExecution/Marrowstye/before-save.
No teleport/FOV/owner mutation. Normal1280x800 target; default1× native game scale.

Implemented: exact current Marrowstye graph/site only. Selected native canonical
SandstoneWall casts the existing two-native-pixel south offset at16% black onto
fully disclosed empty dry approved settlement ground. Reuse existing shade layer,
reversible option, material, dirty-neighbor invalidation and disposal. No new
bitmaps, light simulation, colliders, TileState, save data or population/layout edits.

Acceptance: native RED/GREEN and source/receiver authority/state/resource counters;
actual matched comparison, movement onto/off receiver, wall/ground readers, route,
save/reload and independent visual review. No shade on hidden/memory owners,
occupants, doors, hazards or wet terrain; legacy/pause/travel clear the owned layer.
Compare whole visible scene at normal zoom; broader whole-chunk/area acceptance
requires more than this viewport. Haul aisles and DisusedWing are purposeful spaces;
no population addition is justified by their emptiness alone.

Performance: no hot-path allocations/parsing, no whole-zone scans or resource loads; one neighboring receiver cell is inspected per candidate wall; existing
submitted view/dirty hooks. Profile baseline/current60–90s same earned checkpoint
and native route: Right×4,Left×4,C/period/Return/Return,Up,Down,Up; retain maxima,
GC and tooling/environment limits. Main-thread or renderer spikes remain explicit.

Native progress:21genuine missing-shadow RED failures preceded implementation.
First adjacent run114pass/1fail of115; failure was test-only nonexistent WoodenDoor
blueprint, corrected to real VillageDoor. Receiver-only state cases now also assert
unchanged wall tile; saved surface-lair counter added. Existing Sill/Beating and
settlement groups passed93cases and need not be repeated without source changes.
Independent source and actual comparison review:0P1/P2 within the bounded pilot.

Actual first comparison: current POI Marrowstye/Intake confirmed at39,11;
matched before/after world crop changed1056pixels, all color channels nonincreasing,
confined to thin rows beneath wall runs. No presentation settings changed.
23focused plus93unchanged adjacent native cases pass (116distinct).

Actual input correction: moving into masonry performs the existing break/strike
action (not a passive collision-only probe). Both profile routes included the same
Up approach. A later C/Up/Return reader attempt chose the default break action;
retained wall-reader.png is that failed reader attempt, not a reader acceptance.
Correct Examine is C/Up/X. These are earned test-session state changes only.

## Verified result and remaining limits

Actual movement withdrew shade beneath the player and restored it after leaving:
source39,10 absent at39,11; Right to40,11 restores39,10 and suppresses40,10.
Correct wall Examine preserves the sandstone portrait and truthful25/30Structure.
F5turn3800 and freshContinue40,11HP40 match all639,200 compared world pixels
(0,0,940,680). Independent final reader/reload review also confirmed exact equality.
Actual screenshots, save fixtures, raw test XML and hashes are in
`Verification/GraphicsExecution/Marrowstye/play-receipt.json`.
The initial failed fixture/reader attempts remain evidence, not acceptance.
Raw native XML whitespace is preserved intentionally.

| Editor profile | baseline81.19s | current64.45s |
|---|---:|---:|
| Environment renderer p95 ms | .225 | .210833 |
| Environment renderer max ms | 45.964 | 56.536625 |
| Main thread p95 ms | 14.168 | 14.439 |
| Main thread max ms | 2342.261 | 142.391 |
| GC p95 bytes | 470117 | 18169 |
| GC max bytes | 275852685 | 9895308 |
| Batches usual/max | 39/43 | 41/45 |

Raw profiles: `Verification/GraphicsExecution/Profiles/manual-20261101-215624-802.json`
and `manual-20261101-220405-490.json`. Baseline Editor burst remains unexplained;
current renderer maximum is higher and adds two batches. Typical timings alone
are not a performance pass. R11 remains open; no packaged/Linux/Deck claim.

Coverage stays fallback: real Oven47,21 is still `#` in world and Examine.
Next earned fixture: nativeRight×7,Down×9, F5turn3960 at47,20HP40; actual oven
menu/reader/before are in `WorkshopBaseline`. Reader describes a baking chamber;
its observed action menu offers Examine only. Do not invent a cooking service.
Sparse yards and whole-chunk composition, other shadow receivers, doors/hazards
and full-area travel remain unaccepted. No owners, routes or population changed.

Self-review: no actionable P1/P2 in source or matched actual review. Approved
art bytes are unchanged. No new atlas, cache, runtime scan of the entire zone,
mechanic or save schema. Post-play isolation cleared, scene clean and all75
foreign tracked hashes unchanged. Current packages0a2cc9bae exclude this pilot
and Stillleaf; batch delivery after further accepted material work.

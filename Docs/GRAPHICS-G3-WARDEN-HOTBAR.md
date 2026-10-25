# Warden hotbar: distinguish stance, reach and advance

Status: three native icons integrated, native regression and bounded ordinary keyboard/visual acceptance complete. Separate pre-existing mouse-backend discrepancy is the next corrective slice.

The actual cellar captures show En Garde, Lunge and Follow Through as identical
generic diamonds. This loses useful action recognition. The bounded task is to
give these three exact abilities distinct pictograms with unchanged game rules,
name/status bands, controller bindings, selection and click hitboxes.

Design/implementation prompt: compare the real HUD, reinterpret only its three
icons through imagegen, extract mechanically accurate silhouettes, hand-author
editable native pixels, run native RED for exact mapping/legacy preservation,
integrate in the existing cached atlas, verify native tests and actual keyboard
selection/cast/cooldown. Capture real1280x800 output and review before main push.

## Verification sweep

| Premise | Source finding / consequence |
| --- | --- |
| Every ability currently has an icon | HudHotbarPresentation.IconFor maps only six starter spells. Warden classes fall back. |
| HUD art must be16x24 | World cells are16x24. Existing HUD icons are16x16 at16PPU, drawn uniformly1.75x in disjoint bands. Keep that established UI contract; a16x24 replacement would overlap text. |
| Lunge is a movement dash | It is a stationary reach attack. Use a thrust silhouette, no running or landing marker. |
| Follow Through is repeated attacks | It advances after an eligible killing strike. Use blade plus small step, no invented spin/multihit promise. |
| Icon change needs new update loop | HotbarStateBuilder already compares icon in snapshots. Existing redraw/cache/lifetime is adequate. |

Keep the seven original atlas strings byte-identical. Append exact enum values
rather than renumbering them. Unknown/null/wrong-case/lookalike sources retain
fallback. Names never decide identity. Do not use generated screenshot pixels as
runtime art. Keep this source-code-native icon set editable as readable rows,
export an audit sheet if useful; no new dependency or downloaded model.

## Current pipeline

Greybox/baseline: actual cellar-floor-64-entry.png at1280x800, with normal Warden
and three live abilities. Concept edits only three icon shapes. Selected world
reference negative-space discipline applies; no external artwork is imported.
Acceptance still requires real redraw/input/cast/selection screenshots, distinct
silhouettes at displayed size, stable original six spells/fallback, exact mapping
and no gameplay or saved skill migration.

## Native source review

Built-in imagegen reinterpreted the actual captured HUD; concept and exact prompt
are preserved under`ArtSource/Live16x24/warden-hotbar`. Selected silhouettes:
upright blade/open brace, horizontal thrust, diagonal blade/boot. Generated full
frame rescaling and bright circular arcs were rejected. Editable16x16 rows use
only the established atlas palette. Independent source review accepted guard and
thrust and found the step too symmetric; its sole/shadow now projects right by
one pixel to read as a boot rather than pedestal. No runtime acceptance yet.

Native export:`python3 Tools/Live16x24/export_warden_hotbar.py`. It reads authored
cell rows, never generated image pixels. PNG/contact and C#rows remain outside
Assets; runtime mask integration must follow genuine native RED.

## Native RED,11:48UTC

Factory-backed Warden fixture ran18cases in real Unity:11failed because live
snapshot/submitted icons still used Fallback;7counter/preservation controls passed.
Only after this result were three exact source mappings and enum ordinals7/8/9
appended, with three native row masks in the existing cached atlas. The old seven
masks and palette are byte-identical; no renderer/layout/input/ability code changed.
Native green and bounded ordinary acceptance are recorded below.


## GREEN and ordinary acceptance,2026-10-25 11:59UTC

105distinct native Unity EditMode tests pass,0fail/skip:18new factory-backed
Warden cases plus87hotbar/layout/controller-hint/starting-build regressions.
The fixture verifies actual submitted tiles, exact identities, original7RGBA
hashes, fallback/name/case countercases,16×16 band geometry, cooldown/targeting,
cache reset and bounded redraw. Independent source/code review found no remaining
icon correctness defect. Native source SHA256:
`6f60f07896cc14046e66fe93b083069ee93e76deb8623ffab4e5a0544c086c2f`.

Normal SampleScene/seed64 New Game→Warden, no presentation override or granted
skills: Return activates En Garde; real CD11/state text and guard pictogram remain
visible.2opens Lunge aiming; Escape cancels;3opens Follow Through aiming; Escape
cancels;left bracket selects Lunge;F5 saves. Ready tick10→cast tick20; subsequent
selection/cancellation stays tick20/HP40 at40,12. Captures and receipts use prefix
`Docs/Verification/Live16x24/warden-hotbar-`. Independent screenshot review passes
ready, cooldown and both targeting states, including no text/card overlap.
Earned checkpoint preserved at`/tmp/coo-warden-hotbar-earned`; normal save hashes,
backup hashes and current Linux archive remain unchanged. Editor stopped cleanly,
audit view/preferences/save isolation restored; actual console has0errors/2warnings.

## Self-review and next issue

🔵 Corrected before runtime: source boot initially resembled a symmetric pedestal;
sole/shadow now projects right one pixel. This is native authored pixel art, not
resampled generated art. Old seven masks/palette remain byte-identical.

🧪 Mouse acceptance is NOT passed: two actual clicks on Lunge at displayed
1063,1083/1110 did not change selected Follow Through. A subsequent editor readback
reported legacy pointer1790,270 and Screen2321×1139 despite actual1280×800 output.
Read-only audit found hotbar still uses legacy Input directly, unlike the recently
fixed inventory. This predates the two icon-only source changes; do not alter art
geometry to compensate. Next separate slice: native RED for actual pointer input,
reuse the proven authoritative-native/fallback-legacy contract, then ordinary clicks.
Current raw receipt is`warden-hotbar-mouse-observation.json`; it is not a click-frame
measurement. Separate codepath diagnosis is not yet proof of current nativebuttonedge.

🧪 Bounds: these captures do not execute Lunge hits or Follow Through's kill-step,
accept physical Deck comfort/performance, update packages, or close all skill
icon/world/regional coverage. Controller hint tests pass; physical gamepad not run.
Native caches remain bounded and retained; no frame-time improvement claimed.

## Files and reference classification

CoO-original HUD work, using established pixel/negative-space direction; no Qud
art/code copied. HUD16×16 is its existing UI contract, world remains16×24.
Implementation appends three enum/mapping entries in`HudHotbarPresentation.cs`
and three masks in`HudSkillIconAtlas.cs`. New`WardenHotbarIconTests.cs/.meta`,
editable`ArtSource/Live16x24/warden-hotbar`, and
`Tools/Live16x24/export_warden_hotbar.py` preserve the authoring/review path.
This doc, completion/live direction records and release ledger accompany code.

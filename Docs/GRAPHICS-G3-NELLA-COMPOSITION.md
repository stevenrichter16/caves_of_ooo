# Next Nella composition pass: a worked field edge

2026-10-25 UTC. **Candidate2 implemented and accepted for the bounded seed64/65 field/frontage pass.** Candidate1 passed mechanics tests but failed actual visual review and is retained as rejected evidence. The accepted revision keeps substantial remote fields and completes the principal furrows around the real working group. This is one scene-composition improvement; complete opening art, latest packages and physical Steam Deck acceptance remain open.

## Observed problem and preferred outcome

Actual `nella-ground-arrival.png`, `nella-ground-empty-dry.png`, `nella-ground-rain-replant.png` and `nella-ground-dried-after-wait.png` show a small useful working group beside several similarly weighted rectangular remnants of brown field. Repeated two-column strips and clipped ends dominate the map more than the relationship between Nella, her public crops, the reserve beds and the approach. The broad central green space is useful breathing room; filling it with miscellaneous furniture would not fix that hierarchy.

**Place the complete existing Nella working group on one coherent field frontage, with one dominant field remnant behind it and shorter, unequal harvested remnants farther away.** Use the existing keeper, crops, signs, tray, row owners and grass. This first greybox changes both the cold worksite placement preference and the surrounding row mask; scalar thinning alone is not enough. It adds no new farm service, road, crop type or sprite packet.

Visible payoff must be apparent at the ordinary 1280×800 whole-zone scale: one principal cultivated-looking field remnant associated with the keeper, a clear unpaved working headland, and fewer equally prominent repeated brown blocks. The proposed change should not need a magnified crop tile or a before/after annotation to be noticeable.

## What actually owns this scene

| Source | Verified behavior / consequence |
| --- | --- |
| `Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs:105–148` | Non-Legacy exploration uses three bounded parcel/approach arrangements. OffsetLanes has four shallow rectangles; BrokenEnclosures five; BankCrossing three. Existing routes join all four border ports. Nella is **FieldStrips**, not the northern Fallow/overgrown-croft hedge formation. |
| Same file, `ObjectAt`, around229 | Field strips are planned `CropRow` cells inside parcel boundaries: two columns out of every three, plus the existing seed/condition roll. Approach/lawn/water vetoes apply first. This produces the repeated brown fragments visible in actual play. |
| `Builders/SpreadCompositionBuilder.cs:60–108` | Cold empty-zone builder creates the real owners. `SelectGleanings` replaces a ranked subset of planned rows with `RipeCropRow`:3 in tended,2 in returning scrub,1 after harvest. Those are finite real harvests. Planned layout changes may move them; they must not reduce the budget or create a second source. |
| `Builders/SpreadExplorationResidents.cs:29–84,322+` | Nella's actual connected stamp is placed separately with rollback/authority, real bare-ground and access checks. It chooses a seed-relative anchor at22,12 or58,12, then the nearest fitting origin and rotation. Do not hardcode the observed19,9 origin. |
| Same resident specs | Keeper0,0; public gladroot−2/−1,−1; public emberwheat−2/−1,+1; sign+2,0. Reserve marlroot/pitchpod−2/−1,−3; tray+2,−2; reserve sign+1,−3. Only the two reserve grounds become cultivated. Existing free planting gaps and two-sided service access are meaningful. |
| `Objects.json`, CropRow/RipeCropRow | Cut stubble is an actual terrain owner; ripe rows add real `FieldHarvestPart`. Stubble does not itself make an area a prepared bed or grant permission. Nella's paid/introduction rights, crop stages, finite yield and neutral cords remain separate truthful systems. |

The exact admitted address is `Overworld.11.8.0`; scope must also require the existing non-Legacy FieldStrips plan. Old saves retain their existing owner graphs. Do not rewrite this current earned Nella checkpoint to make the comparison easier.

## Original candidate1 greybox — superseded by the revision below

Implement a **bounded cold frontage plus row mask** in the existing plan/resident path. The land-use portion is pure subtraction of planned CropRow cells after original approach/pocket decisions. The resident portion changes the preferred position of the complete newly generated stamp. Do not move any loaded entity, add brown cells or reinterpret an unrelated owner. These are two cooperating parts of one scene change, not separate improvement claims.

1. Derive the same left/right resident-anchor preference from the existing seed rank. Among the upper parcels, select the parcel nearest that anchor as the primary field. Work in that parcel's local coordinates, not absolute seed64 NPC coordinates.
2. Derive a preferred frontage from that primary parcel: its lower edge at `p.Y + p.Height`, beside its existing vertical approach at `p.X + p.Width/2`. Try origins approximately four cells to either side of that approach, with the existing left/right seed preference choosing the first side. These are candidate anchors, not unconditional placements. Rank ordinary resident candidates by distance to the chosen frontage; retain the existing rotation order, `Fits`, prepared-ground validation, arrival exclusion, maximum trials, rollback and safe fallback. If exact trial geometry conflicts, choose the nearest valid frontage candidate through those same checks. Keep all current local service offsets. With rotation0 the two reserve beds sit three rows behind the keeper, the tray two rows behind on the right, the public crop pair on either side of the keeper's row, and the ordinary sign beside the open approach. No service is duplicated or newly granted. This is the concrete improvement to the loose working group: it belongs to an existing field edge and can be approached/read from the actual lane.
3. Retain the primary field's longest full-depth run groups, with no blanket shortening of its approach-facing ends. Derive a small grassy service bay from the same planned complete-stamp descriptor. Subtract only row cells needed for that clear working edge; keep at least two neighboring original furrow groups at their available depth. This should read as one substantial field associated with the keeper, not scattered individual stalks or a new diagonal ribbon.
4. Select one distant parcel as a secondary remnant. Retain roughly half of its existing furrow-group span at the outer side, with a different stepped end and shorter depth. Other parcels retain only short, unequal remnants, or lose a redundant end group. Do not apply the same L-shaped mask to every rectangle.
5. Keep the existing open lane between the primary field and the working group. The headland remains ordinary grass: no painted road, cultivated flag, fence, wall, storage prop or implied new interaction. Existing route and lawn vetoes win over every mask cell.
6. Use retention-first hierarchy: preserve substantial full-depth runs in the primary field and take most reduction from distant remnants. There is no55–70% total retention target; do not make the already sparse scene emptier to satisfy a ratio. Reject a concept that mainly makes an already sparse map emptier or leaves four equally weighted mini-fields.

The exact seed64 keeper origin is currently19,9, with reserve beds17/18,6 and public rows17/18,8 and17/18,10. These are reference observations, not placement constraints for every seed. Newly generated Nella and all her existing stamp owners may move together as this pass intends; their relative offsets and functionality remain unchanged. The new frontage must use a plan-owned pure descriptor shared by the exact scoped resident placement, not a renderer-derived location or a search for already created actors. Outside11,8/non-Legacy FieldStrips, the original anchor/candidate ordering stays identical.

A valid fallback is preferable to silently breaking site admission, but it does not automatically earn visual acceptance. If either ordinary seed puts the working group away from the retained field frontage, revise the cold candidate preference/mask instead of moving a live actor or adding props. The meaningful before/after is **loose services among four equal fragments → one integrated working edge, clear approach and subordinate remnants**.

## Preserve explicitly

- Accepted Keeper/player/crops/signs/tray/cord/soil pixels and native scales. No atlas work is required for this first composition pass.
- All `GroundAt`, water, road and approach cells, non-CropRow planned terrain objects, exterior border ports, sight/collision rules and original generation RNG ordering. New resident coordinates may change only through their scoped authoritative placement preference. A new mask uses stable plan data; no caller RNG draws.
- Exact1/2/3 gleaning budget, real harvesting behavior, Nella's finite stock, reserve permission/payment/introduction, planting gaps, crop moisture/growth and save identity.
- Existing kitchen/northern/flower masks, other field addresses, Legacy plans, POI/admission refusal and the empty-zone build guard.
- The broad quiet walking area. Do not duplicate an oven, forge, well, tray service, cottage or domestic court to give this plot visual weight.

Do not promise identical generated IDs or service coordinates across a new layout: fewer cold-created rows can change later IDs/candidates. Preserve their semantics and actual generated authority; loaded existing graphs remain unchanged.

## Route and image pipeline

Use the real pre-Nella checkpoint, not the latest already-generated plot:

`/tmp/coo-fresh-opening-after-north/20cb263ebed24a8380168954e23981ca/`

`Quick.sav.gz` SHA256 `07ad1aa59160201edad2d08018cac467b04c6fdf25eb424c7c390a75dfdfe885`; recorded fresh-origin seed64, tick2260,32HP, north works23,17, real coating6974 and cellar supplies. Confirm11,8 is still uncached before replay. Normal Continue and northward walking from11,9 enter Nella from the south; follow actual visible ground and threats rather than teleporting to the historical coordinates.

1. Capture ordinary entry and close working-group view before changes from that checkpoint.
2. Root imagegen uses the actual whole scene as composition input. Ask for unequal harvested remnants and one useful field edge while preserving16×24 grid, all existing routes/owners, approved sprites and UI. Reject invented structures, extra crops, roads, soft shading and decorative noise. Generated output is concept only.
3. Extract only parcel/run masks and the cold frontage descriptor/placement preference into editable deterministic source, then native test-first implementation. No generated bitmap is needed in the game for this slice.
4. Re-enter through the same cold checkpoint normally. Capture matching entry, keeper/public-row/reserve arrangement and the actual field harvest action. Pay8drams or use the real introduction; use the existing finite harvest→pickup→replant→Rain sequence when supplies allow. Verify leave/save/return through actual controls, not injected state.
5. Obtain one seed65 ordinary visual counter using a legitimate fresh start or verified pre-Nella checkpoint. Source tests alone do not establish that the second placement looks coherent.

## Acceptance gates and counter-checks

Candidate2 replaces the subtraction-only assertion with exact primary-furrow
hole additions; all nonrow/ground/routes and out-of-scope pins remain exact.
See actual visual rejection/revision below. Other gates continue to apply.

- Native RED first: exact scoped field layout has an intentionally stronger primary mass / unequal remainder; source comparison proves the proposed composition requirement fails before implementation. Keep this distinct from gameplay-bug claims.
- Compare full plans across seeds64,65,1729 and each of the three non-Legacy topology overrides, plus seeds covering all three conditions. Candidate2 permits scoped `CropRow→null` changes and `null→CropRow` only in eligible original primary-furrow holes outside the service bay. All ground/approach/water/nonrow cells must match the baseline. Other addresses/formations and Legacy plans must remain byte-for-byte equivalent in plan output.
- Build actual owner graphs: exactly1/2/3 ripe rows by condition, plentiful cut rows remain, real harvest yields once, no duplicated source. Keep existing SpreadComposition/Adversarial gleaning and nonempty-zone guards.
- Run existing resident/connected-manifest/claim tests, plus a real factory fixture proving the Nella stamp uses the planned frontage when it safely fits and still commits with two reserve grounds, four public crop owners, tray and signs, plantable gaps, current exact permissions and required approaches. Exercise both seed sides and any accepted rotation, a blocked preferred frontage with truthful safe fallback, and out-of-scope original ordering; failed placement must still roll back cleanly. Verify all relative stamp coordinates and roles together, so moving only the keeper cannot pass.
- Actual1× whole-scene review must find the whole service group visibly integrated with a clear principal field and readable working approach in seed64 and the second seed, without hiding useful crop stages or reducing all fields to isolated noise. If not materially better, do not accept it merely because numerical masks/tests pass.
- Save/return must retain actual rights, finite sources and planted crop state; no live row regeneration. No physical Deck, new package, whole-world art or performance claim follows from this pass.

## Latest material evidence incorporated

`nella-ground-dried-after-wait` at tick3160 shows real wet20→dry19 exhaustion: both reserve crops retain stage1 with zero moisture, and the surrounding old-field repetition remains obvious. `nella-ground-save-return.json` at tick3510 retains the exact cultivated grounds7515/7516, current crop9186/9193, Granted rights,42drams and32HP. Its actual world-pixel crop matches the dried-after-wait scene, and root independently accepted the returned screenshot. Current PNG SHA256 is `0978772cfdfc825f2d8db466238a3842c09fc656aabb0bc2bf844a6ef7363521`.

An initial image-preview discrepancy showed this reviewer an empty-looking return view. Read-only pixel checks found225world colors and36,763 old-earth pixels matching the coherent dry view; the apparent blank preview is retracted and is **not** a runtime defect or added gate. The evidence supports the accepted small material/state improvement while leaving the named broader composition opportunity intact.


## Concept and cold review — 2026-10-25

Built-in imagegen used the actual `nella-ground-arrival.png` capture, with
external observational referenceR01 (quiet ground/readable clusters) from the
reference library. Output `ArtSource/Live16x24/concepts/nella-composition-v1.png`
is a concept, not gameplay. Exact prompt is saved beside it. Root inspected
the result: accept only a stronger primary field mass next to the worksite
and unequal remote remnants. Reject changed UI/text, green gradients,
over-bright/overgrown stubble and any implication of additional ripe crops.
Accepted sprites will remain unchanged; this slice produces deterministic
editable owner-layout rules, not a generated background texture.

Independent cold review corrected a planning premise: seed64 Nella19,9 is
already near the lower edge. The provisional parcel5,3,23,6 / approachx16
claim was an OffsetLanes assumption and is corrected below by actual cold
replay: seed64 uses BrokenEnclosures parcel5,3,18,7 / approachx14. A one-cell
anchor move alone would not provide a meaningful improvement. Remove the blanket55–70% thinning
target. Preserve the principal field's full-depth bands, open a small coherent
grassy service bay around the complete stamp, and reduce repetition mainly
in remote remnants. Do not move the worksite into the reserved y11–13lane.

At the concept stage no generation code or native layout tests had changed.
The next step was to verify this
retention-first mask against the exact actual plans and cold pre-Nella save,
write native RED, then integrate and compare ordinary seed64/65 captures. If
subtraction cannot produce a meaningful improvement, revise the plan openly
before adding or relocating actual row owners. Keep the finite gleaning budget
and all public access/ownership rules intact.


## Verification sweep and baseline replay — 2026-10-25 09:16 UTC

| Earlier assumption | Verified correction before implementation |
| --- | --- |
| The lower edge is always a safe stamp origin. | Broken/Bank upper parcels end at10/11, next to the reserved main lane. The pure descriptor must inspect the complete ten-owner footprint and planting gaps against existing plan exclusions before choosing its bay; actual `Fits` still decides admission. |
| Seeds64/65/1729 cover both anchor sides. | All three choose the left side. Native tests additionally need62 or66 for right parity. |
| Clearing cut rows repairs admission. | `CropRow` already qualifies as bare ground. This is a composition/readability change, not a repaired placement bug. The bay must remain ordinary public grass. |

Root launched the ordinary saved SampleScene under native save isolation, with
no presentation override, then pressed Continue on the earned after-North save.
Read-only inspection confirmed only11,10surface/cellar and11,9 were cached;
11,8 was genuinely cold. Actual arrow-key travel entered the plot from the south
at17,24 and approached18,9. Before-entry/frontage PNG/JSON captures preserve the
old repeated field layout and all existing services. Player32HP, baseline
frontage tick2750. A held northward input struck a blocking shelter tree in11,9;
root then used the visible opening. This is ordinary input evidence, not an
idealized scripted route. Original checkpoint remains unchanged. Play and
isolation ended, scene/prefs/view restored09:16:38UTC.

The previous imagegen concept remains the visual interpretation; no second
concept or sprite packet is required before testing this small scene change.
Next: source-backed native RED, pure mask/frontage implementation and existing
mechanic counters, then compare cold64/65 ordinary Play. No layout acceptance yet.

### Native test-first result

The first discovery attempt found **zero cases** because the new test asset had
not been imported by a scripts-only refresh. It is retained as
`nella-composition-empty-discovery.xml` and earns no RED/GREEN credit. After a
full refresh, root verified the fixture type existed in EditModeTests. Actual
native RED at09:26:29–34UTC ran25:12passed,13failed,0skipped. Four direct
composition assertions found zero shortened remote remnants; nine descriptor/
composition tests failed on the intentionally absent shared descriptor. All
other baseline, budget and authority controls passed. Production changes were
authorized only after that result. The original30native plan snapshots are
preserved in `nella-composition-original-plans.json.gz`; the fixture embeds
compact row masks and whole-plan hashes rather than requiring a temp file.

### Actual topology correction before GREEN

The cold baseline143field cells match the seed64 **BrokenEnclosures** plan
exactly (zero differing coordinates), not the provisional OffsetLanes plan
(122differing coordinates). Its primary parcel is `(5,3,18,7)` with approach
center14 and bottom10. Existing Nella19,9 is one above that bottom. The shared
descriptor derives actual parcel geometry; it does not encode the discarded
OffsetLanes premise. Baseline/source comparison was reported to the implementer
before the first production candidate was frozen.

### First production candidate

After observed RED, the pure immutable descriptor selects the actual upper
parcel working edge, checks the complete ten-owner/two-gap footprint against
original repaired-plan exclusions, and preserves two adjacent original furrow
groups. Its25-cell bay subtracts only CropRows; remote bands are shorter and
unequal. The resident builder only changes its preferred anchor for this exact
connected plot. Native Fits, both original rotations, authority, rollback and
loaded graphs remain unchanged. Native focused GREEN09:36:32–44UTC:
25passed,0failed,0skipped. Independent source review found no actionable P1/P2.
Visual acceptance remains pending; numeric geometry is not a substitute.

### Actual visual rejection and scoped design revision — 09:44 UTC

**Candidate1 is rejected.** The native26-case focused fixture (including the
added right-side factory counter) and131-case related regression pass, but
ordinary seed64 cold entry/frontage captures show143rows reduced to59. The
primary field remains visually broken while the distant fields become crumbs;
the scene is mainly emptier. This does not meet the stated composition gate.
No visual acceptance or release completion is inferred from tests.

For candidate2, explicitly relax the original subtraction-only limit **inside
the primary parcel's existing furrow grid**: fill only empty eligible cut-row
holes outside the bay, reserved approaches/water/lawn/nonrow objects. These
are ordinary cut-row candidates. Existing SelectGleanings still ranks the
resulting rows, so one filled hole can be selected within the unchanged finite
ripe budget; no extra yield is introduced. Retain the original ground, paths,
ports, RNG and exact1/2/3 ripe-food budget. Distant fields need substantial
three/four-cell depths and unequal but coherent widths instead of tiny scraps.
The complete existing service stamp remains bound to the shared safe frontage.
No decoration, new crops, added source yield or accepted sprite replacement.
Write source-observable continuity/depth RED against candidate1 before this
revision. Repeat actual cold seed64 review before proceeding to seed65.

Evidence: `nella-composition-64-after-entry/frontage.png` and paired readouts
are the **rejected first candidate**, never the accepted release image. Player
32HP; normal Continue from the same immutable pre-Nella checkpoint confirmed
11,8 uncached. Ordinary movement reached17,24 then17,10. Baseline travel took
extra turns at a tree, so ticks differ; comparison is geometry, not exact time.

Independent visual review confirms candidate1 primary33→27rows and remote
26/11/33/40→7/3/10/12. Candidate2 is the smallest justified correction: one
complete original furrow group clipped from the4-row secondary, one/two groups
from3-row remnants, original remote condition rolls retained.

Candidate2 native RED09:46:24–32UTC:26total,19passed,7failed,0skipped.
Failures are four remote-depth controls and three primary-hole continuity
controls. Production remained candidate1 until that saved result. Exact
candidate1 source diff is retained as `nella-composition-rejected-candidate1.patch`.

Candidate2 first native attempt09:48:33–40UTC ran26:25passed,1failed.
The remaining seed1 remote-width counter is under review against original
condition holes and the stated complete-furrow clipping rule. This attempted
GREEN is not a passing gate.

#### Candidate2 verification correction

The width failure was a test premise, not missing depth: seed1 BrokenEnclosures
parcel3 has11occupied columns over its whole original height but only8inside
the retained3rows. Its4remaining columns obey the intended bounded clipping;
the counter now compares the original columns in the same retained depth.
Separately root found a real paired-furrow defect: on an18-wide left remnant,
the pixel cutoff kept relative column13 but cut14. Seed2 BrokenEnclosures has
both original rows at18,4 and19,4, so a new explicit paired-cell counter must
fail before replacing pixel cutoffs with complete original furrow-group IDs.
Ground/routes/food/authority controls remain unchanged.

Paired-furrow native RED09:51:25–33UTC:27total,26passed,1failed at the
missing second column19,4. The subsequent minimal fix clips integer furrow
groups, ignoring any trailing gap column. Tests were frozen before production.


## Final native verification and actual Play acceptance — 2026-10-25 10:21 UTC

Final native Unity run `nella-composition-native-final.xml` ran **158 cases:
158 passed, 0 failed, 0 skipped**, 09:53:36–09:54:42 UTC. This is the new
27-case fixture plus 131 related composition, admission, worksite, claim,
rollback and persistence cases. It supersedes the candidate2 attempted GREEN
and the rejected first candidate. Source remained frozen for both final Play
runs; no sprite, atlas, blueprint or renderer was changed.

| Actual run | What the player did | Bounded result |
| --- | --- | --- |
| Seed64 cold Continue | Reused the immutable pre-Nella checkpoint, confirmed11,8 uncached, walked north, talked and paid8drams, gathered Pitchpod into inventory, replanted its seed, cast Rain, fought a scrabbler, gleaned one ripe row, saved/reloaded outside the zone and walked back. | Final105 field rows retain exact IDs/positions after travel. Primary38 rows versus old33; remote13/9/17/28 versus old26/11/33/40. Rights,2resin,1sheaf and replacement crop9153 survive. Growth reaches stage1 then pauses dry. Actual returned frontage is accepted. |
| Seed65 fresh New Game | Ordinary N→Breaker→Enter; walked north through11,9, confirmed11,8 uncached, entered, approached Nella and paid8drams. No injected movement, inventory, HP or presentation override. | All170 rows and ten site owners retain identities through the visit; Nella at22,9, player23,9,40HP. Rights Unknown→Granted,50→42drams, unchanged inventory/equipment. Primary63 rows and remote53/54 form a coherent frontage, with subtler hierarchy than64. |

Independent source review found no new actionable P1/P2 findings; see
[final source/authority review](Verification/Live16x24/nella-composition-final-source-review.md).
Only an explanatory comment was clarified after the frozen final run; runtime
behavior and tests are unchanged.

Independent Play reports:
[seed64 lifecycle and visual review](Verification/Live16x24/nella-composition-64-play-review.md),
[seed65 visual/access counter](Verification/Live16x24/nella-composition-65-play-review.md).
The initial field-food budget remains1/2/3 by condition; the observed seed65
has two unharvested ripe owners. Only seed64 exercised an actual harvest/replant/
growth/save-return sequence. The native fixture separately checks finite yield
and second-harvest rejection. Do not merge these different evidence bounds.

### Capture provenance and player actions

Every `*-final-*.png` is an actual native1280×800 Unity Game capture paired
with a read-only owner/state receipt. No generated image is used as gameplay
evidence. Root used ordinary keyboard input; CUA's very rapid key dispatch
sometimes required repeating a direction after C, and fast search text lost
letters. The repeated inputs are recorded, not represented as first-try success.

Seed64 final entry17,24 is geometrically comparable to baseline entry17,24;
the earlier baseline took extra turns against a tree, so simulation clocks
differ. Candidate1's `*-after-entry/frontage` captures are rejected, and
`*-final-paid-frontage` includes an accidental Pause menu after payment.
Use `nella-composition-64-final-accepted-frontage.png` or
`nella-composition-65-final-paid-frontage.png` for the accepted unobscured views.
The seed65 entry camera partly clips the left field; normal approach reveals it.
No pre-change seed65 Play image is asserted.

Seed64 ordinary combat reduced HP before a scrabbler kill and level2 raised
it to42. This is real intervening gameplay, not a restored/injected health
value. Public gladroot grew and disappeared after watering; produce is visible,
but filtered readouts do not establish its exact floor IDs/yields. Planted
Pitchpod9153 is a normal crop without the original stamp-role property, so it
appears in `objects`, not the original `residentOwners` list.

### Preservation and clean handoff

- Seed64 earned checkpoint: `/tmp/coo-fresh-opening-after-nella-composition/20cb263ebed24a8380168954e23981ca/Quick.sav.gz`, SHA256 `1513031329358a9ac409a9337740c1a5209971615e5ddc14cd5e2b4d0da035b8`.
- Seed65 earned checkpoint: `/tmp/coo-fresh65-after-nella-composition/d5180ca0d60a4a179b51b1879f4f454a/Quick.sav.gz`, SHA256 `5a86b7173939c993377801fd81d065758f020c4ca98ee158cf80ce6f7be64165`. This is a saved checkpoint, not a seed65 reload acceptance.
- Both Play sessions ended normally. The final cleanup reports no active isolation, no borrowed Game view, no pending art launch, null save override and clean SampleScene. Normal LastGameID and absent presentation preference are restored; four normal save/backup hashes and the immutable pre-Nella checkpoint remain unchanged.
- Final active-session console count was0errors/2warnings. The warnings were not recovered by the post-stop console query; they are not claimed as reviewed or absent. No exception/failed action was observed in the scoped lifecycle.
- Unity's23 whitespace-only sprite metadata rewrites and the related native suite's volatile-ID historical census were restored after byte/semantic comparison. The generated census is preserved under `/tmp/coo-nella-native-regenerated-census.tsv`. Two unrelated notice images remain untouched and unstaged.

### Final self-review and honesty bounds

- 🟡 **Fixed:** candidate1 passed tests but made the scene too empty. Actual screenshot comparison rejected it before acceptance; candidate2 fills only existing primary-furrow holes and retains substantial remote fields.
- 🟡 **Fixed:** clipping by pixel width split an existing furrow pair. Explicit failing native pair counter preceded the integer-group correction.
- 🔵 **Corrected premises:** actual seed64 topology is BrokenEnclosures, not the provisional OffsetLanes; the remote-width counter now measures original columns within the retained depth. Both are documented rather than silently weakened.
- 🔵 **Scope divergence:** the initial subtraction-only plan was insufficient. Eligible primary holes now gain ordinary row candidates, and the unchanged finite gleaning selector may rank one as ripe. This adds no extra harvest budget and leaves ground, routes, other owners and loaded graphs alone.
- 🧪 **Open:** whole-opening scene quality, remaining regional kits, physical mouse/controller feel, current-package replay and physical Deck performance. No frame-time improvement is claimed; this pass adds no renderer or texture load, but that is not profiling evidence.
- ⚪ **Observation:** seed65's primary is only modestly larger than its remote fields. The accepted result is a clear working frontage and coherent unequal depths, not a dramatic full-scene transformation. Do not keep changing accepted pixels/layout without a named defect.

**Can verify:** native scope/authority/geometry/budget counters; real keyboard
approach and rights in two cold seeds; the sampled seed64 harvest, replacement
crop, moisture/growth, depletion and persistent return; actual native screenshots.
**Cannot verify from these records:** all seeds/owners/actions, final visual feel
on physical Deck, controller comfort, complete crop maturation, global art
completion, new packaged delivery or a performance gain.

Files changed: the pure `SpreadCompositionPlan` descriptor/mask, two-line
resident anchor preference, new `SpreadNellaCompositionTests.cs` and its meta,
this plan, graphics handoff/completion plan, release ledger and named verification
evidence. Existing concept and approved sprites remain unchanged.

**Next action:** freeze this bounded Nella pass. Review the fresh opening's
six whole scenes as one visual journey against the existing reference direction,
prioritize the largest visible inconsistency, and run the same actual-capture→
concept→owner/state→native implementation→ordinary Play loop. Continue the
regional work after the defined opening gates; do not declare the graphics done.


### Remote preservation and storage follow-through,10:28UTC

Commit `d313eac64` and all118 exact owned files were verified against origin/main.
Only then were four inactive clone Library database files removed after exact
path/type/identity and no-open-handle checks. Actual available-space gain:
158,928,896bytes (151.6MiB). See
[cleanup receipt](Verification/Live16x24/nella-composition-storage-cleanup.json).
Active main caches, all source/editable art, saves/backups, dependencies and
current builds were preserved. Follow-up inspection found no safe target over
500MiB; approximately340MiB remained. This is a storage receipt, not a new runtime
change, test result, package update or whole-opening acceptance.

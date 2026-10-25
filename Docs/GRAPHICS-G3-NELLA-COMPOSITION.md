# Next Nella composition pass: a worked field edge

2026-10-25 UTC. **Recommendation only; no production, Unity, asset or imagegen edits.** Complete the current fresh-origin east-forge/kitchen continuation before opening this separate scene-composition slice. Nella's accepted material work is useful, but is not a complete scene composition.

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

## Concrete preferred greybox

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

- Native RED first: exact scoped field layout has an intentionally stronger primary mass / unequal remainder; source comparison proves the proposed composition requirement fails before implementation. Keep this distinct from gameplay-bug claims.
- Compare full plans across seeds64,65,1729 and each of the three non-Legacy topology overrides, plus seeds covering all three conditions. Only scoped `CropRow→null` differences are allowed; all ground/approach/water/nonrow cells must match the baseline. Other addresses/formations and Legacy plans must remain byte-for-byte equivalent in plan output.
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
already on the lower edge of parcel5,3,23,6 and near its approachx16. A new
anchor20,9 alone is only a one-cell change. Remove the blanket55–70% thinning
target. Preserve the principal field's full-depth bands, open a small coherent
grassy service bay around the complete stamp, and reduce repetition mainly
in remote remnants. Do not move the worksite into the reserved y11–13lane.

No generation code or native layout tests have changed yet. Next: verify this
retention-first mask against the exact actual plans and cold pre-Nella save,
write native RED, then integrate and compare ordinary seed64/65 captures. If
subtraction cannot produce a meaningful improvement, revise the plan openly
before adding or relocating actual row owners. Keep the finite gleaning budget
and all public access/ownership rules intact.

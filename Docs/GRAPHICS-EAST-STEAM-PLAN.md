# East steam-mark audit — read-only, 2026-10-27

Status: native211/211 and actual Editor before/after/play accepted for this scoped steam cue. Fresh compressed Mac/Linux packages completed; actual Mac restart verified. Initial audit used read-only inspection. Root owns capture, imagegen, implementation and ordinary play. This plan follows CLAUDE and the live brief's capture → reinterpret → editable native source → integration → play/counter-check loop.

## Correct the named defect before authoring

The vent body is already native. `FlatPixelEastKitchenCatalog.cs:131–135` maps exact SteamVent to accepted atlas frame16, a neutral inset grille with no plume (`ArtSource/Live16x24/east-kitchen/README.md:27`). The existing `FlatPixelEastKitchenTests.NeutralFurnitureAndCookDoNotInventAvailabilityOrHeatState` deliberately keeps it native even at −50°C. Preserve that artwork, source and reader.

The small white marks in `packaged-cudgel-02-world-2x.jpg` are the separate tile-state cloud overlay. `ZoneRenderer.cs:1276–1378` chooses one prioritized tile-state mark; its final cloud branch paints a pale CP437 apostrophe. It is neither the vent's `^` glyph nor a SteamCloud entity. The saved state confirms this distinction, not just the image.

## Exact checkpoint and observed state

`Docs/Verification/Live16x24/Fixtures/packaged-cudgel-return64/Quick.sav.gz`

- SHA256: `69b214bd644c433d14d88d51840dfec01301a91a49d31963f6cc7f29b9d2b3cc`.
- Format8, seed64, GameID7149edb751014f1c8ef55c71dd7d5548; East `Overworld.12.10.0`, player2697 at26,6, tick3510, HP28/40.
- Nearby SteamVent IDs:9346 at29,7;9347 at30,7;9349 at31,6;9348 at31,7;9350 at32,6.
- Each is non-solid/non-takeable, Temperature36.1345177°C, source Coating=water, CoatingTurns=2, Heat=1, Cold=0, Charge=0, RadiatesEnergy=false. No SteamEffect, FrozenEffect or BurningEffect was present in these saved owner bodies. No SteamCloud entity occurs in the saved East graph.
- The exact version1 tile-state footer has five corresponding steam records: Cloud=steam, CloudTurns=1; empty coatings/residues; Heat=Cold=Charge=0. No other written tile states occur in x20..36,y2..12. The footer parser checked TileState.Begin/End and GameSession.End and consumed the whole footer; it did not load gameplay hooks.
- No blocking owner lies on26,6→27,6→28,6. The remaining saved East creatures are distant: grazer51,17, magpie53,5, farmer66,5. This is checkpoint evidence, not a guarantee about later movement.

Owner/position/Part reads reused the existing bounded read-only parser from `/tmp/coo-earned-temper-audit-final.py`; tile footer decoding follows `SessionTileStateSerializer.cs:22–42,190–205`. Only the stated fields were audited.

## Mechanical contracts and false premises

| Contract | Source | Consequence for art |
|---|---|---|
| SteamVent inherits Terrain; layer1, `^`, Material Steam, conductivity65, porosity1; Thermal95°C initially, vapor100°C, heat capacity1.8 | `Objects.json:32697–32798` | Authored initial temperature is not current temperature and is not an emission switch. |
| It leases water2 turns and Heat1 independently of Thermal; default RadiatesEnergy=false | `TileStateSourcePart.cs:42–76,126–153` | Cooling the object does **not** automatically stop tile steam. Do not invent hot/cold vent frames based on temperature. |
| Each player turn seeds sources, resolves reactions, then decays tile state | `ZoneTileStateSystem.cs:25–40` | Real steam reaction refreshes2 turns then leaves1 after that turn. No per-frame simulation or new ticker needed. |
| Water+Heat1 consumes water and1heat, creates steam2 turns, no occupant effect/damage | `Data/TileReactions/Reactions.json:70–90`; `TileReactionSystem.cs:333–385` | This cloud is not evidence of scalding, burning, boiling damage or a heat radius. |
| Cloud is one replaceable string/duration; WriteCloud and Tick dirty the cell; smoke and veil-mist alone block sight | `ZoneTileState.cs:105,339–350,425–482`; `ZoneTileStateSystem.cs:170–184` | Native mark follows live state and clears/reverts via existing incremental paint. Steam does not grant obscured sight. |
| Actual damaging steam is a separate owned SteamEffect with finite positive density and current source Thermal>100°C; typed Heat2 per physical contact pulse | `SteamContact.cs:22–47,50–59`; `SteamEffect.cs:84–123` | Never infer that hazard from a SteamVent blueprint, cloud mark or detached SteamCloud visual. Preserve existing hazard/readout rules. |
| SteamCloud is a separate layer2 `~` Terrain/Vapor entity, authored Thermal120°C and Lifespan3 | `Objects.json:22499–22562` | Do not accidentally admit/repaint this unrelated entity in a tile-mark slice. |

The vent can still participate in other material/electrical/thermal reactions. This audit establishes current cloud semantics, not blanket safety under spells or changed conditions.

## Smallest bounded visual mapping

Proposed slice: one original transparent16×24 **steam tile-state cue** on exact current admitted East12.10, replacing only the existing final-cloud apostrophe branch when Cloud is exactly `steam` and CloudTurns>0. Match actual captured grille/ground palette. Use sparse pale lobes or a short irregular wisp in the upper part of the cell; binary alpha, Point/Clamp,16PPU, center pivot. No orange heat, glow, smoke-darkening or authored danger radius. Keep the grate's accepted pixels unchanged.

This is a tile-state mapping, not an entity ownership mapping. A genuine steam cloud from any source in the scoped East graph should use the same cue; tying it to an underlying vent would invent provenance. Do not add world owners or change saved data. Preserve the separate vent portrait and its ground-state text. Do not fabricate a standalone inventory/portrait subject for the mark.

Minimal seam: optional cached native tile selected inside the existing final cloud branch in `ZoneRenderer.PaintTileStateMarkWithBrine`, after all higher-priority marks. A small optional steam-mark helper/catalog is enough if needed for borrowed-texture ownership and tests; no new GameObject, global scan, animation ticker, material simulation or general gas system. Reuse active flat/current graph/canonical visible-cell admission; retain CP437 in other zones, legacy modes or missing/malformed resources. Cache sprite/tile and dispose only owned objects. Reset per-cell transform/tint on both native and fallback repaint.

Keep priority unchanged: live veil-mist → residue → oil → otherwise unrepresented coating → charge → heat → cold → cloud. Preserve represented water/brine behavior. Tile-state sorting remains4, above foreground3: the cue must remain sparse and readable when an actor covers the cell, rather than silently disappearing or replacing the actor. Rendering only this cue must not rewrite the selected foreground or reader authority.

## Focused RED and counter-check plan

1. Real factory SteamVent + actual `OnPlayerTurnEnd` on a scoped East cell produces existing native grate plus new native steam mark; assert state unchanged by repeated paint, cloud1 and no reaction damage. Default-resource route fails on the old apostrophe **before** asset assertions.
2. Paired native mark for independent `WriteCloud("steam",2)` without any vent; a cold Thermal-only vent with no tile cloud keeps its neutral grate and produces no invented mark. Do not claim cooling itself disables the source.
3. Clear/expiry, smoke, unknown cloud and malformed zero-duration steam keep their existing fallback/clear behavior. Exact other addresses/POI/foreign current graph/legacy mode refuse new art. Missing/malformed optional texture preserves CP437 and approved vent art.
4. Each higher priority (veil-mist, residue, oil, unrepresented coating, charge, heat, cold) still wins over steam. Removing/expiring the higher mark exposes surviving steam; expiry exposes no ghost. Do not broaden Kitchen brine's existing cloud expectations in an East-only pass.
5. Actual actor cover/uncover preserves selected owner and exact vent reader while the sparse tile cue remains; fog clears mark, reveal restores current state, replaced graph/foreign canonical cell grants no disclosure.
6. Use actual WriteCloud/Tick plus bound render hooks to prove incremental repaint and expiry. An inactive cached zone cannot dirty the active zone. Repeated paint creates no new sprite/tile and does not dispatch mechanics; disposal retains borrowed texture and independent peer.
7. Ordinary save/restart and real safe waits prove the exact five live tile clouds survive/renew without owner, HP, inventory, vent body or recipe changes. Native tests may establish staged cold/priority/expiry controls; do not label them naturally acquired gameplay.

Reuse existing paint harness patterns from `FlatPixelKitchenBrineTests`, `FlatPixelGroundStateReadabilityTests` and `EastSurfaceWaterPresentationTests`; update only scoped prior steam-glyph assertions after RED. Neighbor regression: EastKitchen neutral frames, current ground-state priority, KitchenBrine, existing cloud lifecycle/visibility and active render-hook cases. No broad new tooling needed.

## Safe ordinary observation route

The current26,6 checkpoint already sees all five native grates and marks; **no movement is needed for the first before capture**. PageUp for2× if needed. Press L, then Right three individual presses and Down once to target29,7, Return, choose the actual SteamVent and Examine from the observed menu. Look/reader controls are `InputHandler.cs:2323–2374`; use the displayed pile choice if present, not an assumed shortcut. Escape back out before any wait or walking.

If a closer framing is needed, two observed Right presses reach28,6 through27,6, without entering any vent cell. The packaged checkpoint has the cudgel in inventory;27,6 is now clear. From28,6, L→Right→Down targets29,7. Stop at28,6; do not traverse29,7/30,7/31,6/31,7/32,6 or infer safety merely because they are non-solid. Inspect HP/status after each segment. Two ordinary waits from26,6 can show source renewal safely at distance3 from the nearest vent; inspect live state before using closer waits. No spells, cooling experiments, source removal or hazard-entry demonstration are required for this graphics acceptance.

Independent source review agrees that the white marks are tile-state overlays and that existing dirty-cell hooks suffice. Root must still capture exact live mark/owner observations before imagegen, author editable native source, record native RED/GREEN and inspect real1×/2× paint. Whole-opening, actor animation, other regional clouds, physical Deck and sustained performance remain outside this slice.

## Production continuation — 2026-10-27 01:20 UTC

Current pass: actual baseline captured; concept reinterpreted; native source and
RED tests in progress. Continue from unchanged packaged-cudgel-return64, turn3510,
East26,6, HP28. No movement or simulation mutation was needed for this baseline.
Ordinary C Continue, PageUp, L→Right×3→Down→Return→x confirmed SteamVent9346
and the reader's steam-cloud1-turn text. `east-steam-01/02-before-world` and
`03-before-reader` are original1280×800 Unity captures with read-only receipts.
Each of the five observed cells has only Cloud=steam, CloudTurns=1 and zero
heat/cold/charge. Full isolated save root was archived before the helper finish.

Built-in imagegen used actual capture02, with the prompt and output preserved in
`ArtSource/Live16x24/east-steam`. Selected: a narrow muted broken wisp. Rejected:
the generated extra sixth plume replacing a nearby item, altered grate edges,
scene/UI repaint and soft enlarged pixels. Production is original editable
native geometry; no crop/resampling of the concept. Exact part mapping remains
one live tile-state overlay; accepted vent frame16 and its portrait stay intact.

No new native tests or post-integration acceptance claimed yet. The current
shipped packages remain cudgel source0545aa9a2. Normal saves/preferences and
34 unrelated tracked edits were snapshotted; storage inspection found about
412GiB free and performed no storage cleanup. Root alone controls Unity.

## Hourly checkpoint — 2026-10-27 01:25 UTC

Reported: cudgel delivery complete and pushed through c709e7a63; 177 native
cases and actual Mac restart passed. Steam baseline and concept are captured;
original editable source approved for in-game trial. Native steam RED: 36 cases,
35 failed/1 passed, including two actual-paint failures on the old apostrophe.
Initial class-name exact filter selected zero tests and is not counted as evidence;
correct group_names run produced the retained east-steam-red.xml.

Coming-session goals: finish minimal tile-state routing, pass focused and neighbor
regressions, compare actual1x/2x gameplay and renewal/save-restart, inspect render
cost, review and push exact changes. Full six-scene opening and regional coverage
remain unfinished. Storage inspection-only. Last hourly report:01:25UTC.

## Native integration and review — 2026-10-27 01:34 UTC

`FlatPixelEastSteamCatalog` observes exact live `steam` on disclosed canonical
current East cells. It borrows a16×24 texture, owns one cached sprite/tile, and
is called only in the existing final cloud slot after all higher-priority
marks. Production changes are limited to that optional adapter and
`ZoneRenderer` lazy cache/disposal. Accepted grate art, actor/reader ownership,
simulation and save formats are unchanged.

Initial native RED:36cases,35expected failures/1passed. A first implemented
run exposed one test-fixture lifecycle premise: Unity does not dispatch
OnDestroy automatically on the never-active EditMode harness. The corrected
fixture explicitly invokes the saved callback and labels this as function-level
cleanup verification. Production was unchanged. The separate20-case adversarial
sweep covers exact cloud names, loaded tile-state leases, corners, stale graphs,
read-only callbacks and partial resource loss. TileState JSON checks are not
full-session reload claims. Two independent source/test reviews found no
material issue. Authoritative final native results and actual play are pending.

Source validation passed:21opaque pixels,3colors, binary alpha, clear bottom
nine rows, zero overlap with the unchanged grate. The editor import copy has
the same native PNG SHA02703f7b4db68f73c7b50e6cc48fba7a92eb3ee115d50dc8178fe52d89c50aa5.
The imagegen concept's extra plume over the tonic was rejected; only the five
actual saved clouds are eligible.

## Verified Editor acceptance — 2026-10-27 01:42 UTC

Final native run: **211/211 passed, zero skipped**, authoritative XML
`Verification/Live16x24/east-steam-green.xml`.36focused+20adversarial+35East
water+26EastKitchen+58KitchenBrine+20flat ground+16native ground. The MCP job
falsely timed out before Unity started; native XML01:34:23–01:34:40 is evidence.

Actual captures04–10 show the unchanged earned3510 checkpoint at26,6, five
native marks, exact vent9346 Examine, ordinary walk to28,6 and wait through
tick3540, HP28/40. Both default and zoomed views were visually inspected.
Filenames2x mean two PageUp inputs, not a verified2× pixel scale. The approved
grate portrait and tonic remain intact. All five clouds renew to1turn through
real vent reactions; no teleport, grants, reveal or thermal mutation was used.
Full isolated saves were archived before ending the helper; earned after-state
is retained as`Fixtures/east-steam-after64`. Normal saves/gameplay preferences,
34foreign tracked files and prior Mac build match the protection snapshot.
Editor session/test-run bookkeeping changed normally and is listed in the receipt.

A76.5s,1554sample Editor window measured renderer avg0.123ms/max0.489ms and
40drawcalls. MainThread p993.927ms but max847.7ms: this mixed Editor/tool window
does not establish sustained hardware acceptance or attribute that external spike.
See`east-steam-editor-profile.json` and explicit can/cannot verify fields in
`east-steam-editor-play-summary.json`. No cache/storage deletion.

Self-review: 🟡 inactive test-harness callback premise corrected, no production
change needed. 🔵 approved neutral grate and all higher-priority states preserved.
🧪 Linux execution/physical Deck and whole-opening motion acceptance remain open.
Next: main source push, compressed Mac/Linux delivery and actual Mac Continue.
Then North Patchbearer encounter motion/readability, preserving its medicine idles.

### Packaged steam delivery — 2026-10-27 01:52 UTC

Runtime source`be1c52ca375bab35c202eb976a04449e8ef72991` is verified on
origin/main. Fresh compressed Mac and Linux builds each report0errors4warnings.
Linux archive181files,128688319bytes, SHA
`d27fbe7aef2e5e5dec524902966c4741c5909caffd897585ff2a972da48d3675`;
all payload hashes/sizes/modes and credits independently checked.

Six accepted actual Mac captures show ordinaryContinue3540,default/2×world,
a neighboring grass counter,exact vent29,7reader,wait/save3550 and full
quit/relaunch/Continue. An off-screen window capture was rejected and redone
after native Zoom; no screenshot was repainted or cropped. Known ComputeBuffer
shutdown notice persists. Linuxexecution/Deck and full scene/motion/performance
acceptance are unverified. Evidence:`packaged-east-steam-mac-play-summary.json`,
`packaged-east-steam-independent-review.json`,`packaged-east-steam-restoration.json`
under`Docs/Verification/Live16x24`. Normal saves/gameplay preferences,34foreign
tracked files and prior build/archive match protection; test saves archived,
reviewed Unity build serialization restored. No storage cleanup.

Next: ordinary pre-fight North capture and Patchbearer encounter motion/readability
according to`Docs/GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md`. Current steam is stable.
Last hourly report01:25UTC; this delivery is a visible milestone, not a duplicate
hourly report. Keep the continuation active.

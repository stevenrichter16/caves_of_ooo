# East steam-mark audit — read-only, 2026-10-27

Status: source, current saved bytes and actual packaged screenshot inspected. This audit used read-only inspection. Native tests and new visual acceptance are pending. Root owns capture, imagegen, implementation and ordinary play. This plan follows CLAUDE and the live brief's capture → reinterpret → editable native source → integration → play/counter-check loop.

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

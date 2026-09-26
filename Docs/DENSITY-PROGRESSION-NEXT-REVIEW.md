# C10–12 — independent next-slice review

**Status:** read-only source review on 2026-09-26 after the bounded lair placement repair. No new progression implementation is claimed here. Root coordinates the second-layout owner and the native window. This review extends the verified corrections and acceptance matrix in `DENSITY-UNDERGROUND-LAIRS.md`.

## Recommendation and verified scope

Finish **C10.1, a bounded second ordinary underground layout**, before coupling progression to new loot, boss variants or save-ledger fields. The actual recipe is `OverworldZoneManager.CreateUndergroundPipeline`: SolidEarth plus Strata, followed by connectivity, reciprocal stairs, stair connector, depth landmarks, hazards, one bounded encounter group and containers. Six material bands and three Strata geometry bands already work. A second geometry can retain that tail and use the depth palette without multiplying threats or rewards.

A deterministic roomed alternative from depth3 in ordinary Spread/Sodden/Beating columns is a small useful candidate; depth1, natural Choir columns, authored Overwrit/Stump routes and named POI recipes remain explicit controls. Reuse configurable RuinsBuilder geometry without changing its surface callers. Layout selection should have its own stable seed and a worldgen receipt, leaving the existing population stream unaffected. Confirm both outcomes with actual generated cells and the full occupied-cell API. Counter-check excluded routes, reserved arrivals, clipped bodies, trapped approaches and cached/save-loaded zones. Native acceptance must enter, descend, inspect and return through ordinary controls. No Objects edit is needed for this geometry milestone.

| Remaining claim | Verified current contract | Implementation consequence |
|---|---|---|
| Multi-level lairs | Surface-only lair routing; generic depth routing otherwise; no current saved final-floor plan | Separate C10.2 milestone. Add persistent graph/boss/reward ownership before stairs can represent a real lair stack. |
| Depth-aware bosses | Recorded POI tier is not used by the current biome-only boss map | Review concrete biome/tier variants and tags; do not use AncientGuardian everywhere or infer Boss from item tier. |
| Forever-unique lair rewards | Zone unload can discard the cache; POI Profile is not serialized by SavePointOfInterest | Cache identity alone is insufficient. A versioned world-part record must survive death, removal, unload, saves and failed loads. |
| Higher-tier ordinary finds | ContainerPlacementService.ClampTableTier and LootDropSystem.ResolveTier clamp to 1–3; resolved current Item blueprints have no Tier4+ | Author real bounded T4 variants and a source/value contract first. Increasing a table suffix alone cannot finish C11. |
| Random enhancements are table metadata | LootEntryData has blueprint/table/weight/count/chance fields; Roll returns blueprint strings; LootStocker simply creates those entities | Add an explicit controlled creation hook after the source is approved. Unrecognized JSON fields or modified roll strings would lose the enhancement contract. |
| Enhancement value automatically rises | TradeSystem.GetItemValue reads Commerce.Value times stack count; ItemEnhancing.Apply does not alter that policy | Pin the intended sale value. Do not claim a premium or expand global trading rules without a deliberate design and tests. |
| C1 census is the final economy result | The preserved after overlay contains only armor pools and nine hostile loadouts | Later books, vessels, identities, C13 roster and C14 geometry require a new integrated checkpoint. |

## C11 staged proposal

First author a small T4 weapon/armor set with meaningful stat tradeoffs, source restrictions and explicit Commerce values. Validate the inherited final entities and every actual table path; preserve natural Choir caches, crafted/rental/unique provenance exclusions and visible-equipment death drops. Then consider bounded spawn enhancements through `ItemEnhancing.Apply`, whose compatibility gate, two-slot cap, equipment hooks, saved parts and inspection output already exist. Require 0/100% controls, replay, incompatible-item refusals, unchanged stacks, save/equip/unequip and sale-value checks. Enhancement tier and item tier are distinct contracts.

A legendary-template system is a later ownership milestone: saved generated name/epithet, allowed kit and a reward physically carried by its owner. Its identity must not clone authored lore people. Do not bundle this with the first layout or generic merchant enhancements. BitLocker remains unavailable in regular play, as instructed by the user.

## C12 measurement and acceptance

The initial bounded census used five seeds (1, 64, 1729, 2026, 729490642), 120 wilderness cells, 15 generated lairs and 15 depth1/3/5 cells; it recorded447 handled opening attempts against actual containers and separately measured ten shops and one designed starting grant. It explicitly recorded absent biome lairs instead of fabricating them. Its C1-only armor units rose 19→59 and contained Commerce value 11,706→14,013; those are historical slice results, not final balance evidence.

The C11 follow-up found that the original census checked the legacy container lock only and equated a handled action with a successful opening. Its contents/value totals remain measurements, but the447 count is not proof every key lock was satisfied. The final census must observe both lock authorities and a successful actor OpenContainer event.

For the integrated checkpoint, replay the **recorded baseline zone IDs**, not a fresh first-four-non-POI selection. C14 reservations or POIs can otherwise change sample membership while the seed stays equal. Retain original IDs and report changed source classification; add newly introduced sites as a separate expansion. Keep raw C13 historical IDs and use an explicit display-only mapping for comparisons. Test each new category with controls (books, vessels, armor, offense), preserve actual stack valuation, and distinguish factory kit distributions from natural encounter frequency.

Compare quiet-zone and per-source distributions, legal approaches and real action availability as well as content totals. Run ordinary-stat early/mid/deep routes with distinct builds and starting supplies; report threat readability, preparation, recovery and repeat visits. Core generation can prove geometry, table data and action contracts, but not native appearance, pacing or feel.

The integration regression uses the baseline's exact 672 selected test **files**, with intentional current content/grammar pins inside those files. New fixture files have separate focused evidence and are not silently counted as baseline coverage. Preserve old failed runs, full XML, snapshot hashes and the exact name-based differential. Complete native full EditMode and finite input scenarios before describing the integrated work as verified.

## Review conclusions

- 🟡 Keep multi-level lair ownership separate from geometry; otherwise unload or lower-first generation can clone bosses/rewards.
- 🟡 Lock census membership before comparing economy; seed equality does not guarantee sample equality after world-map changes.
- 🔵 Reuse existing depth material/resource/population rules and enhancement mechanics rather than replacing them.
- ⚪ No second layout, T4 finds, enhanced spawn or legendary behavior is proven by this source review. Their RED, adversarial, census and native gates remain open.

Files inspected: `OverworldZoneManager.cs`, `StrataBuilder.cs`, `SolidEarthBuilder.cs`, `ContainerPlacementService.cs`, `LootDropSystem.cs`, `LootTables.cs`, `LootStocker.cs`, `ItemEnhancing.cs`, `TradeSystem.cs`, `DensityLootCensusTests.cs`, the C1 census receipts and the integrated completion plan. The detailed stair/save/lair sweep remains in `DENSITY-UNDERGROUND-LAIRS.md`.

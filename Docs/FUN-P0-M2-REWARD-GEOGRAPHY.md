# FUN-P0 · M2 — Reward Geography

> **Status:** Pending (after M1) · Parent: `FUN-P0-SPINE.md`
> **Goal:** Travel pays. The second village disproves the photocopy:
> grimoires live in the world instead of one spawn chest, lairs drop
> treasure worth the walk, and each faction village has a face.

---

## Verification sweep corrections (§1.2)

| # | Analysis claim | Sweep finding | Plan impact |
|---|---|---|---|
| C1 | "18-grimoire chest in every village" | VERIFIED (starting :50-52, others :73-74) — **but ContainerPart MaxItems=10 silently drops 8 of the 18 today** (AddItem returns false, ignored; Chest blueprint Objects.json:4524). | The "18-grimoire chest" was always a 10-grimoire chest. Starter cut fixes the overflow by design; add a full-add assertion. |
| C2 | LockedChest is ready to use | **The 'Open' path never consults LockPart** — ContainerPart.Locked (false in blueprint) gates opening; LockPart.IsLocked only gates bump-through. Today a LockedChest opens keyless via menu. Nothing syncs LockPart→ContainerPart. | M2.b must: set `Locked=true` on the boss-chest Container, and flip `ContainerPart.Locked=false` in LockPart's successful-unlock branch. This also fixes the latent IronKeyQuest exploit. |
| C3 | "12 unplaced unique weapons" | **11 unplaced**: scenario-only FlamingSword, IceSword, CryoLance, EmberSpear, AcidicDagger, ThunderHammer, DissolutionMaul; referenced-nowhere Claymore, VenomDagger, SeveranceEdge, PalimpsestBlade. (Battleaxe/Greatsword/Warhammer were debug-bootstrap-only → after M1.a they're unplaced too.) | Boss-drop + chest pool built from this corrected list. |
| C4 | Ambassador spawn "keyed off PointOfInterest.Faction" | POI **is** available at population time (`_poi.Faction`, WireNPC :1242-1249). But `WireNPC` overwrites NPC faction tags with poi.Faction — which **breaks TradeStockBuilder for non-Villagers villages** (`faction != "Villagers"` → continue): Desert/Jungle/Ruins merchants receive NO trade stock today. | Ambassador spawn is straightforward. **Bonus latent bug**: fix TradeStockBuilder's faction gate so faction-village merchants get stock at all. |
| C5 | Chest position pin | VillagePopulationBuilderTests pins starting chest at exactly (43,11) with Contents>0 (not the 18 list). RNG-sequence sensitivity: removing the *other-villages* chest call changes downstream same-seed draws; ambush-rate tests are statistical. | Keep starting-chest placement path identical; expect + rerun statistical suites; adjust only if bounds trip. |
| C6 | Boss placement | Boss lands deterministically at boss-chamber center (40,12) via LairBuilder:77-83; loot scatter is zone-wide random (the ":37 near center" comment is aspirational). LairBuilder holds bossRoom coords in scope — chest insert point is right after :83. | Chest placed by LairBuilder (in-room guarantee), not LairPopulationBuilder. |
| C7 | Faction reference | WorldMapTests:1384 pins starting-village Faction == "Villagers"; no ambassador blueprint exists for Villagers; PaleCurator's faction (PaleCuration) maps to no biome. | Ambassador map: SaccharineConcord→SaccharineEnvoy, RotChoir→ChoirTendril, Palimpsest→PalimpsestEcho, Villagers→none (starting village stays ambassador-free in P0; GlassblownDrifter/PaleCurator become wanderer/lair-adjacent spawns in P1). |

## Scope

**In:** M2.a starter-chest cut + grimoire distribution · M2.b lair boss drops + locked treasure chest (+lock-sync fix) · M2.c faction ambassadors + TradeStockBuilder faction-gate fix.
**Out (pruned):** per-faction trade stock lists (P1 — the gate fix alone un-breaks stock in faction villages); merchant camps (P1); tier-3 tables/underground bands (P1); PaleCurator/GlassblownDrifter placement (P1 — no biome home yet).

## Content readiness

- 🟢 Grimoires, unique weapons, LockedChest, IronKey, ambassador blueprints + conversations all exist.
- 🟢 Delivery systems exist (ContainerPart, death-drop, conversation GiveItem, POI plumbing).
- 🟡 Lock-sync is a small engine change (LockPart unlock → ContainerPart.Locked=false).
- 🟡 TradeStockBuilder gate fix changes stock behavior in faction villages (currently: none at all).

## Sub-milestones

### M2.a — Starter chest cut + world distribution
- Starting village chest → 3 starters: Kindle, ConjureWater, PurifyWater
  (teaching trio: damage, utility, settlement-repair rite). Assert all adds
  succeed (C1).
- Other villages: **no grimoire chest** (remove the :73-74 fallback call).
- Distribution of the remaining 15: Scribe NPCs in non-starting villages carry
  2 biome-keyed grimoires in inventory (sold via existing trade flow — Scribe
  gains InventoryPart stock in VillagePopulationBuilder, bypassing
  TradeStockBuilder); 4 element-keyed ones become lair-boss chest loot (M2.b);
  2 become ambassador conversation rewards (M2.c GiveItem); remainder stay
  scribe-pool so every grimoire has ≥1 world source. Full mapping table in
  implementation log.
- Tests: starting chest = exactly 3 + position pin survives; other-village
  chest absent (counter-check); scribe stock present + biome-keyed; every
  grimoire blueprint reachable from ≥1 source (reachability pin — the process
  lesson from the fun-gap analysis).

### M2.b — Lairs pay
- LockPart success branch also clears `ContainerPart.Locked` (C2) + test
  (RED: open locked chest keyless via container path → must fail now).
- New `BossChest` blueprint (LockedChest clone, KeyId "iron", Locked container),
  placed by LairBuilder beside the boss (C6); boss carries IronKey in inventory
  (existing death-drop drops it — kill boss → key → chest).
- Chest contents by biome: 1 unique weapon (Cave: ThunderHammer / Desert:
  EmberSpear / Jungle: AcidicDagger→VenomDagger pair / Ruins: CryoLance) +
  1 element-keyed grimoire + 1 enhancement mineral (ChoirIron/PaleSalt/GlowQuartz).
- Boss blueprints get equipment where thematic (AncientGuardian wields
  SeveranceEdge — dropped on death via existing pipeline).
- Tests: lair build → chest exists in boss room, locked, contents pinned;
  boss inventory has key; counter-check: chest without key stays locked
  through container open path; statistical ambush suites still in bounds (C5).

### M2.c — Different faces
- VillagePopulationBuilder: for non-starting villages, spawn the faction
  ambassador mapped from `_poi.Faction` (C7 map) near the village square;
  ambassadors already carry full conversations + ChangeFactionFeeling actions.
- Fix TradeStockBuilder faction gate (C4): stock any Creature+InventoryPart
  village NPC whose faction matches `poi.Faction` OR "Villagers" — minimal
  change: accept the zone's village faction, keeping non-village zones out.
  (Requires passing POI or reading zone faction; sweep shows TradeStockBuilder
  gets only SettlementManager — constructor gains optional PointOfInterest.)
- Ambassador grimoire gifts: 2 one-time GiveItem rewards behind existing
  property gates (ChoirGaveGift pattern).
- Tests: per-biome village build spawns correct ambassador (+ none in starting
  village counter-check); faction-village merchant now has stock (RED today —
  pins the C4 bug fix); ambassador gift property-gate one-shot.

## Performance note
All zone-generation-time work; no per-frame/per-turn paths.

## Divergences
- Ambassadors as fixed village NPCs (Qud analog: legendary village denizens) —
  CoO-original placement of CoO-original NPCs.
- BossChest key reuse ("iron" master key) matches existing LockKey content
  contract (LockKeyContentTests pins KeyId=iron); per-lair key IDs deferred.

## Implementation log
(filled per sub-milestone)

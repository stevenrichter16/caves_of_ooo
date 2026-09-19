# Loot finds — made things in the world

**Status:** planned, 19 September 2026. User direction: "increase the drop rates for weapons, armor, offensive items. Right now in the game there is mainly crafting items, which are nice but the player should be able to find already created items." CoO-original tuning; no Qud parity claim.

## Goal

A player who opens containers and wins fights finds finished weapons, armor and offensive consumables often enough to change their kit between towns, without making crafting pointless, emptying the shops' purpose, or breaking the two shipped design rules below.

## What the game does today (measured from the shipped tables)

| Source | Weapons | Armor | Offensive items | What dominates |
|---|---|---|---|---|
| Crate T1/T2/T3 (also wooden barrels) | Dagger 20 % at T1 only | Leather armor 15 % (T2), chain 15 % (T3) | none | coin, reagents, components |
| Sack, basket, hollow log (all tiers) | none | none | none | reagents, food, coin |
| Urn T1/T2/T3 | none | bucklers/cloaks 15–18 % (T2/T3) | none | coin, reagents, salts |
| Bone cache T1/T2/T3 | dagger 15–25 % (T2/T3) | none | none | bone, reagents, coin |
| Strong box T1/T2/T3 | none | 20–25 % one piece | none | coin, components, tonics |
| Humanoid death T1/T2/T3 | none (by design) | none | poison grenade 12 % at T3 | coin, reagents, components |
| Beast and construct death | none | none | none | meat, reagents, components |
| Hostile humanoid loadouts | 11 hostile humanoids carry one dagger/short sword/spear | leather pieces at 20–35 % | none | — |

Offensive items that exist and are nearly absent from loot: three gas grenades (poison, stun, sleep) and six tonics that shatter on a thrown target with a one-cell burst (fire, frost, acid, lightning, poison, bleeding; `ThrowItemCommand.ApplyTonicAoe`).

## Design rules this must keep

- **Choir country holds nothing manufactured** (W4.1; pinned by `GrovelandsFormationTests.TheContainerPool_HoldsNothingManufactured`). Grovelands containers are sacks, woven baskets and hollow logs. Those three tables stay natural: no weapons, armor or manufactured offensive items are added to them.
- **Death loot is the supply line; weapons come from visible gear** (`LootDropSystem` docstring: weapons come from loadouts "where the player can see them before the kill, which is more legible"). Weapons and armor from fights therefore come from richer hostile loadouts, which drop through the existing equipment spill. Humanoid death tables gain only pocket-sized offensive consumables.
- Friendly and service NPCs (farmers, scribes, merchants, wardens) keep their loadouts: this is not a reason to kill villagers.
- Rentals (`Loaner*`), crafted outputs (`ForgedWeapon`) and unique vault pieces stay out of the general pools.

## Verification sweep — corrections before any code

| Tempting premise | Verified fact | Consequence |
|---|---|---|
| There is one loot table per container | `ContainerPlacementService` picks a kind per biome pool (weighted), each kind names a tiered table (`CrateT`, `UrnT` …); wooden barrels use `CrateT` | Raising `CrateT` also raises barrels; intended |
| Tables can nest | `LootTableRegistry` supports `TableRef` entries to any acyclic depth, pick-one and independent modes, validated at boot | New pick-one pools `FindWeaponT1–3`, `FindArmorT1–3`, `FindOffenseT1–3`, referenced from existing tables by chance |
| Creatures drop what they carry | `CombatSystem` death: `Body.DropAllEquipment`, carried inventory, then one death-table roll; suppressed by `NoDropOnDeath`/`Temporary` | Loadout upgrades flow into drops with no code change |
| Loadouts can vary by chance | `LoadoutPart` `Equip` takes `Blueprint[:chance]`, `Pick` takes `count;A;B;C` | Hostile loadouts gain a picked weapon and chance-gated armor |
| Offensive consumables are thrown | Thrown tonics shatter (radius 1) and apply their status; grenades release gas | The offense pools are tonics and grenades |
| AI uses carried grenades | No AI throws items (`AIRetrieverPart` fetches only) | Pocket offense in death tables cannot make fights harder |
| Finds could flood the economy | `TradeSystem.GetSellPrice` = value × performance ÷ faction modifier; T1 weapons are worth 6–18, T3 up to 100 | Measure found-gear value per zone before and after; bound, not fixed here |

## Targets (per roll, expected count, chance-based)

| Table | Weapon | Armor | Offense |
|---|---|---|---|
| Crate T1 / T2 / T3 | 25 / 30 / 35 % | 20 / 25 / 30 % | 20 / 25 / 30 % |
| Urn T1 / T2 / T3 (grave goods) | 15 / 20 / 25 % | 10 / 15 / 20 % | — |
| Bone cache T1 / T2 / T3 (fallen travellers) | 25 / 30 / 35 % | 20 / 25 / 30 % | — |
| Strong box T1 / T2 / T3 | 20 / 25 / 30 % | 25 / 30 / 35 % | 20 / 25 / 30 % |
| Reliquary T1 / T2 / T3 | 15 / 20 / 25 % | 15 / 20 / 25 % | 10 / 15 / 20 % |
| Alchemy shelf T1 / T2 / T3 | — | — | 30 / 35 / 40 % |
| Humanoid death T1 / T2 / T3 (pockets) | — | — | 20 / 25 / 30 % |
| Sack, basket, hollow log, beast, construct | unchanged | unchanged | unchanged |

Existing rows are kept; the new rows are added beside them.

## Semantics (the contract, in player terms)

- A crate, barrel, urn, bone cache, strong box or reliquary in manufactured country often holds a finished weapon, a piece of armor or a throwable tonic or grenade, with better pieces at higher tiers.
- Hostile humanoids visibly carry a weapon and more armor than before, and drop it; they sometimes carry a throwable in their pockets.
- Choir country's containers, animals and friendly villagers are unchanged.

## Sub-milestones

- **L.1 — The census.** An EditMode audit generates real zones across several biomes and seeds, opens every container, and counts finds by category; plus the exact expected values from the table math. Baseline receipt before any content change.
- **L.2 — The pools and the containers.** Nine pools; the table rows above.
- **L.3 — Hostile loadouts and pockets.**
- **L.4 — Adversarial sweep.** Unknown blueprints and cycles rejected at boot; Choir-country and friendly-NPC counter-checks; category floors per tier; economy readout.
- **L.5 — Native proof.** Rides the next accepted journey run: the census records one opened container's contents in a manufactured zone.

## Performance and observability

Content-only apart from the census; rolling happens at generation and death, unchanged. The census emits one `loot/Census` record per (source, tier) cell with counts per category; existing `loot/DeathDrop` records are unchanged.

## Implementation log

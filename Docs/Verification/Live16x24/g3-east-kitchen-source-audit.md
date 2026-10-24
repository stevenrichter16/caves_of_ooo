# G3 east connector / Orven kitchen: source and receipt audit

Read-only audit, 2026-10-24 UTC. No Unity, generation, gameplay, content or rendering mutations were performed. This is a next ordinary-journey guide, not live acceptance or an art packet proposal.

## Result

The actual eastern services already exist. The missing link is **native kit admission and coverage**, not flat projection: G1 requests flat 16×24 everywhere, while `FlatPixelGreyboxCatalog.IsAdmitted` still includes only glade, cellar and the two north sites. East/kitchen therefore remain playable with validated glyph fallbacks and portable native actor exceptions. Do not describe them as reverting to the prior 3D presentation.

Before expanding admission, read the current manager's exact cached graphs, biome and POI at `(12,10,0)` / `(12,11,0)`. The saved two-seed census supports these sites, but is not authority for the current live save. Admission must remain independent of whether Orven, the forge or a harvested crop survives; rendering must not regenerate a missing service.

## Evidence and identities

`Docs/Verification/Live16x24/connected-two-seed-census-summary.json` and its compressed raw census were recorded at `2026-10-23T07:41:24.2753310Z`: 12 detached graphs, seeds64/65, 9.373s, cleanup/restoration true. They prove realized initial owners and stock at that checkpoint, not current FOV, routes, NPC positions or ordinary interaction.

| Graph | Seed64 facts | Seed65 counter |
| --- | --- | --- |
| `Overworld.12.10.0` | Spread, POI null, TemperingShelter / BankCrossing; forge `(7,19)`, Stoneburr `(9,20)`, StoneWalls `(5,17),(6,17),(9,17)` | Same biome/no POI/family; OffsetLanes, forge `(65,4)`, Stoneburr `(67,5)` |
| `Overworld.12.11.0` | Spread, POI null, WaysideKitchen / BrokenEnclosures; Orven `(18,8)`, oven `(16,8)`, bed `(20,8)` | BankCrossing; Orven `(54,16)`, oven `(52,16)`, bed `(56,16)` |

Source assigns kitchen only at the eligible connected resident address and shelter only for the eligible versioned eastern worksite: `SpreadExplorationPlan.cs:115–140`. `CoveredNow` / `PlacementNow` at213–223 still exclude claimed POIs and incompatible contexts. `SpreadExplorationResidents.cs:45–60` supplies relative owners; bounded safe placement chooses the actual site anchor. Do not hardcode the seed64 owner positions into rendering or gameplay.

### Seed64 east owners worth observing on the actual approach

- Forge `(7,19)` and the finite binding harvest `(9,20)`. Seed64 **did not place the authored Cindercaller/Scrabbler pair**; the ordinary Scrabbler initially at `(6,24)` is a different source. Seed65 did place that worksite pair. Do not add the missing pair for a screenshot.
- Pen gate `(53,17)`, grazer `(51,17)`, hedge border around x49–53/y15–19, ordinary and reserved ripe rows `(55,17)` / `(55,19)`.
- Ordinary Farmer `(66,5)`. Steam vents `(76,2),(76,3),(29,7),(30,7),(31,7),(31,6),(32,6)`.
- The actual foreground inventory includes **282 CharmFlowers** (271 potential top-owner cells before fog), 15 ordinary Walls, 15 Hedges, 4 CopperPipes, 7 SteamVents, a MillStone, a Chest and a Sack. An inspection limited to the forge would miss conspicuous route coverage.
- Farmer/chest/magpie stock is real, finite and seed-dependent. The census does **not** establish a complete head+haft+binding forging kit on this route. Avoid promising a successful craft until carried choices are inspected.

### Seed64 kitchen owners and stock

- Orven `SpreadWaysideCook` `(18,8)`; Oven `(16,8)`; unclaimed Bed `(20,8)`; Chair `(20,7)`; two StoneWalls `(16,6),(17,6)`.
- Broken `ConnectedBatchPan` `(18,6)`, escrow `(19,6)`, pickup `(20,6)`. Both trays initially empty/unlocked. Pan starts configured, unrepaired and Idle.
- Ripe prepared `ClaspbeanCrop` `(16,10)` yields two pulp and one seed; stage2, moisture0. Initial ripe harvest does not require watering; regrowth/replant acceptance does.
- Kitchen also contains 215 Hedges, 20 Trees, 33 Bushes, ordinary loose/contained goods, a Viper initially `(79,12)`, and BrinePools `(36,17),(36,18)`. The dense hedge arrangement makes blind repeated movement unsuitable.
- Cook's recorded carried stock was one each RawMeat, Mushroom, Emberwheat, DriedMeat and ToastedEmberwheat. **One grain sold by Orven does not satisfy the two-grain batch recipe.** Preserve or bring actual raw grain; free cooking converts the whole selected stack.

## Next ordinary journey, with no debug supplies or forced outcome

1. From Nella, return south through north works into the glade, preserving actual HP, inventory and permissions. If starting fresh at glade `(40,12)`, approach the east edge using currently visible/passable cells. The glade has a horizontal open approach band at y16, while the tall wall at x53 blocks y0–9; neither makes the trip safe from moving hostiles (`ReferenceGladePlan.cs:44,55,111–112`).
2. Cross the eastern boundary normally. East `(79,y)` ideally wraps to `(0,y)` in12.10; the real transition can choose a nearby eligible arrival if obstructed (`ZoneTransitionSystem.cs:58–73,112–150`). Capture current graph/POI, entry, fallback/native mix and the real forge approach. Use Look to route around current occupants and hazards.
3. Approach the **currently present** forge/Stoneburr site. Look→Return opens the real owner action menu. Harvest the finite Stoneburr and inspect what is actually carried. Forge's normal player UI is the component picker: Heads/blades, Hafts, Bindings, Quenches, Weapon and `>> Craft <<`; actorless `f/r/q` options are not the normal player sequence (`ForgePart.cs:35–77`). Do not imply a completed craft without available parts.
4. Move south out of12.10 into12.11. South `(x,24)` ideally arrives `(x,0)` subject to the same eligible-arrival search. Use current Look/navigation around the kitchen's dense hedges; seek Orven/site from the realized map, using `(18,8)` only as the historical seed64 landmark.
5. Inspect Orven, broken pan and empty trays before acting. Bring **two FireClay**, **two raw Emberwheat**, **one ClaspbeanPulp** and **two drams** for the complete repair/batch path. The cellar supplied two clay, but those are gone if already spent repairing the well. Use actual remaining sources/current inventory; record denied action if unavailable instead of refilling a fixture.
6. Harvest the site's ripe claspbean normally. Adjacent to the pan, select `patch the batch pan (2 fire clay)` (`Tier1Repairs.json:44–51`, `RepairablePart.cs:53–98`). Then, while next to living friendly Orven, select `ask Orven to vouch for the repair` if available. The introduction requires this player's recorded repair, the original pan/world identity and observable nearby cook/pan; repair alone does not grant Nella's rights (`CookIntroductionPart.cs:47–90`).
7. Adjacent to the repaired pan, select `prepare field meal (2 grain, 1 claspbean, 2 drams)`. It consumes actual carried ingredients into escrow and charges the fee. It requires viable nearby friendly staff, functional structure, clear/unlocked storage and the real clock. Completion is due after120 world ticks, **not an asserted fixed number of keypresses**. Observe ordinary wait/travel/return and the actual ready description; no renderer reconciliation (`KitchenBatchPart.cs:17,104–143,158–219,233–249`).
8. Inspect/take the actual parcel from pickup. Ready art must follow the real owned output, not the `Ready` string alone. The carried FieldMeal offers `eat (3d4 healing; stop bleeding)` and stops ordinary BleedingEffect, not every debuff. Record working→ready→taken/empty plus save/revisit.
9. Independently demonstrate ordinary free cooking beside the Oven: inventory→a carried raw ingredient→`cook this stack beside a fire`. Keep the two grain reserved for the batch until done. Oven is a deliberately added **non-finite CampfirePart**, so it does not need finite fuel/150°C and should not borrow those campfire readiness states (`SpreadExplorationResidents.cs:115–121`, `CookingService.cs:96–142`).
10. To rest, stand **on** the unclaimed bed and use `sleep in bed` or `sleep until next time of day`; Orven's Oven intentionally offers no rest. Current enemies/occupancy can deny the bed (`PlayerBedService.cs:24–63`). Return to Nella and relay the introduction only if gathering rights are still Unknown; an already-paid claim must not be portrayed as newly granted. End with ordinary save/quit/Continue and current-state comparison.

## Actionable graphics / admission gaps

1. **Exact east scope missing.** `FlatPixelGreyboxCatalog.cs:161–175` admits only glade/cellar/north. Extend only after live current-graph/biome/no-POI verification, with changed-POI/replaced-graph counters. `SpreadPresentationScope.IsActive` is broader and alone does not exclude POIs. Keep native admission distinct from global flat mode.
2. **Coverage does not travel with ordinary goods/vegetation yet.** `TryResolvePresentation` at684–729 and subject lookup at735–748 allow local art outside admission only for the four portable native actor identities. Existing Hedge/Tree/StoneWall/Stoneburr/item art can be reused deliberately after admission; no need to repaint approved source to fix the scope. G2 authored pond/road/field ground remains exact-glade only; expanding east native foreground must not silently expand its substrate contract.
3. **Actual service identities have no native selector.** There is no current FlatPixelOpeningArtCatalog binding for SpreadWaysideCook, Oven, forge, kitchen pan/trays or Claspbean. Approved `Assets/Art/Cook16x24/cook-16x24.png` should be preserved and bound to the real cook, not a demonstration replacement. Snapshot actual kitchen/forge states first, then design only the encountered gaps. The pen, flowers, industrial remnants and incidental residents are measured coverage gaps, not optional fictional decoration.
4. **State honesty matters more than extra frames.** Batch `Bound` validates exact pan/cook/trays and anchored tray positions; staff can move within range. Working must require the real configured job/inputs. Ready requires real FieldMeal reference, quantity and pickup custody (`KitchenBatchPart.cs:61–93,215–219`). Existing Changed marks pan/escrow/pickup after service transitions (360–364); inspect real consumption/transfer invalidation before adding a frame-time observer. Art must not call `Reconcile`, complete work or change permissions. Neutral trays must not imply arbitrary contents are ingredients/output.
5. **Current documentation drift, not historical evidence to delete.** CONNECTED-SITES line3 still says opt-in, release does not enable flat, and plot ordinary acceptance pending. Lines7/overview also leave obsolete normal-activation/growth gates. Current G1 normal launch/Continue and completed plot receipts supersede those claims; plot/works product ordinary acceptance is still pending. Update only current overview with authoritative G1/ledger references; retain historical checkpoint wording labeled as history. The IsAdmitted XML comment at158–159 also still calls north art greybox.

## Acceptance boundaries

- The detached seed64/65 census proves historical source population, not current ordinary-play appearance. New actual east/kitchen graph and capture receipts remain required before claiming G3 coverage.
- No new encounter, crop, stock, repair material, service or mechanic is recommended here. Missing prerequisites/hostile interruption are valid real outcomes to preserve.
- Current continuous flat fallback is usable but does not satisfy G3's approved-art coverage gate. Neither this audit nor native unit tests establishes physical Steam Deck performance, controller journey completion or packaged release acceptance.

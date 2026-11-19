# Machine V1 dossier: Signal relay (machine 11)

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Lore authority is root `Lore/` only (`Docs/Lore/` unused). All C#/JSON below is a sketch for the implementing engineer, not compiled code. Every in-world string is a DRAFT for an intent card and the fresh-context blind voice gate (lore R10, landmine L17); none is shippable text.

Evidence tags: **[V]** I read the cited line this session. **[R]** taken from `mw/*.md` research, not re-read. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle probe settles it.

Designed against `mw/design/ARCHITECTURE-SPEC.md` (slices S1-S24 and contracts C1-C11 refer to it). Standing gate: `CLAUDE.md` says "No new runtime changes" and keeps explicit work permission and the weapon-payload/equipped-input/save policies behind independent design critique before RED. V1 below needs the **admission policy** critique (section 4.3, R1) and a lifted hold; it needs no work-permission policy.

---

## 0. Verdict

1. **The relay as pitched has no real purpose in the shipped world, and the signal half should not ship.** "Receive reports, contact known people, exchange observations" needs remote state worth reporting and someone who observed it. Neither exists. Unvisited zones do not exist; visited zones are frozen the moment the player leaves; the only remote state that changes with time is lazily reconciled on arrival (`SettlementManager.AdvanceSettlement`, `SettlementManager.cs:87-133` [V]), so a "report" would be the player's own record read back, or an invented observer. The pitch's other half, "connect your remote workshop with a settlement", needs player-owned nodes, and the game has no home/base yet (`mw/home.md`; `lore.md` section 9). Section 3 shows the audit.
2. **The smallest honest relay is a carriage arrangement, not a transmitter.** The only part of the pitch with an authored need, a verified canon vehicle and checkable accounting is "goods still travel through real arrangements and routes": the Saccharine Concord's **contract of carriage** (`Lore/Codex/06_ConcordCarriageContract.md` [V]). V1 is: entrust one carried stack to a Concord ledger-keeper at one authored post, pay the rate, and claim the **same entities** at another authored post after a deterministic number of world ticks. No simulation of the caravan, no random loss, no reconciler, no new object, no placement. The word "departed" is the only status the post enters between; that is the canon honest-limits idiom (clauses 3-4) and it also removes any need to model the journey.
3. **Classification: `wave3-needs-new-system`**, with low coupling. The new system is small (one saved Part holding detached goods, a route table, an admission table, one service) but it is the first cross-zone custody in the game: the project's own plan lists "one real persistent delivery between meaningful places" as M6, Planned (`Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md:45` [V]) and "cross-zone cargo remain open" (`:81` [V]), and eight settlement dossiers name "genuine cargo custody" as the missing foundation [R]. It depends on **none** of S3-S24: it rides the ConcordAccount architecture (a static service, a player-attached state Part, conversation choices on existing ledger keepers; `ConcordAccount.cs`, `ConversationManager.cs:20,231` [V]). It needs S1 (save golden) only. This means it can be built in parallel with the spec's phase A and still reach old saves (section 4.13).
4. **It does not close the relay gate.** The gate is "One physical delivery or report follows a real arrangement, survives interruption/reload and cannot duplicate goods" (`Docs/MACHINE-SYSTEMS-DESIGN.md:95` [V]). V1 meets the delivery clause for exactly two authored posts (three when the player contracts the Sill well to the Concord, section 4.11). It deliberately does not supply connectivity between owned places, coverage rules, reports, or contact; those are deferred to V2/V3 with named prerequisites (section 8).
5. **Purpose is conditional, and the dossier says how it can fail.** The one authored problem it solves is real but narrow (the Last Counter issues a weapon-component consignment and has no forge; `settlement-last-counter.md:122` [R]). Section 4.15 is a purpose test that can fail: if actual play shows nobody ships anything, or carriage always beats carrying, V1 is recorded as not purposeful and parked (owner decision D1).

---

## 1. The pitch and the project's own bar

User pitch, verbatim in substance (`Docs/MACHINE-ROSTER-VISION.md:83-88` [V]): "Receive reports, contact known people, arrange deliveries, and exchange observations between established sites. Coverage could depend on elevation, interference, and repaired infrastructure. Goods still travel through real arrangements and routes. An old tower might become valuable because restoring its transmitter connects your remote workshop with a settlement where you have trading relationships."

Project bar: "Connectivity, observed reports and persistent dispatch/delivery state with genuine cargo custody" supports "one physical delivery or report ... cannot duplicate goods" (`MACHINE-SYSTEMS-DESIGN.md:95` [V]). Expansion step 5: "bounded survey information and a first physical relay delivery. Build on earned knowledge, actual routes and relationships, then connect multiple installations or homes" (`:119` [V]). The vision file's recurring themes bind V1: choices about what survives, ordinary convenience stays (carrying is never degraded), bounded/authored/local, expedition and base relevance.

Canon re-grounding (`lore.md:75` [V]): "transmitter, tower, signal" are absent from `Lore/`; the vehicles are "posts, lamps, bells, cords, beetle jars, dispatch desks, riders"; the canonical "old tower" is the Concord's withdrawn frontier posts (two abandoned counters, `OverworldZoneManager` AbandonedCounterZoneA/B). Player text must therefore never say transmitter, signal, network, power or charge (landmine L16).

---

## 2. Verified foundations, and what they do not give

### 2.1 What exists and is reused

| Foundation | Evidence | Use in V1 |
|---|---|---|
| **One ledger, many posts**: two blueprints carry the `ConcordLedger` tag, `ConcordFactor` (also `ConcordCredit`) and `SaccharineEnvoy`; a post-keeper's conversation already offers account choices and reads the account in its greeting | python scan of `Objects.json` (only those two carry the tags) [V]; `ConcordAccount.cs:8-17` (header), tags at `:27`, `AppendChoices` `:155`, `AppendText` `:180` [V]; `ConversationManager.cs:20,231` [V]; actions registered at `ConversationActions.cs:299-302` [V] | The carriage attaches to the same capability tag and the same two integration points. Reaches existing keepers in **old saves** because it keys on an instance tag, not a Part added at generation |
| Concord keepers exist at Cinderhold (office, `CinderholdCompositionPlan.cs:51`), the Last Counter (supply post, `LastCounterCompositionBuilder.cs:59-63`, legacy stamp `LandmarkBuilder.cs:981`) and 17 wilderness waystations (`LandmarkBuilder.cs:619-640`, `Chance = 20`) | [V] | Endpoints: Cinderhold and the Last Counter. Waystation keepers keep the ledger but are **not** carriage posts in V1 (portable; no authored place identity) |
| Sill gets a `ConcordFactor` **only** when the well is Concord-contracted: `PlaceConditions.json` `ConcordContract.MainWell` has `Arrivals: [ConcordFactor x1]`, applied by `PlaceConsequences.OnZoneEntered` on a real arrival with stable ids | `PlaceConditions.json` [V]; `PlaceConsequences.cs:49-90` (`OnZoneEntered`; arrivals at `:75`) [V] | A third endpoint made by a **player choice** (Concord contract vs hand repair). Corrects `settlement-sill.md:91`, which says no Concord entity can exist in Sill |
| Money without a transaction goes through `Ledger`; `TryTransfer(from,to,amount,reason,out refusal)` conserves the total, refuses on short purse or overflow, and records `trade` diag | `Ledger.cs` [V] | Fee payment to the present keeper; precedent `ConcordAccount.Borrow/Repay` |
| Account on the player's own properties rides the save graph; the same pattern for a state Part: `EnsureWorldMapPart(player)` lazily adds a Part | `ConcordAccount.cs:15` [V]; `WorldMapTraversal.cs:283` [V] | `ConsignmentPart` added lazily on first dispatch; absent in old saves |
| An `Entity`-typed or `IList` public field of a saved Part is serialised, and `WriteEntityReference` queues the referenced entity's **body**, so an entity reachable only through such a field is saved whole | `SaveSystem.cs:112-124`, `:2141-2160` (`CanSerializeType`: `Entity`, `IList`) [V] | **Detached goods stay real entities** (identity, count, state, properties) inside the ledger. Nothing is minted at claim, so duplication is structurally impossible; the round trip itself is [U] until RED R0.2 |
| World time is one integer clock: `WorldClock.CurrentTick`, `DayLengthTicks = 1200`; a world-map step costs 10 ticks | `WorldClock.cs:47` [V]; `WorldMapTraversal.cs:71` comment [V]; due-date arithmetic precedent `ConcordAccount.cs:32,144` [V] | Arrival is `now >= DueTick`, computed at use. No reconciler, no per-turn code |
| Known-place gate: `WorldMap.Visited[,]`, `IsVisited`, saved | `WorldMap.cs:102-137` [V]; `SaveSystem.cs:1382,1405` [V] | A destination must be a visited authored place |
| Carry-vs-pay precedent for the same fiction: `tally.account` offers "carry the notice to Sill" or "settle at Tally, 25 drams" | `Concerns/Tally.json` [V] | Shows the project already treats carrying and paying as two valid paths; the carriage must not replace carrying |
| Finite custody conventions: `InventoryTransferSnapshot` capture/apply/restore and `OwnedPayment` (owned, unequipped, consumable) | `RegionalRequestPart.cs` Deliver [V]; `RegionalSituations.cs:278-284` [V] | Rollback and the ownership clause of admission |
| Distance pricing exists for salt (`RegionalValue`, 0.5 to 2.0 by distance from pans); it is the canon reason Concord caravans exist | `RegionalValue.cs` [V] | An arbitrage hazard the carriage must close (section 4.3) |

### 2.2 What the codebase does not give

- No cross-zone consignment, no dispatch state, no courier: `grep` for classes named `Consignment*`, `Carriage*`, `Dispatch*` in `Assets/Scripts` and `Assets/Tests` finds none [V]. `RegionalSituations` has five authored deliveries where the **player** is the courier (`RegionalSituations.cs:42-53` [V]).
- No NPC carries anything across zones; creatures leave the scheduler when the player leaves a zone (spec C11, [R]). So the "real arrangement" cannot be a walking NPC.
- No second node of any kind owned by the player; `KitchenBatchPart` is zone-pinned (`ZoneID`, `StationX/Y`, `EscrowX/Y` fields; `KitchenBatchPart.cs:21-24` [V]) and the S1 hygiene lint forbids that shape for new Parts, so it cannot be the model.
- No remote read of an unloaded zone's owners by design: `SpreadDiscoveryReports` is "historical reports from frozen eligible addresses, never remote owners" (`SpreadDiscoveryReports.cs:9-10` [V]). V1 keeps that rule: it reads no zone it is not standing in.

### 2.3 Corrections to earlier premises

| Premise | Verdict | Evidence |
|---|---|---|
| "Sill has no Concord entity; ConcordFactor is spawned only in Cinderhold and the Last Counter stamp" (`settlement-sill.md:91`) | Incomplete: Sill gains a Factor by data when `ConcordContract.MainWell` holds | `PlaceConditions.json` Sill row [V]; `PlaceConsequences.cs:75-89` [V] |
| "Tally has no keeper (test pin)" (`settlement-tally.md`) | The pin forbids four blueprints only: `ConcordFactor`, `FilerClerk`, `CurationSorter`, `LastCounterSign`. `SaccharineEnvoy` is not in the list. Whether Tally should get a keeper is still an authoring decision (Counter-as-person is protected), not a test fact | `TallyCompositionTests.cs` `LateGoodsAreNativeAndDoNotCreateForeignQuestGivers` [V] |
| `SettlementManager.HasCondition` / `GetSite` are pure reads (a report could call them) | False: both call `GetOrCreateSettlement`, which **creates** state for an unknown settlement and backfills sites. A pure read must use `GetAllSettlementsSnapshot().TryGetValue` or `TryGetExistingSite` | `SettlementManager.cs:37-67,77-84,407-411` [V]. Decisive in rejecting the bulletin (section 8) |
| "A relay is blocked until the second owned node exists" (many dossiers) | True for **connecting owned places**; not true for a **carriage between existing Concord posts**, which needs no owned node | section 4 |
| `RegionalRequestPart`-style NPC Parts retrofit old saves | False: it is bound only in `OnZoneGenerated` ("Explicit replay on a cached/restored graph is a no-op") | `RegionalSituations.cs:74-78` (`OnZoneGenerated`) [V] |

---

## 3. Purpose audit (honest)

The question: what would a player do with this machine that they cannot do, or cannot do as well, today?

| Candidate purpose | What the shipped world offers instead | Verdict |
|---|---|---|
| **Receive remote reports** (state of a place) | Remote state is frozen or lazily reconciled on arrival; global condition state (`SettlementManager`) is already "heard" as ambient remarks, 4 percent per turn when a resident is near (`PlaceConsequences.cs` `ChancePercent = 4` [V]); the player's own repairs are their own knowledge | **No purpose.** A bulletin re-presents ambient news; relapse foresight for the player's own patches needs an in-world observer that does not exist (fiction-incoherent) and is a journal feature |
| **Contact known people** | Conversation needs presence; offscreen NPC state is not simulated | **No purpose; also hidden-actor and R7/R9 risk** |
| **Arrange deliveries** (goods) | The player carries; five authored errands exist; Concord carriage is canon but has no game form | **Real, narrow purpose** (below) |
| **Connect scattered owned places** | No owned places exist | **Blocked on home/base** (S19) and timed jobs (S16) |
| **Coverage by elevation/interference/repair** | No signal to cover | Deferred; the only honest coverage rule today is "who keeps a ledger post" |

The real purpose, stated plainly. At the Last Counter the player is at "the final-supply station before Urqu-bleeding deep places"; the post issues a weapon-component consignment, its own sign points to "a forge", and the zone has none (`settlement-last-counter.md:122` [R]). Alternatives that already work: pack assembly, M3c away-from-station processing, the Peat Works rig about 11-12 map cells north, the Cinderhold forge at (6,6), local fire clay from the Merchant [R]. Carriage adds one path: **do the forge work at Cinderhold, send the finished goods to the Last Counter, set out east lighter**. It also lets a returning expedition leave heavy salvage at the last post instead of hauling it. These are real but modest uses, and the four existing alternatives make carriage a convenience, never a requirement. That is the intended shape ("ordinary convenience stays").

If the owner does not accept this as a purpose (decision D1), the honest status is: **no purpose today; park the relay behind S16 timed jobs and S19 player-installed nodes, which are what give a relay something to report and somewhere to deliver.**

---

## 4. V1 definition: "Concord carriage" at existing post-keepers

### 4.1 Names and fiction (all drafts; each needs an intent card and the blind gate, R10/L17)

- **In-world name:** the **carriage** (the Concord's contract of carriage), arranged at a post's **dispatch desk**, i.e. with the post-keeper. Canon words: "carriage", "post", "dispatch desk", "consignment", statuses **departed** and **arrived** (`06_ConcordCarriageContract.md` [V]; `lore.md:206`, naming bank row 11 [V]). Not "relay", "signal", "network", "transmitter": those are design words (L16).
- Draft keeper lines in the Concord voice (brisk, priced, rate-tag close; `lore.md` section 8 voice summary [V]):
  - offer (choice text carries the full terms): "[Post] Entrust 6 x salvaged timber to the carriage for the Last Counter: 21 drams. Departs now. Arrival is entered in about a day. That's the rate."
  - status in greeting: "Entered to your name: 6 x salvaged timber, Cinderhold to the Last Counter, departed. Arrived is not entered yet."
  - not due: "Departed. Arrived is a different form, and it is not entered."
  - salt refusal: "Salt goes in the Concord's own caravans. It is not entered on a private consignment."
  - default refusal (echoing shipped copy, `ConcordAccount.cs:68`): "Your name is in default. The Concord carries nothing for it until it is settled."
  - recall: "Taken back from the carriage. The rate stays entered."
- The Last Counter's limit is the whole honest-limits story: V1 offers **no** route past it, and says nothing about what eats deliveries there (L1, R1). The boundary is enforced by data (section 4.3), not by prose.

### 4.2 Parts and fixtures

V1 adds **no world object, no blueprint and no placement.** It adds:

```csharp
namespace CavesOfOoo.Core   // saved type name is assembly-qualified and permanent; Core only (spec section 1 item 17)
{
    public sealed class ConsignmentPart : Part                    // attached lazily to the Player, like WorldMapPart
    {
        public override string Name => "Consignment";
        public List<Entity> Goods = new List<Entity>();           // detached real entities; saved whole via entity references
        public string ConsignmentId = "";                         // "c:<originPost>:<departTick>"
        public string OriginPost = "", DestinationPost = "";      // authored place names, never ZoneID/coordinates (S1 hygiene)
        public int DepartTick, DueTick, Cells, Units, Weight, Fee;
        public bool Open => Goods != null && Goods.Count > 0;
    }
    public static class CarriageService   // static, ConcordAccount-shaped: Quote, TryDispatch, TryClaim, TryRecall, Statement, AppendChoices, AppendText
    internal static class CarriageRoutes  // data: Content/Data/CarriageRoutes.json, per-row dropping validator
    internal static class CarriageAdmission // data: Content/Data/CarriageAdmission.json, canon-cited refusals, total census test
}
```

Capability resolution (adapter pattern, spec section 2 R1 two-consumer): `DispatchPosts.Resolve(Entity keeper)` returns the post for a keeper by **tag** (`ConcordAccount.KeepsLedger`) and the keeper's authored place (`PlaceSiteCatalog.PlaceAt(zoneId)`, `PlaceSiteCatalog.cs:54-66` [V]). A future `DispatchPostPart` (a placed desk, a base node) is the same interface with a different provider; it is **not** built in V1 (two-consumer rule: no second consumer yet).

Hygiene: new fields are public, default-constructible, none named `ZoneID`/`StationX`/`*ReceiverID`; `ConsignmentPart` enters the S1 golden as an addition. `FormatVersion` stays 8. An old save has no such Part and loads as "no consignment".

### 4.3 Rules

R-1. **Who can offer it.** A living, non-hostile, visible conversation partner whose entity carries `ConcordLedger` **and** who stands in an authored place that is the `From` of at least one valid route row. The conversation itself guarantees presence, which removes the operator-presence rule that service terms (C7) would otherwise need. Waystation keepers fail the place test and offer only the existing ledger choices.

R-2. **Routes are data** (`CarriageRoutes.json`, per-row dropping like the proposed `InstallationRegistry`, so one bad optional row never cancels the others):

```json
{ "Version": 1,
  "Guaranteed": ["Cinderhold", "Sill", "the Last Counter"],
  "Routes": [
    { "Id": "cinderhold-last-counter", "A": "Cinderhold", "B": "the Last Counter",
      "Basis": "Concord posts on one ledger; the Last Counter is where the guarantee ends (Lore/Codex/06_ConcordCarriageContract.md clause 3)" },
    { "Id": "cinderhold-sill", "A": "Cinderhold", "B": "Sill",
      "Basis": "Concord contract at Sill brings a Factor (PlaceConditions.json ConcordContract.MainWell)" } ] }
```

Validation (each its own RED case with a valid neighbour): malformed token; duplicate id; `A == B`; a place that is not an authored place (`WorldMapAuthoring.Places`); a place **not** in `Guaranteed` (this is the honest-limits rule as data: no route names anything past the Last Counter); a place in the canon-exempt set (the First Tent, the Quiet's Door, the Deepest Cathedral, Ginmere, Olderdeep, Spivenor, Stillleaf, Wellmeet); empty `Basis`; `Cells` outside 1..16 (derived, not authored). Distance is the Chebyshev distance of the two places' authored cells (same measure as `RegionalValue.DistanceToFlats`): Cinderhold (6,6) to the Last Counter (18,18) is 12; Cinderhold to Sill (10,10) is 4 [V: `WorldMapAuthoring.cs:208-226`]. A route is **live** only when a living ledger-keeper stands at the origin now (always true, we are talking to them) and the destination is `WorldMap.IsVisited` (the player knows the place).

R-3. **One open consignment per player** (`ConcordAccount`'s "one debt at a time" shape). One consignment is **one carried stack**: all `n` units of one blueprint, with `n <= 12` and total weight `<= 40` (proposal; section 4.7). A stack over the caps is refused, not split. Multi-stack consignments (a physical crate UI) are V2.

R-4. **Admission** (`CarriageAdmission`): the item must be *inert while stored* and *ordinary stock*. This is the same class of decision CLAUDE.md gates ("canonical weapon-payload/equipped-input/save policies require independent design critique before RED"): **R1 below must not start before that critique.** Proposed policy, each line a RED case with a counter-check:

| Refused | Reason (source) |
|---|---|
| equipped, or not owned/consumable (`OwnedPayment`, `RegionalSituations.cs:278` [V]) | custody |
| carries `ConcernCargo`, a `RegionalCargoPart`, a `RentalPart`, or is a concern/quest item | an errand's cargo is a person's undertaking; R9; Tine caution that relays must not auto-complete errands (`settlement-tine.md:103` [R]) |
| coins (`NoTrade`), and any good carrying `TradeSalt` | the dram faucet audit (EC2) and the EC5 caravan margin: carrying salt inland pays and is the Concord's own business (`RegionalValue.cs` [V]); a private carriage must not turn that into a riskless loop |
| liquids and any vessel with contents; any container with contents | M1a/M2a finite liquid handling is not a carriage concern; contents are separate entities with their own state |
| bodies and specimens, plaques/Memory-Marble/Tepuibone/Mute-Stone, ancestral bone, anything Gin-Frog derived, documents/codex/books | clause 5 ("bodies not sealed to Curation intake standard", unboxed spores), name-holding materials (R8, L8), Mystery #8 (L1), and "any relayed text that is written down is read" (`lore.md:199` [V], L6) |
| any item with a Part that reacts to elapsed time or turns while it exists (fuel, burning, perishable, growing, kitchen/cooking state) | detached goods are not visited by per-turn systems [U]; freezing a perishable would be an exploit. The allowlist is built by a **total census** over `Objects.json` item blueprints (every blueprint classified admitted or refused with a code) |
| conjured/temporary things | R5: everyday magic is temporary |

R-5. **Fee and payee.** `Fee = Base + ceil(Weight * Cells / Divisor)` drams, paid by `Ledger.TryTransfer(player, keeper, Fee, "concord-carriage")` to the keeper the player is talking to (money conserved; spec C7 default "paid to the present operator"; `Ledger.cs` [V]). Starting constants `Base = 4`, `Divisor = 6` are **tuning, not design**: the bench (section 6, R6) must show two inequalities hold for every admissible blueprint on every live route: (a) `Fee >= max(0, destinationSellPrice - originBuyPrice)` so no route creates trade profit; (b) `Fee <= 30 percent` of the cheapest admissible item's own buy price at the origin for a full legal stack, so the service is usable. If both cannot hold, the cap (R-3) moves, not the inequality. The alternative payee (an EC7-style named sink, `Ledger.TryDebit(..., "concord-carriage")`, precedent `ConcordAccount.cs:97`, and the credit fee as a named net sink at `:17`) is owner decision D2 and goes through the same S10 critique the spec already requires.

R-6. **Time.** `DueTick = now + Cells * 120`. Cinderhold to the Last Counter: 1440 ticks (1.2 days); to Sill: 480. The rate is deliberately **slower than walking** (a world-map step costs 10 ticks plus in-zone walking, so a human crossing is a few hundred ticks [I]); carriage trades time for weight, exposure and free hands, never speed. Arrival is **derived** at use time: `Arrived = (now >= DueTick)`. There is no reconciler, no per-turn work and no away simulation.

R-7. **Claim.** Only at the **destination** post, only when `Arrived`, only by the player who holds the ledger, only at a keeper that passes R-1. The same entity objects are re-attached to the player's pack through the ordinary add path (merging exactly as an ordinary pickup does). If the pack cannot take them (weight/space), the claim is **blocked, the goods stay entered**, nothing is destroyed or spilled (spec C9 "a full receiver blocks").

R-8. **Recall.** Only at the **origin** post, any time before the claim; returns the same entities to the pack under the same blocked-not-spilled rule; the fee stays with the keeper (matches "interruption returns what remains and keeps the fee", spec C9). Recall at the destination is refused (anti-teleport: deposit, then recall at B, would deliver instantly for the fee).

R-9. **Refusals tied to existing systems.** In default on the Concord account: no dispatch (`ConcordAccount.InDefault`). Keeper hostile or dead: no conversation, so no dispatch; a dead destination keeper leaves goods entered and recallable at the origin (canon: "indefinitely in transit", clause 4; never destroyed). The closure ledger is **never written**: the player's undertaking ended with payment; the Concord holds the obligation. A spoken end for a standing arrangement is therefore not needed in V1 (R9 cleanly satisfied by omission); recall is the voluntary end. Owner decision D6 records the choice.

### 4.4 Player verbs (all are conversation choices on a ledger keeper, `nodeId == "Start"`, like `ConcordAccount.AppendChoices`)

| Verb | Offered when | Effect |
|---|---|---|
| `[Post] Entrust <n> x <name> to the carriage for <Destination>: <fee> drams, due in about <d> day(s)` | R-1..R-5 all pass for that stack and route; one choice per (eligible stack, live route), at most 6 stacks listed, heaviest first | dispatch |
| `[Post] Claim the consignment from <Origin>` | the player holds a consignment whose destination is **this** post and `Arrived` | claim |
| `[Post] Take the consignment back (the rate stays entered)` | the player holds a consignment whose origin is **this** post | recall |
| (greeting text, always) status line | any ledger keeper, a consignment held | read-only status |

"A choice never lies" (`ConcordAccount.cs:153` [V]): a choice is shown only if executing it now would succeed; when something is refused for a reason the player can fix or should know, the greeting text states it in one sentence instead (open consignment, in default, pack too heavy to claim, not yet arrived). Nothing is offered for waystations or for any place with no route.

### 4.5 Inputs, outputs, units

| | Unit | Rule |
|---|---|---|
| Input goods | units of one blueprint (stack count) and carried weight (`InventoryPart.GetItemWeight`) | one stack; `<= 12` units; `<= 40` weight (proposal) |
| Fee | drams (integer) | formula in R-5; moved to the keeper's purse |
| Time | world ticks (1200 per day) | `Cells * 120` |
| Output goods | the **same entities**, same count, identity, state and properties | re-attached at the destination; nothing created |
| Output record | `ConsignmentPart` cleared on claim/recall | |

Conservation law, testable: for any sequence of dispatch, save, reload, claim, recall (and any single injected failure), the multiset of entity ids held by player pack plus ledger plus anywhere else is unchanged, and the total drams of player plus keeper is unchanged.

### 4.6 Loss and waste

None by design. Fees are a transfer, not a burn. No goods are ever lost inside the guaranteed route, so no RNG exists (`RngUntouched` is a pin). Goods that cannot be delivered remain **entered** (canon "indefinitely in transit") and recallable at the origin. A refused item stays in the pack untouched. There is no waste destination to define; saying so is the honest answer, and it is why the admission table must be strict (the only way to lose value is to admit something that decays).

### 4.7 Cost

Action/time: a conversation choice, no world turn (like the account statement). Fee: R-5. Capacity: R-3. Wait: R-6. The constants (12 units, 40 weight, base 4, divisor 6, 120 ticks per cell) are starting values to be tuned by the self-auditing bench (CLAUDE.md "Deterministic self-auditing scenarios"); they are not design commitments. The cap is set well below a character's carry capacity (`Str * 15 = 240` at Str 16 [per project memory]) so the service is a staging convenience, not a bulk freight route.

### 4.8 Preview and readers

The choice text **is** the preview (terms carried in the line, as the Concord credit choices do): count, name, destination, fee, due. `CarriageService.Quote(...)` is a pure function returning `{fee, cells, due, units, weight, refusalCode}`; the choice text, the commit and the tests all call it, and commit re-quotes and refuses on any difference (`BeforeCommit` shape; no modifier channel exists yet, so this is a pin against future drift). The greeting reads the status (`AppendText`, same as `ConcordAccount.AppendText`, `ConversationManager.cs:20` [V]). V1 adds **no** journal entry and no inventory-UI entry: the post's greeting is the only place the consignment is shown (honest limit). The spec's S5 reader is not needed because there is no placed object to examine.

### 4.9 Blocked, failed, interrupted, saved states

| Situation | Outcome | Diag (`trade`) | Player sees |
|---|---|---|---|
| second consignment while one is open | no send choices | `CarriageRefused open-consignment` | greeting: one at a time |
| in default | refused | `... in-default` | default line |
| destination not visited / no route | choice absent | `... not-visited` / `no-route` | nothing |
| item refused by admission | choice absent for that stack | `... inadmissible:<code>` | nothing (or the one-line reason for salt/documents) |
| purse short / keeper purse would overflow | refused, nothing moved | `... short-purse` / `payee-overflow` | the fee is in the choice text; short purse hides it |
| exception after goods left the pack | compensating rollback restores goods, drams, ledger | `... rolled-back` | no change |
| claim before due | choice absent; status says not arrived | `... not-due` | "Arrived is not entered" |
| claim at wrong post / at origin | choice absent | `... wrong-post` | nothing |
| pack cannot take the goods | claim refused, goods stay entered | `... pack-full` | one sentence |
| recall at destination | choice absent | `... wrong-post` | nothing |
| destination keeper dead or hostile | claim unavailable; recall still works at origin | `... no-keeper` | status unchanged |
| save / F5 / fresh Continue mid-transit | ledger and goods identical | none | status identical |
| old save with no `ConsignmentPart` | no consignment; first dispatch adds the Part | none | normal |
| quit while a choice is open | nothing committed (all-or-nothing, validate-all-then-apply) | none | nothing |

### 4.10 Alternatives the player keeps (all supported, none degraded)

1. **Carry it yourself.** Free in drams, full control, keeps the goods with you; costs weight, exposure and attention. Nothing about hauling, drag rules or pack weight changes (ordinary convenience untouched).
2. **Skip the shipping and solve the problem at the destination.** For the Last Counter weapon kit: pack assembly, M3c away-from-station processing, the Peat Works rig, buy fire clay from the local Merchant (`settlement-last-counter.md:122` [R]).
3. **Use the existing Concord terms in other forms.** The `tally.account` pattern (carry the letter or pay) stays exactly as authored; carriage does not touch concerns.
4. **Leave it alone.** No system in the world degrades if the carriage is never used; no hand-repair, trade or travel is made costlier to create demand for it.

Anti-dominance guard (R-5 inequality a): the service can never be the strictly better way to make money, and R-6 means it is never the faster way to move.

### 4.11 Consequences outside the carriage menu (cross-system)

- **Encumbrance and exposure:** goods in the ledger leave the carried weight (`InventoryPart.GetCarriedWeight`), changing what the player can pick up on the way and how heavy they travel through hazardous country.
- **Concord account (EC7):** a name in default is refused carriage, so the account matters beyond credit; prompt payers are unaffected.
- **Economy (EC1/EC2/EC5):** fee moves drams into the keeper's purse (conserved, no new faucet or sink); salt is refused so the caravan margin stays the Concord's. The bench proves no profit loop.
- **Repair path choice makes the network:** contracting Sill's well to the Concord (`ConcordContract.MainWell`) brings a Factor, which makes **Sill a carriage post**; repairing by hand (`RebuiltByHand.MainWell`) does not (`PlaceConditions.json` [V]). A player's repair choice therefore changes which places the carriage can reach, using only existing data. Both Sill outcomes already carry price and news consequences (0.97 vs 1.04, Heard lines); carriage adds a third.
- **The Last Counter's authored problem:** forge work at Cinderhold feeds an expedition that begins at the Last Counter.
- **Closure ledger and concerns:** untouched by design (pinned).

### 4.12 Operators and NPC roles

Shipped and sufficient: `ConcordFactor` (Cinderhold; Sill on contract) and `SaccharineEnvoy` (the Last Counter). No new NPC. Canon names three roles the code lacks (Clerk, Broker, Caravan-Master; `lore.md:356` [V]); none is needed in V1 and none is added. Gantry's registrar and wayboard, and a Tally keeper, are V2 candidates (section 8). Operators are resolved by tag at conversation time, never by display name (LocalPeople renames residents).

### 4.13 Placement, retention, census

- **Placement: none.** No object is added, so none of S6/S7/S8/S24 and none of the relapse-regeneration or fresh-only limits (spec C2, C3) apply. The conversation offer works in **existing saves** wherever a tagged keeper already stands [U: R0.5 pins that the tag survives save/load].
- **"A machine in every town" is the wrong census for the relay.** It is a network whose nodes are ledger posts. Census classification (proposal; each needs owner/lore sign-off): **Installed** Cinderhold, the Last Counter. **Conditional** Sill (contract). **Pending, named V2/V3 owner** Tally (keeper decision; hub), Gantry (capability on the registry desk/wayboard, "strong" in `settlement-gantry.md:185` [R]), Sumphold (three-chunk circuit, `settlement-sumphold.md:160` [R]), Tine, Quillhold, Marrowstye and the Salt-Vault (docket idiom, non-custodial only), the Drowned Ledger (reports only, never bodies; by-person carriage must remain), Morrowfast (consent state), Lampwell (alarm-jar idiom). **Exempt with canon source** the First Tent (monument; R7; `settlement-first-tent.md:215` [R]), the Quiet's Door (forbidden; `settlement-quiets-door.md:126` [R]), the Deepest Cathedral (Choir; nothing to dispatch), Ginmere, Olderdeep, Spivenor, Stillleaf, Wellmeet (Tent-Right: dram-priced service forbidden, L3), Posy (Concord clause 5; no stated use), Slip (poor now; unreliable-channel V3). The census ratchet counts these classes; "every town" is satisfied by classification, never by forcing the machine where canon forbids it.
- **Retention:** the state lives on the player, so there is nothing to retain; no `InstallationMarkerPart` is added.

### 4.14 Home/base hooks

- `ConsignmentPart` keys endpoints by string **post identity** (authored place today; `player:<entityId>` later), never zone ids or positions, so a player-installed post can be an endpoint without touching the saved shape (spec section 9 hook 4).
- The `DispatchPosts.Resolve` seam is where a placed `DispatchPostPart` (with `InstallationMarkerPart { Origin = "Player" }`, `StationReceipt`-style access) plugs in; route rows then name an installation id instead of a place.
- Tent-Right home idiom: carriage never carries guest-right, a cloth claim or water; a Tent-Right base is never a fee endpoint (R7, L3).
- Catacomb homes use the beetle-jar/cave-quiet idiom, not Concord carriage; Pale Curation/Recension homes are filed and need a spoken end (R9): all V2+.
- A base node supplies the missing "second owned node"; S16 jobs there supply something to report. Then the signal half becomes purposeful; not before.

### 4.15 Purpose test (an acceptance gate that can fail)

In actual play, fresh world (seed 704860946: Cinderhold's earned checkpoints exist), with a Cinderhold-forged weapon kit and a plausible pack:
1. Count what the player does to get the kit to the Last Counter by each path (carry; carriage; assemble there; Peat Works rig): actions, ticks, drams, risk events.
2. PASS only if **both**: carriage is chosen or chosen-worthy in at least one scripted staging case (heavy/many goods, hazardous crossing) **and** carrying or local assembly is chosen-worthy in at least one other (light goods, fast need). 
3. FAIL (record as not purposeful, park, do not inflate) if carriage is never preferable (fee too high, cap too low) or always preferable (fee trivial, slower than carrying is irrelevant).
The bench (R6) makes the arithmetic side machine-checkable; the play side is the user's judgment recorded in the evidence file.

---

## 5. What V1 needs that the spec lacks (and what it takes from the spec)

**Takes from the spec:** S1 only (saved-Part golden and hygiene lint for `ConsignmentPart`). Nothing from S3-S24: no declared action time (conversation, not menu rows), no station contract, no reader, no placement, no terms, no reconciler, no `WorkJobPart`, no receivers. The kitchen is not generalised (S16 would lift zone-pinned fields; the carriage state is a derived function of two ticks and needs no job engine). Adding S3-style disabled rows later is optional polish.

**Needs and the spec lacks (new contract C12, "consignment custody"):**
1. A saved Part holding **detached entities** as the custody record (`List<Entity>` on the player), with a total save round trip. [U: R0.2]
2. A **cross-zone claim-by-role** rule: the destination is resolved at use time by (place, tag), never by stored position (the same discipline as spec section 9 hook 4); the spec's `IProcessReceiver` (S17) is same-zone and does not cover it.
3. The **admission table** and its total census (inert while stored, canon refusals). Independent critique first (CLAUDE.md payload/equipped-input/save gate).
4. A **route table** with a canon boundary encoded as data (`Guaranteed`).
5. An **atomic two-party transfer outside an `InventoryTransaction`** (goods + fee), or a probe showing a conversation action can run a transaction (R0.3). Fallback is the ConcordAccount discipline: validate everything first, then apply in a fixed order with compensating rollback pinned by fault-injection tests.

Adversarial taxonomy surfaces touched (so the sweep gate applies): state atomicity and rollback, save/load reflection, cross-actor (keeper identity), anti-exploit (recall/claim teleport, free storage, salt/arbitrage), stacking (merge on claim), parser (route/admission JSON), boundary (zero weight, caps, due tick), diag dispatch.

---

## 6. Slices and RED-first tests

One cycle is one isolated-clone EditMode run (about 5 minutes). Every slice is RED first (a compile error counts), has counter-checks, diag pins, a cold-eye pass with both audit angles (taxonomy and Qud-parity-first is N/A: CoO-original), and the hypothesis-driven pass if the first finds nothing. Test files are `Assets/Tests/EditMode/Gameplay/Economy/Carriage*Tests.cs` plus `CarriageAdversarialTests.cs`, namespace `CavesOfOoo.Tests`, fixtures modelled on `ConcordAccountTests.cs` (factory from `Objects.json`, `FactionManager.Initialize`, `Diag.ResetAll`) [V]. Tests compare blueprint/id sets, never raw ids that shift.

| Slice | Content | Cycles | Depends |
|---|---|---|---|
| R0 | premise probes, green on HEAD (settle every [U]) | 1 | none |
| R1 | admission table + total census (**gated on critique**) | 2 | R0, S1 |
| R2 | route data + validator | 1 | S1 |
| R3 | `ConsignmentPart` + `CarriageService` dispatch + fee + rollback | 3 | R0, R1, R2 |
| R4 | claim, recall, derived arrival, blocked states | 2 | R3 |
| R5 | conversation presentation + chain-parity pins + Sill endpoint | 2 | R4 |
| R6 | save/old-save contract, self-auditing economy bench, actual play + purpose test | 1 + play | R5 |

Total about 12 cycles (about one hour of test time) plus play.

### R0 (pins; names are the contract)

`CarriagePremise_EntityReachableOnlyThroughPlayerPartListRoundTripsWithIdCountAndProperties` (the central [U]); `CarriagePremise_DetachedEntityIsNotVisitedByTurnOrTimeSystems`; `CarriagePremise_ConversationActionCanRunAnInventoryTransactionCommand` (RED means use the validate-then-apply fallback); `CarriagePremise_LedgerKeeperTagSurvivesSaveLoadOnAnOldKeeper`; `CarriagePremise_OnlyFactorAndEnvoyCarryTheLedgerTag`; `CarriagePremise_NoShippedClassOrBlueprintNamedConsignmentCarriageDispatch`; `CarriagePremise_SillMainWellContractArrivalPlacesAFactorOnce`; `CarriagePremise_TallyPinForbidsExactlyFourBlueprints`; `CarriagePremise_SettlementReadsAreNotPure` (documents the `GetOrCreateSettlement` side effect that rules out a bulletin); `CarriagePremise_ChebyshevCellsAre12And4`.

### R1 (admission; gated)

`Admission_EveryShippedItemBlueprintIsClassifiedAdmittedOrRefusedWithReason` (total census, fails on unclassified); `Admission_RefusesEquipped` / `..._AcceptsSameItemUnequipped` (counter); `RefusesConcernCargoProperty`; `RefusesRegionalCargo`; `RefusesRentalItem`; `RefusesCoins`; `RefusesTradeSaltGoods` with `AcceptsNonSaltGoodOfEqualWeight` (counter); `RefusesLiquidVesselAndLoadedContainer`; `RefusesBodiesSpecimensPlaquesNameHoldingStonesAndGinFrogGoods` (each named blueprint); `RefusesDocumentsAndCodex`; `RefusesAnyTimeBearingPart` (allowlist, with `NewPartTypeWithoutClassificationFailsTheCensus`); `RefusesStackOverUnitCap`; `RefusesOverWeightCap`; `ZeroOrNegativeCountsRefused`; `AdmissionTableLoadRejectsUnknownPartAndMissingCanonSource`.

### R2 (routes)

One case per error (duplicate id, malformed token, `A == B`, unknown place, place not in `Guaranteed`, canon-exempt place, empty basis) each with a valid neighbour; `OneInvalidRowDoesNotDropOthers`; `NoRouteReachesBeyondTheLastCounter` (derived from the Last Counter cell and `Guaranteed`); `DefaultRowsAreCinderholdLastCounterAndCinderholdSill`; `CellsDerivedFromAuthoredCoordinates`; `RouteIsLiveOnlyWhenDestinationVisited` with the unvisited counter.

### R3 (dispatch)

`Dispatch_MovesStackToLedgerAndChargesFeeAtomically`; `Dispatch_EntityIdentityStateAndCountPreserved`; `Dispatch_DramsConservedBetweenPlayerAndKeeper`; `Dispatch_RefusesSecondOpenConsignment` (counter: first succeeds); `RefusesWhileInDefault` (counter: settle then succeeds); `RefusesUnvisitedDestination`; `RefusesDestinationWithoutRoute`; `RefusesAtWaystationKeeper` (counter: Cinderhold Factor succeeds); `RefusesWhenKeeperDeadOrHostile`; `RefusesShortPurse`; `RefusesKeeperPurseOverflow`; `Dispatch_RollsBackWhenCallbackThrowsAfterGoodsRemoved`; `Dispatch_RollsBackWhenFeeTransferRefuses`; `Dispatch_RefusesWhenQuoteChangedBetweenPreviewAndCommit`; `Dispatch_NeverWritesTheClosureLedger`; `Dispatch_RngUntouched`; `DispatchDiagPinnedWithStableRefusalCodes` (one case per code).

### R4 (claim/recall)

`Claim_BeforeDueRefusedGoodsStayEntered` with boundary trio `due-1`, `due`, `due+1`; `Claim_AtDestinationAfterDueDeliversTheSameEntities`; `Claim_AtOriginRefused`; `Claim_AtWrongPostRefused`; `Claim_ByAnotherActorRefused`; `Claim_PackOverweightBlocksAndKeepsGoodsNeverSpillsOrDestroys`; `Claim_MergesIntoExistingStackLikeOrdinaryPickup`; `DoubleClaimImpossible`; `ClaimThenRecallImpossible`; `Recall_AtOriginReturnsGoodsKeepsFee`; `Recall_AtDestinationRefused` (anti-teleport); `Recall_AfterDueStillWorksAtOrigin`; `DestinationKeeperDeadGoodsStayEnteredAndRecallable`; `ArrivalIsDerivedFromTicksAndCarriageServiceHasNoPerTurnEntry` (source pin); `ClaimRollbackRestoresLedgerAndPack`.

### R5 (presentation)

`Choices_NeverLie_NoSendChoiceWhenRefused`; `Greeting_ReadsConsignmentStatusAtBothPosts`; `ChainParity_ExistingKeeperTextAndChoicesUnchangedWhenNoConsignment` (pins `ConversationManager.cs:20,231` order); `StatusVocabularyIsDepartedAndArrivedOnly` (source pin: no "en route", "lost", "safe", "signal", "network", "transmitter", "power", "charge", no Hush/Urqu intent); `OldSaveKeeperOffersCarriage`; `Sill_ConcordContractFactorBecomesAnEndpoint` with `Sill_RebuiltByHandHasNoEndpoint` (counter).

### R6 (save, economy, play)

`Save_InTransitGoodsRoundTripWithIdsStacksProperties`; `Save_OldSaveWithoutConsignmentPartLoadsAndIsIdle`; `Save_PartHasNoZoneIdOrPositionField` (hygiene lint); `SavedPartContract_AdditionOnly`; **`CarriageEconomyBench`**, a self-auditing scenario per CLAUDE.md (synthetic stimulus, snapshot/measure/restore, control row, loud precondition guards, one diag record per cell, `runId` stamp): matrix of admissible blueprints x live routes, asserting inequalities (a) and (b) of R-5, `NoRouteCreatesTradeProfit_ForAnyAdmissibleGood`, and `FeeNeverBelowRegionalMargin`; merge held until a live `diag_query` audit confirms the matrix. Actual play (section 10).

---

## 7. Canon landmines it must avoid (ids from `lore.md` section 7, rules from section 1)

| Id | How V1 stays clear |
|---|---|
| **L1 / R2** the Hush and what eats deliveries stay unresolved | No route past the Last Counter (data, `Guaranteed`); no text about the east; "indefinitely in transit" is used only for the dead-keeper case and explains nothing |
| **L2 / R1** no intent attributed to Urqu; no power from Urqu/Sari-Pyrite | No machine, no power, no signal; `StatusVocabularyIsDepartedAndArrivedOnly` forbids intent words |
| **L3 / R7** guest-right, cloth, oath, water gift not automated, relayed or priced | Tent-Right places are not endpoints; liquids refused; no guest or oath item admitted; carriage text never mentions hospitality |
| **L5 / L6 / L8** Curation, Recension, catacomb crimes | bodies, specimens, plaques, name-holding stones, ancestral bone, documents refused by canon-cited admission rows (clause 5; the Reader attends every inscription) |
| **L9** material collapse | goods keep identity; nothing converted; salt kept distinct and refused |
| **L11** no forecasting Urqu-bleed or exposing closure-ledger state | the closure ledger is never read or written; no gauge |
| **L15** Concord voice, free taboo | rate is stated, never "free" |
| **L16** technology nouns | player text uses carriage, post, dispatch desk, consignment, departed, arrived |
| **L17 / R10** unverified blind gate | every string here is a draft; the voice gate and intent card come before any copy ships |
| **R9** closure ledger needs a spoken end | V1 creates no undertaken act; recall is the voluntary end; D6 records the choice |
| Clause 5 "anything that sings; anything the Bower-Folk have asked after twice" | covered by refusals for unboxed spores and the Bower items the census flags; Posy is not an endpoint |
| `settlement-tine.md` caution | no errand or concern is ever auto-completed or closed by carriage (`Dispatch_NeverWritesTheClosureLedger`; `RefusesConcernCargoProperty`) |
| Tally: "do not give the Counter a machine verb" | Tally gets no keeper or verb in V1 |

---

## 8. Deferred pitch behaviours and rejected smaller versions

**Deferred (with the reason and the prerequisite):**

| Pitch behaviour | Why not in V1 | Prerequisite |
|---|---|---|
| Receive reports / exchange observations | remote state is frozen or lazily reconciled; nothing new to report; any observer would be invented | owned nodes with evolving state: S16 `WorkJobPart` + S19 installables |
| Contact known people | needs live remote conversation; R7/R9 and hidden-actor risks | none soon; probably never as remote speech |
| Connect your remote workshop with a settlement | no owned workshop exists | home/base (S19, `InstallationMarkerPart` player origin), `DispatchPostPart` |
| Coverage by elevation / interference / repaired infrastructure | nothing to cover yet. The one honest coverage rule today is "who keeps a ledger post"; Sill by contract is already a coverage-by-repair-choice case | owned nodes; a repair site that gates a link (PlaceSites row + physical fixture + canon decision) |
| Restore an old tower | the abandoned counters are east of the Last Counter (zones 19.18.0 and 19.19.0), i.e. in the unguaranteed zone; restoring one is the most interesting V3 and the most canon-sensitive (L1, L11). The 'tower' needs a physical fixture, a repair site, an unreliable-channel rule that never gives the Hush intent | owner canon decision; S19 |
| Gantry registry/wayboard as a node | capability Part on existing objects, "strong" fit, but R7 caution (guest-cloth pole must not be metered) and no keeper-tag path | `DispatchPostPart`, S7/S24 |
| Tally hub, waystation endpoints, more routes | Tally keeper is an authoring decision; waystations are portable posts with no place identity | owner decisions D5; route rows by installation id |
| Multi-stack consignments, a physical crate UI, multiple open consignments | smallest honest V1 is one stack; crate UI needs placement | V2 with S7/S24 and an `IProcessReceiver`-like claim |
| Fee by standing, insurance/underwriting, late or lost goods | economy tuning and canon decisions | after the bench and S10 critique |
| Slip's unreliable channel | "slippage limit" must never read as Urqu choosing (R1) | V3; owner canon decision |

**Rejected smaller versions, and why (so nobody re-proposes them):**
1. **Read-only "post bulletin"** (news and relapse foresight at a keeper). It re-presents ambient news (4 percent per turn today), adds no information the player lacks, needs an observer the fiction lacks, and a naive implementation corrupts state (`SettlementManager.HasCondition` creates settlements; `SettlementManager.cs:407-411` [V]). A fake service; rejected as padding.
2. **Instant delivery / mail teleport.** Violates "goods still travel through real arrangements and routes"; also a free remote stash.
3. **NPC courier.** NPCs do not act offscreen; would need an away simulation the project rules out (spec C9).
4. **Free carriage.** Free storage and trade arbitrage; and it would hollow the player-as-caravan economy (EC5).
5. **Relay completes or closes errands** (concern carry, regional request). Breaks R9 and the Tine caution.
6. **Zone-pinned custody (kitchen style).** The S1 lint forbids positional authority and a cross-zone claim cannot be zone-pinned.

---

## 9. Risks, unknowns, owner decisions

**Owner decisions (this dossier does not decide them):**

- D0. Lift "no new runtime changes" and acknowledge the admission critique gate (R1).
- D1. Accept Concord carriage between the authored Concord posts as the relay's first honest form; otherwise park the relay (section 3).
- D2. Fee payee: the present keeper via `Ledger.TryTransfer` (recommended, conserving) or an EC7-style named sink (`Ledger.TryDebit`); route through the S10 critique.
- D3. Admission policy (payload, equipped, save, time-bearing items). Critique before RED.
- D4. Tune constants only through the bench; do not hand-set.
- D5. Whether Tally gets a ledger keeper (separate from V1; the Counter must not get a machine verb).
- D6. Confirm "no closure-ledger write in V1" (the player's undertaking ends with payment; recall is the spoken end).
- D7. Canon check of the exempt set and the `Guaranteed` list.

**Risks (ranked):**
1. **Detached-entity custody.** If entities not in any zone or inventory are touched by some system, or the save graph does not write them, the central design needs the fallback (an in-world sealed escrow container at each post, i.e. placement and S7/S24). R0.1-R0.2 settle it in cycle 1; this is why R0 is first.
2. **Admission is a policy minefield** (equipped, payload, time-bearing, canon items). Mitigation: total census, critique first, strict refuse-by-default.
3. **Economy.** A frictionless carriage becomes a stash or arbitrage. Mitigation: R-5 inequalities, salt refusal, bench.
4. **Thin purpose.** Two default endpoints. Mitigation: the purpose test can fail; Sill-by-contract adds a third; V1 is honest about being modest.
5. **Hand-kept conversation chains** (`ConversationManager.cs:20,231`) gain another hand-edited decorator; parity pins limit blast radius; a decorator registry is out of scope.
6. **Voice debt.** Every string needs the blind gate; none has passed (L17).
7. **Keeper survivability** (a dead keeper strands goods until recall): mitigated by origin recall; [U] whether keepers can be killed in practice.
8. **Old-save tag assumption** [U]: R0.4.

**Unknowns settled by named tests:** whether a conversation action can drive an `InventoryTransaction` (R0.3); exact `Sill` arrival cell and that the arrival yields a talkable Factor (R0.7); whether the keepers in old saves keep the tag (R0.4).

---

## 10. Actual-play acceptance script (fresh world plus cached control)

Fresh seed 704860946 (Cinderhold earned checkpoints exist, `CLAUDE.md` graphics continuation notes):
1. Reach Cinderhold; talk to the Factor; confirm the existing ledger choices are unchanged (counter) and `[Post]` choices appear only for admissible stacks.
2. Entrust a stack to the Last Counter: the choice text states fee and due; pack weight drops by the stack weight; keeper purse rises by the fee; no turn passes.
3. Try to send a second stack, try salt, try a document: refusals as specified. Try to claim before due: no choice; status says not arrived.
4. F5, fresh Continue: ledger and status identical. Travel to the Last Counter (ordinary route); before due, status says departed; after due, claim: the same entities appear (check ids), weight returns, fee unchanged.
5. A second dispatch from the Last Counter back to Cinderhold; recall it before the claim; goods return, fee kept.
6. Sill: contract the well to the Concord path; confirm a Factor arrives and `[Post]` choices list Cinderhold; in a second world repair by hand and confirm none (counter).
7. **Cached-save control:** load an older save where Cinderhold is already generated: the Factor offers carriage (tag path); an older save lacking `ConsignmentPart` loads normally.
8. Purpose test (4.15), with the user's judgment recorded.
Evidence limits to state: CUA-only images, no packaged performance claim, Linux/Deck unexecuted, voice gate pending.

---

## 11. Verification record

Read this session ([V]): `Docs/MACHINE-ROSTER-VISION.md` (pitch), `Docs/MACHINE-SYSTEMS-DESIGN.md` (row 95, expansion 119), `Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md` (M6 and cross-zone notes), `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `mw/design/ARCHITECTURE-SPEC.md` (whole), `mw/lore.md` (M11, matrices, landmines, naming bank), selected settlement dossiers (relay rows for every settlement; Last Counter, Cinderhold, Sill, Tally details), `Lore/Codex/06_ConcordCarriageContract.md` (whole), `ConcordAccount.cs`, `Ledger.cs`, `ConversationManager.cs` (text chain and choice append), `ConversationActions.cs:299-302`, `RegionalRequestPart.cs`, `RegionalCargoPart.cs`, `RegionalSituations.cs` (definitions, authority, `OwnedPayment`), `RegionalSituationNotes.cs`, `RegionalGuidance.cs` and `SpreadDiscoveryReports.cs` (heads), `PlaceConsequences.cs`, `PlaceConditions.json` (all rows, arrivals, HeardIn), `PlaceSites.json`, `PlaceSiteCatalog.cs`, `SettlementManager.cs` (advance, conditions, snapshot), `SettlementState.cs`, `RepairableSiteState.cs`, `SettlementRepairDefinitions.cs`, `EconomyUpkeep.cs`, `RegionalValue.cs`, `KitchenBatchPart.cs` (fields), `InventoryTransaction.cs` (API), `ConcernItems.cs`, `Concerns/Tally.json`, `WorldClock.cs` constants, `WorldMapTraversal.cs` (cost note, `EnsureWorldMapPart`), `WorldMap.cs` (Visited), `WorldMapAuthoring.cs` (places), `SaveSystem.cs` (`WriteEntityReference`, `ReadEntityReference`, `CanSerializeType`, visited save), `LandmarkBuilder.cs` (waystation and Last Counter stamps), `TallyCompositionTests.cs` (pin), `ConcordAccountTests.cs` (fixture conventions), a python scan of `Objects.json` for the `ConcordLedger`/`ConcordCredit` tags, and a grep for `Consignment|Carriage|Dispatch` class names.

Not read ([U], each named by the test that settles it): the body of `InventorySystem.ExecuteCommand` and whether a conversation action may call it (R0.3); per-turn systems and whether any visit entities that are in no zone or inventory (R0.1); the save reader's full handling of entities reachable only through a Part field (R0.2); whether instance tags are written in entity bodies (R0.4); `PlaceConsequences.FindCell` placement of the arriving Sill Factor (R0.7); item blueprint list and weights for the admission census (R1); price functions for the bench (R6).

No test, build, Unity run or git write was performed.

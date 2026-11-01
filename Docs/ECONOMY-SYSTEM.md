# Economy as a first-class system — findings (research in progress)

**Status:** survey (§4) and proposed design (§5), 2026-11-01. Implementation starts with
the §5.3 verification sweep, then EC1.

**User direction (2026-11-01):** fill out the economy, formalize it, and make it a first-class
system. Start from existing design documents if there are any; otherwise brainstorm a system
that fits the game.

## 1. Existing documents

| Document | What it is | Authority |
|---|---|---|
| `Lore/History/08_MaterialCulture.md` §currency | The currency-trust map: which faction trusts which currency, and the stated texture ("money change meaning as they cross the world") | **Canon** (`Lore/` via `10_Bible.md`) |
| `Lore/Factions/04_SaccharineConcord.md` | The trading power: the honored debt, trade-posts, caravans, underwriting drams | **Canon** |
| `Docs/Design/Mechanics/TRADE_AND_ECONOMY.md` | A study of how Caves of Qud implements trade from the decompiled source: water drams, the Commerce value pipeline, Ego-based performance, buy/sell asymmetry, restocking, haggling and debt | Reference only, not a design for this game |
| `Docs/SHOPPING-PARITY.md` | The Qud-parity sweep of the shipped shopping stack, which it calls about 95% complete | Implemented-system record |
| `Docs/Design/PLAYER-SHOP.md` | A design study of letting the player open a shop; its own header says it is not committed to a build plan | Exploration |
| `Docs/WEAPON-RENTAL-SYSTEM.md` | Weapon rental priced in Ink, a separate wallet from drams | Implemented-system record |
| `Docs/Lore/Palimpsest_Lore_Compendium.md` | The superseded Palimpsest world | **Not** a design source (superseded) |

No existing document designs the economy as one system. The pieces are separate: shopping,
rental, inns, shrines, loot and the player shop.

## 2. What canon fixes

From `08_MaterialCulture.md`, the currency set of `00_Canon.md` Layer 6:

| Currency | Trusted by | Not trusted by | Character |
|---|---|---|---|
| Drams | the Concord, surface villages, most of the world | catacomb villagers (barter first), the Choir (no economy), Tent-Right (oath first) | the closest thing to universal; the Concord underwrites it |
| Ink | the Quartermaster, scribes, preservers | — | destroyed on use; the rental currency; the same substance as embalming ink |
| Lumen-hours | catacomb economies; the Concord as broker | surface dwellers | hyper-local; the Concord runs the futures market |
| Salt | Tent-Right, Pale Curation, preservers, the wasteland | — | the wasteland's reliable trade good; cheap near the flats, precious far from them; preservative and seasoning |
| Honored debt | the Concord internally | — | the Concord's true currency of trust; a Counter-signed contract outweighs any sum |

Two rules follow:
- **Cross-faction exchange is friction, and friction is content.** Drams are dead weight in
  the deep wasteland; the Choir sells nothing; Tent-Right gives water to a guest and never sells
  it.
- **The intended texture is money changing meaning as the player crosses the world.**

## 3. Tension to resolve in the design

The implemented trade uses water drams as coin, inherited from the Qud port. Canon says water
is the truest wealth in the Beating, and among Tent-Right it is given, never sold. A
first-class economy has to decide what a dram physically is in this world, and how a
dram-priced shop behaves in places that do not trust drams.

## 4. Survey of the implemented economy (started 2026-11-01)

**Shape.** Currency is an integer property on the entity: `Drams` (`TradeSystem.CURRENCY_PROP`)
and, separately, `Ink` (`RentalSystem.INK_PROP`). There is no currency type, no ledger and no
single transfer API.

**Pricing** (`TradeSystem`, a Qud port): buy is `ceil(Value / perf × faction × place)`, sell is
`floor(Value × perf / (faction × place))`, where perf comes from Ego (0.05–0.95), faction
standing gives 0.85–1.10, and `PlaceConsequences.PriceModifier` reflects a place's
condition. 272 of 761 blueprints carry a `Commerce` value; the median is 8 drams, the top is
240 (grimoires).

**Traders.** 33 blueprints carry `TraderPart` (stock table plus a starting purse of 30–160).
`TraderRestockSystem` tops purses up to a floor of 100 every 300 turns and re-rolls a shelf
below 3 units.

**Faucets and sinks are scattered.** At least eight services move drams by hand, each with its
own overflow guard:
- `LocalGatheringClaimPart`, `BotanicalInkDeskPart`, `SoddenPreparationPart`, `KitchenBatchPart`,
  `SecondExplorationActions` (fees paid to a worker);
- `ConversationActions`: `PayDrams` and `ContractPlaceRepair` (drams vanish: a sink with no
  payee), and `GiveDrams` (drams from nothing: a faucet with no payer);
- `MorrowfastExpedition` and `ClothClaimantPart` (rewards and gifts);
- `GoldCoin` pickups credit the purse.

**Findings to carry into the design:**

| # | Finding | Kind |
|---|---|---|
| E1 | `TraderPart.Apply` rolls opening stock with `Rng ?? new System.Random()`: shop stock is not reproducible from the world seed | real issue |
| E2 | `GiveDrams` adds without an overflow guard, unlike every fee service | real issue (minor) |
| E3 | Every fee service re-implements the same check-transfer-rollback; none emits a common diag record, so "where did my drams go?" has no query | design gap |
| E4 | `ChoirTendril` has a trader with `ChoirStock` and 70 drams; canon says the Choir has no economy and sells nothing | canon conflict |
| E5 | `PalimpsestEcho` trades with `EchoStock`; the Palimpsest world is superseded (`Docs/Lore/`) | lore drift |
| E6 | Water trades freely (`WellKeeperStock` sells waterskins and tonics). That fits the Spread; nothing yet stops a Tent-Right host from selling water, which canon forbids | canon guard missing |
| E7 | Salt has no trade identity: `PaleSaltVein` and `SaltCrust` exist, but nothing prices salt by distance from the flats ("cheap near the flats, precious far from them") | canon feature missing |
| E8 | Lumen-hours and honored debt have no implementation | canon feature missing |

Further survey notes:
- `SanctuaryPart` takes a 5-dram donation by writing the property directly, bypassing even
  `TradeSystem.SetDrams`.
- `RestAtInn` charges for a room through a conversation action.
- `PaleSalt` (Commerce 12) comes from `PaleSaltVein` harvests. The Tent-Right `SaltMaster`
  wants it for reputation (`WantsMineral`). Nothing prices it by place.
- Canon also lists **Stones** (the one store of value every faction honours, never spent
  lightly), **Reputation** and **Spirit-affinity**. The last two are already their own systems.

## 5. Design (proposed 2026-11-01)

**Goal.** Money is one observable system, and it means different things in different places,
as canon intends. Two halves:
- **The ledger:** every dram and drop of Ink moves through one atomic, diagnosed path, so the
  economy can be audited and balanced.
- **Meaning by place:** what a trader takes, and what a good is worth, depend on who they are
  and where they stand.

### 5.1 Pillars

1. **One ledger (`Ledger`).** Three operations, each with a reason string:
   - `TryTransfer(from, to, amount, currency, reason)` moves money between two parties;
   - `Credit(to, …)` is a named faucet (rewards, gifts, coins);
   - `Debit(from, …)` is a named sink (offerings, contracts, settlements).

   Each operation is atomic, overflow-safe, and refuses a negative or zero amount and a null
   party. Each emits a `trade` diag record: `Transferred`, `Credited` or `Debited`, or `Refused`
   with a reason. Every existing hand-rolled path migrates onto it, which fixes E2 and E3 and
   makes "where did my drams go?" a query.
2. **Who takes what (trust).** A trader's faction decides what it accepts, from the canon trust
   table:
   - **The Choir does not trade.** You can only give yourself; this fixes E4.
   - **Tent-Right is oath-first.** Water is given to a guest, never sold (E6). Drams are dead
     weight in the deep wasteland; salt and goods are the money.
   - Everyone else takes drams as now.
3. **Value by place.** Price depends on where a good comes from and where it is sold, starting
   with salt: cheap near the flats, precious far from them (E7). The same mechanism gives the
   Concord's caravans their reason to exist: carrying goods between regions pays.
4. **The Concord's account.** Concord standing is a ledger the player can read. At enough
   standing, a Factor extends credit: carry debt instead of drams, repay it, and default at a
   cost to standing (canon: "Concord credit — the ability to carry debt rather than drams").
   This is the honored debt made playable.
5. **Reproducible shelves.** Trader stock comes from the world seed and the trader's identity,
   not an unseeded generator (E1).

### 5.2 Sub-milestones (smallest blast radius first)

| # | Milestone | Fixes | Notes |
|---|---|---|---|
| EC1 | Reproducible trader stock | E1 | a seeded generator from the world seed and the trader's ID; restock rolls the same way |
| EC2 | The ledger, then migrate every path | E2, E3 | trade, fees, rewards, offerings, contracts, coins, inn; pins prove no path moves drams outside it |
| EC3 | The Choir does not trade | E4 | the conversation stays and the trade offer goes; decide whether its stock becomes an offering exchange |
| EC4 | Water is never sold by a Tent-Right host | E6 | the trade screen refuses water from Tent-Right; the gift path stays |
| EC5 | Salt priced by distance from the flats | E7 | a `RegionalValue` modifier in the price pipeline beside faction and place |
| EC6 | Tent-Right takes salt, not drams | — | trade-screen design after a sweep of `TradeUI` |
| EC7 | Concord credit at a Factor | E8 (partly) | borrow, repay, default; standing as an account |

Lumen-hours (catacomb) wait for the catacomb economy content. Stones stay a store of value
outside trade for now.

### 5.3 Verification sweep, before EC1

Read every call site in §4 and record each as a payer, payee, faucet or sink, with the line
reference. Confirm:
- the trade-screen flow (`TradeUI`, `InventoryTransaction` drams deltas);
- the restock timing;
- `GoldCoin` pickup;
- how conversations reach a trader;
- whether the canon trust table has data in `Factions.json`.

Log corrections before writing code.

### 5.4 Verification-sweep corrections (in progress)

| # | Premise in §4–§5 | What the code says | Consequence |
|---|---|---|---|
| C1 | No single transfer API exists | `InventoryTransaction` already batches currency deltas atomically, with overflow and insufficient-funds guards and `event/CurrencyDebitRejected` or `CurrencyCreditRejected` records; trade and pickup use it | EC2 builds the ledger on this path and moves the non-transactional call sites (fees, conversation actions, shrine, rewards) onto it; it does not add a parallel mechanism |
| C2 | Trader stock rolls are unseeded everywhere | Fixtures and `WorldTravellers` already scope `TraderPart.Rng`; only ordinary zone generation left it null | EC1 scopes a seed in `ZoneManager.GenerateZone`, only when no caller set one |
| E10 | (pre-existing failing pins) "resting at an inn with a malformed or negative cost is free" | **False premise, corrected.** `RestAtInn` charges only after a successful rest. In these fixtures the rest itself was refused (`invalid_rest_context`): `RestSystem` needs the actor in a zone and the running clock (`TurnManager.Active`), and the three pins (`BiomeHermitTests` ×1, `BiomePhaseBAdversarialTests` ×2) predate that gate, so they never reached the cost parser | fixtures repaired with a real rest context; the parser's fallback-to-10 and never-mint contracts are exercised again |
| E9 | (new) | A picked-up `GoldCoin` credits 5 drams (`PickupCommand`), but its `Commerce` value is 1, and `EnvoyStock` sells 3–9 of them | **Exploit:** buy a coin for about 3 drams, drop it, pick it up for 5, and repeat while the Envoy restocks. EC2 pins it RED first; the likely fix makes the coin's value match its credit |

## 6. EC1 — reproducible trader stock (2026-11-01)

**Invariant (player terms).** The same world has the same shops. A trader generated with a
zone stocks its shelf from the world seed and the zone.

**Change.** `ZoneManager.GenerateZone` scopes `TraderPart.Rng` to
`Random(WorldSeed ^ zone hash ^ 0x7135)` for the pipeline run, and only when no caller has set
one. Fixtures, `WorldTravellers` and the cellar, district and curation builders keep their own
generators. The scope is cleared in a `finally`, so creation outside a zone is unchanged. Restock
already seeds from the turn and the trader's ID.

**Tests** (`TraderStockDeterminismTests`):
- the same world gives the same shops: RED (the shelves differed), then GREEN;
- another zone or world stocks its own shelves (counter-check);
- a caller's generator is respected, and none leaks out.

**Found on the way: the audit benches were order-dependent.**
- `GameAuditAcquisitionBench`, `GameAuditTransferBench` and `GameAuditTransferTests` created a
  `Merchant` and cleared its shelf, but its authored loadout (a short sword and boots) arms it
  whenever an earlier fixture left `LoadoutPart.Factory` set. The merchant then carried 8 extra
  weight, and the capacity cases failed. Whether they failed depended on which fixtures ran first
  (they failed in the full run and in the trade-filter run, and passed alone).
- The benches now suppress both factories while staging the merchant, and refuse loudly if it
  still holds equipment. The transfer tests null `LoadoutPart.Factory` in `SetUp` and restore it.
- `Arena_MerchantIsTheBenchsOwnWhateverTraderStateWasLeftBehind` drives the leaked state
  directly. It was RED with the fix reverted.

## 7. EC2a — the ledger, and the first money bugs (2026-11-01)

**`Ledger`** (`Assets/Scripts/Gameplay/Economy/Ledger.cs`) moves drams outside a trade
transaction:
- `TryCredit(to, amount, source)` is a named faucet;
- `TryDebit(from, amount, sink)` is a named sink;
- `TryTransfer(from, to, amount, reason)` is a payment that conserves the total.

Each operation is all-or-nothing. It refuses a missing party, a non-positive amount, a purse
that cannot cover a debit, and a credit past `int.MaxValue`, and records `trade/Credited`,
`Debited`, `Transferred` or `LedgerRefused` (with `refusal`).

**Migrated in EC2a:**
- conversation rewards (`GiveDrams`), concern settlements (`PayDrams`) and the Concord repair
  contract (`ContractPlaceRepair`, including its refund on refusal);
- inn rooms (`RestAtInn`, sink `inn:<site>`) and the shrine offering (`SanctuaryPart`, which
  had bypassed even `TradeSystem.SetDrams`).

The fee parts (gathering claims, the botanical ink desk, Sodden preparation, kitchen batches,
second-exploration services) and the Morrowfast and cloth-claimant gifts move in EC2b.

**Bugs fixed, RED first** (`MoneySupplyTests`):
- **E2:** a conversation reward near the purse's ceiling wrapped to a negative, which the clamp
  turned into **zero**, wiping the purse. It is now refused whole.
- **E9:** buying a coin, dropping it and picking it up netted +2 drams (RED: 100 → 102), so
  money could be minted while the Envoy restocked.
  - **Fix:** coins are money, not merchandise. `GoldCoin` carries `NoTrade`, so the existing
    `CanBeTraded` veto refuses buying or selling one, including coins a trader carries in a
    loadout. `EnvoyStock` and `GnomeStock` no longer roll coins.
  - **Rejected alternative:** making a coin's trade value equal its credit (5). It inflated
    every reviewed loot-value band (`DensityLairRewardBudgetTests`, `DensityLootCensusTests`:
    12 pins), which is a balance re-review, not a bug fix.
- **E10:** the three inn pins were stale fixtures, not free rest (§5.4). They are repaired and
  now exercise the fallback-to-10 and never-mint contracts again.

**Contract tests** (`LedgerTests`, 13 cases; `MoneySupplyTests`, 4): faucet and sink naming, exact emptying, refusals
leave both purses whole, conservation, self-payment, non-positive amounts and missing parties
everywhere, and diagnostics never steering the money.

## 8. EC2b — the last direct purse writes (2026-11-01)

**Survey correction (C3).** §4 said at least eight services move drams by hand. That is half
wrong:
- The fee parts (gathering claims, the botanical ink desk, Sodden preparation, kitchen batches,
  second-exploration services) and regional request rewards already pay through
  `InventoryTransaction.DeferCurrencyTransfer` or `DeferCurrencyCredit`, which are atomic and
  guarded.
- Only three gameplay paths wrote a purse directly: the Morrowfast expedition reward, the
  cloth-claimant's gift, and the trader restock top-up.

**Change.** Those three now credit through the ledger, with the faucets `morrowfast-expedition`,
`guest-gift` and `trader-restock`.

**Pin.** `NoCodeWritesAPurseOutsideTheMoneyPaths` scans `Assets/Scripts` (outside scenarios) for
any purse write and allows only:
- the money paths: `TradeSystem`, `Ledger` and `InventoryTransaction`;
- three named initialisers: a trader's opening purse, a generated indexer's empty purse, and the
  bootstrap elder.

It was RED on `d9cb7db3a` (three files) and is GREEN now. A new direct write fails the pin and
names its file.

**Not yet observable:** a committed transactional transfer (a fee) records only its rejections,
not its success. A `trade/Transferred` record on transaction commit is follow-up work; existing
diag-count pins must be checked first.

## 9. EC3 — the Choir does not trade (2026-11-01)

**Canon.** `Lore/History/08_MaterialCulture.md` §II: "the Choir has no economy at all (you
cannot *buy* anything from the Choir — you can only give yourself)". The tendril's own
dialogue already says "The Choir gives freely, for what is ours is all of ours." The shop
contradicted both.

**First attempt, reverted.** Removing the tendril's `Trader` part broke three deliberate,
pinned designs (21 tests):
- the Groveland actor art treats that part as essential, so the tendril fell back to a glyph;
- the Choir cathedral composes tendrils with a renewing `ChoirStock` shelf;
- restock eligibility and the `Codex11` document source expect a stocked tendril.

**Change (kept).** The tendril keeps its parts, stock, purse and renewal. What changes is what
canon cares about: nothing on the shelf is for sale.
- `ChoirOffering.KeepsNoShop` (a speaker of the `RotChoir` faction):
  - `ConversationManager.CanTrade` returns false, so no "[Let's trade.]";
  - `TradeSystem.CanBeTraded` vetoes buying from or selling to a Choir trader, so no route
    around the window exists.
- **The offering** (conversation action `OfferToChoir`, from a new `NoTrade` node: asking what
  it would take for what it carries).
  - It costs a fifth of the walker's maximum health, never the last of it.
  - The tendril gives everything it currently holds.
  - The walker gains Choir regard (+5).
  - Refusals change nothing: a tendril holding nothing asks nothing (`nothing_to_give`), and a
    walker too weak to give is refused (`too_weak`).
  - The shelf renews through the existing restock, so a later visit can offer again.
  - Diag: `trade/ChoirOffering` or `ChoirOfferingRefused`.
- `Codex11`, which only `ChoirStock` supplies, is given now, not sold.

**Spore-Wedded villages** are a different matter: canon lets the player trade with them
(`01_RotChoir.md` §216). Only the Choir itself, through a tendril, keeps no shop.

## 10. EC4 — Tent-Right never sells water (2026-11-01)

**Finding.** No Tent-Right member trades: `TentRightHost` and `SaltMaster` have no `Trader`
part. Wellmeet and the First Tent hold free wells and no shops. Canon already holds, so EC4
is a pin, not new code. `TentRightNeverSellsWater` checks every Tent-Right blueprint: none
may stock or shelve water. Its counter-check confirms the detector sees the Well Keeper's
waterskins.

## 11. EC5 — salt is priced by distance from the flats (2026-11-01)

**Canon.** Salt is "over-abundant near salt-flats; precious far from them"; the Concord's power
is its logistics.

**Change.** `RegionalValue.Modifier(item, trader)` scales the value of a salt good (blueprints
tagged `TradeSalt`: `PaleSalt`) by the trader's world distance from the nearest salt pan
(Beating zones in the `SaltPan` formation):
- `0.5 + 0.15` per zone, capped at `2.0`, so it runs from 0.5 beside the flats to 2.0 ten zones
  away;
- 1 for anything else, or for a trader standing nowhere on the surface map.

It multiplies the good's own value in both `GetBuyPrice` and `GetSellPrice`, so a far trader
both charges more and pays more. That differs from the place modifier, which moves prices
against the player both ways.

**What it does to play.**
- A shrewd trader (high Ego) profits carrying salt inland. At performance 0.95, a pale salt
  bought beside the flats for 7 sells far away for 22.
- Nobody profits buying and selling in one place.
- `SaltMaster`'s mineral request is unchanged.

**Tests** (`RegionalValueTests`): near is cheaper than far both ways; other goods keep one
price (counter-check); the caravan margin exists and no loop exists in one place; the factor is
bounded and never falls with distance; an off-map trader stays neutral; and the world really
has flats and hinterland.

## 12. Barter and credit — design (2026-11-01)

**User direction (2026-11-01):** design the barter screen, then do EC6 and EC7.

### 12.1 Verification sweep (before code)

Read: `TradeUI`, `TradeSystem`, `InventoryTransaction`, `InventoryTransferSnapshot`,
`StackerPart.SplitStack`, `DropPartialCommand`, `TraderRestockSystem`, `TradeStockBuilder`,
`ConversationManager.RefreshVisibleChoices`, `RegionalGuidance.AppendChoices`, `PlayerReputation`,
`WorldClock`, `WorldMapAuthoring` (biomes and places), the Beating's landmark stamps, every
`SaccharineConcord` reputation source, `Lore/History/08_MaterialCulture.md` §II and
`Lore/Factions/04_SaccharineConcord.md`.

| # | Premise | What the code says | Consequence |
|---|---|---|---|
| C3 | "EC6: Tent-Right takes salt, not drams" | No Tent-Right member trades (EC4 finding). Canon's real rule is wider: "drams are dead weight in the deep wasteland (where salt and the oath matter)" | **Re-scoped.** What a trader takes depends on who they are and where they stand (§12.2) |
| C4 | (inventory) | Traders who stand in the Beating: caravanserai merchant camps (`Merchant` plus `Warden`), salt-walker hermits (`DesertHermit`), `GlassblownDrifter` at obelisks, `PalimpsestEcho` at archives, and `SaccharineEnvoy` at Concord waystations and the Last Counter | These are the traders the barter rule reaches; the Concord's envoys keep honouring drams |
| C5 | Salt is a trade good | `PaleSalt` (Commerce 12, stackable, weight 1) comes from `PaleSaltVein` harvests in salt-pan formations, and is also a weapon infusion (the dead hate it) | Salt has use value besides trade: "you can eat your money or embalm with it" |
| C6 | The trade screen can make change | `TradeUI` buys and sells whole stacks only; nothing trades part of a stack | Barter needs counts per line, so salt can be small change; partial lines split stacks inside the transaction (the `DropPartialCommand` pattern) |
| C7 | (new finding, F1) | EC3 made every RotChoir speaker shopless. The five named shrine keepers (Mogu, Grib, Nam, Sien, Sopp) are RotChoir with `GnomeStock` shelves, so their shelves became unreachable: no trade, and only the tendril's conversation offers the offering | **Real issue, fixed in EC6:** any Choir speaker who holds a shelf offers the offering (an injected choice, like `[Let's trade.]`) |
| C8 | "At enough standing, a Factor extends credit" | All authored Concord reputation sources sum to roughly +50; Liked is 50 | A line gated at Liked would be dead content. **Re-designed:** a stranger gets a small line, and honouring debts raises standing (canon: "gained by ... paying debts promptly"), so the line grows through use |
| C9 | (inventory) | One Factor exists (`ConcordFactor`, Cinderhold 6,6). `SaccharineEnvoy` keeps the posts at the Last Counter and the waystations | A Factor extends credit; any Concord post-keeper (Factor or Envoy) takes repayment: one ledger, many posts |
| C10 | (turn source) | `WorldClock.CurrentTick` reads `TurnManager.Active`; a day is 1,200 ticks; `InputHandler.HandleZoneTransition` already runs per-zone economy upkeep (`TraderRestockSystem.RestockZone`) | Due dates are ticks; defaults are reviewed on zone transitions and when a Concord ledger is opened, never per frame or per turn |

### 12.2 EC6 — who takes what (tender)

`Tender.For(trader)` decides what a trader accepts, in this order:

| Rule | Tender | Canon |
|---|---|---|
| The Choir | **None** (no shop; the offering) | "you can only give yourself" |
| The Concord | **Drams**, everywhere | "the Concord underwrites it" |
| Tent-Right, catacomb folk | **Barter** | "oath-first", "barter-first" |
| Anyone standing in the deep wasteland | **Barter** | "drams are dead weight in the deep wasteland" |
| Everyone else | **Drams** | "the closest thing to universal" |

**Deep wasteland:** a Beating world cell at least `DeepWastelandDepth` = 3 cells (Chebyshev)
from any other biome: 52 of the Beating's 107 cells. The frontier two cells wide, where Tally's
caravans run, still honours drams. Wellmeet and the First Tent are Tent-Right anyway.

### 12.3 The barter screen

A **deal** has two sides, each a list of lines (an item and a count):
- the player **gives** lines from their pack, each valued at its sell price;
- the player **takes** lines from the trader's shelf, each valued at its buy price.

Counts let a line hold part of a stack, so salt can be small change. Prices come from the
whole existing pipeline: Ego performance, faction standing, place condition and EC5's
regional value.

**Settlement:**
- **Barter tender.** The deal is accepted when what you give is worth at least what you take.
  No drams move. The trader keeps any surplus, and the screen says so before you offer.
- **Drams tender** (deal mode, `[D]` from the ordinary screen). The difference is settled in
  drams either way. The deal is refused if the payer cannot cover it.

**Salt balance (`[S]`).** It closes the gap with salt (goods tagged `TradeSalt`):
- **Short:** add the fewest units of your salt that cover it.
- **Over:** take as many of the trader's salt units as keep the deal acceptable.

**Atomicity.** One `InventoryTransaction` carries every line, both directions, any stack splits
and the dram settlement. A refused line, overweight, a vanished item or a short purse leaves
nothing changed.

**Diag:**
- `trade/Bartered`: given, taken, values, drams, tender.
- `trade/BarterRejected`: the reason.
- The direct buy and sell paths refuse a barter trader (`They take no drams.`).

**Salt purses.** On restock, a barter trader skips the dram top-up and instead tops their
salt up to `DramsFloor` worth of salt at their local salt price (Factory-gated, like shelf
refills).

**Layout** (the existing 80×45 grid, nothing moves):

```
 Barter with the salt-walker            | Your pack
 They take goods and salt, not drams.   | Perf 62%
 ───────────────────────────────────────────────────────────────────────────────
>  2/9  pale-salt          9lb    54    |    1/1 dagger           2lb    11
       mendleaf sprig      1lb     6    |        healing tonic    1lb    17
 ...                                    | ...
 Give 28   Take 54   Short 26: add goods or salt       (row 41: the deal line)
 ───────────────────────────────────────────────────────────────────────────────
 [F1]details  pale-salt  in deal 2 of 9  each 6                     (row 43)
 [Enter/→]add  [←]remove  [A]all  [S]salt  [O]offer  [Tab]side  [Esc]close
```

### 12.4 EC7 — the Concord's account (credit at a Factor)

The player's account with the Concord is a small ledger on the player entity (integer
properties, so it rides the save graph like drams).

**The line.** Credit line = `20 + standing / 2`, capped at 120. It is zero when:
- the player's Concord standing is Disliked or worse;
- an earlier debt is in default.

So a stranger is offered 20 drams; the line reaches 45 at Liked and 95 at Loved. The Open
Ledger "refuses no business"; the line is what that refusal-of-nothing costs.

**Borrow** (a Factor only):
- **Offer:** half the line, or all of it.
- **Debt:** the principal plus a 20% Concord fee (rounded up), due in three days (3,600 ticks).
- **Limit:** one open account at a time.
- **Ledger:** the drams enter through `Ledger.TryCredit(..., "concord-credit")`.

**Repay** (any Concord post-keeper):
- **Options:** settle in full, or pay what you carry.
- **Ledger:** payments leave through `Ledger.TryDebit(..., "concord-repayment")`.
- **Honour:** a debt settled in full, on time and carried at least a day, raises standing by
  5. The one-account rule plus the day keep this from being farmed.

**Default.** When a review finds the due tick passed with drams still owed:
- standing falls by 25, once;
- the account is marked in default, so no new credit is offered;
- a later settlement clears the mark, but earns no honour and does not refund the standing.

**Legibility.** The Concord's reputation "is literally a ledger". A Concord post-keeper's
greeting appends the account: standing, line, owed, and due day.

**Diag** in `trade`: ConcordCredit, ConcordRepaid, ConcordHonored, ConcordDefaulted,
ConcordCreditRefused (each with its reason).

**Money balance:** the fee makes credit a net sink, and a default is a faucet paid for in
standing.

### 12.5 Sub-milestones

| # | Milestone | Ships |
|---|---|---|
| EC6a | Tender | `Tender.For`; the Choir's offering on every shelf-holding Choir speaker (F1); direct buy and sell refuse a barter trader |
| EC6b | The deal | `BarterDeal`, `TradeSystem.TryDeal` (one transaction, partial lines, settlement by tender), the salt balance |
| EC6c | The barter screen | `TradeUI` deal mode (always for barter traders, `[D]` for dram traders); salt purses on restock |
| EC7 | The Concord's account | `ConcordAccount`: line, borrow, repay, honour, default, review on travel; dynamic Factor and Envoy choices and the account greeting |

### 12.6 Performance

- The deep-wasteland map is a 20×20 grid computed once, like the salt-pan list.
- Deal values are recomputed only on a deal change (key press), not per frame.
- The account review is a few property reads on zone transition.

### 12.7 EC6 — implementation (2026-11-01)

**EC6a: tender.**
- `Tender.For(trader)` applies §12.2 in order. `Tender.IsDeepWasteland(zoneId)` reads a
  once-computed 20×20 map (surface only; any zone id that names no world cell is not
  wasteland).
- The direct buy and sell paths refuse a barter trader with `TradeSystem.NoDrams` and record
  `BuyRejected` or `SaleRejected`.
- **F1 fix:** `ChoirOffering.CanOffer` holds for any Choir speaker with something on its shelf.
  `ConversationManager` injects `[Offer] Let the soil taste you.` beside the other injected
  choices, unless the conversation authors its own offering (the tendril's `NoTrade` node).
  The offering message now names the speaker.

**EC6b: the deal.**
- `BarterDeal` is the screen's model: lines on both sides, counts clamped to the stack, and
  values from `TradeSystem.GetBuyPrice` and `GetSellPrice` with a new count overload
  (`GetItemValue(item, count)`; the old overloads delegate with the whole stack, so their
  prices are unchanged).
- `Barter.TryDeal` runs one `DealCommand`:
  - it claims every line and checks presence, count and `CanBeTraded`;
  - it re-prices at the moment of offer;
  - it settles by tender: barter needs give ≥ take, while drams settle the net with purse and
    overflow guards;
  - it moves gives first (freeing weight), then takes, splitting partial lines inside source
    snapshots so a rollback restores the stack. The drams settlement is deferred to commit.
- `BalanceWithSalt` steps one unit at a time (prices round per line). Short: return the
  trader's salt, then add the fewest of yours. Over: take back yours, then take theirs while
  the deal stays acceptable.

**EC6c: the screen and the salt purse.**
- `TradeUI` opens in deal mode for a barter trader, and `[D]` switches at a dram trader:
  - row 41 carries the balance;
  - marked rows show `n/m` or `*`;
  - the detail line shows the count in the deal and the unit price;
  - `[O]` opens an offer popup with the same geometry as the per-item popup.
- `DealKey(KeyCode)` is the input seam the tests drive.
- `TraderRestockSystem` skips the dram top-up for a barter trader and instead tops its salt up
  to `SaltFloorUnits` (`DramsFloor` worth at the local EC5 price). It counts any salt the trader
  holds and records `trade/SaltRestocked`.
- **Real issue fixed on the way:** the panel divider was drawn down all 45 rows, so a `|`
  showed through every space at column 39 of the detail and action lines. It now stops at the
  panels.

### 12.8 EC7 — implementation (2026-11-01)

**Code.**
- `ConcordAccount` implements §12.4. The account is five integer properties on the player:
  `ConcordDebt`, `ConcordDue`, `ConcordOpened`, `ConcordDefaulted` and `ConcordHonored`.
- Blueprint tags mark who holds the books: `ConcordCredit` on `ConcordFactor` (lends) and
  `ConcordLedger` on `ConcordFactor` and `SaccharineEnvoy` (take payment). Faction alone is
  not enough, since the Concord's glass scorpions keep no books.
- `Borrow` and `Repay` review the account first, so a late payment is known to be late.
  `Repay` pays the least of what was asked, what is owed and what is carried.
- **The greeting.** On a ledger keeper's `Start` node, `ConversationManager` appends the live
  choices (`ConcordAccount.AppendChoices`) and the account statement
  (`ConcordAccount.AppendText`):
  - credit (half the line, the whole line) appears only while no account is open;
  - settlement ("Settle my account" or "Pay N of the M I owe") appears only to a player
    carrying drams.

  Both are read-only and change nothing. The actions `ConcordBorrow` and `ConcordRepay` parse
  a whole number of drams.
- **Travel.** `EconomyUpkeep.OnZoneEntered` is the one per-zone economy upkeep: the trader
  restock, then `ConcordAccount.Review`. `InputHandler.HandleZoneTransition` calls it in place
  of the bare restock call.

**Divergence from §12.4.** The line has no separate Disliked guard. `20 + standing/2` already
reaches zero at standing −40, so the guard could never change an outcome (found by asking
which mutant would kill it).

### 12.9 Verification and self-review (EC6, EC7)

**Tests.**

| File | Tests |
|---|---|
| `TenderTests` | 11 |
| `BarterDealTests` | 18 |
| `BarterScreenTests` | 10 |
| `ConcordAccountTests` | 27 |
| `BarterCreditAdversarialTests` | 23 |

**RED first:**
- EC6a and EC6b ran RED against stubs (9 of 11 and 16 of 18 failing).
- The EC6c screen and the EC7 account were written with their tests and proven by mutation
  instead.

**Mutation sweep:**
- 7 EC6 mutants: 6 killed on the first pass.
- **The survivor was a real design flaw.** `Barter.TryDeal` judged value and payment with
  the deal's preview before the transaction judged them again, so the transaction's own
  check was unreachable. Value is now judged once, inside the transaction, at the moment of
  offer, and the deal's performance is read live. The rerun mutant is killed by 5 tests.
- 9 EC7 mutants (honour timing, default once, fee, choices that would lie, the travel
  review, clearing a default, one account at a time, stale performance) — results in the
  implementation log.

**Self-review (§5):**
- 🟡 (fixed) Two judges of a deal's value (above).
- 🟡 (fixed) The trade screen's divider showed through the detail and action lines.
- 🟡 (fixed, F1) The Choir's shrine keepers lost their shops in EC3 and gave nothing.
- 🔵 Selling a stack piecemeal can pay at most 1 dram more than selling it whole: each line
  keeps the old minimum sell price of 1. Separating a stack in the inventory already allowed
  the same, so this is not new, and it is bounded per stack.
- 🔵 A salt purse is a goods faucet the size of the dram floor; carried inland it is worth up
  to four times as much. That is the caravan margin canon intends (EC5). Salt veins, which
  are free, were already a larger faucet.
- ⚪ A travelling merchant who wanders into the deep wasteland starts bartering there, and a
  deal composed elsewhere is re-judged by the new tender (pinned:
  `Adversarial_ATenderChangeIsReadWhenTheDealIsOffered`).
- ⚪ An interior zone (an id that names no world cell) trusts drams, even inside the deep
  wasteland.

**Honesty bounds:**
- **Can verify:** every rule above against EditMode fixtures, the real blueprints, and the
  real conversation and loot data.
- **Cannot verify from tests:** the barter screen's look and feel at play speed, and whether
  the deep wasteland's traders read well in play. The live bench and fixture (§12.10) address
  the runtime; feel needs a playthrough.

### 12.10 Live verification in the Editor (2026-11-01)

The Editor was driven over MCP (`execute_menu_item`, `diag_query`, `manage_editor`) after
reconnecting its bridge (an `Assets > Refresh` re-runs the kickstart).

**`EconomyBarterBench`** (self-auditing; `Caves of Ooo/Scenarios/Economy/Economy Barter Bench`).
Run `237c174b`: **21 of 21 cells pass**, read back through
`diag_query category=economybench kind=Cell`. The cells run against the real bootstrap: the
factory, `Factions.json`, the loot and conversation registries, and the restock factory.
- **Tender:** 8 cells (two controls).
- **Deals:** barter goods-for-goods with no drams; the direct path refusing drams; a dram
  deal paying the difference (control).
- **Salt:** balancing a short deal; a salt purse of 17 units and 0 drams in the deep
  wasteland.
- **Real conversations:**
  - Grib's real conversation offers exactly one offering and no trade;
  - the tendril's real conversation shows no injected duplicate;
  - the Factor's real conversation offers two credit lines and the statement.
- **Account cycle:** borrow, honoured repayment at an envoy, default once, and a second
  review that takes no penalty (control).

**Two findings from the live run:**
- **The bench's records were first dropped.** A new diag category is off unless listed;
  `economybench` joined `Diag.DefaultOnCategories` beside `gasbench` and `questbench`.
- **A recompile can finish during Play** and reload the domain, wiping an earlier run's
  buffer. The audit was rerun after the editor settled (Rule 8).

**`BarterScreenFixture`** opens the barter screen through the real `InputHandler.OpenTrade`
against a salt-walker in the deep wasteland. Captured in the Game view:
- the header reads "Barter with Saltwalker / They take goods and salt, not drams.";
- the marked salt row reads `1/18`;
- the red deal line reads "Give 0 Take 17 Short 17: add goods or salt.";
- the detail line reads "in deal 1 of 18 each 17";
- the key bar fits the width.

**Real issue found there and fixed:** barter rows still printed `$` after each value,
suggesting drams the trader refuses. Pinned in `BarterScreenTests`.

**Not verified live:** pressing the keys at play speed (background control cannot send keys
to an unfocused game), and a natural barter trader met in the world rather than through the
fixture.

## 13. Next steps

1. EC6 and EC7: shipped (§12). Possible follow-ups:
   - buying on account at a Concord trader (carrying debt at the trade screen);
   - Lumen-hours, once catacomb traders exist.
2. E5: `PalimpsestEcho` (a superseded-world trader); decide its fate against `Lore/`.
3. A commit-time `trade/Transferred` record for transactional fees (EC2b note).

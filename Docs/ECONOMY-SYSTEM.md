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

## 10. Next steps

1. Finish the §5.3 sweep (trade screen, restock timing, conversation-to-trader routing,
   faction trust data).
2. EC4: Tent-Right never sells water.
3. EC5: salt priced by distance from the flats; then EC6 and EC7.
4. E5: `PalimpsestEcho` (a superseded-world trader); decide its fate against `Lore/`.

# Weapon Rental and Ink Currency System

> A short-term loaner-weapon mechanic for *Caves of Ooo*. Pay **Ink** to a
> village **Quartermaster** to borrow a weapon for a fight, return it for a
> partial refund, or hold onto it and lose the rest.

This is the design-level companion to the implementation living at
`Docs/WEAPON-RENTAL-SYSTEM.md` (which records every milestone, cold-eye
review, and methodology decision per `CLAUDE.md`).

---

## The pitch

> *You walk into the next village. The dungeon ahead is bigger than the
> sword on your hip. The Quartermaster has a rack of loaner blades. You
> pay in Ink — not coin — and walk out armed. After the fight, you bring
> the weapon back for half your Ink. Or you keep it. Either way the
> Quartermaster is paid.*

The system answers a recurring problem in roguelikes: **you find a tough
fight before you've found a good weapon**. Caves of Ooo's existing economy
(Drams from the Merchant, items from Tinkers and chests) rewards
exploration, but it doesn't cover the "I need an upgrade *right now*"
beat. Renting fills that gap without diluting the value of permanent
weapon drops — rentals are temporary and lossy.

---

## Two new currencies, one new NPC

### Ink (the rental currency)

- **What it is:** an `IntProperty` on the player named `Ink`, separate
  from `Drams`.
- **Visible in:** the Inventory screen header (`Wt:N/M $D iI`).
- **Earned by:** content. Quest rewards (`GiveInk` action), dialogue gifts,
  starting pool. The player starts with **50 Ink**.
- **Spent on:** renting weapons. **Recovered (in part)** by returning them.
- **Not** a circulating currency. Ink isn't passed back and forth between
  NPCs — it's destroyed when paid and created when refunded. That's
  intentional: each rent-return cycle costs the player ~50% of the
  rental price, so Ink is meaningfully finite without an explicit cap.

### Drams (unchanged)

The existing currency stays put. You buy/sell with Drams at the
Merchant. You rent with Ink at the Quartermaster. **They don't
interconvert** — a player who wants Ink earns it from quests and
dialogue gifts, not by selling items.

### Quartermaster (the NPC)

- **Where:** every randomly-generated village. Spawns alongside the
  Merchant in `VillagePopulationBuilder`.
- **What:** a stationary, faction-Villagers, single-`@` (yellow) NPC
  whose `Conversation` part points at `Quartermaster_1`.
- **Stocks:** one each of three loaner weapons (see below). When the
  player rents, the weapon leaves the rack. There is no auto-restock
  in v1 — once the rack is empty, the player has to return something
  to free a slot.

---

## The rental flow

### Rent

1. Bump the Quartermaster → dialogue opens.
2. Pick *"Show me what's on the rack."* → Wares list.
3. Pick a specific weapon (*"The loaner dagger."* etc).
4. The system:
   - Validates the item is `Rentable` (tag) + has `Commerce` (price).
   - Computes cost: `ceil(BuyPrice * 0.25)`. Ego and faction standing
     scale the buy price, so a high-Ego, well-liked player rents
     cheaper than a low-Ego, disliked one — same curve as a normal
     purchase.
   - Checks affordability and inventory weight.
   - Deducts Ink, transfers the weapon, attaches a `RentalPart`
     recording how much was paid and which Quartermaster the
     weapon came from.

### Use

A rented weapon equips and fights identically to any other weapon. **It
cannot be sold elsewhere** — `RentalPart` vetoes the `CanBeTraded`
event. (Otherwise a player could rent for cheap Ink, sell for full
Drams at any village merchant, and pocket both. The veto closes that
exploit.)

### Return

1. Bump any Quartermaster → dialogue → *"I'm here to return a weapon."*
2. Pick *"Here you are."*
3. The system finds every `RentalPart`-tagged item in the player's
   inventory (carried OR equipped) whose `LessorBlueprintName` matches
   this Quartermaster, unequips it if needed, transfers it back, strips
   the `RentalPart`, and refunds `floor(InkPaid * 0.5)`.

Rentals returned at a *different* Quartermaster's blueprint are
refused (the rental is matched by lessor). In v1 every Quartermaster
shares the same blueprint, so this is moot — they're interchangeable.

### Lose

If the player dies or drops the weapon, the rental is effectively
lost. The `RentalPart` stays attached, but the item is no longer in
the player's inventory and there's no auto-recovery mechanism. The
Ink is gone.

---

## The three loaner weapons

| Blueprint | Tier | Damage | Buy price | Rent (Ego 18) | Refund |
|---|---|---|---|---|---|
| `LoanerDagger` | 1 | 1d4 + Pen 1 | 30 | ~12 ink | ~6 ink |
| `LoanerSpear` | 1 | 1d6+1 + Pen 2 | 55 | ~22 ink | ~11 ink |
| `LoanerLongsword` | 2 | 1d8+1 + Pen 3 | 120 | ~48 ink | ~24 ink |

Numbers shift with `Ego` and player reputation. Ego 18 + Neutral
faction is the default starting case.

Each Loaner blueprint declares `Stacker { MaxStack: 1 }` to override
the auto-stacker that all `Item` descendants inherit — without that,
renting two of the same weapon would auto-merge them in inventory and
the second `RentalPart` would be orphaned. (See
`Docs/WEAPON-RENTAL-SYSTEM.md` cold-eye review #3 for the failure
mode walkthrough.)

---

## Design intent — *why* this works

**Rental cheaper than buying.** Buy price is "yours forever"; rent is
"yours for one fight." 25% of buy price is roughly *one fight's worth
of upgrade* — enough to matter for a hard encounter, not so much that
the player rents instead of saving for a permanent weapon.

**Lossy refund pressures decisions.** 50% back means a rent-return
cycle costs the player *something*. They can't farm rentals to cycle
through every weapon in the game. Each rent is a bet: "is this fight
worth 6 Ink to me?"

**Separate from Drams.** A second wallet means the rental loop
doesn't compete with the buy/sell loop. A player who's broke on Drams
can still rent (if they have Ink); a player flush with Drams can't
shortcut the rental tax.

**No auto-restock in v1.** Empty Quartermaster racks force the player
to either find another village or *bring something back*. Returning
becomes an emergent strategy.

**Anti-exploit by construction.** The `CanBeTraded` veto on
`RentalPart` blocks selling. The lessor-blueprint match blocks
returning at the wrong shop. The Stacker `MaxStack=1` blocks
auto-merge silently orphaning rentals.

---

## Player-facing reference card

```
Currency: 50 Ink to start. See your balance in the Inventory header.
NPC:      Quartermaster, yellow @, in every village (alongside the Merchant).
Rent:     "Show me what's on the rack." → pick a weapon. ~25% of buy price.
Return:   "I'm here to return a weapon." → "Here you are." Half your Ink back.
Lose:     Die or drop the rental → it's gone. No refund.
Sell:     Refused. Rentals can't be sold.
Earn:     Quest rewards, dialogue gifts, starting pool of 50.
```

---

## Implementation map (for engineers)

The deep technical record is `Docs/WEAPON-RENTAL-SYSTEM.md`. Quick
pointers:

| Concern | File |
|---|---|
| Ink wallet, rent/return logic | `Assets/Scripts/Gameplay/Economy/RentalSystem.cs` |
| Rental marker on items | `Assets/Scripts/Gameplay/Economy/RentalPart.cs` |
| Dialogue verbs (`GiveInk`, `RentItem`, `ReturnRentals`) | `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` |
| NPC blueprint + 3 loaners | `Assets/Resources/Content/Blueprints/Objects.json` (search `Quartermaster`, `Loaner`) |
| Dialogue tree | `Assets/Resources/Content/Conversations/Quartermaster.json` |
| Village wire-in | `Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs` |
| Inventory UI display | `Assets/Scripts/Presentation/UI/InventoryUI.cs:2062` + `Assets/Scripts/Gameplay/Inventory/InventoryScreenData.cs:80,96` |
| Player starting pool | `Assets/Resources/Content/Blueprints/Objects.json` Player → `IntProps.Ink: 50` |
| Tests | `Assets/Tests/EditMode/Gameplay/Economy/RentalSystemTests.cs` (24), `RentalActionsTests.cs` (13), `VillagePopulationBuilderTests.cs` (integration spawn) |
| QA scenario | `Assets/Scripts/Scenarios/Custom/RentalTestBench.cs` |

---

## Future work

These are tracked in the tail of `Docs/WEAPON-RENTAL-SYSTEM.md` cold-eye
review section but worth surfacing here:

- **Sidebar Ink display.** Header shows it; sidebar (always-on) doesn't
  yet. Layout change.
- **Predicate-gated "Return" choice.** Currently the player can pick
  *"I'm here to return a weapon."* with nothing rented; the action no-ops
  with a polite message. A `IfHaveRental` predicate would hide the choice
  cleanly.
- **Quest-reward Ink grants.** `GiveInk` is implemented but no JSON
  quest uses it yet. Adding a small Ink reward to existing quest stages
  (e.g. `IronKeyQuest`) gives a second source beyond the starting pool.
- **Auto-restock.** A timer or zone-re-entry refresh on the
  Quartermaster's rack so rentals don't permanently empty.
- **Rental UI / list-picker.** Today's dialogue offers one choice per
  blueprint — fine at three weapons, would not scale to twelve.
- **Diag-payload `perf` field.** Symmetry with `TradeSystem.Bought` /
  `Sold` — the rental records omit `perf`, which limits AI debugging
  observability.

---

*Document scope: design. Engineering record at `Docs/WEAPON-RENTAL-SYSTEM.md`.
Last updated alongside M4 wire-in.*

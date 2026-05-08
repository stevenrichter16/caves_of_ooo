# Weapon Rental System

> Rental NPC who lends weapons before a fight in exchange for **Ink**, a new
> currency. Rental persists until manually returned (lost on death). Cost is a
> fixed fraction of the item's buy price; partial Ink refund on return.

Branch: `claude/weapon-rental-system-py3XV`
Status: planning → M1 in progress

---

## 1. Goal & Scope

**Player-visible behaviour (the invariants):**

1. Player has a separate **Ink** wallet, distinct from Drams. Starts at 0; can
   only be increased by content (quest reward, conversation `GiveInk` action).
2. A new NPC, the **Inkbound Quartermaster**, exposes three dialogue branches:
   "Browse rentals", "Return a weapon", "Goodbye".
3. Browsing rentals offers a curated list (3 weapons in v1). Each line shows
   the Ink cost. Selecting one transfers the weapon to the player's inventory
   and deducts Ink. The item is flagged with a `RentalPart` recording how
   much Ink was paid and which lessor it came from.
4. Returning a weapon at the same lessor refunds 50 % of the Ink paid (rounded
   down) and removes the weapon from the player's inventory. Returning at the
   wrong lessor is silently refused (the item stays).
5. Player death drops rental items along with everything else; rentals do not
   self-recover. (No mechanic to recover lost rentals in v1; documented as a
   known limitation.)

**Out of scope for v1:**

- Time-based deadlines / late fees.
- A polished list-picker rental UI (we use one dialogue choice per stocked
  weapon — same approach Qud uses for quest-reward menus).
- Auto-restock on the lessor side.
- Cross-zone rental persistence guarantees beyond what `SaveSystem` already
  provides reflectively.

---

## 2. Verification Sweep — corrections before code

Read these references end-to-end (or via the Explore agent's report dated
2026-05-08); the table below records concrete API shapes I'm relying on.

| Claim | Verified from | Notes |
|---|---|---|
| Currency lives as IntProperty `"Drams"` on entity. | `TradeSystem.cs:18,99-112` | Will mirror with `"Ink"`. |
| `GetBuyPrice(item, perf, trader)` takes performance + faction modifier. | `TradeSystem.cs:75-82` | Reuse for rental cost. |
| Conversation actions are registered via `ConversationActions.Register(name, fn)`; arg is a single string. | `ConversationActions.cs:33-36, 165-168` | Mirror `GiveDrams` / `StartTrade`. |
| `inv.AddObject(item)` returns bool (weight check), `RemoveObject` returns bool. | `TradeSystem.cs:166-172` (existing usage). | Same idiom in rental flow. |
| Conversation JSON schema: `Conversations[].Nodes[].Choices[]` with `Text/Target/Predicates/Actions`. | `Assets/Resources/Content/Conversations/Villagers.json:1-100` | Add `Quartermaster.json`. |
| Test fixture pattern: in-memory `new Entity { ... }` with manual Statistics + Tags + Parts. | `TradeTests.cs:27-79` | Mirror in `RentalSystemTests`. |
| **No existing `Ink` reference anywhere** in the codebase. | grep `Assets/Scripts -r "Ink\b"` | Greenfield — safe to introduce. |

**Corrections from initial sweep:** the agent suggested a `Stat`-typed Ink
under `entity.Statistics`. I'm using IntProperty instead so Ink mirrors Drams
exactly — same call shape (`GetIntProperty`/`SetIntProperty`), same save
serialization, same modify-by-conversation-action shape. Stats are reserved
for combat-relevant numbers (Hitpoints, Strength, Ego). Wallets aren't.

---

## 3. Sub-milestones

Smallest blast radius first. Each ships independently revertible.

### M1 — `RentalPart` + `RentalSystem` (pure logic, no UI/NPC)

**Files:**
- NEW `Assets/Scripts/Gameplay/Economy/RentalPart.cs`
- NEW `Assets/Scripts/Gameplay/Economy/RentalSystem.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Economy/RentalSystemTests.cs`

**API surface:**
```csharp
public class RentalPart : Part {
    public int InkPaid;
    public string LessorBlueprintName;
}

public static class RentalSystem {
    public const string INK_PROP = "Ink";
    public const double RENTAL_FRACTION = 0.25;   // cost = 25% of buy price
    public const double REFUND_FRACTION = 0.50;   // refund = 50% of Ink paid

    public static int GetInk(Entity entity);
    public static void SetInk(Entity entity, int amount);   // clamps to 0
    public static void AddInk(Entity entity, int delta);

    public static int GetRentalCost(Entity item, Entity renter, Entity lessor);
    public static bool IsRentable(Entity item);
    public static bool IsRented(Entity item);

    public static bool TryRent(Entity renter, Entity lessor, Entity item);
    public static bool TryReturn(Entity renter, Entity lessor, Entity rentalItem);
}
```

**RED→GREEN test plan** (each with a counter-check per CLAUDE.md §3.4):

| # | Invariant | Counter-check |
|---|---|---|
| 1 | `GetInk` defaults to 0 | (vacuous; baseline) |
| 2 | `SetInk(p, 50)` round-trips | `SetInk(p, -10)` clamps to 0 |
| 3 | `AddInk(p, 25)` adds to existing | `AddInk(p, -1000)` cannot go negative |
| 4 | `GetRentalCost` = `ceil(BuyPrice × 0.25)` | item with no `CommercePart` → 0 |
| 5 | `IsRentable` true with Rentable tag + CommercePart | missing tag → false |
| 6 | `TryRent` happy path: deducts Ink, RentalPart attached, item moves | insufficient Ink → false, no state change |
| 7 | `TryRent` rejects non-rentable item | already-rented item also rejected |
| 8 | `TryReturn` removes item, refunds `floor(InkPaid × 0.5)` | wrong lessor (blueprint mismatch) → false, no refund |
| 9 | `TryReturn` rejects normal (non-rented) item | (mutation-resistance) RentalPart absent → false |

### M2 — Dialogue actions: `GiveInk`, `RentItem`, `ReturnRentals`

**Files:**
- MOD `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Economy/RentalActionsTests.cs`

**Actions:**
- `GiveInk` (arg: int) — mirrors `GiveDrams` (`ConversationActions.cs:611-620`).
- `RentItem` (arg: blueprint name) — finds first matching blueprint in
  `speaker`'s inventory, calls `RentalSystem.TryRent(listener, speaker, item)`.
- `ReturnRentals` (no arg) — iterates `listener` inventory, returns every
  rental whose `LessorBlueprintName` matches `speaker.BlueprintName`.

**Tests:**
- GiveInk valid amount → Ink rises (counter: GiveInk negative is a no-op).
- RentItem succeeds when stock present (counter: missing blueprint silently
  no-ops, no Ink deducted).
- ReturnRentals returns matching items (counter: a non-matching lessor's
  rental in the same inventory is left alone).

### M3 — NPC + conversation content + scenario testbench

**Files:**
- MOD `Assets/Resources/Content/Blueprints/Objects.json` — adds
  `InkboundQuartermaster` Creature blueprint, three rental weapons
  (`RentalDagger`, `RentalSpear`, `RentalLongsword`).
- NEW `Assets/Resources/Content/Conversations/Quartermaster.json`
- NEW `Assets/Scripts/Scenarios/Custom/RentalTestBench.cs`

**Conversation shape:**
```
Start: "What weapon do you need?"
   ├── "Show me what's in stock." → Wares
   ├── "I'm returning a weapon."  → Return    (no predicate gating in v1)
   └── "Farewell."                 → End

Wares:
   ├── "The bone-edge dagger." [RentItem:RentalDagger] → Start
   ├── "A spear, please."      [RentItem:RentalSpear]  → Start
   ├── "The longsword."        [RentItem:RentalLongsword] → Start
   └── "Never mind."           → Start

Return: action ReturnRentals → Start
```

**Honesty bound:** I cannot launch Unity from this environment, so M3 is
authored to match the existing conversation/blueprint conventions but is
**not compile-verified or play-tested**. The user must run
`mcp__unity__refresh_unity` + `read_console` after pulling the branch. The
M1+M2 EditMode tests will catch any signature drift in the runtime layer.

---

## 4. Performance section

This feature touches:
- No `LateUpdate` / `Update` / per-frame paths.
- No new caches.
- No new MonoBehaviour.
- Conversation actions fire on player input (a few times per session).
- `RentalPart` is a tiny struct-like Part attached to a handful of items.

→ **No performance-sensitive code paths.** Standard Part allocation only.

---

## 5. Save/load

Per `Docs/SAVESYSTEM-DEEP-DIVE-AUDIT.md`:
- Ink is an IntProperty → already serialized with the rest of `Properties`.
- `RentalPart` has only public fields → reflectively round-trips.
- No custom serializer code needed.

**Counter-check planned in M1 tests:** add a round-trip assertion for
`RentalPart` if a save-system test fixture is reachable from EditMode.
Otherwise note it as 🧪 in the doc.

---

## 6. Implementation log

(Populated per phase as work proceeds.)

### M1 — RentalPart + RentalSystem + 17 tests

**Files shipped:**
- NEW `Assets/Scripts/Gameplay/Economy/RentalPart.cs`
- NEW `Assets/Scripts/Gameplay/Economy/RentalSystem.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Economy/RentalSystemTests.cs`

**In-phase self-review (CLAUDE.md §5):**

🟡 Finding M1.1 — *Rented items must refuse to be sold.*
Without a veto, a player could rent a weapon for 25 % Ink, sell it for full
Drams at any village merchant, and pocket both. **Fix shipped:**
`RentalPart.HandleEvent` returns false on `"CanBeTraded"`, riding
`TradeSystem.CanBeTraded`'s existing item-side veto path
(`TradeSystem.cs:343-358`). Two tests added:
`RentedItem_CannotBeSold` (positive), `NonRentedItem_CanStillBeSold`
(counter-check that the veto is specific).

🔵 Finding M1.2 — *Pricing reuses Ego/faction modifiers.*
`GetRentalCost` calls `TradeSystem.GetBuyPrice`, so a high-Ego player gets
a cheaper rental and a Disliked faction charges 10 % more. Intended:
content authors don't need to learn a parallel pricing model.

🧪 Finding M1.3 — *No save-system round-trip test for `RentalPart`.*
`RentalPart`'s public fields will round-trip via the reflective serializer
(per `Docs/SAVESYSTEM-DEEP-DIVE-AUDIT.md` §3.9.5), but there's no explicit
EditMode test. Deferring — the SaveSystem audit's prediction tests already
exercise this path generically.

⚪ Finding M1.4 — *No PlayMode sweep yet.*
Pure-logic milestone. PlayMode validation lands with M3 (the testbench
scenario).

**Test inventory (each invariant + counter-check):**

| # | Test | Mirrors / counter-checks |
|---|---|---|
| 1 | `GetInk_NewEntity_DefaultsToZero` | baseline |
| 2 | `SetInk_RoundTrips` | — |
| 3 | `SetInk_NegativeClampsToZero` | counter to (2) |
| 4 | `AddInk_AddsDelta` | — |
| 5 | `AddInk_NegativeBeyondZeroClampsToZero` | counter to (4) |
| 6 | `GetRentalCost_IsFractionOfBuyPrice` | — |
| 7 | `GetRentalCost_ItemWithoutCommercePart_IsZero` | counter to (6) |
| 8 | `IsRentable_TaggedItemWithCommerce_True` | — |
| 9 | `IsRentable_MissingTag_False` | counter |
| 10 | `IsRentable_MissingCommerce_False` | counter |
| 11 | `IsRented_DefaultFalse_TrueAfterRent` | both branches |
| 12 | `TryRent_HappyPath_TransfersItemAndDeductsInk` | — |
| 13 | `TryRent_InsufficientInk_FailsAndDoesNotMutate` | counter to (12) |
| 14 | `TryRent_NonRentableItem_Fails` | counter to (12) |
| 15 | `TryRent_AlreadyRentedItem_Fails` | counter to (12) |
| 16 | `TryReturn_HappyPath_RemovesItemAndRefunds` | — |
| 17 | `TryReturn_WrongLessorBlueprint_Fails` | counter to (16) |
| 18 | `TryReturn_NonRentedItem_Fails` | counter to (16) |
| 19 | `RentedItem_CannotBeSold` | M1.1 |
| 20 | `NonRentedItem_CanStillBeSold` | counter to (19) |

**Honesty bound:** M1 is authored and reviewed; **EditMode tests have not
been executed** (no Unity in this environment). User must run
`mcp__unity__refresh_unity` then `run_tests mode=EditMode` after pulling.

### M2 — Conversation actions: GiveInk / RentItem / ReturnRentals

**Files shipped:**
- MOD `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` (+~70 LOC)
- NEW `Assets/Tests/EditMode/Gameplay/Economy/RentalActionsTests.cs`

**Decisions:**
- `GiveInk` mirrors `GiveDrams` exactly (parse-validate-mutate, no-op on
  `<= 0` or non-numeric).
- `RentItem` searches the speaker's inventory by blueprint name and
  delegates to `RentalSystem.TryRent`. Player-visible failure messages
  flow from `TryRent`'s existing `MessageLog.Add` calls.
- `ReturnRentals` iterates the listener's inventory **in reverse** so
  `RemoveObject` inside the loop doesn't shift later indices. Filters
  by `RentalPart.LessorBlueprintName == speaker.BlueprintName`, so
  rentals from a different Quartermaster are left alone (forward-
  compatible with multiple lessor types in v2).

**In-phase self-review:**

🟡 Finding M2.1 — *RentItem stock-miss message is generic.*
"Quartermaster has none of those left" fires both when the lessor
never stocked the blueprint and when the player rented the last one.
Acceptable: both wordings are truthful. If playtest reveals authors
need to differentiate, split into "they never had that" vs "you took
the last one". ⚪.

🔵 Finding M2.2 — *ReturnRentals counts successful returns to suppress
the "no rentals" message correctly.* Without the counter, calling
`ReturnRentals` while the player held only foreign-lessor rentals would
print nothing — silent UX. Counter shipped + tested.

**Test inventory (12 new tests):**

| Test | Counter-check |
|---|---|
| `GiveInk_ValidAmount_AddsInk` | — |
| `GiveInk_NegativeAmount_NoOp` | counter |
| `GiveInk_ZeroAmount_NoOp` | counter |
| `GiveInk_NonNumericArg_NoOp` | counter |
| `RentItem_StockPresent_RentsAndDeductsInk` | — |
| `RentItem_BlueprintNotInStock_NoOp` | counter |
| `RentItem_EmptyArg_NoOp` | counter |
| `RentItem_PlayerCannotAfford_NoTransfer` | counter |
| `ReturnRentals_HappyPath_ReturnsAllMatching` | — |
| `ReturnRentals_DoesNotTouchOtherLessorsRentals` | counter |
| `ReturnRentals_DoesNotTouchNonRentedItems` | counter |
| `ReturnRentals_NoRentals_StillSafeToCall` | defensive |

### M3 — Quartermaster NPC + conversation + RentalTestBench

**Files shipped:**
- MOD `Assets/Resources/Content/Blueprints/Objects.json` — adds
  `Quartermaster` Creature blueprint and three rentable weapons
  (`LoanerDagger`, `LoanerSpear`, `LoanerLongsword`), each tagged
  `Rentable` and inheriting `MeleeWeapon`.
- NEW `Assets/Resources/Content/Conversations/Quartermaster.json` —
  4-node dialogue (`Start` → `Wares` / `Return` / `Explain` / `End`).
  Auto-discovered by `ConversationLoader.LoadAll`.
- NEW `Assets/Scripts/Scenarios/Custom/RentalTestBench.cs` — places
  the Quartermaster two cells east of the player, stocks the rack,
  grants 250 Ink, mirrors `MerchantShopShowcase.cs` style.

**Decisions / divergences:**

🔵 *Renamed Quartermaster.* The plan called it `InkboundQuartermaster`,
but the Inkbound faction is from a different project (per
`INKBOUND_LORE_REFERENCE.md`); CoO has no Inkbound faction. Using
`Quartermaster` + `Faction = Villagers` matches the `Tinker` and
`Merchant` precedent and avoids an undeclared faction reference.

🔵 *Renamed weapons.* `RentalDagger` etc. → `LoanerDagger` etc. for
in-fiction readability ("loaner dagger" reads naturally in the message
log).

🔵 *No predicate-gated `Return` choice.* The plan called for hiding
the "I'm here to return a weapon" choice unless the player holds a
rental. v1 ships without that gating; the action's no-rentals branch
handles it gracefully via the M2.2 counter ("You have no rentals to
return here."). Adding a predicate (e.g. `IfHaveTag:Rentable` on the
inventory side) is a v2 polish item — `ConversationPredicates.cs`
would need a new `IfHaveRental` predicate to express it.

**Honesty bound — what I CAN'T verify here:**
- I have no Unity in this environment, so the JSON parses (`python3 -c
  "json.load(...)"` confirms syntactic validity, but Unity's
  `JsonUtility.FromJson<ConversationFileData>` may differ on edge
  cases — Unity's parser is stricter than CPython's about field
  presence).
- The blueprint inheritance chain (Quartermaster → Creature, Loaner*
  → MeleeWeapon) is authored to match other entries in `Objects.json`
  but is not compile-verified.
- The `RentalTestBench` scenario uses `ctx.Spawn / ctx.Factory /
  ctx.World.ClearCell` exactly as `MerchantShopShowcase` does, so the
  surface area is the same.

**User must run after pulling:**
1. `mcp__unity__refresh_unity` (force-recompile + reimport).
2. `mcp__unity__read_console types=[error]` — must be empty.
3. `mcp__unity__run_tests mode=EditMode` — expect +32 tests over the
   pre-WRS baseline; all green.
4. Optional PlayMode sweep: load `Rental Test Bench` from the scenario
   menu, bump the Quartermaster, rent + return + try-to-sell-elsewhere.

### Cold-eye review — 1 finding fixed, 1 deferred

Run after M3 commit per CLAUDE.md "Post-implementation cold-eye review"
section. Read all M1+M2+M3 diffs together.

**Q1 — Symmetry check** (TryRent vs TryReturn read side-by-side)

🟡 **Finding C1.1 — TryReturn lacked the rollback shape TryRent has.**
TryRent (RentalSystem.cs:128-136 pre-fix) on `renterInv.AddObject`
failure does `lessorInv.AddObject(item)` to restore state. TryReturn
did **not** — it called `lessorInv.AddObject(rentalItem)` unconditionally,
*and* called `rentalItem.RemovePart(rental)` BEFORE confirming the
lessor accepted the item. Two latent bugs:
  - (a) If the lessor's inventory rejected the add (over weight), the
    item was orphaned (in neither inventory).
  - (b) RentalPart was stripped before the transfer was confirmed, so
    on a failed add the player would hold an untagged-but-unowned
    weapon.
The check the cold-eye pass uses: *if I swap "OnApply" with "OnRemove"
in my head, would the surrounding code make equally good sense?* —
applied here as "swap rent / return" and the answer was no.

**Fix shipped** in this commit: TryReturn now mirrors TryRent's
rollback exactly:
```csharp
if (!renterInv.RemoveObject(rentalItem)) return false;
if (!lessorInv.AddObject(rentalItem))
{
    renterInv.AddObject(rentalItem);
    MessageLog.Add($"{lessor.GetDisplayName()} can't accept that right now.");
    return false;
}
rentalItem.RemovePart(rental);
```
Plus a new test `TryReturn_LessorOverWeight_RollsBackCleanly` that
forces the lessor inventory to refuse and asserts (a) item restored
to renter, (b) RentalPart still attached, (c) no refund.

**Q2 — Cross-feature consistency** (compare diag payload shapes)

🔵 **Finding C2.1 — RentalSystem diag payloads omit `perf`.**
`TradeSystem.BuyFromTrader` records `{ itemName, itemId, price,
dramsAfter, perf }` (TradeSystem.cs:188-195) so AI debugging can
answer "did the faction modifier work?" RentalSystem records
`{ itemName, itemId, cost, inkAfter }` — same shape minus `perf`.
**Deferred** to a follow-on diff: it's an observability symmetry
issue, not correctness, and the diff would touch every Diag.Record
call. Tracked in this doc; not closing the cold-eye pass on this
finding.

**Q3 — Counter-check completeness**

✓ `RentalPart.InkPaid` exercised via TryRent_HappyPath (records cost)
+ TryReturn_HappyPath (refund formula).
✓ `RentalPart.LessorBlueprintName` exercised via TryRent_HappyPath
(records blueprint) + TryReturn_WrongLessorBlueprint_Fails (counter).
✓ `RentalPart.HandleEvent("CanBeTraded")` both branches:
RentedItem_CannotBeSold (positive), NonRentedItem_CanStillBeSold
(counter).
✓ ConversationActions: every arg branch covered (valid / invalid /
empty / negative / non-numeric / blueprint mismatch / lessor
mismatch / no-rentals).

**Q4 — Doc-vs-impl drift**

✓ Constants in this doc (`INK_PROP = "Ink"`, `RENTAL_FRACTION = 0.25`,
`REFUND_FRACTION = 0.50`) match `RentalSystem.cs:25-30`.
✓ Test counts: M1 = 21 (was 20 + 1 from cold-eye fix), M2 = 12.
Combined: **33** new EditMode tests.
🔵 Minor — the M1 test inventory table ordered the sell-veto tests
after `TryReturn_NonRentedItem_Fails`, but the source file inserts
them before. Harmless ordering drift; both list the same set.

**Process: 1 finding fixed in this commit, 1 deferred with rationale.
Cold-eye pass complete.**

### Cold-eye review #2 — independent reviewer pass

A second cold-eye pass via an independent agent (clean read of the
diff, no prior context) caught two bugs the first pass missed.
Tests-green-feels-clean is exactly when latent issues hide; this is
why the second pass exists.

🟡 **Finding cold-eye-2.1 — Equipped rental cannot be returned.**
The typical user case for this feature is *rent → equip → fight →
come back to return.* Pre-fix, `TryReturn` called
`renterInv.RemoveObject(rentalItem)` directly, which only searches
`InventoryPart.Objects` (line 96). Equipped items live in
`InventoryPart.EquippedItems` and on the `BodyPart` graph — they are
**not** in `Objects`. So `RemoveObject` returned false and `TryReturn`
silently failed. `ReturnRentals` was strictly worse: it iterated only
`inv.Objects`, so an equipped rental was invisible to the loop and
the action printed *"You have no rentals to return here."* even with
one in hand.

Fix shipped:
1. `TryReturn` now mirrors `TradeSystem.SellToTrader:239-243` — if
   the item is equipped, unequip first, then transfer.
2. `ReturnRentals` snapshots both `inv.Objects` and
   `inv.EquippedItems.Values` (deduped) into a candidate list before
   iterating, so equipped rentals are visible to the matcher.
3. Two new tests:
   `TryReturn_EquippedRental_UnequipsThenReturnsAndRefunds`
   (RentalSystemTests) and `ReturnRentals_FindsEquippedRental`
   (RentalActionsTests). Both use the `Body` + `AnatomyFactory.
   CreateHumanoid` fixture pattern from `CudgelSlamTests`.

🟡 **Finding cold-eye-2.2 — Stacker auto-merge would orphan
RentalPart.** `InventoryPart.AddObject` (lines 60-77) auto-merges
stackable items via `StackerPart.MergeFrom`, **consuming the source
entity**. A `RentalPart` attached to a stacker source would be
orphaned on the consumed reference, leaving the merged stack
silently un-flagged — the player would have free use of the rental
permanently and cannot return it (the `RemoveObject` lookup against
the consumed reference would fail). Today's loaner blueprints
declare no `Stacker`, but `MeleeWeapon` is the shared base — adding
`Stacker` to a future child would silently regress the rental flow.

Fix shipped:
- `IsRentable` now rejects items with a `StackerPart`. New test
  `IsRentable_StackableItem_False` asserts the guard.

🔵 **Finding cold-eye-2.3 — Missing fail-message on TryReturn's
renter-side `RemoveObject` failure.** With finding 2.1 fixed, the
remaining `RemoveObject` failure path is a true edge case (post-
unequip the item should always be in `Objects`), but for symmetry
with the lessor-side rollback message, a `MessageLog.Add` was added.

⚪ Other reviewer findings (declined):
- *Faction modifier on refund.* Refund is anchored to `InkPaid` at
  rent time, not recomputed against current reputation. Intended;
  documented inline.
- *Quartermaster lacks `AISelfPreservation`.* Intentional —
  stationary NPC.
- *`cost` vs `price` field name divergence in diag payload.* Same
  observability category as deferred Finding C2.1; deferred together.

**Test inventory after cold-eye-2:**
- M1: 23 tests (was 21 — added `IsRentable_StackableItem_False`,
  `TryReturn_EquippedRental_UnequipsThenReturnsAndRefunds`).
- M2: 13 tests (was 12 — added `ReturnRentals_FindsEquippedRental`).
- **Combined: 36 new EditMode tests.**

**Process: 2 🟡 findings fixed in this commit, 1 🔵 fixed for
symmetry. Cold-eye pass #2 complete.**

### Cold-eye review #3 — inheritance trap + meta-file format

After being told "you missed core functionality initially, do another
thorough review," a third pass turned up two more issues — one of
which would have shipped a completely broken feature.

🔴 **Finding cold-eye-3.1 — Stacker-inheritance bug: cold-eye-2.2's
fix would have broken the entire feature.**

The base `Item` blueprint declares `Stacker` (`Objects.json:18`).
Every `Item` descendant — **including `MeleeWeapon` and therefore
every Loaner blueprint** — inherits a `StackerPart` with the C#
default `MaxStack = 99`. Cold-eye-2.2's `IsRentable` guard rejected
items with *any* `StackerPart`, which means every production Loaner
weapon would have failed `IsRentable` at runtime — `TryRent` would
print *"isn't for rent"* and **the rental flow would never
succeed**. The first reviewer claimed the bug was latent ("today's
Loaner blueprints aren't stackable") but didn't trace the
inheritance chain: `LoanerDagger → MeleeWeapon → Item → Stacker`.

Why the original cold-eye-2.2 concern is still real: a player who
rents a second copy of the same blueprint triggers
`InventoryPart.AddObject`'s auto-merge (line 60-77). The merge calls
`existingStacker.MergeFrom(newSource)` — the **existing** stacker's
`MaxStack` decides whether the merge happens. With the default
`MaxStack=99`, the merge succeeds, the source entity is consumed,
and the new `RentalPart` is orphaned on the consumed reference.

Fix shipped (two-sided):
1. **`IsRentable` loosened** to reject only `StackerPart` instances
   where `MaxStack > 1`. Items with a non-mergeable `Stacker`
   (MaxStack=1) are accepted. This is the correct rule —
   non-mergeable stackers are inert and harmless.
2. **Each Loaner blueprint** (`LoanerDagger`, `LoanerSpear`,
   `LoanerLongsword`) explicitly overrides
   `Stacker { MaxStack: 1 }`. The blueprint loader (`Bake`,
   `BlueprintLoader.cs:160-178`) merges params child-over-parent, so
   the override takes effect.
3. **Tests updated:**
   - `IsRentable_StackableItem_False` renamed to
     `IsRentable_StackableMaxAboveOne_False` (covers the default
     MaxStack=99 case).
   - **NEW** `IsRentable_StackerCappedAtOne_True` — pins the positive
     case the production blueprints rely on.

🟡 **Finding cold-eye-3.2 — `Quartermaster.json.meta` missing
`TextScriptImporter` directive.**

Existing JSON `.meta` files in `Assets/Resources/Content/Conversations/`
include a `TextScriptImporter:` block that tells Unity how to import
the file as a `TextAsset`. My .meta had only `fileFormatVersion` +
`guid`. Unity's importer would regenerate the meta on first
reimport, replacing my GUID with a fresh one — harmless because no
reference uses the old GUID, but unprofessional. **Fix:** added the
full importer directive to match the existing pattern.

**Honest framing:** I rolled out an over-strict guard in cold-eye-2,
the independent agent reviewer didn't catch it because they didn't
trace blueprint inheritance, and only the user's "do another thorough
review" prompt forced me to look harder. The lesson: a 🟡 fix can
introduce a 🔴 if you don't check the actual content path. **Tests
should cover the production blueprint instances, not just hand-rolled
fixtures.** A future improvement would be an integration test that
loads `LoanerDagger` via `EntityFactory.CreateEntity` and asserts
`IsRentable == true`.

**Test inventory after cold-eye-3:**
- M1: 24 tests (was 23 — renamed one, added
  `IsRentable_StackerCappedAtOne_True`).
- M2: 13 tests (unchanged).
- **Combined: 37 new EditMode tests.**

**Process: 1 🔴 + 1 🟡 fixed. Cold-eye pass #3 complete.**

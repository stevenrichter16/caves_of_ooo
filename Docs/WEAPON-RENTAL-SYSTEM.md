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

### Cold-eye review — pending

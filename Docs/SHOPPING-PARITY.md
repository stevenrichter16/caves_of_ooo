# Shopping / Trading — Qud-Parity Sweep

> **Status:** scope is mostly a parity-gap closeout, not a new build.
> CoO already has a substantial shopping system (~95% complete + 18/18
> unit tests GREEN). This ship pins remaining Qud-parity gaps and adds
> the scenario-level diag verification missing from prior trade work.

## What CoO has TODAY (verified in survey + 18/18 tests GREEN)

| Subsystem | Status | File pointer |
|---|---|---|
| `CommercePart` (per-item base value, `int Value`) | ✅ Implemented | `Gameplay/Economy/CommercePart.cs:1-14` |
| `TradeSystem` static class | ✅ Implemented | `Gameplay/Economy/TradeSystem.cs:1-224` |
| Drams currency (IntProperty `"Drams"` on entity) | ✅ Implemented | `TradeSystem.cs:17, 98-111` |
| Trade performance formula (Ego-driven, clamp 0.05–0.95) | ✅ Implemented; Qud-exact | `TradeSystem.cs:41-47` |
| Faction modifier (Loved 0.85 / Liked 0.95 / Neutral 1.0 / Disliked 1.10) | ✅ Implemented | `TradeSystem.cs:54-68` |
| Buy / sell price formulas (Qud-exact) | ✅ Implemented | `TradeSystem.cs:74-93` |
| Stack-aware pricing (× StackerPart.StackCount) | ✅ Implemented | `TradeSystem.cs:30-32` |
| `BuyFromTrader` / `SellToTrader` (validation + currency swap + auto-unequip) | ✅ Implemented | `TradeSystem.cs:116-202` |
| `BeforeTrade` event (per-buy/sell veto hook) | ✅ Implemented | `TradeSystem.cs:137-143` |
| `GetTraderStock` (filter to `CommercePart`-bearing items) | ✅ Implemented | `TradeSystem.cs:207-222` |
| `TradeStockBuilder` (procedural per-zone merchant stock) | ✅ Implemented | `World/Generation/Builders/TradeStockBuilder.cs` |
| `TradeUI` (645-line full-screen barter interface) | ✅ Implemented | `Presentation/UI/TradeUI.cs` |
| `InputHandler.OpenTrade` flow (pause render, route input) | ✅ Implemented | `Presentation/Input/InputHandler.cs` |
| `ConversationActions.StartTrade` action | ✅ Implemented | `Conversations/ConversationActions.cs:165-167` |
| Auto-inject `[Let's trade.]` choice on any inventory-bearing speaker | ✅ Implemented | `Conversations/ConversationManager.cs:163-175` |
| `Merchant` blueprint (Conversation: Merchant_1) | ✅ Implemented | `Resources/Content/Blueprints/Objects.json` |
| `TradeTests` unit fixture | ✅ 18/18 GREEN, 0.82s | `Tests/.../Economy/TradeTests.cs` |

## Qud-parity gap matrix (qud-decompiled-project survey)

| Qud feature | Qud file | CoO status | Priority |
|---|---|---|---|
| `CanBeTradedEvent` per-item veto (quest items, "WontSell" flag) | `CanBeTradedEvent.cs:4-46` | ❌ Missing — current `BeforeTrade` fires on actor, not item | ⭐⭐⭐ Must |
| `StartTradeEvent` per-session (services: identify/repair/recharge) | `StartTradeEvent.cs:4-63` | ❌ Missing | ⭐⭐ Polish |
| Trader-state validation (in-melee, on fire, no inventory) | `TradeUI.cs:362-379` | ⚠️ Partial — checks `traderInv == null` only | ⭐⭐ Polish |
| `TraderCreditExtended` IntProperty (buy on credit) | `TradeUI.cs:381-399` | ❌ Missing | ⭐ Skip v1 |
| Item ownership / theft tracking | (Qud doesn't have either) | ❌ Out of scope | n/a |

**Analysis of which gaps are actual bugs vs cosmetic:**

- **CanBeTradedEvent — REAL BUG.** Current behavior lets the player sell their starting weapon, the IronKey they need for the dungeon, or a quest item — anything with a `CommercePart`. There's no quest-item-protection mechanism. Highest priority.
- **Trader-state validation — semi-bug.** If the merchant catches fire (BurningEffect) or is in active melee, the player can still trade. The ConversationManager probably blocks conversation in melee, but burning is open. Should pin.
- **StartTradeEvent — nice-to-have.** Future-content unblocker (e.g., a NPC who opens trade only after a quest stage). v1 doesn't need it.
- **TraderCreditExtended — Qud-specific.** Skip for v1.

## Scope (what this ship adds)

Five sub-milestones. Smallest-blast-radius first. Each commits as one
reviewable change.

### SP.1 — Plan + branch (this commit)

- `Docs/SHOPPING-PARITY.md` (this file)
- Branch `feat/shopping-parity` cut from main at `26bb549` (post LK merge)

### SP.2 — `CanBeTradedEvent` + `"NoTrade"` tag (one commit)

The quest-item-protection gap. Add a per-item event that any part can
listen to and veto. Minimum-blast-radius design:

```csharp
// In TradeSystem.BuyFromTrader and SellToTrader, BEFORE the existing
// BeforeTrade event fires on the actor:

var canBeTraded = GameEvent.New("CanBeTraded");
canBeTraded.SetParameter("Item", (object)item);
canBeTraded.SetParameter("Buyer", (object)buyer);   // or Seller
canBeTraded.SetParameter("Trader", (object)trader);
canBeTraded.SetParameter("Direction", "Buy");        // or "Sell"
if (!item.FireEventAndRelease(canBeTraded))
{
    MessageLog.Add($"You can't trade {item.GetDisplayName()}.");
    return false;
}
```

Plus a simple `"NoTrade"` tag check helper — items with `Tags: [{ "Key": "NoTrade", "Value": "" }]`
return `false` from `CanBeTraded` automatically. (No new Part class
needed; tags are cheaper.)

**RED → GREEN tests** in `Tests/.../Economy/CanBeTradedEventTests.cs`:

1. `CanBeTradedEvent_FiresOnBuy_WithCorrectPayload`
2. `CanBeTradedEvent_FiresOnSell_WithCorrectPayload`
3. `BuyFromTrader_VetoedItem_TransferDoesNotHappen` — tag a stock item
   "NoTrade", attempt buy, assert no transfer + no drams change
4. `SellToTrader_VetoedItem_TransferDoesNotHappen` — same shape, sell side
5. `BuyFromTrader_VetoedItem_DramsUntouched` — counter-check on currency
6. Adversarial: `CanBeTradedEvent_NullItem_NoCrash`
7. Counter-check: `CanBeTradedEvent_UntaggedItem_TradesNormally`

**Wire one quest-item example**: tag `IronKey` (from `feat/lock-and-key`)
with `"NoTrade"` so locked-door progression can't be sold off.

### SP.3 — Trader-state validation + `StartTradeEvent` (one commit)

Two related Qud-parity additions in one commit since they touch the
same codepath (the `BuyFromTrader` / `SellToTrader` entry).

**Trader-state validation** — in `BuyFromTrader` / `SellToTrader`, fail
fast if the trader is:
- on fire (has `BurningEffect`)
- frozen (has `FrozenEffect`) — can't speak
- stunned (has `StunnedEffect`) — can't speak
- dead (Hitpoints ≤ 0) — defensive

```csharp
if (TraderUnableToTrade(trader, out string reason))
{
    MessageLog.Add($"The trader {reason}.");
    return false;
}
```

**StartTradeEvent** — fired on the trader at the top of
`InputHandler.OpenTrade` (or wherever the UI opens). Allows future
listeners to set service-flags (Identify / Repair / Recharge) on the
event without changing the trade flow. No service implementation in
this ship; just the event hook.

**RED → GREEN tests** in `Tests/.../Economy/TraderStateTests.cs`:

1. `BuyFromTrader_BurningTrader_BlocksTransfer`
2. `BuyFromTrader_StunnedTrader_BlocksTransfer`
3. `BuyFromTrader_DeadTrader_BlocksTransfer` (Hitpoints ≤ 0)
4. `BuyFromTrader_HealthyTrader_AllowsTransfer` — counter-check
5. `StartTradeEvent_FiresWithCorrectPayload_OnOpenTrade`
6. `StartTradeEvent_VetoedByListener_PreventsUIOpen`

### SP.4 — `MerchantShopShowcase` scenario + scenario diag fixture (one commit)

Following the pattern of the 9 scenario diag fixtures shipped earlier
this session (CombatHooks / CombatParity / ThrowableTonics / etc).
Adds:

**New scenario** `Scenarios/Custom/MerchantShopShowcase.cs`:
- Player at center, 200 HP, 100 drams in pocket
- Merchant 2 cells east, stocked with: 3 tonics, 1 weapon, 1 piece of food
- Spare drams pile (a few coin items?) on floor for pickup demo

Menu entry: `Caves Of Ooo / Scenarios / Combat Stress / Merchant Shop Showcase` priority 116 (after the LK showcase at 115).

**New diag channel** for trade observability:

```csharp
DefaultOnCategories = { "event", "effect", "damage", "turn", "furniture", "trade" };
```

Hook `BuyFromTrader` / `SellToTrader` to record `trade/Bought` and
`trade/Sold` payloads:

```csharp
Diag.Record(category: "trade", kind: "Bought",
    actor: buyer, target: trader,
    payload: new { itemId = item.ID, itemName = item.GetDisplayName(),
                   price, drams_after = GetDrams(buyer), perf });
```

**Scenario diag tests** in `Tests/.../Scenarios/MerchantShopShowcaseDiagTests.cs`:

1. `Showcase_BuyFirstStockItem_RecordsTradeBought` — diag query returns 1 record after `TradeSystem.BuyFromTrader(player, merchant, stock[0])`
2. `Showcase_SellInventoryItem_RecordsTradeSold` — same for sell side
3. `Showcase_BuyAttempt_NoTradeTaggedItem_RecordsCanBeTradedVeto` — uses SP.2's CanBeTradedEvent; diag captures the veto
4. Counter-check: `Showcase_BuyWithoutEnoughDrams_NoTradeRecord`

**Smoke test** in `ScenarioCustomSmokeTests.cs`:
- `MerchantShopShowcase_Applies_WithoutThrowing`

### SP.5 — Self-review + roadmap update + merge + push

Per CLAUDE.md §3.4 cold-eye Q1-Q4 pass + roadmap update +
§2.3-template commit.

## Verification (post-implementation)

Three layers:

1. **Per-fixture RED → GREEN** during SP.2-4:
   - SP.2: 7 tests
   - SP.3: 6 tests
   - SP.4: 4 scenario tests + 1 smoke
   - **Total**: 18 new tests on top of existing 18 = 36 trade tests

2. **Targeted regression sweep**:
   ```
   run_tests EditMode group_names=[
     "TradeTests", "CanBeTradedEventTests", "TraderStateTests",
     "MerchantShopShowcaseDiagTests",
     "ScenarioCustomSmokeTests",
     "DiagOnApplyHookTests", "DiagDamageHookTests"
   ]
   ```
   Expected: 100/100 GREEN.

3. **Manual playtest** via the showcase scenario:
   - Click the menu entry
   - Bump the merchant + select "[Let's trade.]"
   - Buy an item, sell an item, observe drams + diag records
   - Try to sell the IronKey (now NoTrade-tagged) — refuses

## Critical files

### New files (SP.2-4)

| Path | Purpose |
|---|---|
| `Docs/SHOPPING-PARITY.md` | Plan doc (this file, SP.1) |
| `Assets/Scripts/Scenarios/Custom/MerchantShopShowcase.cs` | Showcase |
| `Assets/Tests/EditMode/Gameplay/Economy/CanBeTradedEventTests.cs` | SP.2 RED tests |
| `Assets/Tests/EditMode/Gameplay/Economy/TraderStateTests.cs` | SP.3 RED tests |
| `Assets/Tests/EditMode/Gameplay/Scenarios/MerchantShopShowcaseDiagTests.cs` | SP.4 |

### Modified files

| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/Economy/TradeSystem.cs` | SP.2 fire `CanBeTraded` event before existing `BeforeTrade`; SP.3 trader-state validation; SP.4 record `trade/Bought` and `trade/Sold` diag entries |
| `Assets/Scripts/Presentation/Input/InputHandler.cs` (or wherever OpenTrade hooks) | SP.3 fire `StartTradeEvent` |
| `Assets/Scripts/Shared/Utilities/Diag.cs` | SP.4 add `"trade"` to `DefaultOnCategories` |
| `Assets/Resources/Content/Blueprints/Objects.json` | SP.2 add `"NoTrade"` tag to `IronKey` |
| `Assets/Editor/Scenarios/ScenarioMenuItems.cs` | SP.4 menu entry priority 116 |
| `Assets/Tests/EditMode/Gameplay/Scenarios/ScenarioCustomSmokeTests.cs` | SP.4 smoke test |
| `Docs/CONTENT-ROADMAP.md` | SP.5 flip Trading entry to ✅ |

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `TradeSystem.BuyFromTrader/SellToTrader` | `Economy/TradeSystem.cs` | Hook AttemptUnlock-style pattern at top |
| `Diag.Record` (D2 substrate) | `Shared/Utilities/Diag.cs` | SP.4 trade events |
| `Diag.IsChannelEnabled` early-return guard | `Shared/Utilities/Diag.cs` | Hot-path safety |
| `entity.HasTag(string)` for `"NoTrade"` lookup | `Entity.cs` | SP.2 tag-based veto |
| `entity.GetPart<StatusEffectsPart>().HasEffect<T>()` | `Effects/StatusEffectsPart.cs` | SP.3 burning/stunned check |
| `ScenarioTestHarness.CreateContext(playerBlueprint: "Player")` | `Tests/.../ScenarioTestHarness.cs` | SP.4 fixture setup |
| `_harness.Factory.CreateEntity("Merchant")` | `Tests/.../ScenarioTestHarness.cs` | Spawn merchant from blueprint |

## Self-review pre-flagged 🟡 findings

These are designed-in tradeoffs to flag before committing — fix or
defer with a note per CLAUDE.md §5.

- **🟡 SP.2 — Tag vs Part vs Property.** Going with `"NoTrade"` tag
  rather than a new `NoTradePart`. Tags are cheaper and the only
  per-item state is the binary "is this NoTrade?" — no params needed.
  If quest items need richer metadata later (e.g., "soft-bound to
  player after step N of quest X"), promote to a Part. Document.
- **🟡 SP.3 — Trader-state list is not exhaustive.** Also-blocking
  states like Confused, Calmed, etc. are NOT listed. v1 covers
  Burning + Stunned + Frozen + Dead because those are the
  player-visible "obviously can't trade" states. Document the gap;
  expanding is cheap.
- **🟡 SP.3 — `StartTradeEvent` has no listeners in this ship.** The
  event fires but nothing handles it. This is intentional — it's a
  hook point for future content (services, quest gates). Counter-
  check test verifies a listener CAN veto, even though no production
  code does today.
- **🔵 SP.4 — Diag channel name `"trade"`.** Could be more granular
  (`commerce`, `trade-buy`, `trade-sell`). Going with `"trade"` for
  consistency with the other 5 channels (`event`, `effect`, `damage`,
  `turn`, `furniture` — all single-word, broad-noun).
- **⚪ Auto-inject `[Let's trade.]` overbroad** — current behavior
  injects a Trade choice on ANY speaker with InventoryPart, including
  random villagers carrying personal items. Qud narrows this to
  Trader-tagged entities. Pure scope decision: leave overbroad
  (lets the player trade with anyone) for v1, narrow if playtest
  feedback says it's annoying.
- **⚪ Out of scope:** TraderCreditExtended (Qud-specific); item
  ownership / theft (neither codebase has it); haggling skill
  beyond the existing Ego performance.

## Implementation sequence

```
1. Plan to disk (SP.1, this commit)
2. Verification sweep against current code — done above (table)
3. SP.2: CanBeTraded event + NoTrade tag + IronKey wiring + 7 tests
4. SP.3: Trader-state validation + StartTradeEvent + 6 tests
5. SP.4: MerchantShopShowcase + diag channel + 4 scenario tests + 1 smoke
6. Targeted regression sweep
7. Self-review + roadmap update + commit SP.5 + merge --no-ff + push
```

Expected total: ~80 lines new code (mostly TradeSystem hooks) +
~250 lines new tests + ~80 lines scenario + ~30 lines blueprint/menu
edits + this plan (~280 lines). ~½ day of focused work.

## What gets observable to the player after this ship

| Today | After SP |
|---|---|
| Player can trade with merchants (full UI works) | (same) — trading already shipped |
| Quest items / dungeon keys CAN be sold by mistake | NoTrade-tagged items refuse to be sold |
| Burning/stunned merchant accepts trade | Trade refused with "the merchant is on fire" message |
| Trade actions invisible to AI debugging | `trade/Bought` and `trade/Sold` diag records observable via `diag_query` MCP tool |
| 18 trade-related tests | 36 trade-related tests (18 existing + 18 new) |
| No end-to-end scenario verifying trade flow | `MerchantShopShowcaseDiagTests` pins the complete scenario contract |

After this ship, the natural follow-on candidates are:
- TraderCreditExtended buy-on-credit (Qud-parity polish)
- Auto-inject narrowing to Trader-tagged speakers (UX polish)
- Service trades (identify/repair/recharge) — listeners on StartTradeEvent

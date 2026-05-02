# Lock & Key — plan

> Tier 2 feature per `Docs/CONTENT-ROADMAP.md` (Lock & key 💡 → 🚧).
> Adds keyed-progression furniture: locked doors and chests that
> require an inventory key to open. Foundation for future dungeon
> progression (e.g., locked-down zones in caves).

## Goal

Player walks into a locked door. Move is blocked. The bump fires an
unlock attempt against the door's `LockPart`. If the actor has a
matching key in inventory, the door unlocks (and opens this turn);
the next move tick walks through. If no matching key, a "It's locked"
message logs and the move stays blocked.

User-visible payoff:
- Locked dungeon doors that gate progress until a key is found
- Locked chests that gate loot until a key is found
- A class of content (key drops) that becomes worth carrying

Out of scope (deferred):
- Lockpicking skill, lockpick item — Lock & Key v2
- Multi-key locks (3 different keys to open) — over-engineered for v1
- Trapped locks — pairs better with an existing trap rework
- Lock breaking via damage / acid — over-engineered for v1

## Verification sweep (pre-implementation)

| Premise | Status | Source |
|---|---|---|
| `MovementSystem.TryMoveTo` fires `BeforeMove` event with `TargetCell`, allows parts to veto by returning false from `HandleEvent` | ✅ confirmed | `MovementSystem.cs:111-153` |
| `PhysicsPart.HandleBeforeMove` iterates `targetCell.Objects`, sets `BlockedBy=other` when `otherPhysics.Solid` or `other.HasTag("Solid")`, returns false | ✅ confirmed | `PhysicsPart.cs:58-84` |
| `TryMoveEx` returns `(moved, blockedBy)` so callers can detect *what* blocked | ✅ confirmed | `MovementSystem.cs:50-105` |
| Inventory access pattern: `entity.GetPart<InventoryPart>().Objects` is `List<Entity>` enumerable | ✅ confirmed (existing tests) | `Tests/.../CombatContentAdversarialTests.cs:392` |
| GameEvent.New / SetParameter / FireEventAndRelease pattern is the canonical event flow | ✅ confirmed | `MovementSystem.cs:64-100` |
| TriggerOnStepPart precedent for furniture-on-step (similar to "furniture-on-bump") | ✅ confirmed | `Entities/TriggerOnStepPart.cs` |
| Diag substrate D2 hooks (effect/OnApply, damage/DamageDealt, turn/Begin/End) — no `furniture/UnlockAttempted` hook today | ⚠️ to add | `Shared/Utilities/Diag.cs` |
| No existing `DoorPart`, `LockPart`, or `KeyPart` in code or blueprints (`grep -rln`) | ✅ confirmed greenfield | n/a |
| Showcase scenario menu pattern: `[Scenario(...)]` attribute + `Apply(ctx)` + smoke test | ✅ confirmed | `Scenarios/Custom/TrapFurnitureShowcase.cs` |

**No false premises detected.** The only outstanding addition is a
new diag hook (`furniture/UnlockAttempted`) which lands in LK.4.

## Sub-milestones (smallest blast radius first)

### LK.1 — Plan + branch (this commit)

- **Plan to disk**: `Docs/LOCK-AND-KEY.md` (this file)
- **Branch** `feat/lock-and-key` cut from `main` at `272be99`

### LK.2 — `LockPart` + `KeyPart` data shapes (one commit)

**New files**:
- `Assets/Scripts/Gameplay/Items/LockPart.cs`
- `Assets/Scripts/Gameplay/Items/KeyPart.cs`

```csharp
public class LockPart : Part
{
    public override string Name => "Lock";
    public string KeyId = "";        // matches KeyPart.KeyId — empty = no requirement (no-op lock)
    public bool IsLocked = true;     // false = unlocked, won't refuse; true = require key
    // (no behavior in this commit — pure data + tests pin the shape)
}

public class KeyPart : Part
{
    public override string Name => "Key";
    public string KeyId = "";
}
```

**RED → GREEN tests** in `LockKeyDataShapeTests.cs`:
- `LockPart_DefaultsToLockedWithNoKey` — fresh instance has IsLocked=true, KeyId=""
- `KeyPart_DefaultsToEmptyKeyId` — fresh instance has KeyId=""
- `LockPart_KeyIdRoundTripsViaSerialization` — serializing + restoring preserves field values (mirror existing Part-serialization pattern)
- `KeyPart_KeyIdRoundTripsViaSerialization` — same

No production behavior yet. Pure data scaffold.

### LK.3 — Bump-unlock mechanism (one commit)

**Modify** `Assets/Scripts/Gameplay/Items/LockPart.cs`:

```csharp
public override bool HandleEvent(GameEvent e)
{
    if (e.ID == "AttemptUnlock")
    {
        var actor = e.GetParameter<Entity>("Actor");
        if (actor == null) return true;          // can't tell — pass through

        // No requirement → unlocked-as-noop, doesn't refuse
        if (!IsLocked || string.IsNullOrEmpty(KeyId))
        {
            IsLocked = false;
            e.SetParameter("Unlocked", true);
            return true;
        }

        // Look for matching KeyPart in actor's inventory
        var inv = actor.GetPart<InventoryPart>();
        if (inv != null)
        {
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                var key = inv.Objects[i]?.GetPart<KeyPart>();
                if (key != null && key.KeyId == KeyId)
                {
                    IsLocked = false;
                    e.SetParameter("Unlocked", true);
                    e.SetParameter("KeyUsed", (object)inv.Objects[i]);
                    MessageLog.Add($"{actor.GetDisplayName()} unlocks the {ParentEntity.GetDisplayName()}.");
                    return true;
                }
            }
        }

        // No matching key
        e.SetParameter("Unlocked", false);
        MessageLog.Add($"The {ParentEntity.GetDisplayName()} is locked.");
        return true;
    }

    if (e.ID == "BeforeMove")
    {
        // When the BUMP target is *us* and we're locked, fire AttemptUnlock
        // on this entity, then re-evaluate. If unlocked → drop our Solid
        // (or our parent's Solid via the Physics part it shares with us)
        // so the actor's PhysicsPart no longer blocks on our cell.
        // ...
        // NOTE: this hook needs to fire BEFORE PhysicsPart's BeforeMove
        // sees us as still-Solid. Implementation detail to verify in this
        // commit: order of HandleEvent across parts on the same entity.
    }

    return true;
}
```

**Order-of-operations nuance**: the locked-door entity has both
PhysicsPart (Solid=true) and LockPart. When the actor's BeforeMove
fires, the *actor's* PhysicsPart iterates the target cell's objects
and sees the door as Solid → veto. The actor never sees the door's
LockPart. To make bump-unlock work, the unlock attempt has to happen
either:

- **(A)** Inside the actor's PhysicsPart loop: when an Object is Solid,
  check if it has a LockPart, fire AttemptUnlock on the door, and if
  Unlocked=true, skip the veto. Tightest patch but couples
  PhysicsPart to LockPart.

- **(B)** As a separate "BumpInteraction" event the actor fires when
  blocked: if the move is vetoed by a Lockable entity, fire BumpInteract
  on the blocker. LockPart handles BumpInteract. Cleaner separation but
  needs a second event round-trip and a second move attempt.

**Recommendation: (A).** The PhysicsPart edit is a 5-line patch (check
for LockPart on a Solid blocker, fire AttemptUnlock, re-check Solid).
Keeps the move flow single-pass. Couples PhysicsPart to LockPart but
that's an acceptable coupling — the part already knows about the
target cell's Objects.

**RED → GREEN tests** in `LockKeyBumpUnlockTests.cs`:
- `BumpLockedDoor_NoKey_StaysLocked` — actor with empty inventory bumps locked door, move fails, IsLocked stays true
- `BumpLockedDoor_WithMatchingKey_Unlocks` — actor with KeyPart(KeyId="iron") in inventory bumps locked door (KeyId="iron"), IsLocked flips to false, move succeeds
- `BumpLockedDoor_WithWrongKey_StaysLocked` — actor has KeyPart(KeyId="brass"), door wants "iron", move blocked
- `BumpUnlockedDoor_NoKey_PassesThrough` — IsLocked=false, no KeyPart needed, move succeeds
- `BumpLockedDoor_WithKey_KeyStaysInInventory` — using a key doesn't consume it (master-key model — adjust if "single-use keys" are wanted later)
- Adversarial: `BumpLockedDoor_NullActor_NoCrash` — defensive

**Counter-checks**:
- `BumpLockedDoor_NoLockPart_BlocksAsBeforePLockKeyChange` — non-locked Solid blockers (regular walls) still block normally, no AttemptUnlock fires (verify diag substrate has no UnlockAttempted record)

### LK.4 — Door + Chest blueprints + diag hook (one commit)

**New JSON blueprints** in `Objects.json`:
- `LockedDoor` — Solid + LockPart(KeyId="iron", IsLocked=true) + Render (closed-door glyph)
- `IronKey` — Takeable item + KeyPart(KeyId="iron") + Render
- `LockedChest` — Solid + LockPart(KeyId="iron", IsLocked=true) + Container (deferred — for now, just a "pop a status message" interaction)

**Diag hook**: add a `furniture` channel with kind=`UnlockAttempted`:
```csharp
Diag.Record(category: "furniture", kind: "UnlockAttempted",
    actor: actor, target: ParentEntity,
    payload: new {
        keyId = KeyId,
        succeeded = unlocked,
        keyEntityId = keyEntity?.ID
    });
```

Mirrors the existing D2.1/D2.2 hook pattern. Channel registers in
`Diag.SetChannel("furniture", true)` defaults.

**RED → GREEN tests** in `LockKeyContentTests.cs`:
- `LockedDoorBlueprint_HasLockPart_WithIronKeyId`
- `IronKeyBlueprint_HasKeyPart_WithIronKeyId`
- `BumpLockedDoor_FiresFurnitureDiagRecord_OnSuccess` — diag query returns 1+ records with kind=UnlockAttempted, succeeded=true
- `BumpLockedDoor_FiresFurnitureDiagRecord_OnFailure` — diag query returns 1+ records with kind=UnlockAttempted, succeeded=false

### LK.5 — `LockedDoorShowcase` scenario (one commit)

**New file**: `Assets/Scripts/Scenarios/Custom/LockedDoorShowcase.cs`

Layout (player at center; coordinates relative):
```
. . [Wall] [Wall] [Wall] [Wall] [Wall]
. . [LockedDoor] . . . [Chest] . . .
[Player] →
. . [Wall] [Wall] [Wall] [Wall] [Wall]
```

Player loadout:
- HP 200, Strength 24
- IronKey (1) in inventory
- 1 IronKey on the floor 2 cells south (pickup demo)

Flow the player exercises:
1. Bump LockedDoor → door unlocks, "[unlock] You unlock the locked door"
2. Walk through to the chest
3. Bump LockedChest → chest unlocks (same key reused — master-key model)

Menu entry: `Caves Of Ooo / Scenarios / Dungeon Crawl / Locked Door Showcase` priority 200.

**RED → GREEN test** in `ScenarioCustomSmokeTests.cs`:
- `LockedDoorShowcase_BuildsWithoutThrowing`

**Scenario diag fixture** in `Tests/.../LockedDoorShowcaseDiagTests.cs`
following the pattern from CombatHooksShowcaseDiagTests:
- `BumpDoorWithKey_UnlocksAndAdvances` — bump → diag records UnlockAttempted, succeeded=true; second move passes through
- `BumpDoorWithoutKey_StaysLocked` — drop the IronKey before bumping → still blocked, diag records UnlockAttempted, succeeded=false
- Counter-check: `BumpRegularWall_NoUnlockDiagRecord` — bump a regular Solid wall → no `furniture/UnlockAttempted` records appear

### LK.6 — Self-review + commit + merge + push + roadmap update

- Severity-marked findings (🔴/🟡/🔵/🧪/⚪) per CLAUDE.md §5
- Cold-eye Q1-Q4 pass per CLAUDE.md §3.4
- Update `Docs/CONTENT-ROADMAP.md`: flip Lock & key 💡 → ✅ + commit hash
- Commit per §2.3 template
- Merge to main + push

## Critical files

### New files (LK.2-5)

| Path | Purpose |
|---|---|
| `Docs/LOCK-AND-KEY.md` | Plan doc (this file, LK.1) |
| `Assets/Scripts/Gameplay/Items/LockPart.cs` | LockPart data + bump-unlock handler |
| `Assets/Scripts/Gameplay/Items/KeyPart.cs` | KeyPart data |
| `Assets/Scripts/Scenarios/Custom/LockedDoorShowcase.cs` | Manual playtest scenario |
| `Assets/Tests/EditMode/Gameplay/Items/LockKeyDataShapeTests.cs` | LK.2 RED tests |
| `Assets/Tests/EditMode/Gameplay/Items/LockKeyBumpUnlockTests.cs` | LK.3 RED tests |
| `Assets/Tests/EditMode/Gameplay/Items/LockKeyContentTests.cs` | LK.4 blueprint-shape + diag tests |
| `Assets/Tests/EditMode/Gameplay/Scenarios/LockedDoorShowcaseDiagTests.cs` | LK.5 scenario diag fixture |

### Modified files

| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/PhysicsPart.cs` | LK.3: Solid-blocker loop checks for LockPart, fires AttemptUnlock, skips veto on success (~5 lines) |
| `Assets/Scripts/Shared/Utilities/Diag.cs` | LK.4: register `furniture` channel default-on |
| `Assets/Resources/Content/Blueprints/Objects.json` | LK.4: LockedDoor, IronKey, LockedChest blueprints |
| `Assets/Editor/Scenarios/ScenarioMenuItems.cs` | LK.5: Locked Door Showcase menu entry |
| `Assets/Tests/EditMode/Gameplay/Scenarios/ScenarioCustomSmokeTests.cs` | LK.5: showcase smoke test |
| `Docs/CONTENT-ROADMAP.md` | LK.6: flip Lock & key to ✅, recently shipped entry |

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `entity.FireEventAndRelease(GameEvent)` | `Entity.cs:271-276` | Canonical event flow |
| `Cell.Objects` enumeration | `Cell.cs` | Iterating target cell occupants in PhysicsPart |
| `entity.GetPart<TPart>()` generic accessor | `Entity.cs:62` | Locating LockPart / KeyPart |
| `InventoryPart.Objects` enumeration | `InventoryPart.cs` | Inventory key search |
| `Diag.Record(category, kind, actor, target, payload)` | `Diag.cs` | LK.4 diag hook |
| `MessageLog.Add(string)` | `MessageLog.cs` | Player feedback ("You unlock...", "It's locked.") |
| `[Scenario(...)]` attribute + `IScenario.Apply(ctx)` | `Scenarios/IScenario.cs` | LK.5 showcase pattern |
| `ScenarioTestHarness.CreateContext(playerBlueprint: "Player")` | `Tests/.../ScenarioTestHarness.cs` | Test scaffold |

## Self-review pre-flagged 🟡 findings

These are designed-in tradeoffs to flag before committing — fix or
defer with a note per CLAUDE.md §5.

- **🟡 PhysicsPart-LockPart coupling** — LK.3 option (A) couples
  PhysicsPart to LockPart via a `GetPart<LockPart>()` check. Acceptable
  because PhysicsPart already enumerates target-cell objects for the
  Solid check. Alternative (B) decouples but doubles the move
  round-trip. Document the choice in LK.6 self-review.
- **🟡 Single-key vs single-use** — v1 keys are reusable (master-key
  model). If we want single-use keys later, add a `Consumable` flag
  on KeyPart. Not blocking; document.
- **🔵 Diag channel name `furniture`** — broader than just doors/locks.
  Future furniture interactions (lever pulls, fountain drinks, sign
  reads) can share this channel. Consider naming `interaction` or
  `furniture/lock` if specificity is wanted; defer.
- **⚪ No lockpicking, no trap, no break** — pure scope discipline.
  Each is a separate ship.

## Verification (post-implementation)

Three layers:

1. **Per-fixture RED → GREEN cycles** during LK.2-5:
   - LK.2: 4 tests (data shape + roundtrip)
   - LK.3: 6 tests (3 positive + 1 wrong-key + 1 unlocked + 1 null-actor adversarial; counter-check via no-LockPart wall)
   - LK.4: 4 tests (2 blueprint-shape + 2 diag)
   - LK.5: 3 scenario diag tests + 1 smoke

2. **Combat + scenario regression sweep** after LK.5:
   ```
   run_tests EditMode group_names=[
     "LockKeyDataShapeTests", "LockKeyBumpUnlockTests",
     "LockKeyContentTests", "LockedDoorShowcaseDiagTests",
     "MovementSystemTests", "PhysicsPartTests",
     "ScenarioCustomSmokeTests"
   ]
   ```
   Expected: 100%/100% pass.

3. **Manual playtest** via the showcase scenario (LK.5):
   - Click `Caves Of Ooo / Scenarios / Dungeon Crawl / Locked Door Showcase`
   - Bump locked door → unlock message + door yields
   - Walk through to chest → bump → unlock message
   - Drop IronKey, bump locked door → "It's locked." stays blocked
   - Honesty bounds: visual confirmation of door tile changing glyph (closed → open) is a manual-only check.

## Implementation sequence

```
1. Plan to disk (this commit, LK.1)
2. Verification sweep against current code — done above
3. LK.2: LockPart + KeyPart data shapes + 4 RED tests → GREEN
4. LK.3: Bump-unlock mechanism via PhysicsPart hook + 6 tests → GREEN
5. LK.4: Blueprints + diag furniture channel + 4 tests → GREEN
6. LK.5: Showcase scenario + smoke + 3 scenario diag tests → GREEN
7. Combat-scenario regression sweep
8. Self-review + roadmap update + commit LK.6 + merge + push
```

Expected total: ~120 lines of new code + ~250 lines of new tests +
~80 lines of scenario + ~20 lines of new blueprints + this plan
(~250 lines). ~½ day of focused work.

## What gets observable to the player after this ship

| Today | After LK |
|---|---|
| Walls block movement; no exceptions | Walls block; locked doors block UNLESS you have the key, then they yield + log "You unlock the [door]." |
| Keys don't exist as a content concept | Keys are pickable items with a KeyId; matching ID = open matching lock |
| No mechanic gates loot or progression | Locked chests gate loot; locked dungeon doors gate progression |
| Diag substrate has 4 hooks (effect/OnApply, damage/DamageDealt, turn/Begin, turn/End) | + furniture/UnlockAttempted (5th hook) — every unlock-attempt observable to AI debugging |

After this ship, the next natural Tier-2 candidates are PressurePlate
(the trap-furniture ship had this deferred — pairs with traps now)
and Hunger/Food (gameplay loop pacing).

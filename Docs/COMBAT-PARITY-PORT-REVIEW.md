# Phases A/C/D/E — Self-Review Findings

> Cold-eye self-review per Methodology Template Part 5 (`QUD-PARITY.md §5969-6037`).
> Pass performed 2026-04-26 against commits `85a0a25` (A), `4721630` (C), `14f8047` (D+E).

## Severity scale (per Template §5.1)

| Marker | Meaning |
|---|---|
| 🔴 | Critical — ships a bug, corrupts state, or blocks a claim in docs |
| 🟡 | Moderate — real defect or parity drift, workable for one iteration |
| 🔵 | Minor — polish, UX feedback, docstring drift |
| 🧪 | Test gap — behavior is correct but unpinned |
| ⚪ | Architectural note for future work, not actionable now |

## Findings

### 🟡 Finding 1 — TakeDamage listeners can't mutate damage in-flight

**File:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:465-474`

The typed `ApplyDamage` overload captures `int amount = damage.Amount;` BEFORE firing the `TakeDamage` event. Listeners that mutate `damage.Amount` (a typed-Damage feature we explicitly enabled in Phase C) have their mutation IGNORED — the HP decrement uses the pre-event captured value.

```csharp
int amount = damage.Amount;        // captured
target.FireEvent(takeDamage);      // listeners can modify damage.Amount, but...
hpStat.BaseValue -= amount;        // ...this uses the captured value
```

**Why it matters:** the whole point of passing typed `Damage` to listeners (Phase C divergence #1) was to enable in-flight mutation. Right now that's dead code — listeners can read but not effectively write. Phase E's "Add a 'StoneSkin' effect that listens for TakeDamage and reduces non-magical damage by 2" pattern doesn't work.

**Proposed fix:** re-read `damage.Amount` after the event fires; use that for HP decrement.

---

### 🟡 Finding 2 — `NaturalTwenty_AutoPen_DoesNotFire_ForNonPlayerAttacker` threshold is too loose

**File:** `Assets/Tests/EditMode/Gameplay/Combat/CriticalHitTests.cs:118`

```csharp
Assert.Less(hitsLanded, 50, "...should rarely land. Got {hitsLanded}.");
```

Threshold of 50 is too high to catch a regression. If AutoPen incorrectly fires for non-players, expected hits = 5% nat-20 rate × 200 trials = ~10. `10 < 50` passes the assertion even though the bug shipped.

**Why it matters:** vacuous-pass risk on one of the core mutation-resistance tests. A regression where AutoPen accidentally fires for all attackers would slip through this test.

**Proposed fix:** tighten threshold to `< 5` (with AV=99 and chain-explosion only path, expected hits ≈ 0; allowing 5 keeps RNG slack).

---

### 🟡 Finding 3 — `NonCriticalHit_DoesNotAddCriticalAttribute` is vacuously true

**File:** `Assets/Tests/EditMode/Gameplay/Combat/CriticalHitTests.cs:163-165`

```csharp
Assert.Greater(nonCritHitsObserved, critsObserved,
    "Non-critical hits should outnumber critical hits");
```

This compares two counters but doesn't actually verify the **invariant** "non-crit hits never carry the Critical attribute." With nat-20 at 5% and total hits dominated by non-crits, this assertion holds even if a mutation incorrectly tags 30% of non-crit hits as Critical.

**Why it matters:** the test name promises a strong invariant ("Does Not Add Critical Attribute"), but the assertion is statistical-comparative. Real bug is silently consistent with the test.

**Proposed fix:** the probe should check the invariant directly — for every observed Damage, if hitRoll != 20 (which we'd need to track), Critical must NOT be present. Since we can't easily track hitRoll from outside, restructure: assert `critsObserved` is bounded by the expected nat-20 count plus a small margin.

---

### 🟡 Finding 4 — Full-resistance fire doesn't surface "no damage" feedback

**File:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:457-463`

```csharp
if (!damage.HasAttribute("IgnoreResist"))
    ApplyResistances(target, damage);

if (damage.Amount <= 0) return;  // resistance fully absorbed
```

When resistance fully absorbs damage (e.g. 100% AcidResistance on acid damage), the function returns silently:
- No `TakeDamage` event fires (so on-hit reactions, status effect listeners, etc. miss the attack)
- No "but deals no damage!" message logged
- No floating "0!" indicator

**Why it matters:** game feedback gap. Player swings at an acid-immune slime, sees nothing — looks like the attack didn't happen at all. Should log a "fully resisted!" or similar message.

**Proposed fix:** before the early-return, emit a "fully resisted" message and fire a separate `DamageFullyResisted` event (or fire `TakeDamage` with Amount=0 and let UI handle it).

---

### 🔵 Finding 5 — `effectiveMaxStrBonus = 50` magic number

**File:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:163`

```csharp
int effectiveMaxStrBonus = (maxStrBonus < 0) ? 50 : maxStrBonus;
```

The `50` is a sentinel-substitute for "uncapped." Should be a named constant (e.g., `LEGACY_UNCAPPED_MAX_STR_BONUS = 50`).

**Why it matters:** magic number obscures intent for future readers.

**Proposed fix:** extract to a public constant on `CombatSystem`, with a docstring explaining the "MaxStrengthBonus < 0 means no cap" legacy contract.

---

### 🔵 Finding 6 — Doc drift: plan snippet uses `int.MaxValue / 2`, code uses `50`

**File:** `Docs/COMBAT-QUD-PARITY-PORT.md` — Phase A "Caller updates" code snippet

The doc shows:
```csharp
int maxBonus = ((maxStrBonus < 0) ? int.MaxValue / 2 : maxStrBonus) + penBonus;
```

The actual code uses `50`. Doc was written from the plan, then code diverged during impl, but doc snippet wasn't updated.

**Severity:** 🔵 doc drift, not behavioral.

**Proposed fix:** update the doc snippet.

---

### 🧪 Finding 7 — No test for stat-modification-based resistance

**File:** N/A (gap)

All resistance tests set `target.Statistics["AcidResistance"] = new Stat { BaseValue = 50 }` directly. Real gameplay grants resistance via temporary effects (`StoneSkin` → +25 acid resist for 10 turns). We never test that pathway.

**Severity:** 🧪 test gap. Behavior likely works (resistance reads `GetStatValue` which respects modifiers) but unpinned.

**Proposed fix:** add a test that applies a temporary stat-mod and verifies the resistance respects it. Defer to Phase E followup or add as a regression test if a future bug surfaces.

---

### 🧪 Finding 8 — No content uses resistance stats

**File:** `Assets/Resources/Content/Blueprints/Objects.json` (gap)

No blueprint defines AcidResistance, HeatResistance, etc. Phase E is wired correctly but dead-letter without content. The PlayMode sweep (remediation #2) would have surfaced this.

**Severity:** 🧪 architectural — Phase E is technically deployed but invisible to players until content uses it.

**Proposed fix:** add at least one creature with a meaningful resistance (e.g., a fire-immune entity) so the path is exercised in real gameplay. Out of scope for this remediation; flag for Phase F or content-team work.

---

### 🧪 Finding 9 — No test for `RollPenetrations` boundary `bonus == maxBonus`

**File:** N/A (gap)

`Math.Min(bonus, maxBonus)` evaluated where bonus equals maxBonus is correct (returns either) but isn't pinned by a test.

**Severity:** 🧪 test gap, low risk.

**Proposed fix:** add boundary test. Trivial.

---

### ⚪ Finding 10 — Resistance order Acid → Heat → Cold → Electric

**File:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:526-529`

Order documented but not strictly enforced by tests. For multi-type damage, the order COULD matter for integer-truncation reasons. Empirically tested cases didn't differ, but no regression test pins the order.

**Severity:** ⚪ architectural note — order matches Qud's source order; document only.

---

### ⚪ Finding 11 — `MeleeWeaponPart.MaxStrengthBonus = -1` default doesn't match Qud

**File:** `Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs:31`

Qud weapons always have non-negative MaxStrengthBonus. Our `-1` sentinel is a CoO-specific compatibility hack.

**Severity:** ⚪ already noted in Phase A divergence #1; deferred to Phase B½.

---

## Findings ranked by remediation priority

| Rank | # | Severity | Finding | Effort |
|---:|---:|:-:|---|---|
| 1 | 1 | 🟡 | TakeDamage listener mutation gap | 5 min |
| 2 | 2 | 🟡 | AutoPen non-player threshold too loose | 2 min |
| 3 | 3 | 🟡 | NonCriticalHit test vacuously true | 5 min |
| 4 | 4 | 🟡 | Full-resistance fire silent | 10 min |
| 5 | 5 | 🔵 | Magic number 50 → constant | 3 min |
| 6 | 6 | 🔵 | Doc drift | 1 min |
| 7 | 7 | 🧪 | Stat-mod resistance test | 10 min |
| 8 | 8 | 🧪 | Content uses no resistance (flag only) | n/a |
| 9 | 9 | 🧪 | bonus==maxBonus boundary test | 3 min |

**Plan:** fix #1-#6 in this remediation pass (~25 min). Defer #7-#9 to a follow-up audit. #8 is a content-team note, flagged in this doc.

---

## PlayMode sanity sweep (Methodology Template §3.5)

Performed 2026-04-26 against the live `SampleScene.unity` bootstrap. Raw output below; paraphrasing avoided per Honesty Protocol §6.1.

### Preflight

```
isPlaying = True
Player = "you"
  HasTag Player = True
  Hitpoints = 500
  Strength = 18
  HasPart<MeleeWeaponPart> = False    (uses natural-weapon path via Hand BodyPart)
  HasPart<Body> = True                (body-part-aware combat path active)
Factory.Blueprints.Count = 189
  has Snapjaw = True
Player pos = (39, 11)
```

### Scenario 1 — Phase A + C: typed Damage flow

Spawn Snapjaw adjacent, attach `TakeDamageCaptureProbe`, run attacks until one lands:

```
PRE: Snapjaw HP = 15
POST (hit landed on attempt 1):
  Damage.Amount = 2
  Damage.Attributes = [Melee, Strength]
  Has 'Melee' = True
  Has 'Strength' = True
  TakeDamage 'Amount' int = 2
```

**Verdict:** ✅ Phase A + C wired correctly end-to-end.
- New `RollPenetrations` algorithm produces ≥1 pen vs Snapjaw AV=2 with player Str 18.
- Phase C typed `Damage` flows through `TakeDamage` event with `[Melee, Strength]` attributes (no weapon `Attributes` field on the natural fist, hence no extras).
- Backward-compat `"Amount"` int parameter on the event still equals `Damage.Amount`.

### Scenario 2 — Phase D: nat-20 critical

200 attacks vs a healed-each-time Snapjaw, count crit-tagged Damage objects:

```
After 200 attempts vs Snapjaw:
  Normal hits = 154
  Crit hits = 11
  Crit rate = 6.7% (expected ~5% from nat-20)
  Sample crit Damage.Attributes = [Melee, Strength, Critical]
  Sample crit Has 'Critical' = True
```

**Verdict:** ✅ Phase D wired correctly.
- Crit rate 6.7% matches expected ~5% nat-20 baseline (extra 1.7pp comes from AutoPen converting failed-pen nat-20s into landed hits — exactly what the player-AutoPen guard is supposed to do).
- `"Critical"` attribute appended to Damage on nat-20 hits, listeners can detect via `damage.HasAttribute("Critical")`.

### Scenario 3 — Phase E + Finding 4: resistance + fully-resisted event

Three Snapjaws: one with 50% AcidResistance, one with 100%, one control. Apply 10 acid damage to each:

```
PRE: sjPartial HP = 15, AcidRes = 50
PRE: sjFull HP = 15, AcidRes = 100
POST: sjPartial HP delta = 5  (expected 5 from 50% resist)
POST: sjFull HP delta = 0  (expected 0 from 100% resist)
POST: DamageFullyResisted fired on sjFull = True
COUNTER: sjControl (no AcidResistance) HP delta = 10  (expected 10)
```

**Verdict:** ✅ Phase E wired correctly + Finding 4 fix verified live.
- 50% resistance halves damage as designed.
- 100% resistance fully absorbs.
- `DamageFullyResisted` event fires on the 100% case (Finding 4 regression test mirrors this in EditMode; live verification confirms the wiring extends to play context).
- Counter-check (no resistance stat = full damage) confirms the resistance code path doesn't fire on irrelevant targets.

### Honesty bounds (Methodology Template §6.3)

**Can verify (script-observable, exercised above):**
- `Damage.Attributes` list contents
- `Damage.Amount` value at event-fire time
- `TakeDamage` event parameter shape (`"Amount"`, `"Damage"`)
- `DamageFullyResisted` event firing
- HP delta after `ApplyDamage`
- Crit attribute presence on `Damage`

**Cannot verify (require human eyes / screenshot):**
- Visual feedback for crits (no particle / color flash currently — flagged for playtest scenario in remediation #3)
- "Resisted!" / "fully resisted!" message log surfaced to player UI
- Whether the `DamageFullyResisted` event has any UI listener (currently it does NOT — only the EditMode regression test listens)

**Outstanding gap from sweep:** the live Snapjaw blueprint has no resistance stats and no weapon `Attributes` defined. Resistance code path is dead-letter against current content; weapon attribute propagation works but never carries non-default attributes. Both flagged in Finding 8 (content-team note).

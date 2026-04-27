# Combat Tier 2 — Make the port visible in gameplay

> Companion to `Docs/COMBAT-QUD-PARITY-PORT.md`. The combat port shipped
> the mechanics; this doc covers the **content + UX** changes that make
> them observable in actual gameplay.

## Goal

Convert the Phase A-H combat port from "infrastructure that exists" to
"infrastructure shipping in gameplay." Every player swing, every elemental
attack, every dismemberment-vetoing buff should now produce visible
feedback or have meaningful content using it.

## Scope

| In | Out |
|---|---|
| Crit-hit MessageLog feedback | Visual particle FX for crits |
| Damage attributes on a representative subset of weapons (~5-8) | Damage attributes on every weapon (deferred to content team) |
| Resistance stats on 2-3 thematic creatures | Resistance stats on every creature (deferred) |
| One production listener for `BeforeTakeDamage` (Stoneskin effect) | Generic "all status effects can hook BeforeTakeDamage" framework (Tier 4 work) |

## Pre-impl verification sweep

Read targets: `Assets/Resources/Content/Blueprints/Objects.json`,
`Assets/Scripts/Gameplay/Effects/Effect.cs`,
`Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs`,
`Assets/Scripts/Shared/Utilities/MessageLog.cs` (actually at `Gameplay/Events/MessageLog.cs`).

| Check | Finding |
|---|---|
| `MessageLog` API | `MessageLog.Add(string)` — single-arg, plain text. No color/format codes. |
| Existing weapon blueprint shape | Uses `MeleeWeapon` part with params `BaseDamage`, `PenBonus`, `MaxStrengthBonus`, `Stat`. Adding `Attributes` is auto-loaded via `EntityFactory.ApplyParameters` reflection (Phase C confirmed). |
| Existing creature blueprint shape | `Stats` array on each. Adding new stats like `AcidResistance` follows existing pattern; `entity.GetStatValue(name, 0)` reads them at runtime. |
| `Effect` base class hooks | Has `OnTakeDamage(Entity, GameEvent)` virtual. **Does NOT have `OnBeforeTakeDamage`.** |
| `StatusEffectsPart.HandleEvent` | Routes `TakeDamage` → `OnTakeDamage` per effect. **Does NOT route `BeforeTakeDamage` yet.** Adding this is required for Stoneskin to use the Phase F hook through the Effect system. |

**Sweep correction:** the original Tier 2 sketch claimed Stoneskin would be
"~45 min" assuming an Effect base hook for BeforeTakeDamage already
existed. It doesn't. Adding the hook is a small extension to the existing
event-routing pattern (mirrors how `OnTakeDamage` is already routed) but
needs documenting as a code change beyond just blueprints.

## Sub-milestone breakdown (smallest blast radius first, per §1.4)

| Sub | Scope | Blast radius | Tests | Est. time |
|---|---|---|---|---|
| **T2.1** | Critical-hit MessageLog line | 1 line in `PerformSingleAttack` | 2 unit | ~10 min |
| **T2.2** | Damage attributes on 6-8 representative weapons | JSON-only | 2-3 smoke (factory load + propagate) | ~20 min |
| **T2.3** | Resistance stats on 2-3 thematic creatures | JSON-only | 2-3 smoke (stat present + behaves) | ~15 min |
| **T2.4** | `StoneskinEffect` + `OnBeforeTakeDamage` hook in Effect base + routing in StatusEffectsPart | New file + 2 small edits to existing event-routing | 5-7 unit | ~45 min |

Each commits independently. Each is revertable without affecting later sub-milestones.

## Per-sub-milestone TDD plan

### T2.1 — Crit message in MessageLog

**Invariant in user-visible terms:** "When the player rolls a natural-20
hit that lands successfully, the message log contains a line indicating
the hit was critical (e.g., `'you CRITICALLY hits the snapjaw...'` or
similar)."

- RED: 1 test — find a seed that produces a nat-20 hit in `PerformMeleeAttack`,
  assert at least one log line contains "CRITICAL" / "critical" / equivalent.
- Counter-check: identical seed but with the player tag REMOVED (so AutoPen
  doesn't fire, ensuring we observe a non-crit case) — assert no "critical"
  in log.
- Implementation: 1-2 lines in `PerformSingleAttack` between the damage roll
  and the standard hit message.

### T2.2 — Weapon damage attributes (JSON only)

**Invariant:** "A weapon's `Attributes` string in its blueprint propagates
into the `Damage.Attributes` list when the weapon strikes."

Picks (representative cross-section):

| Weapon | Attributes |
|---|---|
| `Dagger` | `Piercing` |
| `ShortSword` | `Cutting LongBlades` |
| `Battleaxe` | `Cutting Axes` |
| `Cudgel` | `Bludgeoning` |
| `Warhammer` | `Bludgeoning Cudgel` |
| `Sporeblade` | `Cutting Spore` |
| `DissolutionMaul` | `Bludgeoning Acid` |
| `SnapjawClaw` (natural) | `Cutting Piercing Animal` |

- RED → GREEN: load the weapon blueprint, equip it, perform an attack,
  capture `Damage` via `TakeDamageCaptureProbe`, verify `damage.HasAttribute("Cutting")`
  for `ShortSword`, etc.
- Counter-check: a weapon WITHOUT the attribute (e.g., `Dagger` doesn't have "Bludgeoning") → propagation correctly excludes the absent attribute.

### T2.3 — Resistance stats on thematic creatures

**Invariant:** "A creature with `HeatResistance = N` in its blueprint takes
proportionally less Fire-attributed damage when struck."

Picks:

| Creature | Stat | Reasoning |
|---|---|---|
| `Glowmaw` | `HeatResistance: 50` | Already phosphorus-themed, glows; resists its own element |
| `Snapjaw` | `ColdResistance: 25` | Cold-tolerant scavenger archetype (mild) |
| `SnapjawHunter` | `ColdResistance: 50` (overrides parent) | Tougher elite version |

(Could add more — kept to 3 to limit scope.)

- RED → GREEN: load the blueprint, apply elemental damage with matching
  attribute, verify HP delta = `damage × (1 − resistance/100)`.
- Counter-check: same creature, MISMATCHED damage type → full damage.

### T2.4 — Stoneskin effect

**Invariant:** "An entity with the `Stoneskin` effect takes 2 less damage
per incoming hit (clamped to 0). The effect applies via `BeforeTakeDamage`
so resistance still applies on top of it."

- Add `public virtual void OnBeforeTakeDamage(Entity, GameEvent) { }` to
  `Effect.cs`.
- Add `if (e.ID == "BeforeTakeDamage") HandleBeforeTakeDamage(e); return true;`
  branch to `StatusEffectsPart.HandleEvent`, mirroring `HandleTakeDamage`.
- New `Assets/Scripts/Gameplay/Effects/Concrete/StoneskinEffect.cs` —
  finite-duration effect that overrides `OnBeforeTakeDamage` to subtract
  a configurable `Reduction` (default 2).

Tests (5-7):
- RED → GREEN: target with Stoneskin takes 10 dmg → HP delta 8.
- Counter-check: same target without Stoneskin → HP delta 10.
- Stoneskin + 50% AcidResistance + acid damage = 10 → 8 (Stoneskin) → 4 (resist).
- Stoneskin reduces 1 dmg to 0 (clamp at 0).
- Stoneskin doesn't break the typed `Damage` attribute list.
- Stoneskin's `OnBeforeTakeDamage` doesn't fire on already-dead targets.
- Adversarial: two Stoneskin instances stack? (Decision: yes, additive — `Reduction` sums per-effect-application like Bleeding.)

## Verification checklist

- [x] T2.1 commit (`a70ac41`): crit message in MessageLog with `CRITICAL_HIT_TAG` constant. 2 tests, full suite 2183 green.
- [x] T2.2 commit (`a9a8ada`): 4 weapon blueprints + 7 natural weapons declare `Attributes`. 8 tests, full suite 2191 green.
- [x] T2.3 commit (`885e13a`): Glowmaw HeatResistance:50, Snapjaw ColdResistance:25, SnapjawHunter override:50. 6 tests, full suite 2197 green.
- [x] T2.4 commit (`49f16ca`): `Effect.OnBeforeTakeDamage` virtual + `StatusEffectsPart` routing + `StoneskinEffect` class. 8 tests, full suite 2205 green.
- [x] Full EditMode suite green after each commit (no regressions)
- [x] Implementation log captured per-commit + this checklist

## Final results

| Sub | Commit | Tests added | Suite total | Notes |
|---|---|---:|---:|---|
| T2.1 | `a70ac41` | 2 | 2183 | Crit MessageLog feedback |
| T2.2 | `a9a8ada` | 8 | 2191 | 4 weapons + 7 natural with Attributes |
| T2.3 | `885e13a` | 6 | 2197 | 3 creatures with resistance stats |
| T2.4 | `49f16ca` | 8 | 2205 | Stoneskin + Effect.OnBeforeTakeDamage |
| **Total** | — | **+24** | — | All 4 sub-milestones independently revertable |

Net: combat port is now player-visible — crits log, weapons carry typed
attributes, themed creatures resist matching elements, and the first
production listener (Stoneskin) demonstrates the BeforeTakeDamage pattern
for future status effects (MagicShield, ScaleBark, etc.).

## Deferrals flagged for future content / polish

- More weapons need `Attributes` (Battleaxe, Sporeblade, EchoKnife, etc. — most exotic blueprints unset)
- More creatures need resistance stats (current 3 is a starting set; ~50 more creatures exist)
- No creature has *negative* resistance (vulnerability) yet — Phase E vulnerability path lacks gameplay validation
- Stoneskin has no per-hit "absorbed!" log line (would add noise; deferred)
- No visual feedback (color/particle) for crits / resisted / fully-resisted events

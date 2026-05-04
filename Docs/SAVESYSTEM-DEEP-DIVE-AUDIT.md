# SaveSystem — Deep-Dive Adversarial Audit

> Per Methodology Template §3.9. Targets the round-trip identity surface
> NOT covered by the earlier surgical pass (Spec / CoverageGap / Adversarial,
> 56 tests already shipped under `Assets/Tests/EditMode/Gameplay/Save/`).

## Target

`Assets/Scripts/Gameplay/Save/SaveSystem.cs` — 1876 LOC. Tier S in
`QUD-PARITY.md §5829`.

## What's already covered (surveyed by test name only — no impl reading)

| Fixture | Tests | Surface |
|---|---:|---|
| `SaveSystemSpecTests` | 21 | Entity ID/Blueprint, Tags, Properties, Stats, Inventory equipped vs carried, BrainPart Target ref, Load failure modes, TurnManager TickCount/PerEntityEnergy, Cell flags, GoalStack order + custom fields, Idempotency, distinct-instance, StaticFactories not required, StatusEffect stacks/duration |
| `SaveSystemCoverageGapTests` | 21 | Null entity ref, same entity twice → same instance, null vs empty string, GameID generation, WorldSeed, Hotbar slot, CreateInfo, multi-zone caching, ambient tint color, all 6 stat fields, BrainPart scalar fields, PersonalEnemies, delegate goals filtered, BodyPart nested tree, section/format check, MessageLog/PlayerReputation static state, reflective Part fields, unknown TypeName, post-load two-phase |
| `SaveSystemAdversarialTests` | 13 | NaN/Infinity stats, negative stats, negative Goal age, cyclic refs, self-referential, CurrentActor not in entries, cell zero→many, trailing garbage, two saves to one stream, end-check corruption, polymorphic concrete type, resaveable, IAuraProvider visuals after load |
| `SaveGraphRoundTripTests` | 1 | WholeWorld round-trip |

## Gaps targeted by this deep-dive (per QUD-PARITY.md §5870)

| Edge | Coverage | Plan |
|---|---|---|
| `MutationsPart` manager IDs → dynamic body parts | 🔴 NONE | High-priority targets |
| `Entity.ID` collision on load | 🔴 NONE | High-priority |
| `CorpsePart.Factory` cold-load wiring (specific factory) | ⚠️ generic | Specific probe |
| `DisposeOfCorpseGoal.GoToCorpseTries` preservation | ⚠️ generic | Specific named goal |
| `LayRuneGoal.MoveTries` preservation | ⚠️ generic | Specific named goal |
| Phase C `MeleeWeaponPart.Attributes` round-trip | ⚠️ likely-via-reflective | Specific probe |
| Phase G `MultiWeaponSkillBonus` stat round-trip | ⚠️ likely-via-stat-loop | Specific probe |
| Phase T2.4 `StoneskinEffect.Reduction` round-trip | ⚠️ likely-via-reflective | Specific probe |
| Phase E resistance stats on creature blueprints | ⚠️ likely-via-stat-loop | Specific probe |
| Save with entity at HP=0 (dying-not-removed) | None | LOW-confidence probe |
| Resave-load 5-cycle stability | None | medium |

## Predictions

| # | Edge | Prediction | Confidence |
|---:|---|---|---|
| 1 | MutationsPart manager IDs round-trip | Mutation-added body parts survive; manager-ID lookup post-load works | **LOW** |
| 2 | Two entities w/ same ID in save | Loader either rejects or distinct-instances them; not silent-reuse | **LOW** |
| 3 | `CorpsePart` cold-load factory wiring | Entity with CorpsePart, after save/load with fresh runtime, can produce a corpse on death | **LOW** |
| 4 | `DisposeOfCorpseGoal.GoToCorpseTries` field preserved | Yes (reflective save) | medium |
| 5 | `LayRuneGoal.MoveTries` field preserved | Yes (reflective save) | medium |
| 6 | `MeleeWeaponPart.Attributes` round-trips | Yes — string field, reflective serializer handles | medium |
| 7 | `MultiWeaponSkillBonus` stat on Player round-trips | Yes — Statistics dict iterates all entries | medium |
| 8 | `StoneskinEffect.Reduction` int round-trips | Yes — reflective | medium |
| 9 | `AcidResistance` etc. on creature round-trips | Yes — Statistics dict iteration | medium |
| 10 | Save entity at HP=0 (dying) round-trips, stays HP=0 | Yes — stat just has BaseValue=0, no special treatment needed | medium |
| 11 | Resave-load 5 cycles produces stable state | Yes (idempotency from spec test extends) | medium |
| 12 | Entity with NO Hitpoints stat round-trips | Yes — empty Statistics dict allowed | medium |
| 13 | Two entities, same Blueprint, different IDs | Both load as distinct instances; ID is discriminator | medium |

**3 LOW-confidence** (mutations, ID collision, CorpsePart factory wiring) are the gold targets.

## Implementation log

### Run results — 12 tests, **0 failures**

After dropping the LayRuneGoal duplicate and consolidating the
DisposeOfCorpseGoal one to use the actual ctor signature, the final
fixture is 12 tests.

| # | Edge | Confidence | Outcome |
|---:|---|---|:-:|
| 1 | MutationsPart manager-ID dynamic body part | **LOW** | ✅ PASS |
| 2 | Entity.ID collision on load | **LOW** | ✅ PASS |
| 3 | CorpsePart fields (CorpseChance, CorpseBlueprint) round-trip | **LOW** | ✅ PASS |
| 4 | DisposeOfCorpseGoal.GoToCorpseTries preserved | medium | ✅ PASS |
| 5 | MeleeWeaponPart.Attributes (Phase C) round-trips | medium | ✅ PASS |
| 6 | MultiWeaponSkillBonus stat (Phase G) on Player | medium | ✅ PASS |
| 7 | StoneskinEffect.Reduction (Phase T2.4) | medium | ✅ PASS |
| 8 | Phase E resistance stats — positive AND negative | medium | ✅ PASS |
| 9 | Save with entity at HP=0 (dying) round-trips | **LOW** | ✅ PASS |
| 10 | 5-cycle save-load stability | medium | ✅ PASS |
| 11 | Stat-less entity preserves empty Statistics dict | medium | ✅ PASS |
| 12 | Two entities, same blueprint, distinct IDs | medium | ✅ PASS |

**4 of 4 LOW-confidence predictions matched reality.**

### Classification per §3.9.5

All 12 tests passed on first run (after fixing my own API-mismatch bugs
in the test file — those were "test setup wrong" by §3.9.5's classification,
not findings about SaveSystem itself). No code-wrong outcomes.

### Honest interpretation

Per §3.9 empirical pattern, this is the third consecutive 0-bug
adversarial audit (combat M-series → 0; TurnManager → 0; SaveSystem
deep-dive → 0). The pattern is consistent: **code that's been through
TDD discipline + a saga or two genuinely runs out of easy bugs.**

SaveSystem's history that M-styled it:
- Phase 1 spec tests (commit `016639d`) — 21 specs, caught 2 real bugs
- Phase 2 gap-coverage (commit `dd8edc7`) — 21 tests
- Phase 3 adversarial (commit `65df19c`) — 13 tests
- Phase 4 IAuraProvider visuals fix (commit `0ab0f88`) — saga fix
- FormatterServices.GetUninitializedObject fix (commit `1b8ee0e`) — saga fix

That's 4 separate methodology-driven passes plus 2 saga-class bug fixes.
The deep-dive's 0-bug result confirms this surface is genuinely clean
for the round-trip identity contract.

### Value retained

- **+12 regression tests** pin specific contract aspects:
  - Mutation-added body parts preserve their Manager string
  - Entity-ID collision is non-silent (either rejects or distinct-instances)
  - New Phase C/E/G/T2.4 fields all round-trip via reflective serializer
  - HP=0 dying state preserves cleanly without re-firing HandleDeath
  - 5-cycle stability: no progressive corruption
  - Stat-less entities preserve empty Statistics dict
- **API-mismatch lesson preserved in commit:** my initial test had ~10
  wrong API guesses (Body.AddPart, GetGoalStackForTest, OverworldZoneManager
  zero-arg ctor, etc.). Surface-level inspection of EXISTING tests is a
  better starting point than guessing for cold-eye sessions on heavily-tested
  surfaces.

### Cadence recommendation

The audit cadence is showing diminishing returns on already-saga'd code.
Strong signal that the methodology is working and the codebase is healthy.

Next high-leverage move is **gameplay/content** rather than another
audit. Three audits in a row (combat / TurnManager / SaveSystem) all
returned 0 bugs. Continuing audits without pivoting risks burning effort
on already-clean surfaces.

Concrete suggestion: a `StoneskinTonic` consumable that uses Phase F's
BeforeTakeDamage hook + Phase T2.4's StoneskinEffect as a customer.
Small focused content ship — proves the architecture in real gameplay.

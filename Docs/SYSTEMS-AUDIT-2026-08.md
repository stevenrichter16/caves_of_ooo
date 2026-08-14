# Systems Audit — Material / Status-Effect / Destructibility / Tile / Bake+Save

> 2026-08-14. Thirteen-agent audit (7 subsystem deep-readers + 1 plan
> critic + 5 adversarial verify batches; ~2.2M tokens, 787 tool calls),
> every critical/notable finding re-verified against source by an
> independent skeptic. **105 findings: 22 critical (all confirmed),
> 57 notable (56 confirmed, 1 refuted), 26 minor (unverified).**
> Three of the most explosive criticals additionally spot-checked
> first-hand in the main session (Examinable param census, WoodenBarrel
> dual pool, GetParameter<int> mismatch).
>
> Trigger: the charm-flower incident and the question "are these
> systems architected so that silent failures keep happening?"
> **Answer: yes — and the audit found the shape of the disease, not
> just more instances.**

---

## 1. Verdict on `Docs/FLAMMABILITY-REDESIGN.md`

**Right diagnosis, right direction, unshippable as written.** Four
critical and four notable defects confirmed in the plan itself:

| # | Plan defect (confirmed) | Consequence if implemented as written |
|---|---|---|
| P1 🔴 | **M2 derivation leaks to creatures and items.** Replaying the bake: 91 blueprints would gain a derived `DestructiblePart` — **67 of them creatures, including Player and MimicChest** (base Creature = Material Flesh, Combustibility 0.15), plus ~24 items (Torch, LeatherArmor, RawMeat, corpses). The §3.5 exclusion governs only the M3 sweep, not the derivation rule. Every NPC and carried item would grow a "Break" menu row (`DestructiblePart.HandleEvent` has no creature guard). |
| P2 🔴 | **M1's RED tests are vacuous on the exact gap they claim to measure.** All three coherence assertions are implications with antecedent "flammable material" — CharmFlowers/Reeds/Chair/etc. have NO Material part, so the antecedent is false and they pass. The suite would instead go RED on creatures/items/AshBed (the scopes the plan excludes) while staying GREEN on the 70-blueprint roster it exists to fix. Needs an explicit must-burn roster as test input. |
| P3 🔴 | **Old saves never receive the fix.** `LoadEntityBody` clears and restores the saved part list; EntityFactory never runs for loaded entities, and autosave fires on every zone transition. Post-M3, every already-explored zone keeps inert scenery forever — this is an RPG whose world state persists, and the plan says nothing about migration. |
| P4 🔴 | *(from the LineTargeting finding)* The plan's premise that "the mutations family was fixed earlier" is false — see F11 below. The plan must include LineTargeting in scope. |
| P5 🟡 | **Unknown MaterialID silently derives nothing** — 28 distinct IDs ship in content; the plan's sketch names ~9 (two matching zero content). Without a mandated fail-loud this reintroduces the exact silent class it targets. |
| P6 🟡 | **Freeze and shock keep the dual-authority shape** the plan diagnoses as D2 — and freeze's thermal path has *no material veto at all* (Ice Lance freezes stone; `TryFreeze` fires no cancellable material check). Half-measure. |
| P7 🟡 | **No live-verification milestone** despite the CLAUDE.md mandate and a real live-only seam (the single `TickMaterialEntities` call site with hand-maintained filter lists — EditMode tests fire tick events manually and cannot see that seam). |
| P8 🟡 | **Coherence test 1 is misstated** ("flame point reachable by Blast" fails every Flesh creature and contradicts the plan's own Succulent profile) and **"blueprint-authored values always win" is unimplementable post-bake** (authored, inherited, and derived params are indistinguishable after the merge; zero-vs-unset exists only at the param-dict level — derivation must run at the *blueprint* stage, before part instantiation). |

Plan status changed to **REVISION REQUIRED**. The revision must: run
derivation on a scenery predicate (explicitly not Creature-tagged, not
Takeable), at the blueprint/param level rather than post-bake; mandate
profile coverage of all 28 MaterialIDs with a failing test for unknowns;
add a save-migration decision; extend the single-authority unification
to freeze and shock; include LineTargeting; restate the coherence
invariants over an explicit roster; and add a Deterministic
Self-Auditing Scenario milestone.

---

## 2. The disease, not the instances — five root causes

Every one of the 78 confirmed defects is an expression of one of these:

**R1 — The substrate cannot say "nothing happened."**
Event dispatch returns true both when a part handled the event and when
*nobody was listening* (`Entity.FireEvent` — veto is the only signal).
`GetParameter<T>` returns `default` on a missing key; the
`SetParameter` overload trio routes silently by compile-time type into
*different dictionaries* (the `(object)` cast convention is load-bearing
and hand-enforced — F20's four broken rites are the direct harvest).
Reflection param application drops unknown keys and unparseable values
with no trace. A system built on "silence = success" produces silent
failures **by construction**.

**R2 — Capability is part-presence, and absence is unobservable.**
Whether a thing can burn/conduct/break/tick is encoded by which Parts it
carries. There is no way to declare intent ("this SHOULD be flammable")
and detect its absence. The 70 fire-blind scenery blueprints, the six
fire-blind furniture pieces found by this audit (Crate, WovenBasket,
HollowLog, Bookshelf, WeaponRack, AlchemyShelf), and the barrel's
double HP pool are all this.

**R3 — One question, many hand-maintained authorities.**
Shock has **five** disagreeing deciders (matrix tags; `IsConductiveMaterial ≥50`;
tile propagation ≥50; Thunderclap >0.5; reaction JSON MinConductivity 50)
— shipped `MetalGrate` (Conductivity 100, no tags) conducts for three of
them and refuses for two. Fire has three-plus (matrix tags vs
Combustibility>0 vs tile threshold ≥50 — OilSlick/TarSeep are refused by
the matrix and ignited by the sim). Solidity has two (tag vs
`PhysicsPart.Solid` — locked doors are invisible to every tag-only
consumer: LOS, line walks, gas seep). Aliveness-of-object has three
(`Gone` / `IsDestroyed` / zone-placement). Ticking has two (TurnManager
roster vs `TickMaterialEntities`' hardcoded effect whitelist).

**R4 — Numbers without units.**
The 0-1 vs 0-100 dual scale is not cosmetic: it makes **TarSeep
permanently un-ignitable** (Volatility 30 × 100 → effective flame point
−2800, the upward crossing can never occur — the blueprint's own examine
text says "very willing to burn") and makes **PeatBog's wetness grow
without bound** (Porosity 80 → negative evaporation; one splash =
permanently fireproof, on the tile authored "answers to either school").

**R5 — Observability doctrine applied unevenly.**
Where the doctrine was followed it *worked* (the matrix's refusal
records, ApplyDamage's rejection reasons, LiquidPoolPart's seven
instrumented branches — all praised by the readers). But the heat chain,
MaterialSimSystem, MaterialReactionResolver, EntityFactory,
BlueprintLoader and the tile reaction engine emit **zero** records.
The flowers took a debugging session precisely because the gate that
ignored them was in the uninstrumented half.

---

## 3. The 22 confirmed criticals

Player-visible or corrupting, reachable in normal play. ⚡ = trivially
fixable (≤ ~10 lines).

| # | Finding | Where |
|---|---|---|
| F1 ⚡ | **All 40 Examinable descriptions in shipped content are dead.** Every blueprint authors param `Description`; the part's field is `Text`. Reflection drops it silently — no examine text in the game has ever shown. | Objects.json ×40; ExaminablePart; EntityFactory.cs:255 |
| F2 | **The bake has zero failure instrumentation.** 2116 params flow through ApplyParameters/ConvertValue; unknown key, bad value, unsupported type — all silently dropped. F1 is one harvest; there is no way to know how many more. | EntityFactory.cs:255-296 |
| F3 ⚡ | **TarSeep can never ignite** — Volatility 30 on the 0-1 formula ⇒ effective flame point −2800; crossing-only ignition never fires. Matrix refuses it too (`Liquid,Tar,Fuel` not in the tag list). | ThermalPart.cs:107 |
| F4 ⚡ | **PeatBog/SteamVent moisture grows unbounded** — Porosity 80/100 flips evaporation negative; one splash = permanently un-ignitable, effect never expires. | WetEffect.cs:44-58 |
| F5 | **"Vaporized" has zero listeners** — boiling a puddle does nothing; the SteamCloud blueprint and the authored VaporTemperature 100 are dead content. | ThermalPart.cs:190 |
| F6 | **Fire cannot melt ice** — fire_plus_ice requires the *ice itself* to be Burning; ice is Combustibility 0 (thermal veto) and not matrix-flammable, so the only trigger path is the weapon on-hit bypass. | fire_plus_ice.json + MaterialReactionResolver.cs:169 |
| F7 | **Tile state is never saved and Permanent pool projections are never re-projected on load** — post-load, every puddle is a rendering with no tile identity (no conductivity, no freeze, no steam). The "decays in 2-8 turns" rationale in Zone.cs is false for Permanent layers. | SaveSystem.cs:949; Zone.cs:417-440 |
| F8 | **Reactions consume Permanent pool projections** — one FlameJet over a WaterPuddle steams off the permanent water coating forever (re-projection only happens in Zone.AddEntity). | TileReactionSystem.cs:307 |
| F9 | **WoodenBarrel has two live HP pools.** Authored with Hitpoints 20 AND DestructiblePart 8. Direct ApplyDamage callers (cryo gas, thrown weapons, acid ticks) drain the hidden pool → `HandleDeath`: "the wooden barrel is killed!", blood splatter, witness broadcast, **Beast-tier loot roll**, contents destroyed without spilling — while look mode shows 8/8. | Objects.json; CombatSystem.cs:1257 |
| F10 | **Burning a dropped torch rolls beast loot.** 53 blueprints have Hitpoints-stat-no-Creature-tag (weapon/armor durability); RouteDamage's fall-through sends them to ApplyDamage → HandleDeath → "the torch is killed by you!", splatter, WitnessedEffect on bystanders, DeathBeastT1 roll. | DestructionSystem.cs:213 |
| F11 | **LineTargeting still skips Wall/Terrain tags** — the mutations family (FireBolt, ArcBolt, IceShard, ChainLightning) cannot hit breakable Walls, pools, bushes, campfires. The 5cfdf572 commit body's claim that mutations were "fixed earlier" was false; the two line walks now disagree on shipped content. | LineTargeting.cs:171 |
| F12 | **Corpses and ground loot eat bolts and steal skill aim** — both walks accept any PhysicsPart/MaterialPart holder; a corpse one cell nearer than the monster becomes Ember Spit's target; thrown spears stop at dropped bones ("for 0 damage"). Corpses-after-a-fight is the normal battlefield state. | LineTargeting.cs:174; SkillLine + EmberSpit line[0] |
| F13 | **Shock: five authorities, MetalGrate breaks the chain** (Conductivity 100, no tags: matrix refuses, Overload chain breaks, while Thunderclap/tile-charge/chain-electricity all conduct through it). | ObjectStatusMatrix.cs:61 et al. |
| F14 | **Fire: matrix refuses OilSlick/TarSeep/AshBed while the thermal sim ignites them** — Ember Spit cannot light an oil slick. The matrix test fixture used OilSeep's tags (the one oil blueprint WITH `Flammable`), hiding the drift. | ObjectStatusMatrix.cs:46 |
| F15 ⚡ | **Shrine blessing can silently no-op while charging drams** — reaches for `GetPart<StatusEffectsPart>()?.` instead of `Entity.ForceApplyEffect` (which auto-creates); success message and diag fire regardless. | SanctuaryPart.cs:58 |
| F16 ⚡ | **Rail Spike's grounding check reads the Hitpoints stat** — scenery ground target ⇒ false "falls before the charge can ground", no Electrified; GroundSurge's priming has the same stat gate. | Galvanism_RailSpike.cs:108 |
| F17 ⚡ | **Four directional rites are dead from the keybind** — InputHandler writes DirectionX/Y via the int overload (IntParameters); ConjureWater/HangingBolt/Fulmination/ConsumingRiteBase read `GetParameter<int>` (object dictionary) → always (0,0) → "The rite fails to resolve." AI casts identically broken. | GameEvent.cs:154 vs InputHandler.cs:3140 |
| F18 | **Lazy StatusEffectsPart creation mid-dispatch re-runs the dispatching handler** — first-ever effect on an entity front-inserts a part during the index loop; ThermalPart receives the same ApplyHeat twice and **doubles the freshly calibrated dose** on the exact tick a pristine tree first ignites. | Entity.cs:257; StatusEffectsPart.cs:25 |
| F19-22 | The four plan defects P1-P3 + LineTargeting scope (counted among the 22). | FLAMMABILITY-REDESIGN.md |

**Refuted (1):** shrine donations stacking unbounded Stoneskin — the
verifier found the stacking guard real. (The verify pass had teeth.)

---

## 4. Confirmed notables — grouped

Full list in the workflow journal; the themes:

**Fire/ice/shock semantics that don't do what content says** — wet
suppression never retried while hot; the two ignition doors
(ThermalPart bypasses the matrix; material-less ThermalPart entities
ignite unconditionally); temperature uncapped (every Attack-sized dose
shatter-checks brittle targets — dose-derived, not crossing-derived);
duplicated moisture constants (PyroIgnition vs ThermalPart);
Thunderclap still on the 0-1 threshold its own docstring warns about;
`TryFreeze` has no material veto (Ice Lance freezes stone);
cryo gas invisible to stat-less matter; acid/electrified ticks never
damage the scenery the matrix invites them onto; `TryShatter` a silent
no-op for structural-HP objects, and its damage path calls
ApplyDamage with `zone:null` leaving dead entities placed.

**The two fire models ignore each other** — a burning entity never
writes tile heat (hedge on an oil slick cannot light it: `ApplyFireToTile`'s
"universal routing" docstring is false for five abilities); tile fire
can never apply Burning to anyone (`MakeEffect` has no Burning case)
and embers alone are harmless to stand in; `FuelExhausted` has no
listener so the authored AshPile exhaust product never spawns; moving a
LiquidPool entity leaks its Permanent coating at the old cell.

**Scenery ticking is a whitelist** — `TickMaterialEntities` rosters
Burning/Wet/Frozen/Acidic/Electrified/Lifespan; matrix-permitted
Smoldering/Charred/Broken never tick (Smoldering's heat emission is
dead code on scenery — passive entities get EndTurn only, never
BeginTakeAction); zero diag in the gate that decides whether scenery
effects advance; resting advances the clock 60 ticks while every fire
and effect stands still.

**Bake/save fragility** — Stat inheritance replaces wholesale
(discarding parent Min/Max — 279 live instances); the save format's
"graceful skip" of unknown types actually desyncs the stream and bricks
the save; Dictionary-typed public fields silently round-trip empty;
EntityFactory's ID counter is never persisted (duplicate IDs after load
guaranteed); `Part.Initialize` never runs on load (implicit contract);
dangling Inherits leaves a skeletal blueprint in service after one
warning; environmental water never wets scenery (pools wet only
creatures that MOVE into them; rain wets only crops — so the
wet-suppresses-fire counter is unreachable for a hedge in live play).

**Substrate** — dispatch can't distinguish handled from ignored;
`Part.WantEvent` is dead API with a false docstring; no double-release
guard on the event pool; Solid dual representation (locked doors
invisible to tag-only consumers); MessageLog unbounded and sometimes
the only record of an outcome.

---

## 5. What is genuinely good (keep, and imitate)

- **The save token graph** — interned entity references, queued bodies,
  placeholder-then-fill identity preservation, atomic writes with
  rollback, section check hashes, and an unusually deep test suite.
- **FireDose** — anchor + measured table + regression pins; named by
  multiple readers as the model for post-incident engineering.
- **ObjectStatusMatrix's fail-closed shape with refusal diags** — the
  doctrine done right; the defects are in its *tag lists*, not its shape.
- **DestructionSystem's ordering discipline** — `Gone` set before the
  notification, `_DeathHandled` double-fire guard, opt-in default,
  honest docstrings.
- **The tile engine's loop guards** — per-(reaction,tile) once-per-pass
  sets, generation counters, lease-model tile sources, and
  LiquidPoolPart's fully instrumented reject branches.
- **The register-test pattern** (`DestructibleRegisterTests`) — the
  enforcement mechanism the redesign should generalize.

---

## 6. Recommendations, in priority order

**P0 — Fix the confirmed criticals as their own backlog.** At least six
are ⚡ one-sitting fixes (F1 Description→Text or field rename, F3, F4,
F15, F16, F17). F9/F10 are one rule: strip Hitpoints stats from
non-creatures or make RouteDamage refuse the creature path for
untagged entities. F11/F12 are the targeting-filter unification the
flammability plan should absorb.

**P1 — Make the substrate fail loud (highest leverage per line).**
1. EntityFactory: unknown param key / unparseable value / unsupported
   type ⇒ `Debug.LogError` + diag `content/BlueprintParamRejected` —
   and a **content coherence test** that bakes all 349 blueprints and
   asserts zero rejections. This single test would have caught F1's 40
   dead descriptions, the barrel's stat, and every future typo.
2. GameEvent: collapse the parameter stores or make `GetParameter<int>`
   read both (F17's class dies); add a dev-mode "unhandled event"
   counter so silence becomes measurable.
3. Fix the mid-dispatch part-insertion reorder (F18): snapshot the part
   list for dispatch, or pre-attach StatusEffectsPart at bake.

**P2 — The revised flammability redesign** (per §1's required
revisions): profiles at the *blueprint* stage with a scenery predicate,
full MaterialID coverage tests, one authority per element (fire AND
freeze AND shock), LineTargeting in scope, explicit must-burn roster
tests, save-migration decision, live self-auditing bench.

**P3 — Observability backfill** on the uninstrumented half: heat
chain, TickMaterialEntities' gate decisions, reaction resolver
no-match paths, bake rejections (covered by P1), tile-layer writes
that reject.

**P4 — Structural cleanups** as opportunities arise: single solidity
authority, one object-aliveness predicate, unify the two line walks,
kill the duplicated moisture constants, cap or crossing-gate thermal
shock, decide the unit scale once (the existing spun-off task).

---

## Honesty bounds

- 26 minor findings were not adversarially verified (severity-triaged
  out); treat them as leads, not facts.
- The verify pass re-read cited code; it did not run the game. Findings
  describing *live* behavior (tick delivery, autosave cadence) rest on
  reading the single call-site chain, which is strong but not a play
  session.
- "22 critical" reflects the auditors' severity calls; some (F5, F6)
  are dead-content rather than corruption and a reasonable person could
  rate them notable.
- One refuted finding of 57 verified suggests a low-but-nonzero
  false-positive rate among the unverified minors.

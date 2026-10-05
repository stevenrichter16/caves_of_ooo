# Spell cast reliability

Status: implemented after observed native RED and verified in the 4,734/4,734 affected-system run, plus the six-check native Play/save bench. The final reader-only polish passed 91/91 follow-up checks. Root alone ran Unity and publishes this work. This is original Caves of Ooo behavior, not a Qud-parity claim.

## Authorized scope and frozen rules

The engagement audit identified health buffs that expire on the global tick clock before a normal follow-up, damage spells that ignore purchased spell bonuses, and terrain casts that change the world before reporting a free refusal. This milestone repairs those contracts without changing damage dice, cooldowns, ink economy, environmental aftermath, or retorts.

- Ley Tap becomes a positive `LeyTapEffect` with public saved `BonusDamage` and three owner actions of duration. Heart Flame becomes a positive `HeartFlameEffect` with public saved `ChargesRemaining` and five owner actions. The existing effect `JustApplied` rule skips the activation action's EndTurn. Skipped owner turns still age effects; other actors and global clock ticks do not.
- Their live effect descriptions show the remaining damage bonus/charges and owner-action window. Reapplication refreshes that buff rather than stacking independent copies. Save/load retains the actual effect and remaining state; no save-format change is needed.
- One bounded cast scope surrounds `SpellSkillPart.OnCommand`. A successful cast with at least one valid, positive-base direct-damage attempt spends each matching buff once. Full resistance still spends it. Every target in the same cast receives the same captured bonus. Empty fired bolts, terrain-only casts, utility, refusal and nonmatching elements do not spend a damage charge. Modifier reads themselves are pure.
- Bonuses add to the direct damage amount before resistance and structural damage routing. Retorts, weapon riders, status damage and terrain damage remain outside this direct-spell contract. Both simultaneous buffs can apply to a fire cast.
- Rites retain their target, ink, status consumption and rounded resonance calculation. Modifiers apply to that computed direct amount after status consumption; conditional school bonuses inspect only remaining statuses. Base-zero rites remain nondamaging and cannot spend a damage buff.
- Preserve every original damage attribute, including multi-attribute rites. Resonance element is separate from damage typing: Shattered Rime's Cold resonance does not turn its existing untyped damage into Cold damage.
- Flame Jet and Backdraft with a valid traced terrain path are paid fired casts even without a creature, matching projectile semantics. A zero-cell/out-of-bounds path with no target remains a state-free refusal. Environmental interaction is retained.
- Hydromancy's existing moisture bonus also applies to Quench and Conjure Water. Other water/terrain sources are not globally buffed.

## Pre-implementation source sweep and corrections

| Premise | Source before this change | Decision |
| --- | --- | --- |
| Three/five turns means three/five follow-ups | LeyTap/HeartFlame use `TickCount + 3/5`; TurnManager grants100 energy/tick toward1000 | Replace with existing owner-EndTurn effect lifetime. |
| A spell query is a cast | Both buff modifier hooks mutate skill-private state | Pure modifiers; consume at successful cast completion. |
| Private buff fields survive saves | Both are NonSerialized; SaveSystem saves ordinary Effects through public fields | Use two specific effects, not a new save framework. |
| All elemental direct damage uses modifiers | Five projectile spells use SpellDamageHelpers;16 other actives use raw damage | Normalize those direct paths and the shared rite damage loop. |
| Rite Element is its damage type | ShatteredRime Element=Cold but DamageAttributes is empty | Separate modifier element from exact damage attributes. |
| False casts leave state unchanged | FlameJet/Backdraft write and resolve fire tiles before empty-target refusal | Valid fired terrain cast returns success and spends a turn/cooldown. |
| All learned water sources get mastery | Only JetBlast/DrenchLob/Undertow call ApplyMoistureBonus | Add it to Quench/ConjureWater only. |

Sources: `Skills/SpellSkillPart.cs`, `SpellDamageHelpers.cs`, `Spellcraft_LeyTap.cs`, `Pyromancy_HeartFlame.cs`, `ConsumingRiteSkillBase.cs`, `Rites_ShatteredRime.cs`; `Effects/StatusEffectsPart.cs:506`; `Save/SaveSystem.cs:1516`; `Turns/TurnManager.cs:9`. Paths are under `Assets/Scripts/Gameplay/`.

## Boundaries and ownership

This agent owns the spell wrapper/helpers, two new effect files, their two EffectDescriber cases, buffs, direct elemental damage callers, Hydromancy callers, FlameJet/Backdraft outcomes, tests and this document. The target-selection agent owns Pyroclasm and will adopt the agreed helper API alongside its target correction. The clarity agent may edit only the rite target-query seam; the shared damage loop remains this milestone's. Root owns JSON, Calm, ColdSnap, Lunge and Bludgeon. No unrelated balance, thermal, Charred or status-system rewrite is included.

The helper retains the existing five-argument API with optional additional damage attributes. A separate exact-attributes entry point supports Rites without changing their damage types. A small disposable cast context provides nested-call cleanup; it does not become a second ability dispatcher.

## Verification plan

Tests dispatch real skill commands with scheduler-owned player actions at normal/fast/slow speed. They pin activation cost, next-action availability, exact expiry, pure repeated queries, simultaneous buffs, multiple targets, full resistance, no-target/utility/nonmatching counterexamples, and actual replacement-graph save/load. Exception/nested cleanup is a source-review boundary until an explicit fixture is added. A table of all21 direct elemental attacks compares equal seeded casts with and without universal investment. Additional checks preserve structural damage, multi-attribute/untyped Rites, finite ink, and zero-base rites. Terrain fixtures use actual oil reactions and paid/refused counterexamples; Hydromancy fixtures compare moisture with/without the root.

Existing direct-hook tests that expected a query to consume a charge intentionally migrate only after observed RED. Old empty-FlameJet/Backdraft refusal assertions also change only where a real traced terrain cast exists. All other established behavior remains a regression gate. Root will record raw native RED/GREEN and any ordinary-play limits here.

## Performance and review

One short-lived scope per actual cast; no per-frame work, object-per-cell presentation, global scans or preview mutation. Existing status readouts consume the new effects. Damage still uses normal cell dirty/FX hooks. Cold review will compare all21 damage paths, zero-base/fully-resisted counters, original attribute sets, save fields and owner-action expiry before completion.

## Implementation ledger

- Source sweep completed; root approved the policy above before production.
- Root observed initial native Unity RED: `SpellCastReliabilityTests` 13 cases (11 expected failures, 2 passing controls); `ElementalSpellDamageContractTests` 33 cases (23 expected failures, 10 passing controls). Terrain tests were initially omitted because their new meta GUID had 33 characters; root corrected it to 32 and ran a separate native RED: `SpellTerrainCastContractTests` 6 cases (4 expected failures, 2 passing zero-path controls). No omitted test was counted as passing. Raw evidence: `Docs/Verification/SkillsEngagement/red.xml` and `terrain-red.xml`.
- Implemented the shared cast scope, saved positive buffs and pure readouts; routed all direct elemental damage and the shared rite loop through the modifier/structural helper while retaining exact original damage attributes. The other agent owns the coordinated Pyroclasm caller. Quench and Conjure Water apply existing moisture mastery. Valid traced terrain casts now report paid success.
- Intentionally migrated two legacy modifier-query tests: querying now preserves charges. The old empty-Flame-Jet refusal test now asserts successful paid terrain resolution. The new command/scheduler fixtures test actual charge consumption.
- First native implementation batch: 281 cases, 272 passed and nine failed across all agents. All 52 new spell cases and the legacy Ley Tap/Heart Flame suites passed. The sole failure within this milestone was the old `AllThree_AreRegisteredAndFitTheSkillsScreen` 55-character description constraint, intentionally superseded by the full-details reader. It now checks all three powers are registered with nonempty descriptions and that the reader contains each exact full description. Raw evidence: `Docs/Verification/SkillsEngagement/first-implementation.xml`. The corrected assertion passed in the broader run; this is not a claim of a single 281/281 pass.
- First broader native run: 4,734 cases, 4,728 passed and six failed globally (`Docs/Verification/SkillsEngagement/broad-first.xml`). The additional owned corrections are test-only: the Cryomancy full-reader sibling replaces its obsolete 55-character cap; two WSP8.4 Heart Flame tests now exercise real casts and owner EndTurns instead of consuming query hooks/global-tick expiry, with a global-clock nonexpiry counter and actual paid reapplication. Two rain fixtures now provide the real planted-owner prerequisites (`Crop` tag and non-takeable, non-solid `PhysicsPart`). Those farming guards already existed in commit `49dc8768f`; malformed fixtures were rejected under an active scheduler. The no-crop/out-of-range controls remain, and no farming production code changed. The broader rerun passed 4,734/4,734.
- The shared test fixture now snapshots/restores the active scheduler, world, settlement registry and message tick provider in teardown. The Conjure Water factory override remains protected by `finally`.
- Added `SpellCastReliabilityBench` (registered scenario name: **Spell Cast Reliability Audit**, category Combat). The attribute does not itself create a Unity menu launcher; root can invoke it from the isolated native reader driver and record a separate receipt. Its six controlled checks dispatch real commands and pay scheduler turns: Ley Tap, Heart Flame, pure readouts, full replacement-graph save/load, one charge across two Flame Jet targets, and a follow-up Kindle. The fixture grants skills and uses plain stationary actors; it proves neither ordinary discovery/input nor visual quality/combat balance. Native runtime execution passed all six cases in both reader-audit runs below. Its scope restores the borrowed scheduler, world, settlement registry, messages and FX even on failure.
- All six new C# meta GUIDs validated as 32 lowercase hexadecimal characters. Source whitespace checks pass. Broader native tests and native Play verification are complete; no gameplay-feel or ordinary-discovery claim is made.

## In-phase source review

- 🟢 All 21 direct elemental actives use the common modifier path. The remaining raw elemental skill damage is intentionally weapon riders or retorts. Original damage attributes are retained; the shared rite helper has a separate modifier element and preserves untyped Shattered Rime. Both zero-base guards precede modifier application.
- 🟢 Charge snapshots are cast-local and every qualifying target receives the same bonus. Successful fully resisted attempts spend once. Both new effects use ordinary saved public fields, owner-action expiry and visible descriptions. Native tests cover actual scheduler follow-ups, all three action speeds, AOE charges, utility/empty/nonmatching controls, and replacement saves.
- 🟢 Independent cold review found no concrete ordinary-route flaw in cast snapshots, once-per-cast spending, positive effect flags, public-field save restoration or owner-turn `JustApplied` handling. This is source review, separate from the native test results above.
- 🧪 Generic reentrant custom spell callbacks are not a shipped feature or proven use case. The disposable scope restores the caller on nested or exceptional resolution, but custom nested same-caster charge reservation is not claimed as independently verified. This is deferred rather than adding a second spell dispatcher.
- ⚪ No damage-dice/cooldown rebalance or combat-feel claim is included. Elemental environmental aftermath, retorts and weapon riders retain their existing rules.

## Owned files

Paths below are relative to `Assets/` unless prefixed with `Docs/`.

- `Scripts/Gameplay/Skills/SpellSkillPart.cs`, `SpellDamageHelpers.cs`, and the damage loop in `ConsumingRiteSkillBase.cs`: one cast snapshot and shared direct-damage rules. The clarity agent separately owns the rite target-query seam.
- `Scripts/Gameplay/Effects/Concrete/LeyTapEffect.cs`, `HeartFlameEffect.cs` and metas; their cases in `Effects/EffectDescriber.cs`; `Skills/Spellcraft_LeyTap.cs`, `Pyromancy_HeartFlame.cs`: visible saved owner-action buffs.
- `Scripts/Gameplay/Skills/Pyromancy_{EmberSpit,FlameJet,Backdraft,FlamingHands,EmberVein,Conflagration}.cs`, `Cryomancy_{RimeGrip,RimeNova}.cs`, `Hydromancy_{JetBlast,Undertow}.cs`, `Galvanism_{GroundSurge,BacklashCoil,Overload,RailSpike,Thunderclap}.cs`: existing direct damage routed through the shared helper. Pyroclasm is coordinated but owned by the targeting agent.
- `Scripts/Gameplay/Skills/Hydromancy_{Quench,ConjureWater}.cs`: existing Hydromancy moisture bonus applied to the remaining learned water sources.
- `Tests/EditMode/Gameplay/Skills/{SpellCastReliabilityTests,ElementalSpellDamageContractTests,SpellTerrainCastContractTests}.cs` and metas: scheduler, charge, damage, resistance, structural, save and terrain contracts. Existing Ley Tap, Heart Flame, Flame Jet, Cryomancy Rime Grip and WSP8.4 suites migrate deliberately obsolete assertions. `Skills/ElementalPortTests.cs` and `Effects/StarterSpell3DOutcomeCaptureTests.cs` supply valid physical crop fixtures for existing watering checks.
- `Scripts/Scenarios/Custom/SpellCastReliabilityBench.cs` and meta: six controlled runtime checks with borrowed-global cleanup.
- `Docs/SPELL-CAST-RELIABILITY.md`: policy, source corrections, native results and limits.

Native regression confirmation (root): `Verification/SkillsEngagement/broad-green.xml` passes **4,734/4,734**, zero failures or skips, 121.62 seconds. This is the affected-system suite in Unity Editor, not the standalone runner and not a whole-game balance verdict. All 283 selected fixture classes are recorded in the XML. Native Play receipt is recorded in the implementation overview after completion.

Final acceptance and remaining play-feel limits: [SKILLS-AND-SPELLS-IMPLEMENTATION.md](SKILLS-AND-SPELLS-IMPLEMENTATION.md). Final isolated native reader run `1e6e8a3575564a818d590d20481539cd` passes ten keyboard checks and six separate spell/save checks with zero errors; final reader-only polish passes 91/91 after the 4,734/4,734 broader run.

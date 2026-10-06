# Fifty improvements: combat, equipment and consumables (1–15)

Status: all15 implementations are complete and verified. Final targeted Unity EditMode receipt `Docs/Verification/FiftyImprovements/final-green.xml` passes831/831, including95 combat tests (94 gameplay/counter/adversarial cases and the12-check controlled witness wrapper). Final-source PlayMode run `f84726915d704c829dea68be2ad672ee` passes60/60 checks, including12/12 combat checks, with no reported errors and6 captures inspected by root. The separately owned deferred-rest lifetime correction is closed in these final receipts. This is composite verification following the initial15,889-case run, not one unfiltered final-state suite. Root observed the intended70-case native RED (37 failed,33 passed) in `all-red.xml` before releasing production. Designs were verified on `bfa625f44`; root owns Unity, content JSON edits and commits. These are Caves of Ooo designs, not new Qud-parity claims. Each numbered item changes a distinct player outcome; tests and maintenance corrections do not increase the improvement count.

## Executed discovery prompts

1. Trace current weapon-active commands, movement vetoes and their actual payment result. Compare with the recently fixed target/charge contracts, and reject already-solved multi-cell or death-after-reaction concerns.
2. Follow carried medicine through tonic application, throwing, status lifetime and kill handling. Inspect actual authored tonics and reagent rules before treating descriptions or old plans as truth.
3. Follow Hammer's Broken marker through equipment use, composition repair and supply sources. Confirm an ordinary repair route and physical material payment before proposing equipment impairment; inspect effect stacking and save fields for persistent consequences.

## Source corrections and scope pruning

| Initial premise | Current source evidence | Decision |
| --- | --- | --- |
| Bare numeric strings are fixed dice damage. | `DiceRoller` accepts only NdS with an optional signed modifier. | RED fixtures use `1d1`/`1d1+6`, and bleeding compares only the existing grammar. No dice grammar expansion. |
| A tonic splash duplicates a wide creature in every occupied cell. | `ThrowItemCommand.cs:642–674` actually uses `Cell.Objects`, which stores anchors. The current failure is an exposed body contact being missed when its anchor is outside the square. | Improvement 3 changes to physical occupants with one owner per impact; duplicate and mutation checks are counters within that one feature. |
| Every mobility skill already behaves like corrected Charging Strike. | `ShortBlades_Disengage.cs:92` returns true even at zero cells. | Keep improvement 1. |
| ForceMoveTo is an ordinary ability-movement method. | `MovementSystem.cs:175–202` explicitly describes externally forced displacement; it skips BeforeMove. Charge, Disengage, Vault and Tumble use it for their acting owner. | Improvement 2 checks the acting owner's voluntary movement permission; it does not change forced movement globally. |
| A poison-resistance stat already exists. | `CombatSystem.cs:1196–1203,1233–1238` only maps Heat/Cold/Electric/Acid. | Drop the proposed poison-resistance fix. No new resistance framework. |
| Flurry and Whirlwind continue after the attacker dies. | `ShortBlades_Flurry.cs:97–101` and `Axe_Whirlwind.cs:97–99` already check both owners' presence/HP. | Drop this duplicate proposal. |
| RepairCost provides a player repair system. | `TinkerItemPart.cs:19` is unused metadata; developer tinkering is not the ordinary material repair route. | Do not enable BitLocker or tinkering UI. Reuse composition/material repair in improvement 8. |
| RepairablePart already fixes Broken equipment. | `RepairablePart.cs:68–94` rejects takeable/carried/equipped targets and only restores structural Repaired state; no BrokenEffect consumer exists in combat. | Add narrowly opted-in portable support while keeping structural defaults unchanged. |
| Burn Salve automatically reignites a patient next turn. | ThermalPart ignites on an upward threshold crossing (`ThermalPart.cs:79–80`), not simply each turn above the threshold. | Do not claim automatic reignition. Improvement 12 makes cooling a real additional medical outcome; the retained hot state is the actual gap. |
| Speed/Strength tonics are already temporary but broken. | `TonicPart.cs:164–177` writes Stat.Boost permanently; actual blueprints omit Duration (`Objects.json:8969–9113`). | Improvement 10 is an intentional finite-buff redesign, with existing saved permanent gains preserved. |
| Killing-blow gas weapons are ordinary content. | No authored `EmitGasOnHitRaw` blueprint was found; that path also loses the dead owner's location. | Exclude this low-reach pipeline proposal. Improvement 13 instead connects existing ordinary Burning to existing Cinder/Charsplit. |
| Two-handed axes are not modeled. | Axe_Dismember's old comment says so, but HandlingService and Equippable.UsesSlots model two hands. | Improvement 9 uses current held-weapon metadata and keeps the change to this passive. |

## 1. Disengage refuses a completely blocked escape without payment

**Plan/problem:** A wall or adjacent creature can prevent every step, yet `ShortBlades_Disengage.cs:70–92` reports success and spends cooldown/action.

**Design:** Return false and the normal `SkillRejected/no_movement` record when no move committed. Any committed step still pays, including an entry reaction returning the actor to its starting position. Retain range 3, cooldown 25, weapon gate, collision and landing effects.

**Acceptance/counter:** Actual command into clear space moves three cells and pays; first-cell wall/creature leaves position, cooldown and input clock unchanged; one clear step then blockage pays. A trap or redirected step remains paid. **Files:** ShortBlades_Disengage; focused command tests. No input/UI changes.

## 2. Rooting prevents self-propelled mobility without resisting enemy shoves

**Plan/problem:** `RootedEffect.cs:4–15,43` promises the actor cannot leave its cell; the four movement actives bypass its voluntary veto through ForceMoveTo/TrySwap.

**Design:** Before committing self movement, consult the acting owner's existing movement/action permission in Charge, Disengage, Vault and Tumble. Use the existing BeforeMove contract rather than deciding that rooted victims resist Slam/Hook. Evasive Roll remains the explicit status escape, and nonmoving attacks/casts remain available. Charge may still attack an already adjacent enemy without moving; refuse only its attempted movement if rooted.

**Acceptance/counter:** Rooted actor cannot cross an otherwise-valid lane/vault/swap and spends nothing; removing Rooted restores the same action. Rooted target can still be displaced by an unrooted attacker. Charge's adjacent attack and Evasive Roll retain their current contracts. **Files:** four skill classes, narrow existing helper if useful; no global ForceMoveTo behavior change.

## 3. Thrown tonic splashes follow complete physical bodies

**Plan/problem:** `ThrowItemCommand.cs:642–674` scans anchors, missing a creature whose exposed body intersects the splash while its anchor lies elsewhere.

**Design:** Capture distinct current creature owners from physical occupants in the impact square before applying payloads; apply once per owner, retain friendly fire and radius one. Validate live current placement before capture. This is one spatial-delivery correction, including safe iteration.

**Acceptance/counter:** Secondary body contact receives one heal/status application; two covered body cells still receive one dose; a footprint hole/far owner receives none. A callback cannot inject an extra owner into the captured impact. One flask leaves the original stack. **Files:** ThrowItemCommand; throwable fixture.

## 4. Poison and bleeding kills retain their actual inflictor

**Plan/problem:** `PoisonedEffect.cs:40` and `BleedingEffect.cs:44` pass null to ApplyDamage. `CombatSystem.cs:1331–1333` awards kill XP to the supplied killer, so these delayed finishes lose source credit even though weapon/tonic application supplied it.

**Design:** Capture a saved Entity source on the two ordinary damage-over-time effects at their normal application boundary. Tick damage uses that source. Retain the first active dose's source during refresh/stacking (explicit deterministic attribution); unsourced environmental effects remain unsourced. Do not repurpose Effect.Owner, which is the patient.

**Acceptance/counter:** A real weapon/tonic-applied poison or bleed finishes a creature and records the player as killer/awards normal XP; an unsourced effect does not fabricate credit; replacement save graph resolves the source to its loaded owner; no double death/XP. **Files:** two effects and the narrow StatusEffectsPart application seam; source/save tests.

## 5. Stronger bleeding is chosen by damage potency, not text order

**Plan/problem:** `BleedingEffect.cs:78` compares dice strings ordinally, so `1d10` can lose to `1d2`, and additive formulas can select weaker damage.

**Design:** Compare expected numeric damage using the existing dice grammar, without rolling or consuming combat RNG. Keep the stronger mean; retain the existing dice on equal means. Preserve the independent harder-save merge and indefinite recovery contract. Malformed input must not replace a valid active dose.

**Acceptance/counter:** `1d10` upgrades `1d2`; reverse order does not downgrade; equal expectation keeps the earlier dose; save difficulty still takes the harder value. **Files:** BleedingEffect and a local/narrow existing dice parser seam; no global dice rebalance.

## 6. Hooks release when their wielder can no longer hold them

**Plan/problem:** HookedEffect comments promise death/zone departure breaks the hook, but `HookedEffect.cs:69` only checks null and `:109` silently skips movement when the hooker has left. The victim remains Hooked and debuff-dependent attacks can still exploit it.

**Design:** At the next owner turn, end the effect before its save roll when the hooker is dead, death-handled, absent from the victim's current zone or no longer the current placed owner. Keep valid same-zone distance pulls and save rules unchanged.

**Acceptance/counter:** Death and actual zone departure remove Hooked with external cause and no save RNG; live same-zone hooker retains normal pull/save behavior; replacement saves preserve valid hook identity. **Files:** HookedEffect and tests.

## 7. Hammer-damaged ordinary gear suffers a bounded, repairable penalty

**Plan/problem:** `Cudgel_Hammer.cs:18–20` and `BrokenEffect.cs:4–21` confirm Broken is only a message. This bought passive has no functional equipment outcome.

**Design:** Opt in a small authored set of common repairable weapons/armor. On those items only, Broken imposes −2 melee HitBonus and/or −1 armor AV (floor zero for armor); one effect applies one penalty. Store actual deltas and restore precisely on repair. Preserve item ownership, enchantments, equipment bonuses and use: damaged gear remains usable. Non-opted-in objects retain their old marker behavior. Ship with improvement 8 and sourceable materials.

**Acceptance/counter:** A real Hammer proc changes effective attacks/armor on an opted-in item; repeated Broken does not bank penalties; remove/repair restores exact pre-break values; loaded broken equipment retains one penalty and repairs once; unrelated items stay unchanged. **Files:** BrokenEffect; exact root-owned common-gear blueprint additions; existing enhancement/equipment tests.

## 8. Repair damaged carried gear with finite physical materials

**Plan/problem:** Existing `RepairablePart`, `CompositionPart` and `RepairRecipeRegistry` already provide native material repair; portable equipment is explicitly excluded. Root approved bounded portable opt-in and rejected BitLocker use.

**Design:** Extend the existing repair action for an explicit portable-equipment recipe/opt-in. Repair only one carried, unequipped, unstacked owned item bearing Broken; material payment is the existing transaction/receipt path. Composition must match. Proposed common recipes: Metal uses one SteelBladeComponent; Wood uses one SalvagedTimber; Leather uses one LeatherBindingComponent. No MetalScrap blueprint was found. These physical materials already occur in ordinary loot (`LootTables.json:512,630,638,988,993,1824,2719`; timber is ordinary repair supply). Repair removes Broken only, not HP damage, temper fatigue, modifiers or names. Structural faults remain one-time world repairs and keep their exact authority rules.

**Acceptance/counter:** Real RepairObject consumes precisely one matching carried unit and removes/restores the penalty; no material/wrong material/equipped/stacked/remote owner refuses atomically; save preserves paid quantity and repaired item. Existing structural repair fixtures remain green. **Files:** RepairablePart, Tier1Repairs data, exact authored equipment Composition/Repairable entries (root edits JSON); no new repair framework or developer controls.

## 9. Two-handed axes gain a modest dismembering role

**Plan/problem:** `Axe_Dismember.cs:31–33` deferred a two-handed distinction because the old engine lacked that metadata. Current HandlingService.GetGripType and equipped UsesSlots provide it.

**Design:** Keep 3% on ordinary axe hits; use 6% only when the actual striking axe occupies two hands (current weapon entity and actual equipped binding). Preserve actual-damage, body, severability and Decapitate gates. Do not modify generic damage, every weapon's proc chance, or Berserk.

**Acceptance/counter:** A controlled 4% roll succeeds only with an actually wielded two-hand axe; one-handed and non-axe strikes do not; two-hand item merely carried grants nothing; no damage never procs. Existing 3% and mortal-body controls stay intact. **Files:** Axe_Dismember and tests. Intentional local role adjustment, not a reported defect.

## 10. Speed and Strength tonics become finite tactical preparations

**Plan/problem:** Actual authored tonics add permanent Boost without Duration. Stacking bottles progressively replaces the timing decision with persistent stat growth.

**Design:** New uses of the two authored tonics grant their existing +20 Speed/+4 Strength for 20 owner turns through one small saved timed-stat effect; same-stat reapplication refreshes the longer duration without adding magnitude. Different stats coexist. Existing nonpositive-Duration generic StatBoost behavior is retained unless explicitly opted in. Root adds Duration=20 to SpeedTonic and StrengthTonic. Earlier saves' permanent Boost fields remain untouched; no retroactive subtraction or guessed migration.

**Acceptance/counter:** Actual tonic changes its intended stat, expires back to the prior value, and restores correctly across save/load; second bottle refreshes without doubling; unrelated base/equipment/legacy Boost remains after expiry. A legacy duration-zero tonic retains prior behavior. **Files:** TonicPart, new effect, exact root-owned two blueprint fields.

## 11. Antidote also treats lingering gas poisoning

**Plan/problem:** Authored Antidote calls CureEffect=PoisonedEffect and says it neutralizes poison (`Objects.json:5157–5219`), but lingering gas poisoning is a distinct negative effect with real damage (`PoisonedByGasEffect.cs:17–21,68–71`).

**Design:** Broaden the explicit ordinary-poison cure family to remove PoisonedEffect and PoisonedByGasEffect together. Do not remove the gas cloud, provide immunity or cure unrelated fungal/elemental effects. Sodden dressing's explicitly ordinary-only treatment stays unchanged.

**Acceptance/counter:** One Antidote cures either/both existing poison classes and consumes one bottle; remaining in gas can expose the actor again; unrelated Burning and positive effects survive. **Files:** CureTonicPart; medicine tests. Intentional medicine improvement.

## 12. Burn Salve actually cools a burning patient

**Plan/problem:** BurnSalve only removes BurningEffect. `BurningEffect.cs:88–95` heated the patient above FlameTemperature and `OnRemove:101–104` does not cool them. The hot material state remains after the medical action.

**Design:** For the explicit Burning cure path, remove the fire and cap existing ThermalPart temperature to its ambient baseline (never heat an already colder target). Do not add Wet, heal HP, replenish fuel, erase Charred or alter environmental heat sources. Subsequent real heat may burn the actor again.

**Acceptance/counter:** Burning/hot target becomes unburning and cooled with one salve; no-thermal target still cures safely; already cold target is not heated; Charred/poison and distant fire remain. **Files:** CureTonicPart; thermal medicine tests. This adds cooling; it does not claim a current automatic reignition bug.

## 13. Surviving a complete ordinary burn leaves usable charred aftermath

**Plan/problem:** Cinder/Charsplit describe waiting for Burning to expire into Charred (`Pyromancy_Cinder.cs:6–16`, `Pyromancy_Charsplit.cs:14–16`). Ordinary non-fuel creature burning sets finite Duration (`BurningEffect.cs:57–62`), but Charred is applied only on fuel exhaustion (`:126`). The promised ordinary combo is absent.

**Design:** When a living non-fuel owner finishes a naturally expired burn, apply one existing CharredEffect. Early cooling, salve, dispel, death and Pyroclasm removal do not earn residue. Keep fuel-exhaustion behavior unchanged and avoid duplicate Charred. Do not increase damage or burn duration.

**Acceptance/counter:** A surviving ordinary creature naturally finishes the burn, becomes Charred, and actual Cinder/Charsplit hooks now recognize it; curing early/death does not char; pre-charred target remains one instance. **Files:** BurningEffect; existing status and synergy fixtures.

## 14. Elemental flasks can affect physical scenery

**Plan/problem:** Splash currently filters all non-creatures (`ThrowItemCommand.cs:663`), although the same elemental effects and ObjectStatusMatrix already support wet, frozen, burning and corrosive physical objects. Water/fire/acid flasks cannot be used for their natural environmental purpose.

**Design:** Within the existing radius-one splash, eligible thermal/material/structural scenery can receive only elemental StatusTonic/BrewItem payloads through the current eligibility matrix. A Wet flask can dampen a timber object and suppress subsequent ignition; frost/acid retain their existing material rules. Healing, stat boost and cures remain creature-only, so a healing bottle cannot repair architecture. Retain one source unit, owner deduplication and friendly fire.

**Acceptance/counter:** Actual thrown Wet flask affects eligible timber; nearby ordinary creature remains affected; unsupported inert scenery and out-of-range objects do not; healing flask cannot restore object HP; actual body contacts work once. **Files:** ThrowItemCommand and a narrow payload-only apply seam if required; no world-generation or new reaction rules.

## 15. Toxic brew potency creates a real, bounded difference

**Plan/problem:** Brewing stores Poison potency (`BrewRules.json:44–51`, `BrewItemPart.cs:64–71`) but `TonicEffectFactory.cs:34–36` ignores magnitude for poison. Different-strength toxic brews currently deliver identical five-turn 1d3 poison.

**Design:** With no explicit authored duration/dice override, derive poison duration as 5 + 2 × (clamped potency 1–3 minus 1): 5/7/9 turns. Damage remains 1d3. Explicit StatusTonic duration/dice still wins. Mirror that duration mapping when converting toxic brew into an on-hit temper; its independent existing proc chance remains unchanged. Do not change first-dose poison refresh policy.

**Acceptance/counter:** Real reagent resolution yielding different Poison potencies produces distinct 5/7/9-turn effects; high potency caps at nine; explicit durations remain exact; temper preserves the same derived duration and one-unit expense; non-poison brewing unchanged. **Files:** TonicEffectFactory, WeaponTemperingService; brew/temper tests. Intentional completion of the documented potency mapping.

## Ownership, verification and performance

- Combat owner: listed skill/effect/item/repair/throw/tempering mechanics plus new tests and this doc. Root owns all Objects.json changes; the exact nine blueprint patches have been applied by root and recorded in `Docs/Verification/FiftyImprovements/combat-content-diff.json`. UI owner owns inspection text integration. By coordination, this owner also maintains the narrow TonicExamineService and EffectDescriber readouts for these mechanics. World-generation files and FoodPart/cooked meal improvements 46–50 are excluded.
- Shared-file risks: RepairablePart/Tier1Repairs may overlap world repair proposals; root coordinates and retains all structural counters. TonicExamineService is combat-owned by coordination; its surge duration/refresh text was updated alongside the mechanics. Existing generic factory preview already reads constructed poison duration.
- Each change receives a positive and a same-setup counter through existing command/effect/repair paths. Root observes native RED before production; source-only compile is not a test result. Save tests use replacement graph ownership, not ID-only equality.
- One-shot command allocations are bounded. No per-frame scan/cache/UI loop is added. New per-turn effects use existing StatusEffectsPart scheduling with direct fields and no per-tick collection allocation. Render changes use existing effect/equipment invalidation; portable repair must notify the existing equipment change bus if needed.
- Native verification can prove inputs, physical outcomes, payments and persistence. It cannot alone establish long-term fun, ideal proc balance, or every generated encounter; those remain explicit design hypotheses.

## RED fixture inventory

Seventy cases are authored across `FiftyCombatMovementTests` (15), `FiftyCombatStatusTests` (20), `FiftyCombatMedicineTests` (20), and `FiftyCombatEquipmentTests` (15). Full current runtime and EditMode test assembly compiled with all new source files included; this is source validation, not a native test result. This initial fixture inventory preceded the observed RED and implementation release. The subsequently saved `all-red.xml` includes the corrected valid dice literals and is the authoritative70-case RED receipt.

The exact authored portable scope is Dagger, LongSword, Mace, Battleaxe, IronHelmet (Metal / metal-equipment-brace), LeatherArmor (Leather / leather-equipment-stitch), and Cudgel (Wood / wood-equipment-splint). Each receives explicit `PortableEquipment=true`; no base blueprint opts in descendants implicitly. Root applied these Objects.json changes after the observed RED.

## Implementation and in-phase self-review

- Native RED receipt: `Docs/Verification/FiftyImprovements/all-red.xml` contains 70 combat cases, 37 intended failures and 33 passing counters. The exported XML includes the corrected valid dice literals; no malformed-dice failures remain in that receipt. Source-only compilation is separately logged under `/tmp/fifty-combat-runtime-compile.log` and `/tmp/fifty-combat-tests-compile.log`.
- All fifteen implementations are complete. Focused fixtures now contain 94 cases (the original70 plus a dedicated24-case `FiftyCombatAdversarialTests`), including ordinary command spending, actual thrown-poison kill XP, authored seven-item finite repair, replacement saves, repair rollback, tonic rollback, reference-bounded cure callbacks, splash arrivals and footprint holes. Root observed all94 focused gameplay cases passing in `review-red.xml`, then all95 cases including the12-check controlled witness in `focused-green.xml`; the subsequent829-case broader-correction receipt also passes, and the subsequent separate PlayMode witness passes12/12 combat checks.
- 🔴 Corrected before GREEN: finite stat surges are scheduled through the consuming command's existing AfterCommit observer, preventing a failed outer inventory action from retaining or refreshing the effect. Direct thrown/nontransaction payload delivery remains immediate.
- 🟡 Corrected before GREEN: cures enumerate original matching effect references once, so a new exposure created by a removal callback survives and cannot cause an unbounded removal loop.
- 🟡 Corrected before GREEN: portable repairs register undo before removing Broken. The existing narrow removed-effect rollback helper restores the original effect and its exact penalties while the material receipt restores paid units. Structural repair defaults and progression acknowledgment remain unchanged.
- 🟡 Corrected before GREEN: natural Charred requires a positive-to-zero owner-turn duration transition, the ordinary expiration cause, a live nonfuel owner, and no already-existing Charred. Setting duration to zero externally is not natural completion. Destructible gone/zero-HP owners are excluded.
- 🔵 Intentional design adjustments: 20-turn authored Speed/Strength preparations preserve old saved permanent Boost; actual two-hand axe chance is locally 6% against the retained one-hand3%; first active poison/bleeding dose retains damage source on refresh. No global damage, combustion or resistance rebalance.
- 🔵 Tumble queries BeforeMove permission without ordinary collision because its destination is occupied by design. TrySwap still validates both whole bodies. The other three mobility actives use ordinary TryMoveTo and its existing movement gate.
- 🧪 Focused95-case Unity EditMode GREEN observed, including the12-check controlled witness; targeted829-case broader corrections also GREEN; separate controlled PlayMode witness12/12 GREEN; the 6% axe tuning remains a feel hypothesis even after deterministic boundary coverage. Test fixtures do not establish an ordinary campaign encounter or long-term balance.

### Owned files

- `Assets/Resources/Content/Data/Repairs/Tier1Repairs.json`
- `Assets/Scripts/Gameplay/Skills/ShortBlades_Disengage.cs`
- `Assets/Scripts/Gameplay/Skills/Cudgel_ChargingStrike.cs`
- `Assets/Scripts/Gameplay/Skills/Acrobatics_Vault.cs`
- `Assets/Scripts/Gameplay/Skills/Acrobatics_Tumble.cs`
- `Assets/Scripts/Gameplay/Skills/Axe_Dismember.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/PoisonedEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/BleedingEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/HookedEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/BrokenEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/TonicStatSurgeEffect.cs`
- `Assets/Scripts/Gameplay/Effects/Concrete/TonicStatSurgeEffect.cs.meta`
- `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs`
- `Assets/Scripts/Gameplay/Effects/EffectDescriber.cs`
- `Assets/Scripts/Gameplay/Items/TonicPart.cs`
- `Assets/Scripts/Gameplay/Items/TonicExamineService.cs`
- `Assets/Scripts/Gameplay/Items/CureTonicPart.cs`
- `Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs`
- `Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs`
- `Assets/Scripts/Gameplay/Repairs/RepairablePart.cs`
- `Assets/Scripts/Gameplay/Weaponcraft/WeaponTemperingService.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatMovementTests.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatMovementTests.cs.meta`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatStatusTests.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatStatusTests.cs.meta`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatMedicineTests.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatMedicineTests.cs.meta`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatEquipmentTests.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatEquipmentTests.cs.meta`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatAdversarialTests.cs`
- `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatAdversarialTests.cs.meta`

- 🟡 Reciprocal review correction: repair rollback now subtracts only the actual penalty restoration made by this transaction, preserving independent callback-added weapon/armor improvements. The dedicated adversarial fixture verifies +3 accuracy/+2 armor survive a rejected outer repair.
- Dedicated adversarial file: `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatAdversarialTests.cs` and its32-hex meta contain the extracted23 post-RED cases plus this rollback delta counter. Original70 RED cases remain in their original fixtures, without inherited duplication.

Review RED gate: root requested a separate observed failure for the newly discovered independent-delta rollback case. The delta fix was saved as `/tmp/fifty-repair-delta-fix.patch` and only that undo block temporarily returned to absolute restoration. The native first-implementation receipt below observed the failure; the fix was reapplied and its counter subsequently passed. This preserves the original70-case RED and the separate review RED.

## First implementation receipt and controlled runtime witness

Root saved `Docs/Verification/FiftyImprovements/first-implementation.xml`. It observes the newly staged independent-delta repair failure, so `/tmp/fifty-repair-delta-fix.patch` was then reapplied. Two delayed-damage XP assertions required a fixture correction: `CombatSystem.HandleDeath` awards XP only to Player-tagged killers, and those fixtures had created ordinary creatures. Adding the Player tag corrects the test precondition; gameplay XP policy is unchanged. Their source attribution assertion already passed.

`FiftyCombatRuntimeBench` now provides12 explicitly controlled native-runtime checks through ordinary commands, real owner turn advancement, authored finite items and replacement saving: blocked/paid movement, rooted refusal, actual Broken penalty, missing/supplied repair cost,20-turn tonic refresh/expiration, thrown poison cost/source and loaded delayed kill credit. It grants the fixture abilities/conditions and uses fixed geometry; it does not establish keyboard combat, campaign discovery, or balance. The separate `FiftyCombatRuntimeBenchTests` executes the same witness before native integration. Clarity driver owner includes its count, observations, failures and completion separately from native reader checks.

New witness files: `Assets/Scripts/Scenarios/Custom/FiftyCombatRuntimeBench.cs` and meta; `Assets/Tests/EditMode/Gameplay/Combat/FiftyCombatRuntimeBenchTests.cs` and meta. The witness handoff had37 owned paths. The final `/tmp/fifty-combat-owned-paths.txt` has40 after the explicitly documented two legacy-test corrections and baseline proof JSON; root separately owns the nine Objects.json edits.

The subsequent `review-red.xml` receipt reports all94 combat gameplay cases passing. The newly added runtime-witness wrapper stopped before the witness at ScenarioContext's non-null caller precondition; its fixture now supplies a placed Player caller and clock, asserts that the witness preserves their context, and restores the prior active clock in `finally`. At that stage the wrapper had not established a completed witness; the later focused-green receipt below supplies that evidence in EditMode.

The `menu-bed-review-red.xml` receipt retains all94 gameplay passes and executes the runtime witness:11/12 checks pass, including replacement-save poison XP. The remaining witness failure was its helper confusing `BlocksTurnAdvance` (defer advancement while visual effects resolve) with paid-command success. The actual input handler uses `Handled` then advances ordinary movement immediately (`InputHandler.cs`, directional ability handling). The helper now follows that existing contract. Its unchanged movement/tick destination requirements now additionally verify cooldown25 immediately on success and24 after the owner turn; failed wrapper assertions include every observed state using ordinary anonymous-object string formatting. No gameplay change was required. The subsequent focused-green rerun completes12/12 in Unity EditMode; the later separate PlayMode receipt below also completes12/12.

Unity caught that the EditMode test assembly does not reference Newtonsoft.Json; the standalone compiler had a broader reference set and missed that boundary. Diagnostic formatting was corrected to ordinary `ToString`/`string.Join` without adding an assembly dependency. The accidentally started stale-DLL run is excluded from evidence.

## Focused GREEN and per-iteration state

Authoritative receipt: `Docs/Verification/FiftyImprovements/focused-green.xml`. All classes below are in `CavesOfOoo.Tests`: Movement15, Status20, Medicine20, Equipment15, Adversarial24 and RuntimeBench1, total95 passed. The last case executes and checks all12 runtime-witness observations. Counts describe validation, not additional improvements.

| Iteration | Implementation and focused state | Primary acceptance/counter coverage |
| --- | --- | --- |
| 1 Disengage payment | Implemented; GREEN | `FiftyCombatMovementTests.DisengagePaysForCommittedTravelOnly`, trap entry and redirected-entry payment; witness verifies3-cell movement, one paid owner turn and cooldown25→24. |
| 2 Rooted mobility | Implemented; GREEN | `RootedActorCannotUseVoluntaryMobilityButUnrootedControlCan` covers four skills, plus adjacent rooted Charge and externally forced displacement controls. |
| 3 Physical splash contacts | Implemented; GREEN | `FiftyCombatMedicineTests.SplashHitsExposedSecondaryBodyExactlyOnce`; adversarial captured arrivals, overlapping contacts and footprint holes. |
| 4 Delayed kill source | Implemented; GREEN | `FiftyCombatStatusTests.DelayedDamageCreditsItsRealInflictor`, `FirstDoseCreditSurvivesStackAndReplacementSave`, actual thrown-poison finish; witness confirms loaded Player source and7XP. |
| 5 Bleed potency | Implemented; GREEN | `BleedingStacksByExpectedPotencyWithoutUsingRandomness`: stronger/reverse/equal/invalid dice and independent save difficulty. |
| 6 Hook lifetime | Implemented; GREEN | `HookCannotOutliveCurrentSameZoneWielder`: absent/dead/foreign-zone versus valid placed source. |
| 7 Broken penalty | Implemented; GREEN | `FiftyCombatEquipmentTests.BrokenPenaltiesAreBoundedOptInAndRestoreOnce`, actual Hammer proc; witness preserves usable equipped damaged gear. |
| 8 Physical repair | Implemented; GREEN | `PortableRepairSpendsOneRealMaterialAndCanRepairLaterDamageAgain`, malformed ownership, authored seven-item recipes, replacement save, outer rollback and independent callback deltas. |
| 9 Two-hand axe role | Implemented; GREEN | `ActualTwoHandAxeHasSixPercentWindowWithoutChangingOtherHits`: actual two-hand binding, striking weapon, paid damage and one-hand controls. Rate remains an unproven feel choice. |
| 10 Finite stat tonics | Implemented; GREEN | `AuthoredSurgeRefreshesThenExpiresWithoutErasingLegacyBoost`, independent stats, failed outer action; witness checks two real doses, refresh without stacking and exact20-owner-turn expiration. |
| 11 Poison-family cure | Implemented; GREEN | `OneAntidoteTreatsPoisonFamilyAndPreservesOtherEffects` and `AntidoteDoesNotConsumeNewExposureAddedByRemovalCallback`. |
| 12 Salve cooling | Implemented; GREEN | `BurnSalveCoolsExistingThermalStateWithoutHeatingOrCuringOtherAilments`, early salve does not create Charred. |
| 13 Natural Charred | Implemented; GREEN | `OnlyCompletedLivingOrdinaryBurnCreatesCharred`, existing Charred nonduplication and forced-zero-duration refusal. |
| 14 Elemental scenery flask | Implemented; GREEN | `ElementalSplashCanDampenSceneryButMedicineCannotRepairIt`, mixed scenery payload admits only elemental effects. |
| 15 Toxic brew potency | Implemented; GREEN | `ToxicBrewPotencyBecomesBoundedPoisonDuration` and `PoisonTemperPreservesPotencyDurationAndSpendsOneQuench`:5/7/9 cap, explicit duration and actual one-unit quench. |

**Can verify now:** compiled gameplay behavior under Unity EditMode, precise action/cooldown and finite-resource counters, physical ownership, rollback, and replacement-save identity. The controlled witness uses authored items and real command/scheduler/save paths with disclosed granted abilities and fixed setup.

**Cannot verify from this EditMode receipt alone:** execution in PlayMode, keyboard combat, ordinary campaign acquisition, visual quality, player enjoyment or long-term balance. The separate PlayMode receipt below closes only its declared controlled-runtime and reader surfaces. The initial broader run, its targeted829-case corrections and the subsequent separate native PlayMode witness are recorded below. All Assets remain frozen; this update changes documentation only.

## Broad-review baseline correction: document source test

The wider regression run reached `DensityDocumentSourceTests.EveryShippedTextMatchesCanonicalWordsAndOrder`, which treats every catalog `Source` as a literal filename. The test, `CodexDocuments.json`, and `ReadableDocumentCatalog.cs` are byte-identical to baseline `bfa625f44`; SHA256 and source-check receipts are in `Docs/Verification/FiftyImprovements/document-source-baseline.json`. The thirteen canonical Codex copies still match their repository titles and exact normalized prose. Three pre-existing Curation records are original local texts whose Source contains annotated lore attribution (`...03_PaleCuration.md; original Marrowstye receiving record`), deliberately pinned by existing readable-document/adversarial tests. This is a demonstrated baseline source/test mismatch, not a baseline Unity rerun or a changed gameplay document.

The accepted narrow correction is test-only: select the explicit expected `Codex01`–`Codex13` blueprint identities, assert their complete set, then retain exact title/prose comparisons. Selecting by identity rather than a `Source` prefix means a corrupted canonical path still fails rather than silently dropping out. Existing exact16-entry catalog, three-local-record attribution, readable ownership, malformed replacement, and read/save tests retain original-record coverage. Splitting the metadata at its semicolon would be wrong: the original local prose does not claim to copy the faction article verbatim.

After the broader native run observed the failure, root approved applying `/tmp/fifty-document-source-test.patch`; that test-only correction is now on disk and its14-case class passes in `corrections-green.xml`. The modified test path and source-proof JSON are added to the combat owned manifest for change accounting only; this maintenance correction is **not** an additional improvement or a gameplay implementation. The manifest also includes the poison regression maintenance below and now has40 paths; both updated fixtures pass their final targeted native rerun.

## Broader regression pin migration: antidote scope

The broader native run completed15,889 cases (15,848 passed,41 failed). One failure is an intentionally superseded poison regression: `PoisonBalanceRegressionTests.ActualTreatmentConsumesOneCuresOrdinaryPoisonAndLeavesGasPoison` expected Antidote to retain gas poison. Iteration11 deliberately makes that existing poison antidote cure both ordinary and gas poison; the focused original RED/GREEN already covers this change.

The retained two-case integration test now states each treatment's distinct scope: Antidote cures both poison forms while leaving bleeding; SoddenFieldDressing cures ordinary poison and bleeding while retaining the exact gas-effect owner, duration and damage. Both still consume exactly one unit from a real two-unit carried stack, retain the remaining owner and do not heal HP. The existing gas-only dressing refusal/zero-consumption case remains unchanged. A new post-RED counter confirms gas-only antidote consumption and that a later real gas exposure can apply again: this cure grants no immunity. No poison damage, snake-dose duration, treatment blueprint or production implementation changes accompany this test migration. The subsequent `corrections-green.xml` native rerun passes all14 `DensityDocumentSourceTests` and all30 `PoisonBalanceRegressionTests`, including the new re-exposure counter. The edit itself was not counted as evidence.

## Final targeted EditMode verification

`Docs/Verification/FiftyImprovements/corrections-green.xml` reports **829/829 passed**. The receipt includes all95 focused combat cases, the12-check controlled witness wrapper, all14 document-source cases and all30 poison-balance regressions. Thus the canonical-source correction and intentional antidote-scope migration are natively verified, alongside retained snake dose, ordinary dressing and finite-payment controls.

This829-case run is the targeted correction/neighbor rerun following the broader15,889-case receipt, not a second complete unfiltered run; retain the original broad failures and the exact subsequent scopes. No failed or stale-DLL attempt is reclassified as passing. At that receipt stage the root-owned separate PlayMode run was in progress; the completed receipt below supplies its later evidence. Assets remained frozen throughout this final read-only review and documentation update.

## Separate PlayMode receipt

Root's native PlayMode run `7cc81439e2f3478a827eaa48c8b598ba` completed in10.72 seconds with **60/60 checks passed**, no reported errors and no fatal exception. Raw report: `Docs/Verification/FiftyImprovements/NativeClarity/7cc81439e2f3478a827eaa48c8b598ba/report.json`. Its distinct groups are13 reader/controlled-start checks,20 meal checks,12 combat checks and15 world checks. Root independently inspected all6 saved UI captures and reported them readable.

The combat subrun `f3476ec17ebc4ba0a6c9e6065dc29565` completed12/12. Raw observations include blocked Disengage tick10→10/cooldown0; clear movement to(13,10), tick10→20 and cooldown25→24; rooted Vault refusal; equipped damaged dagger accuracy0→−2; material repair leaving one of two steel components and restoring accuracy0 at tick20→30; Strength16→20, a refresh at20 turns without stacking, then exact expiration at tick100→300; one actual thrown poison unit followed by replacement-save source restoration, victimHP0 and normal7XP. These are actual direct command/scheduler/save outcomes in PlayMode, with disclosed fixed geometry, granted skills/conditions and authored finite items.

The native key evidence applies to readers/search/loot navigation. The combat, meal and resource benches are detached direct-command witnesses; this receipt does **not** prove keyboard combat, ordinary acquisition/discovery, survival in generated encounters or long-term balance. Earlier wrapper/setup failures remain recorded; later passes followed explicit source or fixture corrections, and the successful receipt does not overwrite those failures.

A subsequent independent world review identified a deferred-rest lifetime edge: an outer callback could kill the actor between scheduling and commit, while queued healing lacked the living check already used by meals and tonics. No shipped killing listener was found, but root approved two bounded adversarial cases and a fix that keeps committed time while suppressing dead-owner benefits and reporting interruption. That separate world correction was then observed RED, fixed and verified by the final831-case and final-source PlayMode receipts below. The earlier PlayMode receipt is not retroactively treated as proving the later change. Combat Assets remain frozen.

## Final-source closure

The two deferred-rest adversarial cases were observed failing before the world owner added one commit-time living-state snapshot. Final cold review confirms the queued path always retains committed clock cost, suppresses healing and bleeding removal for a dead or death-handled owner, and emits `RestInterrupted` with `actor_dead` instead of positive `Rested`. The direct synchronous bed/conversation branch remains unchanged. `Docs/Verification/FiftyImprovements/final-green.xml` then passes **831/831**; this is the previous829-case correction/neighbor scope plus those two lifetime cases.

The final-source PlayMode report is `Docs/Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/report.json`: **60/60**, zero failures/errors, no fatal exception,10.74 seconds, all6 saved UI captures independently reviewed by root. Combat subrun `05b3cdb54f2d4631ba5b698297981cae` is **12/12**, retaining the same exact movement/payment, finite repair,20-turn tonic and loaded-source7XP observations described above. The final run occurred after the rest correction; it is the current PlayMode receipt.

All15 combat iterations and the two explicit legacy-test pin corrections are ready for root's milestone commits. There is no remaining known blocking issue in this owned scope. Evidence still does not establish long-term balance, ordinary content acquisition or keyboard combat: the combat witness is a disclosed controlled direct-command/scheduler/save scenario. No Assets changes were made during this final documentation/review pass.

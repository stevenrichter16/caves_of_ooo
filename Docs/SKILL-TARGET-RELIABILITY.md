# Skill target and movement reliability

Status: implemented after observed native RED; all 109 new target/payment cases and related legacy cases pass within the 4,734/4,734 affected-system native Unity run. This is a bounded Caves of Ooo correction, not a new Qud-parity claim.

## Contract and scope

Frostbind, Pyroclasm, Tumble, Recruit and Dismiss must respect the selected physical adjacent cell. An explicit empty, remote, foreign, aliased or stale cell refuses without changing another neighbor, drawing recruitment RNG or spending cooldown/action. A missing target retains the legacy first-adjacent behavior for existing internal callers. Creature skills reuse `SkillCombatHelpers.FindAdjacentSkillTarget`; Pyroclasm retains burning physical objects, so its narrow local selector must apply equivalent placement/contact validation while using `AbilityTargeting.IsElementalTarget`.

Tumble swaps the selected complete bodies through the existing atomic movement path. Its confusion rider applies only when the target is actually hostile according to `FactionManager.IsHostile(target, actor)`, including existing party and personal-hostility precedence. Neutral and recruited creatures swap without confusion. No new faction mechanism is introduced.

Charging Strike remains usable in an empty lane. Once any move has committed it is a successful paid ability, even if a wall, trap, death, removal or relocation ends the trajectory before an attack. Only no movement and no attack is a free refusal. Track committed movement rather than comparing final coordinates: an entry reaction can return the actor to its starting cell. Preserve existing distance, damage, terrain entry and weapon rules.

The spell-damage owner supplies `SpellDamageHelpers.ApplySpellDamage(target, amount, "Heat", actor, zone, "Fire")`; the final Pyroclasm edit uses that API and preserves both old elemental attributes and structural damage. Shared spell/buff code is outside this milestone's ownership.

## Executed source-verification prompts

1. Trace the directional input `TargetCell` through `SkillsPart.HandleEvent` and each of the five abilities; compare against the existing eight weapon skills. Confirm missing-target behavior and physical-body validation before reusing a helper.
2. Trace party assignment, hostile feeling, swap landing and cell-entry effects. Determine whether a recruited companion receives an Ally tag and whether an interrupted charge may have committed movement despite ending at its origin.
3. Read existing command, multi-cell, trap and input-clock fixtures. Design positive/counter/adversarial checks through actual command dispatch, plus the existing input completion and scheduler for charge payment, without invoking Unity or altering production first.

## Source corrections

| Initial premise | Verified source | Consequence |
| --- | --- | --- |
| Every adjacent target is a creature. | Pyroclasm uses `AbilityTargeting.IsElementalTarget`; structural/material/thermal objects participate. | Do not replace its selector with the creature-only helper. |
| Ally tag identifies recruited followers. | `RecruitedEffect.OnApply` calls `BrainPart.SetPartyLeader`; `FactionManager` resolves party alignment and personal grudges. | Query actual hostility; neutral creatures are not hostile either. |
| A charge ending without a target did nothing. | `ForceMoveTo` fires `AfterMove`, entry triggers and slip resolution before the target-null branch. | Pay for committed movement; do not refund interrupted trajectories. |
| Final position reveals whether movement occurred. | An entry callback may relocate/remove/kill the actor. | Retain a movement-committed flag inside the loop. |
| Evasive Roll needs an action-block exception. | Command dispatch permits the cleanse while blocked-player turns yield input. | No Evasive Roll or global action-veto changes. |
| The eight melee active selectors still need correction. | Their helper validates physical selected cells and refuses aliases. | Reuse them; keep their implementation outside scope. |
| A structural test object can burn without any material identity. | `ObjectStatusMatrix` requires a flammable material or tag even when structural HP exists. | The controlled timber fixture carries `Flammable` and uses normal `ApplyEffect`; corrected before RED. |

## Verification plan

New command fixtures cover both eligible neighbors, empty selection, missing-target compatibility, actor/target secondary body contacts, cooldown re-fire, invalid cell/owner aliases, an unburning selected Pyroclasm neighbor beside burning matter, and an actual burning structural owner with blast centered on the selected contact. Tumble tests distinguish neutral, hostile, recruited, and personally hostile former-friendly relations.

Charge tests cover full/partial/blocked travel, adjacent attack, wrong weapon, a real consumed spike trap and relocation/removal after committed entry. Actual `InputHandler.ResolveAbilityCommand` with a quiet registered Speed100 actor must advance ten scheduler ticks for committed travel and zero for a true refusal. This is controlled EditMode input integration, not delivered keyboard input or an ordinary encounter claim.

No new per-frame path, cache, rendering mechanism or framework is introduced. Existing physical queries and movement notifications remain authoritative. Test-only allocations are acceptable.

## Implementation and self-review log

- Root observed the 109-case native EditMode RED and saved `Docs/Verification/SkillsEngagement/red.xml`: main targeting 27 failed / 14 passed; adversarial targeting 55 failed / 0 passed; charge payment 9 failed / 4 passed. Production remained unchanged until explicit release after this run.
- Frostbind, Tumble, Recruit and Dismiss now use the existing authoritative selected-creature helper. Pyroclasm has an equivalent local physical-owner selector that preserves burning scenery. No helper or global targeting framework was added.
- Tumble uses actual hostile feeling; neutral and party-aligned targets no longer receive confusion solely because they lack an `Ally` tag. Explicit personal hostility retains precedence through existing faction rules.
- Charging Strike retains a committed-movement flag and returns success after movement, including interrupted trajectories. Only a zero-movement, zero-attack result emits the existing `SkillRejected/no_target` refusal. `CommandRouted` remains the existing successful dispatch evidence.
- Pyroclasm uses the agreed common spell-damage API, retaining Heat and Fire typing and structural routing. Its log calls the shared pre-modifier amount **base** damage instead of falsely promising equal final damage on every target.
- Existing Tumble tests now create actual hostile and allied relationships. The old empty-lane charge pin now requires success and no rejection diagnostic; its movement assertion is retained.
- **🟡 Resolved, source symmetry:** Pyroclasm cannot use a creature-only selector; local validation includes current zone membership, actual body contact, live/undestroyed owners and carried/equipped exclusion.
- **🟡 Resolved, atomicity:** final position comparison would refund a charge after a return-to-origin trigger. The flag records successful movement before inspecting its aftermath; relocation, removal and lethal spike tests cover this.
- **🔵 Resolved, description drift:** Dismiss's source comment now distinguishes no cooldown from free action. Its normal action payment is unchanged.
- **🧪 Root-observed initial GREEN:** all 109 new target/payment cases and the related legacy cases passed in the first implementation run. These controlled fixtures verify simulation and input-completion contracts, not keyboard delivery, visual clarity or ordinary encounter feel.
- Independent review of the parallel control/progression work found outdated Bludgeon stacking and Dismiss purchase pins. After root approval, the two Bludgeon integrations now prove real class/passive/root dispatch, no passive extension, retained root stacking and a non-vacuous six-turn ceiling. The Dismiss progression fixture earns two actual level awards to purchase Persuasion and Recruit, then acquires Dismiss with zero remaining SP. These additional migrations passed in the broader native run.
- Standalone compilation of current runtime and the complete current EditMode assembly succeeded after the migrations; this is compilation evidence only, with no tests or Unity run performed by this owner.
- No balance values, JSON, UI, global movement rules or spell resource policy are owned here. Broader skill-engagement work is coordinated separately.

## Owned files

- Six production classes in `Assets/Scripts/Gameplay/Skills`: `Cryomancy_Frostbind`, `Pyromancy_Pyroclasm`, `Acrobatics_Tumble`, `Persuasion_Recruit`, `Persuasion_Dismiss`, `Cudgel_ChargingStrike`.
- New tests and metadata in `Assets/Tests/EditMode/Gameplay/Skills`: `SkillTargetReliabilityTests`, `SkillTargetReliabilityAdversarialTests`, `ChargingStrikePaymentTests`.
- Related fixture migrations: `AcrobaticsTumbleTests.cs`, `CudgelChargingStrikeTests.cs`; root-authorized independent-review migrations in `OnHitSkillEffectsTests.cs` and `ExplorationDepthProgressionTests.cs`.
- This document. Shared damage implementation, JSON and presentation changes belong to the other milestone owners.

Native regression confirmation (root): `Verification/SkillsEngagement/broad-green.xml` passes **4,734/4,734**, zero failures or skips, 121.62 seconds. This is the affected-system suite in Unity Editor, not the standalone runner and not a whole-game balance verdict. All 283 selected fixture classes are recorded in the XML. Native Play receipt is recorded in the implementation overview after completion.

Final acceptance and remaining play-feel limits: [SKILLS-AND-SPELLS-IMPLEMENTATION.md](SKILLS-AND-SPELLS-IMPLEMENTATION.md). Final isolated native reader run `1e6e8a3575564a818d590d20481539cd` passes ten keyboard checks and six separate spell/save checks with zero errors; final reader-only polish passes 91/91 after the 4,734/4,734 broader run.

# Companion care and shared meals — item passes 01–18

Status: implemented; native Unity EditMode regression GREEN. The parent plan is `Docs/ITEM-UTILITY-35-PASSES.md`. Parent-owned isolated Play-mode menu verification is in progress.

## Player-facing invariant

A carried remedy can treat a living, adjacent party companion who actually needs its existing payload. A carried cooked meal can prepare that companion for the next danger. One supply and one actor action are spent only on a useful successful treatment. Self-use remains its existing separate action. Ordinary friendly strangers, hostile former companions, distant targets and corpses are not eligible. An incapacitated companion can receive help; the acting helper must be able to act.

Known-party care is intentionally a touch interaction: an actually adjacent, willing party member can receive help in obscuring cover without a visibility check. This supports rescue inside veilpuff cover. It differs from sight-gated offensive material targeting; it does not offer strangers or enemies or extend physical reach. Peer review raised this distinction and the parent accepted it explicitly.

The inventory offers a named `TreatCompanion|…` or `ShareMeal|…` choice with the recipient and payload. The command binds the zone, both anchor positions, exact recipient ID, selected source quantity and payload signature. Validation occurs at selection, dispatch, after payload callbacks and immediately before the outer transaction commits. No attack roll, thrown collision, forced swallowing by enemies, resurrection or autonomous NPC item-selection policy is introduced.

## Source verification corrections

| Source read | Verified implementation and design consequence |
|---|---|
| `TonicPart.ApplyTo` | Can address a separate recipient, but returns true even for a no-op and receipts only source consumption. Aid must gate actual need and enlist recipient changes. Existing drink payloads reduce one Parched stack. |
| `CureTonicPart.HandleEvent` | Poison cures include `PoisonedByGasEffect`; `All` selects negative effects while preserving boons. Burning-specific cures cool to at most ambient. The broad panacea itself does not add cooling. |
| `SoddenDressingPart` | Not a tonic. Cures exact ordinary `PoisonedEffect` and `BleedingEffect` only; excludes gas poison, fungal infection and healing. |
| `FieldMealPart` | Not `FoodPart`. Heals 3d4 and removes one exact ordinary bleed; does not grant or replace a prepared-meal effect. |
| `FoodPart` / `PreparedMealEffect` | Five selected cooked foods heal and grant one saved meal benefit. A later meal replaces the previous one on the same effect object. Shared preparation must respect native effect veto; a fully healed recipient with an identical full-duration meal has no benefit. |
| `BrainPart.ArePartyAligned` | Supports a player's followers and siblings sharing a final leader. Personal hostility can override the party tie and must be checked separately. |
| `WorldResourceActions` | Existing exact inventory ownership, physical footprint adjacency and current ground checks can be reused. Recipient validation must not reuse ActorCurrent's frozen/action-block refusal. |
| `StatusEffectsPart` | Effect removal invokes OnRemove, emits lifecycle callbacks and stops native auras. Reinserting an effect alone does not restore stat/part bookkeeping or aura presentation. |
| `BloomedEffect` / `WitnessedEffect` | Removal deletes a privately tracked pacing goal. Panacea rollback must preserve that goal and bookkeeping, while retaining independent goals added by callbacks. |
| `Objects.json:SumpsievePad` | Existing description says gas poisoning is excluded, but its actual PoisonedEffect cure also treats gas. Correct the description through the parent content edit; do not narrow the shipped cure. |

## Payload ledger

| Items | Existing payload retained |
|---|---|
| HealingTonic / KnitmossPad | 4d6+4 / 2d4 healing, capped at the recipient's actual HP maximum. |
| Antidote / SumpsievePad | Ordinary and gas poison cure; no protection against another exposure. |
| BurnSalve / SootrootPulp | Burning cure and existing ambient cooling; no fire immunity. |
| Panacea | Existing negative ailments, preserving positive effects and party allegiance. |
| KnotflaxBandage / ClaspbeanPulp / MargincressRibbon | Existing ordinary bleeding cure. |
| SoddenFieldDressing | Ordinary poison and bleeding together; no gas cure. |
| AbsentmintLeaf | Confusion cure, including reversal of its actual DV/Agility penalties. |
| CookedMeat | 3d4 healing, +2 Toughness for 100 recipient turns. |
| ToastedEmberwheat / RoastedMushroom | 3d4 healing, +20 heat / acid resistance for 100 recipient turns. |
| RoastedHearthbulb | 2d4 healing, +20 cold resistance for 100 recipient turns. |
| RoastedStarapple | 3d4 healing, +1 DV for 100 recipient turns. |
| FieldMeal | 3d4 healing plus one ordinary bleed; retains any previous prepared meal. |

Ordinary source evidence is the original obtainable-item catalog and `Next35-proposals-crops.md`. Companion tests will pin all 18 real resolved blueprints and their unmodified self-use action; parent source tests cover the supply routes. These are eighteen item passes sharing care and preparation systems, not eighteen independent mechanics.

## Implementation boundaries and receipts

`CompanionCareActions` exposes the same `AddActions`, `IsCommand`, and internal transaction-bearing `TryAct` contract as the other inventory utility helpers. Parent owns inventory/UI/paid-turn routing. The service reads existing payload fields, limits the feature to the eighteen audited blueprints, and rejects unsupported harmful/custom tonic payloads. Source recipes and old consuming actions remain unchanged.

Recipient rollback must restore only this treatment's changes. Preserve exact removed effect references/order/cause and their bookkeeping, reverse stat/thermal/material deltas, resume removed effect auras, and restore removed pacing goals. For meals, restore the prior single meal and stat shift on veto/outer failure. New independent conditions, stat damage, goals or ownership changes introduced by an outer observer must not disappear. Payment and participant claims use the existing inventory receipt; after-commit diagnostics and render refresh are separately isolated callbacks.

The source sweep required a bounded lifecycle addition: internal receipt callbacks bracket intrinsic `OnApply`/`OnStack`/`OnRemove`, capture partial work in `finally`, and run before public lifecycle observers. Existing public APIs retain their behavior. A companion `Entity` facade overload retains the material/immunity gate and spell FX capture. This prevents a failed cure from rewinding an independent condition applied by an `EffectRemoved` observer. Reflection captures the exact treated effect's existing fields, including private pacing-goal ownership; it does not serialize or rebuild the entity.

## Test plan

1. Real-menu positive case for every selected remedy and prepared meal, exact single-unit spend, recipient outcome and unchanged helper.
2. Unaffected target, stranger, hostile party member, corpse, distance, stale position/quantity/identity/payload, malformed command and incapable helper counter-cases.
3. A paralyzed/confused/frozen recipient remains treatable as appropriate; party affiliation is revalidated after callbacks.
4. Poison-family distinctions, no broadened dressing cure, panacea boons, field-meal one-bleed scope, resistance replacement and saved meal expiry.
5. Meal immunity/veto and no-op protect the supply. Before/after-action exceptions and late final-validation failure restore recipient and source; reentrant actions cannot spend twice.
6. Stat/material/thermal/goal/aura restoration; independent callback changes survive rollback. Native UI and paid-turn checks are parent-owned.

## Evidence and limits

Native RED confirmed in `native-three-groups-red.xml`: 43 companion ordinary cases (19 passed, 24 failed), 24 adversarial cases (24 failed), with missing real inventory choices. Production followed this gate. Three intrinsic/lifecycle-boundary counters and four paired late-recipient-record checks were added during self-review before first GREEN.

Self-review finding (fixed): the first final validator did not bind the recipient health record or treated status-manager identity. Four paired regressions target a post-action replacement, preserving independent replacements while refusing payment for a detached result. Native failure was confirmed before the fix.

First native GREEN attempt (`native-first-green-attempt.xml`) confirmed two real late-record-replacement failures; the final validator now binds the recipient HP record and any status manager actually mutated. Two fixture premises were corrected: MarlbackScrabbler has innate cold resistance, so meal replacement must preserve that baseline rather than assert zero; `new PoisonedByGasEffect()` has default Duration 0, whereas real gas exposure authors a positive duration. The gas-cure cases now use Duration 5, with explicit expired-poison refusal counters. These corrections change no production cure scope.

Peer review found a pre-apply preparation race (fixed): an independent `BeforeApplyEffect` listener may insert/change the previous meal before native stacking begins. Four paired tests demand refusal before intrinsic mutation while preserving that independent meal. Targeted RED preceded the guard.

`native-review-red.xml` confirmed both changed-meal branches fail before the guard. The implementation now validates the exact previous meal record and its preparation fields at the intrinsic boundary, as well as the selected incoming payload. A tampered-payload counter accompanies the fix. The last bounded receipt case covers a later independent meal replacing this transaction's meal before an outer failure; four paired cases received their own RED below. No general stacking or status redesign was needed.

`native-final-meal-review-red.xml` ran 260 combined cases: 258 passed, with only the two later independent-meal replacement branches failing (orphan heat resistance −20). The narrow fix records the preparation immediately after the intrinsic mutation, then yields ownership if a later native meal/removal has already unapplied it. It leaves the independent preparation intact and still refunds this operation's healing and supply. Generic stacking behavior is unchanged.

Final native Unity EditMode evidence: `native-final-regression-green.xml` — **2,005/2,005 passed**, including **85/85 companion cases** (43 ordinary and 42 adversarial). This verifies actual native inventory dispatch, all eighteen resolved item payloads, recipient bookkeeping, save round-trip, veto/refund paths and paired lifecycle/ownership counters. These are real Unity tests, not the non-Unity runner. Parent-owned native Play-mode menu/turn checks are separate and were not yet complete when this group report was updated.

## Bounded final review

- Actual payloads come from the eighteen existing resolved blueprints, with explicit custom dressing/field-meal handling. Remedies do not gain new immunities or cure categories.
- Native effect gates, observers and auras remain in the path. Intrinsic receipts isolate this operation from independently added conditions, damage and later preparation.
- Source selection, recipient reach/alignment/liveness, health identity and affected status-manager identity are validated through final commit. Refused or failed actions preserve their supply.
- Prepared meals remain one saved benefit, use recipient turns and replace prior preparation. Touch-based known-party care in cover is intentional; stranger/enemy targeting remains excluded.
- Peer review found no ordinary healing/cure/payment blocker. The three concrete transaction findings above received paired regression tests and confirmed native RED before their fixes. Further speculative callback combinations are outside this bounded pass; no general status-system rewrite was undertaken.

Controlled tests establish transactional mechanics and save behavior, not companion usefulness in ordinary encounter balance or visual readability. No scene screenshot or ordinary encounter playtest is claimed by this group report.

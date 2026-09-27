# Qualified player combat feedback

Status: complete for the four melee message sites; native 118/118 GREEN and actual corpse-route replay 14/14 PASS, with exact editor restoration.

The actual corpse-harvest Play route `Scenery/NativeCorpseAcceptance/6ba32133d91e40829b0cbbfa23742606` records `You [left hand: dagger] hits sun-striker ...`. Existing `PlayerMessageGrammar.Normalize` handles a verb immediately after literal `you`; a hand/weapon qualifier or `CRITICALLY` separates the subject and verb. This is a player-visible C4 completeness gap, observed during the requested content audit.

Source sweep: `CombatSystem.PerformSingleAttack` owns exactly four corresponding messages: miss, failed penetration, zero rolled damage, and landed ordinary/critical damage. Qualifiers are generated upstream by the actual equipped body-part attack. The generic message normalizer deliberately avoids rewriting quotations and lore. A player tag alone does not make a named subject grammatically second person, so use the same literal `you` subject contract as the existing normalizer.

Plan: execute actual single-attack branches through deterministic combat with player/NPC subjects and qualified/unqualified sources. Require correct verbs and meaningful damage/refusal controls. Then choose the second-person verbs at those four message sites, retaining critical labels, source names, targets, body location, post-resistance damage and event ordering. No general prose parser, damage, RNG, skills, faction, source or save change is needed. Keep NPC wording and quoted/announcement text unchanged. Run the new fixture with existing combat, critical-message and ordinary grammar neighbors; rerun the short actual corpse route to inspect real equipment-qualified output.

The original defect frame/report remains immutable. Native assertions establish the actual log and combat outcomes; the viewed replay is separate evidence for legibility. This does not rewrite every compound English sentence or claim a whole-game copyedit.

## Implementation and executed evidence

`CombatSystem.PerformSingleAttack` selects second-person hit/miss/fail/deal from its original displayed subject before adding hand/weapon qualifiers. Critical damage keeps `CRITICALLY`; named subjects retain third-person verbs even if tagged Player. The four existing branches, their damage values, event ordering and random rolls remain unchanged.

- Native RED: 20 actual-attack cases, eight failed and 12 controls passed before the repair (job `1c3216d5d4cf435f9c37e30067dc7fc3`). These cover five outcomes × two subjects × qualified/plain sources, actual damage/refusal and the UI message observer.
- Native GREEN: the same20 plus98 existing combat, critical-hit, ordinary grammar, multiweapon, dead-attacker and resistance neighbors passed118/118, zero skips/failures (job `c0160625f52e4e42be3fc55c286be38b`,3.1263582seconds).
- Unchanged actual corpse driver replay `a999307e575f495b9b60e1ea16ec7aa8` passed14/14 in13.6044486seconds, zero runtime errors and exact scene/start-scene/seed/save-root/last-game/background/input restoration. Real melee produced qualified miss, critical-hit and ordinary-hit lines with the correct player verbs. Combat RNG was not forced: this run included retaliation, bleeding and one original healing tonic; the actual earned yield was two RawMeat. The earlier one-meat/no-tonic run remains separate evidence.

Raw RED/GREEN results, production preimage/patch and root review are in `Verification/DensityCompletion/CombatFeedback`. Full replay keys, observations, screens and complete Unity log byte range are retained in `Verification/DensityCompletion/Scenery/NativeCorpseAcceptance/a999307e575f495b9b60e1ea16ec7aa8`.

## Q1–Q4 self-review

- Q1: all four melee branches select verbs from the same original subject; player and named cases remain symmetric, including secondary fail/deal verbs and the critical adverb.
- Q2: the existing `PlayerMessageGrammar` literal-you rule is retained. Lore, quotations, names, event order, damage and callbacks are untouched.
- Q3: every outcome has qualified/plain and player/named controls; named subjects are deliberately Player-tagged to catch accidental tag-based rewriting. HP and the published UI message are checked against actual combat execution. Independent peer review found no concrete blocker.
- Q4: status, counts and scope match raw XML, actual replay and the reviewed five-hunk production patch.
- 🟡 resolved: equipment-qualified and critical player verbs plus secondary refusal verbs now pass native RED→GREEN.
- 🧪 separate newly observed follow-up: the replay also contains `You stops bleeding.` from a status-effect site. This was outside the four-site melee checkpoint; the separate follow-up below now closes it.
- ⚪ honesty: viewed frame03 shows corrected qualified miss/hit feedback and preserved NPC hits; its rightmost log text is clipped at the capture edge. It is a foreign Beating biome capture, not evidence of Spread art. Assertions and raw logs prove exact text; the frame proves its visible presentation only within that crop. Broader English copyediting, every biome and combat balance are not established here.

## Follow-up: observed status-effect verbs

Plan recorded before follow-up production: the native replay exposed `You stops bleeding.`. Source sweep found `BleedingEffect.OnRemove` and `SmolderingEffect.OnRemove` both use immediate `stops`, and Smoldering's application uses immediate `smolders`. These are exactly the existing literal-leading-subject normalizer's scope; add only those two known verbs after actual effect apply/remove tests reproduce the failures. Pair literal-you with Sella, require actual effect removal, and retain quoted/announcement/multiline safeguards. Do not alter effect damage, duration, recovery or callbacks. Focused native everyday-grammar and effect neighbors are the verification gate; the earlier raw gameplay receipt remains unchanged. The test's initially assumed collection name was corrected to the actual `EffectCount` API before behavioral RED; no production change was made for that compile error.

Completed: added only `stops→stop` and `smolders→smolder` to the existing exact verb dictionary. Four actual effect lifecycle cases pair both effects with literal-you/named subjects, assert real removal, and compare application/removal/UI output. Initial setup had omitted the Creature tag required by the material effect gate; a later run used the stale assembly while unsupported `Assert.Multiple` prevented compilation. Both receipts are retained as setup/stale evidence, not the final behavioral RED. After correcting the fixture and checking the actual imported assembly date and zero compiler errors, native25 ran23PASS/2 intended player-grammar RED (job `78876ce1937a4a4d80c2043a3c4ca829`). Production then changed. Native160/160 GREEN, zero skips/failures (job `21b7e8373f714ef288016bf3662820cd`,0.3194129seconds) covers everyday grammar, qualified combat feedback, status effects, material simulation, effect save roundtrips and trap bleeding.

Q1–Q4 follow-up: the two new mappings share the existing immediate literal-subject rule; actual application/removal and named subjects are paired. Existing quotation, multiline and raw-announcement controls remain GREEN. No effect code, arithmetic, timing or event handling changed, and the documentation matches the two-line dictionary addition plus four new cases. No extra Play replay or fresh visual evidence is claimed for this textual follow-up.

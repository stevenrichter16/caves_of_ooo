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
- 🧪 separate newly observed follow-up: the replay also contains `You stops bleeding.` from a status-effect site. This is outside these four melee messages and is queued as a separate narrowly tested follow-up.
- ⚪ honesty: viewed frame03 shows corrected qualified miss/hit feedback and preserved NPC hits; its rightmost log text is clipped at the capture edge. It is a foreign Beating biome capture, not evidence of Spread art. Assertions and raw logs prove exact text; the frame proves its visible presentation only within that crop. Broader English copyediting, every biome and combat balance are not established here.

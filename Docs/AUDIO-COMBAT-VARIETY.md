# Combat audio variety

Status: coordinator observed native compiler RED for the absent receipt/helper/cue API before production; assertions did not run in that RED. Production is implemented; final native regression is GREEN. Coordinator also executed the three HP-callback regressions RED before the narrow final-decrement capture fix. This is CoO-original physical feedback, not a Qud sound reproduction. No soundtrack or spell sample is replaced.

## Scope and verified corrections

Give actual weapon attempts distinct blade, blunt and natural swings, with cutting, piercing, blunt and natural impacts only after real damage. Use distinct licensed recordings and the existing bounded audio pool/cosmetic round-robin; do not consume simulation RNG or allocate sources per attack.

| Source | Verified contract / decision |
| --- | --- |
| `CombatSystem.PerformMeleeAttackCore` | `BeforeMeleeAttack` may veto before weapon gathering. The existing visual Attack callback occurs once per outer action, so it cannot identify the actual selected weapon or skipped offhand. Preserve this callback for animation; move audio to each actual `PerformSingleAttack`. |
| `PerformBodyPartAwareAttack` / `GatherMeleeWeapons` | Equipped/default hand weapons are gathered; a multi-slot item swings only from its first slot; offhands can be skipped entirely. The sound receipt must occur after that chance gate. BodyNaturalAttack and legacy actor-level melee parts are also valid. |
| `PerformSingleAttack` | Miss, no penetration, zero rolled damage, damage veto/resistance and actual positive HP delta are distinct. A swing may be audible for a miss, but the impact receipt requires actual HP loss. Copy weapon family before callbacks can mutate it. |
| `MeleeWeaponPart.Attributes` | Exact tokens supply physical family; display name and substring guesses do not. Animal/Unarmed precede physical class, then Cutting, Piercing, Bludgeoning/Cudgel. Null/innate untyped attacks are natural; unknown item attributes retain generic fallback. |
| `NaturalWeaponFactory` | Tag `Natural` means default equipment and also labels BanditBlade/CultistKnife. It does **not** prove a biological attack. A natural-tagged blade with Cutting must still sound like a blade. |
| `ArmorPart` / `GetPartAV` | Armor adds DV/AV and selected physical slot armor matters. There is no shield-block outcome event or proven blocking material. Do not turn failed penetration into a metal/shield clang or a damaging hit. |
| Existing `WorldAudioEventRouter` | Current generic visual swing plus semantic Hit would duplicate the new typed receipts. Replace those production combat emitters/subscription, retaining other semantic inventory/door hooks. |
| Spell paths | Generic `ApplyDamage` is not a melee cue. Existing spell audio owns elemental layers and impacts. New typed sounds are confined to actual regular/committed weapon strikes using `PerformSingleAttack`. Guaranteed weapon-skill helpers have no existing visual Attack hook; expanding them is outside this bounded slice. |
| Lethal outcomes | Capture selected family/contact before damage; publish the impact at the real HP decrement after mutation/veto callbacks and before death removal. Recheck current visible attacker and exact target contact then. A moved/hidden/removed target cannot borrow its old contact even when the same callback kills it. There is no death-status fallback. |

## Small API and ownership

New Core `WorldAudioCombatFamily { Generic, Cutting, Piercing, Blunt, Natural }`; `WorldAudioCombat.Classify(attacker, weapon)` is pure. `Begin(attacker, defender, weapon, zone)` returns a readonly `WorldAudioCombatAttempt` carrying selected family, exact participants/zone/contact and pre-damage visible contact. It publishes one typed swing receipt. `Impact(attempt, actualDamage)` publishes only for positive actual damage. The public `ApplyDamage` API delegates to a private core with a default attempt; only `PerformSingleAttack` supplies a typed attempt. The core dispatches impact immediately around committed HP loss, before lethal removal, while generic damage remains silent. `WorldAudioCombat.Callback` is additive, exception-contained and presentation-only; a readonly `WorldAudioCombatReceipt` carries Attempt, Cue, ActualDamage and IsImpact.

The seven appended cue IDs are `SwingBlade`, `SwingBlunt`, `SwingNatural`, `HitCutting`, `HitPiercing`, `HitBlunt`, `HitNatural`. Cutting and piercing share blade-swish recordings and differ at impact. Existing generic Swing/Hit remain for unknown weapons; no enum Count sentinel is added. The coordinator owns enum-sized player/library storage and the second `places-catalog` merge. Asset work owns fourteen paired recordings and their license/import evidence. All variants must bind, rather than repitching one generic sound and calling that recording variety.

Owned production: new Core helper, narrow CombatSystem receipts, WorldAudioHooks enum append, WorldAudioEventRouter typed subscription. Associated tests and this document are owned here; no Unity or build actions or commits by this workstream. Coordinator owns native tests and catalog/player/library/context changes.

## Tests before implementation

Core fixture covers exact-token families and natural-tag counterexamples; real melee dispatch rather than invented receipts; misses, penetration refusal, zero roll, damage veto and whole-attack veto; skipped/offhand attempts; generic spell-damage silence; copied family through callbacks; observer failure; unchanged gameplay RNG/results.

Routing fixture covers injected distinguishable clip identities, one swing plus one actual impact, hidden/off-zone/world-hidden rejection, actual lethal contact and no old visual-hook duplication. Dedicated adversarial coverage includes lethal and nonlethal target relocation/hide/removal, attacker hide/removal/death, global mute, disposal, and actual assignment of both distinct variants.

Initial RED was compiler RED for the absent agreed helper/cue API; assertions did not execute in that run. Native tests remain coordinator-owned. Existing generic-cue tests and the old literal invalid enum value 13 were updated after RED to the new explicit contract, preserving negative invalid values and generic damage silence.

## Performance

The strike/receipt payloads are value types. Family selection searches existing exact tokens without allocating a split array. The router checks only the two current owners and contact cells; there is no full-zone scan, retained entity cache, new update loop, resource load or AudioSource creation on combat. Playback reuses the coordinator’s fixed 16-source pool and existing cosmetic variant counters. Audio consumes no combat RNG; the observer-failure test compares actual HP and the complete deterministic RNG call sequence against the callback-free path. This is a bounded design claim, not a measured speedup.

## Acceptance and honesty bounds

Require focused native GREEN with counterchecks, unchanged gameplay/combat regression, all new catalog resources present, independent source review and an actual input/audio route where available. Numeric source/cue observations prove dispatch and assigned recordings, not subjective impact quality, balance, physical Steam Deck latency or long-session fatigue. No false block/armor material cues and no extra generic spell impacts are acceptable.

## Review / implementation log

- Source sweep corrected the outer Attack callback's granularity, Natural-tag meaning and absence of an actual shield-block receipt. Coordinator approved seven cues and typed per-strike API before production.
- Coordinator executed native compiler RED (missing `WorldAudioCombatReceipt`, family and new cue IDs). This is compilation evidence, not executed assertion failures. Production followed that release.
- Independent source/test review caught the unsafe potential lethal fallback. Added living/lethal mutation and attacker authority counters before implementing the private damage-core seam; current routing requires exact live contact at dispatch and never consults a death fallback.
- Legacy event fixture now rejects generic visual Attack and semantic Hit as sound authorities, then proves real melee produces sound. Invalid enum regression uses the first unassigned value dynamically, retaining negative and maximum integer counters; former value 13 is a real new cue.
- Self-review identified that the old combat-log HP snapshot precedes damage callbacks. Added three native RED targets for callbacks that heal, damage or exhaust HP before the final decrement; the audio amount must describe only that decrement. All three failed in the coordinator’s 118-case native run, retained at `Docs/Verification/PlaceAudio/native-red.xml` (20 total failures include the independent place-source/credits cases). The selected combat fixtures were 29 core cases (26 passed, the 3 intended failures) and 23 adversarial cases (all passed), 52 total. Audio now measures only the final decrement using a local capture immediately before that write; existing gameplay/log snapshot semantics are unchanged. Final native GREEN: all52 combat cases passed within1478/1478 selected tests; see `Docs/Verification/PlaceAudio/broad-green.xml`.
- No assets, source or test results are claimed as auditioned. Final native GREEN and the22-check isolated Play route are complete (see AUDIO-PLACE-DESIGN.md); subjective listening and physical Deck validation remain unclaimed.

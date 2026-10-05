# Skills and spells: reliable choices and readable commitments

Status: implemented and verified, 2026-10-04. User approved the recommendations in `SKILLS-AND-SPELLS-ENGAGEMENT-AUDIT.md`. This is CoO-original tuning, not a claim of numerical Qud parity.

## Goal and scope

Make the existing skill catalogue dependable and legible before adding more powers. Preserve fast casting and the existing elemental/terrain combinations. The player should understand what a purchase does, whom an action affects, and what resources it commits.

1. Spell reliability: move Ley Tap/Heart Flame windows to owner turns and serializable effects; apply passive and temporary direct-damage modifiers consistently. Consume one qualifying charge per cast, including fully resisted hits, with a stable bonus for every target. Utility, empty and terrain-only casts do not consume damage charges. Terrain effects still commit their action and cooldown.
2. Target reliability: respect selected cells for Frostbind, Pyroclasm, Tumble, Recruit and Dismiss; preserve burning-object Pyroclasm. Use actual party relationships for Tumble. Charging Strike pays after real movement even without a final hit.
3. Readability: full details in Skills and the ability manager; optional, read-only visible-target rite preview with mark consumption and resonance payoff. Keep ordinary casts immediate. Correct authored descriptions and tooltips.
4. Progression and control: require Dismember before purchasing Decapitate; make Dismiss a free, Recruit-dependent utility unlock. Calm breaks only on actual positive HP damage, including hazards; unrelated quest truces retain their existing behavior. Test and bound passive Bludgeon accumulation; make Lunge available several times in a longer encounter.
5. Verify with native Unity EditMode tests, counters, save checks, input-reader checks and a Play witness. Review before publishing to main.

## Verification sweep and corrections

| Assumption | Source-observed correction | Consequence |
|---|---|---|
| Three world ticks means three available casts | Normal Speed 100 actions take ten scheduler ticks | Buff durations must be owner EndTurns, not TickCount timestamps |
| All direct spells use school bonuses | Only five elemental actives use the shared damage helper | Route all direct elemental and rite damage through one policy; leave retorts and melee riders separate |
| A rite's resonance element is its damage type | Shattered Rime has Cold resonance but untyped damage | Separate modifier school from original damage attributes |
| Hobbled slows action rate | It subtracts 3 DV and never changes Speed | Correct descriptions; no global effect redefinition |
| Every pacifist goal comes from Calm | Quest/dialogue code also uses NoFightGoal | Explicit damage-sensitive flag only on Calm's goal |
| TakeDamage proves HP loss | Later listeners can zero the typed Damage object | Break Calm after the final positive HP decrement |
| All abilities need a new confirmation flow | M already permits unbound casting; AnnouncementUI already paginates | Add optional details/preview, retain direct casts |
| Friend means Ally tag | Recruits use PartyLeader relationships | Reuse party-alignment semantics |
| Passive stun is isolated | Bludgeon, weapon-class and tree-root hooks can accumulate | Bound this passive's contribution; do not globally redefine Stunned |

References inspected: SpellSkillPart, SpellDamageHelpers, ConsumingRiteSkillBase, Spellcraft_Calm, CombatSystem.ApplyDamage, NoFightGoal, BrainPart, SkillPurchaseEligibility/BuySkillAction, skill JSON, Cudgel_Bludgeon/StunnedEffect, LongBlades_Lunge, Cryomancy_ColdSnap/HobbledEffect, GrimoireTooltipData, SkillsScreenUI and AbilityManagerUI. Subsystem sweep details and tests live in `SPELL-CAST-RELIABILITY.md`, `SKILL-TARGET-RELIABILITY.md`, and `ABILITY-DETAILS-AND-PREVIEW.md`.

## Readiness and exclusions

🟢 Existing commands, abilities, goals, status serialization and paginated announcements supply the required substrate. 🟡 Timing, target selection, resource boundaries and fog-safe previews need new regression coverage. ⚪ Broad spell damage rebalancing, a new movement-speed status, XP cap changes and new spell families are deferred: corrected modifier wiring needs play experience before a second numerical rebalance. Cold Snap will be described honestly as vulnerability setup rather than advertised as a movement slow.

## Test and review log

- Baseline: main `14ef3e531`; Unity idle. Existing Unity log changes preserved.
- RED tests ran before each production tranche. Native evidence and exact counts are recorded below.
- Q1–Q4 review completed below. Play report must separate measurable behavior from feel/visual inspection.

### Control tuning acceptance criteria

- Lunge: ten-turn cooldown, unchanged range two and ordinary weapon attack. This permits a second use in an encounter lasting ten player actions; no movement, damage or targeting bonus is added.
- Bludgeon: retain 50% proc chance, apply a two-turn stun with Toughness recovery against 16, and never extend an already-stunned target. Existing active Conk/Slam guarantees and the separate weapon/tree-root procs remain intact. The seeded baseline experiment repeats 100 successful damage opportunities and measures accumulated stun, with a non-Cudgel/zero-damage counter and an existing active-stun control.
- Calm: actual positive HP loss ends only damage-sensitive peace. Zero, resisted and listener-cancelled harm must not. Positive damage from environmental sources does. Newly saved goals retain the flag; old goals default to their existing behavior until expiry.
- Dismiss remains an explicit free purchase after Recruit, visible in the existing tree. No retroactive automatic grant or learned-skill migration is required; books/starter grants remain exempt from purchase prerequisites.

### Observed RED (native Unity)

`Verification/SkillsEngagement/red.xml`: 201 cases, 33 passing controls and 168 failures before production. Targeting, UI queries, charges/timing, modifiers, progression and control all reproduced. Passive Bludgeon accumulated 172 stun turns across 100 seeded hit opportunities; 50 further opportunities extended an existing four-turn active stun to 90. These are an isolated passive stress test, not a forecast of ordinary encounter length.

One test fixture initially used the default resistance-stat maximum of 30; it is corrected to 100 so the fully-resisted Calm counter actually exercises immunity. One terrain test meta had a 33-character GUID and Unity ignored its source; corrected with a fresh 32-character GUID. A second native run, `terrain-red.xml`, confirmed four unpaid terrain-cast failures and two passing zero-path controls (6 cases). Neither fixture issue is counted as a gameplay finding.

### First implementation and review corrections

Native first implementation: **272/281 pass** (`first-implementation.xml`). All cast timing, direct-damage modifiers, terrain payment, selected-target, movement-payment, root control/progression/save and actual Lunge cadence cases pass. Remaining failures were one legacy 55-character description cap (now replaced by full-reader coverage) and eight preview tests whose fixture name, “visible target,” collided with the innocent empty-state sentence. The unique fixture name preserves the non-disclosure assertions. No production visibility relaxation is needed.

Independent review identified two legacy tests that expected passive Bludgeon to accumulate disabled turns and one progression case that expected a paid Dismiss without Recruit. They were replaced with real class/root/passive integration and free utility progression controls. The new Lunge clock test also restores the previous global turn manager.

### Broader regression pass

First broad native run: **4,728/4,734 pass** (`broad-first.xml`), covering 283 affected skill, spell, AI, combat, effects, saving, turns and reader fixture classes. One newly authored visible Morrowfast footprint test correctly failed while its removed/hidden controls passed. The preview now asks the existing authored-footprint collision owner and applies perception/ownership checks; the actual cast geometry is unchanged.

Other failures: one Cryomancy legacy description-length pin; two Heart Flame tests that treated clock jumps and modifier queries as casts; and two crop fixtures lacking the actual planted-owner tag/physics required by pre-existing farming validation. These were corrected to exercise full readable descriptions, real casts/owner EndTurns, and physically valid planted owners. Farming production code is unchanged. The follow-up native run passed 4,734/4,734, recorded below.

Native regression confirmation (root): `Verification/SkillsEngagement/broad-green.xml` passes **4,734/4,734**, zero failures or skips, 121.62 seconds. This is the affected-system suite in Unity Editor, not the standalone runner and not a whole-game balance verdict. All 283 selected fixture classes are recorded in the XML. Native Play receipts are recorded below.

### Native Play and visual review

Isolated native run `NativeReaders/ee60c4a903464393bf66dfcf1d50a0bc/report.json` completed with **10/10 reader checks and 6/6 separate spell-bench checks**, zero errors, 18.66 seconds. It used actual keyboard input for Classic start, X/M/D/P, direction, paging/return and quick-cast cancellation. The documented fixture supplies one rite/book and a visible Wet target; it is not evidence of ordinary discovery. The command bench uses detached controlled actors and a complete replacement save graph. Both branches preserve the reader's runtime context. The launcher restores the prior scene, input settings and save root.

Root viewed all three PNGs: purchase prose, owned-ability details and a two-page visible-mark preview are readable within the popup, with their navigation footer visible. The first visual pass also exposed an avoidable duplicate: the entire reference page entered the combat log. The final reader-only correction opens the modal directly while retaining unrelated queued announcements; the fresh native receipt below verifies the cleaned presentation.

Can verify: compiled native behavior, paid owner turns, modifiers/resistance/structure, selected cells, valid plant fixtures, save replacement, resource conservation, visible-only preview, menu state restoration and screenshot layout. Cannot verify: novice comprehension, unaided spell discovery, all encounter balance or long-campaign feel. This does not claim every game test was run.

### In-phase self-review (Q1–Q4)

- 🟡 Fixed: global tick expiry and query-time charge consumption; per-cast snapshots and saved owner-turn effects now share one damage boundary. New status effects do not carry negative flags.
- 🟡 Fixed: picked-target redirection and free partially completed movement/terrain actions. Counters cover invalid cell aliases, held/dead/foreign owners, companions, obstructed movement and traps.
- 🟡 Fixed: misleading descriptions and missing purchase dependency; free companion dismissal retains ownership/duplicate gates.
- 🟡 Fixed after independent review: actual Morrowfast footprint collision now participates in perceived single-target previews; unknown/hidden owners do not leak.
- 🔵 Source symmetry: direct modifier application precedes resistance and structural routing, while rite resonance spending precedes conditional school queries. Temporary charges commit only after successful casts; all direct targets share their snapshot.
- 🔵 Cross-feature consistency: all 21 direct elemental callers plus damaging rites use the same helper contract; untyped rites retain their exact damage attributes. Water mastery affects the matching water-producing powers. Retorts and melee riders remain separate.
- 🔵 Counter coverage: resistance/veto/zero damage, no-op vs committed action, owned vs foreign target, visible vs hidden footprint, unrelated quest peace, saved fragile/durable peace, passive vs active stuns, no-SP free utility, cooldown-not-ready vs ready, and preview vs cast are exercised.
- 🔵 Doc reconciliation: Cold Snap is explicitly DV vulnerability, not a slow; Still Heart wakes on damage; steam tooltips match actual cooldown/status. No blanket spell-number rebalance accompanies the new modifier wiring.
- ⚪ Compatibility: previously saved Calm goals have no fragile-peace flag and retain old behavior until expiration. No save version migration or learned-skill removal.
- 🧪 Deferred: encounter-level balance of corrected spell investments, further Cold Snap/Corrosion roles, and natural post-fire Charred sourcing. These need play evidence after the reliability change, rather than simultaneous speculative tuning.

### Final acceptance

- `reader-log-red.xml`: all six added cases reproduced log pollution or selection being displaced by an older queued announcement.
- `reader-final-green.xml`: **91/91** reader, menu state, announcement and activated-ability checks pass after the final UI-only fix. These include 43 owned reader cases; the broader 4,734 run preceded this final bounded UI change, so its result is not silently relabeled as a later whole-suite run.
- Final native receipt: [report.json](Verification/SkillsEngagement/NativeReaders/1e6e8a3575564a818d590d20481539cd/report.json), **10/10 actual keyboard reader checks + 6/6 separate command/scheduler/save checks**, zero errors, 18.65 seconds. Native context remains intact; the editor restored the original idle SampleScene.
- Root inspected the final purchase/preview PNGs; the independent reader reviewer inspected all three. The full text and page controls remain readable, and the combat sidebar retains its ordinary messages. Reader opening preserves queued world notices and log history. This closes the last visual finding without changing quick casting.
- All 17 new C# files have valid distinct 32-hex metadata GUIDs. Code/content/test diffs pass whitespace checks. Parsed content comparison records only the intended 15 skill rows in `content-diff.json`; `Objects.json` is unchanged.
- Owned code/content/test inventory: [files-changed.txt](Verification/SkillsEngagement/files-changed.txt). Existing Unity logs and unrelated untracked work are excluded from the commit.

In game: **X → select a skill → D** for its complete description and purchase requirements. **M → select an ability → D** for current details. With an owned rite selected, **P** reads its visible marks (choose a direction for lines/cones). Enter/number shortcuts retain immediate ordinary activation.

# F11 mechanics follow-up: generated encounters, shaken flight and diagnostics

## Outcome

Private delta is ready for root adoption on top of immutable `mechanics-stage2`: three production overrides (`SpreadGrazerPart`, `WanderRandomlyGoal`, `SpreadPredatorPart`) and one new28-case test fixture/meta. No shared source, Unity editor or Git mutations were performed here. No hunter/grazer stats, source economy, corpse odds, loot, terrain, scheduler or factions changed in this delta.

A current, visibly nearby exact paired hunter now lets a grazer use its existing random pacing step for flight. Its duration parent and WitnessedEffect remain in place, take their normal tick, and retain their original removal lifecycle. The helper only admits a whole stack made of exact Bored/WanderDuration/WanderRandomly goals, a current reciprocal pair, current owner/zone, visible hunter and distance≤3. Calm, conversation, assigned work, follow/party and combat continue to own their actions. No global Witnessed policy or Brain dispatch change. Hidden old threat memory alone cannot take ownership of pacing.

Production ai diagnostics now report admission rejection/commit, first sight and actual sight-loss/reacquisition transitions, fresh own-kill corpse admission, and terminal hunt/feeding outcomes. Actor/target IDs use standard top-level fields; reasons and phase/budgets live in the payload. Disabled channel avoids payload construction. No per-turn wait/move logging. Fed outcome is emitted after actual removal; the claim still spends before callbacks as before.

## Verification sweep correction and original generated observation

The initial hypothesis called the generated source’s WanderDurationGoal ordinary idle. Reading its only production constructor and the retained actual trace corrected that premise: seed64 Overworld.5.8.0 grazer received **WitnessedEffect at tick100**, after seeing the hunter kill a Magpie. The same effect was removed at tick200 with `owner_died`. Its goal was effect-owned shaken pacing, not casual wandering. Root explicitly approved current exact-pair danger interrupting only the pacing step while preserving the effect/goal lifecycle.

All18 v8 IDs/variants were frozen before simulation; open coordinates were updated once before simulation when the generation owner finalized original-source locality. Candidate helper SHA is 7b909100934e8e5829f74f5c9ff26f56db4234a6d9b761e87e0bdf9425f8b197. Original immutable snapshots, source hashes, traces, allNPC turn diagnostics and summaries remain at `../../mechanics-generated-observation`. Actual candidate Objects contains the approved two original children, not a fixture hunter.

Original18 outcomes:11 leash Aborted,3 lost-sight Escaped,1 Exhausted,1 native Fed,1 native kill/no-corpse PreyGone,1 bounded40-wait continuation under an unrelated Magpie KillGoal. The latter correctly preserved normal threat priority and froze hunt budget while the ordinary goal owned the actor; it is not claimed an infinite scoped hunt. All18 actual source anchors matched the frozen census; all current NPCs (2–6/site) ran; player HP remained40 and had no combat involvement. One natural meal had exactly2 Interact gestures then exact-corpse absence; one own kill naturally rolled no corpse and manufactured none.

Predetermined first2 sites: seed1 Overworld.10.4.0 Aborted at12waits/130ticks with preyHP10; seed64 Overworld.14.8.0 Escaped at7waits/80ticks with preyHP10. No strikes in either. These actual generated runs supplement the earlier synthetic single-Tree escape gate, not replace it.

## RED, GREEN and counter-checks

- `live-gap-red.xml`:24 cases,13 expected failures and11 passing controls against immutable stage2. Three real goal-stack cases (random/duration/shaken) moved toward a nearby hunter instead of flying; scheduled action had no flight; requested production diagnostic records were absent.
- `live-gap-green.xml`:24/24 after the minimal fix.
- `neighbors-green.xml`:336/336 including new28 cases, stage1/2 roles, current original content, ordinary Corpse/retaliation/loot, Witnessed, Phase6 goals, M2 goal controls, GoalStack, FollowLeader, spread actor/adversarial and Brain save contracts.
- `final-28.xml`:28/28 after adapting test JSON parsing and Random alias for actual Unity assembly references. Runtime and full EditMode reference compilation both exit0. Initial test compile used Newtonsoft not referenced by EditMode and ambiguous Unity Random; corrected test only to JsonUtility/System.Random. No production change or test execution was attributed to that failed compile.
- New adversarial coverage includes exact replacement save for paired/unpaired shaken actor; original effect removing its same pacing parent after a flight step; hidden remembered threat leaving pacing authoritative; one scheduled action/one move/zero extra energy; effect duration and parent tick unchanged from ordinary cadence.

## Repeated fixed18 after the fix

All18 same IDs/variants were observed once more in the separate `../../mechanics-generated-observation-after-pacing` directory, using immutable copied delta sources.18/18 still had exact committed source anchors. Outcomes:12 leash Aborted,3 lost-sight Escaped,2 native Fed,1 Exhausted. Maximum24player waits/250ticks, all current NPCs active, no player combat. Two actual meals each had2 gestures and exact corpse removal.

The repeat used ordinary **unseeded live NPC/combat/corpse RNG**, just like the first observation. Therefore differences in outcomes are not causal balance evidence. The repeated18 did not naturally reproduce the shaken branch; its correction is established by the precise paired real-stack28 tests. No reruns or outcome selection were used to seek a desired result. The original no-corpse and externally interrupted traces remain retained.

## Can and cannot verify

Can verify actual candidate factory/world-generation composition, all-current-creature core turn scheduling, exact pair motion and LOS, finite role budgets, natural native attacks/deaths/corpse roll/feeding, effect-owned pacing priority, and source ownership facts observed through read-only native hooks/diagnostics.

Cannot verify Unity seed maps (runner patches string hashing), native input, camera/FOV/render output, full GameBootstrap World TickEnd services, or statistical balance from this small unseeded corpus. Each isolated observation made one disclosed placement of a standard factory Player at a legal dry cell≥25 from initial hunter, with no extra grants/stats; this is not ordinary player travel. World TickEnd services were absent; all actual NPCs were scheduled and none suppressed. The fixture uses production factory content and live RNG defaults, resets test-only loadout/trader/death overrides to null, and wires fresh unseeded brain RNG as ordinary zone entry does.

## Cold-eye review

- 🟡 Fixed: effect-owned random pacing could move healthy paired prey into a returning visible predator. Scoped per-step choice preserves status and parent rather than discarding their authority/lifetime.
- 🟡 Fixed: new scoped role had no production ai-category admission/outcome records. Added bounded transition diagnostics and disabled-channel counters.
- 🔵 Retained by design: ordinary unrelated hostility may temporarily divert hunter and pause its scoped budget; the original bounded corpus records this honestly.
- 🧪 Still owed: native editor integration, real input and five-state model/feeding presentation. Root owns that gate. No gameplay defect is inferred from different unseeded run outcomes.

Use `production-delta-manifest.json` and `test-delta-manifest.json` on top of the earlier stage2 manifests. Stage2 sources/evidence have not been modified. New test requires actual Furrowstalker content; `COO_FURROWSTALKER_BLUEPRINTS` only substitutes the exact candidate Objects while it remains private.

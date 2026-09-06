# GA03h / A11 — mortal death lifecycle verification

Status: COMPLETE, 2026-09-06. Baseline b8dd575f,9417GREEN →9484GREEN
(+67:16initial and51dedicated adversarial). Production source and tests were
implemented before the user's play-session pause. The earlier full9484PASS was
confirmed read-only during that pause. Coding and sprite work were explicitly
reauthorized before completing the performance and final compatibility gates.

## Player-visible result

Fatal limb loss credits the actual attacker, commits death before messages,
rewards and observers, and opens the existing death screen even when preserved
stat modifiers leave computed HP positive. Self-kill level-up rewards cannot
heal that committed death. A living actor still heals on level-up and retains
all ordinary rewards. Saved recovery replaces the dead graph with the saved
living graph; this is an RPG save/load flow, not permanent death.

Ordinary combat refuses corpse cutting, including when its permission callback
independently kills the victim. Axe dismemberment no longer adds a new bleed
after its mortal cut completes death. Nonmortal surviving cuts retain35/1d2
bleeding. Existing same-melee gates stop dead attackers/defenders even with
positive HP modifiers, while living second-weapon hits still work. The four
intentional killing-blow rider dispatchers remain available to living attackers.

## Implementation and scope

Body.Dismember and CheckCombatDismemberment append an optional source argument.
HandleDeath keeps its first-commit tag and normalizes only an existing HP base
with Math.Min(base,0). It preserves modifiers, Min/Max, negative bases and
missing stats. IsDeathHandled is null-safe and means commitment, including
while callbacks run; it does not mean every observer finished successfully.

InputHandler queries that marker at its existing pre-input modal gate. No new
normal-frame listener, collection or renderer path was added. LevelingSystem
guards only HP refill. Axe uses existing numeric and marker checks before its
RNG and after its cut. Combat's three existing participant gates retain their
positions and original numeric checks. The permission callback is revalidated
before the fired diagnostic and Body cut. Existing Death/ApplyEffect/dismember
records remain the observable receipts; no fabricated damage event is emitted.

CoO lifecycle consistency, not exact Qud parity: local Qud dismemberment threads
Attacker, but Qud's last-mortal rule, bleed-before-Die ordering, and UI differ.
CoO's selected-Mortal rule and GA03f detach-before-cleanup remain deliberate.
Zone-null Body calls remain anatomy-only. Direct low-level Body operations on
dead anatomy retain compatibility. Self-credit XP policy is unchanged.

## Runtime gates

All XML rows below ran with zero compiler errors. The separate authoring compile
failure is retained as GA03h-adversarial-compile.log.gz: Random namespace
ambiguity, two source locations/six repeated log lines, fixed with an alias.
That compile failure is not classified as a gameplay RED.

| Gate | Total | Pass | Fail | Start UTC | Seconds |
|---|---:|---:|---:|---|---:|
| red | 16 | 6 | 10 | 2026-09-06 11:36:30Z | 0.410217 |
| corrected-red | 16 | 7 | 9 | 2026-09-06 11:37:22Z | 0.4072162 |
| minimum | 102 | 102 | 0 | 2026-09-06 11:38:54Z | 1.3927816 |
| adversarial | 56 | 53 | 3 | 2026-09-06 11:43:41Z | 0.8215731 |
| corrected-adversarial-red | 57 | 56 | 1 | 2026-09-06 11:45:12Z | 0.6829652 |
| focused | 237 | 237 | 0 | 2026-09-06 11:46:12Z | 1.849238 |
| hypotheses | 59 | 59 | 0 | 2026-09-06 11:48:30Z | 0.9622337 |
| participant-red | 6 | 3 | 3 | 2026-09-06 11:52:05Z | 0.3462004 |
| corrected-participant-red | 6 | 4 | 2 | 2026-09-06 11:53:50Z | 0.3114984 |
| unchanged-focused-red | 244 | 242 | 2 | 2026-09-06 11:55:37Z | 1.3872848 |
| final-focused | 246 | 246 | 0 | 2026-09-06 11:56:45Z | 1.4341572 |
| full | 9484 | 9484 | 0 | 2026-09-06 11:59:08Z | 147.6313981 |
| final-full | 9484 | 9484 | 0 | 2026-09-06 14:02:42Z | 150.8348561 |

Corrections retained honestly: Player has SP but no MP; explicit MP fixtures
were separated from missing-MP controls. Villager is not passive, so witness
preconditions now use actual SummitSinger. Positive-Bonus melee controls use
Max200 to avoid clamping away actual damage. The unchanged-focused RED followed
an asserted source replacement that wrote nothing; it is not a failed applied
fix. The real remaining callback-death and positive-HP participant failures
were reproduced before their fixes. PaleSalt's real modifier is isolated from
other tests' partial registries, rather than changing production initialization.

The51dedicated cases cover HP bounds/missing stats, zero-ActualDamage and weapon
gates, callback death/veto, recursive and independent death credit, actual corpse
metadata, reputation, passive witnesses, equipment drop policies, real self-source
poison gas, UI polling, actual PaleSalt enhancement ordering, and eight complete
primary/offhand participant controls. Assertions occur outside caught callbacks.

## Native functional evidence

Final run `3839d8579a154b2f9a647e4237eff905`:24PASS/0FAIL/0unexpected; 5.0937708seconds,
shutdown5.113407seconds. Owned root held through teardown, saving
unregistered and root removed. JSON and raw log retained.

Actual Player/Villager/Humanoid/Battleaxe/IronshodBoots/CreatureCorpse and bootstrap
registries are exercised. Controlled stats, real skill ownership, GlowQuartz,
inert AI, finite legal combat RNG and veto observers are explicit fixtures.
Temporary observers are absent from checkpoints. Native N/F5/F6/L verifies
fresh-save binding, load replacement, exact gear aliases, death modal input,
positive HP modifiers, XP/reputation and corpse attribution. Combat stimuli
call the full CombatSystem API; keyboard combat targeting is not claimed.

Earlier native24 reports are retained both before offhand review and before the
performance-only harness additions. The first native did NOT demonstrate the
offhand defect; that reviewer inference was retracted. Runtime RED proves it.

## Performance regression profile

The identical healthy arena records25seconds each of idle, individual arrow
steps and held arrows. A northwest8-step loop avoids the two inert NPCs.
Warmup and route alignment are excluded. Required profiler markers and every
accepted move, held repeat, clock/coordinate/HP/Speed boundary are checked.
The200000-frame buffers did not overflow. These branches contribute zero
functional Check groups; the original24-case branch passed afterward.

- before: run`48a766c304cd4237aa5f8545909dd811`, 75.421007s, 68091frames; idle 0/0moves, 0held repeats, discrete 135/135moves, 0held repeats, held 178/178moves, 89held repeats.
- after: run`f8399afeef8d4923aaa5eed48f27eb7f`, 75.374361s, 68580frames; idle 0/0moves, 0held repeats, discrete 137/137moves, 0held repeats, held 178/178moves, 89held repeats.

Input.Update timings in milliseconds, ordered by maximum within each capture:

| Variant | Phase | Mean | p99 | Maximum |
|---|---|---:|---:|---:|
| before | discrete | 0.013130 | 0.010500 | 2.300209 |
| before | held | 0.014627 | 0.013458 | 1.788875 |
| before | idle | 0.004836 | 0.011000 | 0.045042 |
| after | discrete | 0.013859 | 0.014625 | 3.286583 |
| after | held | 0.015664 | 0.012667 | 1.937625 |
| after | idle | 0.004233 | 0.009667 | 0.049958 |

All raw frame/step CSVs are losslessly compressed; JSON retains every marker's
max/percentiles/mean, whole-editor GC and wall-frame samples. Source provenance
proves the before run changed only the death-screen predicate, then restored
the exact after bytes in finally. Profiling followed source preparation, so
this is an explicitly retrospective baseline, not a pre-implementation capture.

Can verify: these two finite workloads accepted every requested move, maintained
healthy runtime state, captured valid markers and completed isolated teardown.
Cannot verify: isolated tag-query cost, statistical equivalence, speedup, zero
allocation, actual-build FPS, combat/death timing, physical keyboard delivery,
pixels or subjective feel. Script, logging, observer and renderer work are
included; recorder observations can be one frame shifted at phase boundaries.
The held-step CSV latency is measured from the preceding observed step for
repeats, and from the key request for the first step; it is not a second
independent keypress latency. Timed runs accepted135 versus137 discrete steps,
so work counts are reported explicitly rather than assumed identical.
Whole-editor wall-frame pauses (about641.5ms before and607.6ms after) remain
unattributed. After discrete Input.Update maximum is3.286583ms versus2.300209ms
before; these single captures support neither speedup nor equivalence claims.

## Review and remaining work

Independent taxonomy/Qud source review and follow-up hypotheses are retained.
Resolved notable findings: permission callback mortality, three numeric-only
participant gates, and test registry ownership. Current final source is clear
for this bounded wave; separate profile review accompanies the raw captures.

Deferred with explicit bounds: A12 scheduling after BeginTakeAction death;
delayed BleedingEffect source loss; arbitrary throwing callbacks/resurrection
mutations; null-target HandleDeath; unrelated cached-survivor skill paths.
No generic death/event framework rewrite or system-wide bug-free claim.

## Attribution and files

Six production paths: Body, CombatSystem, Axe_Dismember, LevelingSystem,
InputHandler and DeathScreenController. Two test classes and three native
scenario/driver/launcher classes accompany their metadata. CombatSystem and
InputHandler receive only the owned incremental patches in the index; their
preexisting spell/input changes remain unstaged. Other protected files remain
outside this commit. No content JSON or sprite asset changes in GA03h.
The next wave authors the user's creature/NPC equipment and improves item art
where the visual review identifies wrong or missing identities.

# Coded-system completeness follow-up

Status: Spread acceptance is complete and published; the live-system audit is active. Weakened stacking is published/native GREEN; pet retrieval and hot steam are separate active plans. Remaining initial observations below are classified by their recorded evidence. Follow `DENSITY-EXECUTION-PROMPT.md` and the test-first workflow before production changes.

## Audit method

Trace definition → factory/registration → ordinary source → player action/AI turn → result and feedback → saved/revisited state. Confirm actual caller and target ownership. A TODO or absent blueprint reference alone is insufficient evidence. Preserve intentionally deferred tinkering, hidden lore and established species restrictions.

## Initial source candidates (27 September)

| Candidate | Observation | Required disproof / failing witness |
|---|---|---|
| Weakened reapplication | `WeakenedEffect.OnStack` replaces `StrPenalty` when a stronger effect arrives, while the applied Strength penalty is not adjusted there; duration is not refreshed. `Rites_SunderingWord` and the on-hit factory actually create this effect. Existing challenge tests cover same-strength reapplication. | Trace StatusEffectsPart stacking, target linkage and save reconstruction. Exercise stronger/weaker repeated applications through normal action and assert exact Strength restoration and duration; verify independent penalties survive removal. |
| Lifesteal feedback/robustness/source | `LifestealPart` has working event code despite stale scaffold comments. Partial overheal reports0 rather than actual recoveredHP; direct HP dereference and arithmetic merit scrutiny. No live blueprint source established yet. | Trace DamageDealt amount semantics and actual authored traits. Do not arbitrarily stock a new artifact. Pair actual damage/heal and capped/zero/missing/dead/overflow controls; distinguish dormant tutorial exercise from a normal-play system. |
| Independent village behavior for followers | AIGuard and AIWellVisitor still contain pre-party TODOs. The party system now exists; BoredGoal dispatches custom behavior. | Read party goal scheduling first: an upstream follower guard may already make these TODOs stale. Demonstrate an actual recruited follower abandoning its follow behavior before changing anything. |

## Execution prompt

Finish and verify C15 first. Then prioritize remaining content ledger gates and these candidates by player impact. For each verified gap, record the corrected premise, write and run the failing normal-route assertion with a matched control, implement the smallest complete fix, inspect adversarial ownership/save paths, run relevant native acceptance, update the living doc and commit with the project template. Reject disproved premises in this table rather than manufacture changes. Continue auditing registration/source/action/persistence links when the original list is exhausted.

## Evidence bounds

No production changes or new success claim follows from this initial read. No assertion is made that all TODOs are defects or that unrelated tutorial code should become ordinary loot. Candidate status must be replaced by concrete receipts or documented rejection.

## Bounded audit: Weakened reapplication (private, 27 September)

Scope: the existing Strength debuff and its actual Sundering Word/readable-grimoire route. This is a CoO-original consistency repair, not new content or a Qud-parity claim. C15 editor work remains root-owned; all test/candidate code stays in `/tmp/coo-weakened-audit` until separately approved.

| Earlier premise | Verified current source / correction |
|---|---|
| Weakened is an unfinished tutorial scaffold | The comments are stale: OnApply/OnRemove and factory mapping are live. Challenge tests already pass their equal-strength case. No new weapon or source is needed. |
| Stronger reapplication updates the applied penalty | OnStack changes StrPenalty only; StatusEffectsPart absorbs the incoming instance before OnApply. The stored stat and removal amount diverge. Requires actual RED before production. |
| Reapplication refreshes remaining duration | Challenge3 explicitly requires the longer duration; current OnStack never changes Duration. Equal-strength challenge test omits the duration assertion. |
| Loading reapplies effects and repairs bookkeeping | SaveEffect stores public magnitude/duration and actor stat modifiers; RestoreEffectsForLoad rebinds Owner without OnApply. Existing saves preserve their serialized state. No broad save rewrite is planned. |
| Sundering Word is merely a tutorial reference | Actual Rites skill data, SunderingWordGrimoire factory blueprint, ReadGrimoire action and five loot rows exist. The spell applies default Weakened only to surviving marked targets after ink spend. Stronger authored weapon usage is not established; do not claim it. |
| Every related stat effect should stack the same way | Berserk refreshes max duration, Hobbled accumulates duration, Parched increments actual penalties per capped stack, ShatterArmor is queried dynamically. Preserve these different contracts. |
| Any repeated Rites effect proves an ordinary solo recast | Its45-turn cooldown exceeds the3-turn debuff. An action-level witness must use separate legitimate ready casters or a pre-existing debuff; do not reset cooldown to manufacture reachability. |

Invariant plan: one existing owned Weakened instance contributes the strongest applied penalty exactly once; an upgrade applies only the difference, weaker/equal arrivals do not double apply or downgrade. Duration retains the longer remaining value (and -1 remains indefinite per base Effect contract). Removal/expiry after any stack or save round-trip restores only that debuff contribution, preserving independent penalties/bonuses. Refused applications do not mutate active state. Existing skill/ink/target/payoff gates remain unchanged.

Test order: (1) focused source RED on stronger upgrade/removal, duration and actual book-learned marked/cold rite with matched controls; (2) minimal effect-only candidate; (3) save/expiry/independent-stat/veto/owner and operation-sequence adversarial probes; (4) relevant existing effects, rite and save differential; (5) peer review and root-owned native acceptance. No new serialization fields, blueprint edits, balance numbers or generic status-system rewrite are intended. Legacy already-corrupted active snapshots cannot be reconstructed exactly without historical applied magnitude and remain explicitly bounded.


### Source disproof before extending the queue

Root read `RecruitedEffect.OnApply`/`OnRemove`, `BrainPart.HandleTakeTurn`,
`BoredGoal.TakeAction` and `FollowLeaderGoal.Finished`/`TakeAction` together.
Normal recruitment pushes a persistent follow goal above existing duties. The
brain only executes the top goal; being close to the leader or temporarily in
another zone does not finish follow, and the follow goal resets its age there.
Consequently the guard/well TODO comments alone do **not** establish a recruited
follower spontaneously choosing independent duties. Existing follow/recruit
fixtures pin adjacent pursuit, cross-zone persistence and save restoration. No
production AI change is justified by that original premise. Timeout/death and
malformed direct leader assignments are separate hypotheses, not silently
classified as normal recruitment defects.

`LifestealPart` is implemented despite its scaffold comment, but searches of live
Resources and runtime construction found no authored ordinary-play source in the
current project; the confirmed direct construction is its programming-challenge
fixture. The capped-heal feedback expression remains a real local arithmetic
candidate, but it does not justify inventing a new normal-play artifact to make
the class reachable. Prioritize the genuinely live Rites/Weakened route and
remaining content acceptance gates first.

### Private implementation and verification checkpoint

Confirmed two defects: stronger reapplication changed stored magnitude without applying the incremental stat penalty, and longer incoming duration was ignored. The focused23-case run produced12 actual RED and11 controls PASS, including the actual blueprint book→inventory Read→command route (marked duration1 incorrectly remained1; cold counterpart passed; ink10→9 and cooldown45 observed without resets). The minimal candidate changes only `WeakenedEffect`: apply the positive magnitude difference to the existing Owner's Strength, then retain the stronger stored magnitude and longer remaining duration; preserve -1 indefinite. Stale scaffold/TODO comments are replaced by the live contract. No save format, actor/loot blueprint, skill cost, source or balance magnitude changes.

After focused23GREEN,14 adversarial cases probe unrelated Parched/Berserk, independently added penalties, forced application, removed-instance identity, indefinite save/ticks and540 bounded generated stack/tick/remove/save operations. The282-case nearby differential is257PASS/25RED before →282PASS after, newly failing0. All25 transitions are new weakening assertions; all245 pre-existing neighboring cases remain GREEN. Native-reference compilation of both fixtures is0errors; actual Unity EditMode and any keyboard acceptance are pending. Exact private preimages and5file manifest live in `Verification/DensityCompletion/Completeness/Weakened/`.

Cold-eye review: Q1 apply-upgrade-remove accounting and save rebind are symmetric; Q2 neighboring fixed/dynamic effects retain distinct stacking policies; Q3 stronger/equal/weaker, veto/pass, marked/cold, finite/indefinite, loaded/original and unrelated-modifier controls are present; Q4 Challenge3 duration wording now matches the candidate. Peer source review found no blocker in this narrow stable-stat contract. Presentation queues/announcements and the resonance registry are restored by the new fixtures, so the tested cast does not leave an audit request for later editor play.

Bounds: this does not infer historical applied magnitude in already-corrupted old saves, or fix arbitrary removal/replacement/late insertion of the Strength stat while an effect is active. Existing apply/remove MessageLog prose still reports BaseValue and can misdescribe effective Strength under unrelated modifiers; that pre-existing feedback defect is recorded separately from this minimal stacking repair, and message accuracy is not claimed here. No new authored weapon source was invented for the on-hit factory probe.

In-phase severity review: 🟡 stronger-stack stat drift and 🟡 ignored duration are fixed in the private candidate and paired by RED/GREEN receipts; 🔵 stale production scaffold comments are corrected. 🧪 native EditMode/keyboard verification remains pending and is not replaced by the standalone282 result. ⚪ pre-existing late-stat-identity and numeric-message limitations are explicitly outside the bounded two-defect patch. The final patch is held for root review/publication; shared production remains unchanged.


### Actual Unity verification and publication

Root published the two unchanged test fixtures first. Native run
`d9e6e2efce994bd9b3091ca48055606b` executes59 cases:32 PASS and27 RED,
comprising the25 intended weakening failures and two missing-launcher controls
for the separately queued traveller audit. All20 pre-existing scene-restoration
controls and12 weakening controls passed. Root then published the exact reviewed
single-source WeakenedEffect repair (SHA12b0e21336b81ba86dbee247f4af0599970c5cb66839280fc65d31f7ef5f9c39).
Native run `dbbea0a039c2459bb672cd2a9d537077` passes59/59 with no skips,
including all37 weakening cases. The existing37-case count is unchanged from the
private candidate; these are actual Unity execution receipts, not compiler-only
claims. Publication manifests retain the exact preimage and candidate hashes.

Can verify: actual effect apply/stack/tick/remove, independent modifier accounting,
serialized graph rebind, real blueprint book/read/cast core flow, marked/cold
controls and540 bounded mixed operations in Unity EditMode. Cannot verify: native
keyboard acquisition of that book, long-run combat balance, historical corrupted
snapshot reconstruction, or the separately documented pre-existing effective-stat
message wording. The full integrated follow-up regression and main push remain
pending; no content-source or player-ability grant was added for this repair.

## Effective Strength feedback follow-up plan

The already-recorded numeric feedback gap is being closed separately after the stacking repair. Source verification: Weakened uses the actual `Stat.Penalty` and the UI/mechanics read effective `Stat.Value`, but application prints `BaseValue` and `BaseValue-StrPenalty`, while removal prints BaseValue. Bonuses, other penalties and minimum/maximum clamping therefore make the current message inaccurate. Current test/source searches found no consumer depending on the old prose. The existing narrow player normalizer already handles immediate `is` and `recovers` and leaves named subjects alone.

Plan before production: record effective value before changing the existing penalty; report its actual resulting effective value. On removal, report effective value after the unchanged subtraction. Use immediate subject verbs for both player/named actors, and describe indefinite duration without negative turns. Add paired actual apply/remove cases for plain, independently modified, minimum-clamped and maximum-clamped Strength; each asserts true stat accounting and UI-observed text. Keep stacking arithmetic, source reachability, effect lifecycle, RNG and save fields unchanged. Run the existing weakening/adversarial/challenge plus everyday-grammar native suite. No new Play acquisition claim is needed for this numeric log correction.

Executed: ten new requested-output assertions ran RED while all37 existing weakening controls passed (`feedback-native47-red.xml.gz`, job `a8c52ba0049241aba828c89219f66577`). The plain-stat pair checks consistent new prose; modified and clamped pairs expose inaccurate old numbers, and the indefinite pair exposes negative-turn wording. These are ten cases, not ten distinct bugs. Application now captures `str.Value` before adding the unchanged penalty and reports the resulting `str.Value`; removal reports that effective value after the unchanged subtraction. The duration label says `until removed` for indefinite effects, refining the pre-fix test wording `until it wears off` to avoid promising automatic expiration. No arithmetic or save field changed.

Native139/139 PASS, zero skipped/failed (job `505750dde9824fa38ecfb10a91bb68d4`,0.9092407seconds) covers47 weakening cases, challenge controls, everyday grammar and status lifecycle. Q1–Q4: application/removal read the same effective stat as gameplay, preserve unrelated modifiers and min/max rules, pair player/named subjects and actual UI callback output, and retain already-tested quote protection. Sources, stacking and saves are unchanged. The earlier numeric-message limitation is now resolved for these apply/remove messages; arbitrary stat replacement remains outside the stacking contract. No extra keyboard acquisition, new visual result or balance claim is made.


## Importance review and stopping rule — user direction, 27 September

When an item stalls, distinguish a normal-play defect from a test-route inconvenience, assess its player impact, and continue only if the value warrants the effort. Record lower-priority limitations rather than treating every imperfect scenario as a release blocker.

| Item | Importance / disposition | Current evidence |
|---|---|---|
| Hidden hot-source inspection | High enough to finish: overlapping terrain can prevent keyboard users from inspecting the object that carries the scald warning | Live steam route reaches real706°C steam, then the menu cannot reach the OilSeep beneath WaterPuddle/SteamCloud. Actual UI RED/repair is next; preserve existing loot-pile behavior |
| Ordinary resource journey | High: finding supplies, purchasing, recovering and saving are core progression | A continuous native route is in preparation; no teleports or resource grants, and bounded cost/source failures remain honest |
| Dog fetching | Low priority for further live-test iteration; defer delivery/save/repeat demonstration | Core changes pass751 native cases including69 new checks. Actual first throw is admitted; ordinary Witnessed fear interrupts the dog for20 turns, then fetch resumes. The24-wait audit ends before delivery; no item/goal loss was established. Keep the failed receipt, do not claim end-to-end native completion, and stop extending this harness now |
| Further generic fire-unit changes | Medium and balance-sensitive | A fresh-content plan identifies mixed units, but no extra thermal bridge or save migration is justified. Complete the concrete hot-source interface issue first; broader fire changes need their own bounded native counter-scene |

This prioritization supersedes treating the unfinished pet keyboard route as a prerequisite for higher-impact work. BitLocker, lore restrictions and old-save boundaries are unchanged.

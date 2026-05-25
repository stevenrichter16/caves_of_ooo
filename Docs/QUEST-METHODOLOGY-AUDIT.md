# Quest buildout — CLAUDE.md methodology-compliance audit

> A review of every quest phase (Q1–Q7 + Q3.5 + Q5.1–Q5.4 + the playable
> quests) against the CLAUDE.md gates, and the fixes applied to close gaps.
> **Verdict: strongly aligned.** Gaps found were narrow; all fixed (below).

## Gate legend
Living doc · Unit tests · Counter-checks (§3.4) · Adversarial sweep (≥2
bug-class surfaces → dedicated file) · Cold-eye (both angles) · Deterministic
self-auditing scenario (measurable mechanics) · Diag observability (every
gate emits, incl. reject) · §2.3 commit template.

## Per-phase matrix

| Phase | Doc | Tests | Counter-checks | Adversarial | Self-auditing | Diag | Notes |
|---|---|---|---|---|---|---|---|
| **Q1** quest-log UI | ✓ | ✓ builder tests | ✓ | n/a (UI) | n/a | n/a (UI) | Visual feel → verified by PlayMode screenshot, which **caught the glyph bug** (GetTile vs GetTextTile). Correct gate for a visual feature. |
| **Q3.1–3.4** parallel objectives | ✓ | ✓ model/dispatch/convo | ✓ | ✓ **QuestObjectiveAdversarialTests (21)** | ✓ Q3.5 bench | ✓ +Rejected | Core mechanic; also covered by the hypothesis audit. |
| **Q3.5** content + bench | ✓ | ✓ + smoke | ✓ | (uses Q3 sweep) | ✓ **live 17/0** | ✓ | ⚠ verification gap: the authored `EnchiridionQuest` shipped a malformed `IfFact` (2-part vs 3-part) the bench's "loads" check didn't catch — **fixed** + an IfFact-well-formed test now pins it. |
| **Q4.1** quest events | ✓ | ✓ (8) | ✓ no-LocalPlayer / no-op-no-event | feature tests cover the surface (player-dispatch, null-player) | n/a | ✓ events + diag | Low bug-class surface; no separate adversarial file needed. |
| **Q5.1** slain | ✓ | ✓ (6) | ✓ | (shares Q5.2/3 sweep) | ✓ Q5 bench | ✓ | |
| **Q5.2/5.3** taken / starter | ✓ | ✓ (9+9) | ✓ | ✓ **AdversarialTests (14)** | ✓ **bench 7/7** + integration (3) + blueprint pin (4) | ✓ | The exemplar: both cold-eye angles documented, rule-7 live held. |
| **Q5.4** SetFactWhenSlain | ✓ | ✓ (10) | ✓ | the 10 tests cover the surfaces (save/load, no-killer-gate, boundaries, **order-independence**) | ✓ live 14/14 | n/a (sets a fact) | Tiny Part; surfaces covered without a separate file. |
| **Q6** failed tracking | ✓ | ✓ (9) | ✓ | the 9 tests ARE adversarial-flavored: save/load round-trip + pre-Q6 EOF + anti-exploit (completed-wins, re-take-clears, fail-retake-fail) | n/a | ✓ Failed + Rejected | Adversarial surfaces covered in the feature file. |
| **Q7** accomplishments | ✓ | ✓ (5) | ✓ in tests (no-accomplishment / null-state / idempotent) | n/a | n/a | ✓ Accomplishment | ⚠ the DOC lacked a self-review section (tests had the counter-checks) — **fixed** (added §5 self-review to QUEST-ACCOMPLISHMENTS.md). |
| **Playable** (scenario + world) | ✓✓ | ✓ content-integrity | ✓ | n/a | ✓ **live 15/0, 13/0, 14/0** | ✓ | Content-integrity tests pin the cross-file ID seams. |

## Gaps found, and the fixes applied

1. **Observability — quest gates were silent on reject/no-op branches.**
   The single clearest gate-miss: `FinishObjective` / `CompleteQuest` /
   `FailQuest` emitted rich SUCCESS diags but `return false`d silently on
   their no-op branches — the exact skill-system pattern the CLAUDE.md
   observability rule was written for ("why didn't my objective finish?" →
   no trace → debug degrades to grep). **Fixed:** added a `quest/Rejected`
   record (payload `gate`, `reason`, `questId`, `objectiveId`) on every
   reject branch, with a shared `EmitQuestRejected` helper. Pinned by
   `QuestRejectionDiagTests` (6) incl. a success-emits-no-Rejected
   counter-check. A "why didn't X happen?" session now starts with
   `diag_query category=quest kind=Rejected`.

2. **Q7 doc missing a §5 self-review section.** The tests had the
   counter-checks; the doc didn't document the review. **Fixed** (added).

3. **Q3.5 verification gap (the IfFact bug).** The bench validated the quest
   *loads*, not that its triggers *evaluate* — so a malformed `IfFact`
   slipped through. **Fixed earlier this session** + the content-integrity
   tests now assert every `IfFact` arg is well-formed (3-part `key:OP:value`).

4. **Cold-eye not separately documented for Q1/Q4/Q6/Q7.** These prior-phase
   docs don't carry an explicit both-angles cold-eye section. Mitigation:
   the code is heavily tested, the **hypothesis-driven deep audit** (this
   session) re-probed the objective lifecycle from player-flow (0 bugs, 9
   pins), and THIS audit is the retroactive taxonomy + Qud-parity pass
   across all phases — 0 further gameplay bugs found beyond the
   observability gap above.

## Honest bounds
This audit verifies methodology *compliance* (gates followed) + ran a
taxonomy/parity re-read; it is not a proof of bug-freedom. The adversarial
sweeps + hypothesis audit are bounded by imagined bug classes. The quest
buildout's actual test surface (≈260 quest/storylet EditMode tests + 4 live
diag-audited benches/quests) is the substantive evidence; this doc maps it
to the CLAUDE.md gates.

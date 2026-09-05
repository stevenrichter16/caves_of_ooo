# Game audit wave1 — diagnostic correctness

Status: **SHIPPED**. Branch claude/game-lore-analysis-jqa7ur. Baseline7674;
final7715/7715 GREEN (+41:12 regression cases,29 dedicated adversarial).

## Verified changes

- Runtime count and query now honor identical exact cause predicates; samples
  come from matching records. Count deliberately ignores query return limits.
- Query/count/assert expose and parse cause_trace_id in the actual editor tool
  schemas and dispatchers.
- Query/count/causal inspection search all8192 retained ring entries; the stale
 5000-entry scan ceiling no longer hides older retained records or ancestors.
- Query response budgets count UTF8 bytes of the entire inner payload. Refusal
  leaves buffer state unchanged and supports narrowing or a larger budget.
- Documentation now separates shipped scalar/turn filters from future arrays,
  projections, wall-clock windows and pagination; corrected trace-ID bit width.

## Verification evidence

| Gate | Raw evidence | Result |
|---|---|---|
| RED before production | GA01-red.xml.gz,21:14:41–42UTC |12 total;11 expected failures;1 unfiltered control green;0 compiler errors |
| Focused repair | GA01-green.xml.gz,21:16:13–14UTC |12/12 green |
| Focused + adversarial | GA01-adversarial.xml.gz,21:18:52UTC |69/69 green:41 new and28 existing |
| Full suite | GA01-full.xml.gz,21:19:34–21:21:04UTC |7715/7715 green;0 compiler errors |
| New metadata | Global Assets GUID scan |2306 unique,0 collisions |
| Diff whitespace | Explicit owned paths, excluding metadata |clean |

The11 initial failures cover multiple assertions of the same four defects;
they are not11 separate bug claims.29 dedicated cases pin conjunctions,
actor/target separation, null-turn windows, exact empty causes, real adapter
behavior, uncapped aggregation, nested scopes, ring rotation and byte-budget
narrowing/override. These are regression pins after repair, not29 discoveries.

## Cold-eye review and corrections

Independent review inspected taxonomy (null/filter/causal/rotation/budget and
state mutation) and declared original diagnostic contracts (source/schema/docs).
No must-fix code finding remained. Review found two documentation issues:
SinceTurn XML still recommended unimplemented wall-clock filters, and the new
report link did not yet exist. Both corrected before commit. Root also removed
stale transport-registration claims and distinguished query-only budgeting.
Comment/documentation-only corrections followed the full green run.

In-phase self-review:
- 🟡 Confirmed cause/count/schema, retained-history and byte-budget defects fixed.
- 🔵 Shipped-vs-planned documentation drift corrected in current contract section.
- 🧪 Remote MCP transport and visually presented tool output were not exercised.
- ⚪ Historical prototype examples remain historical; the shipped section explicitly
  disclaims their unimplemented fields. No new Qud parity claim.

## Honesty and performance bounds

Can verify: actual loaded editor HandleCommand adapters, schema attributes,
response envelope fields and runtime query outcomes. Tests use reflection to
preserve the existing test assembly's editor/plugin separation.
Cannot verify: remote transport registration, network roundtrip or visual feel.
This wave has no gameplay feature, Update/turn hook or optimization claim; no
native gameplay scenario or75-second gameplay profile is applicable. Query
scans remain bounded by the existing8192-record ring capacity.

## Files

Production: Shared/Utilities/DiagQuery.cs; Editor/Diagnostics/DiagQueryTool.cs,
DiagCountTool.cs, DiagAssertTool.cs. Tests: GameAuditDiagnosticTests and
GameAuditDiagnosticAdversarialTests plus metadata. Documentation: whole-game
plan/source inventory, this report/raw XMLs, AI-OBSERVABILITY and daily log.
All staged paths were clean/absent at the audit baseline. Preexisting and
concurrent work remains unstaged and preserved.

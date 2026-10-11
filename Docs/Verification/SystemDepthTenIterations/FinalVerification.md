# Ten iterations — final verification

Baseline `1da5592c3`. This is a bounded current-system audit and implementation,
not an exhaustive claim about the entire repository or historical documentation.
See the [living plan](../../SYSTEM-DEPTH-TEN-ITERATIONS-2026-10-10.md) for sources,
sweep corrections, scope decisions, player outcomes and remaining priorities.

## Native Unity EditMode

Unity 6000.3.4f1, current macOS editor; clean compilation and no console errors.
Final job `941ad3a3628542c19f5fda2b0a9e49b4`: **810 passed, 0 failed, 0 skipped**,
45 requested fixtures, 82.0247723 seconds. Raw: `native-final-integration.xml`;
exact selection: `native-selection.json`. All 45 fixture names matched.

| Iteration | Dedicated native cases | Outcome |
|---|---:|---|
| 1 — spare keys and visual aliases | 29 | All pass |
| 2 — relevant merchant stock | 31 | All pass |
| 3 — ordinary compost and actual planting/payment input | 31 | All pass |
| 4 — natural liquids and FrostLichen review | 49 | All pass |
| 5 — purification teaching and book payload identity | 32 | All pass |
| 6 — player-party combat assistance | 47 | All pass |
| 7 — meaningful Frostbind provocation | 30 | All pass |
| 8 — companion routing | 10 | All pass |
| 9 — saved orders and actual world-menu input | 22 | All pass |
| 10 — complete rendered journal paging | 6 | All pass |

These are 287 distinct dedicated cases. Another 523 selected regression cases
pass, including six added movement-veto counters. Do not add overlapping
standalone run totals to this number.

First integration: `native-first-integration.xml`/`.json`, 711/723 pass. Six
native input cases shared a frozen EditMode clock and did not pass the existing
repeat gate; fixtures now model elapsed input opportunities and explicitly
assert actual popup closure, scheduler cost and payment. Six old movement pins
expected a deliberately removed BeforeMove bypass; history and newer tests
confirmed intentional rooted/permission refusal. The corrected tests separately
verify unvetoed body entry and refusal/rearm, without movement production changes.
The first failed receipt is retained, not replaced by the successful one.

## Native Play sanity and visual review

Final run `9ea47b4346314b75a9b7c65cc9997598`: **14/14 checks**, 0 failures,
0 unexpected errors, complete and error count finalized. Raw report and eight
unaltered screenshots: [Native report](Native/9ea47b4346314b75a9b7c65cc9997598/report.json).
The earlier 14/14 run `4d1ed0c4ed82479096c14632d2ad67e6` is preserved.
A wording-only correction clarifies that new-game bootstrap writes an isolated
checkpoint; the probe performs no explicit save/load round trip. The final
rerun makes no additional gameplay changes.
The launcher restored SampleScene and left Unity out of Play; console error
query after restoration returned zero. No user save or scene was overwritten.

| Visible path | Evidence |
|---|---|
| Keyboard companion stay | Actual look/context menu, free command; 02 screenshot |
| Ordinary movement while staying | Companion remains at ordered position; 03 |
| Controller resume follow | Native LT+A menu and A confirmation; 04 |
| Ordinary movement after resuming | Same recruited actor follows on scheduler; 05 |
| Journal opening and long objectives | Actual Q and first page; 06 |
| Next page and wrapped continuations | Actual PgDn; 07 |
| Final history and controller navigation | LB/RB/Y/B, independent notes and free close; 08 |
| Fixture cleanup | Registry restored, synthetic pad removed, previous pad restored |

All eight captures inspected. A default image preview appeared to omit thin text
on page two; original-resolution review and unchanged PNG pixel checks confirmed
the saved image was intact. No speculative renderer change was made. Menus,
wrapped lines, page indicators and controls are readable in the full-resolution
captures. The long quest and one factory recruit were explicitly arranged by
the test scenario; this is not organic recruitment or quest acquisition evidence.

### Can verify (script-observable)

Native gameplay and serializer paths, item costs/ownership, state guards,
controller/keyboard dispatch, world tick costs, actual short stay/follow walk,
all quest-page reachability, and isolated-fixture cleanup. Native EditMode also
executes planting through the real inventory confirmation before composting.

### Cannot verify (visual / feel)

Full-campaign balance, novice discoverability, every generated zone, natural
recruitment acquisition, physical Steam Deck ergonomics or performance. The
Play probe does not claim save/load; native serialization fixtures cover that.
Screenshots prove the captured frames, not every runtime visual state. No new
Linux release archive was built for this source/content task.

## Content and repository checks

Objects.json was edited surgically: one new SpareIronKey; only InertSludge
changed among old blueprints; 710 old definitions remain identical and none were
removed. `final-content-diff.json` records parsed-object comparison. Stock table
changes and fallback stock match the source proofs in iterations 1 and 3.
All 22 new C# files have valid Unity metadata with globally unique 32-hex GUIDs
(`final-metadata.json`). Git whitespace validation passes. Unrelated Unity logs,
package edits and existing untracked art remain outside these commits.

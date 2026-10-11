# HUD semantic and performance review

Reviewed the integrated HUD/history changes, `Docs/HUD-POLISH.md`, existing input/lifecycle callers and relevant regression fixtures. No Unity calls or repository writes were performed for this review.

## Significant finding

**Thoughts view idle redraw:** `GameplaySidebarRenderer.Render` added an unconditional exact comparison against `_retainedLogEntries`, but that retained list is updated only by the log-panel path. Opening THOUGHTS with a nonempty log, or changing the log while THOUGHTS is open, therefore prevents all later unchanged frames from using the fast path. The whole sidebar is cleared and redrawn repeatedly. This is a concrete regression, independent of appearance.

The proposed one-condition correction skips only the retained-log comparison while `ThoughtEntries` is non-null. Fingerprinting still detects visible thought changes; returning to LOG changes the mode and rebuilds its separate cache. Two existing-API test cases cover a cold thoughts start and a previously rendered log, ten idle frames, a real thought mutation and returning to current log text. Drafts are `/tmp/coo-hud-polish-review/thoughts-idle/tests.patch` and `fix.patch`. Executed RED/GREEN and final regression status remain coordinator-owned.

## Other reviewed boundaries

| Boundary | Finding and relevant coverage |
| --- | --- |
| Ability source truth | Icons use exact source classes; targeting/range and readiness use current ability fields. Snapshot equality includes the new mutable fields. Full mechanics remain in the existing reader. `HudPolishTests`, `HudPolishBehaviorTests`, `HudPolishAdversarialTests` cover range/mode mutation, unknown powers, bounds, cooldown and real name preservation. No additional defect found. |
| Hotbar idle and lifecycle | Equal-frame early return precedes text/card writes. Changed ordinary slots redraw only their own footprint; explicit clear, disabled camera, null/removed slots and aspect changes restore/rebuild the required geometry. Prior slot values are copied. `SteamDeckHudPerformanceTests`, `SteamDeckHudAdversarialTests` and `HudCardFramingTests` retain zero-work, mutation, clear/recovery and header-bounds assertions. No additional defect found. |
| Sidebar content | All current vital/status/haul/focus inputs remain; seven-row status overflow is disclosed. Recent grouping uses exact stored ticks, not player-turn labels. Exact text/tick/count/serial cache matching repairs middle-entry changes. Divider recognition is limited to the actual homogeneous 40-character fence. Raw messages are not rewritten. Relevant behavior/adversarial tests cover these positive and negative boundaries. |
| History data and saves | Bounded range copies and 16-entry windows preserve raw bodies/paragraphs, duplicates and index/character anchors. Revision and replacement generation are transient; save layout and existing document-origin queue are unchanged. `WorldLogHistoryTests` covers old/long/duplicate/equal-restore histories, append stability and bounded reads; input tests compare serialized message bytes and pending announcements. No additional defect found. |
| Input ownership | History opens from eligible gameplay/Look states, preserves return state, refuses an invisible modal, closes stale actor/zone sessions and leaves inventory F2 intact. The actual 1080p stationary-pointer defect was reproduced and fixed separately: movement reclaims hover once, while click hit testing is unchanged. `WorldLogHistoryInputTests` now includes real-pointer overlap regressions and an explicitly scoped prior-sample counter. No additional defect found. |
| Runtime cost and scope | Fixed art caches do not grow per skill/name; recent-log gathering is revision-and-limit keyed; history gathering/wrapping occurs on demand. Arbitrarily long text still has proportional wrapping cost inside its bounded entry window, as documented. No new gameplay simulation, per-frame full-history gather or inventory-action gather found. Matched before/after timing remains a separate empirical gate. |

## Evidence and limits

The coordinator's 156-fixture run passed 3,175 cases before the subsequent pointer/thoughts corrections; it is not relabeled as post-fix evidence. The pointer fixture later passed 14/14. Final expanded regression and matched profiling remain separate gates. Original-pixel findings are in `visual-review.md`; they do not derive from passing counters.

One documentation detail should be updated during final reconciliation: adding the CD mask makes the fixed primitive atlas 12 masks / 96 × 16, replacing the earlier 11 / 88 × 16 description. The faint 1080p sidebar background seams are recorded as cosmetic. No further redesign is recommended by this review.

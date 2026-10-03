# Living fieldwork verification

Design and scope: [living design](../../EXPLORATION-DEPTH-NEXT-DESIGN.md). Prompt execution: [brainstorm, design and implementation prompts](../../EXPLORATION-DEPTH-NEXT-PROMPTS.md).

## Native Unity EditMode

These are native Unity 6000.3.4f1 receipts, not results from the standalone EditModeRunner. Some MCP jobs return completed progress and status rather than the detailed result array; the original responses are retained without synthesizing missing per-test results.

| Gate | Result and evidence |
|---|---|
| Initial content, lifecycle and model RED | [73 completed cases](Tests/initial-red-job.json); failure list capped at25, so no total failure-count claim. |
| Watering RED | [39 completed cases](Tests/watering-red-job.json); missing action/payment/reconciliation failures observed before production implementation. |
| First integrated selection | [131 completed,3 failures](Tests/first-integrated-131.json); stale diagnostic identity filters and existing haul-table omission corrected. |
| Broad affected regression | [2392 completed,5 failures](Tests/native-regression-first-2392.json); [selected fixtures](Tests/native-regression-selection.json). The failures were two current-version pins, the haul-table omission and two additive-scene light collisions. Final targeted reruns below close them. |
| Final content, art, world and adversarial review | [106/106 succeeded](Tests/native-final-content-art-106.json), with no reported failures. |
| Final isolated scene checks | [4/4 succeeded](Tests/native-scenes-final-4.json); original editor scene restored clean. An earlier skipped-scene attempt was replaced and is not counted as passing evidence. |
| Damaged-door cue RED | [10 completed,4 failures](Tests/native-repair-hint-red-10.json); paired controls passed. |
| Final door, repair, hint, watering and harvest selection | [158/158 succeeded](Tests/native-repair-door-hints-final-158.json), with no reported failures. |

Selections overlap. They are not added together into a unique total, and they are not a whole-assembly pass.

## Content and art

[Parsed blueprint comparison](Tests/parsed-blueprint-diff.json): all664 prior objects unchanged, two new objects, no duplicate names. All14 existing source models and the24-swatch palette remain unchanged; four original fieldwork forms were added. Python geometry tests pass6/6. [Unity import receipt](Art/static-import.json) records the reviewed source hash. Source contact sheets are under `ArtSource/ConnectedSpread3D/Review`.

## Independent review

- [World/content review](Review/world-review.md): literal old-save admission, source placement, finite ownership, passage geometry, truthful local prose and scene isolation.
- [Watering and presentation review](Review/watering-art-review.md): exact carried source, reconciliation/payment ordering, rollback, actual-owner model state and damaged-door hints.

## Ordinary-input Play mode

The launcher creates an isolated audit save/settings context, chooses an ordinary Duelist build and sends ordinary keys. It does not grant water, materials, health, position, XP or time. Actual threats and route refusals can stop the run. Screenshots require separate visual inspection.

Interrupted attempts are retained in `Native/*-interrupted.json`: first5 checkpoints before a cautious cellar route refusal; second8 before correcting the driver's mistaken free-fill premise; third11 through real irrigation and repaired wicket crossing before an unrelated informant route refusal; fourth9 through filling before an inherited driver pursued a retreating enemy out of its route. None is claimed as a completed journey.

Final run [4045caba70154a09a61ccafcd4b35ed5](../SpreadDiscoveryExpeditions/Native/4045caba70154a09a61ccafcd4b35ed5/report.json) is complete: **19/19 passed**, failures0, unexpectedErrors0,226 paid local inputs and2 map steps. It verifies the real gourd/seed, pallet hauling/dismantling, paid well filling, three waterings, repaired/opened/crossed/closed wicket, finite northern forage, away-growth13→40, second harvest, next planting and actual F5/unsaved step/F6. Original clean SampleScene was restored; console errors0. Ordinary-camera captures of the new states, menu and final loaded yard were independently inspected.

## Honesty bounds

**Can verify:** exact payment, finite owner removal, crop growth while away, ordinary action menus, saved state, deterministic placement/legacy counters, imported state routing, and the specific native route recorded by its report. Screenshots can show that the tested camera displays the new objects and factual menu text.

**Cannot verify from these checks:** unaided player discovery, long-session economy, every seed/build/biome in Play mode, comparative rendering performance, animation feel, fun or campaign replayability. Those remain playtest questions. The native route selects the repaired wicket; the destructive alternative has separate controlled EditMode coverage.

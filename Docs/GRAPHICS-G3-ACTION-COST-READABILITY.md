# World-action cost readability

2026-10-26. Plan saved before production changes. Source dab99535b; actual
99b86bb02 packaged kitchen capture `earned-east-13-recipe-clipped.jpg` shows
"prepare field meal (2 grain, 1 claspbean~" and conceals the two-dram price.

## Verification and bounded design

- `KitchenBatchPart` authors the complete 50-character label. The menu truncates
  it to41; changing the recipe or its price would be the wrong repair.
- `CenteredPopupLayout` owns an80×45 gameplay overlay. Keep48 columns for short
  menus; expand to longest action label+7, capped at78 for a margin. All audited
  fixed cost labels fit: longest dressing60, ink56, pan50, coals45. Dynamic names
  remain unbounded; preserve the explicit `~` overflow and existing world F1
  reader, which includes the whole selected action. This is not a claim that
  arbitrary strings or generic choice menus have unlimited disclosure.
- Height, one-row-per-action, hotkeys, action identity, controller mapping and
  12-action scrolling remain unchanged. Render and mouse hit regions share width.
- Existing background clearing already remembers its old rectangle. Foreground
  does not; record and clear the actual previous footprint before recentering,
  including close, reader reopen and absent optional background.
- Reuse the existing Qud-inspired tile popup, colors and accepted scene. No new
  art/concept or speculative layout system is needed for this named text defect.

## Verification plan

Native NUnit RED on real emitted labels before runtime edits, then GREEN with
short-menu, maximum-width, input, scroll, background and stale-footprint counters.
Independently review diff and honest bounds. Replay the same earned kitchen
checkpoint with ordinary controls, inspect complete price/F1/back/close, capture
actual pixels. No grants, teleport or runtime injection. Preserve normal saves,
preferences, current builds, approved art and unrelated dirty files.

## Performance

Compute width once when actions are opened. Existing selection renders reuse it;
no new Update, per-frame allocation, resource, atlas, or gameplay state is added.

## Log and self-review

🟢 Both demonstrated defects corrected. Full-price, reader, clean-close, actual expanded-edge mouse commission and short-menu Examine are accepted within the recorded scope. Native regression: 115/115 passed.
⚪ CoO-original consistency correction; no exact Qud parity claim.
🧪 Physical Deck, arbitrary dynamic labels and packaged correction remain open.


### Native width and ordinary keyboard verification

The first run discovered zero cases before assembly import completed; it is invalid
(`action-cost-invalid-empty.xml`). Confirmed RED then ran16 cases,11 failed/5
passed, including the real meal clipping. The first GREEN was96/97; the remaining
failure was a camera projection assumption in the pointer fixture. Unity clips
its pixel rectangle at the view boundary. The fixture now aligns against the
actual projection and asserts the intended grid cell before calling the hit test.
The original pointer failure alone was not diagnostic of production behavior.
Final suite97/97 and boundary suite21/21 have16 overlapping cases: **102distinct
passing tests**, not118. Receipts: `action-cost-final-green*`,
`action-cost-boundary-green*`. Synthetic dressing/ink strings test width; they
are not gameplay proof of those recipes.

Actual Editor continuation of unchanged earned Working tick4830: ordinary Down,
C/Left, open tray, Tab take all, Up, C/Left recovered two grain and one pulp,
cancelled the batch, and exposed the complete57-column recipe at tick4860.
The previously paid fee stays spent (40drams). F1 and back/close preserve tick,
HP6 and ingredients. Native1280×800 captures `action-cost-01` through`03` retain
accepted scene art. F5 saved an earned recovered-input fixture, hash
3df4b7e19d13289530a56bb47d065a0a0cd6656e86a13d397946e61fbf6611d8.

### Live pointer defect and amended implementation plan

Two real CUA clicks, one on the widened right edge and one in the original
interior, cancelled instead of selecting. Runtime `_lastMousePosition` remained
1106,436 (outside the940-pixel gameplay viewport); native Mouse correctly read
276,462 for the second click. This is the already-documented inventory/hotbar
backend mismatch, not a reason to rescale geometry. Editor callback Screen fields
2321×1139 are context-dependent; actual saved PNGs are1280×800.

Reuse the existing one-enabled-native-Mouse policy for popup pointer position
and left-button edge together, with coherent legacy fallback. Seed parked-pointer
state from that same source on Open. Keep keyboard/controller priority and
outside-click cancellation. Add real queued MouseState→HandleInput RED cases,
held/repress, hover/right/outside, stationary-open and fallback counters; rerun
width/neighbor tests and repeat the exact actual click. No broad input migration,
new art or gameplay change is warranted. Root stopped and restored isolated
play at20:13UTC; normal saves/preferences remain protected.


Pointer correction native evidence:13/13 reproduced RED (10 behavioral failures,
3 absent-helper contract failures). `ReadPointer` now reads an enabled native
Mouse's position and left edge together, falling back to the matching legacy
pair. Open seeds the same captured pointer; HandleInput uses one captured frame
for hover and click. No layout transform or new polling allocation is added.
The first pointer discovery was invalid because its newly authored metadata GUID
had33 characters; the metadata now validates as32 hex, and actual assembly import
was verified before the valid RED run. No zero-case run counts as evidence.

The final native group selection comprised six fixtures:
WorldActionMenuPointerTests, WorldActionCostReadabilityTests,
QudControllerChoiceMenuTests, GameAuditShortcutAdversarialTests,
WorldLogHistoryInputTests, SpreadWorldExamineReaderTests. **115/115 passed**,
0skipped,20:23:22–20:23:35UTC (`action-pointer-green.xml`). The MCP15-second
startup watchdog reported a failure before Unity began; the retained job receipt
and full native XML disclose this discrepancy. This is not115 failed tests.
Independent source and fixture review found no material blocker.


### Actual mouse acceptance and restoration

On a fresh isolated Editor continuation of the unchanged recovered-input save,
ordinary C/Left opened the full 57-column recipe. A real CUA click at the newly
exposed right edge selected the meal: 40→38 drams, tick 4860→4870, two grain and
one pulp removed from the pack, and the Working cloth visible. The next menu
shrunk to 48 columns without stale wings or rows. A real mouse click on Examine
opened the same pan's current portrait and 110-tick reader without advancing time.
Captures `action-cost-04`–`06` and their observational receipts preserve the
results. Independent visual and source reviews found no material blocker.

F5 saved the isolated replay; its save tree was archived at
`/tmp/coo-action-pointer-editor-Saves`. Native isolation, normal scene/start scene,
requested seed and build-choice setting are restored; Editor is stopped and
save-root override is null (`action-pointer-play-restored.json`). Thirty-two
Unity-import metadata changes were proven trailing-whitespace-only against HEAD
before restoring just those bytes. All 776 protected normal-save files, 374
protected source files and both pre-existing dirty PNGs remain unchanged.
No storage reclamation occurred; about 418 GiB remained free.

The native result XML is authoritative; the startup watchdog result is retained
as a tooling limitation. The first width run's accidentally mismatched
`action-cost-red.xml`/`action-cost-red-summary.json` are excluded from published
evidence; use `action-cost-confirmed-red*`. No zero-discovery run is a pass.

### Saved implementation and delivery boundary

```csharp
// Open: size once using every action; selection does not recenter the menu.
_popupW = Mathf.Max(_popupW, Mathf.Min(label.Length, MAX_POPUP_W - 7) + 7);
// HandleInput: use one coherent pointer sample for hover and activation.
ReadPointer(out Vector3 mousePosition, out bool mousePressed);
_lastMousePosition = mousePosition;
```

Modified `WorldActionMenuUI.cs`; added `WorldActionCostReadabilityTests.cs` and
`WorldActionMenuPointerTests.cs` with Unity metadata. No recipe, economy, asset,
world geometry, or shared camera change. The amended scope includes the actual
pointer defect found during width acceptance; it is documented before its fix.
This is CoO-specific UI maintenance, not an exact Qud implementation.

Current Mac/Linux runtime packages remain **99b86bb02** and **predate this fix**.
Source and actual Editor acceptance do not replace a corrected packaged replay.
Next freeze and deliver this correction, then continue the collected meal
checkpoint's North/Nella revisit and remaining forge/crop acceptance. Full opening,
regional coverage, Linux execution, physical Deck and performance remain open.

The six `action-cost-01`–`06` complete observational JSON receipts are losslessly archived as `.json.gz`; the actual PNG captures are unchanged. The raw receipts include off-camera owners and are not a visible-coverage census.

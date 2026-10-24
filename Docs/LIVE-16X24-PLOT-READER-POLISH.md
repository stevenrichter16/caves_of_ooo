# Live 16×24 plot reader polish

Status: implemented; native RED→GREEN and ordinary keyboard replay accepted within the limits below. Two presentation defects found during ordinary keyboard plot acceptance on 2026-10-24. Accepted art remains unchanged. This is local Caves of Ooo behavior, not a Qud parity claim.

## Scope and cadence

Repair stale FOCUS after real harvest and route ordinary carried Examine to the existing subject reader. Apply tests first, record native RED, make minimal runtime changes, run bounded regression, independent review, then repeat the actual keyboard actions and capture the Game View. No gameplay state injection. Preserve failed evidence as failed evidence.

The following two saved draft sweeps describe the pre-implementation state; the final native and ordinary results are recorded afterward.

## Saved verification sweep: Look

# Stale Look / sidebar FOCUS — bounded TDD draft, 2026-10-24

Status: scratch only. Root owns active ordinary Play and all Unity operations. Nothing in Assets is modified by this draft. No native test result or compile success is claimed.

## Observed defect

`Docs/Verification/Live16x24/plot-products-loop-resin-ground.png` shows an exact PitchPod resin Examine popup while the right FOCUS still names the removed PitchPod at [18,6]. The paired JSON records no crop at that cell, but does record the output items. P2: stale user-facing information after a legitimate world transaction. Simulation removal and exact Examine succeeded.

## Verified source and corrections

| Source | What it actually establishes | Correction / consequence |
|---|---|---|
| `InputHandler.cs:2483` | `RefreshLookSnapshot` creates an immutable query result and passes it to ZoneRenderer. Callers are Look entry/movement/recenter/restoration. | No existing automatic world-mutation refresh. Do not add another crop-only query path. |
| `InputHandler.cs:3348–3367` | Successful real harvest commits through InventorySystem, pays/processes a turn and requests full redraw; returns to prior input state. Refusal is free. | The repair must not change payment, harvest, menu or return-mode semantics. |
| `ZoneRenderer.cs:758–805` | Full/cell dirty methods invalidate the sidebar painter, but do not replace the Look snapshot. | Repainting alone cannot repair stale prose. |
| `ZoneRenderer.cs:954–991` | The world pass consumes full/cell dirtiness; the full path recomputes FOV in `RenderZoneCore:1039–1046`. | Capture a bounded refresh trigger before dirty state is cleared; perform the query after world/FOV refresh. |
| `ZoneRenderer.cs:1565–1591` / `SidebarStateBuilder.cs:68` | Renderer supplies `_currentLookSnapshot`; state builder always prefers that cached snapshot. | Refresh immediately before the existing sidebar build, with no new painter. |
| `LookQueryService.cs:13–89` | Builds live header/contents/status from a cell; it does **not** gate cell fog itself. | New refresh must check current zone/cell identity plus `Explored && IsVisible` before querying; hidden focus is cleared. Do not expand the query service in this packet. |
| `ZoneRenderer.SetZone:607–634` | Clears snapshot/cursor normally. | Preserve it; reject a delayed old cursor even if a replacement zone has the same string ID. |
| `SidebarRendererTests.cs:75–99` | Existing explicit snapshots can be rendered without a world cursor. | Retain this legacy API. Validation applies when a cursor is bound, not to every direct snapshot caller. |
| `WorldActionMenuUI.SelectAction:259` | Closes UI after choosing an action but selected context can be captured first. | Real fixture snapshots target/cell/pile, selects actual Harvest row, then invokes actual Input dispatch. |
| `Objects.json:45111–45240` | PitchpodCrop is manual, produces 2 resin and 1 seed; raw names are lower-case. | Tests use actual product display names; no inferred case/title contract. Old crop name is a prefix of product name, so do not use naive substring absence to prove old header removal. |

No Qud parity claim: this is a local cached-presentation correction.

## Minimal implementation

Only `ZoneRenderer.cs`. Before its dirty render branch, remember whether a full redraw or the cursor cell was dirty. After FOV/world render, validate the bound active Look cursor, exact zone and player references, canonical cell owner and disclosure. Rebuild the snapshot only for relevant dirty work, missing snapshot, changed cell identity or changed cursor coordinates. Clear invalid or hidden bound Look context. Keep the same snapshot object on unrelated/idle frames. Fullscreen pause returns before this work, and existing resume forces a full repaint.

No new global callbacks, mutation bus, input-mode logic, crop rules, world scans, periodic query, extra renderer, atlas or resource changes. The overall sidebar already builds other state every frame; this change claims only no repeated Look query / LookSnapshot allocation on quiet frames, not zero allocation for all of LateUpdate.

## Draft native fixture

`CavesOfOoo.Tests.LookFocusRefreshTests` (15 cases intended; verify discovered native count).

- Real ripe Pitchpod menu harvest and stale-menu unripe refusal; settled cursor stays fixed. Assert actual crop/output graph, normal one-action tick/energy, cached primary and already-submitted sidebar header.
- Focused cell versus full dirty refresh, and unrelated-cell counter.
- Hidden versus unexplored focus; reveal recovers; hidden private name not painted.
- Full repaint changes FOV first, so refresh cannot query previous visible state.
- Same-ID replacement graph with a delayed old cursor/snapshot; canonical cell replacement.
- Deactivated versus different-mode cursor clears old Look context.
- Fullscreen pause defers query; resume updates.
- Twenty idle frames retain exact snapshot identity and tick; dirty sets settle.
- Explicit standalone no-cursor snapshot remains compatible.

Uses real current renderer LateUpdate and GameplaySidebarRenderer with owned inactive Unity hosts; avoids Awake/global searches and destroys only owned objects. HotbarSaveFixture/HaulingContentScope restore borrowed simulation/message state; additional weak tile-state render binding, callbacks, Unity random and performance frame snapshots are restored. This is native integration coverage, not actual keyboard delivery or feel.

Expected RED: genuine old snapshot/assertion failures in harvest, dirty scope, fog/FOV, replacements, inactive/mode and resume. Idle, explicit standalone and refused action are controls. A fixture compile/setup error or missing visible header does not count as behavioral RED.

## Handoff / native cadence

1. Root reviews/applies `tests.patch` only while Play is stopped, imports new fixture, verifies actual discovered count and records native RED with no compilation errors.
2. Fix any fixture setup defects without weakening behavioral assertions; rerun affected native RED.
3. Apply `production.patch` against manifest baseline; run the focused fixture GREEN.
4. Bounded regression: `SidebarRendererTests`, `SidebarStateBuilderTests` if present, `LookQueryServiceTests`, `RepairCultivationInputTests`, `SpreadWorldExamineReaderTests` and active Look input tests found by root. Do not sum overlapping selected groups as distinct.
5. Ordinary loop: Look ripe crop → Harvest → stay on same cursor; inspect current product, then hidden/revealed focus and resume. New live screenshot must show current FOCUS and exact product popup; no claim that this scratch alone closes that gate.

Files: `LookFocusRefreshTests.cs`, fresh two-line `.meta`, `tests.patch`, `production.patch`, full `ZoneRenderer.proposed.cs`, `manifest.json`. Baseline SHA recorded separately. Tests and production remain separate for RED first.

## Self-review / limits

- P2 observed stale focus addressed by event-driven local seam, pending actual native RED/GREEN and ordinary acceptance.
- Visibility evaluated after full FOV paint; idle validation is constant-time. No hidden contents queried through this new path.
- Existing input refresh entry points can create snapshots directly and are not globally redesigned; this proposal validates the bound snapshot before the next ordinary sidebar paint.
- Owner-part/fog correctness of LookQueryService itself beyond these gates is unchanged. This is not a generic query-authority rewrite.
- The drafted fixture has only source review so far. Native setup remains root's gate; unsupported claims about compilation, performance or visual quality are excluded.


## Saved verification sweep: inventory

# Ordinary carried Examine portrait route — implementation draft

2026-10-24. **Scratch-only delivery.** No repo files changed, no Unity commands, no compilation or tests run. Root must apply `tests.patch`, observe native RED, then apply `production.patch`. `git apply --check` succeeds on both against the captured bases. Source SHA256s are in `base.json`; full draft files accompany the patches for inspection only.

## Player-visible invariant

An already-offered ordinary inventory `Examine` action opens the same structured reader and exact subject portrait used by supported item inspection, while leaving the item, world and action budget untouched. A stale item/provider is visibly rejected and the menu remains available. This closes the carried PitchpodSeed discovery from real Play without adding items, mechanics, sprites, menu entries or changing planting.

## Verification sweep and corrections

| Reference | Verified fact | Design consequence |
|---|---|---|
| InventoryUI.OpenItemActionPopup | Specialized routes are added only for material/tonic/ItemExamineService details; other Examinable items retain existing uppercase event action | Handle that existing command, preserving original menu order/hotkeys and all specialized precedence |
| ExecuteItemAction default + ExaminablePart.HandleEvent | Uppercase action logs text and returns handled; success closes submenu without an announcement | Route only InventoryUI's uppercase action to the current validated presentation branch; leave global event consumers untouched |
| PitchpodSeed / EmberwheatSeed / GoldCoin blueprints | All actual Item inheritors with Examinable prose/valid render/physics/stack; no supported material/tonic/mechanics formatter | Three nonvacuous positive cases; tests assert all special formatters return false first |
| FlatPixelOpeningArtCatalog PlotProduct frame6 | PitchpodSeed portrait art is already mapped | No new art or per-blueprint shortcut |
| ExamineDocumentBuilder.BuildInventory | Checks exact carried/equipped ownership, valid stack and render/physics/examinable ownership before/after constructing description snapshot | Reuse this authority; no weaker string-only fallback |
| AnnouncementDocument.ToPlainText | Existing complete accessible/log fallback, already used by AnnouncementUI.OpenDocument | **Correction to initial recommendation:** generic route builds one structured snapshot and uses its ToPlainText; do not call legacy BuildExamineLine first |
| ExaminablePart.BuildExamineLine enhancement callback loop | Removing the executing ExaminablePart can clear ParentEntity during its legacy loop | One structured snapshot avoids double callbacks and this legacy null hazard; no need to refactor ExaminablePart |
| DensityItemExamineInventoryTests stale-specialized cases | Removing supported weapon/tonic parts after opening must retain existing refusal | Do not broaden BuildItemExaminePopupText for the special routes; they remain unchanged |

## Minimal runtime patch

One file: InventoryUI.cs. Add uppercase `Examine` alongside the existing tonic/item case labels. For that command only, capture a current owned ExaminablePart, build a structured inventory snapshot, recheck exact provider identity, then publish the document with `ToPlainText()`. The existing item-ownership rejection, rejected-menu behavior, diagnostic events, shared popup, return-to-inventory, no-turn semantics and specialized text paths remain.

The ordinary log formatting intentionally becomes the shared document's complete title/facts/sections instead of the old `You see…` log-only line. Authored prose remains unchanged. No duplicate legacy callback or fallback announcement is published if snapshot validation fails. No TDD/art claims until native execution and actual input recapture.

## Test patch

Use existing fixtures only. Counts below are source-attribute counts, not native receipts:

- DensityItemExamineInventoryTests:15→18 cases. Actual PitchpodSeed/EmberwheatSeed/GoldCoin stack2; exactly one original `Examine` action; planting/drop retained; real InputHandler announcement handoff; exact document subject/viewer, authored prose, same accessible snapshot; unchanged inventory/world/health/drams and no pending turn; dismissal returns to inventory.
- ExamineLedgerInventoryTests:12→24 cases. Extend valid structured snapshot cases to seed/coin, callback-carriage invalidation to seed, and stale render/physics owner cases to seed. Five captured-menu defects: removed item, zero stack, foreign custody, missing Examinable, foreign Examinable. Two description-callback cases remove or replace the provider during the single snapshot; require visible refusal, same menu and no stale announcement.
- Combined source count42 =27 existing +15 additional. Expected old runtime:27 existing passes,15 failures. This is a hypothesis, not a promised count. Provider-removal callbacks intentionally expose a real old path exception; Assert.DoesNotThrow turns it into a behavioral assertion failure, not a fabricated fixture/setup error. Diagnose actual output rather than claiming this prediction as evidence.

Existing specialized-route rejection, material priority, non-Item tonic and equipped-item tests remain unchanged. New tests use actual menu lookup/command execution rather than invoking a proposed API directly. The test helper's fallback chooses ordinary `Examine` only when its fixture item has no supported detail formatter; the positive UI cases independently assert the exact command.

## Required gates after applying

1. Apply tests only and run both native fixtures; record actual RED with production untouched.
2. Apply one-file production patch; compile then run both focused fixtures plus the existing material-guidance/equipment-comparison/examine-ledger presentation regression if relevant to parent sweep.
3. Independent cold review of source/test/doc diff. Significant issues fixed before commit.
4. In the existing ordinary plot session (or normal equivalent), carried PitchpodSeed → inventory action Examine must actually show its structured ledger and native seed portrait. Dismiss and confirm same inventory/seed count; then continue plot action. Keyboard/Deck claims require their corresponding real input evidence.
5. Update same-commit living records and push only owned files after root's exact staging/fetch/rebase workflow.

No Assets files or `.meta` files are introduced. No Unity/asset-import work was done by this agent. Root alone owns editor and acceptance.


## Native implementation log

- Tests applied before either runtime patch. Script-only import discovered the two existing inventory fixtures but not the new Look fixture. The first receipt `plot-reader-polish-red.xml` therefore contains42cases:27passed,15failed,0skipped. These are actual missing popup/rejection assertions, not a claimed57-case run.
- Full asset import compiled the new fixture without errors. Separate `plot-look-focus-red.xml` contains15cases:3passed,12failed,0skipped. Failure messages are stale snapshots/headers, hidden context, graph replacement and real successful harvest. Refusal, quiet frames and explicit standalone snapshot are passing controls.
- Applied the two minimal production patches after those RED receipts. Native GREEN and ordinary replay remain pending.
- Independent read-only reviews of both draft patches found no actionable P1/P2; runtime and actual input evidence are still separate gates.

- Focused GREEN:57/57 native,0skipped. Bounded regression:126/127,0skipped. The one failure was the old PaleSalt control that inferred “not repair guidance” from no popup. This deliberately conflicts with the new generic reader. Updated that pin to require exact PaleSalt authored prose/blueprint, no well/oven advice and no MaterialExamined diagnostic, retaining unchanged-item checks. No production change was needed. The original regression receipt is preserved; final rerun follows.


## Final verification and ordinary replay

Focused native57/57 and final bounded regression127/127 passed, zero skipped
(**184 distinct current cases**,30 newly added; do not sum earlier runs).
The original failed regression remains `plot-reader-polish-regression.xml`;
final green is `plot-reader-polish-regression-final.xml`. Independent review
found no outstanding P1/P2. The updated PaleSalt pin asserts blueprint/prose,
no repair guidance and no material event; exact Subject/Viewer references
are separately checked by the generic inventory tests.

Fresh normal seed64 replay from SampleScene again walked two zones north,
paid8drams, selected the ripe Pitchpod through Look and harvested with the
cursor fixed at18,6. `plot-products-reader-before-harvest` shows PitchPod;
`-focus-after-harvest` shows PitchPod seed plus the two resin owners instead.
Tick990→1000 is the normal paid action. The following exact ground resin
reader preserves the current pile FOCUS and passes no turn. No source art
was changed.

After actual pickup (tick1010), inventory Examine now opens the native
PitchpodSeed portrait with its cultivation text. Dismissal returns to the
same inventory. Ordinary EmberwheatSeed and CandyCarrotSeed (displayed as
gladroot seed) also show their distinct authored portraits/stack counts.
All three captures retain tick1010,40HP,42drams and identical inventory.
The player then plants the same acquired PitchpodSeed in the emptied bed:
`-replanted` is a clean world capture at tick1020, same player18,7; seed is
consumed and stage0 crop appears. This supersedes the earlier pause-menu
planting capture without replacing that historical file.

Seven raw1280×800 PNG/JSON pairs are named `plot-products-reader-*`.
The new launch/cleanup receipts are `plot-reader-replay-*`. End-of-replay
console counts are0errors/2existing Metal memoryless-depth warnings. Play,
save-root override, art pending and borrowed view are all restored; preview
scale was restored to its original2×. Fifteen importer `.meta` files were
restored only after proving line-by-line trailing-whitespace equivalence.
The real F5 save was copied locally before owned isolation cleanup; the
checkpoint receipt names its `/tmp` path and byte hashes. It is optional
continuation material, not a packaged build or a modified user save.

## Self-review / divergences

- 🟡 Fixed both observed defects: stale Look snapshot after harvest and
  ordinary carried Examine disappearing into the log. Native controls cover
  refused/free actions, quiet frames, fog, replaced graphs, lost custody and
  description-provider changes. Actual keyboard screenshots confirm delivery.
- 🔵 The planned art-only acceptance expanded to these two narrow UI fixes
  because they prevented truthful use of accepted art. No sprites, recipes,
  crop timing, balance, menu entries or atlas layout changed.
- 🧪 A70ms planting/menu key in the earlier sequence was followed by an extra
  southwest step; the25ms replay stayed at18,7. Not reproduced/isolated enough
  to claim a key-repeat fix. Track separately if it recurs; no input code was
  changed here.
- ⚪ No new performance benchmark. Quiet-frame native tests retain snapshot
  identity; they do not establish whole-frame allocation/fps on Deck.

Can verify: actual keyboard route, item costs/counts, growth observations,
current FOCUS, shared portraits, menu return, native assertion results and
editor screenshots. Cannot verify from this packet: physical controller or
Deck, current packaged launch, continuous animation/audio feel, other seed
compositions, all product families or complete six-zone release.

## Files / next action

Runtime: ZoneRenderer.cs; InventoryUI.cs. Tests: new LookFocusRefreshTests.cs
with fresh `.meta`; extended DensityItemExamineInventoryTests and
ExamineLedgerInventoryTests; updated MaterialGuidanceInventoryTests pin.
Living records: this file, plot-products, main direction, completion plan,
release-coverage. Raw current evidence and independent review live under
Docs/Verification/Live16x24.

Continue remaining ordinary product uses (Marlroot seed portrait, gladroot
output/use, InertSludge trade/use and resin brewing), then the authored works
forage/preparation/brew/coin gate. Preserve approved art. East/kitchen,
remaining G2 scene/action acceptance, complete six-site journey, packaged
activation and hardware remain open; keep continuation active.

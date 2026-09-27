# M2 — Readable world inspection and measured sidebar fit

Status: reader, action recovery, paragraph handling and measured sidebar margin implemented and native-verified. The staged native UI route and all 11 captures are accepted; the separately reproduced PageUp/PageDown adapter gap is repaired and its four native input cases pass. M2 has no remaining scoped implementation or staged UI gate; the combined first-hour integration now passes20,406/20,406 in actual Unity (see `../Integration/`). Specification: `Docs/SPREAD-FIRST-HOUR-PLAN.md` R1/R2 and current-state R4. Qud reference: none. Unity/editor execution belongs to root; this owner prepares bounded source/tests/probes. M3 retains InventoryUI, ItemExamineService and InventoryScreenData.

## Pre-implementation sweep and corrections

| Verified source | Correction / implementation boundary |
| --- | --- |
| `ExaminablePart.BuildExamineLine` includes owner description, item mechanics and effects, but no selected-cell ground status. | A complete world reader must separately include the existing visibility-gated `CellStatusReadout.GroundLine`; owner effects cannot substitute for ground hazards. |
| `InputHandler.ExecuteWorldActionSelection` handles pile Examine separately and otherwise fires the owner event. `TryOpenAnnouncement` and `CloseAnnouncement` already remember the previous input state and restore the appropriate camera. | Preserve pile summary versus exact selected owner, use the existing reader/queue and return state. Do not add a new text system or alternate equip path. |
| The terrain-stack `PickCell` route is already implemented, with ten native cases and a successful actual hot/cool route. | Reuse it unchanged; current-owner validation for opening a full reader is a distinct boundary, not a reason to rebuild picker navigation. |
| `AnnouncementUI.Open(string)`, `PageCount`, `VisibleLines`, `GoToPage` already support paragraphs/pages. | Keep this API stable for M3. Reader contents are a snapshot while open; re-open uses the current owner/state. |
| `WorldActionMenuUI.BuildStatusLinesFor` caps six rows and says to see Look, whose focus is also capped. Action labels themselves truncate. | Full owner/ground detail must be reachable; selected long action text needs a recoverable display route. Do not route overflow back to another capped summary. |
| `SidebarTextFormatter.WrapWithPrefixes` / `WrapPlain` wrap only at spaces; descriptions intentionally contain explicit newlines. | Add paired paragraph/long-word/blank-line formatter tests before changing wrapping. |
| Sidebar34 columns/31 content,8×16/PPU16 sprites match the0.5×1 text grid. Camera metrics have no explicit glyph inset; the redraw cache omits position. | Native projected glyph/camera measurement comes before layout changes. Neither padding nor cache invalidation is yet an established clipping cause. Preserve world framing. |
| Existing sidebar test checks the left tile-coordinate bound only. | New evidence must use actual glyph sprite extents and final camera/transform, including right/top/bottom.1920×1080 is the observed/default baseline; no new minimum-display claim. |
| A valid corpse can be inspected, and loading a save legitimately restores earlier time/items. | Do not require a living target merely to inspect. Free reader operations preserve current state; save/load restores the selected save, a separate contract. |

## Bounded sequence

1. Author current-flow world-reader and paragraph/overflow tests; root executes actual native RED before production.
2. Give root a read-only live-sidebar probe (camera, grid, tile sprite corner projections, rendered rows); record actual observation before any layout fix.
3. Implement only failed current-owner/full-detail/overflow/paragraph behavior; keep M3 API and free inspection/camera return intact.
4. Paired changed/empty/stale/foreign/hidden/current corpse cases, dedicated adversarial cross-instance and callback cases as warranted, focused native GREEN and ordinary keyboard/image acceptance.
5. Q1–Q4 and exact owned-path manifest. No commit/push by this owner.

## Readiness and evidence

- Prior audit: `/tmp/coo-first-hour-readability-review.md` (planning/source inspection only).
- Retained symptom: `Docs/Verification/DensityCompletion/Environment/NativeHotSteam/10851284a3874055a979dcc7bdd1b391/hot-steam-source-examine.png`; complete-fit quality was explicitly not accepted.
- Current evidence and final review appear below. Earlier preparation sections are historical checkpoints, not current pending work.
- Exact current owned paths are recorded in `owned-current.json`; this does not claim M3 comparison production or unrelated shared InputHandler functions.

### Historical preparation checkpoint

- Private fixtures: `SpreadWorldExamineReaderTests`16 and `SpreadSidebarParagraphTests`10. Current-reference test compilation succeeded; peer source review found no concrete blocker. Actual native execution remains pending and must separate premise failures from behavioral RED.
- Root read-only native geometry capture: M0/sidebar-before.json, 1920×1080 camera rect at x1461, width459, height1080; Screen API in that capture reported2077×1251, so these values are recorded separately.710 total glyphs have zero outside-frustum rectangles.303 ASCII text glyphs (excluding decorative CP437 borders) have L27/R54/B27/T0-pixel minimum inset. This proves flush top text, not right overflow. The retained image is `M0/isolated-gameplay-sidebar-before.png`; it is not an ordinary discovery witness.
- R1 private `SpreadSidebarGlyphBoundsTests`3 cases reproduce the measured camera/grid/glyph geometry with owned objects and require a half-row top/bottom text inset. No horizontal-width/font/world-camera change is proposed without a witness. Native RED required before the small safe-row repair.
- M3 confirms existing AnnouncementUI string/page API is sufficient. No M3 file overlap.

### Reader/paragraph native baseline and bounded extension

- Root actual native job `cca9dc475ce9422890af775063b6cf20` exercised the original reader16/paragraph10 alongside other agents' fixtures. Reader authority/full detail and explicit paragraphs failed as intended; exact per-case receipt is `native-red.json`. Four production files were prepared privately, peer-read, compiled against current references (zero errors), then published by root. This is not yet a GREEN claim.
- The first glyph fixture incorrectly used an inactive Grid, yielding zero cell centers and a spurious left-edge failure. Corrected owned active Grid, exact anchor and `(2,39)` local center preconditions passed; actual corrected native3 now fails specifically on top inset0 versus13.49px. `glyph-native-red.json` preserves this distinction. R1 candidate reserves one text row at each vertical edge and keeps full-height background/divider, camera/font/width unchanged; measured baseline text rows40 become38.
- R2 action overflow extension: root approved a separate F1 route for the selected menu action. It will snapshot the freshly offered full label plus existing owner/ground description, perform no action, and rebuild the actual current menu on return. Removed owner returns to the original Look/gameplay mode; disappeared action is not resurrected. Empty menus have no invented details. No new availability simulation or hypothetical action outcome is introduced.
- Supplemental test-only source: original reader16 plus seven current-action/F1 cases, three ownership adversarial cases (hidden pile member, selected physical footprint, removal during description) and three actual-reader newline cases. Actual supplemental execution remains pending. LF already works; CR/CRLF need their own actual witness before normalization.

### Supplemental production and acceptance preparation

- Actual supplemental native run retained in `focused-integration-02-red.json/xml.gz`: seven F1 missing-route failures and two CR/CRLF paragraph failures; LF and three new owner-adversarial cases passed against the first reader change. F1/CR three-file candidate and two-file margin candidate were privately compiled (zero errors), peer-read, and handed to root for exact publication. Root owns actual execution; no GREEN claim is made here yet.
- F1 retains current Name/Command/FireOnActor identity, re-gathers the actual offered rows, rejects ambiguous/disappeared rows, and snapshots the full current label with the existing owner/ground text. It never fires InventoryAction. Return re-gathers the current menu, preserving the highlighted identity only if still present; removed owners return to prior Look/gameplay. The menu footer advertises F1. Empty menus do not fabricate details.
- Native neighboring HotSteam terrain-menu positives were originally authored with default unseen/unexplored cells through private dispatch. The new visibility guard refused those four rows. The narrow fixture correction explicitly establishes a visible explored source cell; exact source/warning/log/free-turn/owner controls remain. M2 already has passing actual fogged/unexplored refusal controls. No visibility restriction is weakened.
- Private staged native acceptance package: `/tmp/coo-first-hour-ui-native`. It uses the existing owned save/scene retry pattern, real N/Look/picker/Examine/F1/paging/Compare/return keys, and records the actual camera-output glyph bounds. Factory OilSeep temperature/effect/ground, long description/nonexecuted action, and carried ShortSword are declared fixtures. Cooling is an explicit between-measurement state change. This does not establish ordinary discovery/acquisition, combat balance, or broad display-size support. Peer read and reference compile are clear; actual route/images/restoration remain pending.
- R4 scope remains factual: current effect/ground wording comes from the existing describers, before long flavor in world Examine. Existing Look stance/pacification, HP and expiry behavior remain unchanged; no power score, predicted AI intention or new hidden-state readout is added.

### Actual gates and first native route observation

- Root authoritative native `cb388617132e4feca966add146cc5ad5`:122/122 pass, including all M2 reader29/paragraph10/glyph3, M3 comparison46 and launcher restoration34. Corrected HotSteam menu positives also passed. No extra broad sweep is requested for this bounded slice.
- First staged native run `49161e5d9908435ca9fe34635db7cb33` reached the real owner reader with a clearly readable scald sentence, resistance note and separate hot-ground line before flavor. The image has visible VITALS top spacing. Both root and this owner inspected the first page; this is first-page evidence only.
- The route then failed to reach the final description paragraph: the harness injected new-system PageDown, while InputHelper's new-keyboard map omitted PageUp/PageDown. The reported index was a loop index, not observed state, so it did not establish page advancement. The corrected driver uses the displayed, mapped RightArrow and pins/records actual `_pageIndex` before every snapshot. Failed evidence is retained. Root additionally authorized four paired actual-input tests (missing PageUp/PageDown versus existing Left/Right controls), followed by only the two missing mappings if native RED is confirmed; no input-adapter refactor.


## Native completion and visual review

- Successful staged route `77cf480c9c004615a8b078373f3cf7ea`: complete, zero failures, five checks, 34 actual key inputs, 9.1466886 seconds. `Native/77cf480c9c004615a8b078373f3cf7ea/report.json` records actual page indices for all three world pages, four action pages and two equipment pages. This fixes the earlier probe that recorded only a loop index. The first failed run remains preserved.
- Root and the M2 owner independently viewed all 11 PNGs. Immediate owner scald text and the separately labeled ground warning are visible before long flavor. First/last pages show the complete long owner description; F1 exposes the selected full long action and returns to its actual menu without dispatch. A fresh reader distinguishes explicitly cooled owner steam from ground that remains hot. Equipment comparison first/last pages show candidate/current facts and its contribution-only/current-state limits, then return to the same inventory. No blocking clipping, lost final paragraph or stale modal overlay was observed.
- Final native sidebar: 527 ASCII glyph rectangles, zero outside the 459×1080 sidebar camera within the 1920×1080 output. Minimum text insets are left27, right13.5, top27 and bottom27 pixels. The previous measured top inset was zero; the fix reserves one text row per vertical edge while keeping background/divider, width/font and world camera unchanged. This is measured acceptance at the observed display, not a newly declared display-size range.
- The route checks unchanged player time/energy/HP, current zone owner IDs/positions/HP, inventory/equipped IDs, source temperature/density, prior input mode and camera. It does not assert every world field is equal. The source, hot effect/ground, long text/action and carried sword are explicitly staged; between-measurement cooling is explicit. This proves readable free inspection and current-state return, not ordinary acquisition/discovery, combat balance or encounter playtesting.
- Paging follow-up actual native `paging-native-red.json`: two missing PageUp/PageDown failures, two arrow controls and eight existing SidebarRenderer tests passed. The change adds only those two new-backend dictionary entries; existing legacy-first press/hold/release behavior is unchanged. Current native-reference compilation and independent peer review are clear. Final native paging4 PASS is retained in `../source-art-save-paging-01.json/.xml.gz`; that mixed238-case run had234 passes and four unrelated expected/premise failures (two M1 missing-launcher cases and two E1 corpse-chance gallery premises), not four paging failures. The prior eight SidebarRenderer controls also passed.

## Final Q1–Q4 and scope

| Review | Finding / resolution |
| --- | --- |
| Q1 — open/close and forward/back symmetry | World Examine returns to its prior Look/gameplay state; F1 returns to freshly rebuilt current actions; inventory comparison retains the existing inventory-return branch. Opening and returning both validate the exact current owner. PageUp/PageDown are a symmetric mapping pair beside the already-working arrows. Actual native captures and state signatures exercise the returns. |
| Q2 — cross-feature consistency | Owner effects and selected visible ground remain separate, reuse existing describers and appear before long world flavor. Existing inventory `BuildExamineLine()` ordering/API is retained. All readers reuse `AnnouncementUI.Open(string)` and normalize LF/CRLF/CR; F1 is explicitly reading only, without an alternate action execution path. |
| Q3 — counter/adversarial coverage | Tests cover removed, moved, hidden, fogged, unexplored, wrong-zone, missing/replaced owner/part, callback removal, corpse, physical footprint, visible pile versus selected owner, empty menu, disappearing/changed action, unchanged while-open snapshot and fresh reopen, long/blank/CR paragraphs, and actual new-backend press/hold/release. Native menu/reader/camera/free-state positives and glyph bounds accompany the countercases. |
| Q4 — docs versus implementation | Measured top flush was confirmed; right clipping was not. The first inactive-Grid fixture and the PageDown harness assumption remain labeled failed premises. M2 adds selected-action recovery via F1 because the original capped status route alone could not recover long action labels. It does not add a text system, inventory comparison engine, threat score, hidden-state reveal or global UI redesign. |

Cold-eye review of the combined current M2 diff found no remaining concrete blocking issue. The earlier mutation hypotheses have dedicated passing controls; no extra broad sweep or new native variant is justified absent a new witness. This is a CoO UI improvement, not a Qud-parity claim.

### Implementation shape

`Examine` validates the current visible selected owner, builds an owner-plus-ground snapshot, revalidates after descriptions, and uses the existing free announcement queue. `F1` uniquely rematches the current action identity, reads its full current label, then rebuilds current rows on close. Sidebar wrapping keeps explicit paragraphs; the sidebar uses 38 safe text rows within the prior 40-row background at the measured baseline.

### Deliberate bounds / deferred work

- ⚪ Full text is a snapshot while its reader is open; reopen refreshes current owner/action state. Action execution still performs its existing authority/reach checks.
- ⚪ No new models were needed for this UI slice. Existing approved world art remains visible around the reader; new encounters and their art are separate M5/E1 work.
- 🧪 Other display sizes and unusual font/camera configurations are not visually certified by the one 1920×1080 route. Source tests exercise the measured geometry, not an invented universal resolution contract.
- ⚪ M3 owns the comparison extension. M2's native route observes its reader/return integration only; it does not claim ownership of its projection or equip planning.

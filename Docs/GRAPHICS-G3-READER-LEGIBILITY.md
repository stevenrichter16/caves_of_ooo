# G3 — Examine reader legibility

2026-10-25 UTC. **Implemented; native and bounded ordinary acceptance complete.** The plan below
was written and independently reviewed before production changes. Root completed
the ordinary six-site route and saved its exact state before editing. This is
one measured correction within the existing bronze Examine/announcement reader,
using its actual crop screenshot. Preserve
approved artwork and the connected-route priority in
[GRAPHICS-COMPLETION-PLAN.md](GRAPHICS-COMPLETION-PLAN.md).

## Observed problem and measurement

The Claspbean paragraph in
[kitchen-crop-ripe-reader.png](Verification/Live16x24/kitchen-crop-ripe-reader.png)
is visibly cramped at the actual 1280×800 capture size. A read-only Pillow scan
of x=163..776 measured the ivory ink `(227,221,195)` against `(13,13,13)`:

| Paragraph ink, inclusive screen y | Blank scanline before the next line |
| --- | --- |
| 362–375 | 376 |
| 377–390 | 391 |
| 392–405 | 406 |
| 407–421 | 422 |
| 423–436 | 437 |
| 438–451 | — |

Each successive line has **one completely blank pixel row** between its ink.
This establishes very tight leading. It does not establish duplicate text,
actual vertical overdraw, truncated content or a gameplay-state error.

Source agrees: `HudPixelFont.Build` paints seven ink rows at 12 pixels/unit;
`AnnouncementUI.DrawText` submits a 1.6 scale while `Render` advances body lines
by one grid unit. The maximum ink height is therefore 7/12×1.6 = 0.9333 units,
leaving 0.0667 units before the next line, approximately one screen pixel in
the 680-pixel-high/45-row gameplay overlay. Native submitted-transform tests
must verify this geometry before implementation; this audit ran no Unity test.

## Verification sweep

| Premise | Source-checked result and correction |
| --- | --- |
| Text overlaps vertically | Not demonstrated. The actual image contains a blank scanline between these rows; frame this as a minimum-leading/readability requirement. |
| Existing typography tests already prove comfortable leading | `ExamineLedgerTypographyTests.BodyLettersStillFitTheirRowsAndRemainInsideThePanel` checks one row's ink height <1 and horizontal separation. It does not compare two populated adjacent rows or require a useful gap. Its `PaintedBounds` helper already reads actual sprite alpha and `Tilemap.GetTransformMatrix`. Extend it. |
| The shared font should be shrunk or rebuilt | Unnecessary. `HudPixelFont` is shared with the HUD; preserve its atlas and cached tiles. Preserve the existing reader's 1.6 body scale, title scale/advance, colors, width and portrait slot. |
| More line spacing can retain all 25 lines | That would expand the reader from at most37 rows to43 with the proposed spacing, violating the existing inventory-action clearance. Reduce page capacity with the spacing. |
| Inserting blank strings is the right spacing fix | No. `VisibleLines`, `_wrappedLines` and section/fact semantics must remain exact. Add visual leading without manufacturing prose rows. |
| All popup controllers share this renderer | They share the overlay but use distinct controllers. Scope production to `AnnouncementUI`; leave DialogueUI, inventory/action menus, sidebar, hotbar and `CenteredPopupLayout` unchanged. Examine, level-up and ordinary announcements that already use AnnouncementUI receive the same body spacing. |
| Footer hit regions require a new input system | No. `HandlePointerCell` derives the footer row from `_popupH`; preserve it and verify against the revised height. Existing keyboard/gamepad routes and `GoToPage` remain valid. |
| Portrait must move separately | No. Its slot is measured relative to `_worldTopY` in `OnGUI`; retain that header relationship and `MeasurePortraitRect`. A taller centered panel naturally moves the complete header. |

Relevant source read: `AnnouncementUI.cs` (capacity/page API, Open/OpenDocument,
Render, ComputePopupPosition, DrawText/DrawChar, pointer handling and clearing);
`HudPixelFont.cs`; `CenteredPopupLayout.cs`; `GameplayViewportLayout.cs`;
`ZoneRenderer.UpdatePopupOverlayGridLayout`. Exact current paths are under
`Assets/Scripts/Presentation/UI` or `Assets/Scripts/Presentation/Rendering`.

## Preferred minimal design

1. Use a named **1.25-unit body baseline advance** instead of1. Keep font size
   and horizontal wrapping unchanged. At1280×800 the expected maximum-height
   ink separation is about4–5 pixels, subject to actual raster placement.
2. Set `AnnouncementUI.PageLineCapacity` to **20**. Measure the body height as
   `ceil((max(1, visibleLines)-1) * 1.25) + 1`. The largest body remains25 grid
   rows; `BodyTop8 + body25 + footer4` remains37 rows. The centered bottom row
   remains4, preserving the existing protected inventory-action rows.
3. Keep body glyphs at their current distinct logical tile coordinates and add
   a per-line local y translation of `-i * (BodyLineAdvance-1)` to submitted
   transforms. Apply exactly the same translation to fact labels, values and
   their CP437 divider. The first line has zero translation. Do not move title,
   rules, footer or frame using this offset.
4. Compute `_popupH` from the measured body extent, then retain existing frame,
   footer, hit testing and old-region clearing. No new public service, font
   atlas, MonoBehaviour, draw layer or gameplay/document dependency is needed.

Planned private shape, not implemented code:

```csharp
const float BodyLineAdvance = 1.25f;
public const int PageLineCapacity = 20;
int count = Math.Max(1, VisibleLineCount);
int bodyRows = Mathf.CeilToInt((count - 1) * BodyLineAdvance) + 1;
_popupH = BodyTop + bodyRows + 4;
// Render body line i at its existing tile row, translating its complete
// label/value/divider by -i * (BodyLineAdvance - 1) local y units.
```

Tradeoff: a document needs approximately25% more pages when it fills every
page. Short readers remain compact; no words are removed, shortened or reflowed
to different widths. Two-unit leading would be excessive and reduce capacity
farther; shrinking the body would work against Deck readability. Do not adopt
either as a shortcut. If the actual reader is too tall or rhythm looks wrong,
adjust only this leading/capacity pair after the first real comparison.

## Test-first implementation gates

After root stops Play and authorizes the edit window:

1. Extend the existing `ExamineLedgerTypographyTests` fixture first. Use its
   real `AnnouncementUI.OpenDocument`, `Tilemap.GetTile/GetTransformMatrix`,
   sprite alpha scan and `GameplayViewportLayout.Measure` at1280×800 and
   1920×1080. A two-line H/g/j/p sample must keep all glyphs and require at
   least0.25 world units of painted vertical clearance. Preserve the exact
   HudPixelFont tile and1.6 scale so hiding or shrinking text cannot pass.
   Run native RED before touching production; this RED proves inadequate
   leading under the new acceptance criterion, not historical overlap.
2. Add the same-baseline fact check: a populated later fact row's label, value
   and divider must all translate together. Pair it with the unshifted first
   line. Keep title horizontal/vertical clearance and portrait controls intact.
3. Add boundary cases for20 lines,21 lines, an exact multiple and a final
   partial page. Collect `VisibleLines` through `GoToPage` and compare them
   exactly with the wrapped source: no omissions, repetitions or invented
   blanks. Invalid page requests must preserve the current page.
4. Assert actual transformed last-line ink is above the footer rule, every
   painted row remains inside its opaque panel, and the maximum logical frame
   remains37 rows with all submitted tiles at y>=4. Pair full-page checks with
   a short one-line notice and fact-heavy/multisection document. Coordinate-only
   tile occupancy is insufficient for the translated body.
5. Verify long→short→close clears the old tiles/transforms, first/last-page
   footer labels and pointer cells agree, and header/portrait/approved native
   content remain unchanged. Where a control is added after production, label
   it honestly as a later counter-check, not an independently observed RED.

Existing affected regression fixtures, all read during this audit:

- `ExamineLedgerTypographyTests`: measured glyph ink and title constraints.
- `ExamineLedgerUITests`: section spacing, footer pointer routing, frame/short
  notice and long→short→close cleanup.
- `DensityExamineLayoutTests`: authored item census and enlarged descriptions.
- `DensityDocumentPaginationTests`: all13 canonical documents, content
  completeness, page bounds, protected action rows and punctuation mapping.
- `SpreadReaderPagingKeyTests`: real new-input backend press/hold/release;
  derives expected indices from the public capacity instead of hardcoding25.
- `ExamineLedgerGamepadTests`: A/B, shoulders and D-pad pagination without world
  commands. This simulates input; it is not physical Deck acceptance.
- `FlatPixelExaminePortraitLayoutTests`: native integer-scaled16×24 image,
  model fallback, invalid/tight slots.
- `ExamineLedgerInventoryTests` and `ExamineLedgerQueueTests`: current owned
  subject and unchanged level-up/document/queue semantics as bounded regression.

No new test fixture is necessary unless the focused additions become unwieldy.
No `.cs` or `.meta` file is created in this plan-only phase.

## Ordinary acceptance and release bounds

Use the ongoing connected route and the same actual Claspbean reader where
available. Compare unedited1280×800 captures before/after, including a long
reader's final page and return to world/inventory. Measure pixel gaps again;
inspect title/portrait, fact alignment, footer, pagination and world restoration.
Exercise normal keyboard and existing simulated gamepad tests; record physical
input separately. Reading, paging and closing must not advance world time or
consume/change the subject.

This is original CoO layout work within the user's approved Qud-like style;
no Qud-source parity claim, new asset or new imagegen concept is needed for a
spacing correction. A new concept is only warranted if the scope becomes an
actual redesign, which this plan explicitly avoids.

Native tests can establish submitted geometry, complete text, controls and
clearing. Actual captures can establish the sampled visual spacing. Neither
proves comfortable physical Deck use, continuous performance, all six-site
graphics completion or packaged delivery. The currently preserved package is
still the October11 build and remains explicitly pending after continuity.

## Performance and observability

Follow `Docs/PERF-FOUNDATION.md`: preserve cached glyph tiles and explicit
Open/page/input-hint redraw cadence. Use only scalar arithmetic per body line;
no per-frame layout discovery, texture readback, new cache or world scan. Alpha
scanning belongs in tests and this read-only audit, never runtime. The maximum
panel fill remains37 rows and fewer body glyphs are submitted per full page.
Do not infer a measured speedup from that count. Existing document/gameplay
diagnostics remain unchanged; this presentation-only correction adds no new
success/rejection gameplay gate. Native XML and before/after captures provide
the relevant evidence.

## Plan review and continuation

- 🟡 Measured: one-pixel interline gaps in the actual Claspbean paragraph make
  dense text harder to scan. Pending native RED and minimal implementation.
- 🔵 Preserve: accepted colors/font/title/portrait, exact text, subject authority,
  current controls, frozen world state and unrelated popup controllers.
- 🧪 Open: native transformed-ink confirmation, actual proposed spacing,
  full-page footer/fact geometry, ordinary returns and physical device reading.
- ⚪ Deferred: broader reader redesign, unrelated HUD/log typography and new
  whole-scene art. Continue the six-site route instead of expanding this slice.

Independent read-only review by `projection_cold_review` agreed with the
geometry and the1.25/20 tradeoff and supplied fact/footer/content counter-checks.
Neither reviewer ran Unity or modified Assets. No RED/GREEN count is claimed.

**Files changed in this phase:** this new plan only. Update it with real
RED→GREEN, ordinary captures, findings and exact production files in the same
eventual implementation commit, following `CLAUDE.md` §2.3.


## Implementation evidence — 2026-10-25

- First invocation (`reader-leading-red.xml`) accidentally ran the already
  loaded assembly before asset refresh:11 pre-existing cases passed. This is
  retained as an invalid RED attempt, not evidence for the new assertions.
- After explicit refresh and confirmed compiler/test-runner idle, native
  `reader-leading-native-red.xml` at01:55:17–01:55:18UTC ran38 cases:
  **31 passed,7 intended failures,0 skipped**. The measured leading was
  0.0666676 units at both1280×800 and1920×1080; three page-boundary checks
  correctly rejected the old25-line capacity. Production remained unchanged
  until that result was obtained.
- Minimal implementation changes only `AnnouncementUI`:20-line capacity,
  1.25 baseline advance, measured body extent, and matching transform offsets
  for text labels/values/dividers. Header, portrait, body glyph scale and
  gameplay ownership are unchanged.
- The ordinary before image is `sixsite-reader-before.png`, taken at kitchen
 25,14,tick7160 from replanted and now ripe Claspbean13515. Its actual final
  checkpoint is byte-preserved before test isolation cleanup.

- Native GREEN at01:56:55–01:57:01UTC: **108 distinct cases passed,0 failed,
  0 skipped**, across the nine listed fixtures. Includes the38-case RED group;
  do not add those counts. XML and summaries are in `Verification/Live16x24/`.
- Independent source review and delegated control crosscheck found no P1/P2.
  Before/after pixel review measured the same Claspbean paragraph changing
  from1 blank scanline to4–5 between lines. The portrait remains byte-identical
  after its panel-relative translation. See
  [independent review](Verification/Live16x24/reader-leading-independent-review.md).
- Ordinary C Continue reopened the byte-preserved final checkpoint without
  forcing presentation. All ten sampled gameplay fields match the prior
  kitchen return. C/Down/x reopened the same crop; I/Tab/selection/F1 opened
  the actual equipped2699 sword. Right→Left→Right→Enter tested both pages and
  final close; Escape returned from inventory to the world. Five reader/return
  snapshots preserve the same tick7160,42HP, equipment, inventory, introduction
  and sampled objects. This is actual keyboard acceptance. Controller/pointer
  behavior is covered by native tests, not a physical Deck session.
- `sixsite-reader-after.png`, `sixsite-reader-sword-first.png`,
  `sixsite-reader-sword-last.png`, `sixsite-reader-inventory-return.png` and
  `sixsite-reader-world-return.png` are unedited1280×800 actual captures.
  The sword's second page retains all four aligned facts and its close footer.
- Console0errors,2existing Metal warnings,46logs. At02:02:13UTC root stopped
  Play and restored preferences, view, SampleScene and null save override.

## Final self-review and handoff

- 🟡 **Cramped leading corrected:** nonvacuous native RED precedes the runtime
  change; actual paragraph clearance improves with full-size glyphs preserved.
- 🔵 **Controls and content retained:** complete pages, exact facts, opaque panel,
  title/portrait, protected underlying action rows and cleanup all pass.
- 🧪 **Bounded acceptance:** this is not a whole-HUD redesign, every-document
  visual check, physical device test or performance measurement. Current
  October11 package still predates this work.
- ⚪ **Next graphics gap:** the separate decorative-stubble/prepared-bed cue is
  recorded in the six-site journey; do not silently rename historical rows.

Runtime file: `Assets/Scripts/Presentation/UI/AnnouncementUI.cs`.
Tests: existing `ExamineLedgerTypographyTests.cs` and
`DensityDocumentPaginationTests.cs` under `Assets/Tests/EditMode/Presentation/UI`.
No new asset, sprite, cache, gameplay system or `.cs` file was introduced.
Continue with the delivery/coverage plan after exact owned push and safe storage
inspection, preserving the accepted art.

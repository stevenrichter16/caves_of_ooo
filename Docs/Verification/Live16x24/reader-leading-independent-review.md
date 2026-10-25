# Reader leading — independent source and actual-image review

Reviewed 2026-10-25 UTC by `/root/projection_cold_review`, with an independent source/control and four-image countercheck by `/root/projection_cold_review/look_focus_draft_review`. **No actionable P1/P2 found.** This review used saved source, XML/JSON receipts and PNG pixels; neither reviewer operated Unity or executed tests.

## Source and native evidence

The bounded `AnnouncementUI` change preserves the body font and its 1.6 scale, uses 1.25-row baseline spacing for prose, fact labels, values and dividers, and reduces page capacity to 20. A full page remains 37 rows high, occupying overlay rows 4–40; the protected underlying action rows 0–3 remain clear. The footer is not translated and its pointer hit row uses the same computed panel height. Keyboard/gamepad page logic shares the 20-line capacity. Clearing uses the previous logical rectangle before recomputing the new one; translated ink remains inside that rectangle, and reused foreground cells receive complete transforms.

The new tests measure actual glyph alpha bounds and submitted identities/transforms, preventing missing or shrunken text from passing. They also check fact alignment, final-row/footer clearance and complete 20/21/40/41-line pagination. Existing long→short→close, pointer, keyboard, gamepad and inventory/reader controls remain applicable.

- `reader-leading-native-red.xml`: 38 distinct cases, 31 passed, 7 expected failures, 0 skipped; 01:55:17–18Z. The earlier `reader-leading-red.xml` ran a stale 11-case assembly and is **not** accepted RED evidence.
- `reader-leading-green.xml`: 108 distinct cases, 108 passed, 0 failed/skipped; 01:56:55–01:57:01Z. Counts were independently checked from the saved XML; these are the root's native executions, not additional reviewer runs.

## Actual image measurements

All four reviewed PNGs are 1280×800. Read-only Pillow analysis selected exact paragraph ivory RGB `(227,221,195)`, scanning screen columns 163–776 inclusive and grouping consecutive scanlines containing that color. Coordinates below are zero-based and inclusive. No images were edited.

| Matching Claspbean description paragraph | Occupied ivory scanline bands | Fully blank scanlines between its six lines |
| --- | --- | --- |
| `sixsite-reader-before.png` | 362–375,377–390,392–405,407–421,423–436,438–451 | 1, 1, 1, 1, 1 |
| `sixsite-reader-after.png` | 355–368,373–387,392–405,411–424,430–443,449–462 | 4, 4, 5, 5, 5 |

The preceding three-line Current State paragraph similarly changes from 1/1 blank scanlines to 5/5. The prior screenshot was cramped, **not demonstrably overlapping**. Normal paragraph separation is retained. The Claspbean portrait crop `(690,180,735,250)` before is byte-identical to the after crop shifted 31 pixels upward; no portrait recoloring or rescaling was introduced.

Visual inspection confirms the after capture retains the full `Type | Crop` fact, both footer rows, 1/1 indicator and unobscured hotbar. The fullscreen inventory reader in `sixsite-reader-sword-first.png` retains the complete frost-quenched title, diagonal native sword portrait, wrapped hit-effect text, next/back controls and 1/2. `sixsite-reader-sword-last.png` retains all four Type/Material/Composition/Weight facts and aligned separators, clear footer separation, close/back controls and 2/2. Its final ivory fact ends at scanline 651, above the footer separator; no clipping or ghost rows were observed. Ordinary neighboring prose rows in both sword pages have 5–6 blank scanlines at this larger fullscreen viewport scale.

The sword UI receipts independently identify page 0/2 and page 1/2, 20 document lines on each, and the corresponding next/close footers. Before/after Claspbean JSON matches all ten non-timestamp top-level fields: zone, flat mode, player position, HP, tick, drams, knowledge, equipment, inventory and selected objects. Tick remains 7160 and HP 42. This verifies the recorded state snapshot, not every unrecorded world field.

## Bounds

This supports improved spacing in the actual Claspbean and two-page equipped-sword readers, plus the saved native controls. It does not establish physical Steam Deck legibility, all display sizes/documents, pointer or gamepad use in these particular screenshots, or complete graphics acceptance. Titles, portraits and other popup controllers were preserved; no broader typography or gameplay change is claimed.

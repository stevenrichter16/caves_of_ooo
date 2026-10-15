# Examine and announcement dialog concepts

Five imagegen mockups requested on 2026-10-14. **Design choices only; no runtime UI changes.** Created with built-in `image_gen.imagegen`, one independently prompted image per direction, using the existing native gameplay screenshot as the reference. Exact prompts are in `prompts.json`.

| Choice | Preview | Intent / tradeoff |
| --- | --- | --- |
| A — Refined terminal | [A](A-refined-terminal.png) | Familiar centered, single-column reader; easiest shared shell for objects, books and brief notices. |
| B — Specimen card | [B](B-specimen-card.png) | Dedicated object preview improves recognition; omit that compartment for text-only notices. |
| C — Field ledger | [C](C-field-ledger.png) | Warm bronze frame and labeled rows; calm archival presentation with more vertical space. |
| D — World-anchored inspection | [D](D-world-anchored.png) | Narrow panel connected to its subject; preserves more of the map, but long prose wraps more and generic notices need a centered fallback. |
| E — Split reader | [E](E-split-reader.png) | Prose and factual details in separate columns; useful for equipment/mechanics, with the detail column collapsed when absent. |

Recommendation: A as the most adaptable shared reader, or B if object imagery should be prominent. E is strongest when inspecting equipment with several mechanical properties. Choose before implementing.

## Verified current behavior

`InputHandler.OpenWorldExamineReader` opens **AnnouncementUI**, not DialogueUI. The same AnnouncementUI receives level-up messages (`LevelingSystem`), books, comparisons, ability help and various other notices. It currently accepts a single string, uses a 56-column black panel, wraps body text to 52 columns, and paginates long content. The popup is centered in the gameplay column while leaving the sidebar visible.

Actual controls allow A/Enter/Space to advance or close the final page, B/Esc to close, and LB/RB or D-pad left/right to page. The mockups show a one-page sample with A Close, B Back and 1/1. Multi-page designs would replace A Close with A Next and show page-navigation hints. No repair action is being offered by the reader.

The shared hierarchy proposed here is: optional message type/subject, current conditions before flavor, optional factual sections, fixed navigation footer. Empty sections collapse for short notices such as “You advance to level 2!” Future implementation should use explicit optional content fields rather than assuming the first line of every announcement is an object title or inferring mechanical facts from prose. Preserve current queue chaining and return-state behavior.

## Sample content and review

All five images use the existing `RepairLinedWell` description from `Objects.json` and damaged diagnosis/material/quantity from `clay-well-lining` in `Tier1Repairs.json`. Masonry and two fire clay are verified authored values. A damaged example state is illustrative, not a screenshot of a live damaged instance. The well preview is concept art, not a newly installed model. D's world anchor is a proposed presentation affordance.

All five generated images were visually inspected for readable title/body, correct sample text, separation of condition and prose, controller footer, and keeping the sidebar/hotbar unobscured. Imagegen slightly restyles background glyphs; those differences are not proposed HUD changes. Type sizes, contrast, wrapping, page behavior and object-preview rendering still require implementation and native/Deck validation. No gameplay or performance claims are made from these images.

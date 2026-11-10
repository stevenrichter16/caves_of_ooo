# Dialogue export

Source: main `e5a782a2e8d91354798b917e3aceed36b4c5f42f`, compiled 10 November 2026. The downloadable PDF, Word and plain-text editions are in `Client/wwwroot/downloads`.

The editorial reading order preserves all 80 conversation trees / 672 nodes / 1,465 choices, 35 choice-triggered messages, 450 audited runtime passages and 17 supplementary passages. Four endings are explicitly alternative, with each epilogue before its matching aftermath. Conditions, placeholders and optional/unconfirmed scenarios are labeled. No unwritten lore scenes are fabricated.

`plot-order.json` assigns each fixed node exactly once and stages 60 runtime records around actions. `runtime-dialogue.json` preserves audited runtime speech, choices, quest-status text and narration with source references. The coverage map identifies contemporary visible names despite frozen internal identifiers. The supplementary passages include all four exact ending constants. All fixed/runtime IDs were independently checked in PDF text and DOCX XML; a separate review verified Stillleaf ordering, epilogues and excluded unwritten scenes. Selected PDF pages were rendered and visually inspected.

To regenerate, import the pinned core, install Python packages `reportlab` and `python-docx`, then run `python3 Docs/DialogueExport/generate.py`. DejaVu Serif/Sans fonts are expected under `/usr/share/fonts/truetype/dejavu`. The script asserts complete unique node/runtime coverage and emits integrity hashes. Update the audited input manifests and compilation date when refreshing to a new game revision; this is not a generic C# string extractor.

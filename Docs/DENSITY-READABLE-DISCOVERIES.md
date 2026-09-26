# Readable discoveries — C5

Status: core, sources and pagination implemented; native verification in progress, 26 September 2026.
Parent: `DENSITY-COMPLETION-PLAN.md`; baseline `50ef23d2`.

## Contract

The thirteen existing canonical Codex artifacts become readable copies, with
full text, attribution and real sources. Read is available in world and inventory
menus, non-consuming, repeatable and free of invented knowledge/quest rewards.
An inaccessible or missing document refuses cleanly. Long text is paginated
within the existing modal viewport; no page or final paragraph may be clipped.

## Verification sweep

| Premise | Source correction | Decision |
|---|---|---|
| A generic book reader exists | `GrimoirePart` grants a skill/property; it does not serve full repeatable lore reading | Separate read-only `ReadableDocumentPart`, preserve grimoire semantics |
| The modal can display a Codex artifact intact | `AnnouncementUI` wraps all text but has no page/scroll bound | Add tested pagination for long messages; keep short-message behavior |
| Every Codex artifact is absent | Plaque-wall entries already exist as scenery | Preserve them; copies are additional discoverable texts |
| The prose needs new authorship | `Lore/Codex/*.md` contains 13 canonical in-world artifacts | Import existing wording, normalize Markdown presentation only; record source hashes |
| Any action event proves accessibility | Generic inventory event command does not itself verify carried membership | Read checks carried/equipped ownership or actual nearby zone membership at execution |
| A pool proves discovery | Loot can be disconnected | Tests begin at live library/tomb/shop sources and generated containers |

## Design and sources

Use a cached Resource catalog keyed by stable document ID; blueprints carry the
ID and existing item/commerce/render semantics. Runtime reading never reads the
repository Lore directory. A tracked import receipt binds resource text to the
canonical source hash; no protected answer is added. Books describe themselves
as copies/transcripts, not duplicated unique original artifacts.

Library/bookshelf pools contain culturally appropriate collected accounts;
oaths, contracts and pamphlets also reach scribes/travellers. Plaque records and
ward songs reach tomb/undertaker sources; the grove sign reaches the Choir;
the condition report stays in the sealed-vault source. Loot changes are merged
after their own RED tests through the loot owner. Natural Choir containers,
death loot, unique artifacts, existing grimoires and all old rows are preserved.

Reading returns full title/body as one queued announcement. Pagination belongs
to the presentation layer; the data service does not truncate canonical text.
Success/refusal diagnostics include document ID/reason, with actor/target in
their standard top-level fields. There is no read-once flag to block rereading.

## Tests and acceptance

Core RED/GREEN: all 13 factory instances, live source reachability, canonical
content integrity, repeat reads, stack/HP/energy/property preservation, adjacent
world and carried/equipped reads. Counter-cases: detached/remote/foreign items,
removed source/actor, unknown/empty document, invalid catalog and unrelated
commands. Save/restore the document ID and retain read access.

Dedicated adversarial suite covers malformed catalog input, duplicate IDs,
Unicode/long lines, source/actor ownership changes, same-blueprint identities,
stacked books, suppressed diagnostics and missing actor/inventory/zone.

Native RED/GREEN: popup tiles stay inside 80×45, complete line sequence across
pages, first/last boundaries, forward/back/escape, short-message compatibility,
and close/reopen cleanup. A real found book must be taken and read via keyboard.
Staged text proves UI only; source census and natural acquisition are separate.

## Status / self-review

- Core compile RED recorded against 50ef23d2: missing catalog/part, zero executed
  cases. Implemented read-only catalog/part and thirteen surgical book additions;
  **22/22** core cases passed in the isolated runner. Source RED then recorded 13 missing-source failures + 1 canonical-prose control; the combined core/source run is **36/36 GREEN**. Thirteen authored additions preserve every previous loot row. Pick-one sources use explicit weights (library 100, bookshelf copies 80 each, sealed report 150); independent shop copies use 70–100% and the tomb copy 35%. These are reading copies, never duplicated unique originals.
- ⚪ This is CoO lore presentation, not a Qud identification/history port.
- ⚪ Source text remains testimony; the Mystery Ledger is unchanged.

- Native UI RED: 15/15 document pagination failures (viewport overflow and absent navigation), preserved with the combined 31-case everyday/document receipt. Initially implemented 37 text lines per page, bounded previous/next, Enter/Space/click advance, Escape close, and complete redraw cleanup.
- Independent adversarial review: 32 additional cases; two physical-footprint reach bugs were RED and fixed using SpatialQuery distance while retaining actual zone membership. Combined review/core **54/54 GREEN**. Native NUnit lacks NonParallelizable; removed that fixture-only attribute after the native compile check.
- Source pairs: 01/library; 02/drifter; 03/bookshelf T2; 04/scribe; 05/undertaker; 06/merchant; 07/curator; 08/bookshelf T1; 09/tomb; 10/bookshelf T2; 11/Choir trader; 12/elder; 13/sealed vault. Each tested at actual stock creation with weight/chance-zero counter-control. No new book rows enter natural containers or death loot.

- Visual review confirmed long-page footer conflict with inventory controls and unmapped typographic punctuation. Seven new native checks recorded **6 RED + 1 ASCII control**. Reduced pages to 33 lines to leave the inventory action rows clear and reused the existing Cp437.Map at drawing time; catalog/VisibleLines preserve canonical text unchanged. **54/54 native reader tests pass**, including all22 pagination/glyph tests and32 adversarial cases.
- Native acquisition attempts are retained. First rejected the test's assumption that the configured spawn was Morrowfast (scene starts at2.6.0); route now requests the actual3.6.0 source. Second revealed test assumptions about cardinal standing space and one tick per player action; diagonal contact is legitimate and an ordinary action consumes1000energy over10clockticks. Third passed13behavior checks but lacked the planned seventh capture; final capture run adds explicit backward-page evidence. These were fixture corrections, not changes to source prices, stats or NPC schedules.

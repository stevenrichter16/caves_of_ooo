# Cold review — first-hour combat/source/save integration

Reviewed by `combat_density`, independently of root’s source/native gate. Exact current hashes and scoped reads are in `cold-review-combat.json`. No Assets, Unity, user saves or staging were touched.

## Important finding

**P2 — Pair placement accepted source authority changed by creation callbacks.** `SpreadRareEncounterBuilder.TryPlace` checked current map/selection at entry, then created both actors. An actual existing `ObjectCreated` callback could change the selected address to Beating or add a POI, yet the pair committed. The viper branch already rechecked its own current authority. This is a concrete generation-contract violation; no ordinary infinite-farming or security exploit is claimed.

The private matched regression produced **2 failures / 2 unchanged positive controls** on current production. The minimal candidate rechecks the same pair’s current manager/factory, exact frozen pair address, existing eligibility and absence of an already claimed source after construction, between placement and at commit. Existing rollback removes only a candidate still owned by this zone. It changes no selection, E2 branch, data, RNG, save field or success payload. **92/92 focused pair/viper tests pass** privately; runtime and EditMode reference compilation both have zero errors. Root reviewed and published the exact two-file candidate; actual Unity confirmation remains pending in the final integration gate. Raw paired XML and exact two-path candidate are under `PairAuthority/`.

## Bounded Q1–Q4 result

- **Symmetry:** the pair/viper postcreation mismatch above was the only important asymmetry found. Ambush’s saved latch and pending wake/HP baseline match the public-field serializer and constructor-bypassing load path.
- **Cross-feature contracts:** initialized-empty/missing legacy metadata does not reroll or backfill; family-specific viper admission preserves the pair. A placement refusal retains the ordinary population group. Actual cap/sword/cudgel references, body/Physics links and cached save identities are covered.
- **Counterchecks:** reviewed repeated awake/asleep/pending and explicit Rearm saves, brainless initialization, real native legacy bytes, optional overlap/current-map/route/refusal, unchanged population RNG, disarmed/blocked reach, and actual death/drop/pickup/save. Added only four grounded callback cases; no broad framework or gameplay changes.
- **Documentation:** Lunge is a two-cell attack with no movement. The pair retains actual T1 gear, and latchcoil retains HP8/natural bite/one gland at75%. No automatic enhancement, global drop rate, new faction or hidden lore layer appears. Administrative unload regeneration stays explicitly outside ordinary cached return/save persistence.

The complete parsed `Objects.json` delta against HEAD contains **only three added objects** (`SpreadHurdleCutter`, `SpreadDitchMate`, `SpreadLatchcoil`), with **zero changed or removed existing declarations**. Existing GlassScorpion/plain Shambler/Choir/BitLocker decisions are not rewritten by that data patch.

No additional substantial regression or ownership/save defect was found in this bounded read. This is source review, not a new whole-project verification result. Existing actual native evidence is ambush/save127, pair+optional source88, and staged M4 native13. M4 has zero observed tactic rows; its staged success is not natural discovery or a native Lunge claim. The separate E2 keyboard witness remains its own gate.

Legacy private pending-wake/last-HP values were never serialized and remain unrecoverable. The current tests deliberately pin that limitation rather than inventing old state. Exact hashes exclude current logs and mutable native reports.

## Subsequent full-suite findings

The later unfiltered native run exposed a null-manager dereference in the new `SaveSystem.RareEncounters` load assignment, missed by this initial read. `WorldEntityTests` legitimately round-trip managerless synthetic sessions. The bounded peer follow-up agrees with guarding only that assignment: plan authority belongs to an actual manager, and existing `World` metadata needs no separate hydration. Standalone owns that repair and its matched tests; root owns publication/native validation.

The same run exposed one stale equipment allowlist. Adding only the two exact rare kit rows to `GameAuditEntityEquipmentContentTests.Kits` preserves the original exact-set and no-Loadout controls while automatically adding real gear/count/body/Physics tests. Private baseline91 had90PASS/1RED naming exactly those two extras; candidate93/93GREEN and reference compilation0, with independent peer clearance. Exact one-file candidate and raw evidence are in `EquipmentPin/`. No production loot or source data is changed.

## Subsequent native and peer confirmation

The pair callback repair passed all92 current source tests in actual Unity within
the336-case save/equipment/source selection. The independent render/readability
peer also confirmed exact pair/map/factory checks and owned rollback, with four
nonvacuous callback cases and no optional-source/payload change. The same native
selection closes all34 nullable-save and the stale equipment-pin failures. The
reader correction passes a separate64-case native selection. Earlier pending
statements above describe the review's original checkpoint, not current status.

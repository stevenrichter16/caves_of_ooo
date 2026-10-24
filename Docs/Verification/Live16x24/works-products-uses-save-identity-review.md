# Saved factory identity correction — independent review

2026-10-24. Implementation author: `/root/projection_cold_review`.
Independent reviewer: `/root/projection_cold_review/look_focus_draft_review`.
Root owns Unity/native execution. The reviewer used source reads only, with no edits, compilation or tests.

Reviewed EntityFactory.ReserveLoadedIdentity and saturated allocation, SaveReader.RunLoadHooks insertion, the12-case EntityFactorySaveIdentityTests fixture and Docs/SAVE-ENTITY-IDENTITY-RESTORE.md.

Result: **no material finding**. Standalone graphs finish their final check, and sessions finish footer plus plan validation, before reservation. The whole decoded token graph is reserved before the first hook; the preceding plan restorers do not allocate factory entities. Lower IDs cannot rewind the factory. Saved opaque/numeric strings remain unchanged. Int32 saturation uses the already supported GUID-N identity form instead of wrapping or reissuing the maximum.

The fixture source review checked the real repro sequence: historical carrot2, fresh factory plant1, new sprig2 before the fix; the test then asserts actual inventory-action discovery, two-unit preparation and spent-source refusal. Nested inventory/container/equipment, inactive session owners, borrowed higher factory, corrupt final checks and opaque IDs provide separate counters. HotbarSaveFixture restores SeedPart.Factory/SettlementRuntime, and the action fixture explicitly restores HarvestablePart.Factory.

Root's actual RED at16:34:44–45 UTC:12total,6passed,6failed,0skipped; expected failures cover numeric high-water, nested-before-hook, inactive-zone stock, two exhaustion cases and the missing real action row. First zero-case discovery run is not RED. Root's actual focused GREEN at16:36:32 UTC:12passed,0failed,0skipped. Receipts are Docs/Verification/Live16x24/works-products-uses-save-identity-{red,green}.xml.

Limits: affected regression and ordinary earlier-checkpoint replay were still pending at this review. This prevents new collisions with decoded saved numeric owners; it does not rewrite already-colliding saves, reserve identifiers never present in the decoded graph, or coordinate arbitrary unrelated factories. No preparation/custody permission was relaxed, and no save format changed.

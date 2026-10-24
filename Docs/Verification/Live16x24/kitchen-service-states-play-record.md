# Ordinary kitchen state-art replay — 2026-10-24

Seed65, real GameBootstrap/default flat presentation; token
`ba49bf5d27bc4239b0b4c6f3a9d1c8d5`, game
`c757c38ed9c4452583230acac209cb85`. Root alone operated Unity.
The repaired checkpoint was earned in the earlier ordinary grain/cellar/repair
route, not authored by a debug helper. Isolated copy only; user saves preserved.

1. Continue with C. At51,3/tick5110, actual repaired pan7208 is Idle; player50
   drams/Cook100, actual recipe inventory. `idle` captures/receipts.
2. C,Down,B commissions via the real action menu; F5 saves. At5120 player48,
   Cook102, escrow owns2 Emberwheat+1pulp. Submitted pan is serviceframe0;
   pickup is unchanged neutralframe7. `working` world/job/tile evidence.
3. Temporarily set camera view multiplier .416666657 for the native2× comparison
   `working-2x`; gameplay untouched. F6 normal reload restores default1.2 and the
   same recorded Working fields at5120. `working-reloaded` proves settled art
   before any movement or time action.
4. Eleven ordinary period waits reach5230, the exact120tick due time from5110.
   Input tray empties; one FieldMeal9249 occupies board7210 with commission
   station7208/customer2696/ordinal1. Pan becomes neutral; board serviceframe1.
   `ready` world/job/submitted-tile evidence.
5. F5/F6 Ready; C,Down,X inspects pan. Actual current-state prose says one parcel
   is ready; neutral pan portrait is correct. `ready-reader` capture.
6. Escape,Right,Right to53,3/tick5250. C,Down,X shows actual ready board and
   matching serviceframe1 portrait. `board-reader`; Escape then `ready-reloaded`
   receipt/capture. This latter receipt follows two movement turns, not a
   same-tick whole-world reload equality claim.
7. C,Down,O opens the one actual parcel (`pickup`); Return takes it. Tick5260:
   jobIdle, trays empty, same meal9249 carried by2696. `collected` frame and
   submitted tiles show neutral pan and board. No stale meal picture remains.
8. F5/F6, no movement. Exact named recorded fields match; same meal in inventory.
   Restore camera1.2, capture `collected-reloaded`. All11 saved PNGs1280×800.
9. Stop Play. Restore owned view/seed/start scene/preferences/save isolation.
   `kitchen-service-states-cleanup.json`: false playing/isolation/artPending/
   viewBorrowed, null override. Final console0errors and2known Metal memoryless
   depth warnings. Earlier compile-cycle MCP websocket reconnect was transient;
   no gameplay exception remained in final Play console.

## Measurement and bounds

85.12s profile starts before commissioning and stops after Ready: includes save,
Working reload,11waits, idle/UI/tool/capture time; collection is outside window.
64360 recorder samples, no wrap. ZoneRenderer.LateUpdate mean0.095ms,
p990.175ms,max29.699ms. MainThread mean1.319ms,p992.217ms,max959.029ms;
large maximum includes save/load/tool sampling and is not isolated to new art.
DrawCalls max43; Batches max42. This is not a baseline comparison, GPU cost,
packaged frame rate, zero-GC proof or physical Steam Deck performance test.

`kitchen-service-states-summarize.py` derives14 assertions,11capture dimensions/
hashes and481 distinct native passes from raw receipts/XML. Five native tests
explicitly skip because even a retained4096byte allocation reports0 on this
Unity runtime; independent quiet-frame checks still run. Native tests cover
forged/foreign/stale/hidden/covered/partial-transfer/worker-death states that this
ordinary replay did not physically exercise. No claim of complete G2/G3,
all crop/forge states, continuous motion, six-site journey or release acceptance.
The earlier pre-art run establishes actual meal consumption at fullHP only.

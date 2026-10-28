# Buckler: bounded Editor acceptance, 2026-10-28

One original native16×24 Buckler body replaces its glyph in ordinary ground and authorized carried/equipped readers. Root verified actual Sill gameplay; this is not a complete equipment family, area polish or packaged release. Current Mac/Linux packages43a151ddb exclude this change and portable seeds.

## Reproduce

Use the established `LiveFlatPixelGreyboxTools.Launch()` isolation, copy the preserved fixture Quick pair into that run's `FlatPixelGreyboxMarker` directory, then ordinary C/Continue. Never load these fixtures into normal user saves. Root ended both owned runs through `Finish()`; final Play=false, SaveRootOverride=null, scene clean.

- Baseline `Fixtures/buckler-ground167`: earned Sill143, normal unequip/drop/step aside; Player2695 at61,3,HP38,turn1550; Buckler2700 at60,3. Four accepted before images preserve equipped reader/actions and ground/reader. Two Dagger trial images are explicitly excluded.
- After01/02: matched ground and Look/Examine show a compact round rim/raised boss at the same cell and in the portrait. Floor, geometry and other approved bodies are unchanged.
- Ordinary adjacent Look→g pickup automatically equipped the empty right hand. After03 is therefore named **auto-equipped-reader**, not carried. After04 confirms Unequip and displayed AV1/DV8.
- Ordinary Unequip→F1 produces after05 carried comparison: empty right hand, candidate AV+1/DV+1. After06 shows Equip(auto)/Equip(manual), displayed AV0/DV7. `buckler-carried-state.json` records2700 in loose inventory, no equipped Body slot.
- Ordinary Equip(auto)→F1 produces after07 and `buckler-reequipped-state.json`: same2700 in an owned Hand, removed from loose inventory, owned ArmorAV1/DV1. No attachment body is added to the player.
- Escape to world/F5 produces after08 and `Fixtures/buckler-reequipped168`: HP38,turn1560. Quick.sav.gz SHA256 `467fc5ea181d27599ec4e7e3ff890588cbd9727a15930c7943550e7e3ef3df39`; Quick.json `35ad6b7ba130fa57b1b7e005409cecd80228bb6d12ada699b48d63909239d7e9`.
- Finish, fresh isolated Launch/C/Continue168 gives after09 world, after10 equipment and after11 reader. Same2700/player/HP/custody; old60,3 contains Grass6221 only. Saved/restarted world rectangle(0,40)-(940,640) differs in **0/564000pixels**. This excludes the UI/log; it is not a full-frame equality claim.

## Corrections, provenance and limits

`buckler-trial-inventory-only-receipt.json` searched only InventoryPart.Objects after automatic equip, incorrectly yielding no item; its raw GetStatValue("AV"/"DV") fields are not displayed combat totals. It is retained as excluded exploration. Corrected read-only receipts union loose inventory with actual Body equipped owners; UI captures establish displayed values. A first receipt-helper compile attempt used the wrong Body namespace; the saved source correction is Core.Body, not Core.Anatomy.Body. Neither helper changed gameplay.

The imagegen scene is a labeled design study, not a gameplay screenshot: it altered UI/text and supplies no acceptance evidence. The native body is original editable Blender unit-pixel geometry,144quads/10groups; v1's narrow boss and mislabeled contact were corrected in v2. Two rebuilt pixel/cell outputs match; saved blend bytes differ, while both reopened geometries match. The rejected source remains preserved. Root and the runtime author independently inspected the art; the art author reviewed the other author's runtime/tests, with authorship disclosed.

Native RED8 preceded implementation. Initial combined156 run had one incorrect expected-lighting assertion; only that test premise was corrected against the unchanged painter. Final job3a51fe6a6fe247468bad053ce146ea23 passes156/156:8Buckler+48IncidentalEquipment+23SharedSidearms+52GlobalPresentation+5PortableSeed+20ExpeditionEssentials. No overlapping historical totals are added. All receipts and exact reviewed bytes are in the plan/review.

`buckler-editor-profile.json` is a154.457s Editor interval with37,515frames and wrapped last4096 samples, including readers/idle/tool time. Renderer mean0.0070ms,p990.01825ms;24drawcalls/batches,5661triangles in those retained samples. Main-thread sampling includes a2.292s tool/UI stall. This is a bounded observation, not a controlled speedup, representative FPS or physical Deck measurement. No new shield combat/block mechanic was authored or claimed exercised.

Root visual assessment: readable round silhouette and raised center, restrained palette, unchanged ground scale and consistent world/portrait identity. Sparse Sill/mire composition, remaining equipment/flora/services, optional animation and all55+5area-polish scopes remain open. Next evidenced coverage opportunity is the two planted legacy crops in earned166, using approved planted/sprout frames; automatic yield replacement must remain mechanically truthful.

Publication audit: exact owned diff check passes excluding Unity's generated buckler.png.meta trailing blank values; importer bytes/hash are preserved rather than cosmetically rewriting accepted metadata. Fetch showed HEAD=origin/main before commit. A redundant rebase attempt declined because the shared tree/index is dirty; no rebase or stash ran and no upstream commits were missing. Exact owned commit/push follows.

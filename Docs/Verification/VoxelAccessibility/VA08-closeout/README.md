# Voxel accessibility verification

VA07: 14544 passing / 14576 total,32 unchanged baseline failures,zero compiler errors. All28 added cases pass:18 real native walking/travel pins,8 navigation-discovery RED-to-GREEN cases,2 null-output controls. VA03’s seven legacy selected-filter errors reproduce without new tests in VA06; the final full-run baseline is unchanged.

VA05 audits a copied saved world and fresh same-seed control:37 destinations each,17 matching town censuses,29 map descents plus2 edge transfers all successful. The original24 cached ground graphs contain zero towns. The user’s saved16.6position is directly east of Sumphold. The original save bytes still match the captured hash at closeout; no save rewrite occurred.

2,113 source/content/metadata/assembly inputs remain identical between original and isolated project. New GUIDs have no collision. Independent reviews caught and corrected audit pilot-catalog omission and test-global/loot cleanup. New tests walk the native movement path; batch transfers use isolated in-memory probe actors and do not prove walking from every local cell in every seed. No physical keyboard or current live-frame claim.

The standalone world-map.html was inspected in the browser and Sumphold selection verified. Remaining conversion scope lives in Docs/VOXEL-CONVERSION-BACKLOG.md. Native census recipe failures are mappings, not an exhaustive missing-model inventory. Original3.7pilot misses were measurement errors and are absent from VA05.

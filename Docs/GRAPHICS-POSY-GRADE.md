# Posy native palette comparison

Posy's native woven walls, stone interiors, grass, actors and river now retain
their authored hues. The exact current Spread/Village/Posy surface with an empty
profile uses white color filter, contrast5, saturation0, vignette0. This replaces
the extra(.85,1.05,.90)green filter and saturation12 only while native mode is active.
Legacy rendering, neighboring regions and invalid current authority retain
canonical grading. Water and hazard source colors/selection remain unchanged;
the global grade changes their displayed world hues too.

The initial wall-memory investigation is explicitly deferred: Cell/SaveCell
does not store a durable last-seen VineWall connection mask. The current task
requires trustworthy previously disclosed appearance, so no connected or
isolated remembered frame was invented. Existing fallback/memory policy stays.
See [decision](Verification/GraphicsExecution/PosyGrade/memory-decision.md).

Native17 positive-premise RED failures+3controls precede189/189 GREEN across
BiomeColorPatcherTravelTests, including20new Posy cases. Independent source and
seven-pair actual visual review both report0P1/P2. No art, layout, population,
shadow, save-schema or mechanics changes.

Actual GameBootstrap earned3180HP40seed704860946at39,11 supplied matched center,
doorway29,15,interior29,17,bank39,18 and floor/grass/water readers. Down4Left10Down2
entered the real open doorway; Up2Right10Down3reached the riverside. Grass reader
at39,18was correctly identified, thenDown1anddirectionDownreadWaterPuddle39,20.
No effectful action/injection/invincibility/healing/damage/loot/quest acceptance.
The earned actor's Soaked condition declined normally; Dusk→Dark is matched.

Ordinary world travel5.9→4.9→5.9 restored canonical Spread grading outside and
white5/0/0 on return. A TabopenedPause was closed before world travel; it is not
travel evidence. F5turn3660HP40at39,11 and fresh Continue retain the palette.
All2435 terrain/wall IDs/positions and TileState match; two campfire light
intensities and one VillageChild's two cooccupancy cells differ at matched
interior. Presave/reload differs only in those two ambient light intensities.
All seven pair sidebars/hotbars and three reader panels are exact. Reload world
and hotbar are pixel-identical;17423sidebar pixels change with save/load logs.

After-only111.136s Editor2319x1283 profile retains74796unwrapped samples;
renderer p95 .087417ms/max97.305125ms,Mainmax1915.301875ms. This includes Editor
and tool scheduling; no causal speedup, packageFPS or R11closure claim.

83foreign tracked changes, normal saves and gameplay preferences are preserved;
four session/test metadata changes disclosed. Root stopped Play/rootnull/clean.
Current c9ca0d2be Mac/Linux precede Lampwell sundew and Posy grade. Next: batch
both accepted slices into current Mac/Linux, actual Mac acceptance and honest
Linux-unexecuted limits. Deck work remains deferred; hourly task ACTIVE.
Whole-area polish/V0–V7 remain open, including remembered vine walls and sparse
outdoor composition. Traversal/base proposals remain design-only.

Evidence: [receipt](Verification/GraphicsExecution/PosyGrade/receipt.json),
[before](Verification/GraphicsExecution/PosyGrade/before-center.png),
[after](Verification/GraphicsExecution/PosyGrade/after-center.png).

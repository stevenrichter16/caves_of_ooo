# Bounded graphics findings

Read-only synthesis of fresh October17 captures at source5bb8e249d. This is evidence for review, not a fix plan. No original image has been retouched and no production behavior has been changed.

## Directly visible concerns with source support

1. **Journal punctuation and the current-stage marker are missing.** JRN-01/02 show the accepted levy objective as `(0 3)`, page numbering as`1 1`, and no`>` before the yellow current stage. JRN-03 loses the slash in its page/footer text too. `QuestLogUI.cs:70,212,269` emits those characters through `CP437TilesetGenerator.GetTextTile`; the atlas allocates their tiles but its painted font dictionary omits`>` and`/`. This is now a reproduced visible defect at both1280×800 and1280×720. The much narrower, widely spaced journal lettering also differs substantially from the new reader/skill interfaces; that readability comparison is visual, not a behavioral claim.

2. **The skill discipline rail disappears on one keyboard reopen.** SKL-01 has a full left discipline list; SKL-02 retains the frame, heading, overview and details but loses all discipline names and the selected highlight after closing and reopening. The passing remembered-selection check only covers model selection. The exact render/lifecycle cause and whether another input restores the rail remain unknown. The separate route failure`all_disciplines_reachable` is explained by a fixed13-iteration audit against14 roots; it is not proof that the game's14th discipline is unreachable.

3. **Equipped-item row loses the closing bracket.** ITEM-01/02 visibly end the equipped marker at`[E`. `InventoryUI.cs:1840` appends` [E]`; the49-column row starts atcolumn32, while DrawText stops beforecolumn80. Its last character is discarded. This is supported as a row-layout overflow rather than a cropped PNG.

4. **Several popup footers have weak contrast.** BASE-02/03/04, UI-01/02 and UI-03..05 put dark instructions outside the opaque panel on detailed terrain. The text exists, but it is noticeably harder to read than panel content. StartingBuildMenuUI, PauseMenuUI and DialogueUI explicitly place those hints beyond the bordered content. No input failure is inferred.

5. **Some dialogue punctuation falls back to a question mark.** UI-03 displays a replacement character after Padok's “at your service,” where `CandyTax_Quest.json:8` contains an em dash. This is distinct from the journal's transparent cached glyphs; it does not change the actual quest choices.

## Visible differences whose causes or significance are narrower

6. **Regions currently look like different generations of art.** The Spread/Sodden samples have richer ground detail, dimensional silhouettes and more shaded surfaces than GRO/STU/BEA/OVE. Source confirms Spread/Sodden-specific palette, terrain and actor refinements and lighting profiles in SpawnRing3DPresenter, SpreadCommonTerrain and related libraries. All six biomes have native3D support; dominant terrain in the flatter frames has explicit model recipes. Different real day bands also contribute. Neither “the four regions are entirely2D” nor “every owner is voxel-modeled” follows from these screenshots. Per-owner fallback and missing-mesh counts were not recorded for those exact frames. Overwrit's sampled sparse ground is intentional content, not evidence of missing decoration.

7. **Low world objects and roles are often hard to identify at normal scale.** The exact dropped dagger in ITEM-05 is much harder to recognize than its ITEM-03 portrait. Its re-equipped blade is not separately clear in ITEM-06, although ownership is confirmed. At Drowned Ledger, plank routes and broad camp boundaries read more clearly than preserved bodies, fine records, stakes and small worker roles. This is a scale/recognition observation, not proof of missing models or a failed settlement layout. The route was guided, so unaided place recognition remains untested.

8. **Named NPC Examine content can be thin.** NPC-01 has a readable dressed humanoid portrait, Flesh material and Sitting condition, but description only says “You see Clerk Padok.” The sidebar calls Sitting “Afflicted,” which reads oddly for normal occupation. This is one named clerk sample; it does not establish that all NPC descriptions are shallow.

9. **Input guides are connection-sensitive in some contexts.** BASE-01 shows keyboard and controller start instructions with no controller connected; BootMenuController logs both unconditionally. HUD-03 and SKL-02 show controller hints after keyboard actions because the virtual pad remains connected, matching NativeGamepadInput's connection-based policy. THR-03/04 provide actual keyboard/controller ready-state guidance with the correct next-confirm instruction. These captures do not test last-used switching everywhere or physical Deck comfort.

10. **Combat information can obscure the immediate object.** WRK-03/04's red Bleeding/damage text is legible but overlaps the small player/gate interaction area. THR-03/04 visibly clip some prepared-benefit prose at the Focus/log boundary, while the dedicated lower overlay clearly says ready to throw and names the confirmation controls. These are information-layout observations, not evidence that the actions failed.

## Positive evidence worth retaining

- The selected item and creature readers fit their material/condition text, portraits and controls; the dagger's whole page remains visible at720p.
- UI-04/05's three settled wilderness report descriptions match their three memory choices at both resolutions. Settlement topics are not mixed into that captured branch.
- Door controls/logs track the actual same gate's open/closed state under live danger. The cave crop route proves a real harvest and portable item light; no decorative-only state was invented.
- Throwing explicitly distinguishes body/ground mode and names first contact/rest location. Measured Throw ready overlays explain confirmation without reopening inventory.
- Skill overview cards, owned ranks, missing requirements and detail pagination are readable in the successful SKL frames. Retaining SKL-02 prevents those successes from hiding the reopen defect.

## Questions this package cannot settle

Poison/gas contrast across backgrounds, actual spell-impact readability, broadly distinct enemy silhouettes in a normal encounter, motion quality, flicker, physical controls, timing and frame pacing remain unassessed. Neither a route's passing logical checks nor its recorded failure substitutes for visible evidence of those properties. The liquid-route stop and partial combat/work/biome reports remain attached; no fixes were made during capture.

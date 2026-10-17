# Full-screen skills route — fresh 1280×800 review

Fresh run `1520868bb67143b18257bd3574f2c661`, written 17 October 2026 at approximately 13:23:26–13:23:33 UTC, copied unchanged from `Docs/Verification/FullscreenSkills/Native/<runId>` to `evidence/routes/skills800`. The package contains the raw report and all 15 route PNGs. Only the three selected originals below were opened for this focused visual review; all three are complete 1280×800 PNGs. No source, scene, model or UI was changed.

## Setup and actual modifications

The existing `ReferenceGladeNativeBatch.LaunchSkillsAt(800)` route starts an isolated seed-64 game in **ReferenceGlade.unity**, not SampleScene. It uses its normal player and inventory. The native player first verifies ordinary HP 40, non-dev state, no BitLocker and full reveal off (`ReferenceGladeNativePlayer.cs:80–86`). This skills branch occurs before the general route's optional composition/full-reveal work.

`ReferenceGladeNativePlayer.Skills.cs:18` explicitly sets **SP BaseValue to 10**. It adds a virtual Input System Gamepad (line 20); the containing route also adds a virtual keyboard and alters background input settings temporarily. Purchases themselves use queued native controller inputs through Character > Skills and actual purchase review. The route buys Acrobatics, Vault, the Long Blades root, Riposte ranks 1–3, Deflect ranks 1–3 and En Garde, consuming all 10 staged points. Vault is assigned to slot ten. These are real UI purchases from a staged budget, not evidence that an ordinary fresh player naturally earned this build. There is no HP, inventory, direct skill-part or rank grant in the skills branch. No world/combat turn is spent; the report verifies unchanged tick, energy and position.

The virtual pad stays connected until the routine's `finally` at line 100. Keyboard X reopening at lines 96–97 therefore remains a connected-pad state. `SkillsScreenUI.View.cs:77` chooses hints using `NativeGamepadInput.IsConnected`; this is not a valid no-controller hint comparison.

## Raw outcome — do not report a clean pass

The report records 23 cases, one failure, zero unexpected errors, no fatal exception, `complete=false` and `publicRouteComplete=false`. The failing case is `all_disciplines_reachable`.

Source explanation: the loop at `ReferenceGladeNativePlayer.Skills.cs:62` visits exactly 13 disciplines, then line 78 compares the visited count against the full model count. There are now **14 authored nonhidden discipline roots**, including Throwing. This particular check is stale and cannot establish an actual inability to reach the last discipline. The route continued through purchases and reopen. Preserve the failure and its limitation; do not fix the audit during this capture assignment.

## SKL-01 — Ranked Long Blades overview, synthetic controller

Image: `evidence/routes/skills800/14-ranked-long-blades-overview.png`.

Question: Does the prerequisite overview connect owned ranks, remaining options and the selected skill's actual effects?

Caption: The Long Blades overview shows owned Deflect and Riposte at rank 3/3, owned En Garde, and other abilities needing SP. Riposte's first detail page explains its counter chance and the need for a held long blade/parry source. Zero SP reflects the route's completed purchases.

Visible findings: three clear columns separate disciplines, prerequisite cards and detail. The selected Riposte card and Long Blades discipline use gold; owned ranks use green; unavailable points use muted red. Detail text continues onto page 2, explicitly indicated by `DETAILS 1 / 2`; the final sentence ending “Deflect or” is a page boundary, not silently lost content. The visible current defense warning says a held long blade is needed, which is honest for this player's actual starter gear. No text overlaps are apparent at 1280×800. Thirteen discipline names are visible on this rail page; Throwing's later page is not shown and has no obvious page counter here. The lower footer uses controller hints. Useful paired comparison: SKL-02.

## SKL-02 — Keyboard reopen with controller still connected

Image: `evidence/routes/skills800/09-reopened-keyboard.png`.

Question: Does closing and reopening preserve a complete usable browser, not only selection state?

Caption: The route closes the same overview with controller B and reopens it using keyboard X. Long Blades and Riposte remain selected, but the entire discipline-name list is visually absent while its frame, heading and footer remain.

Visible defect: all discipline entries and the selected discipline highlight visible in SKL-01 are missing. Middle overview cards and right detail still render. The route's passing `keyboard_reopen_remembers_selection` check verifies model selection, not visible rail content, and therefore does not cover this regression. No claim is made yet about whether a subsequent input redraw restores the rail; that was not captured. The vanished purchase notice is expected because `SkillBrowserModel.Open` clears Notice; do not conflate that with the missing rail. Source inspection shows `SkillsScreenUI.Open` rerenders, the model refreshes and the view iterates the discipline list, so the exact render/lifecycle cause remains unproven without further runtime inspection. **Do not replace or omit this frame to hide the defect.**

Input limitation: controller hints remain `[Y]`, `[B]`, `D-pad / LS`, and `LB / RB`, because the synthetic pad is still added. This proves a keyboard reopen operation, not a genuine keyboard-only UI state or physical Deck hardware behavior.

## SKL-03 — Last Pyromancy list page and locked Hearthwarm

Image: `evidence/routes/skills800/07-pyromancy-last-page.png`.

Question: Does a large discipline expose its final ability and explain a locked skill's practical use?

Caption: On Pyromancy's second power-list page, Hearthwarm is selected. Its description explains a three-turn warming aura for thawing, drying soaked ground and coaxing a campfire, with range, targeting, cooldown, point cost and the missing Pyromancy requirement.

Visible findings: all text fits the single detail page; `POWERS 2 / 2` makes the list pagination explicit. The root and locked skills use distinct state text. Already-owned starter Flaming Hands appears among locked learned-tree choices, reflecting the actual starter ability rather than a capture grant. The sidebar shows eight remaining staged SP at this earlier point in the route. The title `HEARTHWARM` and the structured practical description are readable. The bottom “Range: 1 cells” is a minor grammatical issue, not a behavior failure. This is long explanatory text, not a prerequisite-heavy chain; the raw Axe locked-prerequisite and warning screenshots are also retained but were not selected/opened in this three-image sample.

## Evidence limits

These stills demonstrate macOS Editor rendering and queued native Input System behavior. They do not test physical Steam Deck ergonomics, controller latency, performance, legitimate progression to the staged ranks, or combat outcomes. The route report is retained with its failing check. Current-stage quest marker/progress and long/paged dialogue are separate pending coverage tasks handled elsewhere.

# Full-screen skills: Field Manual and Discipline Board

Status: implemented and reviewed. All four milestones and ten polish/design passes are complete. A (Field Manual) is the default; B (Discipline Board) is the optional overview.

## Goal and milestones
1. A read-only grouped browser model and safe purchase/assignment commands.
2. Retained full-screen UI: discipline rail, selected discipline list or board, persistent details, explicit ownership/affordability/requirements. No world/log behind it.
3. Native Deck context: LB/RB changes discipline, arrows/stick select, A reviews/confirms, B backs out, X assigns, Y switches view, right stick pages details. Keyboard and mouse work too. Selection is remembered.
4. EditMode invariant/counter-check sweep, native 1280x800 visual/input scenario, cold review and fixes; update delivery build if successful.

## Verification sweep / corrections
| Source inspected | Finding / design consequence |
|---|---|
| SkillsScreenUI / InputHandler | Existing 60-column popup flattens all entries; D and LT+RT intercept into reader. Replace rendering and intercept; preserve free menu lifecycle. |
| NativeGamepadInput | Menu shoulders alias left/right. Append Skills context to separate shoulders from pane navigation; preserve quarantine and other contexts. |
| SkillPurchaseEligibility / BuySkillAction | Evaluate is read-only; Execute is authoritative and emits PurchaseAttempted. Revalidate on confirmation; never duplicate purchase rules. |
| SkillsScreenStateBuilder / AbilityDetailsBuilder | Raw obfuscated descriptions exist; use safe details builder. Hidden entries need filtering. Group by parent class, not display name. |
| SkillRegistry / authored JSON | 13 disciplines, 100 powers; Pyromancy has 15. Only 3 explicit power dependency edges. Most powers are siblings, not progression tiers. |
| ActivatedAbilitiesPart | Assignment must use the existing source-class identity and slot API; discovery-owned Vault may lack its tree. |

## Design
CoO-original presentation, not a claim of Qud UI parity. A full-screen retained Canvas uses a dark teal background, cream text, ochre selection, green ownership, and text labels alongside colors. B keeps the same rail/details but replaces rows with paginated sibling cards. Lines show only the selected card's actual, visible prerequisites; text lists all prerequisites even when off-page. This preserves readability for the large disciplines without inventing dependencies.

Purchase uses a review panel showing SP before/after; cancel is initially selected. B cancels review before closing the screen. Assignment presents all ten real slots and the occupant that will be replaced; no ability is activated by browsing or assignment. Descriptions are locally paged when too long, with explicit page count. Selection/view survive reopen and refresh within the session. No save format changes.

## Readiness and performance
Green: purchase rules, registry, activated ability ownership, controller sampler and existing native harness patterns. Green: final full-screen layout, overview pagination and focus verified in native captures at 1280x800 and 1280x720. Retained uGUI builds its hierarchy once, updates text/images only after input/state changes, and has no new per-frame registry traversal or UI allocation. Hidden canvas is disabled. Performance claims limited to rebuild/idle assertions; no physical Deck certification.

## Verification / implementation log
Completed: canceled/stale purchases, unknown/hidden data, discovery-only and book-owned skills, real graph edges, selection/pagination, slot replacement, and controller isolation have regression coverage. Native testing includes rendered screenshots and input at 1280x800 and 1280x720. Script-observable UI behavior and inspected captures are verified; physical Deck feel/performance is not.

## Ten post-implementation polish passes (user requested)
Each pass begins with a focused design question, acts on an observed weakness, and retains test or visual evidence.
1. **Typography:** Does it still look like this game? Use the existing CP437 UI atlas in retained meshes; verify punctuation and long words rather than importing an OS font.
2. **Full-screen composition:** Is every pane readable at 1280x800 and 1280x720? Inspect native captures, fix clipped controls and spacing.
3. **Controller focus:** Can every discipline/power be reached without a mouse? Isolate shoulders from pane movement, make focus obvious, quarantine held controls.
4. **Requirement honesty:** Can the player tell why a skill is locked? Show current/required stats, OR alternatives and actual prerequisites; preserve discovery ownership and hidden knowledge.
5. **Overview meaning:** Does the board reveal genuine relationships? Trace actual requirements only; give off-page dependencies an explicit text surface.
6. **Purchase safety:** Is the pending transaction clear and deliberate? Default to cancel, show SP before/after and revalidate on commit.
7. **Assignment clarity:** Does the player understand slot replacement and moving an existing binding? Explain both, reject passive/unowned skills, test duplicate removal and cancellation.
8. **Long content:** Can all authored descriptions, names and large disciplines be read? Page locally; expose clipping and retain complete text in details.
9. **Idle cost and lifecycle:** Does a motionless menu do work or leak state? Retained hierarchy, idle render-count checks, free-turn assertions, reopen/close/reader restoration.
10. **Consistency and final review:** Align colors, actionable hints, keyboard/mouse/controller behavior, run regression sweep and inspect final native captures.

Initial RED: missing browser model and full-screen API/context compile failures recorded in Verification/FullscreenSkills/red.txt. First GREEN: 23/23 native Unity EditMode (4c7392b58a4541b79aaaeeadad2244b0). The first fixture omitted ActivatedAbilitiesPart; fixed the fixture to represent the real player composition, not production ability registration.

Polish review RED (cda32eb9fc354806a1e8baf52d7aec08): 4/8 failed, proving missing CanvasRenderer, unsupported mathematical punctuation, hidden-prerequisite prose leak, and hidden-parent suppression of owned book powers. Fixed all four; 19/19 model/polish/controller tests now pass (7d1b2015ca2645cd98ea6a10418d5bb9). Initial native behavior run 831fbec8201a4a4fbbdc012a6c303877 passed 14 checks, but its screenshots exposed blank text; this run is explicitly not visual acceptance.

Passes 1, 3, 4, 7: added required CanvasRenderer to retained pixel glyphs; normalized minus/degree/comparison text; prevented right-column board navigation wrapping diagonally; disabled irrelevant footer actions and made assignment wording truthful. Hidden names are masked through both browser requirements and shared detail-reader prose; hidden-parent book-owned abilities remain visible under an unknown discipline. Input handles at most one input source per frame and pointer detail paging redraws once.

Passes 2, 5, 8: Native 1280x800 run 97f73d29683047099e6519780131757e passes 14 checks and its text is now visually present/readable. Reviewing the actual overview caught a diagonal root link crossing behind an unrelated card; routed selected root links through the central gutter so they cannot suggest a false intermediate prerequisite. Paging arrows now fit their buttons. Long summary labels end with ellipses while details retain full text; long words and all 71 synthetic detail lines survive pagination. Added an always-visible passive/active, range and cooldown strip so the basics do not depend on the current description page. Owned abilities also show their actual cooldown/binding details.

Pass 6: purchase review now explicitly states Left/Right selection and confirm/cancel controls. Cancel remains the initial choice; selection changes invalidate pending purchases. Counter-check tests cover stale SP, cancellation and duplicate confirmation.

Pass 9: native idle test confirms no menu rebuild while idle; full visit changes no position/tick/energy. Canvas is retained and disabled on close. Early/empty registry, existing book-owned skills, duplicate slot movement and cancellation have dedicated counter-checks.

Pass 10 / cold-eye review: stale assignment details were caught after the broad green sweep; RED 64f4a72c0d0a49538c64ba80eb68f02b showed the old shortcut after moving Vault. Assignment now refreshes cached details immediately. The strengthened final native harness records every visited discipline, exact selected-class preservation, renderer/label-height checks and a real Dismember -> Decapitate overview; it restores any preceding gamepad device.

## In-phase self-review
- Yellow, fixed: native labels invisible without CanvasRenderer; geometry regression plus rendered capture.
- Yellow, fixed: hidden prerequisite names leaked through the older shared reader, and hidden parents suppressed book-owned children. Shared safe naming and unknown parent grouping now cover both directions.
- Yellow, fixed: small page buttons clipped glyphs; explicit label vertical centering.
- Yellow, fixed: Unicode minus/degree/comparison characters lost numerical meaning; normalization retains signs and inclusive comparison.
- Yellow, fixed: straight overview link could imply an intermediate dependency; gutter routing avoids unrelated cards.
- Yellow, fixed: details could retain old shortcut after assignment; selected details now refreshed on commit.
- Blue, fixed: misleading slot wording, inactive actions, inconsistent locked-state color, right-edge diagonal wrap, simultaneous pointer/key processing and duplicate paging redraws.
- Documented: this is an original full-screen presentation preserving the game's existing font and skill semantics, not a new skill progression system. Overview cards page in authored order; selected prerequisite links and text convey dependencies without invented tiers. No respec or save format changes.
- Test limit: native synthetic gamepad and Mac captures verify routing/layout, not physical Steam Deck ergonomics, Steam Input overrides or battery/frame-rate performance.

## Final verification

| Check | Final result / evidence |
|---|---|
| Unity EditMode regression sweep | **188/188 passed**, 0 skipped; 27 new tests across three fixtures. Job `272e9aa9438d4e36a704a685ce3c8be0`; [receipt](Verification/FullscreenSkills/editmode-final.json). Includes existing purchase, live blueprint, state builder, ability readers and native gamepad/adversarial fixtures. |
| Native 1280x800 | **16/16 passed**, 0 unexpected errors, completed and errors finalized; run `c079f69f01a64e3faf2e5ad30f0dd963`. |
| Native 1280x720 | **16/16 passed**, 0 unexpected errors, completed and errors finalized; run `13318ec98e404142a652336eb726baf3`. |
| Visual inspection | Final Field Manual, purchase review, ten-slot assignment, Vault overview, long Pyromancy page/overview and Axe locked dependency inspected. Text is visible, controls fit and real links avoid unrelated cards. Final selected captures and raw reports retained beside this doc. |
| Cold review | Independent read-only control/doc check found only stale completion wording; updated here. All identified production issues fixed before commit. |

Can verify: real bootstrap with native Input System events through Character > Skills; all 13 disciplines reachable; cancellation and one-time SP purchase; selected-class preservation; slot ten assignment; idle rebuild stability; keyboard reopen; unchanged scheduler tick, energy and world position. The test explicitly stages 10 SP for UI transactions, without making a progression claim. The harness restores the prior scene, Game View, saves and prior current gamepad.

Cannot verify: physical Steam Deck feel, Steam Input overrides, Linux rendering/performance on hardware, battery cost or subjective progression balance. This Mac cannot execute the Linux player. The synthetic gamepad is evidence for input routing, not physical Deck certification.

## Implementation contract and design boundaries

```csharp
// Browsing uses read-only eligibility; only explicit confirmation buys.
Model.ReviewPurchase();
Model.ConfirmPurchase(); // BuySkillAction.Execute rechecks live eligibility.
Model.Assign(slotIndex); // Existing slot API; then refresh cached details.
```

| Reference / intended direction | Delivered boundary |
|---|---|
| Qud-like style and legibility | Existing game CP437 atlas, restrained dark teal panels, ochre selection, cream text, explicit green/red ownership/lock labels. Original CoO layout; no claim of exact Qud UI parity. |
| A: whole-screen selector | Persistent discipline rail, powers and details; complete descriptions use local pages. |
| B: optional overview | Paginated sibling cards in authored order; actual selected prerequisite links and complete textual requirements. Most authored powers are independent siblings, so no invented progression chain. |
| Steam Deck support | Dedicated native context, visible prompts and synthetic-controller tests at both handheld resolutions; physical hardware remains untested. |

## Files changed
- MOD `Assets/Editor/Scenarios/ReferenceGladeNativeBatch.cs`: launch isolated skills audits at 800p and 720p.
- MOD `Assets/Scripts/Presentation/Input/InputHandler.Controller.cs`: route the dedicated Skills controller context.
- MOD `Assets/Scripts/Presentation/Input/InputHandler.ControllerHints.cs`: hide world hints behind the full-screen menu.
- MOD `Assets/Scripts/Presentation/Input/InputHandler.cs`: use persistent details instead of intercepting into a popup.
- MOD `Assets/Scripts/Presentation/Input/NativeGamepadInput.cs`: separate discipline shoulders from directional navigation.
- MOD `Assets/Scripts/Presentation/Rendering/AbilityDetailsBuilder.cs`: mask unknown prerequisite names consistently.
- MOD `Assets/Scripts/Presentation/UI/SkillsScreenUI.cs`: full-screen input, focus, purchase and assignment lifecycle.
- MOD `Assets/Scripts/Scenarios/Custom/ReferenceGladeNativePlayer.cs`: dispatch and report the native skills scenario.
- MOD `Assets/Tests/EditMode/Presentation/Input/AbilityReadersInputTests.cs`: retain reader compatibility using selected class identity.
- MOD `Tools/SteamDeck/README-Steam-Deck.md`: document skills controls and safe transactions.
- NEW `Assets/Scripts/Presentation/Rendering/SkillBrowserModel.cs`: grouped skill state and authoritative purchase/assignment commands.
- NEW `Assets/Scripts/Presentation/UI/SkillsScreenUI.View.cs`: retained Field Manual and prerequisite board layout.
- NEW `Assets/Scripts/Presentation/UI/SkillScreenText.cs`: render existing CP437 glyphs in retained UI meshes.
- NEW `Assets/Scripts/Scenarios/Custom/ReferenceGladeNativePlayer.Skills.cs`: real native input and rendered lifecycle verification.
- NEW `Assets/Tests/EditMode/Presentation/Rendering/SkillBrowserModelTests.cs`: browser ownership, purchase and assignment invariants.
- NEW `Assets/Tests/EditMode/Presentation/Rendering/SkillBrowserPolishTests.cs`: hidden knowledge, text, details and adversarial cases.
- NEW `Assets/Tests/EditMode/Presentation/Input/FullscreenSkillsInputTests.cs`: controller routing, focus, pagination and lifecycle counter-checks.
- NEW `Assets/Scripts/Presentation/Rendering/SkillBrowserModel.cs.meta`: Unity asset identity.
- NEW `Assets/Scripts/Presentation/UI/SkillsScreenUI.View.cs.meta`: Unity asset identity.
- NEW `Assets/Scripts/Presentation/UI/SkillScreenText.cs.meta`: Unity asset identity.
- NEW `Assets/Scripts/Scenarios/Custom/ReferenceGladeNativePlayer.Skills.cs.meta`: Unity asset identity.
- NEW `Assets/Tests/EditMode/Presentation/Rendering/SkillBrowserModelTests.cs.meta`: Unity asset identity.
- NEW `Assets/Tests/EditMode/Presentation/Rendering/SkillBrowserPolishTests.cs.meta`: Unity asset identity.
- NEW `Assets/Tests/EditMode/Presentation/Input/FullscreenSkillsInputTests.cs.meta`: Unity asset identity.
- NEW `Docs/FULLSCREEN-SKILLS.md` and selected `Docs/Verification/FullscreenSkills/` evidence: living design, ten passes, review and raw verification.

## Steam Deck delivery

Linux x86_64 non-development build succeeded with 0 errors and 4 existing C# warnings, job `build-a2973ab6b6`, 11.69 seconds; [receipt](Verification/FullscreenSkills/build.json). Warnings concern pre-existing hidden `Owner` members, unreachable GameBootstrap code and an unused Backstab field. The ELF x86_64 executable is packaged under `Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-current`; the archive is `CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`. It contains the updated skills UI, prior Vault/Spread changes, controls guide, existing audio credits/licenses, source revision and verification receipts. Archive contents and executable permissions are checked during packaging. Physical Deck execution remains unverified.

The three tracked settings assets changed automatically by Unity's build were clean before building and restored afterward; unrelated package/MCP log changes remain untouched and excluded from this feature commit.

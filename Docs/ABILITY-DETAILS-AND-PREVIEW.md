# Ability details and optional rite preview

Status: complete and verified. Root observed initial RED plus focused regressions, broad native GREEN, final reader regression GREEN, and the final isolated native keyboard/benchmark receipt. Sources are frozen for root commit. This is original CoO presentation work, not a claim of Qud UI parity. Root alone operates Unity and commits.

## Goal and small UI flow

Skills and the ability manager retain ordinary purchase, binding and fast activation. D opens the selected entry in the existing paginated AnnouncementUI. Closing the reader restores the same menu selection and scroll. Full authored prose remains available, followed by actual declaration range/targeting/cooldown, purchase conditions and available live costs. A passive is identified as passive. No reader attaches a skill, purchases, invokes damage hooks, starts cooldown or spends time.

P in the ability manager optionally reads an owned consuming rite. Self/radius rites read immediately; line/cone rites ask for one of the normal eight directions, then show a reader. Escape returns to the manager. Neither P nor a reader close casts. Enter/number-key casting keeps the existing direct path and no new confirmation.

Preview calls ResonanceSystem.Preview for visible candidate owners only. It reports spent/declined marks and resonance, labeled as resonance rather than final damage, and includes the actual rite description. Hidden cells stop directional reading; invisible occupants must not alter the disclosed candidate list. The reader explains that unseen interception and subsequent world changes can alter a real cast. Shared targeting gains opt-in read-only/visible-only collection; ordinary casts retain their existing geometry and FX defaults. No global opportunity markers, omniscient targets, channels or automatic combo choice.

## Verification corrections before production

| Assumption | Verified source correction |
| --- | --- |
| Reuse DetailsTextPopup | No such class exists. AnnouncementUI already wraps and paginates, including a detached VisibleLines snapshot. |
| Manager has full help | AbilityManagerStateBuilder currently shows bindings/cooldown only. Skills rows retain full prose but the UI clips its one-line footer. |
| Target collection is automatically pure | SpellTargeting and RiteTargeting record into an active SpellFxCapture; read-only collection must explicitly suppress that recording. |
| A preview can reuse damage calculation | Damage hooks consume buffs; some rite ComputeDamage overrides mutate cast counters. Preview must never call either. |
| All radius/self input means self-only | SelfCentered includes actual radius attacks. Rite Shape is the canonical geometry declaration. |
| Any source class is purchasable | Details admit current registry rows, honor hidden/obfuscated flags, and query existing purchase eligibility without changing it. |
| Resonance preview provides final damage | It supplies consumed/declined marks, rider tags and a resonance multiplier. Flat and special rite damage differ; final damage is deliberately not claimed. |

Source references: SkillsScreenUI, AbilityManagerUI, SkillsScreenStateBuilder, AbilityManagerStateBuilder, AnnouncementUI, InputHandler announcement/menu restoration, ActivatedAbilitySpec, SkillRegistry, SkillPurchaseEligibility, ConsumingRiteSkillBase, RiteTargeting, SpellTargeting, ResonanceSystem, GrimoireInk. Buff remaining readouts will use the spell milestone's persisted owner-action effects, never modifier queries.

## Scope and ownership

This agent owns UI readers, pure detail/preview builders, selected-rite preview input, narrow shared target-query opt-ins coordinated with the spell agent, focused tests/metas, and this document. Root owns JSON descriptions and GrimoireTooltipData. Spell agent owns damage/buff mechanics and the rite damage loop. Existing framework, purchasing, saves, skill costs and quick casting remain authoritative.

## Tests and native acceptance

Initial fixtures: AbilityDetailsTests, RitePreviewTests, AbilityReadersInputTests. New APIs are reflection-resolved so their absence produces assertion RED, not an assembly-wide compile failure. Assertions cover full prose/live metadata, unowned passive/active details without learning, unknown/hidden/obfuscated admission, actual mark preview with counter-checks, ink/cooldown/HP/effects/FX purity, visible interception and walls, fog/invisible/foreign/carried owners, and reader restoration without activation or purchase. The initial cases already include hidden/foreign ownership, malformed directions, stale selected abilities and repeated-query/FX adversarial boundaries. Additional coverage is limited to findings from native results and review.

After root observes RED: minimum implementation, root native EditMode GREEN, standalone ordinary reader scenario/menu, native screenshots from purchase details, manager details and optional rite preview. Script evidence can prove text, selected state, no spend and no cast. Pixels can prove layout and visible content. Neither proves novice comprehension or balance; no final acceptance claim before those receipts.

## Review and receipt ledger

Root observed all 34 new assertions fail at the missing query/reader seam in `Docs/Verification/SkillsEngagement/red.xml`, then explicitly released production. One preliminary native compile caught test dictionary-tag syntax; corrected before that assertion run. No Unity operations or commits were performed by this agent. A source-only compile of all current runtime sources passed with the existing five warnings; it does not substitute for native tests.

Implementation: `AbilityDetailsBuilder` reads full registry prose, side-effect-free purchase eligibility and detached ability declarations. It renders actual rite shape/ink/mark capacity/collateral and current cooldown. Generic active costs/shape/collateral continue to use authored prose: the declaration schema does not encode those fields, so the reader does not invent a universal policy. Live Ley Tap and Heart Flame effects use `EffectDescriber`. `RitePreviewBuilder` validates the actual owned rite/ability and current zone, then reads visible candidates through the same geometry collectors with FX disabled. It calls actual `ResonanceSystem.Preview` and never computes final damage. Hanging Bolt uses its public per-mark paralysis constant; other rites retain the explicit resonance/special-rule distinction.

D/P input is intercepted only inside the corresponding menu. Menus remain logically open with their footprints hidden; readers restore current snapshots, selected identity and scroll. Ordinary cast methods are unchanged. Source-only cold review checked this symmetry and the spell agent's saved-effect/cast-scope seam; native behavior and pixels remain pending.

### Owned files

- New `Assets/Scripts/Presentation/Rendering/AbilityDetailsBuilder.cs`, `RitePreviewBuilder.cs`, with metas.
- Modified `Assets/Scripts/Presentation/UI/SkillsScreenUI.cs`, `AbilityManagerUI.cs`, and `Assets/Scripts/Presentation/Input/InputHandler.cs`.
- Modified `Assets/Scripts/Gameplay/Combat/SpellTargeting.cs`, `Assets/Scripts/Gameplay/Magic/RiteTargeting.cs`; only target-query section of `Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs` (spell agent owns its damage loop).
- New `Assets/Tests/EditMode/Presentation/Rendering/AbilityDetailsTests.cs` (10 cases), `RitePreviewTests.cs` (21 after the focused footprint cases), and `Assets/Tests/EditMode/Presentation/Input/AbilityReadersInputTests.cs` (12 after the log/queue cases), with metas.
- New `Assets/Scripts/Scenarios/Custom/AbilityClarityNativePlayer.cs`, `Assets/Editor/Scenarios/AbilityClarityNativeBatch.cs`, with metas.
- This document. No ability-manager snapshot/state schema change was necessary.

### Native receipt contract

Menu: `Caves Of Ooo/Scenarios/UI/Ability Clarity Native Audit`. Static launcher: `CavesOfOoo.Editor.AbilityClarityNativeBatch.Launch()`. It follows existing isolated-save/scene/input restoration conventions. Reports and three reader screenshots land in `Docs/Verification/SkillsEngagement/NativeReaders/<runId>/`.

The controlled fixture starts Classic through ordinary menus, then explicitly flattens an isolated starting zone, repositions the original player, grants one Hanging Bolt skill and a finite original grimoire, places a stationary actual factory target with Wet, and sets bounded visibility. UI interaction uses native X/M/D/P/direction/Escape/Enter keys. The observer separately enumerates public copied reader pages to verify full prose; that enumeration is not native paging evidence. Ten checks cover actual entry/exit, full prose/live values, selection/scroll restoration, no resource/turn/effect/binding changes and the unchanged quick-cast direction state. Independent screenshot inspection is required before any visual acceptance claim. This is a controlled reader proof, not organic acquisition, combat balance or novice-usability proof.

Initial implementation native run: combined 281 cases, 272 passed and 9 failed. Eight clarity assertions collided with fixture name `visible target` appearing in the legitimate generic sentence `No visible target in this shape.` The fixture target is now named `marked kestrel`; all name/mark exclusion assertions remain intact. All six reader/input cases and all ten detail cases passed in that run. Observer-only native key pacing was increased to clear InputHandler's existing move-repeat gate; this changes no game input timing. Re-run pending.

Cold review found a real, bounded scene-specific targeting mismatch: `Cell.IsSolid()` also consults Morrowfast's off-anchor authored footprint collision. The visible-only line collector initially handled ordinary solids/doors but omitted that lookup. Three targeted cases now use the actual installed inn-northern-barrel owner at (49,17), its empty off-anchor ray cell (49,16), and absent/hidden controls. These are authored before a production correction; root-observed RED pending. No global spell geometry or scene framework change is proposed.

Root's broader 4,734-case run observed the visible Morrowfast footprint RED while removed and hidden controls passed. Root released the narrow correction: the visible line query now consults the existing authored footprint blocker and verifies its current owner/render/physics before considering it. Ordinary line/cone/radius casting remains unchanged. The lookup may populate existing derived scene caches, as the runtime collision query already does; it does not change saved scene fields, occupancy, effects, ink or turns.

Root also authorized running the spell agent's six-case `SpellCastReliabilityBench.Apply` at the end of the same isolated native reader session. The report preserves ten reader checks separately from the six command/scheduler/save checks, requires both groups plus restoration of the reader snapshot for completeness, and does not relabel benchmark commands as native UI evidence. Native GREEN/capture still pending.

### Pre-capture Q1-Q4 review

- Q1: both D readers hide the original menu footprint, preserve its logical open state and callback, then revalidate the selected identity before restoring the centered menu camera. Optional P uses a distinct input state; no cast dispatcher call occurs on reader close.
- Q2: purchase and owned details share one builder; radius/cone/line preview shares existing geometry with explicit perception and no-FX opt-ins. The Morrowfast exception now follows the existing line collision source. Buff text comes from the shared live EffectDescriber.
- Q3: current fixtures pair readable full/owned details with unknown/hidden/obfuscated cases; preview visibility with fog/unexplored/render-hidden/foreign/carried/dead cases; interception with its removal; resource purity with real marks and finite ink; and reader return with stale ability removal and non-rite refusal. The real off-anchor footprint test has removed/hidden controls.
- Q4: no channeling, final-damage prediction, universal collateral promise or organic discovery claim is made. The native observer distinguishes keyboard reading from public page enumeration and separate spell command/save evidence. Remaining visual acceptance and native results are pending; source review alone does not establish legibility or feel.

Native regression confirmation (root): `Verification/SkillsEngagement/broad-green.xml` passes **4,734/4,734**, zero failures or skips, 121.62 seconds. This is the affected-system suite in Unity Editor, not the standalone runner and not a whole-game balance verdict. All 283 selected fixture classes are recorded in the XML. Native Play receipt is recorded in the implementation overview after completion.

Root observed broad native EditMode GREEN: **4,734/4,734 passed**, including the three Morrowfast footprint cases, saved spell buffs and shifted progression. Isolated native keyboard capture launched next; Assets frozen during Play. The green count is the combined affected-systems sweep, not 4,734 newly authored reader tests.

First isolated native receipt `NativeReaders/ee60c4a903464393bf66dfcf1d50a0bc/report.json` passed all ten keyboard reader checks and all six separate spell benchmark checks; root inspected the three PNGs. Pixel review caught avoidable log pollution: reusing `MessageLog.AddAnnouncement` copied every full optional detail into the combat pane. Six bounded input tests now cover D in both menus and P, each with/without a pre-existing world announcement; they require full selected reader prose, unchanged combat messages/flash/queued notices, then normal queued-notice delivery and menu return. Production correction awaits root-observed RED. The intended fix opens AnnouncementUI directly using the same centered overlay and return-state fields, without changing MessageLog semantics.

Root observed all six log/queue regressions RED in `reader-log-red.xml`, including the queued notice overriding the requested details. The minimum fix now opens the existing AnnouncementUI directly while setting the original menu return state and centered overlay. Existing CloseAnnouncement still drains world notices afterward and then restores the menu. No MessageLog or generic announcement implementation changed. Assets frozen for final verification.

## Final acceptance receipts

- Initial 34-case assertion RED: `Docs/Verification/SkillsEngagement/red.xml`. Morrowfast correction was observed RED in `broad-first.xml`, with removed/hidden controls already passing. The six log/queue regressions were observed RED in `reader-log-red.xml`.
- Broad native EditMode GREEN before the last UI-only correction: `broad-green.xml`, **4,734/4,734 passed**. Final affected reader/input/announcement suite after the correction: `reader-final-green.xml`, **91/91 passed**, including all **43** clarity cases (10 details, 21 preview, 12 input). These are deliberately separate receipts, not a claim that the full 4,734 were rerun after the final small change.
- Final isolated keyboard run: `NativeReaders/1e6e8a3575564a818d590d20481539cd/report.json`, complete=true, reader failures=0, unexpected errors=0, **10/10** reader checks. Separate spell benchmark `c3283ca6128b4dc2b623d57d6e3b95a6`: **6/6**, failures=0, replacement-graph save/load and multi-target charges passed. Reader context restored=true.
- Root and this agent independently inspected the final three PNGs. Purchase and ability descriptions are fully wrapped and visible; preview shows the actual visible Wet mark, 1/2 marks, two-turn Hanging Bolt pin, ink/collateral and explicit reading-only wording. The two-page preview has visible navigation. The right combat pane now retains ordinary messages and the optional direction prompt, without duplicate reader prose. No clipping or overlapping main reader text was observed at the captured 1920x1080 size.

Final Q1-Q4 pass is complete with the two significant findings corrected through observed RED (authored footprint interception and log/announcement separation). No further significant source/receipt mismatch remains. Honesty bounds: native evidence uses the documented controlled fixture; page enumeration is an API observation; screenshots cover this captured resolution, not every screen size; organic spell acquisition, combat balance, novice comprehension and fun remain outside this slice. No final-damage prediction or channeling was introduced.

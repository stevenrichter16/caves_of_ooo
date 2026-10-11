# HUD polish — compact skills and grouped world log

Status: **implemented; final3,223 native EditMode tests pass; actual gameplay checks pass at1080p,800p and720p.** The user chose mockup A’s compact skill bar with mockup B’s grouped log. The saved prompt/references are in `Docs/HUD-MOCKUP-DESIGN-PROMPTS.md` and `Docs/Design/HudMockups/` (commit273da934c). Behavioral RED, review corrections and raw evidence are retained below. Matched profile review is complete; final Linux delivery is being finalized.

Implementation was imported after the audio20 sequence. Root owns Unity tests, actual Play, screenshots, profiling, commits and delivery. The reviewed source bundles remain in `/tmp/coo-hud-polish` and `/tmp/coo-hud-polish-history`; the narrow header follow-up is in `/tmp/coo-hud-polish-review`. The helper only performs staged imports and never operates Unity or Git.

## Selected design and exact implementation prompt

Implement ten compact skill cards within the current80×6 hotbar, keeping its existing screen hit geometry and full-resolution cameras. Every card keeps the real hotkey, explicit ready/cooldown/pending/empty state, one fixed cached icon and two name rows. The selected/pending header presents current name, readiness and actual targeting/range with the existing device-specific controls. It discloses truncation instead of claiming that arbitrary names/mechanics fit. The existing ability-details reader remains the authority for full mechanics.

Use original small code-native pixel art for the actual six starter powers: Ember Spit, Flaming Hands, Jet Blast, Ground Surge, Rime Grip and Calm. Their imagery must clearly distinguish fire, two flaming hands, water, electricity, ice and a violet calm symbol. Unknown powers use a neutral fallback. A new fixed normal-aspect pixel font is scoped to the gameplay hotbar/sidebar; other screens and global CP437 art remain intact.

Preserve every vital, effect/status, haul and focus source field. Present the recent raw message window in chronological groups separated by a muted teal rule whenever the stored scheduler tick changes. Exact adjacent duplicates collapse only within the same tick. Do not invent player-turn numbers, narrative categories, filters or new simulation. Append an on-demand `World log` entry to the existing Character menu so every saved raw message remains accessible. Retain saved text/tick/serial values, pending announcements, document audio provenance, return state and controller mappings.

Keep the established hotbar idle fast path, lifecycle clearing, caller-mutable snapshot defenses and full-resolution HUD. No per-frame full-history, inventory-action or world gathers are introduced. Fixed art caches and bounded recent/history windows must have explicit contracts. Native Play and original pixels, not source-only tests, decide whether the small layout is readable.

## Verification sweep and source decisions

| Verified source | Contract and resulting decision |
|---|---|
| `GameplayHotbarLayout`, `ZoneRenderer`, `CameraFollow` |80columns×6rows; ten8-column slots and existing all-six-row hit testing. Keep these values and camera reservations. Row5 becomes one combined header; rows4…0 hold each card. |
| `ActivatedAbilitiesPart`, `ActivatedAbility`, `HotbarStateBuilder` |Slot assignment and current fields are authoritative. TargetingMode and Range are mutable; snapshot equality must include both. `IsUsable` is currently cooldown-based, so `READY` makes no new resource/can-cast promise. |
| Ability source classes and tooltip table |Six exact SourcePowerClass values choose art; renamed/imitation display names do not. Real power is `Pyromancy_FlamingHands`, not an invented skill. Full mechanics remain in `AbilityDetailsBuilder` and the actual ability-manager detail route. |
| Existing CP437 generator |Global text uses doubled8×16 geometry. The new fixed ASCII5×7 glyphs have uniform scaling and a separate atlas. New HUD-owned high-bit frame/solid masks keep the existing cell geometry; unknown text uses a readable fallback. |
| Current hotbar renderer |Copies prior slot values, skips equal frames, and clears on lifecycle changes. Keep that path, include new icon/target values, repaint only changed cards where indices are ordinary, and update icon placement when aspect changes. |
| `SidebarStateBuilder` |Normal HUD consumes at most the requested recent window (default30) and its existing direct vital queries. Cache recent gathering by both MessageLog.Revision and requested limit. Do not build inventory categories/actions. |
| `SidebarRenderer` |Old wrapped-log cache checked ends/count while fingerprint examined more data. Retained exact text/tick/count/serial comparisons are needed for middle-entry corrections and grouping; copy values because builders reuse scratch buffers. |
| Actual vital renderer budget |Seven rows, including a possible sixth HAUL line. Preserve all vital rows and disclose extra status text with `>` at the real budget. Full effect details remain in the existing inventory effects reader. |
| `MessageLog` save and phase15 document audio |Historical raw lists and announcement text queue remain intact. Add only in-memory revision/generation and bounded range reads. Preserve the parallel `AnnouncementOrigins`, `DocumentReadAudio` handoff and Restore/Clear silence exactly. |
| Current native launcher |Always forces1920×1080. The baseline/after profile retains this exact recipe. Add explicit functional HUD launchers for1280×800/720 through the same temporary Game view and restoration lifecycle; external Game view selection alone is insufficient. |
| Native/test teardown |A returned test result does not prove cleanup complete. HUD launch guard checks compiling/updating, actual installed test-runner activity and prior native/save/scene restoration flags. Coordinator must also wait for the completed job, settled editor and restored scenes before launching. No cancellation or test-runner mutation is performed. |

These are CoO presentation choices informed by the selected mockups, not a claim of exact Caves of Qud UI parity. Existing controls and gameplay semantics remain the source of truth.

## Layout and bounded caches

The hotbar keeps all ten slots. Row4 contains an inset hotkey and state (`READY`, `CDn`, `CD999+`, `AIM`, `EMPTY`); the icon is centered at y3 and uniformly enlarged1.75×; rows1/0 contain the name; a small selected/pending marker sits beside the icon. A thin inset border on the existing background layer visibly frames all ten cards. Its tint changes with selected/pending state and its old footprint is erased when the card changes. Cooldown text never overwrites either name row. Header detail and device guidance have separate bounded columns. Overflow uses `>` and full names/mechanics remain available through the existing ability reader.

The frame cache has one64×80 sprite, scaled to the unchanged8-column×5-row card footprint with a1px inset border. Its background tile replaces forty per-cell fills; there is no per-frame allocation. The icon cache has seven tiles in one112×16 atlas: six original16×16 designs and one fallback. The HUD font has95ASCII tiles in one96×60 atlas, point filtered and uniformly scaled. There is no dynamic per-name/per-character texture creation. Cache reset/rebuild follows Unity object lifetime conventions. Actual readability, visible spacing, color distinction and clipping remain native pixel gates.

The recent sidebar retains chronological unmodified bodies, paragraphs and serial scroll identity. Quiet rules separate contiguous stored-tick groups; no sorting by tick or fabricated turn labels occurs. The normal recent window remains30. The public view is read-only and the renderer retains its own value copies.

Full history opens only on demand in16-raw-entry windows, with range API reads capped at64 entries. Every raw duplicate remains visible. New in-memory `Revision` advances on Add/Clear/Restore; `HistoryGeneration` advances only Clear/Restore, including equal-count/serial replacement. Neither stamp changes the save format. A very long authored message can still require proportional wrapping inside a bounded window; this is not constant work for arbitrary text length. Stable open frames check revision/input without rebuilding lines or repainting.

## Input, history and existing readers

World D-pad L/R still cycles skills; X activates; LT+X opens abilities. Targeting A/RT confirms and B cancels. Keyboard hotkeys and all existing hit boxes remain. The full-history action is appended to Character after its existing rows. F2 opens history only from Normal/Look and does not steal inventory’s effects shortcut. History supports actual menu directions/LB/RB, B return and local Home/End. Opening/paging/closing is free and restores exact prior state. Missing canvas/current ownership refuses a hidden modal.

History does not publish or consume announcements, pretend to be a physical document, or arm page audio. Its additive MessageLog merge must preserve audio15’s separate transient origin queue, committed document-read authority and announcement delivery. The final regression selection includes physical-page and pending-announcement cases. Future source changes are merged by reviewed hunks; old full input/MessageLog copies must never overwrite them.

## Test-first and integration sequence

1. Finish audio20 and wait for native/test/scene teardown. Import only the existing-API HUD profile observer and its launcher hooks. Record its source revision/diff and execute the `before` recipe while the old visual/history implementation remains in place.
2. Import only `HudPolishBehaviorTests`, `HudCardFramingTests` and `WorldLogHistoryBaselineTests`. The visual fixtures reach eleven layout/content/cache assertions plus six actual-frame/icon-gap/state-erasure assertions using existing APIs. The history fixture uses reflection for new history members so missing APIs produce assertion failures, not compiler-only evidence, and continues into real behavior once present. Record native RED XML before production.
3. Apply the six visual source changes, four new art/presentation files, additive history source/input changes, remaining fixtures and narrowly justified legacy visual-pin updates. The helper checks exact destinations, staged source hashes and existing visual/pin baselines; ambiguity/refused anchors require review, not forced overwrites.
4. Run focused new and existing regressions. The original visual fixtures contained 11 baseline + 6 framing + 20 ordinary + 27 adversarial cases. One later header-coverage regression increases framing to 7 and visual cases to 65. History contributes 19 core + 11 native-input + 6 baseline-reflection cases, for 101 executed focused cases. Authored counts and native results are distinguished in the ledger below.
5. Fresh actual functional Play at1920×1080,1280×800 and1280×720. Inspect original-resolution screenshots for the selected layout, six distinct symbols, readable names/status/focus/log, complete10slots, no overlap, understandable cooldown/pending/empty states and unchanged full-resolution HUD.
6. Wait for teardown, then run the unchanged profile observer with `after` label and identical1080p/settings/focus/input recipe. Retain raw samples and compare only matching successful captures. New full-history behavior has no old-feature performance baseline and is assessed separately.

The helper stages `baseline`, `red`, `production` are explicit; default invocation computes a read-only plan. Applying needs an external empty backup directory and writes only a hard-coded whitelist. It never starts tests, refreshes Unity, changes source control, or labels results GREEN. Existing source drift and unexpected new-file collisions stop before writes. Production import requires successful baseline data and all three executed behavioral RED fixtures. These checks organize the coordinator’s evidence; they do not cryptographically prove that a native receipt corresponds to a Git revision.

## Native functional and matched-profile contracts

`hud-polish` starts the ordinary isolated seed64 actor at40,12 with the six real starter powers. Real synthetic gamepad input cycles/cancels, then one legal visible empty-ground Ground Surge commits through production input and cooldown.34real empty-slot attempts plus one after that paid cast produce inspectable raw history without injecting MessageLog. The route opens the original ability details, reaches history through Character, pages before the recent30 window, observes idle reader work, returns, uses F2/Home/End, captures Look focus, and saves/loads with actual F5/F6. It checks raw history prefix/selection retention, idle hotbar repaint counters and screen-target HUD cameras. The scheduler and ordinary NPCs remain active; no grants, teleports, cooldown forcing or direct ability execution occur. Functional checks are18plus ordinary boot, with screenshots captured for review.

`hud-profile` compiles against the pre-redesign APIs and has no new font/icon/history dependency. Recipe `hud-existing-input-v1`:120idle frames,24actual D-pad cycles,40alternating9/0empty attempts,12older and12newer sidebar scroll keys,6Look/right/escape cycles,12settle frames,120idle frames. All inputs are free, with unchanged actor/zone and no direct renderer calls. The before/after label affects only the receipt. Startup buffers, screenshots and file IO are outside the sampled interval.

Raw data records explicit observer and completed-snapshot frames, scheduler tick, HUD render/clear counters, native marker availability/units, source slot identities and actual screen/camera/graphics/audio/frame-limit/focus settings. `ProfilerRecorder.LastValue` can refer to an earlier completed frame; it is not falsely attributed to a snapshot. Blank means unavailable, not zero. Markers nest and must not be summed as exclusive total time. Diagnostic snapshot/input-observer overhead is shared by both runs. Idle counters and active work are reported separately. There is no FPS, standalone-build, physical Steam Deck, GPU, long-session or subjective comfort claim.

## Evidence ledger — do not replace pending with inference

| Gate | Status | Receipt / result |
|---|---|---|
| User-selected A+B and saved mockups | Complete | Design commit273da934c; design prompt/images above. |
| Source/API sweep and staged import | Complete | Exact whitelist/hash imports; additive MessageLog/input merge preserves document origins. |
| Initial fixture authoring | Complete and subsequently executed | Tests were staged before corresponding implementation; behavioral native RED captured before production import. |
| Independent visual/cache/font-data review | Bounded findings handled | Transparent legacy primitives and variable-width header coverage were found and corrected. Source review and offline projections do not establish native readability. |
| Independent native route/profile premise review | No remaining material API/premise finding | Ground Surge, real slot messages, counters and three-resolution launcher compiled; the menu selector was corrected to use `Display` as described below. |
| History isolated core | 19 / 19 pure GREEN | Separate compiler RED and two adversarial REDs preceded fixes. Native UI is covered separately by the focused run. |
| Import-helper offline test-first check | 21 / 21 offline GREEN | Missing-module RED preceded implementation. Full staged imports were tested only in disposable filesystem copies; this is importer evidence. |
| Old-HUD baseline | Complete: 11 / 11 checks, no unexpected errors | `Profile/c698fb395ce546248e5348bc32ac35b9/report.json`; recipe `hud-existing-input-v1`, before stage, 6,233 samples with no overflow. Raw JSON/CSV and `BaselineSource` retain the source/observer record. This is one side of the comparison, not a performance gain. |
| Behavioral native RED before production | 23 executed: 22 failed, 1 passed, no skips | `Docs/Verification/HudPolish/red.xml`; 11 visual-content, 6 framing and 6 history-baseline cases reached assertions. Existing unchanged-frame work behavior already passed. |
| First focused attempt and header RED | 101 executed: 98 passed, 3 failed, no skips | `Docs/Verification/HudPolish/first-green-attempt-and-header-red.xml`; one real header-coverage defect and two fixture premise errors, classified below. |
| Focused native GREEN | **101 / 101 passed**, no skips or failures | `Docs/Verification/HudPolish/focused-green.xml`; native EditMode duration 3.605 seconds. This does not establish actual Play, visual readability or measured performance. |
| Functional Play1920×1080 | **19/19, zero unexpected errors** | Final-source `Native/c461f3c50f9a4816b1acb7f6daa1962e/report.json`, 13.316s; original pixels reviewed separately. |
| Functional Play1280×800 | **19/19, zero unexpected errors** | Final-source `Native/435f30fba2d14461aedc8ff47c052176/report.json`, 13.340s; original pixels reviewed separately. |
| Functional Play1280×720 | **19/19, zero unexpected errors** | Final-source `Native/2f137a3b306a41098bff85b830f45b6b/report.json`, 12.920s; original pixels reviewed separately. |
| Matched post-implementation profile | **Complete; no performance-gain claim** | `profile-comparison.json` and `profile-reproduction-comparison.json`; exact unchanged observer/settings/input recipe. One first-run whole-frame allocation anomaly did not recur. See `profile-review.md`. |
| Final independent review / delivery | **Review complete; Linux package pending** | `cold-eye-review.md`, `visual-review.md`, `profile-review.md`; both material findings fixed, final3,223tests pass. Build evidence follows separately. |

## Scope decisions and self-review

🟡 Implemented and covered by the focused native suite: mutable range/target identity; same-tick-only duplicate collapse; exact middle-log cache identity; revision+requested-limit cache; read-only public recent view; explicit sidebar Clear recovery; actual7-row vital overflow; scoped font; explicit three-resolution launcher contract.

🟡 Integration boundary: phase15 physical document origins must remain byte-for-byte in existing blocks, with original `ConsumeAnnouncement(out documentAudio)` to `AnnouncementUI.Open(msg,documentAudio)` intact. The helper adds only history state/range/input hooks and guards this boundary. Final physical-page/native tests passed in the combined suite.

🧪 Final broader regression, original-resolution pixels at all sizes and actual controller/save/history routes passed. Matched profile interpretation and packaging follow below. Art bitmap dimensions or tile identities do not prove legibility. Live failure should retain its receipt and be classified before changing production or loosening a route.

⚪ Deliberate bounds: no new UI framework, animation, category filters, mechanics, inventory/world scans or broad global font replacement. Selected compact detail may clip, with `>` and existing full readers. Full history is on demand and does not invent narrative categories. No claimed performance benefit before real baseline/after evidence.

## Files and ownership

Visual source: `HotbarSnapshot`, `HotbarStateBuilder`, `HotbarRenderer`, `SidebarStateBuilder`, `SidebarTextFormatter`, `SidebarRenderer`; new `HudHotbarPresentation`, `HudPixelFont`, `HudSkillIconAtlas`, `HudCardFrame`.

History source: narrow `MessageLog`, `InputHandler`, `InputHandler.Controller` additions; new `WorldLogHistoryModel`, `WorldLogHistoryUI`, `InputHandler.LogHistory`. Save format, AnnouncementUI and physical document audio are unchanged.

Validation: four visual fixtures, history baseline/core/input fixtures, justified six existing visual-pin updates, baseline and functional native partials, narrow shared native main/batch hooks and the staged outside-repository import helper. New C# files include fresh32hex metas. Root appends actual receipts/results and commits this living document with the final implementation.

## Selected-A follow-up before import
Root reviewed the selected image and the first static projection and found that underlines alone made the visual change too subtle. Six current-API tests were then authored before adding the fixed thin-frame sprite and enlarged icon placement. Names now occupy rows1/0; the1.75-unit icon bounds are y2.125…3.875, leaving positive gaps from the full text-sprite rectangles of the name and state bands. Number/state remain within their card, with no extra height or remapped hits. Frame tint resets on selection/pending changes and removed cards erase the old background sprite. Stable frames retain both foreground and background sentinels. The separator accent is now a visible muted teal; its grouping still reflects raw tick boundaries only. These source/geometry checks now pass in the focused native suite. Original-resolution visual inspection remains pending.

## Focus contrast follow-up before import
The legacy focus renderer dimmed every line after its first two, including actual HP/effect details. One current-API behavioral test now checks a later rendered effect line is Gray while decorative dividers stay DarkGray. It was authored before the narrow outside-draft color correction. Content, row limits and focus collection are unchanged. The native assertion now passes; live readability remains pending.

## Native border observation and scoped correction
Original-resolution audio captures exposed blank old box borders. Source review confirmed `GenerateTextTileset` creates256non-null sprites but paints only its ASCII dictionary: high-bit box/solid cells contain no pixels. The initial draft had mistakenly preserved that assumption for sidebar rules. One current-API behavioral test now checks actual separator alpha and an opaque hotbar-header background while retaining the exact global font object; the test was authored before the scoped correction. The HUD font now owns twelve fixed8×16 box/solid masks in one96×16 atlas, with unchanged.5×1geometry. Only hotbar/sidebar callers use them; no CP437 world/menu code changes. The primitive pixel and visual source assertions now pass in the focused native suite; actual rendered screenshot acceptance remains pending.

The phase19town capture also showed intermittently absent old title/cards despite a visible hint. Read-only lifecycle review found no proven cause: zone/menu clears invalidate retained state, the snapshot always contains ten slots, and the capture copies the rendered frame without manually rendering cameras. Phase18quiet04 actually retains all slot labels/title but no borders. Do not claim the HUD redesign fixes the unclassified phase19 issue; native snapshots and actual original pixels must confirm recovery and continued visibility.

## Native integration findings and corrections

The first 23-case run against the old production HUD recorded 22 assertion failures and one existing invariant pass. The staged production was then imported. A separate header-coverage test was added before its fix and executed with the other focused cases: the first 101-case run produced 98 passes and these three failures.

| Failed case | Classification and bounded correction |
|---|---|
| `HudCardFramingTests.HeaderBackgroundCoversEveryCellAndFollowsAspectChanges` | **Production defect, observed RED.** The newly opaque half-unit header blocks left gaps when hotbar cells were wider. Only header background transforms now scale horizontally to the actual cell width and refresh when aspect changes. The regression checks all 80 submitted bounds through aspect 8 → 10 → 8. Card/name geometry and idle behavior are unchanged. |
| `HudPolishTests.EqualFrameDoesZeroHotbarWorkAndChangedPowerRepaintsOnlyItsCard` | **Fixture premise error.** Its background sentinel was on the changed first card and was correctly repainted. The sentinel moved to the unchanged card at `(16, 0)`; the zero-work and unchanged-card assertions remain. No production relaxation was made. |
| `WorldLogHistoryInputTests.ActualCharacterMenuAppendsWorldLogAndNativeAOpensOlderHistoryWithoutCost` | **Fixture/API premise error.** `InventoryAction.Name` is the internal `Controller` command label; visible rows use `Display`. The test now compares `Display`, and the two matching selectors in the actual HUD audit use `HighlightedAction?.Display`. Menu contents, input and costs were unchanged. |

After the header fix and the two premise corrections, all 101 focused native EditMode cases passed, with no skips, in 3.605 seconds. The actual functional runs at 1280 × 800, 1280 × 720 and 1920 × 1080, screenshot review and matched after profile have separate pending gates. Do not infer their results from the focused suite.

### Live divider and punctuation correction

The first 1280 × 800 native route completed all 19 interaction checks with zero failures or unexpected errors (`Native/03caac4ca1ef4d2eb230f2675239c81c/report.json`). Original-pixel inspection still found a presentation defect: TurnManager's stored 40-character CP437 double-line divider wrapped into question marks, and supported Unicode punctuation also fell back to `?`. The passing interaction report was not treated as proof of correct pixels.

Ten additional behavioral cases cover the actual sidebar glyphs, the fixed double-line pixel mask, existing punctuation mappings, exact divider recognition, serial-based chronological identity and unchanged raw log entries. Short, long, mixed-glyph and prefixed messages remain ordinary text. The narrow correction reuses `Cp437.Map` in the HUD-only font and treats only the exact stored CP437 divider or its Unicode old-save form as one quiet separator. An explicit divider suppresses an adjacent inferred tick separator; stored messages and the full-history reader remain unchanged. Unknown glyphs retain the bounded `?` fallback. No world/menu font or HUD geometry changes are included.

Native RED ran all 21 cases in `HudPolishBehaviorTests`: 15 passed and six failed on the intended missing rendering behavior. All four new ordinary-text counterchecks passed (`divider-red.xml`). The two-file correction then compiled with zero errors. The coordinator has started the final 155-fixture native suite; its result and fresh pixel verification remain pending. The existing 19/19 route predates the correction and does not satisfy its post-fix visual gate.

## Combined regression corrections

The first combined native EditMode sweep ran 3,153 cases: 3,150 passed and three failed, with no skips (`Verification/HudPolish/first-broad.xml`). All 111 new HUD/history cases passed, including the divider correction. The three failures were reviewed before changing expectations:

- `ConnectedInventoryInputTests` still pinned ordinary food as free. Its original October 2 expectation predates the approved October 10 self-consumption change in ancestor `0ff878e1f6467b6557db4e987ceb282cd586d56c`. Current production consumption code was unchanged by this work. Update only the stale paid predicate and add ordinary-food veto, outer-rollback and stale-source cases. Include the existing 19-case `PaidSelfConsumptionInputTests` as independent corroboration.
- Two old `SteamDeckHudPerformanceTests` used a background sentinel at `(0, 0)` with an explicitly zero-card snapshot. That cell no longer holds a card. Move the sentinel to the actual header at `(0, GridHeight - 1)` and assert that tile exists before tinting. Keep all zero-work, changed-text and trailing-erasure assertions unchanged.

The 156-fixture rerun passed all 3,175 cases with no failures or skips in 197.668 seconds (`Verification/AudioPolish20/Delivery/final-editmode-green.xml`). All 111 new HUD/history cases and 793 audio cases passed. No gameplay timing policy or rendering implementation was relaxed to satisfy these three tests. A later controller-hover defect discovered in actual 1080p Play and a thoughts-view idle-cache review finding require the final post-fix sweep below.

## Actual-play follow-up: stationary pointer and controller navigation

Both post-divider-correction handheld routes completed 19/19 checks with zero unexpected errors: 1280×800 `Native/d5a77bbff6224351a5814ca23d298b7f` (16.4121s), and 1280×720 `Native/9c3380f3f8784f48ae0b0a99bbc5088f` (13.4165s). Original-pixel review found complete ten-card layouts, distinct symbols, legible names/cooldowns, clean grouped rules, full details/history and restored save/load HUD. These receipts precede the pointer follow-up.

The 1080p route failed twice while navigating Character. Preserve both incomplete receipts (`Native/6c4aba2084ff4aee943a8f517bc34e00` and `Native/9471d5cb83714f598feed3205213c561`); neither counts as successful Play. A read-only diagnostic recorded the actual parked legacy pointer at (390,590), hovered row5 and twelve consecutive Factions highlights despite real D-pad attempts. `WorldActionMenuUI.HandleInput` applied hover every frame, so a stationary pointer could undo keyboard/controller movement. No cursor warp, retry expansion or synthetic selected row was used to bypass it.

Two real-input regression cases first failed for the intended neutral-frame selection reset (`stationary-pointer-red.xml`: 11 pass,2 fail). The narrow fix samples pointer position on Open and reclaims hover only when that sample moves; existing actual-click hit testing is unchanged. A counter uses the actual pointer/camera hit test with only a seeded prior sample to establish one-time hover reclaim and Open rearming; it does not claim synthesized OS motion. All14 input cases then passed (`stationary-pointer-green.xml`,1.530s). Fresh1080p gameplay and the final expanded regression subsequently passed, as recorded below.

## Final cold-eye review: thoughts-view idle work

🟡 Fixed before delivery: the new exact retained-log comparison was unconditional, but THOUGHTS never updates the independent wrapped-log cache. With a nonempty/changed hidden log it therefore forced a full sidebar clear/repaint on every unchanged thought frame. Two regression cases (cold thoughts view and prior log render) reached the intended failure: each of ten idle frames repainted (`thoughts-idle-red.xml`:21 passed,2 failed). The one-clause fix applies retained-log equality only while LOG is visible. The same tests positively require a changed thought to redraw and a return to LOG to show current hidden text rather than stale text. The final159-fixture native sweep includes these cases and subsequently passed, as recorded below.

The successful post-pointer1080p route `Native/203cb58428fb47f9bbeb302ce38db763` completed all19 checks in14.752s, zero unexpected errors. Its recorded actual pointer stayed at(390,590); ordinary D-pad input traversed every Character row to World log. All seven HUD captures were reviewed at original resolution. The independent pixel review is `Verification/HudPolish/visual-review.md`.

⚪ Cosmetic limitation: faint one-pixel vertical background seams are visible on the1080p sidebar but do not obscure content. No additional framework/layout work was introduced for this. Physical Steam Deck comfort/performance and subjective musical listening remain outside the verified scope.

## Final combined native EditMode result

All **3,223/3,223** cases across159fixtures passed, no skips, in195.981s after both the stationary-pointer and thoughts-cache corrections. Raw receipt: `Verification/AudioPolish20/Delivery/final-editmode-after-pointer-green.xml`; exact source-validated list: `final-pointer-fixtures.json` beside it. This includes116new HUD/history cases and793audio cases. The three added existing menu fixtures cover generic shortcut/reopen behavior, Normal/Look returns and accepted/refused world-action receipts. It does not relabel the earlier3,175-case result as post-fix evidence.

The thoughts-mode regressions now require zero sidebar paints/clears over each ten-frame stable interval, a real visible-thought update to repaint, and return to the current log. Final functional routes and profile remain separate evidence.

The final post-thought-fix functional receipts are1080p `c461f3c50f9a4816b1acb7f6daa1962e` (13.316s),800p `435f30fba2d14461aedc8ff47c052176` (13.340s) and720p `2f137a3b306a41098bff85b830f45b6b` (12.920s). Each completed19/19 with zero unexpected errors and final cleanup. Earlier successful/failing captures remain historical evidence.

## Matched performance review and final audio smoke

The unchanged observer SHA256 is `8158024d0e41a899da470830dec753f2e1e0d3e4c0b8336fa43ff9684bea02e4`, matching the retained baseline import receipt. Both after runs completed11/11 with zero errors and matched settings/action sequences. First after `00059d01db8144d586c8d6259fd552ab` retained an unexplained5.30s whole-frame allocation spike (152samples over1MB, mostly without HUD paints). Source review found no matching unbounded HUD work; no speculative fix was made. One unchanged reproduction `53a296fe226c41bd9fafe3c5e3531eac` recorded5,416samples with zero over1MB (max59,835bytes) and no GC collections. The original anomaly remains recorded; its cause is not proven.

Both after runs and baseline perform exactly104hotbar/70sidebar redraws across the recipe, with zero HUD redraws in both stable idle intervals. In the reproduction, whole-frame message-allocation p95 is27,763bytes versus26,059 baseline and the nested sidebar median is0.00675ms versus0.001583ms. This is small additional cost, not an optimization claim. See the independent `profile-review.md`; nested markers cannot be added and whole-frame allocation is not attributable solely to HUD. No physical Steam Deck/FPS/GPU claim follows.

Supplementary final-source audio19 town travel passed10/10 (`AudioPolish20/19/3d1fd447831248f0889ca01ffbe9f1ae`,17.383s); audio20 actual Audio-menu gain/mute/unmute passed12/12 (`AudioPolish20/20/ac2eff5bf22543deb8d2a88b0f0c14bb`,13.270s), each zero errors with original screenshots reviewed. The town-return HUD has complete text/frames. Earlier incomplete text observations are not assigned an unproven cause. These are final smoke checks, not extra iterations; the delivery explicitly selects the new phase20 receipt.

## Final implementation review and source checkpoint

🔵 The four-question review checked input Open/Close and Normal/Look returns; MessageLog Add/Clear/Restore revisions and unchanged serialized bytes; fixed font/icon/card lifecycle and exact cache identities; raw-history versus grouped-sidebar semantics. All new behavior has ordinary/counter/adversarial cases, including current-only icon identity and message clock grouping. Existing action/audio provenance is preserved. Independent reviews are retained with their original scopes and timestamps rather than rewritten as broader claims.

SCOPE DIVERGENCE: actual1080p Play required the small generic menu stationary-pointer fix. Exact stored turn fences and Unicode punctuation needed explicit scoped glyph support after original-pixel review. Full retained history is an on-demand Character/F2 reader; grouping the recent sidebar alone would otherwise conceal access to older messages. No gameplay balance, global font, save-format or camera-reservation change was included. The ordinary-food test pin was stale against existing approved consumption policy; only the test/counters changed.

Final source is ready for a Linux x86_64 build. Packaging records the exact full source commit and native test/build evidence. Until that receipt exists, this document does not claim a delivered player.

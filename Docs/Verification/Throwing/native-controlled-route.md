# Controlled native throwing acceptance route

Status: corrected unattended routes pass33/33 checks at1280×800 and1280×720. Independent inspection confirms the source and recovery menus are now visible and legible at both sizes. Earlier31-check reports retain the diagnosed invisible-menu defect and are superseded for visual acceptance. Synthetic mouse delivery was rejected; actual mouse acceptance remains pending.

Launcher: `Assets/Editor/Scenarios/ThrowingNativeBatch.cs`, menu **Caves Of Ooo → Scenarios → Combat → Throwing Native Audit**. Runtime: `Assets/Scripts/Scenarios/Custom/ThrowingNativePlayer.cs`. Root coordinates editor execution and reads the resulting `Native/<run-id>/report.json` plus eleven PNGs. A six-minute watchdog is finite; each action waits at most twelve seconds for normal state and resolved payment.

The launcher uses `NativeSaveIsolation` and refuses dirty/unsaved scenes or an existing Play session. It snapshots/restores the prior scene setup, start scene, bootstrap seed/build-choice flag, private save root and owned player preferences. The runtime restores original input settings, background behavior, current keyboard/gamepad, and removes synthetic devices. The modified zone and camera belong only to that isolated Play session; the user's campaign is never loaded.

## Disclosed fixture

N → Classic is entered through the actual boot/build menu. Then the original starting zone is cleared, a bounded 17×11 stone floor placed, and the player moved to (20,12), Strength/Agility set to30 and carry capacity500. One actual MarlbackGleaner at (25,12) has Brain removed, HP500 and natural AV/DV0, making it a stationary surviving target. An actual ShortSword is equipped; Dagger stack3, Warhammer and WaterTonic are carried. A separate Dagger at (22,12) challenges body-directed throws through low loose items. The five throwing skills are granted and the existing combat RNG seeded123. Setup is intentionally controlled and precedes observations.

After setup every menu, selection, command, movement and save/load uses queued native keyboard or synthetic Gamepad states through the production Input System. Reflection observes current menu/cursor state; it does not invoke gameplay commands. Exact scheduler cost uses `tickDelta*speed + energyBefore-energyAfter`, compared with `ActionThreshold*actionCount`.

## Finite route and controls

1. Actual inventory equipped-item row → Throw. Aim at the creature, capture body/ground previews, cancel into the same item popup, reopen and throw. Confirm exactly one action, exact grounded identity, damage, and unchanged intermediate floor dagger.
2. Actual ability manager → Measured Throw → source picker. Cancel freely, reopen, select the dagger stack and prepare the creature. Confirm one paid action, unchanged stack, live preparation and cooldown5. Native F5/F6 must restore the same state. Throw the same stack through inventory: one unit/action, preparation consumed, cooldown4.
3. Actual inventory WaterTonic route. Synthetic D-pad moves to (23,15); Y changes aim, LB narrows nine preview cells to one, B cancels freely. Reopen defaults Broad, LB focuses, A confirms. Confirm one consumed tonic/action and one finite film with nine exposure units. F5/F6 preserves its current round budget and graph.
4. Ability manager → Driving Throw → Warhammer. Capture conditional push preview, confirm native throw, verify damage, target at(26,12), exact hammer at(25,12), cooldown11 and owned recovery mark.
5. Four native right-arrow steps reach(24,12), costing four actions. Ability manager → Recovering Step → exact hammer moves to(25,12), secures one carried unit without equipping, pays once and sets cooldown5. F5/F6 preserves the graph.
6. Walk left twice to(23,12), paying twice. L → Left selects the preserved floor dagger at(22,12); Enter → actual world action menu → Throw → shared targeting. Cancel must restore Look at the exact source with unchanged ownership and zero time. Reopen and aim at the surviving target(26,12), then confirm exactly one paid action, damage and the exact one-unit dagger at contact. Final F5/F6 preserves the graph. No unresolved action guard or blocking FX may remain.

## Honesty bounds

**Can verify:** actual input routing, known preview/state, identity/count, current cooldown/preparation, finite film disposition, scheduler payment and stable serialization. Reports preserve each boolean check, observation signature and screenshot path; `complete` requires the whole route and zero driver/editor errors.

**Cannot verify:** natural source access, affordable progression, AI decisions, encounter balance, physical Steam Deck/Steam Input/performance, novice understanding or attractive/readable pixels. Natural acquisition has a separate campaign route. Root must inspect captures independently at1280×800/720; this document does not convert script completion into visual acceptance.


## Bounded timing receipt

The runtime samples `Time.unscaledDeltaTime` from fixture readiness through at most the next90seconds of the actual route, without padding idle time. The report includes frame count, sampled elapsed seconds and p50/p95/p99/max milliseconds. Menu/input pacing, screenshots, save/load and Editor overhead remain included. This is a reproducible observed route interval; it does not isolate rendering/GC or establish physical Steam Deck performance.


## First native attempt and harness correction

`Native/1bac8a0157e442ac95010228cdaa8ba0/report.json` records the first1280×800 attempt: the fixture check passed, then bounded row navigation stopped before the first throw/screenshot (35.76seconds, no Editor errors). Inventory opens with Equipment focused, although its inventory row list is already populated. The driver incorrectly treated a matching list row as proof of active inventory focus. It now presses the actual Tab key and verifies focus before navigating, without modifying gameplay. The rejected attempt remains evidence; its frame samples describe that failed route only.

The next attempt also queues synthetic mouse clicks at the actual rendered aim/splash button bounds, requiring Body→Ground→Body and Broad→Focused→Broad with zero payment, and records queued versus legacy-observed coordinates. This tests the actual GUI route only if the mode changes occur; synthetic Mouse state alone cannot prove Unity's legacy/IMGUI bridge. The original mouse device is restored and the injected device removed. Actual runtime Screen dimensions are now recorded by the scenario and per capture.


## Actual mouse acceptance gate

The second route, `Native/c5e51c0b6e4d41a48ba9072ff76faaf4/report.json`, completed in42.48seconds at a reported and captured **1280×800**. All equipped/carried/ground-source, skill picker, keyboard/controller, paid action, finite film, recovery and F5/F6 checks passed. Two first-toggle mouse checks failed because queued InputSystem Mouse state did not reach the legacy/IMGUI controls; the reverse-toggle checks alone are not mouse evidence. That rejected report is retained with `complete=false`. This was a test adapter limitation, not an observed physical-mouse gameplay failure.

The accepted driver now uses an explicit manual mouse gate: invoke **`CavesOfOoo.Editor.ThrowingNativeBatch.LaunchWithExternalMouse()`**. Ordinary `Launch()` remains unattended and makes no mouse acceptance claim. At the tonic preview, the driver reports `externalMouse.stage` and the current `_throwControlsRect` bounds, then waits for actual GameView clicks to produce **Ground → Body → Focused → Broad**. It checks the observed modes, unchanged source/cursor and zero payment, then resumes normal controller checks. Each transition has a90-second bound, under the existing six-minute overall watchdog. The driver does not call toggle methods or mutate the selection. Capture/control positions are read from current rendered state rather than fixed screen coordinates. Timing for this variant includes real operator input waits.


## Final code-level regression gate

[final-death-visibility-regression-green.xml](final-death-visibility-regression-green.xml) reports **1330/1330 native Unity EditMode cases passing**, zero failures,339.8seconds. This includes lethal self-splash settlement before the death modal and restored stable load, alongside the surviving contact-stun control. These are code-level regression results. The final controlled routes and actual mouse gate remain distinct live evidence, described below. No physical Deck/performance claim follows from this result.


## Final unattended receipts and independent pixel finding

- [1280×800 report](Native/2bd74b3e3b3a41ee9e910de64b1d0aac/report.json): complete=true,31/31 checks, zero failures/errors,40.36seconds, eleven captures.
- [1280×720 report](Native/3f46a1702fa2477c94a8e5a38ff28644/report.json): complete=true,31/31 checks, zero failures/errors,43.39seconds, eleven captures.

Both prove the scripted state/payment/custody/save route. Independent inspection of the720 captures finds the target controls and focused splash visible and inside the screen. However, `03-measured-sources.png` and `08-recovery-source.png` contain **no source menu**, although their recorded state is `ThrowPopupOpen`. This is a real adapter defect: `HandleAbilityManagerInput` calls `CloseAbilityManager` after the activation callback; that method preserves the new state but unconditionally restores the gameplay camera, disabling the overlay enabled by the new source picker. The script could navigate its invisible rows, so its31 state checks do not establish pixel acceptance. The capture waits0.2seconds and end of frame, excluding an immediate-before-render capture as the explanation. This finding requires a narrow transition regression/fix and fresh captures before visual acceptance.

The720 timing sample records29,442frames over41.83seconds, p50/p95/p99=1.21/1.63/3.04ms, maximum4384.74ms; the800 sample records26,996frames over38.49seconds,1.43/1.93/2.69ms, maximum477.37ms. These are observed Editor-route durations only. The large720 maximum is retained, without unsupported attribution to gameplay, rendering or GC; these samples do not establish smooth physical Deck performance. Both runs have `externalMouse.requested=false` and make no real-mouse claim.


`source-popup-visibility-red.xml` confirms the camera handoff regression: three native cases, two controls passing and one expected failure after actual ability-manager activation closes into `ThrowPopupOpen`. The minimal correction preserves that replacement modal's camera and defers announcements until its selection ends. Normal manager cancellation and directional targeting retain their gameplay-camera behavior. The live driver now independently records active popup camera, active nonempty foreground tiles/renderer and matching culling layer at captures03/08; its expected check count is33. Updated GREEN and fresh visible-menu captures are still required, and these structural checks do not replace pixel inspection.


[source-popup-visibility-green.xml](source-popup-visibility-green.xml) now reports **72/72 native cases passing**, including the new replacement-modal regression and neighboring cursor, camera, modal and ability tests. This closes the code-level camera handoff finding. Fresh33-check live routes and independent source-menu pixel inspection are being run separately.


## Corrected live acceptance

After the camera fix, both complete runs pass **33/33 checks with zero failures/errors**, including the two added source-overlay checks:

- [1280×800,7ef6804d5a204abe9bffbd592a900f43](Native/7ef6804d5a204abe9bffbd592a900f43/report.json):39.76seconds and eleven captures.
- [1280×720,d1e9a12ed2484c33ac5629b9d8d97299](Native/d1e9a12ed2484c33ac5629b9d8d97299/report.json):39.02seconds and eleven captures.

Independent inspection of captures03 and08 in both runs confirms readable, visible, centered source and recovery menus with distinguishable selected rows. They supersede the earlier31-check captures for the corrected menu behavior. The camera fix does not change payment, custody, targeting or save mechanics, and all earlier route checks remain passing. Actual mouse-button use still requires the external gate; both corrected runs are unattended.

The corrected800 route timing samples28,543frames over38.21seconds, p50/p95/p99=1.30/2.01/2.86ms, maximum246.70ms;720 samples31,621frames over37.51seconds,1.09/1.69/2.50ms, maximum145.43ms. These preserve the same Editor/input/capture/save scope limits above and cannot establish Deck performance.

The1330-case broad sweep and72-case focused camera sweep contain **1385 distinct passing test-case full names** (55focused names were outside the broad selection). This is a verified union of the two receipts, not a claim that1385cases ran together after the final narrow camera change.


Final focused source review found no further concrete regression in the camera fix. AbilityManager closes its old tiles before the activation callback draws the replacement; the new guard preserves only `ThrowPopupOpen`. Ordinary close and directional activation still restore gameplay. Skill source cancellation clears its tiles/camera and returns Normal; skill targeting cancellation also returns Normal; inventory targeting returns to the same item popup and world targeting restores its exact Look cell. Pending announcements remain queued while source/target selection owns input, then the normal-input announcement check opens them after cancellation or paid resolution. No additional source edit followed this review.

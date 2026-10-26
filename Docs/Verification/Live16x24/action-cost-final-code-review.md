# Final code and test review: world-action prices and native pointer

2026-10-26. Scope: saved `WorldActionMenuUI.cs` diff and the new `WorldActionCostReadabilityTests.cs` and `WorldActionMenuPointerTests.cs` fixtures. This pass was read-only apart from this scratch report; no Unity, player, test execution, Git operation, or project edit was performed. The reviewer authored the pointer fixture earlier; production implementation was authored by root.

## Conclusion

No material correctness blocker found in the bounded implementation. It resolves two separately established defects: fixed-width clipping of authored action costs and a legacy/native pointer mismatch found during ordinary mouse replay. No gameplay recipe, price, action identity, world state, artwork, or camera calibration change is introduced.

## Production assessment

- Width is computed once from all actions at `Open`, including offscreen rows, with the same `Display ?? Name ?? ""` fallback used by rendering. It stays between48 and78 columns. All reviewed draw, clipping, centering, arrow, background, and pointer bounds use the same width. Short menus retain their previous footprint; scrolling does not shift or resize the menu.
- Recording and clearing the prior foreground rectangle before recentering prevents residual glyphs when switching to a narrower or shorter menu. Close and reader hiding clear the actual old footprint. The already-recorded background follows the same ownership boundary. Absent optional backing remains supported.
- `ReadPointer` snapshots one enabled native `Mouse` and reads both its position and left-button press edge. Missing or disabled native devices use the corresponding legacy pair. `Open` seeds the parked-pointer sample from the same source. `HandleInput` captures once, stores the sample before hover/click dispatch, and the private hit-test reads that captured position. No native-edge/legacy-position mixing or midframe backend reread remains.
- The existing keyboard/controller ordering, logical action rows, shortcut identity, selected target/cell, reader return, outside-click cancellation, and twelve-row scroll limit are preserved. Moving the pointer can reclaim hover; a parked pointer cannot repeatedly undo keyboard navigation. A mouse activation leaves `SelectedActivationKey` as `None`.
- Empty/null action lists still render the established empty-menu row. Null Display/Name fields retain their existing fallback. Null elements within a populated action list were already unsupported by the render loop and are not newly promised here.

## Native evidence independently inspected

Parsed `Docs/Verification/Live16x24/action-pointer-green.xml` directly. It contains **115 distinct cases, all Passed, zero failed/inconclusive/skipped**, start20:23:22UTC and end20:23:35UTC. It includes all21 cost-readability cases and all13 pointer cases. This completed native XML is affirmative test evidence despite the separately retained startup-watchdog report; the watchdog is not evidence of115 test failures.

The pointer RED receipt records13 failures before the pointer implementation:10 behavioral failures and3 missing-helper contract failures. The first zero-discovery attempt was invalid because the newly supplied metadata GUID had33 characters; root corrected it, and the current GUID validates as32 lowercase hexadecimal characters. That metadata authoring error belongs to this reviewer and is not counted as behavioral RED.

The earlier width suite's first mouse fixture had an invalid assumed viewport center; the repaired fixture asserts actual screen-to-grid projection before testing bounds. That original pointer failure alone was not independently diagnostic. The exact50-character meal-label clipping was directly reproduced by painted-text assertions.

## Fixture quality and bounds

- Cost tests inspect emitted Tilemap glyphs,48/78-column boundaries,40/41/42 and71/72-character transitions, explicit overflow, retained height, offscreen longest choices, scrolling/absolute selection, old-footprint cleanup, close, and reader lifecycle/highlight/hints.
- Pointer tests queue real Input System MouseState events through saved `HandleInput` and project points through an owned camera/render target into the actual painted grid. They cover short/wide menus, the expanded right edge,1280×800 and1920×1080 targets, hover/right-button nonactivation, outside/border/footer cancellation, held/repress behavior, stationary-open keyboard navigation, coherent backend pairing, and missing/disabled native fallback. Countercases include subsequent successful clicks, avoiding blanket-ignore implementations.
- Owned render targets are detached, released, and destroyed; inherited cleanup destroys owned GameObjects and restores the Input System. No legacy pointer is mocked or overwritten by these tests.
- Dressing and ink labels in the width fixture are synthetic long-cost strings, not the authored recipes. The actual authored fixed-cost lengths were source-audited separately. Do not turn the synthetic cases into gameplay claims for those recipes.
- Fallback tests compare the actual returned legacy pair; they do not synthesize or prove a physical legacy mouse. The connected-gamepad hint test does not prove physical Steam Deck input. The width fixture's reader test exercises menu lifecycle; the neighboring reader suite and actual root replay provide broader integration evidence.

## Actual replay and remaining limits

Root reports ordinary CUA replay now selects the expanded meal action, pays the genuine two-dram cost40→38, advances tick4860→4870, and shows Working. Root also reports a short-menu Examine click opens its actual110-tick reader. Corresponding saved evidence uses `action-cost-04-mouse-commissioned`, `05-short-menu`, and `06-mouse-reader`. This code-review pass did not operate Unity or independently inspect those image pixels; root's visual/interaction acceptance remains separately attributable.

Dynamic labels exceeding71 characters still use explicit `~` truncation. The existing world F1 reader discloses full current action text; generic choice menus do not thereby gain universal detail disclosure. No claim is made for physical Deck comfort, physical legacy-device delivery, every other popup/world pointer path, or updated packaged binaries. Root is finalizing living records with those version and evidence boundaries.

## Reviewed saved-source hashes

- `WorldActionMenuUI.cs`: `ce5bc7accaa6714e0b380f0999279b3467833ea91438fa54042b4a0143e4cda8`
- `WorldActionCostReadabilityTests.cs`: `337af05c2b4e516e99b903a204a4bccf50254834e6f9203979a1dc6a03648aaa`
- `WorldActionMenuPointerTests.cs`: `12c10318ab37518556e8858352c76b34b10beb382cf1d9bf4870ba3171d603f3`

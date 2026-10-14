# Composable builds — five review passes

Requested 2026-10-13 after local storage cleanup and the parry-label follow-up. Scope is the composable-build feature series described in `COMPOSABLE-BUILDS.md`: ranked purchases, current saves, Deflect/En Garde/Riposte, strike provenance and reaction bounds, full-screen skill presentation, worksite roles, the Warden opening, and comparison/input evidence. Legacy-save migration remains explicitly out of scope.

## Plan and acceptance

1. **Combat and reactions:** trace otherwise-hit defense, guard consumption/fallback, perception/equipment eligibility, counters and follow-up suppression, interrupted multi-strikes, allowance reset and action payment. Reproduce substantive defects with a failing test before repair.
2. **Ranks, purchases and persistence:** check one authoritative skill part, rank metadata, atomic upgrade/acquisition/refund, skill removal/regrant, current-save validation before publication, and ability identity/state retention.
3. **Presentation and controls:** enumerate actual parry providers; distinguish current defense from passive/activated benefit for each; check rank previews, ownership, unavailable equipment/status, purchase review, hotbar and overview consistency. Riposte grants counters, not parry probability.
4. **Encounters, starts and source integration:** check local guard behavior against generation/source budgets, perception and peaceful/party/flee state; inspect Warden kit, ordinary acquisition, source restoration and existing-start stability.
5. **Final integration and cold review:** read repairs against their neighboring systems and documentation; rerun a bounded native regression selection at the final source state. Verify archive/source integrity and main synchronization. Record limits without treating unrelated full-suite, physical Deck or blind-player gates as satisfied.

Independent initial reviews may run in parallel; production changes and test runs are coordinated so each failing test is meaningful. Each pass records findings (or the concrete contracts checked), repairs, counter-checks and evidence. Review passes do not require invented changes when the implementation is correct.

## Cleanup prerequisite

The user confirmed Unity was closed. Verified the editor process was absent and `origin/main` included the preservation archive, then removed `Library`, six obsolete diagnostic build directories, and two superseded partial backup chunks. Current playable builds/archives, source, saves and diagnostic receipts remain. The old clean release worktree is being retired through the managed archive tool, not a manual recursive delete. Exact cleanup evidence is in `Verification/StorageCleanup-2026-10-13/closed-editor-cleanup.json`.

Native tests after deleting Unity's cache will regenerate the necessary import/compilation cache. This cost is separate from deleting obsolete build caches and does not justify deleting source or saves.

## Results

Completed all five passes, with independent cold review of the repairs. Native Unity EditMode: **66 tests / 17 expected RED failures / 49 passes**, then **329/329 GREEN** across 15 targeted fixtures. Receipts: `Verification/ComposableBuilds/review5-native-{red,green}.xml.gz`. An earlier command-line startup attempt in `review5-red/` exited because the editor had reopened; it is not test evidence.

| Pass | Contracts reviewed and result |
| --- | --- |
| 1 — combat | Otherwise-hit gating, guard consumption/fallback, perception/equipment, counter provenance, allowance reset, Flurry/Whirlwind interruption and action payment. Fixed player grammar (`You parry/catch`, `your strike`) with six actual-resolution narration cases; no mechanical change. Clarified selected-weapon counter versus nested full-weapon-gathering scratch ownership. |
| 2 — ranks/save | 🟡 Acquisition and teardown could both throw, leaking an exception out of purchase recovery. Contain teardown failure, preserve both error types, refund once and release transaction gate. 🟡 Two individually valid copies of one skill class could load; reject duplicates during graph decoding before globals/hooks. Tests include subsequent successful purchase and distinct valid rank owners. No save-version or migration change. |
| 3 — UI | Deflect is the only additional parry provider. Add separate current/preview passive benefit from shared combat percentages; preserve current equipment/stun warnings and En Garde active total. Fifteen cases cover rank/guard combinations, unowned preview, both browser modes, purchase refresh and non-provider controls. Riposte remains counter chance only. Checked 45-column detail wrapping/paging and non-mutating previews. |
| 4 — world/start | Reviewed guard opt-in, home/zone/radius, party/peace/flee, visible target acquisition and bounded return/detour movement; staged yard/forge source budgets and restoration; Warden root skills, weapon, consumables and earned defense ranks. Existing encounter, worksite and starting-build suites passed; no new substantive defect found. |
| 5 — integration | Independent cold read of repair diffs and neighboring hooks, publication order, UI shared values, no doubled refund/guard percentage. All 329 selected native tests passed; no compiler errors after refresh. |

### Self-review and limits

- 🟡 Purchase and duplicate-save findings fixed before commit; positive tests have flag-flipped controls.
- 🔵 Player narration fixed locally; no global message replacement or combat probability changes.
- 🧪 An initial cold-import TagManager parser warning predates this feature. The same file at feature base has 30 slots; live Unity resolves World=8, Sidebar=9, PopupOverlay=10 and Hotbar=11. No runtime layer failure reproduced, so no speculative settings change in this patch.
- Can verify: native EditMode behavior, purchase/save boundaries, generated worksite/start contracts and UI model text/flows.
- Cannot verify from this run: visual feel, physical Steam Deck operation, blind-player discovery or the full project suite. Prior evidence remains separately described in `COMPOSABLE-BUILDS.md`.


## Next task, after these five passes

User requested a floating text cue when an attack is parried. Design and implement it after the current review cycle, using a reusable event-text mechanism suitable for other combat/world events rather than a parry-only rendering path. Inspect existing floating combat text, 3D rendering, visibility rules and pooling before choosing the integration. Require real parry/non-parry controls and retain current combat behavior. Complete three additional review passes covering event correctness, presentation/lifecycle/visibility, and final integration/performance before delivery.

Follow-up scope: include other truthful combat outcomes and initial status-effect application to the player and visible NPCs. Examples: Parried, Stunned, Poisoned, Frozen and Burning. Emit only after an outcome/application succeeds; routine duration refreshes, per-turn ticks and rejected applications must not claim a new status took hold. Reuse the same event-text path with readable placement, bounded density and existing fog/visibility rules. Audit effect application/force-application and combat outcome seams before finalizing supported messages.

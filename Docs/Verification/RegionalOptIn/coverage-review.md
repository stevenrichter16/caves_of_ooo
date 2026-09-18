# Staged R3 review after R2 integration

Read-only source comparison against live `4aab2aac`; no Unity/compile run. R3 fixture changed only under this `/tmp` folder. Current source APIs still support the staged fixture: public native command path, gathered actions, detached zone manager, actual Part fields, MessageLog, DiagQuery, and full GameSessionState serialization. Fixture setup and save roundtrip match already-shipped native regional tests. The source cache is deliberately prewarmed in rollback tests; cache hydration is not claimed transactional.

The new `accept` command remains intentionally absent in production at staging time. These should become assertion REDs, not missing-type compiler failures. The twenty-one cases are nine test methods with 5+2+2+2+2+1+3+1+3 cases respectively.

## Design decisions to retain

Reading an **inactive** offer must say it is not accepted; reading an **active** offer must acknowledge that it is already active. The old plan's unqualified “say not accepted” sentence needs that state qualification (root owns the live doc). Reading never replaces an existing active/released/completed note in this agreed slice. A current cached-source observation may be shown in the ephemeral preview; persistent active notes remain their recorded interaction-time snapshot. No existing test explicitly requires refreshing active note text after later source depletion.

Cold sources remain unknown. Present cached owners must not all be called unavailable. Harvested crop owners remaining as stubble must not be called available. Finite cargo viability stays on acceptance, with exact ownership/index/save safeguards unchanged. An unavailable recovery can still be explained by a legitimate nearby recipient without an Available cue.

The R2 outcome fixture is now installed and contains **13 cases**, including the added Beating recovery/no-invented-bank countercase. The older copied R3 plan still calls it pending/twelve; update that current-status text during implementation, retaining the original dated review as history.

## Exact existing read calls: intentional classification

Paths below are under `Assets/Tests/EditMode`; line numbers refer to the current R2 source. Keep ordinary `RegionalSituationNotes.Read(player)` calls unchanged throughout; they read the journal and are unrelated to the action rename.

| File | Change to explicit acceptance | Keep read / special treatment |
|---|---|---|
| `Gameplay/Storylets/RegionalSituationTests.cs` | Setup/action lines 162,186,205,227,255,257,303. Viability refusal 325 becomes accept. | Keep gathered read row 302 and add accept row. Keep repeated active read 308; strengthen to unchanged note rather than treating it as another acceptance. Before 303, a real read may be exercised with no note, then accept records the note. |
| `Gameplay/Storylets/RegionalSituationAdversarialTests.cs` | Setup lines 64,92,106,118,128,143,154,174,187,230,245,320. Present/destroyed cargo viability at 316 becomes accept. | Retain every existing transaction/ownership/habitat/cargo fault; pure unavailable preview is a separate positive assertion, not weakened acceptance. |
| `Gameplay/Storylets/RegionalSituationUsabilityTests.cs` | Setup lines 41,58,77,94,113. | The exhausted-supply note at 77 is the **initial acceptance** after an observed removal, not a later active refresh; preserve its real exhausted-note assertion. |
| `Gameplay/Storylets/RegionalSituationPresentationTests.cs` | Commitment after lines 36 and 62; initial active-note setup 69. | Keep read-row assertion 60 and distant refusal 82. Prefer real read followed by explicit accept at 36/62 so the cue stays Available after preview and becomes Active only after acceptance. |
| `Gameplay/Storylets/RegionalSituationCargoLifecycleTests.cs` | Eligibility 74,81 and commitment 83 become accept. | Preserve dropped-away/restored cargo lookup and no-replacement controls; read can now explain loss. |
| `Gameplay/Storylets/RegionalSituationContainerLifecycleTests.cs` | Eligibility 73,79,80,92 and commitment 94 become accept. | Preserve actual native Put/Take, nested owner and save-roundtrip counterchecks. |
| `Gameplay/Storylets/RegionalSituationCargoRollbackTests.cs` | Eligibility 74,85,93 and commitment 95 become accept. | Preserve the failed Taken callback's real rollback and later recovered same-owner control. |
| `Presentation/Input/RegionalSituationWorldActionTests.cs` | Delivery setup 83 becomes accept. | At 67 run read then accept then release: three successful callbacks, zero turns. Keep stale-read row 114 and parameterize a stale-accept row too. Keep ordinary Examine's independent dispatch. |
| `Gameplay/Storylets/RegionalSituationOutcomeTests.cs` | Initial setup 50,62,83,102,114,126,140,150,153; reacceptance 109 must become accept. | Insert a read before 109 and assert the released receipt remains unchanged; only the explicit accept restores the active note. Preserve all 13 R2 cases, including real filter stock/no fabricated wetland-bank result at 83. |

Native scenario `ChunkGameplayNativeAudit.cs:239` currently reads then asserts Active. Split read/accept gates and capture truthful preview before the active request. Retain R2 trade inspection and reopened historical receipt; do not shorten the current native path to the older pre-R2 route. Batch required gates and runner scope text need the new route's actual counts, not guessed replacements. Root owns these edits.

## Remaining coverage boundaries

The staged fixture uses public native command dispatch, not the private InputHandler C-menu seam. The existing world-action fixture and native walkthrough remain mandatory for zero-turn/menu-return/UI evidence. Existing dead/hostile/forged/graph/cargo tests should retain their meaningful controls with the new accept command; this nine-flow fixture does not replace them. No live feel, visual typography or gameplay balance is certified by source review.

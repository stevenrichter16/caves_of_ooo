# Measured Throw: retained follow-up

Status: implemented, reviewed and verified in Unity, 16 October 2026. User reported selecting an item and waiting
without a throw. Explicit request: keep the prepared item and target selected,
make the next confirmation clear, and test keyboard and Steam Deck controls.

## Verified contract and plan

This is CoO interaction design, not a Qud parity claim. Preserve the two paid
actions and +4 penetration/cap rules. Only remove redundant selection steps.

| Sweep finding | Implementation decision |
| --- | --- |
| Selecting an item only opens the aim cursor | Phase1 clearly says confirm to prepare; preparation itself spends the turn. |
| `ConfirmThrowTarget` exits to Normal after preparation | Keep the same pending item and target, switch to Ordinary mode, and require a fresh confirmation for the throw. |
| `EndTurnAndProcess` synchronously runs NPC turns | Retain identity across the paid turn; revalidate before follow-up after normal rendering/FOV has caught up. Never use the FX payment branch to pay preparation a second time. |
| `RefreshThrowPreview` normally resolves whichever entity occupies the cursor cell | Guided follow-up locks the original entity. Track only a currently visible occupied cell in range; never substitute a replacement or reveal a hidden target. |
| Preparation validity excludes target visibility/range | Explicitly check these when showing/confirming follow-up. Source loss, interruption, actor/zone replacement or target loss dismiss without extra cost or refund. |
| Keyboard Enter and Deck A/RT share confirmation | Require release before the next confirmation. Cancellation remains free; no automatic launch while holding confirm. |
| Current Escape cancellation preserves gameplay preparation | Preserve this behavior; backing out does not refund preparation or cooldown, and ordinary throw remains possible on the same next action. |

1. Run failing input tests covering retained identity, both paid actions,
   keyboard/Deck press-release behavior, cancel and stale source/target controls.
2. Add the smallest input-state continuation and clear phase1/phase2 prompts;
   keep gameplay damage, cooldown, recovery and save semantics unchanged.
3. Run affected input/throwing/controller tests, independent review, and fix
   actionable findings. Update the existing isolated native scenario to use
   direct keyboard and controller confirmation and inspect rendered prompts at
   1280×800 and 1280×720.
4. Record exact results and limits; restore editor/save settings; commit with
   the living doc, fetch/rebase and push main.

Readiness: existing skill, ordinary throw, cursor, input mappings and isolated
native launcher are available. No new assets or combat mechanic are needed.

## Implementation

The pending cursor retains the exact item, target entity and preparation serial.
After the paid preparation it becomes an Ordinary throw with a release latch:
holding Enter, A or RT cannot trigger the follow-up. Phase1 says confirm to
prepare; phase2 says **READY TO THROW — confirm again; no wait needed**, with
keyboard or controller controls. The sidebar repeats the complete action hint
before longer trajectory details. The skill tree description explains both
actions. The same target remains selected if it moves to another visible cell
within range; directional/ground retargeting is locked during this continuation.

The next input frame revalidates after normal FOV rendering. Invalid preparation,
source, owner, visibility, health or range dismisses the continuation with an
interruption message and no launch/payment/refund. Ordinary ThrowItemCommand
still validates and owns extraction, damage, landing and payment. Cancel exits
the guided cursor but retains the already-paid gameplay preparation for an
ordinary throw on the same next action, as before. No damage/cooldown changes.

The existing controlled native scenario now uses direct second confirmations;
its prior preparation save check cancels first, then saves/loads and throws via
the ordinary path. The natural expedition harness was adapted to retain this
cursor through its paid-action helper and cancel freely when its strategy
declines the opportunity. Other callers retain the helper's original behavior.
This compatibility edit is not a claim of new natural-campaign acceptance.

## Verification and self-review

| Gate | Result |
| --- | --- |
| RED before production edits | Real Unity job `3779269a53884cf18c3e935039eebcf2`: 25 completed, 10 expected failures because the cursor was discarded, 15 controls passed. `Verification/Throwing/measured-followup-red.json`. |
| First GREEN | Job `37ca72db060643b59328152c30120a03`: 109/109 passed, no failures/skips. Input, skill, Hurler start, payment boundary, visible source and controller suites. `Verification/Throwing/measured-followup-first-green.json`. |
| Reviewed GREEN | Job `cc5db482620c418cb2ac9bb67b651630`: 156/156 passed, no failures/skips; adds throw lifecycle and mutation/combat adversarial coverage. `Verification/Throwing/measured-followup-reviewed-green.json`. |
| Initial native 1280×800 | `Verification/Throwing/Native/54b5ab17c14c4eaf96e3d825eafafcfe/report.json`: 40/40 passed, 0 failures/errors. Independent visual review caught one grammatical typo and a clipped duplicate sidebar hint. Both polished before final captures. |
| Final native 1280×720 | `Verification/Throwing/Native/062a78919efe4c3fb62ba3d350674e3a/report.json`: 40/40 passed, 0 failures/errors. Preparation and keyboard/controller ready prompts inspected; independent review confirmed complete instructions fit both the sidebar and bottom panel. |
| Final native 1280×800 | `Verification/Throwing/Native/02119c76754e46d5b7c3db96e1352d6e/report.json`: 40/40 passed, 0 failures/errors. Final polished preparation/ready prompts inspected at Deck resolution. |
| Editor restoration | Play stopped, native save isolation inactive, save-root override cleared and native exit code 0. Original GameView selection restored and audit-owned sizes removed. Final console check returned 0 errors. |

`ThrowCursorInputTests` now has 36 cases (19 added, two existing preparation
expectations updated). Positive/counter-check pairs cover same-frame duplicate
handling, held/released/fresh keyboard Enter and controller A/RT, exactly one
action each for preparation/launch, free cancellation, exact source/target
replacement, visible target movement, target death/hiding/range, player
displacement, locked-versus-ordinary retargeting, phase/device prompts and `.`
not paying or auto-throwing while ready.

- 🔵 Independent review confirmed synchronous scheduler handling and no second
  preparation payment; retained entity identity prevents occupant substitution.
- 🔵 Fixed wording from “enemy” to “target”: neutral entities remain selectable,
  so the UI must not imply a hostility safety restriction.
- 🔵 Avoided creating a new retained intent every frame and removed the temporary
  equipped-list/set allocation from the existing custody check, with equivalent
  dictionary membership semantics verified by the broader throw tests.
- 🔵 Native screenshot review moved the follow-up control hint before verbose
  sidebar details and corrected “an target” to “a target.” These are UI-copy/
  ordering changes after the 156-case run; final native captures cover them.
- 🔵 Final independent review has no open correctness or layout findings. The
  two final native routes each verify one paid preparation, a distinct second
  confirmation and one paid launch through the production keyboard/controller
  handlers. Held inputs cannot launch automatically; B cancels for free.
- 🧪 Can verify: named Unity test assertions, controlled native real handlers,
  exact per-action costs, item custody and the inspected rendered prompts.
  Native controller input uses Unity Input System devices through the actual
  mappings, not direct command calls.
- 🧪 Cannot verify: physical Steam Deck/Steam Input hardware, performance or
  unaided discovery; no new natural expedition or balance comparison. The
  native fixture has an inert target and explicitly authored test supplies.
  Mouse world confirmation is not newly proven by this keyboard/controller
  request. Existing mouse-mode-button evidence remains historical.

Files: production `InputHandler.cs`, `InputHandler.Throwing.cs`,
`ThrowingRules.cs` (allocation-only custody query), `ThrowingSkills.cs`,
`Data/Skills/Throwing.json` (only Measured description changed); test
`ThrowCursorInputTests.cs`; native drivers `ThrowingNativePlayer.cs`,
`SpreadDiscoveryNativePlayer.cs` and `.ThrowingContinuation.cs`; this doc,
parent throwing plan and verification receipts/captures. No new Unity assets
or `.cs` files were required.

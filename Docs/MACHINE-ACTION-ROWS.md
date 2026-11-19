# Machine action rows: declared turn cost and disabled rows

Living record for slice S3 of the machine architecture spec. Date 2026-11-18.

## Problem

A new world or pack command had to be added to two hand-written `||` chains (`InputHandler.ExecuteWorldActionSelection`, `InventoryUI.ExecuteItemActionWithActivationKey`) or it silently ran without a turn (or, in the world
menu, was refused through the generic dispatcher). A row that could not be used right now simply vanished, so a player could not tell "there is no forge here" from "the forge is fouled and needs repair first".

## The contract

`InventoryAction` gains three additive fields (`Assets/Scripts/Gameplay/Inventory/InventoryAction.cs`):

- `Time : ActionTime { Unspecified = -1, Free = 0, OneAction = 1 }`, default `Unspecified`: the legacy chains decide, exactly as before.
- `DisabledCode` (a stable token) and `DisabledReason` (one sentence). `IsDisabled` is true only for a non-blank reason (a whitespace-only reason is not disabled).
- `InventoryActionList.AddAction(...)` now returns the row (and takes an optional `time`); `AddDisabled(name, display, code, reason, key, priority)` lists a row that cannot be used. A disabled row's command is `Disabled:<code>`, so no consumer ever
  reads a null command and nothing can dispatch it; a blank code becomes `unavailable` and a blank reason becomes a generic sentence rather than an enabled dead row.

Behaviour:

- **World menu.** A disabled row is listed dimmed (even when highlighted). Enter, click, controller and hotkey all go through `SelectAction`: a disabled row logs its reason, records `event/ActionDisabled { code, command, name }`, selects
  nothing and keeps the menu open; a disabled hotkey does not arm `SelectedActivationKey`. `ExecuteWorldActionSelection` carries the same guard defensively. A declared row (non-empty command, not disabled) is run once after the reach gate:
  `PerformInventoryActionCommand` in the shared transaction, `OneAction` charges exactly one action on commit (`Free` and a refusal charge nothing), then the menu returns. Undeclared rows fall through to the unchanged chain.
- **Pack menu.** `ItemAction` copies `Time` (with a separate `TimeDeclared` flag, because a struct defaults to `ActionTime` 0, which is `Free`), `DisabledCode` and `DisabledReason`. A disabled pack row is dimmed, labelled "(unavailable)" (both renderers draw only the
  label) and does nothing but report and keep the popup on the same row. The legacy charge test moved verbatim into `LegacyPackActionSpendsTurn` and is evaluated only for a committed command and an undeclared row, as before; a declared
  row's own time decides otherwise.
- `Disabled` wins over `Time`. The legacy chains are not deleted; producers migrate by declaring `Time`.

## Evidence (native)

`ActionTimeDeclarationTests`, 40 cases through the real `InputHandler` world menu and the real `InventoryUI` popup (the same reflection fixtures the oil, composite and rig suites use):

- declared `OneAction` charges one 10-tick turn; the same row undeclared runs but charges nothing (the gap); declared `Free` beats a legacy chain that charges `Harvest`; an undeclared `Harvest` still charges one turn (control); a row that does not commit charges
  nothing; the reach gate still applies; an empty command falls through instead of dispatching;
- a disabled row is listed, selecting it logs the reason with no turn, no transaction and the menu still open; the enabled neighbour still selects (control); a disabled hotkey behaves like Enter; `FireOnActor` does not change that; disabled wins over a declared time
  in both menus; the diag pin carries code, command and name; nothing records when nothing is disabled;
- model: defaults, blank reasons, blank codes, duplicate names, a mutated row after sort, hotkey sharing;
- the legacy pack charge table is frozen (`FillWaterskin`, `DrinkWaterskin`, `Cook`, `LightTorch`, `ExtinguishTorch`, `Harvest` charge; `Examine`, `Take`, an unknown command do not).

Adjacent suites run unchanged (1,107 cases across machine, inventory ledger, examine ledger, world and pack menu, alchemy still, audit and retention suites): 1,106 pass; the one failure,
`GameAuditSplitIdentityAdversarialTests.OuterFailureRestoresExactSourceWithoutLeavingReachableClone("throw", true)`, fails identically on HEAD (it is in the recorded baseline).

## First consumer: the field work rig says why

A rig that is not ready lists no craft rows, so standing beside a fouled rig showed repair, haul and dismantle rows and nothing about the workshop. `ForgePart` now adds one disabled row, "work at the rig", for an actor within one cell
whenever `StationReadiness.Evaluate` fails, with a one-sentence reason per readiness code (`faulted`: "The rig is not sound. Repair it before working at it."; `hauled`; `harvested`; `destroyed`; anything else a generic sentence).
It sorts at priority 5, after every usable row and before Examine, so the menu opens on a usable row. A ready rig, a rig out of reach and an ordinary forge add no such row (counter-cases in `MachineRigDisabledRowTests`, 12 cases; the existing rig suite
still passes). Editor play at the Peat Works rig confirmed the dimmed row, and that Enter on it logs the reason, spends no turn and keeps the menu open (`Verification/MachineSystems/InstallationsActual/rig-disabled-row-*.png`); that play had the row at
priority 100, which started the cursor on it, so it was moved to 5 and covered natively.
The reason sentences are plain mechanical messages in the style of the rig's existing refusals ("The rig must be sound, released and within reach."); they name no world noun.

## Limits

- The process rows' reasons (S12) are a separate slice that uses this mechanism.
- Rounds (multi-action costs) wait for a second consumer; only `Free` and `OneAction` exist.
- The world menu has no detail line for a disabled row yet beyond the log message; the pack menu says "(unavailable)" in its label.

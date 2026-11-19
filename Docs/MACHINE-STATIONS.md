# Machine stations: one contract for "a machine you stand next to"

Living record for slice S4 of the machine architecture spec (see [MACHINE-INSTALLATIONS.md](MACHINE-INSTALLATIONS.md) for S1/S2/S6/S7). Date 2026-11-18.

## Problem

Three independent 3x3 scans decided whether an actor could use a machine: `ForgeWorkReceipt.Resolve` (strict readiness, actor read through its cell), `AlchemyStillPart.IsNearStill`
(presence only, actor read through its position) and `WorldAudioWork.Forge` (an audible bench, its own capture rules). The field work rig was special-cased inside the generic
`ForgeWorkReceipt.Ready`. A new kind of bench would have meant a fourth scan and another edit to core code.

## The contract

Stations are found by what a Part provides, not by its concrete type (`Assets/Scripts/Gameplay/Stations/`):

- `IStationCapability { Kinds, Reach, Readiness }` on a Part: `ForgePart` provides `forge` and `cold-work` (strict, reach 1), `AlchemyStillPart` provides `still` (presence only, reach 1).
- `StationReadiness.Evaluate(owner, zone, kind, out code)`: the forge's strict clauses moved verbatim (current zone; a placed owner whose cell round-trips; exactly one parented provider of the
  kind; no repair fault; not hauled; not harvested away; not destroyed), then every `IStationReadinessGuard` Part on the owner. `ForgePart` is itself the guard that carries the rig-payload clause, so the
  generic Part no longer names the rig from the receipt. Stable codes: `stale-zone`, `cell-mismatch`, `no-provider`, `two-providers`, `faulted`, `hauled`, `harvested`, `destroyed`, plus guard codes (`rig-payload`).
- `StationQuery.Resolve / Near / NearPresence / Collect`: one scan order (dx outer, dy inner, then the cell's object order) so existing tie-breaking is unchanged; `Collect` lists unready stations with
  their block code (the basis for a future "blocked because" row); `NearPresence` keeps the still's older rule bit-for-bit.
- `StationAccess.Allowed`: the permission seam. It loops `IStationAccessGuard` Parts on the station; with none present it allows, so every shipped station keeps its open, communal behaviour. Enforcement
  (terms, fees, gifts) is a later slice after its independent design critique.
- `StationReceipt.Begin`: the forge's receipt generalised. Access is asked before any claim; the exact station is retained through the commit callbacks (a station that moves, fouls or is replaced refuses the
  commit); a refusal records `furniture/StationRefused { code, kind, command }`.

`ForgeWorkReceipt` and `AlchemyStillPart.IsNearStill` are now one-line forwards. `WorldAudioWork.Forge` is deliberately not migrated (it answers a different question).

## Evidence (native)

- The legacy `Ready`, `Near`, `Resolve` and `IsNearStill` are copied into `StationQueryParityTests` as ORACLES (internal helpers reached by reflection) and compared with the contract over 13 station states
  (ready, faulted, repaired, hauled, harvested, unharvested, gone, hp0, two forge parts, no forge part, rig good, rig bad, forge-and-still) at 25 offsets each (325 layouts), plus two adjacent forges in scan order,
  the same cell, an unplaced actor, another zone and null arguments: identical in every case.
- The still keeps its leniency on purpose (a faulted, hauled, destroyed still is still "near"); the strict contract would refuse it, so a later slice must prove the difference unreachable before it is tightened.
- A test-only bench kind with reach 2, a test-only readiness guard (out of fuel) and a test-only access guard prove a new station kind, veto and permission seam work with no edit to the scanners.
- The receipt is exercised through the real `PerformInventoryActionCommand` transaction: a callback that moves or fouls the station refuses the commit; the control commits.
- Adjacent suites run unchanged and green: forge, alchemy, brew, mineral, salvage, field work rig, reclamation, oil, contained liquid, machine and tinker suites (1,363 tests).

## Limits and next

- The rig's disabled row ("the rig is fouled and needs repair first") needs the disabled-row mechanism (spec S3) and is not done here; the rig still shows no rows when it is not ready.
- `MineralPreparationPart` and `BrewReagentsCommand` still use the weaker presence check; moving every station user onto the receipt is slice S9, whose first step is a probe table (some of its named users already use the receipt).
- A `StationPart` for new benches and a "works here" reader line come with the work-kind table (S13).

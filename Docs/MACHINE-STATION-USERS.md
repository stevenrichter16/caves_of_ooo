# Station users: the probe table for slice S9

Read-only survey, 2026-11-18, of every production caller that decides "is a forge or still here" (HEAD `11c10ba33`). Slice S9 of [MACHINE-ARCHITECTURE-SPEC.md](MACHINE-ARCHITECTURE-SPEC.md) moves every user onto one receipt; this table is its first step, so the change list is evidence, not memory. Contract: [MACHINE-STATIONS.md](MACHINE-STATIONS.md).

| Caller | Station kind | Check | Receipt (`StationReceipt.Begin` / `ForgeWorkReceipt.Begin`)? | S9 action |
|---|---|---|---|---|
| `ForgeWeaponCommand` (Validate, with a tapped station) | forge | `ForgeWorkReceipt.Near(actor, station)` | through `FieldWorkRigRules.Workshop` for the rig; otherwise validate-only | none: the tapped-station path is already exact |
| `ForgeWeaponCommand` (no station named) | forge | `ForgePart.IsNearForge` (`StationQuery.Resolve`) | no | uses presence at validate time; a callback that moves the forge between validate and commit is not refused |
| `ReforgeWeaponCommand`, `TemperWeaponCommand` | forge | same pair as above | same | same |
| `WeaponSalvagePart` | forge | `IsNearForge` then `ForgeWorkReceipt.Begin` | yes | none |
| `MaterialProcessPlan` / `MaterialProcessService` (ForgeCold rows) | cold-work | `ForgeWorkReceipt.Resolve` + `BeginWork` | yes (`Begin`, retained through commit) | none |
| `MineralPreparationPart` | forge | `ForgePart.IsNearForge` | **no** | add the receipt: RED `MineralPreparationRefusesWhenCallbackMovesTheForge` |
| `BrewReagentsCommand` (single and batch) | still | `AlchemyStillPart.IsNearStill` (presence-only) | **no** | add the receipt: RED `BrewRefusesWhenCallbackMovesTheStill`, `BatchBrewRefusesWhenStillSwapped` |
| `PreparationRecipePart` (still recipes) | still | `IsNearStill` at plan time and in the callback | partial (callback re-checks presence) | route through the receipt |
| `FieldWorkRigRules` | forge (rig) | `ForgeWorkReceipt.Current` + `Begin` | yes | none |
| `InventoryUI.Crafting` (readers), `InventoryDecisionDetails` | forge, still | `IsNearForge`, `IsNearStill`, `ForgeWorkReceipt.Near` | no (read-only) | none: readers stay lenient |
| `MaterialProcessMisses` | forge | `ForgeWorkReceipt.Resolve` + `StationQuery.Collect` | no (read-only) | none |
| `SpreadDiscoveryNativePlayer.Mendleaf` (scenario) | still | `IsNearStill` | no (scenario checks) | none |

## What S9 must still prove first

- The still's lenient presence rule (a faulted, hauled or destroyed still still counts as near) must be shown unreachable in shipped play before it is tightened, or kept deliberately (`PresenceOnly`). The camp still and the camp bench make the install state the one new way a station can be present but inert: the `InstallablePart` `Loose` guard already vetoes the forge's strict readiness; the still ignores guards under `PresenceOnly`.
- Each RED test above fails on HEAD today by construction (no receipt means no refusal when a callback moves the station); the access seam (`IStationAccessGuard`) only applies to receipt users, so a service-terms slice must not start before S9.
- Behaviour change to name in the commit: a brew or mineral preparation that today succeeds after an outer callback relocates the station will refuse.

## Not changed here

No code changed; this is the survey that makes S9 a small, evidence-first slice.

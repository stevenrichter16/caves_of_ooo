# Native liquid transfer audit — private readiness

This four-file harness is ready for coordinated publication, but has not run in the Unity editor. Parent owns the editor and publication window. Copy only the four files listed and hashed in `readiness.json`, preserving their new metas. Do not copy compiler outputs or replace shared NativeSaveIsolation.

## Required integration before the run

- The reviewed general-liquid runtime, native InventoryUI route, two blueprints, three guaranteed stock rows, and Morrowfast no-global-factory fallback must be integrated.
- The reviewed exact-owner poured-pool renderer recipe must be integrated before claiming native liquid art coverage.
- The editor must be idle; all existing scenes must be saved. The launcher rejects dirty or untitled scenes.

Invoke `CavesOfOoo.Editor.DensityLiquidNativeBatch.Launch()` for a non-exiting editor run, or the menu **Caves Of Ooo → Scenarios → World → Density Liquid Transfer Native Audit**. `Run()` is the finite exit-editor batch entry point. The driver watchdog is330seconds; launcher watchdog360seconds. Evidence goes to `Docs/Verification/DensityCompletion/NativeLiquids/<RunId>/report.json` with PNG captures.

## What the scenario does

It starts a real seed64 new game through N, buys the generated flask and waterskin from actual Sella stock through native chat/trade, and verifies the moved instances and quoted currency debits. The ordinary Player blueprint starts with50drams/Ego16; each vessel's Commerce value is8. Actual runtime affordability is a required precondition. No items, money or health are granted.

It searches bounded generated/cached zones for real finite water, oil and acid sources. Only player travel is shortened. Source owners, terrain, threats and actor stats remain unchanged; candidate standing cells require a32-cell hostile separation. The real inventory interface must expose and execute each fill/pour command.

The water sequence proves12units drawn,12poured,3drawn into the purchased waterskin and9refilled into the flask. F5 must change the on-disk checkpoint and report success; a real native pour then changes the live volumes. F6 must replace the Player object graph and restore IDs, quantities, original source depletion, position, coins, scheduler state and the unchanged disk hash. Native Examine must show exact current contents without consuming a turn.

An unlike oil source must expose neither a mixed fill nor pour-into-unlike-pool choice and must preserve volume/energy/tick. This is menu refusal evidence; executed stale-selection refusal is covered separately by core tests. Oil and acid are then filled and poured through normal controls and inspected/captured separately.

Completion requires all21named checks, exactly21checks total, no runtime/errors, and at least10screenshots. Partial runs cannot produce `complete=true`. The planned full run produces12captures.

## Limits and restoration

This is scripted native interaction with actual generated supplies, not natural route discovery, encounter frequency, balance or enjoyment evidence. Screenshots need manual inspection for truthful water/oil/acid rendering. Recoverable pool/flask quantities are tested; tile exposure leases and thermal reactions are not a mass-conservation model. There is no scheduler bypass or manual NPC scheduling; normal zone-transition registration is allowed.

The launcher uses the existing NativeSaveIsolation owner-token pattern and explicit start-scene override. It restores scene setup using the reviewed editor-update retry after Play teardown; the driver restores input device/settings, background operation and its diagnostic channel. Root should retain before/after settings and log evidence as with the thermal launcher.

## Verification so far

- Offline C# API/syntax compilation against existing native project/engine DLLs: passed. An unchanged copy of MenuShortcutMap source is included only to expose its internal helper to this scratch assembly. This does not claim Unity import or runtime validation.
- Independent read-only review by combat_density: no concrete blocker in acquisition, volume, save proof or cleanup. Its schedule-wording correction is included.
- Native execution and manual screenshot review: pending.

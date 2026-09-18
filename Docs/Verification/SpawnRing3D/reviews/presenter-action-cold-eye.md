# Independent ring presenter and native action probe review

Read-only production review, 2026-09-10. Root reports 504 targeted tests GREEN before this review. I did not run Unity. With later explicit authorization I added only the six regression tests described below and their copied/fresh-GUID metadata; root owns their actual RED and production fix.

## Must fix: whole-surface failure needs whole native repaint

**P2 — cross-renderer invalidation / atomic fallback.** `SpawnRing3DPresenter.Refresh` (lines 89–100) correctly releases the entire owned 3D surface when a new runtime model is corrupt. `Release` unsubscribes all five listeners, disposes equipment/patches/surface and clears all reference maps. Partially instantiated objects are descendants of the surface root, so even instances not yet registered in `views` are removed. It then preserves the current native graph/source and records a cached failure, preventing expensive repeated retries.

However, `ZoneRenderer.LateUpdate`'s incremental branch (lines 924–940) still paints only the initial dirty set after that release. `EnvironmentSpriteRenderer.PostRender` (lines 971–994) retains all other previous native suppression claims and only releases dirty cells and their neighbours. A stationary player can therefore see the whole 3D view disappear while untouched native floors/entities remain absent. The next ordinary full redraw repairs it, but fallback must not require a player move. The original Felling presentation is synchronized earlier in the frame, so it also remains hidden during the failure frame until resynchronization.

Recommended minimum: detect ready/visible ring→failed transition in the ordinary renderer refresh path, resynchronize its original presenters and perform a full native repaint in that frame. Simply setting a dirty flag can be swallowed by the surrounding `_fullDirty=false`/clear sequence or postpone the fallback; test the actual paint result. Healthy incremental updates should retain their normal narrow work.

With root authorization, `Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DFallbackInvalidationTests.cs` adds six tests:

- Actual distant ground and actual distant `CaveHermit` native bodies, each with corrupt-model and healthy controls (four cases).
- Ready original Felling presentation restored within the actual failure frame, paired with healthy suppression (two cases).

The fixture uses the real imported ring library/current generated zones, ordinary `ZoneRenderer.LateUpdate`, real environment claims and only a new `Tepuibone` fixture entity. The distant probes lie beyond the dirty-neighbour expansion. It verifies no move, turn or extra native membership mutation. Only the bad-model candidate is a deliberately corrupted private prefab clone; the borrowed binding is restored in `finally`. No production edits or actual RED results are claimed here. New meta GUID: `e936f0a6fafe4342ad06057be8ea12a1`.

## Requested presenter changes: source-clear with bounds

- **Runtime cleanup:** clear ownership and disposal order are sound for the tested missing/foreign-material or missing-renderer creation failures. Whole-native repaint is the downstream issue above; the ownership cleanup itself does not leave live claims.
- **Static mesh-fragment picking:** the clicked cell must currently be visible before any raycast. Independent static Felling hits then retain their exact native owner even when its remote anchor is hidden. Transient actors/items still require the anchor cell's current visibility, including before a stale view is reconciled. A current native fallback top object keeps selection priority.
- **Cell fallback restricted to batched geometry:** a missed irregular independent mesh is no longer rescued by its anchor cell. Batched objects, which intentionally have no separate collider, keep the native top-cell fallback. Outputs reset on invalid picks.
- This does not claim full physics saturation, all simultaneous-presenter interactions, camera feel or GPU fragment correctness. Existing targeted/adversarial tests and separate native/GPU receipts remain the evidence for their own scopes. This is original presentation work; Qud gameplay parity is not being asserted.

## Separate native action probe: no source-proved must-fix found

Reviewed `/tmp/codex-spawn-ring-actions/SpawnRing3DNativeAudit.Actions.cs` and README against actual input, menu, harvest, combat, hook, turn-manager and base-audit APIs. The draft has not been compiled/run by me.

- `GlowQuartzVein` uses the actual current factory and unchanged shipped 1–2 yield /100% chance. Real C/direction/menu-key input reaches `HarvestablePart`; reading the displayed `_actions`/`_shortcuts` avoids a guessed binding. Counting carried plus ground yield correctly permits native overflow. Generic Harvest's current zero-turn dispatch is accurately identified. The source does not emit an Interact hook, and the report does not invent one.
- The temporary Snapjaw gets actual factory anatomy/gear/health/faction, only the new fixture's required zone and turn registration, and untouched native RNG. Ordinary hostile-bump input executes real combat and a synchronous native turn. One Attack callback is expected per `PerformMeleeAttack`, independent of how many body weapons attempt strikes.
- Additive observers run after the already-bound presenter listener. A queued `Clip` plus real controller `HasState` proves consumer routing; it does **not** prove a visible rendered pose. Current/next Animator hashes and actual screenshots are recorded, and the report explicitly states retaliation/death can supersede an action before the first rendered frame.
- A real miss/resist/no-penetration outcome is valid and has the explicit unchanged-fixture-HP control. Source IDs distinguish player damage from native third-party effects. Positive HP loss requires corresponding callbacks; using greater-or-equal rather than exact equality allows healing/overkill/native side effects without inventing damage. Hidden callback participants do not receive a false visible-pose assertion.
- The combat log uses only entries appended since the preflight. `MessageLog` is currently append-only during this path, so `Count` followed by `GetMessages().Skip(start)` is valid here.
- Cleanup captures all nonfixture entity/cell references before removing only the two exact declared fixtures. It preserves native harvest yield, death drops, effects and other real consequences in the private disposable session. The base audit's explicit nested-iterator disposal runs this `finally` on abort/failure.

Verification bounds to preserve during adoption: this remains optional and outside measured profile phases, after both save/load and UI acceptance. Root must adopt the explicit callsite and inspect its same-run `actions.json` with complete/cleanup true and zero failures, plus six actual screenshot files. An enabled-action run that lacks those receipts must not be described as action acceptance merely because the older generic native report passed. Player Hit can remain unobserved when retaliation causes no damage; the empty hook list is an observation limit, not proof of a visible Hit pose. No broad combat balance or guaranteed-hit claim is supported.

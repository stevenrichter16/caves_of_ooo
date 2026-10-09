# D2 readiness: one finite retreat-cover supply

Read-only source audit, 2026-10-09. No Assets edits or Unity calls. Parent owns release gates and native verification. Recommendation: defer D2 for this slice and assess E's existing movement danger weights next. D2 has a clear player lesson, but first needs a bounded, separately tested gas-throw payment seam. Do not expand a universal tactical AI or gas rollback framework merely to ship one rare move.

## Verified purpose and actual source

The lesson is worthwhile: an injured soursprayer spends its only bladder and its whole turn obscuring a retreat; the player can press through dangerous cold, go around, wait out the veil, or recover the unused bladder by preventing its use. There is no conjured stock, free movement, new spell, faction immunity or refill.

`SpreadExplorationPlan.FamilyFor` fixes the TrappersStore family at Overworld.11.11.0 for current versions. `SpreadExplorationWorksites.cs:27–44,62` admits a current population receipt of one or two matching ordinary hostiles, but creates a Soursprayer/Scrabbler pair only when there were already two. Store placement also requires an actual eligible ordinary cache and a fitting layout. A manifest entry alone is not a source witness.

Historical native reports provide stronger evidence: `Docs/Verification/SpreadDiscoveryExpeditions/Native/{74355329f8df4633adcaa58ec5cde635,77d7e740bb62465eaaad6b32ce22b1c6,a9fc3161baa34b92aca3ca25f422898d}/report.json` all contain real Soursprayer turns while player observations are in Overworld.11.11.0 and PASS trap_actual_southern_store. Respectively 13,28,2 action windows contain this actor. This proves an ordinary historical source, not current three-seed generation or an opportunity to use the proposed retreat item. One report's soursprayer dies to an existing spike trap; it is not guaranteed to survive to teach the move.

Before implementation, ask parent to run a read-only fresh source probe at exactly 11.11 for current seeds64/1729/729490642. Record family, source receipt count, committed worksite, exact caster, actual carried stock and source budget. Do not force a second creature into a one-actor source. If absent content, retain the older store without enrichment. Missing/malformed factory content must preserve source rollback. Copy the narrow optional-source receipt pattern used by emergency water; do not change the global Soursprayer blueprint or saved zones.

Current Soursprayer has12HP, SightRadius6, FleeThreshold.2, preferred range3, an equipped cudgel and glimmer brine. Proposed source-only threshold.4 gives a practical retreat window at1–4HP. The combination of low HP, preferred range3 and required observed distance4–6 may make the move infrequent; a normal native fight must prove an opportunity before claiming it works in practice. Do not quietly increase HP/range/population to rescue the design.

The item already exists: `Data/Farming/BiomeCrops.json:499` supplies one VeilpuffBladder per mature Cave crop. Its real GasGrenade part is cryo-mist density10, level1 plus veil-mist opacity4 turns. It is weight1, one-hand, light and throwable. A STR12 soursprayer can throw it with the current Handling rules; no empty-hand requirement exists. This authoring would carry one imported harvested bladder, not grow an unexplained Cave crop at the store. Existing item and model suffice.

## Exact throw and gas corrections

- `ThrowItemCommand.cs:159` uses TraceFirstImpactToTarget. That trace checks creatures and targetable loose objects before wall collision (`LineTargeting.cs:124`), so an empty-looking cell is not proof of a clear throw. A closed door redirects the burst to the approach-side last traversable cell. Require the preview's actual empty first impact to equal the selected center.
- Choose the second *actual Bresenham path cell toward the observed body contact*. Do not use actor +2*sign(delta). Example actor(10,10), hostile(16,11): sign-based(12,12) misses the sight ray; actual second ray cell is(12,10). Trace to that short center again and require matching impact, no creature/object hit and no solid blockage.
- The gas-grenade branch has no accuracy/random-drift roll. Pass brain.Rng to the existing command, but do not invent RNG consumption or fumble probability. “Fumble” is the trace returning the actor's own cell, not a random result. Injected-RNG tests should pin no extra draws for this payload.
- `GasGrenadePart.cs:64–102` spawns a3×3 cryo cloud. OpenCloudPath restricts *opacity*, not harmful gas. GasCryoPart applies actual cold damage and chilling to eligible damageable bodies; density10 currently means2 damage per application. Noncreature vulnerable objects can be affected. No ally/self immunity.
- Check all actual occupied self cells against the burst, not only the anchor. Refuse currently observed nonhostile/party bodies whose actual footprint intersects the burst. Do not use the player's FOV or see hidden allies through walls. Hidden unknown occupants and later gas diffusion remain ordinary risks, not permission to promise perfect safety.
- Gas can merge into existing gas and writes independent tile-cloud state (`GasFactory.cs:82–113`, ZoneTileState.WriteCloud). Removing newly created entities is not a complete rollback.

## Feature prerequisite: paid throw semantics

`InventoryCommandExecutor.cs:38–55` executes the command before committing and rolls back on exception. ThrowItemCommand extracts stock with undo (`:395–450`), then detonates inside Execute (`:201,:326,:622`). The undo restores inventory but does not undo merged/new gas or tile opacity. A throwing MessageLog observer *after* detonation, or a mid-detonation zone/tile callback, can therefore return failure and refund stock after some world effects exist. This is a rare existing exceptional-callback issue and a prerequisite for the proposed NPC policy; it is not a reproduced ordinary-play crash or reason to block the independent C/D1 release.

Adding only a BeforeCommit predicate outside the command is unsafe: it can explicitly reject after the world has already changed. ThrowItemCommand also does not send PerformInventoryActionCommand's Before/AfterInventoryAction events, so do not promise generic inventory veto semantics for this throw without implementing and testing that seam.

Smallest future direction: a narrow gas-grenade execution branch that validates and claims the exact carried unit, captures actor origin/impact/payload, validates source/control and first impact immediately before an irreversible paid release, then treats post-release presentation failures as a spent action. Do not misuse AfterCommit's documented informational-observer contract for world mutation without an explicit design decision. Preserve existing player throw behavior and real trace; do not implement a second detonation policy in AI. A release failure/partial effect must never grant a refunded bladder and another AI action. Confirm the defect with native RED before changing this shared command. A generalized gas/world transaction is outside D2.

## Small future policy contract (after prerequisite)

Extend existing TacticalSupplyPart with `RetreatCover=false` and a pure exact-carried Veilpuff query; preserve `SelfDousing` explicit opt-in and no-stock truth. Reuse WorldResourceActions.Carried for exact ownership, singleton/duplicate-ID and owned-part validation rather than weaker Inventory.Contains checks. Admit only exact Veilpuff with the actual known owned GasGrenade payload, not a tonic or arbitrary future grenade. Actual inventory remains stock authority; no separate mutable charge counter.

Add one `TryUseRetreatCover(threat,zone)` call in the *visible* FleeGoal branch after emergency water/medicine and before movement. Require current actual Flee goal/low-health eligibility, living grounded uncontrolled nonplayer actor, currently visible hostile contact at4–6, clear old sight ray, a legal outward retreat step, exact stock, valid real payload and legal actual first-impact center. Failure has no side effects; success consumes this entire opportunity and does not also move. D1's next opportunity then loses sight and retreats from its observed contact for a finite budget. No Kill/Bored handoff changes.

Observed feedback must use actual actor name and AddObserved policy. Throw's current blanket MessageLog.Add lines also require narrow observer-safe gas-throw feedback; suppressing only the AI's extra message leaves information leaks. Use the existing item projectile/world cloud visuals. Examine should describe literal available stock and cold/ally costs, not assert an inexhaustible trick.

## Meaningful future RED / countercheck matrix

1. Real Brain/Flee action with exact carried bladder at safe4–6 contact: one item gone, opacity on the true ray, no movement/attack, one scheduled opportunity; following opportunity retreats from last seen contact. Repeat has no supply.
2. Shallow-angle ray above; diagonal/corner obstruction, loose intervening item, wall/closed-door approach-side impact, multi-cell hostile visible secondary contact; none may substitute a different burst center.
3. Self or observed allied/nonhostile secondary body cell in burst refuses; hidden unknown occupants are not magically detected; actual throw still damages vulnerable cold targets and eventually diffuses.
4. Distance3/7, already hidden target, blocked outward step, controlled/Stunned/following actor, opt-out, removed source, foreign/malformed payload, wrong/duplicate/stacked stock refuse without payment. Later valid carried item is selectable after a malformed one.
5. Real exact command invalidation before release: no gas and no payment. Post-release observer exception: no refunded stock, no second action; include preexisting merge gas/cloud to rule out naive rollback. RNG draw count remains unchanged in gas branch.
6. Player-visible vs offscreen actor feedback; save before use preserves one real item and opt-in; save after use preserves absence; death before use drops one, after use zero; revisit does not refill.
7. Exact fresh11.11 source against ordinary current generation, not forced actors/cleared terrain. No-budget/missing-content controls preserve older site. One ordinary Play fight must actually reach the decision window without injecting HP/items/goals during the acceptance route.

## Ordering decision

Defer D2 until its paid-release seam and actual opportunity are proven. E touches existing movement choices that currently bypass known terrain weights, so its benefit reaches ordinary approach, retreat and ranged positioning across many fights. It also lets the player's already-shipped oils, gas, cold and ground preparation change enemy choices. Prefer a bounded existing-weight choice improvement next, with forced-path/footprint/resistance counterchecks and no new global planner.

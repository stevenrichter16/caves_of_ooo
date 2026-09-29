# Physical obstacles strand ordinary pursuit — bounded next-slice audit

**Result:** one important confirmed navigation defect. The corrected unchanged-production run has **43 cases: 31 passing controls and 12 failures exposing the same defect**, in 0.765 seconds of private test execution. No production fix, shared Assets, Unity operation or Git mutation was made. This is a prerequisite for fair tactical hauling, not additional scope for the current F11 publication.

## Player-visible consequence

A stationary beam, barrel or hedgerow can stop an ordinary single-cell melee pursuer even when there is an open walkable detour. In the fixed synthetic map, pursuer `(6,10)`, player `(12,10)`, barrier at `x=7,y=6..14`, all three exact authored Physics-only owners strand the real KillGoal at its initial cell through **31 NPC actions / 30 player waits / 310 scheduler ticks**, with zero remaining energy after each action and no damage to the player. The route around the open end takes ten ordinary movement steps; separate controls execute that entire path successfully.

This is not universal failure around every prop. One isolated original beam/barrel/hedge is bypassed by the existing diagonal fallback and reaches adjacent contact in five calls. The equivalent tag-Solid wall takes the proper detour and reaches contact in nine scheduled NPC actions / 90 ticks. A genuine two-cell actor takes its footprint-aware contact route around each original Physics-only boundary in nine actions / 90 ticks. Even a one-cell explicit footprint chooses an executable first step where an otherwise identical legacy single-cell actor plans into the blocker.

The proposed inverse-hauling geometry also fails: one original FallenBeam plugging a gap in a short actual Hedge boundary disables progress for all thirty bounded calls. Removing only that beam restores the straight approach; the plugged layout still has the same measured open bypass. **Do not advertise the stalled AI as useful defensive separation.** These objects still do not become sight walls, and no ranged/projectile cover behavior is claimed.

## Exact source seam

- `KillGoal.cs:55–75` uses footprint-aware `FindPath.ToContact` for explicit bodies; ordinary actors call `AIHelpers.TryApproachWithPathfinding`.
- `AIHelpers.cs:399–420` first checks tag-based `IsPassable`, attempts a real move, then searches with `ignoreCreatures:true`. The real move correctly fails against the Physics-only owner.
- `FindPath.cs:141–158` only checks `IsCellBlockedByCreature` for the legacy single-cell branch when `ignoreCreatures` is false. Despite its name, that helper at `:276` checks **all** Physics.Solid owners. With true, it skips stationary noncreature props too. The returned path repeatedly starts inside the same blocked owner.
- `AIHelpers.cs:333–365` last-resort stepping tries toward-target diagonals. It can pass a single prop, but cannot move parallel to the multi-cell boundary where the real detour begins. It therefore stays at the same cell, and the stateless next action repeats the same plan.
- `PhysicsPart.cs:111–131` continues correctly refusing physical occupancy; `Cell.cs:99/236` deliberately distinguishes Solid-tag topology from movement; `Cell.BlocksMovement` includes the Physics-only owners. Changing the global Solid/LOS semantics would be an inappropriate fix.
- `Zone.cs:114–132` footprint placement already skips only actual Creature-tagged owners when requested and still respects stationary physical props. The larger-actor control consequently works. Closed door capability, locked-door refusal and stationary opening all pass their three paired cases.

## Classification and retained corrections

Eleven fixture methods cover the ten prewritten player-flow questions plus the fully executed bypass counter. Twelve failing cases comprise three search-policy cases, three ordinary approach cases, three scheduler cases, one equivalent explicit-footprint comparison, one phantom route through a fully sealed beam boundary, and the concrete loaded hedge throat. Thirty-one controls pass, including creature omission, ordinary walls, thin props, actual bypass execution, all door cases and multi-cell bodies.

The initial 43-case result was 27 pass / 16 fail. Four failures were invalid wide-actor preflights: Search was asked to overlap the player’s occupied anchor. The actual KillGoal correctly uses ToContact. Only that test helper was corrected, giving 31/12. Original source/log/XML and `SETUP-CORRECTIONS.md` preserve the mistake; none of those four is presented as a gameplay bug. No production changed between runs.

## Minimal proposed next slice

1. After F11 publication, adopt the private tests first, run actual Unity EditMode and retain genuine baseline RED. Do not use private runner results as a native test claim.
2. Change only the single-cell physical-occupancy predicate in FindPath so `ignoreCreatures:true` omits **actual Creature-tagged physical actors**, while Physics.Solid noncreature props still block intermediate route cells. Preserve the existing Solid-tag check, operable-door path cost/opening contract, multi-cell/contact branch and legacy intentional goal-cell handling. Avoid adding Solid tags to content, changing LOS/collision, removing obstacles, making KillGoal attack furniture or globally changing greedy behavior.
3. Require all current 43 audit cases to pass, retaining creature-occupied route omission and door/wall/contact counters. If the direct approach’s optimistic tag-only check remains, its failed move must fall into the now-correct A* route; do not refactor that harmless failed attempt without a separate measured reason.
4. Run the proposed 31-fixture regression request: planner, normal chase, physical body/contact, doors, terrain/gas preference, ordinary goals/following, turn movement, drag lifecycle, and F11’s finite hunt/flight callers. These are selected dependencies, not a request for another expensive whole-world census.
5. Verify one bounded native player/pursuer/drag layout with real paid pulls and actual open bypass before building the new defensive hauling content. The expected outcome is honest delay followed by pursuit around the load, not an invented line-of-sight shield or a permanently frozen enemy. Existing wall/door and exact source/save contracts remain release gates.

## Evidence limits

The actual production pathfinding, Physics movement, goal and energy scheduler run in the private .NET10/stubbed runner. Props are instantiated from the real unchanged Objects.json. Actors are deliberately small explicit test entities with the ordinary Brain/KillGoal/Physics/Speed contract; this is not a naturally generated Marlback, a native keyboard interaction, a complete world tick service run or a Unity performance measurement. The full route is executed using actual MovementSystem; the player itself waits and no NPC is paused in scheduler fixtures. No attack outcome is needed because fixtures stop at adjacency. Native test/Play confirmation remains root-owned for the next milestone.

`test-manifest.json` proposes one fixture and fresh unique meta for later adoption, but does not authorize an immediate shared import. `source-manifest.json` pins the relevant tested source and blueprint snapshot. `classified-results.json` stores every actual result/trace. Both original and corrected XML receipts remain unmodified. The runner DLL remains available for reproducibility; its fixed geometry does not depend on patched generation hash values.

# Movable obstacle pursuit audit — hypotheses frozen before detailed source read

Scope: next-milestone source/behavior audit only. No shared Assets, Unity or Git changes, and no production fix. Private logic-runner evidence is not native input or timing evidence. The prior F11 release remains independent.

Player-flow questions to test with bounded real movement/goal cases:

1. With a short physical beam/barrel/hedge barrier and an open 8-direction bypass, a melee pursuer can reach the player instead of repeatedly planning into a blocked cell. A no-barrier case supplies a positive movement control.
2. Otherwise identical authored Solid-tag wall geometry should not differ from a Physics.Solid-only barrier in route reachability.
3. A single movable obstacle may be handled by the direct diagonal fallback even if the longer barrier exposes a bad path; test both instead of attributing every blocked approach to A*.
4. Ignoring creatures during search should skip an ordinary occupying creature, but still respect an actual stationary physical barrier. Actual execution may wait/refuse a temporarily occupied creature cell; do not call that structural no-route evidence.
5. A closed door must follow its existing action contract rather than be treated as a transparent physical hole; compare existing door opening/route behavior without inventing a new promise.
6. If the player is on the far side of a multi-cell boundary with an open end, the actual KillGoal and scheduler should make progress through that end within a bounded action budget. No movement teleport or fake success.
7. A multi-cell creature may take a different footprint-aware path; test actual footprint and an adequately wide bypass so an impossible body fit does not masquerade as a pathfinding bug.
8. Direct approach, direct fallback stepping and pathfinding fallback should be distinguished by actual calls/positions, not inferred from a returned nonempty path.
9. A completely sealed structural barrier may rightly prevent progress; its refusal is a negative control, not a request for tunneling or deleting the barrier.
10. Path and execution should agree on passability for stationary noncreature Physics.Solid owners, including the actual FallenBeam/barrel/Hedge blueprints when practical. Load need not block LOS; no promised shooting or sight cover.

Execution gate: choose 6–12 narrowly paired fixture methods once API shapes are read, with explicit condition cases. Run unchanged production first. Preserve any setup errors separately. Stop after bounded source/test effort if a real behavioral defect cannot be reproduced. No repair is authorized by this audit.

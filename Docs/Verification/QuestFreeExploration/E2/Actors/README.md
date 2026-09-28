# E2 actor behavior readiness

2026-09-27. Production published under the root's released source window; generation/data wiring remains root-owned. Original CoO behavior design, not a claim of Qud parity.

## Verification and corrections

Primary reflection fixture executed before production: 23 cases, 21 missing-role assertion failures and 2 ordinary combat/Calm controls passed. First candidate then passed23. Adversarial46 found three real defects: wide-bodied threat/territory admission used only anchor/ordinary actor rules, and the renderer callback could observe a consumed row before the saved Fed flag. Occupied-cell admission and pre-callback saved state repaired them; 46GREEN. A detached-role pair then found one null-owner configuration failure (47PASS/1FAIL); narrow guard fixed it. Final52/52 pass. Runtime and isolated native test-reference compilers return0; this is not executed Unity or native journey evidence.

## API and transaction boundary

- `SpreadTerritoryPart.Configure(zone, post, left, top, right, bottom, graceTurns=2)` requires an attached, living, single-cell creature and exact ground post. Rectangle spans at most13 cells per axis; actor and post are inside. Grace is1–6 actions; default2 means one warning action followed by two complete grace actions, then combat. Saved fields include exact Post/WarningTarget refs, source zone/coordinates, home/rectangle, GraceRemaining and bounded ReturnAttempts.
- `SpreadGrazerPart.ConfigureForage(zone, targetRow, reservedRow)` requires exact distinct owned unspent RipeCropRows within12 cells. Source refs/positions, Fed and approach attempts are saved. Composer must separately prove reachable entry/target/reserve and call only after source placement. One feed consumes only Food, leaves ReservedRow, and creates no yield or healing. At most24 approach actions.
- Both parts operate only when native BoredGoal is current. Calm, work, follow and other existing goals win. Party-led actors decline role control. Mere territory entry never edits PersonalEnemies or faction reputation. Actual player damage retains native personal retaliation; target departure releases only territorial pursuit.
- Healthy grazer flight considers current nearby living player/hostile body within3 cells and uses one native movement action; it stops when no threat is near. A trapped animal idles. No child is pushed, no clock/energy is edited. Scheduler controls payment.
- Root composer must check its exact current plan/receipt before and after generation callbacks, refuse/move back only its still-owned actors, and never use these Parts as source-generation authority. No new water behavior is claimed by this unit.

## Review bounds

Q1: warning begin/exit and feed/reserve state are saved symmetrically; both actor and source live membership/backlinks checked. Q2: native tactic/melee/path executor reused without child chaining; old field harvest shares only existing stubble mutation, not new yield/stock policy. Q3: shared two-file preimages checked before publication; no Objects/save schema/faction/global registry production changes. Q4: focused counters cover removal/movement/death/foreign backlinks, full graph load, ordinary action veto, actual paid action and party/Calm. Independent standalone_verify read found no concrete blocker.

Remaining gates: root native focused plus neighbors, real generation/composer integration, approved body/art and ordinary keyboard observation. These tests use explicitly staged synthetic actor/source graphs and do not establish natural discovery, visual quality or encounter balance.

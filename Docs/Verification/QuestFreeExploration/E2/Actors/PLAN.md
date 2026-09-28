# E2 actor behavior — private implementation unit

2026-09-27; source sweep at HEAD8b01bf02. No shared Assets/Unity/git changes. Root owns world composition/content/art and publication.

## Source corrections before code
- `BoredGoal`66–102 acquires faction hostiles before `AIBored`; add one role dispatch before that path, not another late idle handler.
- `BrainPart`666–687 immediately executes newly pushed children. Roles perform at most one movement/warning/feeding operation directly and never push an action child. Scheduler alone pays the ordinary action.
- `FleeGoal.Finished` uses health (`GoalHandler.ShouldFlee`), so proximity flight needs an independent current-distance condition.
- `SavePart` constructs then writes public mutable fields; entity references are tokenized. New role parts use saved fields. No new custom goal or unsaved progress fields are needed. Factory scans Part subclasses automatically.
- `NoFightGoal`, FollowLeader/current work/combat goals already sit above BoredGoal. Do not displace them. Role dispatch declines party-led actors and preserves genuine personal enemies.
- `FieldHarvestPart` makes food and marks stubble; grazer feeding must not route through player harvest and secretly mint dropped grain. Add a narrowly validated consume-row path if tests prove needed, sharing only the stubble mutation.

## Proposed composer API
`SpreadTerritoryPart.Configure(Zone zone, Entity post, int left,int top,int right,int bottom,int graceTurns=2)` captures exact post reference/position, current owner home, zone ID and bounded territory rectangle. Saved warning target/grace and local pursuit are separate from PersonalEnemies. Outside/moved/dead/detached post refuses ordinary territorial activity; genuine personal hostility remains normal combat. Warning action plus two subsequent holder actions give actual opportunity to leave. New entry restarts warning; no ordinary faction mutation.

`SpreadGrazerPart.ConfigureForage(Zone zone,Entity targetRow,Entity reservedRow)` captures two exact distinct ripe row refs and positions. Composer owns generation-time reachable proof; part rechecks exact unspent owner/backlinks locally. At most one consumed target, saved on the animal and row, no product/HP grant. Healthy proximity flight outranks feeding. A narrow `ConfigureWater` visit API may follow after root confirms its draw-point source contract; visiting must not claim drinking/volume consumption.

Tests first, missing-API reflection assertions against original current runtime plus unchanged ordinary/Calm/current-goal controls. Then minimal candidate; saved warning/feeding graph and actual scheduler payment controls; dedicated owner/mutation/adversarial checks. Native reference compilation and root native gate remain separate.

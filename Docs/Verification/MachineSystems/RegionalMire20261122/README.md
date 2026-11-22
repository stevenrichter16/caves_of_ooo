# Natural regional mire and a water-cooling countercase — November22,2026

Runtime source: `910f86dae8e341badf2dbd032bacf7e62289dd08`; documentation
head before this receipt: `5c73de45e22c7eb1fbceab686ee4e45107af165e`.
This is actual GameBootstrap input on existing behavior, with read-only state
observations. No runtime change, new recipe, test rerun or package was needed.
It extends fixture processing evidence with a naturally generated feed source.
It also reproduces an **open P2: recoverable water receives repeated hearth cooling**.
It does not accept finite dousing balance or a complete fractionating machine.

## Provenance and scope

Start was the already earned public-inventory checkpoint1620, game
`8ab6a6384c2a44deb8b3a68303809d8f`, in Overworld.4.9.0.
The authored workshop equipment, camps, currency, HP400 maximum/current399 and
expanded carrying capacity are fixtures. Acquiring that gear, ordinary trip
difficulty, danger, commercial viability and balance are excluded.
The five existing mire flasks2231–2235 stay11,12,12,12,1 throughout and supply no
input to the new batch. Empty flask2236 receives only the natural source.

The route was source-guided, not unaided discovery. Native Shift+Comma opened
the world map at34,12;13Right+2Up and native Shift+Period reached Works17.7,
40,12 at1940. SixLeft+3Up reached dry34,9 at2030 beside natural MirePool13151
at34,8, liquid bog-mire, volume60. The character did not enter that pool.
The source exists in the generated Sodden district; it was not placed or refilled
by observation code. No item grant, teleport, invincibility or tick jump occurred.

## Collection, travel and actual processing

The C/Up menu and fill subgroup distinguish the existing11/12 and1/12 receivers
from empty2236. Cancelling left2030/source60/receiver0 unchanged.
The chosen ordinary empty-receiver row paid2030→2040: source13151 fell60→48,
and2236 became12/12 bog-mire. F5 was archived **before Stop**. A separate normal
Play/Continue retained exact measured2040 actor/source/flask state.

Actual return first revisited4.9 at2360; no still stood there. The map's kept-camp
note and a read-only cached-site census identified the existing camp at10.11,
so native travel continued there at2540, then threeUp to40,9 at2570.
Camp still2148 stands41,8; owned hearth2147 stands40,8. The detour and its time
remain earned costs; this is not an optimal-route comparison.

The exact row `SettleLiquid|water|2236|12` converted only the collected flask:
2570→2580,12 mire→3 water in2236. Hearth fuel26.5→21.25 is5 process fuel plus
0.25 ordinary burn. Temperature345.297882→338.891937 includes ordinary thermal
evolution. Source13151 stays48. The other mire flasks are unchanged.

Settling consumes the entire input and leaves unrecoverable dregs as an
abstraction; no separate waste inventory is created. Baking is a different
existing choice:8 mire/10 fire fuel→1 fire clay while keeping the remainder.
Natural heat within2 cells can settle without fuel but cannot bake.
These latter choices are source-verified, not newly played here.
Neither concentration nor fluid volume currently changes flask carrying weight.
Generic settled oil cannot refuel the LampOil-only torch action.

## Outside-machine outcome and new P2

At2580 the existing pour-one-water hint explicitly said “Cools your hearth; it
burns lower.” The action paid2580→2590:2236 water3→2, hearth temperature
338.891937→293.4141, fuel21.25→21, light still on. It banked the hearth; it did
**not** shut it down. PouredLiquidPool14782 retains1 water at40,8.
Output stock is therefore still3:2 carried+1 ground.

Native C/Up then selected the same flask's ordinary fill row:
2590→2600, flask2→3, pool14782 retired, temperature288.0458/fuel20.75.
Another native pour-one paid2600→2610: flask3→2, new pool14783 water1,
temperature243.584885/fuel20.5/light on. The same output water receives another
40-degree cooling dose, alongside normal per-turn decay, without net water loss.
This is a finite-resource defect in the current cooling contract, not duplication
of liquid. It is independent of the already closed oil-ignition correction.

Source cause: FireDouse.Apply applies cooling after LiquidVesselService has
deposited every poured unit into recoverable stock. Existing FireDouseTests only
check flask decrement, not flask+ground stock. A correction needs a reviewed
policy for the **newly poured dose** that pays for cooling, protecting preexisting
pooled water, unrelated natural sources, rollback and callbacks. Do not globally
change steam/freeze behavior or reuse the oil policy by assumption.

No claim of finite dousing cost, full shutdown or a balanced survival loop is
earned. A later useful-output qualification should prefer an existing crop's
water need, drinking against existing debt, or clay for an actual repair/proofing
choice. Do not create a hazard or compulsory chore merely to justify processing.

## Inventory, persistence and exclusions

The published dense Inventory search finds both the watering grimoire by name
and2236 by public2/12 water state. Native Down selects2236; F1 page0 of2 identifies
the exact flask and its supported water uses. Only that first flask-reader page
was captured; no full two-page reading claim. Navigation/search/readers stayed2610
or2590 as recorded and spent no gameplay time. The new trip does not requalify
density, previously measured44/66 entries.

F5 archived2610 before Stop. Fresh Play/Continue retains exact measured zone,
40,9, tick2610,HP399, source13151 at48, all12 flask states, pool14783 water1 and
hearth2147 fuel20.5/temperature243.584885/light on. Both fresh comparisons are in
observations.json. Large earned saves remain outside the repository:
- `/private/tmp/coo-regional-mire-drawn-earned-20261122`
- `/private/tmp/coo-regional-mire-processed-earned-20261122`

Free operator detours are disclosed in observations.json: M opened Abilities;
an extra Escape opened Pause, blocking later attempted keys until Tab closed it;
initial Inventory navigation reached Tinkering and the first water search selected
the watering grimoire. Corrected navigation then selected2236. A read-only observer
failed compilation on nonexistent LiquidCoveredEffect.GetDescription and is
excluded. No paid mis-action or runtime source edit resulted.

The old unloaded-fire chronology limitation remains: absence does not charge
fuel. This trip does not accept an authoritative unloaded simulation or NPC
survival enrollment. No FPS, physical-input/controller, packaged-play or new
hardware claim. Current Mac candidate910f86dae is unchanged.

Main stopped, save override null, scene clean, profiler off; original7 GameView
presets/custom0/index0 restored. Fresh18:02UTC preservation checks find84 foreign
tracked files and861 normal files unchanged, excluding Unity analytics and
TestResults.xml. No whole-directory or byte-exact preference claim.
Independent bounded source/design and subsequent source/artifact/gameplay-evidence
review are recorded in review.md; neither is a second independent game operator.

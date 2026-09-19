# Day to day — the player's ordinary hours

**Status:** planned, 19 September 2026. User direction: fill out "the day-to-day experience for the player and making more use of the available mechanics and systems" (after the breadth pass). Pairs with [LOOT-FINDS](LOOT-FINDS.md) (finding made things). CoO-original; no Qud parity claim.

## Goal

Between the large quests, the player's hours should be made of small decisions the game already has the machinery for: read what just happened in plain words, see what time of day it is, prepare water before crossing the Beating, cook what they hunt and forage at any fire, and decide when to rest. Each item below uses systems that exist and are under-used; none adds a new simulation.

## What the ordinary hours look like today (verified)

| Moment | What exists | What the player meets |
|---|---|---|
| Reading the log | Most messages are built from `GetDisplayName()`; the player's display name is "you" | "you is confused!", "you picks up tepuibone.", "you is drenched.", "you's overload finds no conductors." in every native capture |
| Time of day | `WorldClock`: four bands per 1,200 ticks (Dawn, Height, Dusk, Dark; underground names by depth); the Beating's glare parches during Height; rest advances the clock | The band is never shown |
| Thirst | `ParchedEffect` (−1 Str/−1 Agi per stack, up to three, until watered) from the Beating's glare; cured by standing in water, drawing at a well, or one stack per water tonic | Every Felling-leg capture shows "parched (badly)"; nothing can carry water |
| Food | `FoodPart` heals when eaten; raw meat (1d4, from 35–55 % of beast kills) and starapples (2d4) have cooked forms (3d4) that exist only in innkeepers' stock | No way to cook; campfires, hearths and ovens (`CampfirePart`) exist in every settlement and camp |
| Rest | `RestSystem`: free at fires, paid at inns (Well Rested), full heal, +60 ticks, blocked near hostiles | Rest cannot be aimed at a time of day |

## Verification sweep — corrections before any code

| Tempting premise | Verified fact | Consequence |
|---|---|---|
| Messages have a grammar helper | None; one call site builds "is … and cannot act" by hand; possessives are built as `name + "'s"` | Normalise at `MessageLog.Add`: a message beginning with the lowercase subject "you " gets second-person agreement and a capital; "you's " becomes "Your " |
| Tests pin the broken forms | No test asserts "you is", "you picks" or similar | The normaliser can be central; GREEN runs reveal any indirect pin |
| A liquid container exists | None (`LiquidVolume`/`LiquidContainer` absent) | A small `WaterskinPart` with charges, filled at wells, cisterns and standing water, drunk one stack at a time — the water tonic's rule |
| Wells cure thirst | `WellPart` "Draw water" clears all stacks; Morrowfast's cistern carries a `WellPart` | Filling is offered on the same furniture, and on water tiles |
| Cooking has a hook | `FoodPart.Cooking` is a flavour tag ("Raw", "Meal"); nothing reads it for a verb; `CampfirePart` sits on campfires, hearths, ovens and Morrowfast's outdoor stove | A `CookablePart` (`Into` blueprint) on raw foods offers "cook" beside any `CampfirePart` owner |
| The clock is available to the UI | `WorldClock.CurrentTick`, `GetBand`, `BandName(band, depth)` are public and stateless | The sidebar shows the band; rest can wait for the next band |

## Semantics (the contract, in player terms)

- **The log speaks to you.** A message about the player reads in the second person: "You are confused!", "You pick up tepuibone.", "Your overload finds no conductors." Messages about anyone else are unchanged.
- **The time of day is shown** in the sidebar by its band name (surface or underground vocabulary).
- **A waterskin** holds three drinks. It fills at a well, a cistern or standing water; each drink removes one stack of parched. Provisioners and well keepers sell them; Morrowfast's provisioner has one.
- **Cooking.** Beside a campfire, hearth, oven or stove, raw meat, starapples and mushrooms can be cooked — the whole stack in one turn — into cooked meat, roasted starapples and roasted mushrooms, which heal more.
- **Rest until …** At a fire or inn bed the player can rest until the next band begins (at most one band), under the same hostile-scan rule; the heal is unchanged.

## Sub-milestones (smallest blast radius first)

- **D.1 — The log speaks to you.** Central second-person normalisation; possessive; capitalisation.
- **D.2 — The day is shown.** Sidebar band line.
- **D.3 — Water you can carry.** `WaterskinPart`, the `Waterskin` item, stock rows, Field Note lines on the Beating legs.
- **D.4 — Cooking at a fire.** `CookablePart`, three recipes (one new product: roasted mushroom).
- **D.5 — Rest until the next band.**
- **D.6 — Adversarial sweep and native proof** (the accepted journey: grammar visible in the log, the band in the sidebar, a waterskin filled and drunk on the Beating leg, meat cooked at Morrowfast's stove).

## Performance and observability

The normaliser runs once per message (string checks on the first word only; allocation only when it rewrites). The sidebar band line joins the existing snapshot fingerprint. `furniture/WaterskinFilled`, `event/WaterDrunk`, `event/Cooked`, `furniture/RestUntil` records, each with a refusal twin carrying a reason.

## Implementation log

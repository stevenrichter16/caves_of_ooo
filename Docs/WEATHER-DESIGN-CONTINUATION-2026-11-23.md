# Weather and seasonal tool design continuation

Resumed at the user's explicit request on November23,2026 (America/Chicago).
The hourly machine task is ACTIVE. The earlier pause checkpoint below is retained
as the research baseline; existing uncommitted work is preserved.

## Current design checkpoint

The full [weather and seasonal tool proposal](WEATHER-AND-SEASONAL-TOOL-USES.md)
now exists and has bounded independent design/source critique with zero
publication-blocking P1/P2 findings. It includes ten candidate weather
opportunities, five tool/apparatus families with different seasonal work choices,
regional discovery, bounded resource/time/save policies and staged W1/W2/W3.
No weather runtime or gameplay acceptance. Before W1 RED, finalize catchment
identity/overlap/ownership/coverage and exact bounded crop rain chronology,
including the existing wet-tick remainder. W2 must demonstrate two different
jobs on the SAME physical frame under distinct seasonal/material conditions.

The research checkpoint and next-work list below record the earlier pause;
the linked full proposal now governs design continuation. Current separate
Mac receipt publication remains pending. Do not rebuild or replay earned work.

## User direction to retain

Weather should make certain activities more rewarding. Skipping an opportunity
should usually mean missing a bonus, while ordinary exploration remains
comfortable. This supersedes the earlier pressure-first weather discussion.
Avoid compulsory drinking, drying, equipment repair or exposure chores introduced
to justify tools. Existing ordinary alternatives and lasting improvements stay
useful. Weather opportunities should be visible, optional and understandable.

The latest addition is that some of the SAME tools should have different
practical uses in different seasons, beyond merely gaining a numerical bonus.
Use observable material or world conditions and explicit capabilities to explain
those uses. For example, softer sediment, ripe seed heads or fallen wood could
present different work. These are candidate design directions, not implemented
weather interactions or promises of automatic harvest permission.

The broader agreed loop remains exploration -> recognizable useful find ->
several possible uses -> gathering/carrying/processing -> equipment, camps or
lasting town projects. Qualify natural discovery through exploratory walks,
not only prepared routes. Repeated usefulness should come from new finds,
destinations and choices rather than manufactured wear or mandatory upkeep.

## Research checkpoint at the earlier pause

At the earlier pause, source and lore research had begun and a full design had
not yet been written. The full proposal now exists as noted above. No weather
runtime, tests or playtest has been produced; all new weather uses remain
proposed.

Read CLAUDE.md, SYSTEMS-DESIGN-PRINCIPLES.md, MACHINE-GAMEPLAY-LOOPS.md and the
latest MACHINE-SYSTEMS-EXECUTION-PLAN.md. Established repository documentation
is the destination for the full design; no external Page was created.

Committed source inspected at runtime7edc5a4c2 includes these useful boundaries:

- `Assets/Scripts/Gameplay/Turns/WorldClock.cs`: the authoritative clock is
  TurnManager.TickCount; one day is1200ticks, four300tick bands. Future weather
  and seasons must derive from game time, with no wall-clock economy or reload
  rerolls. Final weather/season durations have not been chosen.
- `Assets/Scripts/Gameplay/Farming/CropTime.cs` and CropSystem.cs: elapsed
  growth/moisture already have bounded reconciliation. Dry crops pause.
  CropWateringService.cs spends one actual carried water unit for40moisture.
  Natural-rain watering would be a new adapter; Conjure Rain is an existing
  spell, not evidence of a natural-weather scheduler.
- `Assets/Scripts/Gameplay/Items/ShadePart.cs`: installed shade applies to
  whoever stands in range. CampAwning currently has Shade and PackableFixture;
  it has no rain collector, drying rack or windscreen capability. Rain catching
  requires an explicit supported addition, not an assumption from its name.
- LiquidFlask has LiquidVessel capacity12. Receiver identity, compatibility,
  finite quantity and state-aware grouping must remain coherent.
- `Assets/Scripts/Gameplay/Preparation/CuttingToolPart.cs`: the explicit
  FlexibleStock capability is narrow. Dagger declares it; Hatchet has combat
  cutting attributes but no CuttingTool Part in the inspected blueprint.
  Combat labels do not grant arbitrary woodcutting, pruning or processing.
- CurationSaltRake is a physical tool/weapon whose description says it levels
  preservation salt; that does not establish generic raking/harvesting or the
  ability to preserve a body. A portable general sieve was only a candidate.
- `Assets/Scripts/Gameplay/Alchemy/NaturalHeat.cs`: declared low natural heat
  can support existing mire settling, but does not bake clay. Sunny weather
  must not silently become kiln heat or a universal processing energy source.
- `Assets/Scripts/Gameplay/Items/FishTrapPart.cs`: existing traps reference
  shared place-bound fish stock and capacity. Any later seasonal fishing
  opportunity must preserve stock accounting rather than spawn free catches.

Lore authority read: Lore/README.md, relevant Bible geography references,
11_SecondSpine.md, MYSTERY-LEDGER.md, History/02_Geography.md and
History/08_MaterialCulture.md. Ordinary meteorology must not convert the
metaphorical description of Urqu as weather into agency, a forecastable mystery
detector or a canonical explanation. Surface weather should not automatically
apply deep underground. Region and culture differences need their actual lore
and material conditions; a universal temperate calendar has not been approved.

## Next design work after an explicit resume

1. Write a bounded weather design with concrete activity benefits, discovery
   cues, material inputs/outputs, alternatives and persistence. Each example
   must distinguish current foundations from required additions.
2. Include a seasonal tool-use table. Explain at least two materially different
   supported work choices for selected tools, their sources/consumers and the
   capabilities still needed. Preserve ownership, gathering rights and finite
   harvest budgets; do not grant uses by season labels alone.
3. Define regional weather profiles, provisional cadence, forecast/readability,
   baseline comfort and how ordinary exploration reveals opportunities. Avoid
   narrow must-attend windows, lost baseline yields or weather-gated essentials.
4. Specify authoritative timing, bounded unloaded behavior, exact payment and
   rollback, finite catchment/resource accounting and save/reload countercases.
   No passive unlimited rainwater-to-profit or source regeneration on travel.
5. Choose one small first implementation slice using actual existing systems.
   Rain helping crop moisture and a separately qualified collection capability
   were candidates, not a selected or approved implementation. Later drying,
   winnowing and seasonal preparation need actual process contracts before use.
6. Obtain a bounded independent design/source critique, then refine and publish
   the design and scheduled continuation. Native RED, runtime implementation
   and actual play are subsequent gates; no weather behavior is accepted yet.

Graphics remains cancelled; capstan/cultures parked; Deck deferred. All twelve
complete-machine gates and the original failed full regression remain open.

## Separate preserved work

Inventory1fda and weapon-supply7ed are published. The Mac7ed app has completed
bounded source/package/startup/preservation review with0P1/P2, but its receipt
publication remains pending in existing uncommitted owned docs and artifacts.
See Docs/Verification/MachineSystems/MacCandidate7EDC5A4C2 and the saved hourly
checkpoint. Do not repeat its accepted tests, build or startup. Do not call
controlled packaged gameplay/save/controller/FPS/Intel verified. Preserve all
18apps, earned checkpoints, normal saves/preferences and foreign changes.
Do not stage/revert foreign MACHINE-PROGRAM-HANDOFF.md or
ProjectSettings/PackageManagerSettings.asset. No commit/push was performed
while fulfilling this stop request.

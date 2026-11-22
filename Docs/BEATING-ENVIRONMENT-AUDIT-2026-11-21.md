# Beating environment: player, NPCs and creatures

Requested investigation, November21,2026 (America/Chicago). Source baseline
3736ae52d, ordinary main GameBootstrap play, and independent bounded source review.
No gameplay rules changed and no new test runs were needed for this investigation.
Final independent source/artifact review found zero P1/P2. This was not a second
operator playing Unity. Fresh post-audit hashes matched84 tracked foreign files
and861 normal game files, excluding Unity analytics/test output; no whole-tree
or preferences-file byte-exact claim is made.

## Finding

The sun is a player survival rule; the rest of the material/hazard system is much
more widely shared. "Natives are adapted" is currently the explanation for a
blanket nonplayer exemption, not an authored biological or cultural capability.
Even an unprotected outsider or companion avoids natural noon Parched exposure.
This is an explicit v1 divergence, not proof that every NPC has heat resistance.

| Environmental effect | Player | NPCs and other creatures | Evidence |
| --- | --- | --- | --- |
| Surface Beating glare at Height | Ten consecutive exposed paid turns add Parched; up to3 stacks, each −1 Strength/Agility. It does not directly damage HP. | No natural glare application, including bareheaded bandits and wildlife. Companions share the call-site exemption. | `BeatingGlareSystem.cs:45`, `InputHandler.cs:1337`, native ten-turn comparison. |
| Headwear, interiors and installed shade | Reset exposure; shade does not cure existing Parched. | Shared geometry/head-cover queries could recognize them, but no NPC glare writer needs them and no shade-seeking AI was found. | `BeatingGlareSystem.cs:56`, `ShadePart.cs:24`. |
| Fire and real thermal exposure | Shared burning, damage, heating/cooling and resistances. | Same general effect/turn machinery. Native scorpion burned after ordinary Ember Spit despite ignoring sunlight. | `TurnManager.cs:343`, `ThermalPart.cs`, native fire control. |
| Brine, oil/tar, charged/frozen liquids | Contact/coating and authored reactions. | Shared creature contact/reaction eligibility; not a player-only rule. Some path searches penalize harmful liquids, steam, gas and slips. | `LiquidPoolPart.cs:78`, `TileReactionSystem.cs:411`, `TerrainNavigationWeight.cs:15`. |
| Hot ash / hot terrain marks | Tile heat supports reactions and low-heat still processing. It is not automatically body-temperature damage. | Same limitation. Heat+oil can damage occupants; heat+water's tile steam reaction creates a cloud with zero occupant damage. Actual hot `SteamEffect` contact is a separate scalding mechanism. | `TileStateSourcePart.cs:149`, `TileReactions/Reactions.json`, `SteamEffect.cs:82`. |
| Wind, dunes, salt and regional fauna | Landforms affect access; the biome selects its population mix. | The same solid terrain constrains routes. No Beating sandstorm exposure, salt-driven dehydration or day/heat-responsive species schedule was found in the audited paths. | Composition/formation builders, `PopulationTable.cs:542`. |

## Actual native comparison

An isolated fresh CampAwningShowcase started at Height300 in Overworld.13.14.0,
seed64. Native keyboard waits advanced exactly ten paid actions to400. All four
actors were outdoors, bareheaded, outside shade, with HeatResistance0:

| Actor | Parched300→400 | Strength / Agility300→400 | HP300→400 |
| --- | --- | --- | --- |
| Player2211 | 0→1 | 18/18→17/17 | 40→40 |
| AmbushBandit2098 | 0→0 | 14/15 unchanged | 22→22 |
| AmbushBandit2116 | 0→0 | 14/15 unchanged | 22→22 |
| Scorpion2178 | 0→0 | 10/16 unchanged | 10→10 |

The scorpion moved from32,18 to39,13 and attacked; its zero exposure was not caused
by suspension of its AI. The bandits stayed at their ambush positions. Their zero
Parched is consistent with the blanket exception, not evidence of special native
physiology. Nonhostile villagers and recruited companions were not hand-played
in this comparison; their exemption follows the production call graph.

For a shared-material control, the player used ordinary Ember Spit southwest on
that same scorpion. At410 it had HP5, temperature558.1875, Burning=true and still
HeatResistance0. The log reported2 fire damage on its turn. This establishes real
fire vulnerability, not damage from natural sun or ash. Player and all three
nonplayer bodies had temperature/ambient25 before/after the sunlight interval.
No regional ambient-temperature writer was found: ThermalPart defaults ambient25.
Sun glare does not consult HeatResistance or ThermalPart at all.

Full readbacks:
[native observations](Verification/MachineSystems/BeatingEnvironment20261121/native-observations.json).
Earned F5 checkpoint410 archived at
`/private/tmp/coo-beating-actors-earned-20261121/ed8cb73c62154cf6aac918327c8a1387`.
Read-only inspection observed actors; ordinary input performed waiting/fire/save.
No actors, statuses, damage or time were injected. NativeSaveIsolation owned the
temporary save root. Input callbacks/device/settings were cleaned up and main
ended idle, scene clean, null save override, original seed/last scenario restored.

## Existing adaptation and AI, precisely

- SunStriker has basking/noon flavor but ordinary creature thermal behavior; no
  glare-tolerance Part or HeatResistance is declared. Scorpion likewise has no
  special sun/heat capability. Neither has a sun-driven activity schedule.
- GlassScorpion has a Glass material and a real ignition threshold1500 versus
  Scorpion500, with different heat capacity/brittleness. This is a material
  difference, not automatic immunity to fire damage or dehydration.
- TentRightHost actually equips a LeatherCap and boots, a practical cultural
  protection. Its cap currently makes no difference to natural NPC glare because
  the NPC is excluded before head-covering could matter. MarlbackTunnelguard's
  cap is probabilistic; covered and uncovered individuals both avoid glare.
- `AIWellVisitorPart` is a random5%-per-bored-turn walk beside literal `Well`.
  It does not test thirst/Parched, drink, spend water or recognize a Cistern.
  Base content attaches it to Farmer, not the TentRightHost. It is not evidence
  of working wasteland hydration behavior.
- Generic combat/status AI is real. Some actors with TacticalSupply can use
  water against burning during combat. Environmental navigation has finite costs
  for supported dangers; it does not universally avoid every hazard, and has no
  sun/shade/thirst cost. Do not present these as a complete survival AI.

The source labels the sunlight divergence explicitly in
`Docs/FELLING-W1-W2-PLAN.md:721`: avoid permanently parching every outdoor native.
This is understandable protection against unattended settlement attrition, but
it diverges from the standing shared-capability direction. Lore/Factions/07_TentRight.md
grounds survival in wells, clothing, shelter and hospitality, with people moving
with water/season; it does not establish universal biological NPC immunity.

## Recommended next slice (proposal, not implemented)

1. Put exposure state on each susceptible actor, driven by one world-time policy;
   avoid a single static counter or per-actor speed multiplying sun exposure.
2. Model susceptibility/tolerance explicitly. A native animal's adaptation, a
   human's headwear, a roof and an awning should each have an intelligible cause.
   Foreign companions/visitors should use those same rules. Heat resistance alone
   need not imply dehydration immunity; keep the distinction explicit.
3. Add a small shelter/water response for participating NPCs before enabling
   their exposure: seek accessible shade, protect the head, use finite carried
   water or an allowed well/service. Avoid emptying every communal well in a
   simplistic loop or creating player-maintenance chores to keep towns alive.
4. Prove one exposed visitor, one protected host, one explicitly adapted animal
   and a companion beside the player, with common shade/water, interruption and
   reload. Bound unloaded-zone reconciliation; no wall-clock death simulation.

Do not simply run the current glare method once for every NPC. It shares a static
counter and assumes the player callback cadence; that would mix actors' exposure
and make population/action speed change the outcome. This investigation does not
authorize claiming the proposed exposure/AI work is already shipped or accepted.

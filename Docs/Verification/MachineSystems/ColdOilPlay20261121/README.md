# Native cold/brine and camp oil chain — November 21, 2026

Root used ordinary GameBootstrap keyboard/menu input in the authored
ProofingWorkshopShowcase seed64 fixture (400HP, Strength70/cap5000).
These are fixture interaction checks, not ordinary acquisition/balance evidence.
No gameplay methods, grants, turn jumps or native tests were used for this play.
Raw observations: [observations.json](observations.json).

- Loaded frost kit2145 with one FrostLichen (8→7). Made camp through C at40,9:
  five fixtures placed, two water-dependent trays remained carried. Stoked hearth
  2147 with3timber+1strike; timber30→27, striker20→19.
- Camp still2148 settled flask2231's12mire into3oil70→80. Fuel29.75→24.5
  includes5batch plus0.25ordinary burn. Three other mire flasks stayed12 each.
- Spread one GlimmerBrine8→7 on36,9 at110→120. This is a finite aqueous film,
  **not a freshwater pool**. Laid cold trap2412 at120→130. Stepping into it
  yielded tick150/player36,10, ice3, Frozen, slip and consumed trap. Forced
  blocked turns are included in that interval. Ordinary waits thawed by210,
  HP400; film/ice cleared. Negative Frozen.Duration is the existing Cold model,
  not evidence of permanent freeze.
- Loaded remaining kit2146 with FireMoss8→7 at210→220. F5 archive, then fresh
  Play/Continue retained tick220, oil3, same flask/camp IDs and loaded kit.
- Poured **one** oil west to35,10 at220→230: flask3→2, pool2706 Volume1 and
  oil film. Laid fire trap2707 at230→240. Step240→250 caused HP400→388,
  consumed trap/film and left embers3/smoke2, no Burning. This contrasts with
  the previously recorded bare-grass fire trap's zero direct damage.
- Left260, waited290, reentered300: HP388, no retrigger, no tile residue/cloud.
  Pool2706 still oil1 and contact applied Oily again. F5 and fresh Play/Continue
  retained tick300/HP388, spent kits, flask2231 oil2, hearth2147 fuel19.75,
  timber27 and remaining mire flasks12. [Visible aftermath](spent-aftermath.png).

## Important open conservation issue

Independent bounded source review identified a P2 follow-up: ordinary poured
oil's recoverable pool survives combustion of its projected tile film. Root
observed surviving Volume1; recovery/re-pour/re-ignition was source traced, not
played in this run. LiquidVesselService adds pool volume; Zone.ProjectPool adds
the coating; TileReactionSystem removes the coating; consumption callbacks retire
FiniteLiquidFilmPart owners only. Ordinary LiquidPoolPart contact still coats
entrants. Transfer conservation tests do not prove combustion depletion.

This is an existing abstraction boundary, not a new trap-only defect. Do not
claim the poured unit was consumed or accept the finite-fuel foundation. Next
slice needs native RED/countercases for poured combustible volume, source/film
ownership, mixed/co-located pools, recovery, reload and natural-source exclusions;
do not attach a noncollectible thin-film part as a shortcut.

## Scope and preservation

Earned archives (game d3c56928ecec4cd0a4532b3402867974):
`/private/tmp/coo-cold-oil-ready-earned-20261121` and
`/private/tmp/coo-cold-oil-spent-earned-20261121`.
No cold armed reload, NPC trigger, Grove refusal or independent gameplay operator
claimed. Frost/freezing reactions, finite still output and fire/oil reaction were
played; all twelve complete-machine gates remain open.

Main stopped clean/nullroot; temporary input removed. Fresh hashes:84 foreign
source files and861 normal game files unchanged against retained baselines
(Unity analytics/TestResults excluded). No fresh whole-directory/prefs byte-exact
claim. Independent source/evidence critique is not independent game operation.

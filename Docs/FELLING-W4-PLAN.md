# W4 — The Grovelands: Choir country

> Plan-to-disk per CLAUDE.md. Design source: `Docs/FELLING-WORLD-DESIGN.md`
> §3.4 (derived design, NOT canon); canon: `Lore/` — especially
> `Lore/Codex/11_ChoirGroveSign.md` (the grove-edge sign, quotable
> verbatim), `Lore/History/05_Spirits.md` (the Wedded),
> `Docs/Design/WORLD-INGREDIENTS.md` (GroveRed brew-math, already
> designed). Sibling precedent: `Docs/FELLING-W3-PLAN.md` — the Sodden
> arc this plan mirrors, including its review cadence and its cuts.

## 0. Scope

The Grovelands stops borrowing the jungle's everything. Identity:
"where the substrate surfaces at scale — and it is KIND, which is the
horror." Everything grown, not made; the absence of manufactured
stuff is the fingerprint. Six sub-milestones: formations + glow,
grove rules, bestiary + tendril activation, the Bloom (pruned v1),
Cinderhold + the pruning contract (with the R1 promotion), close-out.

## 1. Verification sweep — corrections table

| Design/plan claim | Verified reality | Evidence |
|---|---|---|
| "Shamblers, Rotlings, Mosshulk (all ship)" | **FALSE for Shambler** — Rotling and Mosshulk exist in Objects.json; `Shambler` does not. WORLD-INGREDIENTS.md also lists `ShamblerSporeSac` as a Grovelands harvest. Both must be authored in W4.3. | Objects.json blueprint census |
| "ChoirTendril (ships, with dialogue)" | TRUE — `ChoirTendril` blueprint + `ChoirTendril_1` (37 nodes) in RotChoir.json. W4's job is SITING it (TendrilFen), not writing it. | RotChoir.json |
| "GroveShrine stamp ships" | TRUE — LandmarkBuilder.cs:496, with the five named choir NPCs (Mogu/Grib/Nam/Sien/Sopp, 5-node conversations each). | LandmarkBuilder.cs:491-559 |
| "`veined-pulse-mycelium` liquid ships" | TRUE — LiquidDefinitions/veined-pulse-mycelium.json. | ls LiquidDefinitions |
| "spore gas pockets (`fungal-spores` gas ships)" | TRUE — GasDefinitions/fungal-spores.json, BehaviorKind FungalSpores. | GasDefinitions |
| "Motes: Spore (ships)" | TRUE — AmbientMotesRenderer.MoteKind.Spore. | AmbientMotesRenderer.cs:22 |
| "movement cost +1 (soft ground)" | **NO SUBSTRATE** — no per-tile movement-cost system exists (Speed is per-actor). CUT from W4 (see §1a). | MovementSystem.cs sweep |
| "resting in a grove ... triggers the encasement offer" | **NO REST VERB** — RestAtInn is a conversation action (inn-only); there is no wilderness rest input. The offer needs a different observable (see W4.5 + R3). | InputHandler sweep, ConversationActions.cs:698 |
| "Choir reputation" | The faction's registered name is **RotChoir** (Factions.json:44) — every rep hook uses that string, and PlayerReputation already tracks it. | Factions.json |
| "Concord tendril-pruning contracts" | Cinderhold (6,6) IS the Grovelands' only village POI and IS SaccharineConcord — the contract has a natural home. But Concord is shared by three places, so Cinderhold profiling triggers W2 R1's rule: THIRD name-keyed profile → promote to a `Place.Profile` field (see W4.6). | WorldMapAuthoring.cs:200-218 |
| "Olderdeep / Deepest Cathedral" as W4 places | NOT village POIs — they are future sinkhole mouths (WorldMapAuthoring.cs:193 note), i.e. W5-vertical scope. W4 does not touch them. | WorldMapAuthoring.cs:193 |
| GroveRed "vital:3, toxic:1" | The brew substrate ships (Alchemy/BrewItemPart with `EffectsRaw` string + BrewProperty). GroveRed authors as an item with BrewItem params per WORLD-INGREDIENTS.md:46 (value 18, the flavor line as written). | BrewItemPart.cs:25-38 |

### 1a. Scope-prune with rationale (the boat-lanes list)

- **CUT: per-tile movement cost** — no substrate; inventing one for
  one biome is a W-scale engine feature. The bog didn't slow you
  either; consistency is itself a defensible read.
- **CUT: Encasement Colonnade formation** — Tier-4, pairs with
  Wall-Catching and the Cathedral (both W5-vertical). The other four
  formations carry the identity without it.
- **CUT: the Cathedral set-piece, Wall-Catching, memory-consumption
  services, sinkhole village mouths** — all W5-vertical by the design
  doc's own build order.
- **CUT: Spore-Block rooms / Drosera rings** (anti-Bloom economy) —
  catacomb-siting content; the W4 Bloom v1 doesn't need its
  counter-economy to land.
- **CUT: Choir-Iron-carried-openly insult hook** — needs an
  "openly carried" observable (equip-state per zone biome); small but
  fiddly, and the insult reads better once Choir dialogue can react
  to it. Defer with this note.
- **KEPT lean: the Bloom** — v1 scope defined at W4.4; the front
  overlay + driven hosts + eruption ship, the cure-by-Choir ritual is
  a conversation hook, and everything else Bloom-related waits.

## 2. Content-readiness

- 🟢 ChoirTendril + 37-node tree; GroveShrine stamp + five named
  choir NPCs; fungal-spores gas; veined-pulse-mycelium liquid; Spore
  motes; LightSourcePart (the tenth fire's part); RotChoir faction +
  rep track; Rotling, Mosshulk; TrapperCamp/VineWall ambient stamps.
- 🟡 GroveRed/GroveHoney designed (WORLD-INGREDIENTS.md) but not
  authored; Shambler + ShamblerSporeSac named by design but absent;
  BloomedEffect named, unbuilt (GoalHandler substrate exists —
  CommandGoal/FleeGoal precedents for a compelled goal).
- 🔴 none — every W4 dependency either ships or is authored in-phase.

## 3. Sub-milestones (smallest blast radius first)

**W4.1 — Four formations + the glow + the look pass.** Enum appends
after BogFace: `Grove, TendrilFen, FruitingWall, CompostingField` +
`GrovelandsPool` (Grove×2, TendrilFen×2, FruitingWall×2,
CompostingField×1 + one more Grove — groves ARE the biome) +
`GrovelandsFormationBuilder` (priority 2500, Override + LastFormation,
FormationApplied diag, the W2 full-reachability repair wherever
solids are placed). New blueprints: `MycelialColumn` (solid 'O' or
'♣'-class glyph, pale ochre, **LightSourcePart radius 3 green-white**
— groves glow at night from across a chunk), `GroveSeep` (walkable
water source at the radial center — reuses the WellPart cure contract:
"the seep is free"), `SubstrateMat` (non-solid ground feature '.'
recolored — visual only, no cost, per §1a), `FruitingBody` (wall-class
'%' growth), `GroveSign` (the Codex/11 readable — grown wood, VERBATIM
canon text in the examine; the letters are grown, not carved).
Shapes: Grove = radial column ring + seep center + sign at the east
entry (canon: "at the east entry"); TendrilFen = braided lanes with
ChoirTendril entities sited (the 37-node tree FINALLY SPAWNS — the
formation hook, like MawToad in the copse); FruitingWall = short
dense growth lines; CompostingField = ordered rows of part-consumed
remains ("everything here is somebody" — voice-gated examine text,
state only). BiomePalette Grovelands case (dark loam, living light:
0.88/0.92/0.85). Containers: GrovelandsPool (HollowStump-heavy —
nothing manufactured; the Choir has no material culture, so NO
crates/chests in the wilderness pool). **The ambient STAMP catalog
too** (the W3 re-review lesson): Grovelands currently aliases
Jungle wholesale — GroveShrine belongs here, the Ziggurat does not;
W4.1 gives Grovelands its own array (GroveShrine, MendleafGarden,
HuntersBlind — the shared fields the re-review extracted) and pins
it like the Sodden's. **Non-solid scatter placement uses
BuilderSpawn.TryPlaceOnce from birth** (the OpenMire stacking
lesson — a walkability guard cannot see non-solid occupants).
**Gate: ASCII look pass on all four + a night-glow sanity note
(monochrome dump can't show light; record the honesty bound and
verify glow via LightSourcePart-present pins).** Tests mirror
SoddenFormationTests.

**W4.2 — The grove rules.** The sign's law becomes mechanics:
- *The seep is free* — GroveSeep drinks clean + cures (WellPart
  contract), pinned.
- *Eat nothing red* — `GroveRed` authored per WORLD-INGREDIENTS.md:46
  (BrewItem vital:3 toxic:1, value 18, the flavor line as designed —
  "the best healing reagent in the game AND it ruins every brew");
  `GroveRedGrowth` harvest bush sited in Grove/TendrilFen.
- *Do not dig* — harvesting a MineralVein in a Grovelands surface
  zone → RotChoir rep loss, immediate, with a message and a diag
  record (hook at the vein-harvest path; verify the exact seam at
  implementation — HarvestablePart or the vein's own part).
- *Fire is a crime* — an ignition (TryIgnite success) on an entity in
  a Grovelands surface zone → major RotChoir rep loss + diag. Hook at
  the ThermalPart.TryIgnite seam (verify-first: the event carries
  Source; the zone's biome is resolvable via WorldMap.FromZoneID).
Counter-checks: digging/burning in NON-grove biomes costs nothing;
the seep in a non-grove zone… does not generate (formation-only).
Tests: each rule + counter + diag emission per gate branch.

**W4.3 — The bestiary + tendril activation.** `GrovelandsTier1/2/3`
+ `GrovelandsLairGuards` replace the Jungle alias (Sodden's W3.4 is
the template; counter-pin that the STUMP still borrows). New
blueprints: `Shambler` (the design doc claimed it ships — §1 caught
the false premise; author it: slow fungal walker, Beasts… NO —
faction question: Shamblers are Bloom-driven fauna, faction Beasts)
+ `ShamblerSporeSac` (Corpse harvest, WORLD-INGREDIENTS.md:156),
`GlowMoth` (ambient, passive, glyph light — no LightSourcePart per
perf rules on moving entities; verify), `WineLeafSundew` (stationary
snare — REUSE GreatdewSnarePart with its own tuning; color =
feeding-state is a render note recorded as future work).
ChoirTendril population siting: TendrilFen guarantees 1-3 (the
formation hook); ambient tables may roll more anywhere Grovelands.
Gate 7-adjacent voice checks: tendril dialogue ALREADY exists —
W4.3 adds no tendril text, only placement.

**W4.4 — The Bloom, v1.** The Driving Bloom is TERRAIN, not a
faction: no dialogue, no rep track, never funny.
- `BloomedEffect` (TYPE_GENERAL | TYPE_NEGATIVE — curable in
  principle, but see the cure note): a compelled-goal effect — while
  it holds, the bearer's Brain gets a Bloom goal pushed each turn
  (move toward open ground / away from allies; attack what stands
  adjacent regardless of faction), the GoalHandler substrate's
  CommandGoal/FleeGoal shape. On the bearer's death: eruption —
  fungal-spores gas burst at the corpse (GasFactory, the W3.3 spawn
  path).
- **Bloom-front overlay**: a zone-level condition (authored flag or
  low-chance roll on Grovelands zones) that seeds FruitingBody
  eruptions through the zone + 1-2 driven hosts (existing creatures
  + BloomedEffect) + "the unfinished" set dressing (a Bloomed
  workshop stamp: tools down mid-task).
- **The cure requires the Choir**: CureTonicPart "All" must NOT
  strip it (mirror the oath's W2 lesson — but BloomedEffect IS
  negative… decision: the Bloom is cured ONLY by the Choir's
  early-stage ritual, a GroveShrine/named-choir conversation choice
  (CureEffect BloomedEffect action) — so BloomedEffect overrides
  its type to exclude the TYPE_NEGATIVE cure-all bit and documents
  why: the Bloom is not an ailment a tonic understands. R4 below.)
- Player infection path: spore gas exposure while the front holds
  (verify the FungalSpores gas behavior's infection hook — G.8d
  contagion exists; the Bloom rides it or sits beside it,
  verify-first).
Tests: compulsion overrides (a Bloomed guard attacks its own),
eruption spawns gas, cure-by-ritual works, cure-all does NOT,
counter: unbloomed hosts behave normally, save/load of the effect.

**W4.5 — The doll, and the quiet pieces.** The doll in the wall
(design gate 4): ONE authored Grovelands zone (tenth-fire pattern —
a const zone id, a bare pipeline variant if the roll needs quiet),
a `WovenDoll` woven into a mycelial column, uneaten, **no examine
text beyond bare sight** — pinned by a gate test like the tenth
fire's. Plus the encasement offer, v1 (R3): the offer lives with the
named choir NPCs at grove shrines — a conversation branch that
appears only when the listener is BADLY HURT (new predicate
`IfStatBelowPercent` — tiny, mirrored on IfStatAtLeast), gentle,
refusable, ONCE (a fact latch). Accepting it in v1 is declined by
the Choir ("not yet — you are only a little tired"; the actual
encasement is W5+ content) — the OFFER is the mechanic; the
refusal is spoken-no. This keeps canon's scene without shipping a
death-alternative system half-built.

**W4.6 — Cinderhold + the pruning contract + the R1 promotion.**
R1's own rule fires: Cinderhold would be the THIRD name-keyed
profile, so FIRST promote profiles to data — `Place` gains a
`Profile` string (e.g. "TentCamp", "ConcordPost", "Boatyard",
"ExcavationCamp", "Intake", "PruningPost"); CreateVillagePipeline
switches on it; the existing name/faction-keyed branches migrate;
PlaceProfileTests / SoddenProfileTests re-pin unchanged behavior.
Then Cinderhold: Concord post texture + a `PruningContract` quest at
the Concord factor NPC — the canonical first-act tradeoff: accept
(prune tendrils: harvest N ChoirTendril... no — cutting tendrils =
attacking Choir content; v1 shape: carry the pruning WRIT to the
grove and post it, or refuse) → **accept = RotChoir rep down,
Concord rep up + drams; refuse aloud = Concord rep down, spoken-no
register**. Both sides cost something; neither is wrong; the quest
text never says "free" in the Concord's mouth (voice gate). Mirror
the W3.6 wiring (StartQuest/fact/CompleteQuest, cross-file fact
lint, NoTrade on the writ).

**W4.7 — Close-out.** Reviews per SM inline + a three-lens workflow
if budget allows (the W3.7 pattern); hypothesis-driven deep audit
(6-12 player-flow hypotheses, RED-first); design-gate sweep (§9
gates 2, 4, 7, 10, 12 all touch W4 — the doll, Urqu language if any
text mentions pressure, the Choir never says *dead*, the Concord
never says *free*); ASCII look passes recorded; PlayMode honesty
bound; §5 log complete; exit statement.

## 4. Performance / observability

Per-frame additions: MycelialColumn LightSourcePart instances (the
lightmap already handles the tenth fire + village lanterns; a grove
ring is ~8-14 sources in one zone — within the existing budget, but
the look pass should note render timing if the editor is available).
Per-turn: BloomedEffect pushes one goal per bearer-turn (bounded by
bearer count); the Bloom-front seeding is gen-time. Diag: every gate
that rejects emits (`worldgen/FormationApplied` Grovelands;
`effect/BloomCompelled`, `effect/BloomErupted`; `faction/GroveDug`,
`faction/GroveBurned` — or the existing rep-change records if they
already carry reasons; `quest/*` ships). New caches: none.

## 5. Implementation log

### W4.1 — Four formations + the glow (SHIPPED)

**Files:** Formation.cs (Grove/TendrilFen/FruitingWall/
CompostingField + GrovelandsPool 3-2-2-1), GrovelandsFormationBuilder
(NEW), OverworldZoneManager (pipeline gains the builder), Objects.json
(MycelialColumn 'O' &w + LightSource r3, GroveSeep '~' &C + Well,
FruitingBody '%' &m, GroveSign 'I' &G with Codex/11 VERBATIM,
CompostRow '%' &w non-solid), ContainerPlacementService
(GrovelandsPool: HollowLog/WovenBasket/Sack — nothing manufactured;
PoolFor + ContainerKind made public for the contract test),
BiomePalette (Grovelands case), LandmarkBuilder (GroveShrineStamp
extracted; Grovelands ambient array = shrine + mendleaf + blind;
Ziggurat stays jungle-only), GrovelandsFormationTests (23 tests).

**SCOPE DIVERGENCES:**
- SubstrateMat CUT at implementation: the palette carries the ground
  look, and the tile atlas keys raw Latin-1 chars (a fancy glyph
  falls back to '?'), so a mechanics-free mat entity risked an
  unreadable glyph for zero gain. Same atlas fact moved the column
  from the design's '♣' to 'O' (color-differentiated from the cyan
  Well, per the vein convention).
- The grove CLEARS ITS OWN FLOOR (one allowed dig, the Causeway
  precedent): the design table's own words are "columns ringing a
  seep, OPEN floor", and without the clearing the look pass showed
  the ring drowned in forest.

**Look pass (the gate; two rounds):**
- Round 1: CompostingField unmistakable (ordered '%' rows; the
  GroveShrine stamp rolled beside it — the five named choir NPCs
  around their fire, placed at last); TendrilFen's tendrils + bank
  growth read (its water veins are TileState coatings — the known
  monochrome-dump bound); FruitingWall thin; **Grove FAILED
  twice-over** — at the first authored cell nothing built (a random
  lair POI routes around the formation builder; dump cells must
  dodge lairs), and at real forested cells the ring drowned, the
  seep and sign SILENTLY absent (IsOpenGround guards = the
  phantom-neutral-pass class; the open-grass tests could never see
  it).
- Fixes: the floor clearing (above); the seep GUARANTEED on the
  cleared center; the sign walks an eastern arc (clearing its
  doorpost) with a cleared-floor fallback; FruitingWall 10-13 lines
  at 90%.
- The forest-fixture pin then caught one more real bug:
  PlaceSolidIfHarmless demanded GLOBAL reachability, so any
  pre-existing pocket (upstream scatter this builder didn't cause)
  vetoed every sign and tendril forever. Now a DELTA contract —
  placement must not make reachability worse — which is the honest
  rule on imperfect zones.
- Round 2: the Grove reads as designed — clearing, gapped ring, '~'
  at center, 'I' at the east entry, lone columns glowing beyond;
  FruitingWall reads as broken growth lines.

**Honesty bounds:** night glow is invisible to the dump — verified
at the part level (LightSource radius 3 pinned); TendrilFen's veins
likewise coating-invisible. Tests: seep/sign guaranteed ON DENSE
FOREST (15 seeds), every-open-cell reachability 40 seeds ×3 solid
formations, TryPlaceOnce stacking pins (R9a), tendril 1-3 + only-
the-fen counters, verbatim sign pins, container/catalog contracts.

**Tests:** 6915 → 6938 (+23). Green headless ×3 across the fix
rounds (the forest pin was RED twice — first for the missing sign,
then for the global-reachability veto — before going green).

### W4.1 review round 1 (workflow, post-commit) — 12 confirmed

Two-lens adversarial workflow (mechanics / fidelity; 14 agents, one
skeptic per finding). All 12 confirmed findings fixed in the
review-fix commit; RED confirmed for the behavioral ones.

- 🟡 **The seep could be entombed in VineWall** (~1 grove in 16 —
  the verifier SIMULATED the CA to quantify it): the clearing skips
  walls, production terrain IS walls, and the forest pin used Trees
  (Solid tag, no Wall tag) so it couldn't see the class. Fixed: the
  one allowed dig extends by exactly one cell — the water finds its
  way up through anything. Pinned deterministically (an all-VineWall
  zone) plus the strengthened scatter pin with cell-level open+
  reached asserts.
- 🟡 **ConnectivityBuilder judged passability by the Solid TAG while
  movement uses physics** — it certified corridors THROUGH the
  seated ChoirTendril (untagged Physics-solid) and its carve could
  delete Solid-tagged formation guarantees. Fixed: passability is
  now !BlocksMovement (the same test movement uses, benefiting every
  biome), and GroveSign drops its Solid TAG so no carve can ever
  remove the law.
- 🟡 **TendrilFen was one sine strand, not the design's braid** —
  two phase-offset strands now cross and part; pinned by braid
  geometry (columns with coated cells ≥4 rows apart).
- 🟡 **FruitingBody/MycelialColumn authored Combustibility with NO
  Thermal part** — every ignition path routes through ThermalPart,
  so the tuning was dead data and the grove's signature scenery was
  invisible to W4.2's coming fire-is-a-crime hook. Both get
  flashpoints (320/1.2, 420/2.0), DeadTree too (300/1.2 — a W3.1
  instance of the same class), and a NEW inverse lint makes
  Combustibility-without-Thermal unshippable (six pre-existing
  furniture pieces exempted with a recorded reason: the
  furniture-combustion pass is future work).
- 🟡 **The catalog validation gates were hardcoded six-biome lists**
  — Sodden's and the Grovelands' own arrays shipped unvalidated.
  Both gates now iterate every enum value.
- 🟡 **PlaceSolidIfHarmless could OCCUPY a one-cell pocket** (the
  delta counts filling a stranded cell as improvement — a sign
  nobody can ever read). Isolated-cell veto added.
- 🟡 **CompostRow's "loot" half is orphaned** — recorded cut with an
  owner: W4.3's harvest pass gives CompostRow a Harvestable ("the
  pile gives things up"), which is that SM's native territory.
- 🔵 GroveSign's six-phrase pin left ~8 sentences unguarded →
  whole-text equality against the canon constant. 🔵 The poolless
  exemplar churned four times → now legacy biomes (Jungle/Cave),
  which never join the formation machine. 🔵 GroveSeep's authored
  '&C' was silently overwritten by the water definition's '&c' at
  runtime (LiquidPoolPart.ApplyDefinitionRender) — authored data now
  matches the definition-driven contract. 🔵 **HollowStump was a
  false premise in THIS plan** (§3 "HollowStump-heavy"): it is a
  Solid Harvestable yielding 1-4 GoldCoin, has no Container part —
  and a coin-dispensing stump contradicts "nothing manufactured".
  The shipped HollowLog-led pool is the correct reading; recorded
  here as the §1-style correction the sweep missed.

### W4.1 review round 2 (hypothesis audit) — 4 pins, 1 refuted, 2 flakes

Player-flow hypotheses against the fixed W4.1, RED-first where a
fix would have been needed (none was — all four pinned as correct):
- ✅ Drinking at the seep cures Parched through the REAL action wire
  (the sign's promise, end to end).
- ✅ A burning column burns down (round 1's flashpoint made it
  possible) and its glow needs no cleanup — the lightmap re-scans
  entities, so a removed column is a dark column.
- ✅ The glow survives save/load (LightSourcePart reflection reach).
- ✅ Attacking the Choir's tendril under the cloth IS oathbreak —
  RotChoir is factioned, therefore a person under IsPerson, and the
  grove-edge sign carries the ruling itself: "EVERYTHING HERE IS
  SOMEBODY." Kept as designed, pinned with the canon line.
- ❌ REFUTED (recorded so it stays refuted): "the fen-seated tendril
  trades with empty shelves" — TradeStockBuilder only stocks
  Villagers, but TraderPart SELF-stocks on ObjectCreated from its
  own ChoirStock table (TraderPart.Apply), so the tendril arrives
  with goods and a 70-dram purse.
- Two UNRELATED failures in the pin run identified as flakes by
  rerun (both green): DiagPerfTests' 200ns ceiling (machine load —
  workflows were running) and the Campfire flicker probability test.
- Look re-verify: the fen now reads as TWO winding growth bands that
  converge and part — the braid; the grove unchanged and correct.

### W4.2 — The grove rules (SHIPPED)

**Files:** GroveLaw.cs (NEW — the sign's law as mechanics:
IsGroveGround reads the biome statically off the zone id; OnDig -15
RotChoir, OnIgnite -40, player-only, grove-surface-only, each with a
faction-channel diag naming the act), ThermalPart (OnIgnite charged
at the one seam every ignition passes through), HarvestablePart
(OnDig charged at the harvest seam), Diag ("faction" joins the
default channels), Objects.json (MineralVein tags on the three
veins — the law's honest hook; GroveRed: ReagentItem vital:3
toxic:1, Commerce 18, the WORLD-INGREDIENTS flavor line verbatim;
GroveRedGrowth: non-solid forage bush yielding 1-2), the builder
sites red growth at grove edges + along fen veins (TryPlaceOnce per
R9a), GroveLawTests.cs (9 tests).

**Design notes honored:** the seep's rule shipped in W4.1; "eat
nothing red" is a WARNING, not a fine — picking the red costs
nothing and the reagent is exactly the designed trap (best vital in
the game AND a brew-ruiner); digging is defined by the MineralVein
tag, so foraging never triggers it; fire charges per player-caused
ignition and NOT on spread (propagation events carry the burning
entity as source) — a multi-target fire spell charges per thing lit,
deliberately: the Choir counts acts.

**Fixture lesson (recorded for the next fire test):** ignition
fixtures must mirror MaterialPrimitivesPhaseATests' proven shape —
Hitpoints stat, 0-1 Combustibility scale, joules under the
|delta|>200 thermal-shock branch. The first fixture used a 500-joule
blast and tripped paths the test never meant to probe.

**Tests:** 6945 → 6954 (+9): dig charge + elsewhere/NPC/forage
counters; fire charge + elsewhere/spread counters; the reagent's
numbers and flavor pinned; edge-siting existence.

## 6. Critical review of this plan (before implementation)

**R1 — The profile promotion is load-bearing, do it FIRST inside
W4.6.** Migrating existing branches while adding Cinderhold in one
commit is two behaviors in one blast radius; split into W4.6a
(promotion, zero behavior change, all profile tests still green)
and W4.6b (Cinderhold content on the new field).

**R2 — The Grove's column ring is a sealing risk.** A radial ring of
SOLID columns around the seep is exactly the shape that traps the
center. The W2/W3 lesson applies verbatim: track placed columns, run
EnsureAllReachable (every-open-cell), and the ring must always have
gaps (the sign says "walk anywhere; everything is path" — the canon
IS the reachability requirement).

**R3 — The encasement offer's trigger is the plan's softest spot.**
No rest verb exists; the shrine-conversation + badly-hurt predicate
is a real scene but NOT the design doc's "lie down anywhere soft."
Recorded as a deliberate v1 divergence at plan time (not discovered
later): the full rest-trigger follows whenever a wilderness rest
verb ships. If the user wants the verb instead, it is an input-layer
feature and W4.5 grows by one SM.

**R4 — BloomedEffect vs the cure taxonomy.** W2 established
cure-all = TYPE_NEGATIVE. The Bloom is negative but must NOT be
tonic-curable (the cure-requires-the-Choir is its narrative
function). Shipping it WITHOUT TYPE_NEGATIVE is the minimal honest
mechanism, but it inverts the flag's meaning ("negative" would now
mean "tonic-curable" not "bad for you"). Alternative: keep
TYPE_NEGATIVE and add an `Uncurable` override consulted by
CureTonicPart. Decide at W4.4 with a one-paragraph note; either way
a counter-test pins panacea-does-not-cure-the-Bloom.

**R5 — WineLeafSundew reusing GreatdewSnarePart** is content reuse,
not code reuse — verify the part's tuning fields cover the sundew
(they do: GrabChance/HoldTurns/Corrosion) and give it its OWN
diag-visible identity via the blueprint name in SnareGrabbed records
(already carried by ParentEntity).

**R6 — The pruning contract must not weaponize the courier lesson
in reverse.** The writ is NoTrade from birth; the accept/refuse rep
deltas are asymmetric on purpose (accepting angers a god's
landscape; refusing angers a shipping company) — pin BOTH deltas so
a balance pass can't silently flatten the tradeoff.

**R7 — Tendril siting must not double-spawn.** TendrilFen guarantees
1-3 ChoirTendril AND the ambient tables may roll more — cap the
formation's guarantee count and let tables be tables (the MawToad
precedent), but counter-pin that a NON-fen formation zone can still
roll tendrils from tables without the guarantee (no bleed
assertion confusion).

**R8 — The Bloom-front must never fire on Cinderhold or a shrine
zone in v1** (a Bloomed village is a whole design conversation) —
the overlay roll excludes POI zones, pinned.

**R9 — Carry the W3 re-review's structural lessons forward, not
just their fixes.** (a) Every non-solid scatter placement goes
through TryPlaceOnce (see W4.1). (b) Any new AoE or multi-target
loop snapshots its targets before damaging any (RouteDamage is
side-effectful on cell lists since W3.3 — the Pyroclasm lesson).
(c) Any new NPC that belongs to an Order gets a faction pin in its
profile test the day it ships (the CurationSorter lesson). (d) Any
prop text that teaches a mechanic must be backed by the mechanic or
trimmed (the Greatdew lesson) — the Bloom's driven-host messaging
in W4.4 is the first place this bites.

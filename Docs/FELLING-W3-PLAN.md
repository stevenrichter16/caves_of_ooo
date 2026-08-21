# W3 — The Sodden: the flood's country

> **Status: planned + critically reviewed, ready to implement.**
> Written 2026-08-20 from a three-agent research sweep (canon lore /
> code substrate / spec digest — full transcripts in the session
> workflow logs). Continues `Docs/FELLING-W1-W2-PLAN.md` (W1+W2
> shipped; see its §7.11). Same discipline: verify-first, TDD,
> look-pass gate, headless batchmode verification, mid-phase and
> close-out adversarial review workflows.

## 0. Scope

Six formations (open mire, peat-cuts, reed maze, drowned copse, the
causeway, bog-face), the Bog-Taken as readable terrain-bodies (three
pre-Felling ones hand-placed at the Drowned Ledger), methane peat,
the Sodden's own bestiary/containers/stamps, place profiles for
Sumphold + the Drowned Ledger, and the body-courier contract
(sealed-to-Curation-standard, the canon Codex/06 clause). Dependency:
W0.6 only (met).

## 1. Verification sweep — corrections table

Everything below re-verified against `Lore/` (canon) and the code by
the research sweep; design-doc wording corrected where it drifted.

| Claim | Finding | Cite |
|---|---|---|
| "Sumphold, a Sodden town" | **Sumphold sits on a SPREAD cell** — (15,6) is 'S', tier 1, the last dry cell at the bog's border. This is canon-FAITHFUL, not a map bug: "raised-ground town at the EDGE of the bog country... Gateway to the Sodden places." Profile keys on the place name, not on biome. | `WorldMapAuthoring.cs` row 6; `Lore/History/02_Geography.md:69` |
| Drowned Ledger is a Recension camp | Canon calls it a **bog-VILLAGE** with a **joint** Recension + Pale Curation presence ("Sorter expeditions — joint with the Recension"), tier 2 on the authored map (design doc said 3). | `02_Geography.md:77`; `03_PaleCuration.md:106` |
| "Bog-Taken never resolved — Mystery Ledger discipline" | The consciousness ambiguity is **adopted design discipline (IDEAS.md:409), NOT a formal Mystery Ledger entry**. W3 treats it with ledger discipline (never adjudicated in any text) but does NOT claim ledger authority; formally ledgering it would be a canon amendment for a lore pass, not a code commit. | `Lore/MYSTERY-LEDGER.md` (no entry); IDEAS.md:409 |
| "bodies sealed to Curation intake standard" | Canon verbatim — Clause the fifth of the carriage contract: "The Concord will not carry... bodies not sealed to Curation intake standard." The courier quest quotes the real clause. | `Lore/Codex/06_ConcordCarriageContract.md` |
| bog-mire liquid | **Already defined** (Agi −2, 1 acid/turn, FireDampen 50) with **zero consumers** — the third dead-content revival of this arc (after freeze_water and MineralTradeService). | `LiquidDefinitions/bog-mire.json`; grep |
| methane peat = "one JSON param" | Half-true: `BurnOffGasPart` exists and its own docstring names "a peat bog venting methane when torched", but **no flammable/marsh gas definition exists** (7 gases, none fits). One new GasDefinitions json + the PeatBog param. Verify-first at W3.3: read GasPlasmaPart to pick the behavior kind. | `BurnOffGasPart.cs:10-11`; `GasDefinitions/` |
| "boat lanes on river edges" | **CUT (scope-prune with rationale):** no boat mechanic exists, water is universally walkable (`Cell.IsPassable == !IsSolid`), and the authored map has **zero river cells in the Sodden region**. Canon gives Sumphold boat-BUILDERS (texture), not boat travel. Boats are a future system, not a W3 line item. | `RiverRows` all '.' in the NE; `Cell.cs:210` |
| Bog-Taken emerging "a thousand years later" | Canon: the drowned of the cataclysm, still emerging from peat; Bog-Take is the one **natural** preservation method — "found, not made"; Salt-Pure sect holds it "tolerated but lesser" (dialogue texture). | `01_Spine.md:169`; `08_MaterialCulture.md:91`; `03_PaleCuration.md:54` |
| Word ban | **"mummy" is banned from in-game text** — directly constrains every Bog-Taken examine string. Voice gates: Curation never says *dead*; Concord never says *free*. | `00_Canon.md:373`; `VOICE-CARDS.md` |
| Gin Frogs | In the bestiary AND under Mystery gate 7: nothing may make them load-bearing, nothing explains them. They may EXIST as spawns; no text frames them. | design §9 gate 7 |
| Tithe-in-the-Peat | A canon bog numen (takes a name for safe passage) the design doc never sited. **Not W3** — numina are their own pass; recorded so it isn't forgotten. | `05_Spirits.md:195` |
| Standing water generator | None exists — RiverChunkBuilder only flows one axis. Mire is formation work (BrineLens's blob pattern is the closest shipped shape). | `RiverChunkBuilder.cs:42-49` |

## 2. Content-readiness

| Piece | Readiness | Why |
|---|---|---|
| Formations | 🟢 | machine proven twice (W1.1, W2.1); BrineLens = the mire template |
| MirePool / Duckboard / DeadTree / PeatBank blueprints | 🟢 | PeatBog/BrinePool patterns; bog-mire liquid ships |
| Bog-Taken bodies | 🟡 | new blueprint family + hand-placement; ALL text through voice gates, "mummy" ban |
| Methane | 🟡 | new gas definition (verify-first: behavior kind) + one blueprint param |
| Bestiary | 🟡 | 3 new small creatures (Reedfrog, Bandfrog, MawToad); Greatdew has a real mechanic (see R4); Riverwarden's lunge-drag deferred |
| Place profiles | 🟢 | W2.6's seam, extended to key on Name for Sumphold |
| Courier contract | 🟢 | storylet substrate + QuestMarkerTriggerPart + SellMineral-style precedent all ship |

## 3. Sub-milestones (smallest blast radius first)

**W3.1 — Six formations + the look pass.** Enum appends after
BrineLens: `OpenMire, PeatCuts, ReedMaze, DrownedCopse, Causeway,
BogFace` + SoddenPool (mire/reeds common, causeway/bog-face rare) +
`SoddenFormationBuilder` (priority 2500, Override + LastFormation,
self-repair where solids are placed, FormationApplied diag with its
own biome name). New blueprints: `MirePool` (Terrain '~' dark,
LiquidPool bog-mire — the dead liquid's first consumer; TileStateSource
water), `Duckboard` ('=' walkable plank, non-solid), `DeadTree` ('T'
grey, Solid), `PeatBank` (solid cut-face wall, Material Peat,
flammable). Shapes: OpenMire = mire blobs + tussock paths (BrineLens
pattern, guaranteed-crossable via the W2 full-reachability repair);
PeatCuts = comb trenches of mire between PeatBank rows (worked banks —
W3.2 seats bodies in the faces); ReedMaze = Reeds braid with 1-cell
channels (crossability sweep mandatory); DrownedCopse = DeadTree
scatter over water coating; Causeway = ONE Duckboard line E-W with
`ClearFor` rights (the safe line); BogFace = a long PeatBank edge with
strata. Sodden's own container pool (Basket/HollowLog fit — keep
Jungle's pool? NO: canon is boats and peat — `SoddenPool`: WovenBasket
4, HollowLog 2, Crate 2 (boat-builder's), Sack 2) + HazardTerrain stays
JungleEntries (PeatBog 40 already leads it — correct). BiomePalette:
own case (tea-water, olive sedge). **Gate: ASCII look pass on all six
via AsciiDumpTool before commit.** Tests mirror BeatingFormationTests
(stable selection, reachability, signatures + counter, every-open-
cell-reachable sweeps on PeatCuts/ReedMaze/OpenMire).

**W3.2 — The Bog-Taken.** `BogTakenBody` blueprint ('&' peat-brown,
non-solid, Examinable — text written under the voice rules, no
"mummy", no adjudication of consciousness, "found, not made" as the
register's frame). PeatCuts + BogFace formations seat 0-2 in their
banks (the ordinary drowned — "a centuries-deep cemetery whose
contents are visible"). THREE pre-Felling bodies as a distinct
blueprint (`PreFellingBody`) hand-placed ONLY in the Drowned Ledger's
zone (authored placement, W2.8 tenth-fire pattern); their examine
text yields fragments and attributes them (a Recension marker tag) —
never confirmation of anything (Naro gate). Render-coverage entries
for every new glyph. Tests: placement density bounds, the
Drowned-Ledger-only pin for pre-Felling bodies (counter-check: no
other Sodden zone ever has one), a text-lint test asserting the
banned word never appears in any shipped Examinable string
(game-wide — cheap and permanent).

**W3.3 — Methane peat.** Verify-first: read `GasPlasmaPart` +
GasDefinitions schema; author `marsh-gas` (flammable-flavored
behavior kind chosen from what ships; Seeping). PeatBog blueprint
gains `BurnOffGas { GasId: marsh-gas, DamageTriggerTypes: Heat;Fire }`
— the part's own docstring finally honored. MirePool gets it too
(same peat). Tests: burning a PeatBog vents the gas (TakeDamage with
Fire attribute → GasFactory spawn → gas entity present), non-fire
damage does not (counter-check), the chain (gas cloud adjacent to a
second PeatBog propagates on ignition — if the shipped gas behaviors
support ignition; otherwise the chain is a documented 🧪 deferral and
the vent alone ships).

**W3.4 — The Sodden's bestiary.** `SoddenTier1/2/3` + lair guards
(replacing the Jungle alias). New blueprints: `Reedfrog` (passive,
flees, Harvestable resource — the slime-pool ecology note goes in the
docstring for W5), `Bandfrog` (skin-contact damage on melee contact —
verify-first: the on-being-hit reflect pattern; if none ships, its
contact damage is a natural-weapon-on-defense deferral and v1 ships
it as a weak poisonous biter), `MawToad` (den in drowned copses —
formation hook: DrownedCopse spawns them). Gin Frogs: spawn as
`GinFrog` (simple hopper, zero explanatory text — gate 7 pinned by a
test asserting its Examinable is empty or purely physical).
**Greatdew (R4 decision): ships v1 as a stationary snare-plant** —
solid plant; a creature entering an adjacent cell rolls a grab
(RootedEffect + acid tick per turn held); NOT attacking it lets the
hold expire ("stillness passes") — implemented on the shipped
TriggerOnStep/adjacency substrate if it fits (verify-first), else
deferred whole with the reason written here. Danger thickens per tier
(counter-checked); indicator species stay absent (same D3 pin as the
Beating).

**W3.5 — Place profiles: Sumphold + the Drowned Ledger.** The W2.6
seam grows a name key (Sumphold's faction is Villagers — the profile
block for it keys on `poi.Name`). Sumphold: boat-builder texture
(`BoatFrame` prop stamp — canon "boat-builders"; a `TollRolls`
readable on the bridge approach quoting the Codex/10 toll-keeper
line's register, NOT its text), peat-cutter NPCs (reuse Villager +
a `PeatCutter` blueprint with Harvestable-adjacent flavor), and the
Bog-Taken trade counter (dialogue speaks the controversy — canon:
"sometimes sold (controversially) to the Concord"). Drowned Ledger
(faction Palimpsest → profile): `ExcavationCamp` stamp — tents,
numbered stakes (`SurveyStake` prop), the reading-tent (interior-
marked, a `ReadingTable` with a body ON it — the Codex/10 scene as
architecture), a Recension scribe NPC + a Curation Sorter NPC (joint
presence per canon). Their dialogue: Recension card ("attribution
before assertion"), Curation card (never *dead*; "Status:
continuing"). W3.2's three PreFellingBody placements live here.

**W3.6 — The body-courier contract.** A repeatable quest-class at the
Drowned Ledger: the Curation Sorter offers carriage work — a
`SealedBogTakenBody` item (Takeable, Weight 30 — a real burden;
sealing is the Sorter's work, not a player minigame in v1) delivered
to Marrowstye (12,12 — the Curation regional place). Wiring:
storylet quest (StartQuest from dialogue; objective = IfFact set by a
`QuestMarkerTriggerPart` at Marrowstye + IfHaveItem; delivery choice
on the Marrowstye filer-clerk TakeItems + GiveDrams + rep). The offer
dialogue quotes Clause the fifth verbatim (canon text, Concord
register where the Concord speaks). Refusing the contract aloud gets
the spoken-no treatment (the closure-ledger tutorializes later; the
verb exists now). Tests: full loop (offer → carry → deliver → paid),
refusal path, weight actually taxes (counter: an unburdened carrier
moves free), the clause text pinned to the canon string.

**W3.7 — Close-out.** Mid-phase review workflow ran at ~W3.3 (three
lenses over W3.1-3.3); close-out workflow over the rest; hypothesis
pass; ASCII look at Sumphold/Ledger/formations; §5 log filled per SM;
W3 exit = suite green + gates §9 checked + this doc's log complete.

## 4. Performance / observability

Per-turn additions: none (formations are gen-time; BurnOffGas is
damage-triggered; the snare rolls only on adjacency events). Diag:
`worldgen/FormationApplied` (Sodden biome), `gas/BurnOff` (ships),
quest diags (ship), `mineral-trade`-style records not needed — the
courier pays through quest actions. Every new gate that can reject
emits its reason.

## 5. Implementation log

### W3.1 — Six formations + the look pass (SHIPPED)

**Files:** Formation.cs (enum appends after BrineLens + SoddenPool
8-entry weighted + PoolFor case), SoddenFormationBuilder.cs (NEW —
six routines + shared EnsureAllReachable/FloodFromWest full-
reachability repair, remove-own-only), OverworldZoneManager.cs
(CreateSoddenPipeline appends the builder), Objects.json (MirePool /
Duckboard / DeadTree / PeatBank appended surgically),
ContainerPlacementService.cs (SoddenPool: WovenBasket 4, HollowLog 3,
Crate 2, Sack 1), BiomePalette.cs (Sodden case — tea-water filter),
SoddenFormationTests.cs (NEW), SpreadFormationTests re-baselined
(poolless exemplar Sodden→Grovelands), TerrainRenderCoverageTests
(+4 GlyphOnlyByDesign entries).

**Scope divergences from this plan:** SoddenPool container weights
shipped as HollowLog 3 / Sack 1 (plan said 2/2 — a hollow log is the
more Sodden of the two); DrownedCopse ships DeadTree 7% + permanent
water coating 23% (plan's "scatter over water coating" made
concrete). PeatCuts self-repair uses the W2 lesson directly: banks
are tracked and only own walls are removed on breach.

**R2 verified at the look pass:** dense permanent mire coatings
render fine (same projection as rivers); the ground line reads
bog-mire in a mire, as designed.

**Look pass (the gate) — run over all six via AsciiDumpTool,
one authored Sodden zone each (13.0.0 / 16.0.0 / 14.0.0 / 17.0.0 /
17.1.0 / 15.0.0, + 18.2.0 second Causeway):**
- PeatCuts, ReedMaze, Causeway, BogFace: signatures unmistakable
  first look. The causeway wobbles across 2-3 rows with mire lapping;
  a 2-cell sunk-board gap reads as age, and MirePool is walkable so
  it strands no one.
- 🟡 **OpenMire failed the first look** — 5-8 small blobs survived
  tree/rock rejection as ~2% of the zone: a forest with puddles, not
  a bog, in the double-weighted commonest formation. Fix: 9-12 blobs,
  radius 2-5, plus a 3% lone-pool scatter ("the flood never fully
  drained"). Second dump reads as bog with tussock paths. Same
  finding-class as W2.1's DuneBelt.
- ⚪ Honesty bound: DrownedCopse's signature is invisible in a
  monochrome dump (DeadTree is 'T' like living trees; TileState
  coatings aren't dumped). In-game color (&w dead vs green living +
  water sheen) carries it; the signature test pins the content.
- False alarm walked down: stray '=' in non-Causeway zones is
  HollowLog containers / CopperPipe ruin stamps, not Duckboard bleed
  (the cross-signature counter test also pins this).

**Tests:** 6810 → 6823 (+13). All green headless, twice (before and
after the OpenMire retune).

### W3.2 — The Bog-Taken (SHIPPED)

**FALSE PREMISE caught by the verification sweep:** this plan
prescribed "hand-placed ONLY in the Drowned Ledger's zone (authored
placement, W2.8 tenth-fire pattern)" — but the tenth-fire pattern
(zone-id check in the BIOME case) can never fire for the Ledger:
every authored Place is installed as a Village POI
(WorldGenerator.PlacePOIs), and POI zones route to
CreateVillagePipeline BEFORE the biome switch. The three bodies
live in the W2.6 profile seam instead (poi.Faction=="Palimpsest" &&
poi.Name=="the Drowned Ledger"), which is also where W3.5 grows the
rest of the place. A first-draft Sodden-case hook was written and
REVERTED when the sweep caught the routing.

**Files:** Objects.json (BogTakenBody &w + PreFellingBody &y — '&'
follows the shipped SaltCuredBody precedent, but NON-SOLID by plan:
a solid body beside a trench bank could seal a one-wide passage the
reachability repair can't see, since it removes only own walls),
SoddenFormationBuilder (SeatBogTaken: 0-2 per cut zone, on open
ground against a SURVIVING bank, after the repair; PeatCuts +
BogFace only), LandmarkBuilder (DrownedLedgerBodies stamp — three
'B', no walls, no marker: the peat is the ledger),
OverworldZoneManager (DrownedLedgerZoneID const documenting the
routing + the profile-seam branch), BogTakenTests.cs (NEW).

**The text gates (R3):** BogTakenBody describes state only ("It was
not buried. It fell, and the bog wrote it down"). PreFellingBody
yields fragments inside a quoted Palimpsest tag ("Recension XI ...
Reading incomplete") with the attribution explicit ("The attribution
is the expedition's. The body says nothing") — non-speech stated
without adjudicating consciousness; no "Naro", no "seventh"
(negative-asserted by test). The "mummy" lint is GAME-WIDE and
permanent: every blueprint's examine copy, whatever feature ships
it.

**Render coverage:** bodies are PhysicalObjects (layer 1), not
Terrain-tagged, so TerrainRenderCoverageTests does not demand
entries — noted here so the plan's "render-coverage entries" line
reads as checked, not skipped.

**Self-review note (§2.1 honesty):** tests and implementation were
written in one pass this SM (single headless cycle per run); the
content-existence tests are RED-by-construction absent the content,
but the per-test RED step was compressed. Counter-checks: unworked
formations surface 0 bodies; Sumphold and ambient Sodden zones grow
0 PreFellingBody; the banned-word lint passes over the whole
shipped corpus including SaltCuredBody.

### W3.3 — Methane peat (SHIPPED)

**SUBSTRATE FALSE PREMISE caught by the sweep:** this plan's test
sketch assumed "TakeDamage with Fire attribute → GasFactory spawn"
just works on a PeatBog. It could not: terrain has no Hitpoints
stat, CombatSystem.ApplyDamage deliberately early-outs on statless
targets, and the STRUCTURAL path burning terrain actually takes
(BurningEffect → DestructionSystem.RouteDamage → Damage) never
fired a TakeDamage event at all. BurnOffGasPart — whose own G.9
docstring promises "a peat bog venting methane when torched" — was
unreachable for every prop in the game.

**The fix, minimal:** RouteDamage's destructible branch now fires
TakeDamage with ApplyDamage's exact contract (pre-decrement,
listeners may mutate damage.Amount, clamped ≥0). Listener sweep
before the change: BurnOffGasPart (the intended hearer),
StatusEffectsPart (forwards to effects' OnTakeDamage — benign for
props), DamageFlashPart (props don't carry it). No behavior change
for anything that existed.

**Content:** marsh-gas.json (Seeping, BehaviorKind Poison — chosen
from what ships; methane in a low hollow chokes). PeatBog gains
Destructible HP 40 (peat is Fuel — enough fire CONSUMES it, venting
the whole way down) + BurnOffGas{marsh-gas, DamagePer 6, Heat;Fire}.
MirePool gains the same vent but Destructible HP 200 / Hardness 5 —
the burning tick erodes it by the 1-minimum, so the water
effectively never burns away but the trapped gas still comes up.

**🧪 deferral (per plan R-clause):** gas-cloud ignition (the
cloud-to-next-bog chain) needs a flammable gas behavior; none ships
(Poison/Stun/Confusion/Cryo/Sleep/FungalSpores/Plasma). The vent
alone ships; the chain is deferred with this note in
MarshGasTests' docstring too.

**Tests (MarshGasTests.cs, +7):** vent on fire (end-to-end through
RouteDamage, the real burning path), blunt-blow counter (damage
still lands, no gas), mire hardness pin, burn-the-bog-away
consumption, DeadTree-burns-in-silence wiring counter, registry
pins off the REAL shipped json file, BehaviorKind→GasPoisonPart
resolution. One compile fix (missing using CavesOfOoo.Data).

**Tests:** 6850 → 6857 (+7). All green headless.

### W3.4 — The Sodden's bestiary (SHIPPED)

**Verify-first results (the plan's three open questions):**
- *Bandfrog's on-being-hit reflect:* SHIPS. ScaldingVeilEffect's
  OnTakeDamage is the pattern; `CausticSkinPart` is the part-shaped
  sibling (anatomy, not ailment — permanent, and a panacea can't
  cure what an animal is made of). Contact means contact: adjacency
  (Chebyshev ≤ 1) gates the reflect, so melee qualifies and a spell
  from across the marsh does not; the rejecting gate emits
  `damage/SkinContactRejected` with its reason. **Born with the
  recursion guard the veil lacks:** reflected damage carries a
  marker attribute and skin never answers skin — two bandfrogs
  trading a blow must not ping-pong to mutual death. (ScaldingVeil's
  own two-veils exposure was flagged as a spin-off task, not fixed
  here — W2 shipped it; the fix pattern now exists to copy.)
- *Greatdew's adjacency-grab:* does NOT ship (IAuraProvider is
  visual-only; no adjacent-entry event exists). Per R4's own gate,
  v1 rides the step-trigger substrate: the plant is NON-SOLID and
  the grab fires when you walk into the dew itself
  (`GreatdewSnarePart : TriggerOnStepPart`, ConsumeOnTrigger=false —
  a plant is not a mine). Chance-gated grab → RootedEffect(4) +
  AcidicEffect; "stillness passes" is RootedEffect's ordinary
  expiry, zero new plumbing. Both branches emit diag
  (SnareGrabbed / SnareBrushed).
- *Reedfrog's harvest:* SHIPS via the Viper pattern (Corpse part
  HarvestBlueprint) → `FrogOil` (Commerce 8 — Sumphold lamps, a
  W3.6-adjacent trade good). The W5 slime-pool ecology note lives in
  the Reedfrog's examine copy as promised.

**Content:** Reedfrog (passive, flees, oil on legs), Bandfrog
(caustic skin), MawToad (Staying ambusher, AV 2, the copse's
resident), GinFrog (gate 7: NO Examinable part at all — "You see a
gin frog." is the entire record, pinned by test so any future
explanatory text is a failure), Greatdew, FrogOil.
SoddenTier1/2/3 + SoddenLairGuards replace the Jungle alias
(Grovelands still borrows — counter-pinned). DrownedCopse now
guarantees 1-2 MawToads (the den), on top of whatever the tables
roll. No human trouble in the wilderness tables by design: the
road crews are Sumphold's people, and Sumphold is a POI.

**Tests (SoddenBestiaryTests.cs, +13):** table names per tier +
lair guards; Grovelands-still-borrows counter; fail-loud
every-entry-is-a-real-blueprint gate (the "Waterskin" lesson);
no-jungle-legacy sweep; gate-7 pin; contact reflect + not-adjacent
counter + environmental-null counter + two-bandfrogs recursion pin
+ poison chance boundaries (0/100); dew grab at 100 / brush at 0 /
stillness-passes expiry walk-away; copse always houses 1-2 toads;
reedfrog passive + harvest pins.

### W3.5 — Place profiles: Sumphold + the Drowned Ledger (SHIPPED)

**The three-body contract survives the architecture:** the plan's
"a ReadingTable with a body ON it" would have made a FOURTH body
next to W3.2's scatter stamp — and BogTakenTests pins the Ledger at
EXACTLY three. Resolution: one combined `ExcavationCamp` stamp
replaces `DrownedLedgerBodies` (method deleted) — one body on the
reading-tent's table, two among the numbered stakes, three total.
The re-pin (`TheStampSwap_KeptTheCountAtExactlyThree`) makes any
future camp edit that changes the count fail loudly.

**Sumphold** (R1 honored: name-keyed profile branch, second of the
two allowed before promotion to a Place.Profile field): boatyard
stamp — BoatFrames, two PeatCutter spawns, and `TollRolls` quoting
the REGISTER of Codex/10 ("short count — forgiven — my error")
with a test negative-asserting the reading's confession ("not
kind", "her face") never leaks into a prop. The Bog-Taken trade
controversy lives in PeatCutter_1's dialogue — "Two answers in one
mouth" — and refuses to settle, which is the canon position.

**The Ledger:** ExcavationCamp — reading tent (interior-marked),
one ReadingTable, SurveyStakes, RecensionScribe + CurationSorter in
joint presence, each speaking their Order's card: "Attribution
before assertion" (Recension) and "Status: continuing" (Curation —
the file does not use the other word; the sorter's line mentions
the player's word only to decline it, which is the card).

**Voice notes:** the sorter's "we are still deciding the tense" is
about the FILES, not an assertion of inner life (R3 holds). All new
examine copy passes the game-wide mummy lint by construction.

**Tests (SoddenProfileTests.cs, +7):** boatyard present at Sumphold
+ Tine counter (name-key gating); register-not-reading negative
pins; camp census; exact-three re-pin; the Orders' cards asserted
in the loaded dialogue; fail-loud NPC↔conversation wiring gate.

**⚪ accepted:** stamp placement is generic, so the boatyard is not
guaranteed river-adjacent — the yard is BY the water in fiction,
somewhere-in-town in generation. Cosmetic; noted for a future
water-aware placement pass if it ever grates.

### W3.6 — The body-courier contract (SHIPPED)

**The loop:** the Curation Sorter offers carriage (gated
IfQuestNotStarted); acceptance = StartQuest BogBodyCourier +
GiveItem SealedBogTakenBody (Weight 30). The filer-clerk at
Marrowstye's intake window (CurationIntake stamp, PaleCuration
faction-keyed branch) carries the delivery choice, gated
IfQuestActive + IfHaveItem, running TakeItem + SetFact + GiveDrams
25 + rep +10 + CompleteQuest. The offer quotes **Clause the fifth
verbatim** (Codex/06:31), pinned to the canon string so a
paraphrase is a test failure — and the sorter's framing explains
why a PERSON carries it: the clause made the Concord careful of
the whole category, sealed or not.

**SCOPE DIVERGENCES (both recorded, both pinned):**
1. *No QuestMarkerTriggerPart.* The plan sketched the objective fact
   being set by a marker at Marrowstye; shipped, the CLERK sets it —
   the clerk IS the destination, and a marker would complete the
   objective on arrival while the body is still on your back. The
   cross-file fact-string lint pins clerk↔quest agreement.
2. *"Repeatable" is v1-deferred.* StartQuest refuses completed ids
   (the QS.3 registry guard), so the substrate is one-shot per
   quest id. The offer hides after completion
   (TheOffer_DoesNotRepeat pins it as the shipped contract).
   True repeatability needs a repeatable-quest substrate decision —
   future work, not smuggled in here.

**The burden is real:** Weight 30 against a Strength-1 courier's 15
capacity = overburdened = CANNOT MOVE (InventoryPart's BeforeMove
block); drop it and the legs work again. Pinned both ways.

**The refusal:** "No — and I say it aloud" gets the spoken-no
treatment in the Sorter's register ("a no said aloud files clean"),
and starts nothing — pinned.

**Tests (BodyCourierTests.cs +7, SoddenProfileTests +1):** clause
verbatim pin; cross-file fact lint; full loop through the real
ConversationManager (offer → carry → deliver → paid → rep → closed);
refusal; deliver-needs-both-paper-and-parcel (two hidden-choice
counters); no-repeat pin; the burden; Marrowstye window census.

### W3.7 — Close-out (COMPLETE)

**Review shape (divergence from this plan's own sketch):** the plan
called for review WORKFLOWS at ~W3.3 and at close-out; the monthly
spend limit killed 15 of 21 verify agents in the W2 close-out
workflow, so W3's reviews ran INLINE instead — per-SM severity-marked
self-reviews in every commit, a focused cold-eye after W3.4 (Q1-Q4
over the four diffs, 0 🟡+), and this hypothesis-driven deep audit.

**Hypothesis audit (12 player-flow hypotheses; classification per
CLAUDE.md):**
- 🟡 H1 CONFIRMED (RED→GREEN): the sealed body was sellable to any
  merchant mid-contract — item gone, quest active forever, offer
  hidden forever: softlock by shop. Fixed with the substrate's own
  NoTrade tag; pinned at the CanBeTraded seam.
- 🟡 H4 CONFIRMED (RED→GREEN): a burning bandfrog's tick damage
  carries Source = the arsonist; standing adjacent, every tick read
  as "contact" and seared them. Fire is not touch: elemental damage
  (Fire/Heat/Cold/Acid/Electric/Poison) never triggers the skin.
  Direction note: a future flaming WEAPON'S mixed damage will also
  skip the reflect — element wins over touch, accepted and recorded.
- ✅ H12 PINNED-AS-CORRECT: a reflect that kills the attacker
  mid-dispatch neither throws nor poisons the corpse (the guards
  were already right; now they are regression infrastructure).
- Walked and closed without tests: H2 (dropped body persists — RPG
  zone persistence, recoverable), H3 (a slip that slides you into
  the dew gets grabbed — correct, the bog is like that), H5/H11
  (population-placement cosmetics), H6 (killing the Sorter doesn't
  strand the contract — the clerk holds completion), H8-H10
  (covered by substrate suites or vacuous).
- ⚪ H7 recorded: a burned-away PeatBog leaves its permanent
  water coating on the tile (TileState outlives the entity).
  Cosmetic; the ground remembers the bog, which is almost right
  anyway.

**Design-gate sweep (design doc §9, W3-relevant):** gates 1 (Naro —
negative-asserted), 7 (Gin Frogs — pinned), 9 (consciousness never
resolved — all texts state state; "continuing" is a filing stance),
11 (mummy — game-wide lint) ✅. **Gate 12 caught a live violation:**
Reedfrog's examine copy said "In W5 terms: ... an ecology, not a
backdrop" — design register in the player's face. Text fixed, and a
NEW game-wide lint (phase tokens \bW\d\b, §, "design doc" in any
examine copy) makes the class unshippable.

**Look passes:** all six formations (W3.1, incl. the OpenMire
retune), Sumphold + the Ledger (W3.5). **PlayMode sanity sweep:
DEFERRED with the same honesty bound as W2** — the GUI editor's
wedge cycle makes live runs unreliable; everything asserted this
phase is EditMode-observable. Unverified live: gas-cloud rendering
over mire, camp color reads, the courier walk at real pace.

**W3 exit:** suite green (6891/6891), this doc's log complete per
SM, §9 gates checked ✅.

### W3 re-review (post-exit, user-directed) — the workflow round

A three-lens adversarial workflow (taxonomy / canon-fidelity /
cross-system integration; 3 finders → dedup → 1 skeptic per finding;
20 agents, 0 refusals-by-budget this time) re-read the whole W3
range. **14 findings confirmed, 3 refuted.** All fixed in the
re-review commit; RED confirmed before every behavioral fix.

**🔴 critical:** GiveItem ignored AddObject's refusal — the shipped
player's Inventory MaxWeight=150 meant accepting the carriage with a
near-full pack VANISHED the sealed body while "You receive..."
printed: quest active forever, offer gated off forever. A refused
handout now falls at the listener's feet (and the no-ground fallback
refuses honestly instead of lying about receipt).

**🟡 notable (5):**
- OpenMire double-booked ~40 cells/zone with stacked MirePools (the
  walkability guard can't see non-solid occupants) — double gas,
  double burn budget. New BuilderSpawn.TryPlaceOnce guards OpenMire
  AND the Beating's BrineLens (same latent pattern, proven at seed 8).
- Retaliation (CausticSkin/ScaldingVeil) can kill the ATTACKER
  mid-swing via the pre-decrement TakeDamage dispatch — pre-fix the
  corpse's hit line printed after its death line, on-hit dispatchers
  fired from the corpse, and the dual-wield loop (defender-HP-only
  check) let the zone-removed corpse swing its off-hand. Two guards:
  the swing loop breaks on attacker death; PerformSingleAttack
  returns after ApplyDamage if the attacker died. The exposure
  predates W3 (ScaldingVeil) but W3.4 made it routine.
- The elemental-not-contact gate was six exact strings while the
  damage model collapses Lightning/Shock/Ice/Freeze/Laser aliases to
  flags — ElectrifiedEffect ticks (Lightning-only) would have
  reopened the arsonist bug the moment their source-threading lands.
  The gate now reads the flag helpers (+ Light/Disintegration:
  element wins over touch, consistently).
- CurationSorter shipped Faction=Palimpsest — the Curation officer
  was mechanically a Recension member: her own contract's rep never
  reached her. Now PaleCuration; a two-Orders pin keeps the joint
  presence honest.
- Pyroclasm iterated the LIVE cell list while RouteDamage (W3.3's
  TakeDamage emission made it side-effectful on scenery) destroyed
  entities inside it — whoever stood on the wreckage was skipped.
  Snapshots targets first (the FlamingHands pattern).

**🔵 nits (7), all addressed:** the elemental gate now emits its
rejection reason like its adjacency sibling; TollRolls' closing
line now uses canon's OWN negative ("The rolls do not record a
face" — the rolls DO name the keeper; the crosser is the anonymous
one) with a pin; Greatdew's grab message no longer teaches
struggle-worsens (v1 has no such mechanic — the Apatheia lesson
ships when the mechanic does; examine copy reworked to
non-mechanical menace); Duckboard gains Destructible HP 8 (it
ignited but its burn ticks fell into the exact silent void
RouteDamage's doc-comment names — the safe line burns like its
neighbors, and walkable mire means a lost board never breaks
reachability); the SeatBogTaken docstring's "centuries-deep
cemetery" quote re-attributed to design prose (it is not in the
cited lore file — canon's line is "bog-bodies become visible at
depth"); ReedMaze's "sightline maze" comment made honest (FOV is
wall-keyed; no concealment hook ships) and the reed-concealment
mechanic recorded as future work HERE, beside the boat-lanes cut;
**recorded divergence (missed by rule 7 at W3.5):** TollRolls was
planned "on the bridge approach" and shipped inside the boatyard
stamp — no bridge exists in generated Sumphold; same
generic-stamp-placement limitation as the river-adjacency ⚪, and
the rolls read as the surviving ARCHIVE of the old bridge-toll per
Codex/10's margin note.

**Identity fix the lenses missed, caught inline:** the Sodden's
AMBIENT stamp catalog aliased Jungle wholesale — vine ziggurats,
Choir grove shrines, and jungle hermits rolled in the peat. The
Sodden now has its own array (hermit, mendleaf patch, hunter's
blind), exactly the fix the Spread's catalog docstring describes for
the same drift; pinned by test. (W4 note: Grovelands still aliases
Jungle — RIGHT for GroveShrine, wrong for Ziggurat; W4.1 owns it.)

**Refuted (3, recorded so they stay refuted):** the "Concord won't
touch the bodies" premise-vs-canon contradiction (conflates the
carriage clause with the purchase trade); the "silent stamp
placement could strand the three PreFellingBodies" (guaranteed
placement + the exact-three pin both hold); the "MirePool vents six
times its structural damage" disproportion (accumulator semantics
are Qud-parity and deliberate). The Sodden is a place now: it has a shape, its
dead, its gas, its beasts, its two towns, and one piece of honest
work for a strong back.

## 6. Critical review of this plan (before implementation)

**R1 — Sumphold-on-Spread is a feature, not a bug** — but the profile
key must be the NAME (faction Villagers is shared by five places).
Keying profiles by name is a mild smell (string-coupled content);
accepted for two places (Sumphold, First Tent already does it inside
the TentRight branch) — if a third name-keyed place appears in W5+,
promote to a Place.Profile field on the authored map then.

**R2 — MirePool + the slip system:** bog-mire is authored NOT
slippery (correct — mire sucks, it doesn't slide) but IS a LiquidPool
→ it projects a permanent coating → the ground line reads "bog-mire"
everywhere in a mire. Verify the look/readout copes with dense
permanent coatings (it does for rivers — same projection). No change
expected; verify at W3.1.

**R3 — The bodies are readables under the STRICTEST gates in the
game** (voice cards + "mummy" ban + no adjudication + Naro). The
text-lint test (banned word, game-wide) turns one gate into
infrastructure. The consciousness line: every body text describes
STATE, never inner life. Reviewed at W3.2 commit, and the close-out
lens re-checks it.

**R4 — Greatdew's full design (immobilise-4, struggle worsens,
stillness passes) is a combat-loop mechanic, not a prop.** V1 ships
the snare (grab + hold + acid; not attacking lets it lapse) ONLY if
the adjacency substrate carries it without new event plumbing —
verify-first at W3.4, defer whole otherwise. The Apatheia teaching
reading survives either way ("struggling makes it worse" is the v1
mechanic's exact shape).

**R5 — The courier body is an inventory item, not a drag prop.**
SaltCuredBody's drag-only pattern is more evocative but a cross-map
drag is punishing beyond fun and blocks zone transit (verify: can
dragged props cross zones at all?). Weight 30 in inventory is the
honest tax. If drag-across-zones ships someday, a premium drag
contract can join.

**R6 — Methane's "chain of soft detonations" may exceed shipped gas
behaviors.** The vent alone (fire → gas) is guaranteed; the chain
(gas ignition propagating) depends on what GasPlasmaPart-class
behaviors do. Plan ships the vent, tests the chain IF supported,
defers it in writing otherwise — no silent scope loss.

**R7 — No new status-effect special paths** (the standing scope
guard): the snare uses RootedEffect + the acid pipeline; the mire
uses the liquid system; bodies are Examinables. Anything demanding a
bespoke path is a stop-and-think, not a workaround.

**R8 — Three research agents ≠ ground truth.** Every load-bearing
claim they made that a sub-milestone builds on gets re-verified at
that SM's verify-first step (the W2 arc caught one such error —
"Beating aliases DesertTier3 at every tier" — this plan's numbers get
the same skepticism).

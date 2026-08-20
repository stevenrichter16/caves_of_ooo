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

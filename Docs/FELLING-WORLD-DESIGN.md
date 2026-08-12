# The World of the Felling — canon-grounded world & biome overhaul

> **Status: design document, no code.** Written 2026-08-11 against the
> canonical lore corpus (`Lore/` — Bible + Second Spine authority chain)
> and a full engine inventory (worldgen, tile systems, rendering, game
> systems — thirteen parallel deep-reads, ~1.85M tokens of source).
>
> **The brief:** overhaul the entire game world to fit the canon. Keep
> every mechanic. Design the actual chunks the player walks through —
> what they look like on an 80×25 CP437 grid, who lives in them, and how
> they plug into the systems that already ship.
>
> **Authority obeyed:** `Lore/10_Bible.md` §IV (amended by
> `11_SecondSpine.md`) on bare fact; `MYSTERY-LEDGER.md` overrides in the
> other direction; `TERMS.md` for vocabulary; `catacomb_village_design.md`
> canonical for villager culture; voice cards for any in-world text.
> `IDEAS.md` used only where canon adopted it.

---

## 0. The world in one breath (what everything below serves)

A god-tree that held the world together commanded six mortals to fell it.
The Strikes ascended them into gods — Memory, Substrate, Preservation,
Beauty, Exchange, Roots — but the seventh chosen, Naro, refused, so
**Naming** has no bearer. For ~1,080 years the world has limped on,
bound but missing its cornerstone. The empty seventh position tries and
fails to incarnate — **Urqu, pressure, never agent** — and the failures
un-name things: the *sari… sari…*. Now the **Thinning** has come: the
Root (the Tree's sleeping taproot, under the petrified stump that is the
central tepui) is tiring, and an ordinary person from a river village
must decide how the stalemate breaks: **Consume, Preserve, or Renewal.**

Three design facts follow, and every biome below is built from them:

1. **The geography is the Tree's fossil.** The stump is the central
   tepui; lesser tepuis are fossilised root-buttresses; sinkholes opened
   where roots were densest; rivers run along veins-that-were; the flood
   made the bog; the wasteland is where the canopy was thinnest and the
   raw sun hit first; the catacombs are where leaf-light survived as
   fungal bio-light (`Lore/History/01_Spine.md:166-171`,
   `03_History.md:58-61`).
2. **The world is a hand-authored network of places, not a geometry.**
   ~22 named places, each with an authored **Strangeness Tier (1–5)**
   that the player never sees as a number but always feels — bio-light
   brighter, NPCs stranger, air wrong, *sari* louder
   (`02_Geography.md:37-47,158-165`). Tally is the traversal hub; the
   Root is the narrative centre; **they are deliberately different
   places**.
3. **Stuff is theology.** Every faction's material culture is its god's
   function in objects — you read a room's allegiance off its walls
   before anyone speaks (`08_MaterialCulture.md` §I). So biome design IS
   faction design IS narrative design; a chunk's tile composition is
   dialogue.

And one tonal law: **the flowers matter because the Thinning is coming
for them.** Everyday magic — flower-charms, glowing porridge, festival
meadows — is the same binding-force as the cosmology, and the horror
lands only if the whimsy is real first (`Bible §VIII`,
`09_Magic.md` §II).

---

## 1. What the engine already gives us (kept, all of it)

The inventory found the engine dramatically ahead of the world. Nothing
below requires new architecture; the load-bearing systems all ship:

| Canon concept | Shipped mechanic it lands on |
|---|---|
| Places / travel network | 20×20 WorldMap + POI system + zone pipelines (`OverworldZoneManager`) |
| Vertical world (sinkhole → catacomb → rootway) | `Overworld.X.Y.Z` z-levels, stairs connection registry, depth pipelines |
| Ground statuses (wet, oil, ice, embers, charge) | `ZoneTileState` + `TileReactionSystem` (JSON rules) + `TileStateSourcePart` terrain |
| The 26 liquids incl. lore liquids (memory-bath, iron-gall-ink, tepuibone-slurry…) | `LiquidRegistry` — pure JSON, 16 wired mechanic fields |
| Gases (spores, sleep-vapor, methane burn-off) | Gas entity system, 7 shipped definitions, `BurnOffGasPart` |
| Bio-light / darkness | `LightMap` (ambient floor + `LightSourcePart` + LOS), per-zone `AmbientTint` |
| Faction presence & reputation | Factions.json + rep tiers + trade-price modifiers + dialogue gates |
| Readable records, rumor tiers | `KnowledgePart` (built, used on exactly ONE NPC — dormant) |
| World-reactivity / escalation events | Non-quest storylets (engine complete, ZERO shipped — dormant) |
| Stones with Stake | Mineral veins + `WantsMineralPart` (complete, 0 blueprints use it — dormant) |
| Preservation liquids | honey/resin/salt coatings already ship as liquids with real mechanics |
| Structure vignettes | `StampCatalog` ASCII stamps (21 shipped, cheap to add) |
| Sanctuary | `SanctuaryPart` (exists; currently a no-heal shrine) |
| Ink as a second currency | Rental/Ink economy (one lessor — dormant) |
| Locked vaults & keys | `LockPart`/`KeyPart` (barely exercised — dormant) |

The overhaul's centre of gravity is therefore **authoring, re-mapping,
and activation** — not systems programming. The genuinely new mechanics
(§7) are eight small pieces, each landing on an existing substrate.

---

## 2. The world map — authored, not rolled

### 2.1 Kill the noise

`WorldGenerator.cs:36-50` currently quartiles one noise field into
Cave/Desert/Jungle/Ruins. Canon forbids exactly this: the world is "a
hand-authored network of ~30 places, not a global geometry"
(`02_Geography.md:158`). **Replace the noise pass with an authored 20×20
biome table, an authored tier table, an authored road/river overlay, and
authored POI placements.** The grid stays; the lottery goes. Walking
between places crosses wilderness chunks of the intervening biome — the
canon's "the journey is content" for free.

Seeds still vary *chunk interiors* (formations, spawns, stamps roll per
zone from `WorldSeed ^ zoneID.GetHashCode()` as today). The *map* is
fixed, like the lore's places are fixed.

### 2.2 The map

Six surface biomes (§3), 20×20, x→east, y→south. Sill keeps
`Overworld.10.10.0` (the engine hardcodes the start in four places;
canon starts the player at Sill; the current starting village *becomes*
Sill).

```
    0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19
 0  G G G G G G G G S S S  S  S  D  D  D  D  D  D  D
 1  G G T T T G G S S S S  S  S  D  D  D  ⌂  D  D  D
 2  G T T T T T G S S S S  S  D  D  D  D  D  D  D  D
 3  G T T ♣ T T G G S S S  S  ○  D  D  D  D  D  D  D
 4  G T T T T ○ G G S S S  S  S  D  D  D  ○  D  D  D
 5  G G T ✕ T T G G S S S  S  S  S  D  D  D  ⌂  D  D
 6  G G G G ○ G ⛏ S S S S  S  S  S  S  ⌂  D  D  D  D
 7  G G G G G G S S S S S  S  S  ⌂  S  D  D  D  D  D
 8  G G G G G S S ⌂ S S S  S  S  S  S  D  D  D  D  D
 9  O O O G S ❀ S S S ~ S  S  S  S  ⌂  S  D  D  D  D
10  O O O O S S S ~ ~ ~ ☖  ~  ~  ~  ⌂  S  S  D  D  D
11  O O ✝ O O S S S S S S  S  S  S  S  S  ⌂  S  S  S
12  O O O O O S S S S S S  S  ⌂  S  S  S  S  S  S  B
13  O O O O S S S S S S S  S  S  S  S  B  B  B  B  B
14  B B O O S S S S S S ⌂  S  S  B  B  B  B  B  B  B
15  B B B B B S S S S S S  S  B  B  B  ⌂  B  B  B  B
16  B B B B B B B S ⌂ B B  B  ≡  ≡  ≡  B  B  B  B  B
17  B B B B B ⌂ B B B B B  B  ≡  ≡  ≡  B  B  B  B  B
18  B B B B B B B B B B B  B  ≡  ≡  B  B  B  B  ⌂  B
19  B B B B B B B B B B B  B  B  B  B  B  B  B  B  B
```

Legend — biomes: `S` the Spread · `D` the Sodden · `B` the Beating ·
`G` the Grovelands · `O` the Overwrit · `T` the Stump (root-slope) ·
`≡` Great Salt Plain (Beating sub-region) · `~` the river-vein.

Named places (POIs): `☖` **Sill** (10,10 — start) · `⌂` towns —
**Gantry** (7,8), **Tine** (13,7 lake), **Cinderhold** (6,6 `⛏`),
**Posy** (5,9 `❀`), **Marrowstye** (12,12), **Quillhold** (14,9…10),
**Tally** (10,14), **Wellmeet** (8,16), **First Tent** (5,17),
**Salt-Vault** (15,15 — lesser tepui), **Sumphold** (15,6),
**Drowned Ledger** (17,4…5), **Slip** (16,11), **The Quiet's Door**
(16,1), **The Last Counter** (18,18) · `○` sinkhole mouths —
**Olderdeep** (4,6), **the Deepest Cathedral** (5,4), **Lampwell**
(12,3), **Spivenor** (16,4) · `♣` **the Root of the World** (3,3) ·
`✕` **the Felling-Site** (3,5) · `✝` **the Unsaying** (2,11).

Unmarked, deliberately: the **first Sealed Library** (its location was
un-remembered — it must never appear on the map UI until found), the
**tenth fire** (deep Beating; a fire, no explanation, ever —
Mystery Ledger §4), the two **Door sinkholes**, and every bleed-site.

**The Overwrit is a hole in the map.** The world-map zone renders those
cells near-blank and the road overlay visibly bends around them — "the
way handwriting bends around a hole in the vellum"
(`THE-OVERWRIT.md:22`). The region reads large by map-gap, not zone
count.

### 2.3 The tier table

An authored 20×20 int (1–5), replacing the Manhattan-distance tier.
Broad strokes: the Spread 1; the Sodden and Beating 2 (3 deep); the
Grovelands 3 (4 near the Stump); the Overwrit 4; the Stump 4, the Root
and Felling-Site 5; sinkhole floors = surface tier +1. Tier drives
population tables, hazard density, ambient corruption, *sari* frequency
and bleed eligibility (§7.2). Canon rule kept: **tier is authored, not
radial** — Slip is a Tier-3 wound a day's walk from Tier-1 farmland.

### 2.4 Roads and the river

An authored per-cell overlay (`Road`, `River` flags). Zone pipelines
consume it: a road cell's zone gets a road formation (packed-earth lane,
waymarkers — some scraped smooth, which is underreading content); a
river cell's zone gets `RiverChunkBuilder` (ships). The road network
radiates from **Tally** (canon: the busiest node), NOT from the Root.
Concord waystations stamp along roads; the caravan edges of the lore's
travel graph are literally these lanes.

---

## 3. The six surface biomes — chunk design in full

Per biome: identity → what a chunk looks like at ground level →
formations (the topology variants that kill chunk-sameness) → render
spec → ground statuses & hazards → flora/fauna → structures → faction
presence → sound → mechanics hooks → engine work.

The formation axis is the anti-sameness machine: each biome rolls one of
4–7 formations per chunk (deterministic per zone ID), so two adjacent
chunks of the same biome are different *kinds* of place. Every formation
obeys two rules: **nameable in four words from the map edge**, and **it
changes a decision, not a texture**.

---

### 3.1 THE SPREAD — recovered river country (Tier 1)

**Identity.** The material frame of disbelief. Farms, hedgerows, river
meadows, mills, folk-god shrines. Its stuff expresses *no* god — that
ordinariness IS the design (`08_MaterialCulture.md` §VII: "the material
frame of disbelief"). The Tree is a children's story carved on lintels
no one reads. This is where the player learns the world is ordinary, so
that watching it stop being ordinary means something.

**Ground level.** Grass and worked earth. A hedgerow with a stile. A
water-meadow where geese complain. An old lintel with worn carving. A
shrine — bread, salt, a small vessel left "for whichever force is
listening this season." In festival season, a **conjured flower-meadow**
blooming in a fallow field, gone by morning.

**Formations** (roll 1 per chunk):

| Formation | Shape | The decision it changes |
|---|---|---|
| **River-meadow** | `RiverChunkBuilder` water + banks + fords | crossing points channel movement; Riverwarden lurks at deep fords |
| **Field-and-hedge** | parallel hedgerow lines (`#` green, gaps every 6–10) | short sightlines in open country; hedges burn (Combustibility high) |
| **Coppice** | sparse tree CA (open woods) | cover both ways; forage |
| **The old road** | packed lane E–W or N–S + waymarkers, verges | fast lane, watched lane; scraped waymarker = first undertext |
| **Fallow common** | open sheet + flower-charm patches + a shrine | nothing between you and what sees you; the flowers read Urqu-bleed (below) |
| **Mill-stead** | one big stamp (mill/farmhouse + yard + orchard) | a lived-in place; owners; doors |

**Render.** Floor `.`/`"` grass with `GlyphVariants`; warm palette,
tint (1.0, 1.0, 0.94). Hedges dark-green `#`. Water via the shipped
river animation stack (FlowsEast tags). Ambient motes: Leaf, Bee.
Sprite-mode: existing grass/water macro sets serve as-is.

**Ground statuses.** Mild: water coatings at fords; **FlowerField** — a
new charm-terrain (`TileStateSourcePart` variant, `LifespanPart` ~200
turns): coats its cell with a cosmetic petal residue, faint
`LightSourcePart` at dusk. **The mechanic:** flower-charm terrain rolls
its lifespan against the zone's Urqu-bleed — in a bleeding zone the
meadow wilts visibly early. Grandmothers say the flowers "aren't taking"
— the player's first, deniable Thinning instrument. (The Bible's whole
tonal thesis, made walkable.)

**Flora/fauna.** Geese; Reedfrogs (tadpoles keep slime-pools clean —
co-occurrence rule from the bestiary); Bandfrogs by water; Sun-Striker
lizards in clearings (ecosystem-health indicator: they vanish where
corruption arrives); snapjaw-tier threats only at the biome's edges;
Riverwarden (dwarf caiman: lunge-grapple-drag) in deep water.

**Structures.** Mills, drying racks, river-shrines, the Concord
trade-post (every second village), waystones. Reuse HermitHut,
MendleafGarden stamps. New stamps: RiverShrine, MillStead, FestivalField.

**Faction presence.** Villagers (mainline); Concord Factor; one
itinerant Recension scribe *passing through* (seasonal — a storylet, not
a resident); a Curation filer-clerk at Gantry doing mundane records.
Faction politics as paperwork.

**Sound.** River, geese, market noise. ***sari… sari…* NEVER sounds
here** — until the scripted inciting event at Sill (a storylet: the
elder goes pale; the myth becomes true; the game begins).

**Mechanics hooks.** Everyday-charm items and services (festival
contracts, glowing porridge for a sick child — small quests with real
warmth); the flower-bleed instrument; the closure-ledger tutorialises
here (decline a quest to the giver's face and the elder *thanks you for
saying so* — teaching that a spoken no is a closure, `11_SecondSpine.md`
C4).

**Engine work.** Reframe the existing starting village as **Sill**
(rename, drop the Adventure-Time legacy quest pair per
`Docs/LEGACY-CONTENT.md` quarantine); retire the Elemental Crossroads
pins; new formations = parameterised reuse of existing builders +
2 new small ones (hedgerows, lanes).

---

### 3.2 THE SODDEN — the flood's country (Tier 2–3)

**Identity.** Where the Tree's blood-flood never fully drained
(`03_History.md:58`). Peat, black standing water, drowned coppices —
and the **Bog-Taken**: the drowned of the cataclysm, still emerging
from peat a thousand years later. *Walking a bog is walking a
centuries-deep cemetery whose contents are visible.* The Recension's
oldest excavation (the Drowned Ledger) reads the world's only intact
pre-Felling witnesses here.

**Ground level.** Tea-dark water between tussocks. A cut peat-bank with
clean tool marks — and a leathery brown forearm in the face of the cut,
tagged with a Recension marker. Duckboards, some rotten. Reeds taller
than you. A boat tied to a dead tree. Bubbles rising where nothing
should breathe.

**Formations:**

| Formation | Shape | The decision |
|---|---|---|
| **Open mire** | Archipelago — tussocks over deep bog | route-picking cell by cell; missteps sink (Sticky + damage over turns) |
| **Peat-cuts** | Comb — harvested trenches, water-filled, worked banks | cover trenches; Bog-Taken exposed in faces; peat-cutter NPCs |
| **Reed maze** | Braid — channels through tall reeds | 1-cell sightlines; ambush country; boat lanes |
| **Drowned copse** | dead trees standing in shallow water | open but obstructed; Maw-Toads den here |
| **The causeway** | one duckboard line across everything | THE safe line — and everyone knows it, including what hunts |
| **Bog-face** | Edge — a tall cut showing strata | readable stratigraphy (Recension content); bodies at every depth |

**Render.** Black-brown water `~` (dark cyan on near-black), sedge `"`
olive, tussocks `.` ochre, dead trees grey `T`. Tint (0.82, 0.86, 0.80).
Fog-heavy feel via slightly lifted `UnexploredColor`. Motes: Drip.
Sprite-mode: water shoreline set reused with a peat recolour.

**Ground statuses & hazards.** The tile layer works hard here: `PeatBog`
terrain (ships) asserts water; **bog-mire** liquid (ships: dampen + acid
+ Agi−2) in mire cells; **methane** — `BurnOffGasPart` on peat cells
vents flammable gas when burned, so fire on the bog is a chain of soft
detonations (all shipped mechanics, finally sited); deep-bog cells =
Sticky + sink. **The Bog-Taken as terrain-objects:** interactable
preserved bodies (peat-brown `&`), each a readable — a body IS a record
here. Some are ordinary drowned; three are pre-Felling (hand-placed,
Drowned Ledger only, consciousness ambiguous — **never resolved**,
Mystery Ledger discipline).

**Flora/fauna.** Gin Frogs (brood-armor — knock the babies off; in the
bestiary AND the Mystery Ledger: never explain them); Bandfrogs;
Maw-Toads (eat lantern-beetle colonies — an economic predator);
Reedfrogs; leeches; the Riverwarden; **Greatdew** (3-cell carnivorous
sundew: immobilises 4 turns + enzyme damage — stillness passes safely,
struggle worsens it — the Apatheia teaching-plant).

**Structures.** Sumphold (raised-ground town, boat-builders,
peat-cutters, casually-known Bog-Taken trade — "controversial" is
dialogue); the Drowned Ledger (Recension expedition camp: tents,
numbered stakes, a reading-tent with a body on a clean table); peat
stations; bog-walk shrines. New stamps: PeatStation, ExcavationCamp,
DuckboardRest.

**Faction presence.** Recension (the excavation — decades old, slow,
reverent); Concord (buys Bog-Taken bodies, controversially); Curation
(disapproves of the Concord trade; files the recovered).

**Sound.** Bubbles, bitterns, silence with texture. *sari* at the bog's
deep eastern edge only.

**Mechanics hooks.** Body-delivery contracts (bring the dead to
Marrowstye sealed to Curation intake standard — the corpse-handling
standard from `Codex/06` as a courier quest-class); peat-cutting as
harvest; bog-iron and Black-Gall nodules as mineral veins; boat travel
along river-vein edges (fast lanes with Choir-Touched water flavour).

**Engine work.** Mire/tussock formations = CA-parameterised +
archipelago mask (new small builder); Bog-Taken = blueprint family +
hand-placement pass; methane peat = existing `BurnOffGasPart` on the
PeatBog blueprint (one JSON param).

---

### 3.3 THE BEATING — the wasteland (Tier 2–3)

**Identity.** Where the canopy was thinnest, the raw sun hit first, and
the first generation's defining trauma was **light** (`03_History.md:59`).
Salt flats, exposed pre-Felling ruins (never buried — less flood-mud
here, `03_History.md:29`), wells as the only nodes that matter, and
**Tent-Right** — the only faction not god-founded, the people the Tree
did not name, whose three-day oath does Naming where the Root cannot
reach. **The design inversion is the whole biome:** the most hostile
environment in the game contains the mechanically safest interiors in
the game (`07_TentRight.md:55-56,123`). The delta between outside and
inside IS the faction.

**Ground level.** White glare. Heat shimmer on a polygon-cracked salt
crust. A pre-Felling street grid standing waist-high out of the pan,
door-frames opening onto nothing. A line of camels' worth of caravan
dust. A black goat-wool tent with a cloth on a pole — and stepping under
it, the temperature of the world changes.

**Formations:**

| Formation | Shape | The decision |
|---|---|---|
| **Salt pan** | Sheet — polygon crust, zero cover | exposure: heat ticks + total sightlines; a duel floor |
| **Ruin field** | `RuinsBuilder` reused — exposed pre-Felling grid | the old world, walkable; archaeology; shade |
| **Dune belt** | `DesertBuilder` reused, re-palette | soft going, occluded lines |
| **Caravan road** | well-to-well lane + waymarkers + bones | the road is life; leaving it is a decision |
| **Wind barrens** | rock + DryBrush scatter | brush burns; scorpion country |
| **Brine lens** | Archipelago — thin crust over deep brine | crust breaks; `BrinePool` (ships) doing structural work |
| **Hush-edge** | beyond the Last Counter: wrongness | continuous Urqu-bleed; no formations repeat; do not linger |

**Render.** Salt near-white `.`/`,` with polygon ridge `─│┼` accents
mapped via `Cp437.Map`; ruins bone-grey; brightest tint in the game
(1.05, 1.0, 0.9) — HDR `&*` accents for glare; heat shimmer via the
existing animated-glyph channel on `≈`-class cells. Motes: Glint.

**Ground statuses & hazards.** **Sun-glare**: in exposed Beating cells
during the Height hour-band (WorldClock, §7.1), creatures without
head-slot cover take heat: the shipped tile-layer Heat channel written
ambiently at low density + a `Parched` stacking status (small Str/Agi
malus, cured by water). Interior/shaded cells (`IsInterior`, tent
footprints) are exempt — shade is architecture. Salt ground: mycelium
cannot grow (no Choir tendrils spawn in Beating — canon), and
Bloom-infected creatures take salt damage (PaleSalt anti-fungal rule,
already in the material palette).

**Flora/fauna.** Sun-Strikers, Glass Scorpions (ship), BrittleHound
(ships), Sari-Snakes **only when Urqu is active** (state-reactive
spawning: the bestiary's whole indicator-species design), Sky-Sari
shadow events (an aerial stoop: a shadow crosses your tile two turns
before the hit), Saltbriar (forage), Wardline snakes near settlements
(they suppress Sari-Snake spawns — placeable pest control).

**Structures.** **Tent-Right camps** (stamp: tents + well + the
guest-cloth pole; §7.4 oath mechanics); **Wellmeet** (Tier-2 hub, oath
is law, salt-trade origin); **the First Tent** (pilgrimage monument —
"not a temple; there is no god — a monument to a choice"); **the Great
Salt Plain** (Pale-Salt mining region, `≡` cells); wells (well-keepers
hold real power; poisoning one is the second-worst crime); **the Last
Counter** (the Concord's final outpost: "We cannot guarantee delivery
beyond this point" — and two *abandoned prior outposts* further out,
because it has been pulled back twice — time-depth as level dressing);
**the tenth fire** (one campfire, deep pan, burning, untended, no
explanation, no examine text beyond what is seen — Mystery Ledger §4:
forbidden from answering, everything and everyone).

**Faction presence.** Tent-Right (mainline: clan-elders, well-keepers,
salt-masters, the Namers); Concord caravans under guest-rights;
Curation salt-buyers; Catchers *outside* oath-ground only.

**Sound.** Wind, crust creak, caravan bells. Nights are enormous.

**Mechanics hooks.** The **three-day oath** (§7.4) — claiming
hospitality at any tent = 3 days absolute protection: Catchers can't
follow, pursuit waits outside (scripted early scene: your pursuers
camped at the boundary, waiting politely), Urqu-bleed minimal under
cloth. The player can swear the oath — gaining the power to extend
sanctuary (pacify a hostile who claims the bond) and the obligation to
host at bad moments. Oathbreaking = the single worst reputation loss in
the game. Salt economy: mine → Tent-Right authority → Concord transport
→ Curation demand.

**Engine work.** DesertBuilder re-palette + salt-pan/ruin-field/brine
formations; `Parched` status + hour-band glare (small); oath status +
sanctuary AI honor (§7.4); state-reactive spawn hooks (§7.6).

---

### 3.4 THE GROVELANDS — Choir country (Tier 2–4)

**Identity.** Where the substrate surfaces at scale — and it is *kind*,
which is the horror. The Choir is the Wedded (Selen, god of Substrate,
**joy** — "the all-including delight is the horror"), distributed
through every tendril; the groves are her fingertips. Grove-density
rises toward the Stump (roots were densest there). Everything here is
grown, not made: the Choir has **no manufactured material culture** —
the absence of stuff is the fingerprint.

**Ground level.** Mycelial columns like cathedral pillars, pale ochre
and violet, bioluminescent veins pulsing slowly. A grown-wood sign at
the grove edge, letters raised in living grain, weathered smooth by
travellers' touching it for luck. A clean seep of drinkable water. Red
growths you have been told about. Somewhere, faint, singing. The ground
is soft. It would be so easy to rest.

**Formations:**

| Formation | Shape | The decision |
|---|---|---|
| **Grove** | Radial — columns ringing a seep, open floor | the grove rules apply (below); the centre is the reason you came |
| **Tendril fen** | Braid — tendrils tracing old water-veins | lanes; tendrils are interactable (37-node dialogue tree ships!) |
| **Fruiting wall** | Rubble — dense vertical growth | short lines, climbing spores; harvest |
| **Composting field** | half-eaten things in ordered rows | loot and horror in one pile; "everything here is somebody" |
| **Encasement colonnade** | columns with the dead visible inside | Tier-4: faces in the substrate; a Wall-Caught player wakes here |
| **Bloom-front** | ⚠ condition overlay, any formation | the Driving Bloom corruption pass (below) |

**The grove rules as mechanics** (straight from `Codex/11`, the
grove-edge sign — readable in-game):
- *The seep is free.* Clean water, genuinely, always.
- *Eat nothing red.* GroveRed forage: `vital:3, toxic:1` — the best
  healing reagent in the game AND it ruins every brew (the folk-warning
  as brew-math, already designed in `WORLD-INGREDIENTS.md`).
- *Do not dig.* Digging/mining in a grove = Choir rep loss, immediate.
- *Lie down only if you mean it.* Resting in a grove while badly hurt
  triggers the encasement offer — a conversation, gentle, refusable,
  once. ("We can tell the difference.")

**Render.** Loam near-black; growth pale ochre `♠`/`♣` recoloured,
violet accents; bio-glow = `LightSourcePart` on major columns (soft
radius 3, green-white) so groves *glow at night from across a chunk*.
Tint (0.88, 0.92, 0.85). Motes: Spore (ships).

**Ground statuses & hazards.** Substrate mat cells: movement cost +1
(soft ground); spore gas pockets (`fungal-spores` gas ships); the
`veined-pulse-mycelium` liquid (ships: Electric immunity) pools at grove
hearts — the Choir does not mind lightning. Choir-Iron carried openly =
rep loss (insult); fire in a grove = major rep loss (crime) — both are
one-line faction-feeling hooks on existing events.

**The Bloom-front overlay** (the Driving Bloom is **terrain, not a
faction** — no dialogue, no rep track, never funny): any Grovelands or
catacomb chunk can carry the front condition — fruiting bodies erupt
from the half-done (a Bloomed workshop: tools down mid-task, work
abandoned — *the unfinished* as set dressing); **driven hosts** (any
creature + `BloomedEffect`): compelled to climb high cells, move to
open positions, attack a direction, drop what they're doing —
implemented as an AI-goal override effect; eruption on death (spore gas
burst). The player can be infected; **the cure requires the Choir**
(early-stage encasement ritual) — the Bloom's chief narrative function
is to push you toward the Choir. Spore-Block rooms are 100% Bloom-safe;
Drosera rings eat the spores — the anti-Bloom material economy, sited.

**Flora/fauna.** Shamblers, Rotlings, Mosshulk (all ship);
ChoirTendril (ships, with dialogue); glow-moths; Wine-Leaf sundews
(colour = feeding state = danger read); the **doll in the wall** — in
ONE grove, a doll woven into the mycelium, uneaten. No examine text
explains it. (Mystery Ledger §5.)

**Structures.** Grove shrines (GroveShrine stamp ships); the grown-wood
signs; beetle-paths kept clear "for the ones who are still walking";
Spore-Wedded village mouths (sinkhole entries); the approach to the
Deepest Cathedral (Tier 4: non-Choir-aligned players are in danger; the
substrate sometimes forms her old face — a render-event set piece).

**Faction presence.** The Choir (which is a god wearing a landscape);
Spore-Wedded villagers; Concord tendril-pruning contracts (the canonical
first-act tradeoff: accept = Choir rep down, refuse = Concord rep down).

**Sound.** The singing (distant, chordal); *sari* audible at Tier 3+.

**Mechanics hooks.** Memory-consumption services at grove hearts (the
encasement — input-locked, witnessed, per canon); Wall-Catching (§7.5)
from Cathedral sinkholes; Choir reputation is act-based and *never
forgets* ("no faction remembers the player's history more reliably").

**Engine work.** JungleBuilder re-palette + grove/fen/colonnade
formations; grove-rule hooks (rest-trigger, dig-trigger, red-forage);
`BloomedEffect` + front overlay pass; the Cathedral set-piece zone.

---

### 3.5 THE OVERWRIT — the scraped region (Tier 4)

**Identity.** The Great Manifestation (Gen 25) was a season-long failed
birth of the seventh god, and it **scraped** a region — not destroyed:
scraped, the way a page is scraped for rewriting. What is left is what a
scraped page is: *not empty. Faint.* (`THE-OVERWRIT.md`.) Nobody
resettled it. The horror is specified in negatives: ground too smooth,
growth too sparse and too new, **no ruins where ruins should be** — a
region this old should be full; it has been *prepared*.

**Ground level.** Flat ground with no stones in it. Grass that is all
the same height. No wall lines, no lintels, no wells — and you notice
the absence because every other biome taught you their density. The
*sari… sari…* is louder here than anywhere but the Felling-Site.
Travellers who have never met describe it in the same words: *the
feeling of being somewhere a sentence used to be.* And at thresholds —
certain hours, certain weathers, Thinning surges — **the old letters
bleed through**: a road resolves underfoot; a well; a town square with
its name almost readable on the trough.

**Formations.** Deliberately few — monotony is the design:

| Formation | Shape | The decision |
|---|---|---|
| **The Blank** | Sheet — single floor glyph, NO variants | low entropy reads as wrong precisely because every other biome is variant-rich |
| **The Rim** | Edge — where the good roads bend away | last waymarkers; descendants' pilgrimage benches face inward |
| **Shallow-bleed town** | authored: the square, the trough, the grandmother's door | threshold-triggered reveal; legible, human, recent |
| **Deep-bleed fragments** | 3, single-cell-to-few-cell, hand-placed | the bookbinder-marsh drying-lines; the shore where no sea is; the kiln-yard of unfired vessels |

**Hard canon rules, enforced as code review gates:** bleeds are
hand-authored, never procedural ("generated bleeds are wallpaper;
wallpaper is forbidden"); bleeds close **from the edges inward** —
that collapse direction is doctrine, not polish; bleed yields are
knowledge and singular curios, never farmable; the deep fragments are
never labeled, mapped, or explained — Maeleth's own catalogue entry for
them reads, in full, "prior"; **no under-text ever resolves into a
coherent mappable prior world** (Mystery Ledger §9 + amended policy).

**Render.** Desaturated everything: `BiomeColorPatcher` saturation −40,
contrast −10; floor a single glyph with **GlyphVariants deliberately
absent**; tint (0.9, 0.9, 0.9). During a bleed: revealed cells paint at
full saturation *through* the grey — warm lamplight colours in a
bleached world (order-0 rewrite via the bleed mask, §7.2). Nothing
animates except during bleeds.

**Population.** Nearly none. The **Unsaying** at the heart — a former
town, mostly Hush-bled, still partly inhabited by half-scraped hermits
who are "no longer entirely themselves" (dialogue where nouns slip);
Catcher cells walk the region on the anniversary of the Manifestation
and on that night catch nothing and speak to no one (a scheduled
storylet the player can witness).

**v1 content budget** (canon-specified): one hub-adjacent entry zone,
two interior zones, one shallow-bleed town, three deep-bleed fragments.
That's it. Scarcity is enforced by canon, not taste.

**Mechanics hooks.** **Underreading** (§7.3) reaches mastery here —
holding a bleed open is a channel-action with cost; the region
self-teaches (self-taught underreaders are "thumb-wet"). The Overwrit
never grows back; under Renewal, people finally build *at its edge,
facing it* — a post-ending world-state change.

**Engine work.** The bleed mask (per-cell bitfield zone-variant +
trigger table, §7.2) — the one genuinely new render-adjacent system;
the WorldClock it depends on (§7.1); everything else is authoring.

---

### 3.6 THE STUMP — the Root-slope (Tier 3–5)

**Identity.** The petrified stump of the god-tree — the central tepui —
with the Felling-Site at its base and the Root sleeping beneath. The
geology of every tepui is *the Tree's anatomy fossilised*; this is the
big one. Elevation is the content key: the real Sarisariñama survey
maps cleanly to bands, and roughly a third of the bestiary exists to
indicate a band or a state (`sarisarinama_bestiary_design.md:37-48`).

**Ground level, by band:**
- **Foothills** (outer chunks): blackwater creeks, 20m waterfalls,
  spray-zone pools — biodiversity hotspots; Spear-Leaf stands over
  streams where Glasspane Frogs call (transparent skin, visible beating
  heart); wine-red sundews on shaded banks.
- **The slopes** (mid): fungal forest over root-buttress ridges —
  colossal petrified roots radiating downhill like walls of grain-marked
  stone; the **Grainfield** — parallel stone ridges that are the wood
  grain of a bole a mile wide, running the same compass direction across
  every slope chunk, making the mountain read as one object.
- **The summit** (inner): petrified canopy — bromeliad scrub on stone
  domes; Tank-Brocchinia (drinkable rainwater, studyable
  micro-ecosystems, inoculable with glow-fungi for portable light);
  Summit Singers calling at twilight — **their silence is the alarm**;
  humid dwarf-forest at the rim, cloud passing through.
- **The Felling-Site** (one authored zone, Tier 5): the circle where the
  bark had gone soft as an old hand. Six positions of bare ground where
  no plant has grown in 1,080 years — **and the empty seventh: a single
  point, not a zone, where the air is wrong** — the highest Urqu-bleed
  point in the world, tile-level design. The Staking (the endgame
  ritual) happens here.
- **The Root of the World** (terminal): the stump's heart; all three
  ending-rituals; the Rooted's chamber just inside, behind the wall he
  reaches for.

**Formations:** Cascade gorge (Terrace + spray pools), Buttress ridge
(Braid at architectural scale), Grainfield (parallel ridges), Summit
scrub (Radial domes), Rim forest (Tube — a green crack), Sima mouth
(Edge — a void in the chunk; the descent, §4).

**Render.** Pink-grey sandstone (the real tepui is pink sandstone);
grain ridges `═`-class glyphs mapped to CP437; spray zones get the water
animation + Drip motes; summit gets cloud — a slow-drift fog overlay via
the existing cloud tile channel (currently render-only — its first
legitimate use). Tint shifts with band: warm base → cool bright summit.

**Flora/fauna by band** (the bestiary, sited): lowland — Sari-Snakes
(Urqu-reactive), Wardlines, Maw-Toads, Yellowfoot Wayfarer (tortoise
pack-animal that outlives owners — a walking archive); foothill —
Cascade-Father (spray-zone indicator), Glasspane Frog, Cascade-Lacer;
summit — Summit Singer, Brocchinia-Sentinel, Sky-Sari (apex, stoops);
simas — Gin Frogs, Pricklebrow gecko colonies (communal nests: 16
defenders — placeable living alarms), Helmwood Frogs (their presence
pings hidden water passages — the Door-detector species).

**Structures.** Waterfall shrines; Recension survey stations (they pay
per Tepui Bronze specimen); the Felling-Site circle; Sealed-Library
sinkhole (somewhere on the slopes — **never marked**; walled in
Tepuibone + Memory-Marble + Choir-Iron; a `LockPart` vault whose key is
a quest-arc); Olderdeep's mouth at the foot.

**Faction presence.** Everyone, thinly — every faction's deepest content
converges here but as designed sequence, not garrison. Catacomb-villager
territory below; Choir approach from the Grovelands side.

**Mechanics.** Tepuibone veins (mineral, anti-Urqu, the heaviest stone);
`tepuibone-slurry` liquid (ships!) finally has a home; elevation bands
as population-table keys; the *sari* gradient peaking at the Site.

**Engine work.** One new terrain builder family (ridge/terrace masks);
band-keyed population tables; the two authored Tier-5 zones; sima-mouth
edge formation connecting to §4 verticals.

---

## 4. The vertical world — sinkholes, catacombs, rootways

Canon's spatial grammar is vertical, and the engine already speaks it
(`Overworld.X.Y.Z`, stair connection registry, depth pipelines). A
**Sinkhole POI** claims a world cell and routes its z-levels to bespoke
pipelines (the multi-zone vertical recipe is proven — worldgen digest
§7d).

### 4.1 The sinkhole stack

```
Z=0  THE MOUTH    — surface chunk with the lip (Edge formation):
                    a void in the world, ringed by spray-fed green.
                    Overgrown mouths are found, not shown.
Z=1  THE DESCENT  — Terrace formation: ledges, rope anchors, cache
                    ledges (a prior expedition's supplies… and the
                    expedition). Falling is cheap and often lethal;
                    climbing back up costs turns and gear.
Z=2  THE FLOOR    — one archetype per sinkhole (rolled at worldgen,
                    then FIXED — the world persists):
                    · Drowned Sima (standing water, Gin Frogs,
                      semi-drowned green — the survey's ground truth:
                      mossy walls, bromeliad water, gecko nests)
                    · Time-Locked Forest (endemic species found
                      nowhere else; an Inquiry hotspot)
                    · Stranded Settlement (a catacomb village, §4.2)
                    · Choir Cathedral (substrate-grown vault; the
                      Wall-Catching network node)
                    · Boneyard (millennia of falls; what feeds there)
                    · Sealed Library (ONE, hand-placed, locked)
                    · Pure Catacomb (§4.2, no village — just the dead
                      and one keeper)
                    · Door (rare: exits into a DIFFERENT sinkhole's
                      floor across the map — the wormhole edge;
                      never mapped)
Z=3+ CATACOMBS / ROOTWAYS — below the floors, where placed.
```

### 4.2 Catacomb villages — the underground biome proper

The canonical source is `catacomb_village_design.md` (515 lines,
"if a question is answered there, it is answered"). The chunk design:

**Ground level.** You see the *glow* before you see the people.
Slime-painted signage in luminous green; a threshold you feel underfoot
(the Pebble-Sundew doormat — stepping on it announces you); a main
chamber whose centre is the **hearth-patch** (mature glowing fungus —
the village's physical and political heart; threatening it is war);
wall-stories of carved niches rising three tiers, rope ladders, carved
stairs; the **plaque-wall** — the village's literal history, oldest at
the floor, a chiselled-smooth niche where a name was judicially removed
(refusal-to-inscribe: the worst punishment — you cannot be Re-Membered);
a child's plaque behind an adult's: I WILL SLEEP HERE WHEN I AM DONE.
SAVE MY PLACE. People sleep in their own future tombs. It is not
morbid. It is home.

**The light IS the architecture.** Sight radius collapses past the
lantern-line; navigation is by light-quality ("two dims down, past
where the glow goes green"); the village day-cycle is
Bright / Half-Bright / Dim-Down / Dark-Watch, driven by the Patch-Tender
(WorldClock-synced ambient level modulation, §7.1). Distance is
measured in dims. Loud speech is an obscenity; leaving a lantern
burning at dinner is wasting light — culture-comedy hooks.

**Formations within the catacomb family:** Niche gallery (Comb),
Hearth chamber (Radial), Cistern, Tended way (beetle-path lanes),
**Dead zone** (over-harvested light-ecology scar: TRUE dark — ambient
zero, no bio-light grows, and the **eyeless apex predators** hunt by
sound — light is the player's tool, not theirs), the Sealed Dim (a
ritual darkness event, §7.7).

**The five village archetypes** (drop-in variants of one kit):
Patch-Bright (Concord-trading, with an unwanted Catcher cell in a
disused wing), Wall-of-Names (Recension-allied, 700-year plaque-wall),
Spore-Wedded (Choir-integrated — Spivenor is the canon instance),
The Quiet (hostile-isolated, 30 souls, barely intelligible dialect),
The Wardward (hunter village, apex-predator skulls on the walls).

**Defensive kit as readable tiles** (threat-specific architecture —
a legibility requirement): Spore-Block walls = Bloom-proof; Drosera
rings = Bloom/vermin biological defence; Choir-Iron lintels = tendril
veto; the Pebble-Sundew threshold = announcement; beetle alarm-jars =
crushed jar releases pheromone alarm. A player learns to read which
protection a room carries — and which it lacks.

**Olderdeep** (the founding village, Tier 5 approach): the
patch-chamber is an oval room; the Rooted lies at one focus — reclined,
arms wide, reaching toward the far wall, a bio-lit fungal plume erupting
from his sternum, held for 1,080 years; the gap between his fingertips
and the wall is sacred and no one walks through it; the bio-light dims
slightly there. Meeting him = **sleeping on the patch** (in his arms).
You wake having spoken with him in dream, and you smell of patch-bloom
for weeks (a status other villagers react to). This is a level-design
deliverable, not decoration — and it is the Renewal gate.

**Fauna:** lantern-beetles (jars, shoulder-rigs, hatcheries), glow-moths,
cave-fish, Pricklebrow geckos, slime-snails, cave-rats (pets or pests —
a generations-old argument), Bandfrogs; hunted: dead-zone apex
predators, Sari-Snakes (ward-fluid source), Bloomed creatures;
sacred: Sima-Stewards (echolocating seed-bringer birds who FOUNDED the
floor ecology — killing one is taboo; their clicks are usable sonar; and
their *absence* marks a dead zone).

### 4.3 The Rootways (Z=4+, under the Stump)

The living taproot's country: warm walls, grown geometry, the world's
oldest dark. Formations: Vein gallery (Tube), **Gradient room**
(chemical gradients as invisible walls — pathing curves and the reason
is not visible), the Weeping (sap coatings — the tile-layer working at
Tier 5), Heartwood cavity (Radial), Bark-fold maze (Rubble). Population:
lesser numina (pre-Tree fragments pooled at high Strangeness), nothing
ordinary. The Root's door at the bottom. All three endings walk through
here.

---

## 5. Places as level-design anchors (the god-rooms)

Five gods are **places**, each demanding one bespoke set-piece room and
one interaction verb — the game's boss fights, containing no bosses:

| God | Place | The room | The verb |
|---|---|---|---|
| Maeleth (Memory) | Quillhold deep stacks | the unshelved room; the First Account under glass; no one reads it aloud | long transcription work → the scribe's voice changes mid-dialogue |
| Othren (Preservation) | the Salt-Vault | tens of thousands of Salt-Cured bodies indexed under their last words — cosmic horror by scale, procedurally NEVER; her chamber; the lamp left an extra hour (do not tell her it is for her) | earn Trusted; attend the annual file-review ("Status: continuing") |
| Selen (Substrate) | the Deepest Cathedral | her body, mostly woman-shaped after a millennium; the substrate forms her face and loses it | pilgrimage-with-offering; the name-scene (three castings, once) |
| Tovreth (Exchange) | Tally, the high desk | printing presses; the contract with the scraped clause the eighth | pay the meeting-fee; he is the most accessible and the most tired |
| Ylaes (Beauty) | Posy | the founding village IS her artwork; she is the centrepiece, resin-cast, watching | behaviour-mediated dialogue: your aesthetic acts are your half of the conversation |
| Dohren (Roots) | Olderdeep | §4.2 | sleep on the patch |

Plus the two non-god anchors: **the Felling-Site** (six bare positions +
the empty seventh — a point, not a zone) and **the Root** (terminal).

---

## 6. Translating it to 80×25 CP437 — the render contract

Constraints honored (from the rendering inventory): one glyph per cell,
top entity wins; CP437 only (Unicode via `Cp437.Map` or it renders
`?`); sorting orders −1..7 fully allocated — new visuals ride existing
channels; the pure-ASCII path must stay fully functional
(`GraphicsPolish` off is the shipping fallback); never reveal state
through fog; all art 16×16 PPU 16 Muted Overgrowth.

**Per-biome ASCII identity** (glyph + tint + variant-density is the
cheap 90% of biome feel):

| Biome | Floor | Accents | Tint | Palette note |
|---|---|---|---|---|
| Spread | `.` `"` variant-rich | `#` hedges, `~` river | 1.0, 1.0, 0.94 | warm greens/golds |
| Sodden | `.` `,` tussock | `~` near-black water, `T` grey snags, `&` the Taken | 0.82, 0.86, 0.80 | brown-dark, tea water |
| Beating | `.` `,` near-white | `─┼` crust polygons, bone-grey ruins | 1.05, 1.0, 0.9 | glare + HDR accents |
| Grovelands | `.` dark loam | `♣` pale growth, violet veins, glow lights | 0.88, 0.92, 0.85 | dark ground, living light |
| Overwrit | ONE glyph, no variants | (bleeds paint full-colour through grey) | 0.9, 0.9, 0.9 | desaturated −40 |
| Stump | `.` pink-grey | `═` grain ridges, spray `~` | band-shifted | pink sandstone |
| Catacombs | `.` near-dark | slime-paint greens, plaque `≡` walls | ambient LOW | light is content |

**Light as the underground's terrain.** Per-zone `AmbientLevel`
(new small field consumed by LightMap in place of the 0.4 const):
Spread 0.40 → sinkhole floors 0.22 → catacombs 0.12 → dead zones 0.02 →
Rootways 0.08-with-warmth. The introspection doc's UX warning is
honored: 0.12 keeps the room navigable; the *content* (plaque reading,
dead-zone traversal) is what demands carried light. Bio-light sources
(patches, beetle-jars, slime-paint, Glow-Quartz) are `LightSourcePart`
entities — already how light works. The catacomb day-cycle and the
Sealed Dim modulate AmbientLevel on the WorldClock — a sequenced,
wall-by-wall dimming is a scripted event walking `MarkCellDirty`
across the room.

**The Thinning on screen.** A global **bio-light state** float (§7.7)
scales catacomb ambient + patch light radius + flicker event frequency.
The Rooted's nightmares dim every village at once; players who keep
notes notice weekly agitation. Consume's ending beat — every light out
at once, then returning *changed* — is this one variable, scripted.

**Sound without audio.** The *sari… sari…* is a MessageLog ambient
system: frequency = f(tier, Urqu-bleed); phrasing varies by culture
(the voice cards); at Tier 1 it appears exactly once, scripted, at
Sill. Summit Singer silence, Sima-Steward clicks, the bog's bubbles —
all text-log ambience with state-keyed frequency. Cheap, and the log IS
this game's soundscape.

---

## 7. New mechanics (small, each on an existing substrate)

Everything the canon demands that doesn't ship yet, sized:

**7.1 WorldClock.** Turn-derived hour bands (Dawn / Height / Dusk /
Dark; catacombs run Bright / Half-Bright / Dim-Down / Dark-Watch).
Consumed by: Overwrit bleed thresholds, Beating glare, catacomb cycle,
grove dusk-glow. One static class reading the turn counter. *No
day/night render pass needed* — hour bands drive events and statuses,
not a lighting simulation.

**7.2 Bleed mask (Overwrit only).** Per-cell bitfield on the zone +
2 authored variants (surface / bleed layer per revealed prop); a
data-driven trigger table per zone (hour band, weather tag, Thinning
flags); reveals event-driven via `MarkCellDirty`; closure animates
edges-inward. Canon supplied the implementation sketch verbatim
(`THE-OVERWRIT.md:81-89`).

**7.3 Undertext.** An `undertext` field on readable items/plaques —
two read-states; underreading reveals state two. KnowledgePart pattern;
canon calls it "the cheapest possible implementation of the whole theme
— ship this first." Diag: `category=undertext`, kinds
Underread/UnderreadRefused/BleedRevealed/BleedHeld/BleedClosed.

**7.4 The oath.** "Under the Cloth" status (3-day timer) + sanctuary
zones honoring it: hostile AI holds at the boundary (a faction-feeling
override while the status holds), Catcher behavior vetoed, bleed
suppressed. Player-sworn oath = a mirrored obligation trigger (an NPC
claims *your* hospitality) + a pacification verb (extend the cloth).
Oathbreak = the largest single rep event in the game.

**7.5 Wall-Catching + Being-Preserved.** Two displacement mechanics,
one shape: a status accumulates → at threshold, the player wakes
somewhere else. Wall-Catching (Choir): falling into Cathedral sinkholes
or voluntary use; destination is the Choir's choice among Cathedral
nodes; a Wall-Bound counter rises per use, permanently. Being-Preserved
(Catchers): triggered at low HP near a cell, thrown preservative pots
stack the status; at 100% you wake in a Curation archive — local,
regional, or central depending on *which named Catcher* caught you
(Wenil / Kavin / Pais — recurring persons, not respawns). Both reuse:
status effects + zone teleport + the world log.

**7.6 State-reactive spawning.** Population tables gain optional
predicates on world flags (UrquActive, BloomFront, EcologyDamaged).
A third of the canon bestiary is indicator species; static tables kill
the design. Small extension to `PopulationTable` + a world-flag store
(`NarrativeStatePart.FactBag` — ships).

**7.7 The Thinning clock + closure-ledger v1.** A global escalation
scalar (storylet-driven — the dormant non-quest storylet engine's first
real use): drives bio-light agitation, *sari* frequency, Sari-Snake
populations, Last-Counter dialogue, Sealed Dim length. Coupled to the
**closure-ledger**: quests/contracts/rentals emit
`closure/Closed|Refused|Abandoned` (diag contract from
`11_SecondSpine.md` C4); Abandoned increments local bleed, Closed and
**spoken-Refused both drain it** — every decline verb must exist in
dialogue (the Concord has paperwork for saying no; of course it does).
v1: coarse counters + zone bleed nudges; full simulation later.

**7.8 Grimoire/ink note.** The Ink economy (dormant) becomes the
Recension's scrip — "ledgers literally kept in ink" (TERMS.md). Scribe
services, copy-work, witness contracts pay in it. (Resolves the open
task #72 direction: ink is renewable *through the Recension*, at their
rates, with their questions.)

---

## 8. Factions on the map (presence, not territory)

Canon: no faction owns a region; presence is a per-place strength;
strongholds are rare; politics lives in mixed places. Applied:

| Faction (engine ID) | Display | Stronghold | Ambient presence |
|---|---|---|---|
| Palimpsest | **the Recension** | Quillhold | scribe-stations at every Tier-2/3 place; excavations (Sodden); survey posts (Stump) |
| RotChoir | the Rot Choir | Deepest Cathedral | groves rising toward the Stump; tendrils in Thresk-like lower quarters; Spore-Wedded villages |
| PaleCuration | Pale Curation | the Salt-Vault | regional preservation chambers (Marrowstye); salt-buyers (Beating); filers everywhere |
| SaccharineConcord | the Concord | Tally | trade-posts in nearly every place; caravans on every road; the Last Counter |
| Villagers | (surface folk) | — | the Spread; Sumphold; Wellmeet's town-half |
| GlassblownRemnant | (unexplained) | — | KEPT AS-IS: an under-text relic, canon by Mystery Ledger §6 — never explained |
| *new* TentRight | Tent-Right | Wellmeet / First Tent | the Beating; caravan guest-rights everywhere |
| *new* CatacombFolk | the Rooted's people | Olderdeep | every catacomb village; Lampwell light-trade |
| *new* BowerFolk | the Bower-Folk | Posy | installations "wide and shallow" — one glow-moth display in a catacomb village, a composed canyon |
| *new* ImminentArchive | the Catchers | (cells only) | disused wings of Curation outposts; road-shrine pamphlets |
| — | the Driving Bloom | — | **NOT a faction.** No rep track, no dialogue, ever. Terrain condition + effect. Bloom-rep routes through the Choir |

Internal IDs frozen (save-compat, TERMS.md engineering note); the
rename is display-name + dialogue only. The three named Catchers
(Wenil, Kavin, Pais) are persistent named NPCs with memory.

---

## 9. Mystery-ledger & sourcing compliance (design-time gates)

Checked against every entry — these are **build gates**, not
suggestions:

1. **Naro's reason** — the Staking asks the player to stake a reading;
   nothing ever grades it. No item text, god line, or epilogue may
   confirm any reading. All Seventh-evidence must be *attributable*
   (a document with a keeper, a memory with an heir).
2. **The Root's other dreams** — bio-light patterns that match no omen
   record exist as flicker events; no text explains them.
3. **The tenth fire** — placed, burning, unexamined. Forbidden from
   answering: everything and everyone.
4. **The doll in the wall** — one grove. No explanation. The Choir
   will not say, in any voice it has.
5. **The Glassblown Remnant** — the shipped Factions.json entry stands,
   as an unexplained remnant. Their silence is constitutional.
6. **The Bower's sight** — her resin-cast eyes see; what seeing is, is
   never stated.
7. **Gin Frogs** — in the bestiary, in the simas. No cosmological frame
   accounts for them and none will. Nothing may make them load-bearing.
8. **The layers** (§9) — no bleed, relic, god, or design doc counts,
   dates, or bottoms out the under-text. There is no design-canon
   answer. Fragments only, forever.
9. **The Preserved's consciousness** — evidence both ways, never
   resolved. (The Salt-Vault's horror depends on it.)
10. **Urqu language rule (lintable):** no intent-verbs on Urqu in any
    authored text — pressure flows, pools, deforms; mouthpieces carry
    the wanting. Superstitious NPCs may violate it; the design layer
    may not.
11. **Cultural sourcing:** structural shapes only, no lifted names or
    sacred vocabulary (no Wazaka/Makunaima; no Arabic hospitality
    terms; no Wanadi framing on fauna; no "mummy"; no real-stone
    culture-names); credits ledger updated per new borrowing; primary
    ethnography before authoring anything touching Pemon/Ye'kwana
    material. The word "mummy" is banned from in-game text.
12. **Voice gate:** every readable prop passes the voice cards — lexical
    taboos per culture (the Choir never says *dead*; the Concord never
    says *free*; the Catchers never say *die*), and the design-doc
    register is banned from in-world text. Fewer, better readables.

---

## 10. Build order (phases, each shippable)

Methodology per CLAUDE.md throughout: plan-to-disk per phase, pre-impl
verification sweep, TDD, counter-checks, adversarial sweeps where 2+
surfaces apply, cold-eye + both-angles audit, living doc in the same
commit. Every gate emits diag.

- **W0 — Plumbing (unblocks everything).** Authored world map + tier
  table + road/river overlay replacing noise (biome enum appended,
  never reordered); `AmbientLevel` per zone; WorldClock; display-name
  renames (Palimpsest → the Recension); starting village reframed as
  Sill; legacy quest quarantine. *Mostly data + small code; heavy test
  re-baselining.*
- **W1 — The Spread & Sill.** Formations, flower-charm terrain +
  bleed-wilt instrument, folk shrines, the inciting storylet. The
  Tier-1 world must feel warm before anything threatens it.
- **W2 — The Beating.** Salt pans, exposed ruin fields, Parched/glare,
  Tent-Right camps + the oath (7.4), Wellmeet, the First Tent, salt
  economy, the tenth fire (placed, silent), the Last Counter + its two
  abandoned predecessors.
- **W3 — The Sodden.** Mire/reed/peat formations, Bog-Taken terrain
  bodies, methane peat, Sumphold, the Drowned Ledger, body-courier
  contracts.
- **W4 — The Grovelands.** Grove rules, tendril content activation
  (the 37-node dialogue tree finally spawns), Bloom front overlay +
  BloomedEffect, spore gas siting, the doll (one grove).
- **W5 — Vertical.** Sinkhole POI + Descent/Floor pipelines, three
  floor archetypes first (Drowned Sima, Stranded Settlement, Choir
  Cathedral), catacomb village kit + five archetypes, bio-light
  economy, plaque-walls (KnowledgePart at scale), dead zones, Wall-
  Catching + Being-Preserved (7.5), Lampwell, Spivenor.
- **W6 — The Stump.** Elevation bands, bestiary-by-band with
  state-reactive spawns (7.6), the Grainfield, sima mouths, the
  Felling-Site (authored, tile-level), Olderdeep + the Rooted's
  chamber, the Sealed Library vault.
- **W7 — The Overwrit.** WorldClock triggers → bleed mask (7.2),
  undertext (7.3 — though it can ship as early as W1 on documents),
  the four authored zones + three fragments, the Unsaying, underreading
  teachers.
- **W8 — The gods & the clock.** The five god-rooms + verbs, the
  Thinning escalation storylets + closure-ledger v1 (7.7), Quillhold /
  Salt-Vault / Tally / Posy as full places, Slip's east quarter.

Estimated at agent-pace (per MEMORY.md's working model): W0 is a
focused multi-session phase; W1–W4 roughly a phase each; W5 and W6 are
the two big ones; W7–W8 are authoring-heavy but system-light. Each
phase leaves the game playable and each biome complete when it lands.

---

## 11. Appendix — four chunks, sketched

40-column excerpts of 80×25 zones (right half elided), pure-ASCII tier.
These are *illustrations of feel*, not stamps.

### A Spread chunk — field-and-hedge, the old road (Tier 1)

```
" " " . " " # # # # # # # . # # # # " "     # hedge (green, burns)
" , " " . " " " . " " " " . " " " , " "     . lane / worked earth
= = = = = = = = = = ⌂ = = = = = = = = =     = the old road
" " ! " " . " " " " . " " " " " " . " "     ⌂ waymarker (scraped —
" " " " # # # # . # # # # # # . # # " "        undertext, later)
" . " " # " " " " " ❀ ❀ " " " . " " " "     ❀ flower-charm meadow
" " " s " . " " " ❀ ❀ ❀ " " " " " , " "        (wilts early if the
" " " " " " . " " " ❀ " " . " " " " " "         zone bleeds)
```
The decision: hedges cut the open country into rooms; the road is fast
and watched; the meadow is the instrument you don't know you have yet.

### A Sodden chunk — the causeway (Tier 2)

```
~ ~ ~ , ~ ~ ~ ~ . ~ ~ ~ ~ , ~ ~ ~ ~ ~ ~     ~ black water
~ , ~ ~ ~ . ~ ~ ~ ~ ~ & ~ ~ ~ . ~ ~ , ~     . tussock (safe-ish)
= = = = = = = ▒ = = = = = = = = = = = =     = duckboards
~ ~ T ~ ~ , ~ ~ ~ ~ . ~ ~ ~ T ~ ~ ~ ~ ~     ▒ rotten plank (rolls)
~ ~ ~ ~ g ~ ~ ~ , ~ ~ ~ ~ ~ ~ ~ , ~ ~ ~     & Bog-Taken, visible
, ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ F ~ ~ ~ ~ ~ ~ ~ .     T drowned snag
~ ~ , ~ . ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ . ~ ~ ~ ~     F Gin Frog (silent)
```
The decision: the causeway is THE line, and everything that hunts knows
it; leaving it is a cell-by-cell gamble; the body in the peat is a
readable, and someone will pay for its delivery — sealed to standard.

### A Beating chunk — salt pan with a Tent-Right camp (Tier 2)

```
. ─ . . ┼ . . ─ . . . ─ . . . . ┼ . . .     ─ ┼ crust polygons
. . . . . . . . ▲ ▲ . . . . . . . . . .     ▲ tents (interior cells:
─ . . ┼ . . . ▲ ○ ▲ ! . . ─ . . . ┼ . .        no glare, oath holds)
. . . . . . . . ▲ ▲ . . . . . . . . . .     ○ the well
. . ─ . . . . . . │ . . . . ┼ . . . . .     ! the guest-cloth pole
. . . . ┼ . . . . │ . . . . . . . ─ . .     │ caravan road south
. . . . . . ─ . . │ . . s . . . . . . .     s Sari-Snake (only if
. ┼ . . . . . . . │ . ─ . . . . . ┼ . .        Urqu is active)
```
The decision: outside is glare, thirst and total sightlines; under the
cloth is the safest ground in the game. Your pursuers are two chunks
behind you. They will reach the pole. They will wait outside, politely,
for three days. That's the faction.

### A catacomb village — hearth chamber (Tier 3, ambient 0.12)

```
█ █ ≡ ≡ ≡ █ █ █ ≡ ≡ ≡ ≡ █ █ ≡ ≡ ≡ █ █ █     █ carved wall
█ n n n n █ . . . . . . . █ n n n n n █     ≡ plaque-wall (readable;
≡ n . . . . . . ☼ ☼ ☼ . . . . . . n ≡        one niche chiselled
≡ n . j . . . ☼ ☼ ☼ ☼ ☼ . . . j . n ≡        smooth — ask no one)
≡ n . . . . . ☼ ☼ ☼ ☼ ☼ . . . . . n ≡     n niche homes (3 stories)
█ n . . . ✳ . . ☼ ☼ ☼ . . ✳ . . . n █     ☼ the hearth-patch (glows,
█ █ . . . . . . . . . . . . . . . █ █        radius 4, IS the town)
█ ≡ ≡ █ ∴ ∴ ∴ █ . . . █ ∴ ∴ ∴ █ ≡ ≡ █     ✳ Drosera ring  j beetle-jar
█ █ █ █ █ █ █ █ █ ░ █ █ █ █ █ █ █ █ █     ∴ Pebble-Sundew threshold
                  ░ ← the way out, two dims down, past where
                       the glow goes green
```
The decision: light is the map. The patch is warmth, food, politics and
the god's dream in one object; the plaque-wall is the village's entire
history, readable; and if the lamps start shuttering wall by wall, be
stone, be moss, be nobody's, be the wall's — until morning.

---

## 12. Honesty bounds

- **Nothing here is play-verified.** Formation variety, glare pacing,
  catacomb ambient levels, bleed drama — all designer assertions until
  a build exists and screenshots are taken. The light-economy numbers
  in §6 especially need live tuning (the introspection doc's
  quit-trigger warning is real).
- **The map in §2.2 is a first draft.** Canon fixes the *relationships*
  (Tally ≠ Root; Overwrit as map-hole; Salt-Vault near wasteland salt;
  grove-density toward the Stump; Sill on a river at Tier 1); the
  specific cells are mine and cheap to move.
- **Placement choices flagged:** the Unsaying is sited inside the
  Overwrit's rim (canon says the Great Manifestation scar is "possibly
  the Unsaying" — I committed the identification; one line of lore
  review can undo it). The Quiet's Door went to the far NE on thin
  evidence. Thresk didn't get a cell (the map is full at 22 places;
  Slip carries the contested-town role) — restoring it costs one cell.
- **The legacy content decision is the user's:** retiring the
  Adventure-Time starting quests and the Elemental Crossroads is
  implied by `TERMS.md`'s quarantine + this redesign, but it deletes
  shipped, working content. Flagged, not assumed.
- **Two lore-internal contradictions surfaced during study** (for a
  lore pass, not this doc): Pale Curation founding "year ~45" vs
  "~year 70" (`03_History.md:72` vs `:84,196` — the developed account
  says 70); a deep-bleed fragment list drift between
  `PALIMPSEST-INTEGRATION.md:259` and `THE-OVERWRIT.md:52` (canonical
  doc wins: the kiln-yard, not the fire-pits).
- **Save-compat:** biome/POI enums append-only; faction IDs frozen;
  old saves reference old biomes — a new world is a new game. This
  overhaul assumes new worlds; it does not migrate existing saves.

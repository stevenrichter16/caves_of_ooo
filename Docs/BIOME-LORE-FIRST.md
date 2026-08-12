# Biomes, Lore-First — deriving the world from the Felling

> **Status: brainstorm. No code, no tests, nothing committed but this
> file.** Written 2026-08-11 against the user's brief: *the biomes were
> decided first and the lore was retrofitted; every chunk inside a biome
> is essentially the same chunk.* Both halves of that are true and both
> have a precise mechanical cause. This doc names the causes, proposes a
> structural fix for each, and then brainstorms the biome roster that
> falls out of the tepui cosmology once the structure can express it.
>
> **Corpus warning (added after a branch-wide audit).** This repo holds
> **two lore corpora describing different worlds**, and the first draft
> of this doc read the wrong one:
>
> - **`Lore/` (repo root) — the tepui-thread. THIS IS CANON.** The
>   Felling, the Root of the World, the Six gods, Urqu, the Thinning.
>   `Lore/10_Bible.md` is the declared authority; `Lore/README.md` states
>   the conflict rule. Last touched 2026-07-15. It says plainly:
>   *"Tepuis are the defining geographic feature of this world"*
>   (`Lore/History/00_Canon.md:58`).
> - **`Docs/Lore/` + `Docs/Lore/v2/` — the Palimpsest world.** The
>   Overwriting, Reedhaven, the Sealhand Crusade, the Ember Order, the
>   Glassblown Remnant, the Brine Communion. Last touched 2026-05-04;
>   `Docs/Lore/v2/README.md` calls itself "a parallel redesign." **None
>   of those faction names appear anywhere in `Lore/`** (verified by
>   grep). It contains no tepui.
>
> Sections marked ⚠️ below still carry first-draft reasoning from the
> Palimpsest corpus. The *shapes* survive; the *faction attributions* do
> not.
>
> Sources read: `IDEAS.md` (Root of the World, Sinkholes, Catacombs,
> Urqu, bioluminescent economy, catacomb-villagers), `Lore/` canon
> (Bible, `History/00_Canon.md`, `History/01_Spine.md`,
> `History/02_Geography.md`, Factions), `Docs/Lore/v2/`
> (World, Places — **superseded**), `Docs/Design/WORLD-INGREDIENTS.md`,
> `Docs/WORLD-DESIGN-INTROSPECTION.md`, `Docs/W1-WORLD-CEMENT-PLAN.md`,
> and the shipped worldgen (`WorldGenerator.cs`, `OverworldZoneManager.cs`,
> the five terrain builders, `LandmarkBuilder.cs`).

---

## 1. Why the lore feels retrofitted — the mechanical cause

`WorldGenerator.cs:30-50`. One 2-octave noise field over the 20×20 world,
quartiled:

```
n < 0.25 → Cave     n < 0.50 → Desert
n < 0.75 → Jungle   else     → Ruins
```

That is the entire world map. There is no elevation, no water table, no
drainage, no distance-from-anything. A Desert cell can border a Jungle
cell which borders a Cave cell, because the four names are just four
buckets of one scalar.

**A biome map made of noise buckets cannot express a cosmology, because
nothing in it is a consequence of anything.** The lore says the world was
made by a specific event — a colossal tree was felled; the stump remains
as the tepui; the rivers, the species and the flood all came out of the
felling. That is a *causal* geography. Noise quartiles are an *acausal*
one. So lore could only ever be attached to the biomes as labels, which
is exactly what the user is feeling.

### ⚠️ Correction — the canon already rejected the obvious fix

The first draft of this section proposed deriving the world map
geometrically from the Root: elevation as distance-from-Root, drainage,
a debris belt oriented by the fall. **Canon has already considered and
rejected exactly that**, and I hadn't read it when I wrote it.

`Lore/History/02_Geography.md:13-17` — Phase 2 v1 committed a concentric
ring world (Heart / Veins / Spread / Sodden / Beating / Hush). **Phase 2
v2 dropped the concentric model entirely**, after a player-experience
critique the user asked for: a global geometry "optimizes for coherence
viewed from orbit, a problem the player does not have." The specific
costs named were a backtracking tax, a circumferential-travel problem, a
one-way funnel, content-density inversion, and a two-coordinate load
players never actually hold.

What replaced it (`02_Geography.md:156-165`, locked):

- The world is a **hand-authored network of ~30 places**, not a global
  geometry. Roughly 2–4 travel edges each.
- **Distance from the Root is an authored *Strangeness Tier* (1–5), not
  a coordinate.** Tier governs substrate density, divine presence and
  Urqu-bleed. Players never see the number; they feel it.
- Tier is deliberately **not** tied to travel distance — a Tier-4
  catacomb mouth can sit a day's walk from a Tier-1 farming valley.
  Designer control of the strangeness curve is the entire point.
- **Tally** (Concord exchange) is the traversal hub; **the Root** is the
  narrative centre. Keeping those separate is the fix v2 delivers — v1's
  named error was making the cosmological centre also the traversal
  centre.

So the honest version of this section's claim is narrower, and it still
stands:

> The noise-quartile biome map is acausal, and that is why the lore reads
> as retrofitted. But the replacement canon wants is **an authored place
> network with per-place tiers**, not a derived-from-the-Root coordinate
> system. The biome layer's job is to make each *place* legible, not to
> encode a world-shape.

That is a smaller and cheaper change than the one I first proposed, and
it is the one the lore is actually asking for. What survives intact is
everything in §2 and §3 below — those are about the interior of a chunk,
which no lore decision touches.

**Where the two corpora's places live.** The canon place list
(`02_Geography.md:55-99`) is Sill (the recommended start), Gantry,
Cinderhold, Tine, Quillhold, Tally, Posy, Wellmeet, Marrowstye,
Sumphold, Olderdeep, the Salt-Vault, Lampwell, the Drowned Ledger,
Thresk, the Deepest Cathedral, Spivenor, the Last Counter, Slip, the
Unsaying, the Felling-Site, the Root. Several are already terrain briefs
in disguise — the Salt-Vault is built "down into and around the tepui's
sinkhole" (`Factions/03_PaleCuration.md:30`), Lampwell is the
bioluminescent-economy capital, Sumphold is bog-edge raised ground.

---

## 2. Why every chunk is the same — the mechanical cause

Each biome has exactly **one** terrain builder with **one** hard-coded
parameter set applied uniformly to all 2,000 cells:

| Builder | The whole algorithm |
|---|---|
| `CaveBuilder.cs:21-23` | cellular automata, SeedChance 55, noise ≤ 0.47 |
| `JungleBuilder.cs:19-22` | same CA, SeedChance 48, trees 10%, bushes 8% |
| `DesertBuilder.cs:18-20` | noise ≥ 0.85 → wall, rocks 5%, cacti 3% |
| `RuinsBuilder.cs` | rectangular rooms + corridors |
| `StrataBuilder.cs` | depth-banded CA |

So two Cave chunks differ only in *where the same noise landed*. They are
never a different **kind** of place. The variation that does exist is
bolted on top of an unchanging substrate — a structure stamp
(`LandmarkBuilder`, 21 stamps), hazard patches (`HazardTerrainBuilder`),
a creature scatter. A shed in a field is still a field.

Stated plainly: **there is exactly one topology per biome, and it is
hard-coded in the builder.** That is the whole of the sameness problem.

---

## 3. The structural fix — three axes instead of one

Today: `Biome → generator`. One-to-one. Proposed:

```
Chunk = Topology  ×  Biome palette  ×  Condition overlay
        (shape)      (matter+rules)    (what happened here)
```

### Axis 1 — Topology (biome-agnostic, ~10 of them)

The shape of the space, independent of what it's made of. These are
reusable across every biome, which is what makes the combinatorics pay:

| Topology | Reads as | Changes play by |
|---|---|---|
| **Sheet** | flat, no cover at all | sightlines are total; nowhere to break line of fire |
| **Blob** | today's CA cave | the familiar baseline — keep it, stop making it the only one |
| **Braid** | channels, fissures, streambeds | movement is committed to lanes; ambush geometry |
| **Terrace** | shelves and ramps, elevation bands | high ground, drop-offs, one-way descents |
| **Archipelago** | passable pockets in an impassable matrix | crossings are the decision; the matrix is the hazard |
| **Rubble maze** | dense small obstruction | short sightlines, melee-favouring, slow |
| **Radial** | everything organised around one centre | the centre is the reason you came |
| **Comb** | regular carved cells — niches, vaults, rooms | door-by-door clearing; the grid is legible |
| **Edge** | one border of the chunk is a void or cliff | the map has a wrong side; falling is real |
| **Tube** | you are inside something | no flanking, one axis, claustrophobic |

The current game ships **Blob, Comb (Ruins), and a degenerate Sheet
(Desert)**. Seven of ten are missing entirely.

### Axis 2 — Biome palette

What the matter is, what grows, who lives there, what statuses the ground
holds, what the loot table is. This is largely what `BiomeType` already
means — it just stops implying a *shape*.

### Axis 3 — Condition overlay

One cheap mutator applied on top of any topology+palette: **flooded,
burning, Choir-bloomed, salted-dead, frozen, layered-thin, fog-blind,
recently-fought-over**. Overlays multiply variety at near-zero authoring
cost and they are the natural home for the status-terrain work already
shipped (`TileStateSourcePart`, `HazardTerrainBuilder`).

**Arithmetic:** 10 topologies × 9 palettes × 8 overlays is 720 nominal
chunk kinds. Realistically you'd whitelist maybe 6 topologies per palette,
which is still ~50 distinct chunk kinds against today's 4.

### The two rules that keep it honest

1. **A formation must be nameable in four words from the map edge.** "Salt
   sheet with a sinkhole." "Trunk-road over a chasm." If the player can't
   name it, it's decoration, not a formation.
2. **A formation must change a decision, not just a texture.** If the only
   difference is which glyph the floor uses, it isn't a formation — it's a
   tint, and tints already exist (`OverworldZoneManager.GetBiomeTint`).

---

## 4. The biome roster, derived from the Felling

Vertical first, because the lore's geography is vertical: *summit → sima →
floor → catacomb → rootway*, which the engine already supports as
Z=0→Z=1→Z=2+ (`WORLD-DESIGN-INTROSPECTION.md:30-38` — the design and the
engine already agree here).

### 4.1 The Cut — the summit plate ⭐

The sheared top of the stump. Petrified wood-grain stone (tepuibone), rain
scoured off it in hours, almost no soil.

- **Why it exists:** it is the cut surface where the Tree was felled. The
  grain of a bole a mile across is still visible in it, running one way
  across the whole plate.
- **What that does to play:** **no soil means the Choir cannot root here.**
  The safest ground in the world from the Choir and the worst ground for
  food or water. ⚠️ *First draft credited this to the Ember Order and the
  Glassblown — both Palimpsest-corpus factions that do not exist in
  canon. The canon tenants are open;* `History/00_Canon.md:64` *already
  defines a Summit band ("bromeliad scrub, the most-endemic species"), so
  the ecology is canon and only the residents need authoring.*
- **Formations:** *Grainfield* (Braid — parallel fissures all running the
  same compass direction across every chunk on the plate, which makes the
  plate read as one continuous object); *Rain-pan terrace* (Archipelago —
  perched pools, the only water); *Wind barren* (Sheet — no cover, and the
  wind is a mechanic); *The Lip* (Edge — one border is a sima void);
  *Knuckle* (Radial — a burl the size of a hill); *Fissure forest* (Tube —
  a scrap of cloud-forest grown in one deep crack, green walls, bare stone
  fifty feet above).

### 4.2 The Simas — the descent shafts ⭐

The sinkholes that pierce the plate. Already fully designed in
`IDEAS.md` — this doc adds only the formation list.

- **Formations:** *Open mouth* (Terrace — wide, ledged, survivable);
  *The Throat* (Tube — near-vertical, needs rope); *Overgrown lip*
  (the entrance you can walk past without knowing); *Broken shelf*
  (Terrace + gap — the descent is severed halfway and the crossing is the
  puzzle); *Waterfall shaft* (wet, loud — sound masks both directions);
  *Cache ledge* (a prior expedition's supplies, and the expedition).

### 4.3 Sima floors — the variety engine

Each floor is a different *world*, per the IDEAS archetype table: Drowned
Sima, Time-Locked Forest, Stranded Settlement, Choir Cathedral, Boneyard,
Sealed Library, Pure Catacomb, Door. **This biome solves chunk-sameness by
construction** — no two instances share content — which makes it the best
proof-of-concept for the formation axis even though it is the most
expensive.

### 4.4 The Rootways — the living taproot

Below the stump: wood that never petrified. Warm walls, breathing stone,
grown geometry.

- **Formations:** *Vein gallery* (Tube); *Gradient room* (chemical
  gradients as invisible walls — you cannot path straight and the reason
  is not visible, which is the Choir's whole architecture per
  `IDEAS.md`'s First Root description); *The Weeping* (sap as hazard —
  reuses the coating system directly); *Heartwood cavity* (Radial);
  *Bark-fold maze* (Rubble).

### 4.5 Splinterfall — where the canopy came down ⭐ **best cost/impact**

A belt of colossal petrified branch debris, thinning with distance from
the Root, oriented by the direction of the fall.

- **Why it exists:** the canopy of a world-tree had to land somewhere.
- **What that does to play:** you walk *on* and *through* fallen trunks the
  size of buildings. Tepuibone is the surest binding material in the game
  (`WORLD-INGREDIENTS.md:86`), so this is the salvage economy's home and
  the Concord cuts here.
- **Formations:** *Deadfall lattice* (Rubble maze at architectural scale);
  *Trunk-road* (Tube/Edge — walk the length of one enormous bole with
  drops either side); *Splinter field* (Rubble); *The Stack* (Radial — one
  trunk speared vertically into the ground, and everything arranged around
  it); *Sawn ground* (Comb — a Concord cutting camp, regular and
  industrial against all that chaos); *Hollow bole* (Tube — inside a
  trunk).
- **Why it's the cheapest win:** it is *only* walls and floors arranged
  differently. No new systems, no new statuses, no light economy. Pure
  topology, and it makes the Felling visible from the ground for the first
  time.

### 4.6 The Wash — the flood's dumping ground

Where the flood that came out of the felling put everything it carried.

- **Formations:** *Braid* (channels and gravel bars); *Debris fan* (Radial,
  fanning from a gap in the hills); *Cut bank* (Terrace with an exposed
  stratigraphy face — **the layers are readable**, which is Palimpsest
  content sitting in terrain); *Gravel barren* (Sheet); *Ox-bow marsh*;
  *Wrack bar* (flood-deposited bone and wreck — the Boneyard's surface
  cousin).

### 4.7 The Whitepan — evaporite salt flats ⭐ **replaces Desert, honestly**

Terminal drainage. The water arrives and stops and leaves its salt.

- **Why it exists:** the drainage has no outlet. That is the whole story.
- **What that does to play:** **the Choir cannot grow in salt** — canon,
  and it makes salt strategic rather than scenic. ⚠️ *The first draft
  hung this on the Sealhand, the Glassblown and the Brine Communion —
  all Palimpsest-corpus, none in canon.* **Canon puts something better
  here:** the Pale Curation's first preservation chamber was "a salt-vault
  in a lesser tepui near a wasteland-edge salt source… salt was abundant
  in the post-Felling raw-sun zones and salt preserves"
  (`Lore/Factions/03_PaleCuration.md:30`), and that vault grew into the
  Salt-Vault, the central archive, "built down into and around the tepui's
  sinkhole." Wellmeet is the canon salt-trade town
  (`History/02_Geography.md:67`). So the whitepan is **Pale Curation
  home ground and the Tent-Right's economy**, which is a stronger
  attribution than the one I invented.
- **Formations:** *Sheet* (zero cover — a duelling ground, and a terrible
  place to be outnumbered); *Polygon crust* (the hexagonal desiccation
  cracks as a low-relief ridge maze); *Brine lens* (Archipelago — thin
  crust over deep brine, and breaking through is the hazard; this is
  `BrinePool` doing structural work instead of decorating); *Salt garden*
  (Rubble — crystal towers); *Wellmeet crust* (a salt-trade market on the
  pan); *Cure-field* (Sheet + overlay — Pale Curation's open-air salt
  curing, geometric and deliberate).
- **Why it's cheap:** the status-terrain layer already ships brine, salt,
  and crust hazards. This is mostly a palette and a formation table.

### 4.8 The Overwrit — the scraped region ⭐ **replaces Ruins, honestly**

**This one got *better* when I found the right corpus.** My first draft
invented "the Layered" out of the Palimpsest world's Overwriting. Canon
already has the real thing, and it is sharper:
`Lore/Design/THE-OVERWRIT.md` — a region *scraped*, not destroyed, by a
season-long failed birth of the seventh god. Strangeness Tier 4.

- **What it is like normally:** near-blank terrain, wrong by absence.
  Ground too smooth. Growth too sparse and too new. **No ruins where
  ruins should be** — a region this old should be full, and it has been
  *prepared*. The `sari... sari...` is louder here than anywhere but the
  Felling-Site. Roads bend around it "the way handwriting bends around a
  hole in the vellum."
- **What that does to play:** the dual-layer idea, but **canon supplies
  the trigger I was missing.** At thresholds — certain hours, certain
  weathers, Strangeness spikes, Thinning surges — *the old letters bleed
  through*. A road resolves underfoot. A well. A town square with its
  name almost readable on the trough. So the chunk is not permanently
  two-layered; it is blank terrain that **periodically remembers**, and
  the player learns to be standing in the right place at the right hour.
  That is a far cheaper implementation than a persistent dual map *and*
  a better scene.
- **Formations:** *Blank* (Sheet — the horror is the emptiness, and it
  should be genuinely boring to cross); *Bleed-street* (a street grid
  that exists only during a bleed); *The prepared ground* (too smooth,
  too new); *Shallow bleed* (the most recent scraped layer, most nearly
  legible — where descendants come to stand where a grandmother's door
  faintly is); *Rim* (Edge — where the good roads bend away);
  *Bleed-close* (where `BleedGlass` is recoverable, once, per
  `WORLD-INGREDIENTS.md:92`).
- **Cost:** still the most expensive item here, but the threshold model
  makes it a *condition overlay on a Sheet* rather than a second map.
  That may be tractable after all.

### 4.9 The Reach — Choir ground ⭐ **replaces Jungle, honestly**

Where the mycelium surfaces in force.

- **Formations:** *Grove* (Radial — cathedral columns, open floor);
  *Mat* (soft ground, everything sinks, movement costs more);
  *Fruiting wall* (Rubble — dense vertical growth); *Composting field*
  (things half-eaten — loot and horror in the same pile); *Tendril road*
  (a Choir-grown road; canon's *Wall-Catching* has the Choir choose your
  destination — `IDEAS.md`, and a travel-graph edge type at
  `History/02_Geography.md:115`); *Bloom front* (an actively
  advancing edge — this is a condition overlay promoted to a formation).

### 4.10 The Catacombs — burial geometry

Fully designed in `IDEAS.md` including the bioluminescent light economy.
Formations: *Niche gallery* (Comb), *Collapsed stack*, *Tended chambers*
(the catacomb-villagers), *Dead zone* (true dark + eyeless apex predators),
*Cistern*, *The unreadable oldest*.

**The introspection doc's UX warning stands** — `WORLD-DESIGN-INTROSPECTION.md:132-164`
argues a hard light economy is a quit-trigger without an ambient floor.
Believe it.

---

## 5. What I'd actually build, in order

**The formation axis is worth more than any new biome, and it's cheaper.**
Ten new biomes on a one-topology-per-biome engine would produce ten new
kinds of sameness.

1. **Formation layer, retrofitted to the current four.** No new content:
   give Cave five formations (blob / braid / terrace / radial / rubble),
   Jungle four, Ruins four, Desert four. Every existing chunk in the game
   gets more varied without a single new blueprint. This is the change the
   user actually asked for, and it is a builder-selection refactor plus
   four to six new terrain generators.
2. **Splinterfall.** Cheapest new biome, biggest cosmological payoff — the
   Felling becomes visible. Pure topology; reuses every existing system.
3. **Whitepan** replacing Desert. Reuses the shipped status terrain; makes
   salt strategic; and canon already homes the Pale Curation's founding
   salt-vault and Wellmeet's salt trade there.
4. ~~**Causal world map.**~~ **Struck — canon rejected it** (see §1's
   correction box). The replacement is **Strangeness Tier as a per-zone
   authored property**: give a zone a tier 1–5 that drives substrate
   density, Urqu-bleed and encounter strangeness, and let the *authored
   place network* decide where the tiers sit rather than deriving them
   from coordinates. Much cheaper than the geometry rewrite I first
   proposed, and it is the thing that actually delivers "the world gets
   stranger as you go deeper."
5. **The Cut and the Simas.** The vertical grammar. Engine already
   supports the Z-levels, and `History/00_Canon.md:62-64` already
   authors the elevation bands.
6. **The Overwrit.** Last — but cheaper than I first thought, because
   canon's threshold-bleed model makes it an overlay rather than a
   second map.

Rough shape, agent-pace: (1) is a few sessions; (2) and (3) are about one
each on top of it; (4) is now a small property plus its consumers rather
than a worldgen rewrite; (5) is a multi-phase effort; (6) is a phase.

---

## 6. Honesty bounds

- **Nothing here is verified against play.** These are shapes on paper. The
  claim "a Sheet chunk plays differently from a Blob chunk" is a designer's
  assertion, not a measurement.
- **The roster is deliberately over-long.** Ten biomes is a brainstorm, not
  a plan. §5 is the plan.
- **I did not audit the reframe cost.** Renaming Desert → Whitepan and
  Jungle → Reach touches shipped dialogue, quest text, population tables
  and 21 structure stamps.
  `WORLD-DESIGN-INTROSPECTION.md:194-206` already flagged this and asked
  for a grep of biome-specific references in conversations and storylets
  before committing to any reframe. **That audit has not been run.**
- **I read the wrong corpus on the first pass, and the first draft was
  wrong in two specific ways.** See the corpus warning at the top. The
  errors were: (a) proposing a Root-derived geometric world map, which
  `Lore/History/02_Geography.md` had already committed and then
  deliberately dropped; (b) attributing biomes to the Ember Order, the
  Glassblown, the Sealhand and the Brine Communion, none of which exist
  in canon. Both are corrected in place above. Everything in §2 and §3 —
  the sameness diagnosis and the three-axis fix — was unaffected, because
  it is about the inside of a chunk and no lore decision touches that.
- **Two corpora still coexist in the repo and nobody has retired one.**
  `Lore/` (tepui-thread, canon, 2026-07-15) and `Docs/Lore/` +
  `Docs/Lore/v2/` (Palimpsest world, 2026-05-04). `Lore/` declares
  itself authoritative and `Docs/Lore/v2/README.md` calls itself a
  parallel redesign, so the precedence is clear on paper — but the
  Palimpsest corpus is still sitting in `Docs/` unmarked, which is how I
  walked into it. **Worth a one-line authority banner on
  `Docs/Lore/README.md`** pointing at `Lore/10_Bible.md`. That is a
  five-minute fix that prevents the next reader making my mistake.
- **The cultural-sourcing obligations in `IDEAS.md` apply to everything
  here.** The tepui, sima and catacomb material inherits structural shapes
  from Pemon and Ye'kuana cosmology and from real Guiana Highlands geology;
  the ledger at `IDEAS.md:902` requires that borrowing stay visible and
  credited, and requires real reading before any in-game text is authored.

# Palimpsest Combat System v0.1 — spec vs. what is actually in the game

**Analysed:** 2026-08-09, against the branch at SM8.
**Method:** every claim below was checked against source or content
files, not recalled. "Reachable" means *a player in a normal
playthrough can encounter or use it* — not merely that a class exists.

---

## The one-paragraph verdict

The spec's **vocabulary** is largely already in the game, and in several
places the shipped version is *richer* than the prototype asks for — 25
liquids where the spec wants 3, 13 data-driven reactions, 7 gases. What
is missing is not nouns but **verbs and framing**: there is no Stamina,
no Ink *pool*, no tile substrate layer, no residues-as-tile-state, no
Scrape economy, no combo rewards, and no reading UI. And there is one
deep architectural divergence that matters more than any individual
gap: **the shipped system is actor-centric where the spec is
tile-centric.**

---

## 1. The architectural difference that matters most

| | Spec | Shipped |
|---|---|---|
| Where state lives | **The tile.** Substrate / Structure / Coatings / Residues / Energy / Cloud | **The creature.** 34 `Effect` subclasses on `StatusEffectsPart` |
| What a reaction reads | two tile layers | two **entity materials** (`MaterialReactionResolver`) |
| What a rite reads | the tile it targets | the **statuses on a creature** (`ResonanceSystem`, SM7) |

Everything in SM7–SM8 — resonance, riders, the multiplier curve, ink,
the four rites — operates on **statuses attached to creatures**. The
spec's entire §3 world-state model, §11 reaction rules, and §15 Scrape
economy operate on **tiles**.

This is not a blocker; it is the honest measure of the work. The
`ResonanceSystem` I shipped is a `Preview`/`Spend` pair over "a thing
that has statuses" — it does not care that the thing is a creature. A
tile-state layer that exposed the same surface would slot in. But
**nothing in the game currently writes durable state to a tile as a
first-class layer**, so the spec's most distinctive ideas (Scrape, ink
harvesting, layered tiles) have no substrate to stand on yet.

---

## 2. Resources — the largest divergence

| Spec | In code? | Reachable? | Reality |
|---|---|---|---|
| **Stamina** (max 6, +2/turn, skills cost 1–3) | ❌ **No such stat anywhere** | — | Skills are gated **only by cooldowns** (8–45 turns). There is no per-encounter tactical resource at all |
| **Ink pool** (max 8, +1 per 4 turns, grimoires cost 2–4) | 🟡 Different shape | ✅ Yes | `GrimoireChargePart`: **10 charges on each book**, 1 per cast, refilled by `InkVial`. A per-item resource, not a pooled regenerating one |
| Skill cooldowns 1–4 turns | ❌ | — | Shipped cooldowns are **8–45 turns**, an order of magnitude longer |

**This is the single biggest gameplay difference.** The spec's loop
assumes you cast several skills per fight and are limited by Stamina.
The shipped game assumes you cast *one* power every several turns and
are limited by cooldown. Those produce very different combat rhythms,
and the spec's Stamina economy is not a tweak — it is a new system that
would change every one of the 12 primer skills.

---

## 3. World-state model (§3–§9)

| Spec layer | In code? | Reachable? | Evidence |
|---|---|---|---|
| **Substrate** (Stone/Soil/Wood/Metal on tiles) | ❌ Not as a tile layer | — | `MaterialPart` carries `Combustibility`/`Conductivity`/`Porosity`/`Brittleness` — but **per entity**, not per tile |
| **Structure** | 🟡 Partial | ✅ | `Tree` ✅, `VineWall` ✅, `WoodenBarrel` ✅. **Missing: `Vine` (non-blocking), `StonePillar`, `MetalGrate`, oil-filled barrel** |
| **Coatings** | ✅ **Far richer than asked** | ✅ | **25 liquid definitions** incl. `water`, **`oil`** (Combustibility 90, Slippery, FlameTemp 250), `acid`, `brine`, `pitch`, `lava`, `honey`, `sap` |
| **Residues** (Embers, Ash) | ❌ Not as tile state | — | "ember"/"ash" appear only as *flavour* in `CampfirePart`, `OvenSitePart`. No residue layer, no `Embers` that ignites oil |
| **Energy** (Heat/Cold/Charge, discrete 0–2) | 🟡 Different model | ✅ | `ThermalPart.Temperature` is a **continuous float** with real thermodynamics; `ElectrifiedEffect.Charge` is a float. The spec's tidy 0–2 channels do **not** exist — the shipped model is more simulationist and less legible |
| **Clouds** | 🟡 Partial | ✅ | 7 gases (`fungal-spores` = the spec's Spores, `poison-vapor`, `sleep-vapor`, `stun-vapor`, `cryo-mist`, `confusion-vapor`, `plasma-gas`). `SteamEffect` exists as an **effect**, not a cloud. **No Smoke** |
| **Player-facing statuses** | ✅ Mostly | ✅ | Wet ✅ Burning ✅ Charged(Electrified) ✅ Frozen ✅ Rooted ✅ Spored(FungalInfection) 🟡 — **Oiled and Chilled have no creature status** |

**Notable:** the spec proposes Oil as a headline new coating. `oil`
already exists as a fully-specified liquid, and `OilSlick` / `OilSeep`
blueprints exist. What is missing is a *player ability that writes it*.

---

## 4. Reactions (§11) — exists, but keyed differently

**13 data-driven reactions ship today** in
`Content/Data/MaterialReactions/`, resolved by
`MaterialReactionResolver`:

`water_plus_fire`, `oil_plus_fire`, `fire_plus_ice`,
`lightning_plus_conductor`, `acid_plus_organic`, `fire_plus_organic`,
`fire_plus_bone`, `fire_plus_fungal`, `cold_plus_metal`,
`cold_plus_crystal`, `cold_plus_chitinous`, `fire_plus_raw_meat`,
`fire_plus_raw_starapple`

Against the spec's eight core reactions:

| Spec reaction | Shipped? | Note |
|---|---|---|
| Electrified Water | 🟡 | `lightning_plus_conductor` exists; **conduction through wet tiles does not** |
| Charged Metal | ❌ | No metal tiles to conduct through |
| Steam (Water+Heat) | ✅ | `water_plus_fire` produces `SteamEffect` |
| Freeze (Water+Cold) | 🟡 | `FrozenEffect` on creatures; **no Ice terrain, no traction rule** |
| Melt (Ice+Heat) | 🟡 | `fire_plus_ice` exists; not terrain-level |
| Ignite Oil | ✅ | `oil_plus_fire` |
| Ignite Structure | 🟡 | `fire_plus_organic` burns things; **no 3-turn burn → Ash → stump lifecycle** |
| Bloom (Spores+Water) | ❌ | Crops exist, but no spore→vine reaction |

The spec's reactions are **tile+tile**; the shipped ones are
**material+material on entities**. Same philosophy, different subject.

---

## 5. Abilities (§13–§14)

The 12 primer skills and 4 rites built in SM3–SM8 map onto the spec
surprisingly closely — **by convergence, not by copying** (they were
built before this spec arrived).

| Spec skill | Shipped analogue | Difference |
|---|---|---|
| **Douse** (Wet + push 1) | **`Hydromancy_JetBlast`** — cone 2, Wet 0.8, push 1 | Near-identical. Cone instead of single-target |
| **Static Needle** (Charged) | **`Galvanism_GroundSurge`** — line 4, 40% Electrified, push 1 | Shipped also pushes |
| **Ember Strike** (Heat 1) | **`Pyromancy_EmberSpit`** — single, Burning | Shipped applies Burning directly, not a Heat channel |
| **Rime Cut** (Cold 1) | **`Cryomancy_RimeGrip`** — Frozen, deeper if Wet | Shipped is the full status, not a Cold level |
| **Oilmark** (Oiled) | ❌ **Nothing applies Oil** | The clearest single missing skill |
| **Spore Dart** (Spored) | 🟡 gas grenades only | No targeted spore skill |
| *(no spec equivalent)* | `Backlash Coil`, `Rail Spike`, `Flame Jet`, `Backdraft`, `Drench Lob`, `Undertow`, `Glacial Wall`, `Cold Snap` | Shipped has 8 skills the spec does not |

| Spec grimoire | Shipped analogue | Difference |
|---|---|---|
| **Fulmination** (requires Wet, consume, heavy shock) | **`StormAnvilMutation`** | Shipped consumes Wet *or* Electrified *or* Frozen and scales super-linearly; spec writes `Charge 2` to the tile and lets the world resolve it |
| **Ignition Script** (consume Oiled/Heated → Heat 2) | 🟡 `RenderedSteamMutation` | Shipped wants Wet+Burning; spec wants Oiled/Heated → writes Heat |
| **Rime Seal** | ❌ | Planned as SM10 "Shattered Rime" |
| **Verdant Margin** | ❌ | No spore economy |
| **Ashen Index** (consume Burning → Ash+Smoke) | ❌ | No Ash, no Smoke |
| **Scrape** | ❌ **Entirely absent** | See §6 |
| *(no spec equivalent)* | `HangingBolt`, `ScaldingVeil` | Control-instead-of-damage, and self-targeted resonance |

**The deepest divergence in abilities:** the spec says a grimoire should
write `Charge 2` and let the reaction system decide everything. Shipped
rites **resolve their own payoff** — they call `ResonanceSystem`, get a
multiplier and rider tags, and apply the damage themselves. That is more
data-driven than hard-coded combo logic, but less so than the spec's
"emit and walk away".

---

## 6. Entirely absent — and these are the spec's best ideas

| Feature | Status |
|---|---|
| **Scrape / Reversion / Ink refund economy** (§15–§16) | ❌ **Nothing like it.** The spec's most novel mechanic: the battlefield becomes a resource you harvest. Requires tile layers to exist first |
| **Combo rewards** (§19) — Flourish / Echo / Overprint | ❌ No chain detection, no reward tier |
| **Resonance Dial** (§20) | ❌ = my planned **SM12** |
| **Inspect Mode** (§21) | ❌ `EffectDescriber` gives text, but no layered reveal |
| **Reaction Log** (§22) — expandable causal chains | ❌ `MessageLog` is flat. *However*, the `spell` diag category (SM7) already records the causal chain machine-readably — a reaction log has a data source |
| **Accessibility layer** (§23) — symbols + motion per resonance | ❌ Not started |
| **Formal resolution order** (§26) | ❌ Not formalised as 7 steps |
| **Loop guards** (§27) — ReactionID+ActionID+TileID, depth caps | ❌ No formal guard. `LootTableRegistry` has cycle detection; reactions do not |
| **Original tile snapshot** (§28) | ❌ Nothing to restore to |
| **Skill/grimoire permanent upgrades** (§17–§18) | ❌ Skill trees grant *powers*, never *modifiers to existing powers*. `Undertext`, `Marginalia`, `Ghostwriting` have no analogue |

---

## 7. What the spec would gain from what already exists

Worth stating in the other direction, because it changes the build order:

- **25 liquids with real physical properties** (conductivity,
  combustibility, adsorbence, evaporativity, slipperiness) — the spec's
  3 coatings are a subset.
- **13 data-driven reactions** already in the shape §25 recommends.
- **`ThermalPart`** is a genuine heat simulation with flame/freeze/
  brittle temperatures — richer than Heat 0–2, though harder to read.
- **A gas system** with drift, density and 7 gas types.
- **Push and pull primitives** (SM1/SM5) that already run the full
  movement pipeline, so a shoved creature *does* enter its destination
  cell.
- **The `spell` diag category** already emits `ResonanceFired` /
  `ResonanceDeclined` / `RiteCast` / `RiteRejected` — §22's Reaction Log
  and §21's Inspect Mode have a data source waiting.

---

## 8. Honest summary of the difference

**Same philosophy, different subject, different economy.**

1. **Subject:** spec writes to *tiles*; shipped writes to *creatures*.
   This is the one that decides how much of the spec is reachable
   without new foundation.
2. **Economy:** spec has Stamina + a regenerating Ink pool driving a
   fast per-turn rhythm. Shipped has long cooldowns + per-book charges,
   which is a slower, chunkier rhythm.
3. **Legibility:** spec is deliberately coarse (Heat 0–2) so players can
   reason about it. Shipped is a continuous simulation that is more
   physically honest and much harder to read — which is exactly why the
   spec's §20–§22 reading tools matter more here, not less.
4. **The harvest loop is the genuinely new idea.** Write → React →
   Transform → **Harvest** → Rewrite. Nothing in the shipped game does
   the harvest step, and it is what turns the environment from scenery
   into a resource.

**The smallest bridge:** a tile-state layer that exposes the same
`Preview`/`Spend` surface `ResonanceSystem` already has. That single
addition would make rites able to read tiles, make Scrape expressible,
and let the 25 existing liquids and 13 existing reactions participate —
without rewriting any of SM1–SM8.

# Emergent Alchemy / Brewing — Design Exploration

**Status:** 🟦 DESIGN / NOT YET IMPLEMENTED — direction confirmed
(emergent alchemy), phasing confirmed (M1 Build-Alongside → M2
Layer-On-Top), **M1 design questions LOCKED (§6)**. Ready to build M1
once weapon/spell-crafting scope (§7) is folded in.
**Branch:** `claude/rpg-crafting-system-8gbflx`
**Origin:** user — *"If this is an RPG there should be a more in-depth
crafting system. It shouldn't be tedious for the sake of false depth,
but it should be a little more creative than the cutting tinkering
system."*

> This doc is the **explain + compare** deliverable the user asked for:
> (1) crafting-system archetypes in general, (2) the chosen direction
> (emergent alchemy/brewing) fleshed out against the real CoO code
> seams, and (3) the two integration approaches — *build alongside* vs
> *layer on top* — explored with concrete examples + a recommendation.
> No production code ships until the integration approach is locked.

---

## 0. What CoO already has (verification sweep — read before designing)

| System | Where | Feel |
|---|---|---|
| **Bits tinkering** ("the cutting system") | `Assets/Scripts/Gameplay/Tinkering/*`, `Recipes_V1.json` | Disassemble item → abstract R/G/B/C "bits" → spend bits to *build* or *mod* from a known recipe. Validation-first, atomic rollback (`TinkeringService.cs`). Utilitarian: shred → currency → recipe output. |
| **Item enhancements** | `Docs/ITEM-ENHANCEMENTS.md` (322 tests) | Minerals/sigils infused into gear as event-hooking Parts (Pale-Salt vs undead, Choir-Iron vs fungal). Flavorful, but routed through the same recipe+bits flow (`mod_palesalt_infuse` etc.). |
| **Effect system** | `Assets/Scripts/Gameplay/Effects/Effect.cs`, `StatusEffectsPart.cs` | Burning, Wet, Frozen, Acidic, Electrified, Poisoned, Stoneskin, Bleeding, Charred… each a `Effect` with magnitude/duration. |
| **Tonics** | `StatusTonicPart.cs` | `EffectName` string → `CreateEffect()` dispatch → applied on drink/shatter. **This is the seam alchemy produces into.** |
| **Liquid coatings** | `Docs/LIQUID-COATING-SYSTEM-PLAN.md` (shipped) | Property-driven interaction *already exists*: wet amplifies electric, oil is combustible, honey is sticky. Per-liquid knobs (Combustibility, Conductivity, FreezeTemperature…). |
| **MaterialPart + tags** | items carry `MaterialPart{material, tags}` | The hook for "ingredient X has property Y." |

**The load-bearing insight:** CoO does *not* lack interaction depth — the
liquid system already resolves emergent element combos (wet→electric,
oil→fire). What it lacks is a **player-facing way to author those combos
through crafting.** Alchemy is the missing front-end on a substrate that
half-exists. That's why this direction is high-leverage and low-risk:
we are surfacing an existing engine, not inventing physics.

---

## 1. Crafting systems in general — the archetype map

Crafting systems differ by *where the player's creativity lives*. Five
common archetypes, with where each puts the "fun" and how each fails:

### A. Recipe-lookup (what the bits system is)
- **Loop:** gather currency/ingredients → match a known recipe → press
  craft → deterministic output.
- **Creativity lives in:** *acquisition* (finding the recipe + mats).
- **Fails as:** a checklist. Once you know the recipe it's a vending
  machine. "False depth" = a long recipe list that's really just a
  shopping UI. This is the feel the user is reacting against.

### B. Quality / affix rolling (Diablo-style craft)
- **Loop:** material grade + skill check → variable-quality output, RNG
  affixes.
- **Creativity lives in:** *gambling / optimization* (re-roll for the
  god roll).
- **Fails as:** a slot machine → grind. (This was the user's flagged-as-
  risky option; documented here for completeness, not pursued.)

### C. Modular assembly (Monster-Hunter / Dead-Cells weapon parts)
- **Loop:** pick components (head + haft + binding) → stats/abilities are
  the *sum/composition* of the parts; swap to retune.
- **Creativity lives in:** *configuration* (build-crafting).
- **Fails as:** combinatorial bloat if part count is huge; tedium if
  swapping is fiddly.

### D. Emergent / property-combination (Breath-of-the-Wild cooking,
   Noita alchemy, Qud's liquid mixing) — **the chosen direction**
- **Loop:** combine ingredients that each carry *properties*; the result
  is **computed from how those properties interact**, not looked up.
- **Creativity lives in:** *discovery + understanding the rules*. "I bet
  fire-moss + lamp-oil makes a flaming coating" — and it does, because
  the properties compose, not because an author wrote that one recipe.
- **Fails as:** opaque randomness if the rules aren't learnable, or
  tedium if you must re-discover everything constantly. The fix:
  **legible, consistent property rules + a discovery log that
  remembers.**

### E. Transformation / infusion (essence extraction)
- **Loop:** harvest essences from the world/creatures → infuse into gear.
- **Creativity lives in:** *sourcing* — crafting becomes a reason to
  fight specific enemies / visit specific biomes.
- This is adjacent to D and a natural *content source* for it (see §4).

**Why D for an RPG, not a roguelike:** in an RPG (CoO is explicitly one —
`PROJECT-IDENTITY.md`) the player keeps one character across sessions, so
**knowledge is permanent progression**. A discovery-driven system where
"I have learned that ash-cap + brine = a corrosive draught" is itself
character growth fits the genre perfectly — it's not lost on death, and
it rewards a curious long-lived character rather than a grindy run.

---

## 2. The chosen direction — Emergent Alchemy / Brewing

### 2.1 Core model

Three data concepts:

1. **Reagent** — an ingredient with tagged **properties** (each a
   `(propertyId, potency)` pair). A property is the *atom* of the system.
   - e.g. `FireMoss` → `{ heat: 2, volatile: 1 }`
   - e.g. `LampOil` → `{ combustible: 3, viscous: 1 }`
   - e.g. `GlimmerBrine` → `{ corrosive: 2, conductive: 2 }`
   - e.g. `CandyHeart` (Adventure-Time flavor) → `{ vital: 2, sweet: 1 }`
   Reagents are just items with a `ReagentPart` (sits next to the
   existing `MaterialPart`).

2. **Property → Outcome rules** — a small, *legible* rule table that maps
   property combinations to effects. This is the engine. Examples:
   - `heat + combustible` ⇒ `Burning` effect, magnitude scales with
     min(heat, combustible).
   - `corrosive` alone ⇒ `Acidic`; `corrosive + conductive` ⇒ `Acidic`
     *and* `Electrified` (a "galvanic draught").
   - `vital + sweet` ⇒ `Regeneration`; `vital + bitter` ⇒ a stronger but
     `Nauseated`-tinged heal.
   - `volatile` present ⇒ the brew can be **thrown** (shatters, applies
     to a tile/area) instead of drunk.
   These rules *reuse* the element couplings the liquid system already
   encodes — alchemy is the authoring front-end on that physics.

3. **Brew form** — what the combined output *is*, decided by the reagent
   mix + the station: a **Tonic** (drink → effect on self),
   a **Coating** (apply to weapon → on-hit effect, ties into liquid
   coatings), a **Throwable** (volatile → area effect), or a **Food**
   (longer, gentler buffs — the Adventure-Time cooking angle).

### 2.2 Why this isn't tedious (the anti-false-depth guardrails)

- **No fixed recipe list to grind.** You combine by *intent* ("I want
  fire"), reasoning about properties you've learned.
- **Discovery is permanent.** A `BrewKnowledgePart` (or extends the
  existing `BitLockerPart`/known-recipe storage) remembers every combo
  you've resolved, with a readable log: *"FireMoss + LampOil → Flask of
  Living Flame (Burning 3, throwable)."* You discover a rule once; after
  that it's one click.
- **Properties compose predictably.** Once you know `heat + combustible =
  fire`, you can predict *any* heat reagent + *any* combustible reagent.
  Knowledge transfers — that's the depth, and it's the opposite of a
  memorized recipe list.
- **Small property vocabulary, large surface.** ~12–16 properties →
  hundreds of meaningful combos without hundreds of authored recipes.

### 2.3 How it lands on existing seams (no new physics)

| Alchemy concept | Existing seam it produces into |
|---|---|
| Brew result = set of `(EffectName, magnitude, duration)` | `StatusTonicPart.CreateEffect` dispatch (`StatusTonicPart.cs:35`) already turns exactly these tuples into `Effect`s. |
| Drink a brew | `ApplyTonic` item event (already wired). |
| Coat a weapon | Liquid-coating system (shipped). |
| Throw a volatile brew | Throwable-consumables / tonic-shatter (Tier-2 path in `ELEMENTAL-TONICS.md`). |
| Reagent properties | `ReagentPart` next to `MaterialPart` + entity tags. |
| Crafting validation/atomicity | `TinkeringService` pattern (validate-first, rollback) — reuse wholesale. |
| Discovery persistence | known-recipe storage pattern on the player (mirrors `BitLockerPart.KnowsRecipe`). |

So the *new* code is mostly: `ReagentPart`, a `PropertyRuleTable`
(JSON-driven, like `Recipes_V1.json`), a `BrewingService` (mirrors
`TinkeringService`), a discovery store, and reagent content. The
**effect application, throwing, coating, and event plumbing already
exist.**

---

## 3. Integration approach A — **Build Alongside**

> Keep bits-tinkering for gear (build/mod weapons & armor). Add brewing
> as a **separate pillar** focused on *consumables* — tonics, coatings,
> throwables, foods — with its own station and its own "currency"
> (reagents, not bits).

### How it works
- A new **Alchemy station / "still"** (furniture, like the existing
  tinkering access) opens a brewing UI distinct from tinkering.
- Inputs are **reagents** (foraged herbs, monster parts, minerals,
  liquids) — *not* bits. No disassembly-into-bits step; you gather
  reagents by playing (forage nodes, butcher kills, harvest biomes).
- Output is a consumable whose effects are computed from §2.1.
- The two systems share *nothing* at runtime except the `TinkeringService`
  validation *pattern* (copied, not coupled).

### Concrete example — "Flask of Living Flame"
1. Player forages `FireMoss` `{heat:2, volatile:1}` and loots `LampOil`
   `{combustible:3, viscous:1}` from a sconce.
2. At the still, drops both in.
3. `BrewingService` unions properties → `{heat:2, combustible:3,
   volatile:1, viscous:1}` → rule `heat+combustible ⇒ Burning(mag 2)`,
   rule `volatile ⇒ throwable`.
4. Produces `BurningCoatingFlask`: throwable, shatters into a Burning
   tile (reusing liquid-coating + tonic-shatter). Discovery logged.
5. Bits-tinkering is untouched — you still tinker a Sharp mod onto your
   sword separately.

### Pros / cons
- ✅ Lowest risk; zero churn to the 322-test enhancement suite + tinker
  tests. Two clean, independently-testable pillars.
- ✅ Clear mental model for the player: "tinkering = gear, alchemy =
  consumables."
- ✅ Ships incrementally — brewing MVP can land without touching tinker
  code at all.
- ⚠️ Two crafting UIs / two resource economies to learn.
- ⚠️ Doesn't make *gear* crafting more creative — only adds a new domain.

---

## 4. Integration approach B — **Layer On Top**

> Make property-combination the **unifying crafting grammar**, and let
> bits-tinkering become one consumer of it. Reagent properties feed *both*
> consumables *and* gear modification, through one surface.

### How it works
- The **property/outcome engine from §2.1 becomes the core.** Brewing a
  tonic and modding a weapon are the same act with different *targets*:
  - target = *nothing* → produces a consumable (tonic/throwable/food).
  - target = *a weapon/armor* → the resolved properties become an
    **item modification** (an `ITinkerModification` / enhancement Part),
    i.e. brewing a "fire" property onto a sword = the Flaming mod, but
    *authored by the player's reagent choice* instead of a fixed
    `mod_flaming` recipe.
- **Bits become optional / demoted.** Reagents are the creative input;
  bits (if kept) are just a cheap fallback substrate for plain builds.
  The existing `mod_palesalt_infuse` (mineral-as-ingredient, zero bit
  cost) is *already this pattern* — Layer-On-Top generalizes it: every
  mod becomes "infuse a property-bearing reagent," not a recipe lookup.
- `TinkeringService.TryApplyModification` stays the execution/atomicity
  path; the *recipe lookup* in front of it is replaced by *property
  resolution*.

### Concrete example — same reagents, gear target
1. Player has `FireMoss` + `LampOil` (as above) and a `ShortSword`.
2. At the (now unified) workbench, selects the sword as target and drops
   the reagents.
3. Property engine resolves `heat+combustible ⇒ Burning-on-hit`.
4. Instead of a flask, it produces a **Flaming modification Part** on the
   sword (via the existing enhancement/`ITinkerModification` apply path,
   which already fires `OnEquipped` correctly per the E.5.1 audit).
5. The *same* `FireMoss+LampOil` knowledge that brews a fire flask also
   forges a flaming sword — one grammar, two targets. That's the
   "creative depth" unification.

### Pros / cons
- ✅ One cohesive, deeply creative crafting identity. *Every* craft —
  consumable or gear — flows from the same learnable property rules.
- ✅ Retroactively makes the existing mods/enhancements feel creative
  (they become emergent outcomes, not menu items).
- ✅ Naturally subsumes the mineral/sigil infusion system (it's already
  ingredient-driven).
- ⚠️ Highest integration cost: must route `TryApplyModification` through
  property resolution without breaking the 322 enhancement tests + tinker
  tests. Needs a careful compatibility shim + adversarial sweep.
- ⚠️ Risk of *over*-unifying — if "brew a sword" and "brew a potion" feel
  too samey, the distinct flavors blur. Mitigate by keeping target-
  specific output framing.

---

## 5. Recommendation (for user decision)

**Phase it: A first, then B as an evolution.** They are not exclusive —
A is the MVP that proves the property engine with zero risk to shipped
systems; B is the same engine *pointed at gear* once it's proven.

1. **Milestone 1 (Build Alongside):** ship the property/outcome engine +
   `ReagentPart` + `BrewingService` + a still + ~12 reagents + the
   discovery log, producing **consumables only**. Bits-tinkering
   untouched. This validates that property-combination feels creative and
   legible before we let it near the 322-test gear suite.
2. **Milestone 2 (Layer On Top):** once the engine is trusted, add the
   *gear target* path so the same reagents can forge mods — generalizing
   the already-ingredient-driven mineral infusion (`mod_palesalt_infuse`)
   into "any property-bearing reagent." Demote bits to an optional
   fallback substrate.

This sequencing gets a creative, non-tedious system in front of the
player fast (M1), keeps every existing test green, and earns the deeper
unification (M2) only after the core is proven — matching the
smallest-blast-radius-first methodology in `CLAUDE.md`.

### Property vocabulary — starter set (to pin in M1)

`heat · cold · combustible · conductive · corrosive · volatile · viscous
· vital · toxic · bitter · sweet · luminous · numbing · binding`
(~14 atoms → enough for hundreds of legible combos; expand by content,
not by code).

---

## 6. M1 design lockdowns (DECIDED — pin before code)

These were the open questions; now locked with rationale. Any change
after this point is a scope divergence and gets noted per `CLAUDE.md`.

### 6.1 Discovery framing → **Hinted (LOCKED)**
Reagent examine text **names its properties** (e.g. *"Fire-moss —
smells of heat; brittle and volatile."*). Combos are still discovered by
trying, but the *atoms* are legible, so a player can *reason* toward an
outcome instead of brute-forcing. Legibility is the core anti-tedium
guardrail — blind discovery degrades into a wiki-lookup chore, which is
exactly the "false depth" the user rejected. A first-time resolved combo
emits a discovery message + logs to the knowledge store.

### 6.2 Station vs anywhere → **Station for tonics/coatings, field-brew for foods (LOCKED)**
A **still/alchemy bench** (furniture, mirrors the tinkering access seam)
is required for tonics, coatings, and throwables — the "real" brews.
**Simple foods** (single- or dual-reagent, gentle buffs) can be made
anywhere from the inventory, so the cozy Adventure-Time cooking angle has
no friction. Rationale: gates the powerful outputs behind a place you
return to (gives the world texture) without taxing the low-stakes ones.

### 6.3 Failure outcomes → **Legible mishap, never silent (LOCKED)**
A contradictory or unresolved mix produces **"inert sludge"** (a junk
item) by default, and a *small, telegraphed* mishap only when a
`volatile` property is present with no stabilizing partner (e.g. minor
self-damage + a "the flask cracks!" message). Never a silent fizzle —
the player must always learn *why* it failed. The mishap is capped and
non-lethal (RPG, not roguelike — no run-ending punishment for
experimenting).

### 6.4 Quantity / potency → **Property-set + single potency tier (LOCKED)**
The **set** of properties determines *which* effects fire. Potency
(effect magnitude) comes from the **highest-potency contributing
reagent**, not the count — so dropping 3× FireMoss does **not** out-scale
1× FireMoss + a better heat reagent. This kills stack-grinding dead: you
improve a brew by finding *better reagents*, not by hoarding quantity. A
later content tier may add a `concentrate` station upgrade for +1 tier,
but base M1 is count-insensitive.

### 6.5 Reagent acquisition → **Forage / butcher / harvest, no bit-disassembly (LOCKED)**
Reagents come from *playing the world*: forage nodes (plants), butchering
creature kills (monster parts), harvesting biome features (brine pools,
glow-caves), and looting. **No** "disassemble item → reagent" step — that
would re-import the cutting-system feel we're moving away from. Reagents
are found, not shredded.

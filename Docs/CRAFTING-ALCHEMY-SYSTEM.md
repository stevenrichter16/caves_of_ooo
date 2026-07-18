# Emergent Alchemy / Brewing — Design Exploration

**Status:** 🟨 M1 CODE-COMPLETE (unverified) — M1.1 resolver + M1.2
brewing service/parts/knowledge/diag + M1.3 command/still/reagent
catalog all **authored** (§8); M1.1 cold-eye review run with 5 findings
fixed/pinned + plan critique (§9). ⚠️ **97 tests authored, NONE yet
run** — the user will run the EditMode suite in Unity later; treat any
failure there as a real finding. M1.4 added the adversarial sweep (which
caught + fixed the thrown-brew gate bug) and Food content. Remaining M1
polish: reagent world placement (needs a design decision — no spawn-table
system exists), brewing UI panel. **M3-L1 (modular weapon forging) now in
progress** — sequenced ahead of M2 because M2 touches the enhancement
suite's runtime seams, the riskiest thing to modify without a test
runner; M3-L1 is additive-only (§8 M3 log).
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
(effect magnitude) is the **max potency among the rule's required
properties** in the merged profile (precisely: profiles merge by MAX per
property across reagents, then each fired rule reads the max over its
own RequireAll set) — never a count or a sum. Dropping 3× FireMoss does
**not** out-scale 1× FireMoss + a better heat reagent. *(Wording
tightened in the M1.1 cold-eye pass — finding F3 — to match the shipped
`BrewResolver.ComputeMagnitude` exactly.)* This kills stack-grinding dead: you
improve a brew by finding *better reagents*, not by hoarding quantity. A
later content tier may add a `concentrate` station upgrade for +1 tier,
but base M1 is count-insensitive.

### 6.5 Reagent acquisition → **Forage / butcher / harvest, no bit-disassembly (LOCKED)**
Reagents come from *playing the world*: forage nodes (plants), butchering
creature kills (monster parts), harvesting biome features (brine pools,
glow-caves), and looting. **No** "disassemble item → reagent" step — that
would re-import the cutting-system feel we're moving away from. Reagents
are found, not shredded.

---

## 7. Sibling pillars — Weapon crafting & Spell crafting

> The user's follow-up: *"besides alchemy how can we make weapon and
> spell crafting unique and deep and creative?"* Confirmed scope:
> weapon = **all three layers**, spell = **full grammar**, **design
> only** (no code yet). The governing principle: **each pillar needs its
> own distinct creative *verb*** so the three don't collapse into "merge
> buckets of stuff" (which would be the false depth we're avoiding).

| Pillar | Creative verb | Feel | Reuses |
|---|---|---|---|
| **Alchemy** | **Combine** properties | Chemistry — fuzzy, organic | Effect system, liquid physics |
| **Weaponcraft** | **Assemble + temper** | Engineering — physical, persistent, historied | `MeleeWeaponPart`, `IItemEnhancement`, liquid quench |
| **Spellcraft** | **Compose** a grammar | Programming — abstract, structural, executable | `BaseMutation`, `ActivatedAbilitiesPart`, `MutationDamageHelpers` |

The three interlock through one **material economy** (§7.3) without
sharing a mechanic, so each stays distinct.

---

### 7.1 Weapon crafting — Modular forging + tempering + saga

Three stacked layers. The seams below are all real (from the
weapon/combat system map).

#### Layer 1 — Modular structure (the *assemble* verb)
A weapon is forged from **components**, each contributing part of the
final `MeleeWeaponPart` plus one quirk:

| Component | Drives (`MeleeWeaponPart` field) | Quirk slot |
|---|---|---|
| **Blade / head** | `BaseDamage` dice + `Attributes` damage-class (Cutting/Piercing/Bludgeoning) | primary on-hit (`OnHitEffectsRaw` entry) |
| **Haft / grip** | `MaxStrengthBonus`, hands, an implied speed/reach tag | handling quirk |
| **Binding / edge** | `PenBonus`, `HitBonus` | an `IItemEnhancement` (e.g. Serrated) |

- A new **`WeaponAssemblyPart`** records the installed component
  blueprints so the weapon can be **re-forged** — swap the blade, keep
  the haft. Your favorite sword *evolves* instead of being replaced
  (RPG identity, not loot churn — `PROJECT-IDENTITY.md`).
- Assembling = compute `MeleeWeaponPart` fields from the component set +
  attach each component's quirk (on-hit spec / enhancement Part). The
  combat path (`CombatSystem.PerformSingleAttack`) already consumes all
  of these, so **no combat code changes** — forging just *writes* the
  fields the engine already reads.
- Components themselves are found/looted/alchemy-made → crafting becomes
  a reason to chase *better parts*, not to grind identical mats.

#### Layer 2 — Tempering / quench (the *process* verb + the alchemy bridge)
A **temper** is a process applied to an assembled weapon at the forge,
using a **quench medium** — and the quench media *are alchemy outputs*
(essences/coatings) + world liquids (reuses the shipped liquid system's
per-liquid knobs: FlameTemperature, FreezeTemperature, Conductivity…):

| Quench medium | Gain | Trade-off (the depth) |
|---|---|---|
| Frost-brine essence | `+Cold` on-hit (`OnHitEffectsRaw: Frozen`) | `Brittle` tag → lower weapon Hitpoints max |
| Lava / ember essence | `+Heat` on-hit (Burning) | heavier → handling penalty |
| Galvanic draught | `+Electric` on-hit | needs a charge source to keep proccing |

- Tempering writes to `OnHitEffectsRaw` / attaches an `IItemEnhancement`
  and adjusts stats — **every effect it produces already has a runtime
  consumer.** Trade-offs are real stat deltas, not flavor text.
- **Folding / pattern-welding:** repeated tempers stack with *diminishing
  returns* (each fold adds a smaller bonus) + a signature render string.
  This is the "deep but not grindy" knob — folding has a soft cap.
- This is the concrete **alchemy → weaponcraft bridge**: a brewed
  essence is the quench medium. The two pillars touch through *material*,
  not through a shared mechanic.

#### Layer 3 — The weapon remembers (the *saga* hook — the novel part)
A new **`WeaponSagaPart`** accumulates history via combat events the
engine already fires:
- Increments tag-keyed kill counters off the `Died`/`OnAttackerHit`
  path (the same hooks `EnhancementSerrated.OnAttackerHit` uses).
- Counts temperings, records the forger's name, the first-blood zone.
- At **thresholds**, a **latent property awakens**: a blade that has
  slain 50 `Fungal` things *wants* to become anti-fungal → auto-attaches
  the matching `IItemEnhancement` (reusing the enhancement apply path,
  which fires `OnEquipped` correctly per the E.5.1 audit).
- Persists via save/load reflection (public fields, like `RentalPart` /
  `MaterialPart`). This is the part Qud does **not** have — a weapon with
  a saga is uniquely an RPG-with-a-persistent-character idea.

**Diag:** `category=craft`, kinds `WeaponForged` / `WeaponTempered` /
`SagaAwakened`, payload = components, temper history, awakened property.

---

### 7.2 Spell crafting — Rune composition (the *compose* verb)

A spell is built like a **sentence**: `[Form] + [Essence] + [Modifiers]`.
This maps almost 1:1 onto the existing mutation/ability infra — the win
is replacing **one C# class per spell** with **one data-driven
`ComposedSpellMutation`** parameterized by a rune composition.

| Rune kind | Picks | Maps onto (real seam) |
|---|---|---|
| **Form** (delivery/shape) | bolt · beam · nova · touch · aura · ward · summon | `ActivatedAbilitiesPart.AddAbility(..., targetingMode, range)` + a Cast strategy (generalizes `DirectionalProjectileMutationBase.Cast`) |
| **Essence** (the verb) | fire · frost · shock · acid · light · vital(heal) · force · drain · bind | `DamageDice` + `ElementAttribute` + the on-hit `ApplyOnHitEffect` (reuses the `Effect` system: Burning/Frozen/Acidic…) |
| **Modifier** (inflection) | amplify · extend · chain · pierce · split · **trigger(condition)** | wraps the cast: amplify→damage/magnitude, extend→duration, chain→re-target adjacent, trigger→deferred cast on `BeforeTakeDamage`/turn events |

**How composition materializes:**
1. Player **learns runes** from the world (spellbooks, defeating casters,
   ley sites) into a `RuneKnowledgePart` (mirrors `BitLockerPart.Knows…`).
2. At an inscription seam, composes a spell from known runes → produces a
   **`ComposedSpellMutation`** instance (a single `BaseMutation` subclass
   that reads its composition data instead of hard-coding behavior).
3. It self-registers an activated ability via `AddMyActivatedAbility(...)`
   — so it slots into the existing cast pipeline (`SkillsPart`
   command routing, cooldown, FX) with **zero new casting code**.
4. **Cost & cooldown derive from the composition** — more/stronger runes
   → higher `CooldownTurns` + SP cost. This is the natural balance lever
   and the anti-spam guardrail (you can't stack five modifiers for free).

**Worked examples (same small vocabulary, huge space):**
- `Bolt + Fire + Chain` → a firebolt that arcs to an adjacent enemy
  (Chain modifier re-invokes the essence on a neighbor).
- `Ward + Frost + Trigger(on-hit)` → a frost-retaliation shield (Trigger
  defers the Frost essence onto the caster's `BeforeTakeDamage`).
- `Nova + Acid + Amplify` → a corrosive burst, bigger magnitude, shorter
  range — composition-priced higher.

**Relationship to the existing magic trees** (`MAGIC-SKILLS-DESIGN.md`):
the 8 trees (Pyromancy, Cryomancy…) stay as the **passive mastery /
modifier layer**. `SpellcraftSkill.OnGetSpellDamageModifier` already
sums across owned skills — composed spells flow through
`MutationDamageHelpers.ApplySpellDamage`, so tree bonuses apply to
*crafted* spells automatically. Existing actives
(`Spellcraft_Empower`/`ArcaneSurge`/`LeyTap`) become composition boosters
rather than fixed spells. **Composition = what a spell *is*; trees = how
good you are at it.** No conflict, clean layering.

**Diag:** `category=spellcraft`, kinds `RuneLearned` / `SpellInscribed` /
`SpellCast`, payload = rune composition, derived cost, derived cooldown.

---

### 7.3 The interlocking material economy (keeps pillars distinct)

```
                 ┌────────────────────────────┐
   forage /      │        ALCHEMY             │
   butcher /     │  combine reagent           │
   harvest  ───▶ │  PROPERTIES → brews        │
                 └──────┬──────────┬──────────┘
                        │          │
         quench media   │          │  distilled rune-ink /
         (essences) ────┘          └──── mana-reagents
                        │                       │
                        ▼                       ▼
              ┌──────────────────┐    ┌──────────────────┐
              │   WEAPONCRAFT    │    │   SPELLCRAFT     │
              │ assemble+temper  │    │ compose grammar  │
              │  +saga (gear)    │    │  (abilities)     │
              └──────────────────┘    └──────────────────┘
              also sockets stones/sigils (shipped enhancement system)
```

Alchemy is the **chemistry feedstock**; weaponcraft the **physical
output**; spellcraft the **abstract output**. Three creative verbs, one
shared material flow — depth from interlock, not from a single bloated
mechanic.

---

### 7.4 Suggested build order (still design-only)

1. **M1 — Alchemy property engine** (already locked, §1–§6). Proves the
   property/discovery core with zero risk to shipped suites. *Build this
   first.*
2. **M2 — Alchemy → Layer-On-Top** (gear-target path; §4).
3. **M3 — Weaponcraft L1+L2** (modular `WeaponAssemblyPart` + tempering;
   consumes M1 essences as quench).
4. **M4 — Weaponcraft L3** (`WeaponSagaPart` latent-awakening).
5. **M5 — Spellcraft grammar** (`ComposedSpellMutation` + runes +
   inscription); biggest magic-system change, so last + its own
   verification sweep against `MAGIC-SKILLS-DESIGN.md`.

Each milestone is independently shippable, TDD per `CLAUDE.md`, emits its
own diag category, and gets the cold-eye + adversarial gates. Nothing
here is built until you greenlight a milestone.

---

## 8. Implementation log

### M1.1 — Pure brew-resolution core ✅ written (⚠️ unverified in this env)

The dependency-root sub-milestone: a deterministic, side-effect-free
resolver that turns a set of reagents into a brew outcome. No inventory,
no zone, no RNG — purely `reagents → BrewResult`. Everything else in M1
(the service, discovery store, station, content) builds on this.

**Files (NEW):**
- `Assets/Scripts/Gameplay/Alchemy/BrewProperty.cs` — `BrewPropertyAmount`
  (one (property, potency) pair) + `BrewProperties` vocabulary constants.
- `Assets/Scripts/Gameplay/Alchemy/BrewRule.cs` — `[Serializable]` rule DTO
  (RequireAll / ForbidAny / Effect / Form / Priority / MagnitudeScale).
- `Assets/Scripts/Gameplay/Alchemy/BrewResult.cs` — `BrewOutcomeKind`
  (Invalid / InertSludge / Mishap / Brew) + `BrewEffect` + `BrewResult`.
- `Assets/Scripts/Gameplay/Alchemy/BrewRuleRegistry.cs` — static JSON
  registry mirroring `TinkerRecipeRegistry`
  (EnsureInitialized / InitializeFromJson / ResetForTests).
- `Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs` — the pure resolver.
- `Assets/Resources/Content/Data/Alchemy/BrewRules.json` — 7 starter
  rules using only effects the existing `StatusTonicPart.CreateEffect`
  dispatch already supports (Burning / Frozen / Acidic / Electrified /
  Poison / Stoneskin / Wet).
- `Assets/Tests/EditMode/Gameplay/Alchemy/BrewResolverTests.cs` — **17**
  tests, each positive assertion paired with a §3.4 counter-check.
  *(Record correction: the M1.1 commit body claimed 18; the actual count
  in that commit was 17 — cold-eye finding F2. The M1.2 pass added 3
  more, bringing this file to 20.)*

**Locked-decision conformance:**
- §6.4 potency = **MAX** across reagents, never summed →
  `Potency_DoesNotScaleWithDuplicateReagents` pins it.
- §6.3 failure never silent → `Mishap` (volatile, unstabilized) vs
  `InertSludge` (no reaction) are distinct, both carry a `Reason`.
- Combinatorial emergence → `CorrosivePlusConductive_ProducesBothAcidAndShock`
  (two rules fire on one mix = a galvanic draught).

**Honesty bound (CLAUDE.md §6.3):** this code was authored in a remote
container with **no Unity editor / dotnet** — so RED→GREEN was **NOT
observed here**. The tests are written to fail-without / pass-with the
implementation, but the TDD cadence (confirm RED, confirm GREEN) and the
full EditMode suite must be run in Unity before this is considered green.
`.meta` files are not committed (Unity generates them on first import).

**Deferred to M1.2+:** `ReagentPart` (the in-world carrier), `BrewingService`
(validate inventory + consume atomically + create item + emit
`category=alchemy` diag records, mirroring `TinkeringService`), the
discovery/knowledge store, the still furniture + UI, and reagent content.

### M1.2 — Brewing service + world integration ✅ written (⚠️ unverified in this env)

Everything between the pure resolver and a playable command surface.
Shipped in the same pass as the M1.1 cold-eye review (§9), whose F1/F4
fixes landed here.

**Files (NEW):**
- `Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs` — the name→Effect
  switch extracted verbatim from `StatusTonicPart.CreateEffect` so tonics
  AND brews share ONE canonical dispatch table (they can never drift).
- `Assets/Scripts/Gameplay/Alchemy/ReagentPart.cs` — blueprint-authorable
  `PropertiesRaw` ("heat:2, volatile:1") + snapshot-cached parse
  (mirrors `MeleeWeaponPart.OnHitEffectsRaw`), plus `FlavorText` for §6.1
  hinted discovery. EntityFactory resolves blueprint part name "Reagent"
  automatically (name + "Part" convention, `EntityFactory.cs:230-249`).
- `Assets/Scripts/Gameplay/Alchemy/BrewItemPart.cs` — the multi-effect
  consumable carrier; listens for the same `ApplyTonic` event
  `StatusTonicPart` uses, applies every entry via `TonicEffectFactory`.
  A galvanic draught is `EffectsRaw = "Acidic:2;Electrified:1"`.
- `Assets/Scripts/Gameplay/Alchemy/BrewKnowledgePart.cs` — permanent
  discovery log **keyed by rule ID** (learn the RULE, not the reagent
  pair); storage + restore shim mirror `BitLockerPart`.
- `Assets/Scripts/Gameplay/Alchemy/BrewingService.cs` — validate-first,
  atomic multi-reagent consume with rollback, output creation
  (`BrewedTonic` / `InertSludge` blueprints), "Healing" → instant
  `TonicPart.Healing` dice mapping, discovery recording, and full
  `category=alchemy` diag coverage (`BrewResolved` / `BrewRejected` /
  `BrewDiscovered`) — the instrumentation tinkering never got.
- Tests: `ReagentPartTests` (8) · `BrewItemPartTests` (7) ·
  `BrewKnowledgePartTests` (5) · `TonicEffectFactoryTests` (4) ·
  `BrewingServiceTests` (13) · +3 in `BrewResolverTests` (F1/F4 pins +
  RuleId). **M1 cumulative: 57 authored tests** (all ⚠️ unverified here —
  Unity run required).

**Files (MOD):**
- `StatusTonicPart.cs` — `CreateEffect` now delegates to
  `TonicEffectFactory` (behavior-preserving; existing tonic suites are
  the regression net).
- `BrewProperty.cs` — shared `ParseList` grammar (drops malformed
  potency entries rather than treating `int.TryParse`'s 0 as a value —
  the CLAUDE.md pitfall).
- `BrewResult.cs` / `BrewResolver.cs` — `BrewEffect.RuleId` provenance;
  F4 tiebreak fix (first-listed rule wins).
- `BrewRuleRegistry.cs` — F1 duplicate-ID fix (last-wins in BOTH access
  paths).
- `Diag.cs` — "alchemy" added to `DefaultOnCategories`.
- `Objects.json` — `BrewedTonic` (inherits TonicItem, Drink=true) +
  `InertSludge` (inherits Item) blueprints.
- `BrewRules.json` — `brew_mending` (vital → Healing, vetoed by toxic).

**Service contract:** `TryBrew` returns TRUE when the brewing ACT
completed — including Mishap (reagents consumed, no item) and
InertSludge (junk item) outcomes; the caller reads `BrewResult.Kind`.
FALSE = validation/execution failure with NOTHING consumed. Mishap
self-damage is applied at the command/UI layer where zone context lives
(M1.3), not in the service.

**Still deferred to M1.3:** the player-facing command/UI surface (still
furniture + inventory command mirroring `CraftFromRecipeCommand`),
production reagent blueprints + forage/butcher placement, mishap
self-damage at the command layer, throwable-brew wiring, per-effect
potency→duration mapping, and the save/load round-trip test through
`SaveGraphSerializer` (needs Unity to run — flagged 🧪).

### M1.3 — Player-facing surface: command, still, reagent catalog ✅ written (⚠️ unverified in this env)

**Files (NEW):**
- `Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs` — furniture marker
  part (ChairPart/BedPart shape) + static `IsNearStill(actor, zone)`
  3×3-box adjacency check (`Zone.GetEntityPosition` + `GetCell` +
  `Cell.Objects`).
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs`
  — mirrors `CraftFromRecipeCommand`; runs through
  `InventorySystem.ExecuteCommand`. Owns the two rules that need zone
  context: **still gating** (§6.2 — the mix is resolved *purely* at
  Validate time; any outcome except a pure-Food brew requires an adjacent
  still) and **mishap self-damage** (§6.3 — `MishapDamageMax = 2` via
  `CombatSystem.ApplyDamage`, clamped so Hitpoints can never drop below
  1: experimenting never kills outright).
- 13 reagent blueprints + `ReagentItem` base + `AlchemyStill` furniture
  in `Objects.json`. Profiles deliberately overlap (§9.2 C1 mitigation):
  FireMoss+LampOil → burning coating; FireMoss+BlastcapSpore → burning
  **throwable** (volatile joins in); BlastcapSpore alone → mishap
  (volatile+combustible but no heat — "do not shake, do not warm");
  GlimmerBrine alone → galvanic draught (acid+shock); GlacierSalt →
  Frozen+Stoneskin coating (two rules fire); CandyHeartRoot+VenomGland →
  mending vetoed by toxic → poison tonic. Blueprint inheritance merges
  parent tags/parts (`BlueprintLoader.cs:158-204`), so reagents inherit
  Physics/Commerce/tags from `ReagentItem` and override only
  Render + Reagent params. FlavorText names each reagent's properties
  in-fiction per §6.1 hinted discovery.
- `Assets/Tests/EditMode/Gameplay/Alchemy/BrewReagentsCommandTests.cs` —
  12 tests: still gating (adjacent/diagonal/same-cell pass; absent/
  distance-2 reject with nothing consumed), Food-form field-brewing
  allowed, mishap damage applied + capped + never-below-1-HP +
  not-applied-on-success counter-check, validation plumbing, and the
  `IsNearStill` helper directly.

**M1 cumulative: 69 authored tests** (all ⚠️ unrun — user will run the
EditMode suite in Unity later, per explicit instruction).

**Still open after M1.3:** world *placement* of reagents (forage nodes /
loot tables / merchant stock — content-pass work; the blueprints exist
and merchants can price them via Commerce), a brewing UI panel (the
command surface is UI-ready; the panel itself is presentation work),
throwable-brew shatter wiring, Food rules content, per-effect
potency→duration mapping, save/load round-trip test (Unity-gated 🧪).

### M1.4 — Adversarial sweep + throwable-brew fix + Food content ✅ written (⚠️ unverified in this env)

The CLAUDE.md adversarial-sweep gate (alchemy hits 4+ taxonomy
surfaces: state atomicity, parser, stacking, diag dispatch contracts).

**🔴→fixed: the sweep caught a REAL latent bug while being designed.**
`TonicPart.HasThrowablePayload` (the gate `ThrowItemCommand` uses to
decide shatter-vs-inert-landing) checked Healing/StatBoost/
CureTonicPart/StatusTonicPart — but not `BrewItemPart`. A volatile
"Throwable"-form brew **landed like an inert rock** instead of
shattering into its 3×3 AoE. One-line fix in `TonicPart.cs` (add the
BrewItemPart check); the whole downstream chain
(`ApplyTonicAoe` → `tonic.ApplyTo` → `ApplyTonic` event →
`BrewItemPart.HandleEvent`) was verified already-correct by read — only
the gate was missing. Pinned by 3 tests (positive + bare-tonic
counter-check + healing-path regression guard).

**Files:**
- MOD `TonicPart.cs` — the one-line throwable gate fix.
- NEW `BrewingAdversarialTests.cs` — **28 tests** across: parser
  malformed inputs (double-colon, ±signs, unicode, 2×10⁹ potency),
  resolver rule-table degenerates (missing fields, unknown properties,
  MagnitudeScale 0 / fractional rounding, case-insensitive veto, empty
  table), atomicity rollback shapes (stack+plain restored together on
  sludge-blueprint failure; stack-of-1 boundary; empty-property reagent
  = sludge-not-invalid), cross-instance same-blueprint reagents, diag
  contract invariants (success≠Rejected, rejection≠Resolved, mishap
  outcome in payload, channel-off changes nothing), throwable pins,
  discovery abuse (pre-existing knowledge merged not clobbered;
  sludge/mishap teach nothing), live EffectsRaw mutation, and **content
  integrity pins** (all 13 production reagents parse to the known
  14-atom vocabulary + carry FlavorText; all production rule forms and
  properties are valid — content typos become test failures instead of
  silent runtime no-ops).
- MOD `BrewRules.json` — `brew_snack` (sweet → Healing, **Form Food**,
  vetoed by toxic): field-brewing is now real content. EmberFruit alone
  → field-brewable snack; CandyHeartRoot (vital+sweet) → mending tonic
  (higher-priority rule wins the form → still required), healing potency
  maxed across both rules.

**M1 cumulative: 97 authored tests, 0 run** (user instruction: suite
runs later). World placement of reagents investigated and consciously
deferred again: CoO has NO loot/spawn-table system (minerals arrive via
scenarios/merchants), so placement means either merchant stock wiring or
a new forage-node system — a design decision worth its own sub-milestone
rather than an invented-blind spawn hack.

### M3-L1 — Modular weapon forging ✅ written (⚠️ unverified in this env)

First slice of the weapon pillar (§7.1 Layer 1). **Sequencing note
(scope divergence from §7.4):** built before M2 (alchemy gear-target)
because M2 must route through the 322-test enhancement suite's runtime
seams — the riskiest thing to modify with no test runner — while M3-L1
is purely additive: new parts + a service that WRITES `MeleeWeaponPart`
fields the combat path already READS. Zero combat-code changes.

**Files (NEW):**
- `Assets/Scripts/Gameplay/Weaponcraft/WeaponComponentPart.cs` —
  blueprint-authorable component: Slot (Blade/Haft/Binding) +
  contributions (BaseDamage, Pen/Hit bonuses, MaxStrengthBonus,
  Attributes, OnHitEffectSpec quirk, NameFragment).
- `Assets/Scripts/Gameplay/Weaponcraft/WeaponAssemblyPart.cs` — records
  the installed component blueprints (plain string fields → save-layer
  friendly) so the weapon can be **re-forged**: the RPG-identity hook
  where a favorite weapon evolves instead of being replaced.
- `Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs` —
  TryForge (validate → consume 3 components w/ rollback ledger → create
  `ForgedWeapon` → compute stats → record assembly) and TryReforge
  (swap ONE component; the displaced one is re-created from its
  blueprint and returned — components are stateless; stats recomputed
  deterministically so forge(A)→reforge(B)→reforge(A) ≡ forge(A)).
  Combination math: blade drives dice; bonuses SUM; strength cap is
  MAX; attributes union de-duplicated; quirks concatenate into
  `OnHitEffectsRaw`; name = haft + binding + blade fragments
  ("oak-hafted serrated steel blade"). Diag `category=craft`:
  WeaponForged / WeaponReforged / ForgeRejected (added to
  `DefaultOnCategories`).
- Content: `ForgedWeapon` base (inherits MeleeWeapon) +
  `WeaponComponentItem` base + 6 components (SteelBlade/IronSpike
  blades, Oak/Willow hafts, Leather/SerratedEdge bindings — the
  serrated kit carries a `Bleeding,20,1d2,15,0` on-hit quirk).
- `WeaponForgingServiceTests.cs` — **17 tests**: assembly math (dice /
  sum / max / union-dedupe / quirk / naming), validation (wrong slot,
  non-component, unowned — nothing consumed), 3-component rollback,
  re-forge swap + determinism + non-forged-loot rejection, diag pins,
  production content pins.

**Cumulative authored: 114 tests (97 alchemy + 17 weaponcraft), 0 run.**

**Deferred to M3-L2:** forge furniture + inventory command (mirror the
still/brew-command pattern), tempering/quench (consumes M1 essences —
the alchemy bridge), component drop/merchant placement, and the
adversarial sweep for weaponcraft (gate applies: atomicity + parser
(OnHitEffectSpec) + cross-instance surfaces).

### M3-L2 — Tempering: the alchemy→weaponcraft bridge ✅ written (⚠️ unverified in this env)

The §7.1 Layer-2 quench, plus its safety coupling and the weaponcraft
adversarial sweep.

**Files (NEW):**
- `WeaponTemperPart.cs` — temper state (count, HP penalty applied,
  specs) in plain public fields for save-layer reflection.
- `WeaponTemperingService.cs` — **a brewed COATING is the quench
  medium** (BrewItemPart, Form "Coating" — Tonic/Throwable rejected):
  quenching consumes the coating and writes its effects onto the weapon
  as on-hit specs (`Burning:2` → `"Burning,40,,0,2"` — potency scales
  chance 20+10·p capped 50, and magnitude) in the exact
  `OnHitEffectsRaw` grammar combat already consumes. **Real trade-off:**
  each temper fatigues the metal (Hitpoints max −2, floored at 1, and
  the RECORDED penalty equals what was actually applied so the refund
  can't over-heal); the metal holds at most **2 tempers**. Name gains a
  quench prefix ("flame-quenched oak-hafted serrated steel blade").
  Diag: `WeaponTempered` / `TemperRejected`.
- `WeaponcraftAdversarialTests.cs` — **14 tests** across: temper
  mechanics + cap + form gating + empty-coating + self-quench gate +
  stacked-coating consumption + HP floor boundary; **temper↔reforge
  cross-system consistency** (see below); forge stacking; the
  swap-loop anti-dupe probe (reforge out + immediately back in must not
  mint components); diag contracts incl. channel-off.

**Files (MOD):**
- `WeaponForgingService.TryReforge` now calls
  `WeaponTemperingService.ClearTemper` after recompute — **the
  consistency coupling**: recompute already wipes temper on-hit specs +
  the quench name prefix (stats rebuild from components), so without the
  melt, the HP penalty would linger on a weapon whose specs no longer
  justify it. Fiction: *re-forging melts the temper away.* Re-tempering
  after a re-forge starts fresh from zero (pinned).

**Cumulative authored: 128 tests (97 alchemy + 31 weaponcraft), 0 run.**

**Scope divergence from the M3-L2 sketch:** forge furniture + the
forge/temper inventory commands are deferred to M3-L3 (they're
mechanical mirrors of the still/brew-command pattern and belong with
the UI pass); shipping furniture with no command consuming it would be
dead content. Component world-placement shares the reagent-placement
design decision (no spawn-table system exists).

---

## 9. M1.1 cold-eye review + critical plan analysis (2026-07-18)

Run per CLAUDE.md §Post-implementation cold-eye review, with extra
weight because M1.1 was authored in an environment with **no Unity/test
runner** — code that was never run deserves a harsher read.

### 9.1 Code findings (all fixed in M1.2 unless noted)

**🟡 F1 — Registry duplicate-ID divergence.**
`BrewRuleRegistry.LoadFromJson` kept the FIRST instance of a
duplicate-ID rule in `RulesInOrder` while `RulesById` stored the LAST —
`GetAllRules()` (the resolver's path) and `TryGetRule()` would disagree
about what the rule is. **Fixed:** in-place replacement → last-wins in
both paths; pinned by
`Registry_DuplicateRuleId_LastDefinitionWins_InBothAccessPaths`.

**🟡 F2 — Test-count honesty slip.**
The M1.1 commit body claimed "Tests: +18"; the file contained **17**.
The pushed commit can't be amended; the record is corrected in §8 and
here. Process note: count test methods with a grep before writing the
commit body, don't recall the number.

**🔵 F3 — Doc-vs-impl drift on §6.4 potency wording** ("highest-potency
contributing reagent" vs the implemented max-over-required-properties).
**Fixed:** §6.4 rewritten to match `ComputeMagnitude` exactly.

**🔵 F4 — Form tiebreak surprise.**
`>=` in the form-priority scan made the LAST-listed rule win priority
ties — nothing an author reading the JSON top-down would predict.
**Fixed:** strict `>`; file order = precedence order; pinned by
`Form_EqualPriority_FirstListedRuleWins`.

**🧪 F5 — Unpinned effect-name seam.**
Nothing verified that the effect names brew rules emit are actually
creatable by the tonic dispatch — a typo'd rule would silently produce
nothing. **Closed structurally** (TonicEffectFactory extraction = one
canonical table) **and pinned** by
`Production_BrewRuleEffectNames_AreAllDispatchable`.

### 9.2 Plan-level critique (the honest read)

**C1 — The "emergence" is currently thinner than the pitch.** With
mostly single-property rules, 8 rules ≈ 8 recipes wearing a trench coat.
What makes it *feel* emergent is (a) multi-property rules
(`heat+combustible`), (b) multi-property REAGENTS whose combinations
produce non-obvious unions, (c) veto interactions (heat spoils frost,
toxic spoils mending), and (d) multi-rule firings (galvanic draught).
The engine supports all four; **the content burden is real and lands on
M1.3** — it must ship 12+ reagents with overlapping 2-property profiles,
or the system plays like a recipe list. This is the single biggest risk
to the design's promise.

**C2 — Five vocabulary properties are dead ends in M1.** `numbing`,
`bitter`, `sweet`, `luminous`, `binding`* have no (or only placeholder)
rule outcomes because the effect layer they need doesn't exist yet
(no Nauseated-tier debuff mapping, no light-radius consumable, no
food-buff effect). (*binding → Stoneskin shipped; the other four are
genuinely dead.) Honest choice made: keep them in the vocabulary as
**documented reserved atoms** — they parse, they merge, they match no
rule → inert sludge, which is correct forward-compatible behavior — and
M1.3+ adds their outcomes. The alternative (trimming the vocabulary)
would churn reagent content twice.

**C3 — Discovery is keyed by rule, pinned now.** A discovery model keyed
by reagent-pair would make knowledge non-transferable and grindy. Locked
in M1.2: `BrewKnowledgePart` stores RULE IDs; `BrewEffect.RuleId`
carries provenance. This is now architecture, not intention.

**C4 — Potency→duration mapping is a stub.** Potency feeds the
magnitude slot only; duration-style effects (Poisoned, Stoneskin) get
defaults regardless of potency. Documented in `BrewItemPart`; M1.3
refines per-effect. Not a bug — a declared simplification.

**C5 — No progression gate on brewing yet.** Any reagents, anywhere,
full-strength output. For an RPG this is thin: M1's implicit gate is
world placement (better reagents live in harder biomes), which is
content, not code. An Alchemy skill gate (mirroring how skills gate
tinker tiers) is the natural M2+ layer — flagged as an open design
question, deliberately NOT built now.

**C6 — Existing-system gap surfaced: tinkering has ZERO diag
instrumentation.** `TinkeringService` emits only MessageLog lines — a
standing violation of the CLAUDE.md observability rule, discovered while
mirroring it. Alchemy shipped fully instrumented
(`BrewResolved`/`BrewRejected`/`BrewDiscovered`); backfilling
`category=tinker` records into TinkeringService is a good standalone
fix-branch candidate. **Not done here** — out of this feature's blast
radius.

**C7 — The unverified-authoring risk is concentrated, not eliminated.**
Everything here compiles against APIs read from source (`Diag.Record`,
`DiagQuery.Filter`, `EntityFactory.LoadBlueprints`, `TonicPart.ApplyTo`
event params, `StatusEffectsPart.HasEffect<T>`), and test fixtures
mirror shipped suites line-for-line. But until the Unity EditMode run
happens, RED→GREEN discipline is suspended and every "pinned" claim in
§9.1 is provisional. **First action on a Unity-connected session: run
the full suite; treat any failure as a real finding, not test noise.**

### 9.3 What the review did NOT find

No atomicity holes in the service flow (validate → consume-with-ledger →
create → add → rollback-on-any-failure mirrors the audited
TinkeringService shape), no iterator-over-mutation hazards (all loops
index concrete lists), no stringly-typed seam without a pin (F5 closed).
The hypothesis-driven deep-audit pass (CLAUDE.md) still applies once the
suite is green in Unity — player-flow hypotheses like "brew with the
knowledge part already present from an old save" and "drink a brew whose
effect name was removed from the factory" are the M1.3 candidates.

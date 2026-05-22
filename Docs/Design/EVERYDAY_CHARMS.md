# Everyday Charms — Content Roster

> **Content spec, not architecture.** The architecture for everyday magic already
> exists in [`EMERGENT_EVERYDAY_MAGIC.md`](EMERGENT_EVERYDAY_MAGIC.md): everyday
> spells are **cell-property modifiers** (organic matter, moisture, scent, airflow,
> sound, light, structural integrity, ground cover, + temperature/liquid), and the
> simulation reacts emergently. This file does two things that doc invites and
> leaves open:
>
> 1. Answers its **open question #5** ("how many everyday spells is enough?") with a
>    catalogued roster that extends the original ~10, each a **unique property
>    fingerprint** (design rule: no two charms modify the same combo in the same
>    direction).
> 2. **Bridges the system to the lore** in [`../../Lore/09_Magic.md`](../../Lore/09_Magic.md):
>    everyday charms are *small binding-work* on the world's binding-field, and that
>    gives them three properties the architecture doc doesn't model — a **bind/unbind
>    operation**, a **Thinning-sensitivity** (charms weaken as the world thins —
>    the domestic early-warning of the apocalypse), and **cultural variants**
>    (cosmetic, per the Phase-8 material grammar).
>
> **Honors the existing doctrine:** properties-only, no special-cases, genuinely weak
> in isolation, power is combinatorial, **no combo recipe book** (emergence notes here
> are one-line teasers, not a checklist), no hidden multipliers.

---

## 1. Lore framing (from `Lore/09_Magic.md`)

Everyday charms are the **small end** of the magic scale-spectrum: ubiquitous folk
practice, learned from kin like recipes, used by everyone for convenience, ceremony,
and *delight* (a field of flowers because it is pretty). They are **cantrip-tier** (per
`EMERGENT_EVERYDAY_MAGIC.md` open-Q#3): memorized permanently, weak in isolation,
always available, **no faction gate**, near-zero cost, cannot be sigiled.

Three lore facts become mechanics:

- **They only touch the *bound* side of the world** — they rearrange already-named
  things (soil, air, light), never selves. That is why anyone can cast them and why
  they carry no moral weight at this scale.
- **They are temporary.** Only deep, costly, material-anchored binding lasts (a
  salt-cure holds a century; a conjured meadow holds an afternoon). Charms decorate
  and ease ordinary life; **they cannot produce lasting value** — no permanent
  resources, no replacing labor. This is the core anti-abuse property and it falls
  straight out of the lore.
- **Binding charms faintly reinforce the world; unbinding charms faintly thin it.**
  (§4 — optional, flavor-tier completion-ratio trickle.)

---

## 2. Extended charm-definition format

Reuse the existing data format and add three lore-bridge fields (`Operation`,
`ThinningSensitivity`, `CulturalVariants`):

```
CharmID:        "Petalfall"
Category:       Whimsy
Operation:      Bind            # Bind | Unbind  (lore: which way it works the field)
TargetType:     Area (radius 2)
Duration:       120 turns       # BASE duration, before Thinning scaling (§3)
ThinningSensitivity: High       # High | Medium | Low — how fast it degrades (§3)
PropertyModifications:
  - Scent:        +0.4 (type: Floral)
  - LightModifier:+0.1
  - GroundCover:  +0.15 (type: Petals)
CulturalVariants:               # cosmetic only — identical property output
  - Sill:     "tossed handful of river-petals, hummed"
  - Wasteland:"one pressed dried bloom, sparingly — exquisite, sparse"
  - Catacomb: "drifting spores of bloom-light instead of petals"
```

No behavior code. Just deltas + the operation/sensitivity tags. The simulation does
the rest; the Thinning system and (optional) completion-ratio read the tags.

---

## 3. The Thinning degradation model (the domestic early-warning)

Per `Lore/09_Magic.md` §VIII, the Thinning's most intimate symptom is **everyday
charms beginning to fail** — festival-flowers wilting before noon, spring not coming
when the grandmothers call it — *before any scholar names the crisis.* Implement as a
single global read:

- A world-state `ThinningLevel` ∈ [0,1] (already the Phase-6 completion-ratio's
  cosmic correlate; rises through the game).
- Effective magnitude and duration scale by `1 - k·ThinningLevel·sensitivity`, where
  `sensitivity ∈ {High=1.0, Medium=0.6, Low=0.3}` and `k` is a tuning constant
  (~0.7 suggested).
- Above a threshold (`ThinningLevel > ~0.6`), a charm has a small **fizzle chance**
  (the meadow half-blooms and dies by midday; the porridge-glow gutters).
- **Region modifier:** in despairing/depopulated regions where *no one conjures
  anymore*, charms degrade faster (a haunting texture — the places that stopped making
  small beautiful things thin first). Optional; flag for playtest.
- **NPC surfacing:** village elders comment as `ThinningLevel` rises ("the flowers
  don't hold like they did when I was a girl"). This is the player's earliest,
  gentlest sign that something is wrong — *years before the gods stir.*

*High-sensitivity charms (the purely pretty ones — Petalfall, Warmsnow, Glowmark) fade
first; Low-sensitivity charms (the practical ones — Coax Ember, Clean Surface) hold
longest. The whimsy dies before the utility, which is exactly the right heartbreak.*

---

## 4. Bind/unbind & the completion trickle (optional, flavor-tier)

Per `Lore/09_Magic.md` §VIII, everyday *binding* (adding, arranging, growing,
brightening) faintly reinforces the world's binding-field; *unbinding* (removing,
dispersing, withering) faintly thins it. If implemented (soft-lean yes, but may stay
flavor-only):

- Each **Bind** charm cast adds a tiny ε to the binding/completion reservoir; each
  **Unbind** charm subtracts ε.
- ε is **deliberately minuscule and non-farmable** (a per-charm-per-region cooldown on
  the trickle, or diminishing returns) — the point is *thematic* (a world full of
  people making small pretty things is quietly holding itself together), not a grind
  lever. If balancing proves fiddly, keep it pure flavor (no reservoir effect) and
  preserve only the NPC/world-text that *says* it's true.

---

## 5. The roster

Extends the original 10 (Field of Flowers, Shake Leaves, Gentle Breeze, Warm Hearth,
Clean Surface, Muffle, Create Spring, Dry Wind, Brighten, Dim — all in
`EMERGENT_EVERYDAY_MAGIC.md`). Each new charm below has a **distinct property
fingerprint** (verified in §6). Property deltas are illustrative; tune in playtest.

### Whimsy & beauty (Bind) — *the heart of "because it's pretty"*

- **Petalfall** — drifting petals/blossom in the air. `+Scent(Floral) +Light(0.1)
  +GroundCover(Petals,0.15)`. *Sensitivity: High.* The courtship charm — a Petalfall
  that follows a walking caster leaves petals in their footsteps. (Distinct from Field
  of Flowers: airborne/settling, **no** ground organic matter or moisture.)
  *Emergent tease: pollen-scent draws pollinators; settled petals are light, dry cover.*
- **Warmsnow** — soft snow that falls warm and harmless. `+GroundCover(Soft,0.2)
  +Light(0.05) Temp:neutral`. *Sensitivity: High.* Pure festival delight; melts to
  nothing. (Distinct: cover+light with **no** temperature drop — "warm" snow.)
- **Glowmark** — a hovering mote of light that follows the caster (festival cuffs that
  shimmer; a child's nightlight). `+Light(point, follows)`. *Sensitivity: High.*
  Implemented as a short-lived light-entity, not a cell mod. (Distinct from Brighten =
  static area; Glowmark = mobile point.)
- **Mistveil** — a thin decorative ground-fog. `+Moisture(air,0.3) -Light(0.15)
  -Sound(0.1)`. *Sensitivity: Medium.* Pretty and atmospheric. (Distinct combo:
  moisture + dim + hush together.) *Emergent tease: dampens fire, softens footfalls.*

### Garden & growth (Bind)

- **Quicken Bloom** — coaxes *existing* plant life to grow and spread faster.
  `+Light(0.3) +Moisture(0.3)` over an organic cell (a growth **catalyst** — adds the
  inputs, not the plants). *Sensitivity: Medium.* (Distinct: pure growth-inputs, adds
  no organic/scent itself.) *Emergent tease: turns a Field of Flowers into a creeping
  one over many turns.*
- **Dewfall** — a freshening morning dew. `+Moisture(ground,0.4)
  +GroundCover(Dew,0.1)`. *Sensitivity: Medium.* (Distinct from Create Spring: no
  standing liquid, no sound — just damp + sheen.) *Emergent tease: damp ground resists
  fire; conducts a little.*
- **Greenmantle** — coaxes moss/lichen onto stone & walls. `+OrganicMatter(low,0.25)
  +Moisture(low,0.2)` on **vertical/stone** cells. *Sensitivity: Low.* (Distinct:
  organic on walls, not floor; low magnitude, no scent/cover/light.) *Emergent tease:
  a mossed wall is faintly combustible cover where stone usually isn't.*
- **Sweeten** — clears a sour smell / sweetens water and air. `+Scent(Fresh,0.3)`
  (pure). *Sensitivity: Low.* Comfort-tier; the most trivial charm. (Distinct: lone
  positive scent.)

### Hearth & home (Bind / neutral)

- **Coax Ember** — kindles a controlled flame in *existing* fuel. `+Temperature(sharp,
  single cell)`. *Sensitivity: Low.* (Distinct from Warm Hearth = gentle radius
  warmth; Coax Ember = a point ignition spike on one cell — lights the lamp, the
  hearth, the candle.) *Emergent tease: it is a point ignition source — mind the dry
  flowers.*
- **Hearthsong** — a warm carrying tone (a call to supper, a festival note).
  `+Sound(Pleasant, sustained point source)`. *Sensitivity: Medium.* (Distinct: a
  *positive sustained* sound source; Create Spring's sound is bundled with water,
  Shake Leaves' is brief.) *Emergent tease: sound carries downwind; lures the curious,
  alerts the wary.*

### Cleansing & letting-go (Unbind) — *faintly thin the world*

- **Scatter** — sweeps loose leaves, petals, dust away. `-GroundCover(0.4)
  -Scent(0.3) +Airflow(minor)`. *Sensitivity: Low.* (Distinct from Clean Surface: keeps
  liquid & organic; only clears *loose* cover, scent, stirs air.) *Emergent tease:
  clears a scent trail before predators follow it.*
- **Wither** — the gentle anti-Field; lets a growth die back (a grief-charm at a
  grave; clearing a plot; spite). `-OrganicMatter(0.5) -Moisture(0.3)`.
  *Sensitivity: Low.* **The clearest everyday Unbind** — and a quiet character beat:
  the only common charm that *takes the pretty thing away.* (Distinct: removes
  organic+moisture, the inverse of Field of Flowers.)

---

## 6. Property-fingerprint matrix (design rule #5 — no redundancy)

Each charm hits a unique combination/direction. (`+`/`–` = adds/removes; blank =
untouched. Org=OrganicMatter, Moi=Moisture, Sce=Scent, Air=Airflow, Snd=Sound,
Lgt=Light, Str=StructuralIntegrity, Cov=GroundCover, Tmp=Temperature, Liq=Liquid.)

| Charm | Op | Org | Moi | Sce | Air | Snd | Lgt | Str | Cov | Tmp | Liq |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Field of Flowers* | B | + | + | + |  |  | + |  | + |  |  |
| Petalfall | B |  |  | + |  |  | + |  | + |  |  |
| Warmsnow | B |  |  |  |  |  | + |  | + | · |  |
| Glowmark | B |  |  |  |  |  | +(pt) |  |  |  |  |
| Mistveil | B |  | + |  |  | – | – |  |  |  |  |
| Quicken Bloom | B |  | + |  |  |  | + |  |  |  |  |
| Dewfall | B |  | + |  |  |  |  |  | + |  |  |
| Greenmantle | B | +(wall) | + |  |  |  |  |  |  |  |  |
| Sweeten | B |  |  | + |  |  |  |  |  |  |  |
| Coax Ember | B |  |  |  |  |  |  |  |  | +(pt) |  |
| Hearthsong | B |  |  |  |  | + |  |  |  |  |  |
| Scatter | U |  |  | – | + |  |  |  | – |  |  |
| Wither | U | – | – |  |  |  |  |  |  |  |  |
| Warm Hearth* | B |  |  |  |  |  |  |  |  | + |  |
| Create Spring* | B |  | + |  |  | + |  |  |  |  | + |
| Dry Wind* | U |  | – |  | + |  |  |  |  |  |  |
| Muffle* | U |  |  |  | – | – |  |  |  |  |  |
| Gentle Breeze* | – |  |  |  | + |  |  |  |  |  |  |
| Brighten* | B |  |  |  |  |  | + |  |  |  |  |
| Dim* | U |  |  |  |  |  | – |  |  |  |  |
| Clean Surface* | U | – |  | – |  |  |  |  | – |  | – |
| Shake Leaves* | – | + |  |  |  | + |  |  | ± |  |  |

\* = existing in `EMERGENT_EVERYDAY_MAGIC.md`. Each row is a distinct fingerprint;
new entries don't collide with existing ones. *Where a near-collision exists
(Warmsnow vs. Warm Hearth both touch Cover/Light) the distinguishing axis is the
Temperature column — Warmsnow is temp-neutral "warm snow," Warm Hearth is pure
radius heat.*

---

## 7. Object & comfort charms (a separate, simpler surface)

The lore lists charms that act on **objects and creatures**, not cells — *glowing
porridge, mend a pot, freshen milk, calm a beast, ease an ache.* These are **not part
of the emergent cell-property system** and must not be (the architecture doc's rule:
no direct creature status from everyday spells). Keep them as a deliberately tiny,
non-combat, social/cosmetic surface:

| Charm | Op | Effect | Bound |
|---|---|---|---|
| **Glowing Porridge** | B | an item emits soft light for a while; comfort for a sick child | short item-light buff |
| **Mend** | B | closes a small crack in cloth/pot/tool (cosmetic/trivial repair only — never gear stats) | minor, item-only |
| **Freshen** | B | a perishable keeps one extra day | short item-timer extension |
| **Settle** (calm a beast / soothe a baby) | B | a brief, weak nudge toward calm in one creature — **non-combat, no mechanical pacify in a fight** | tiny mood tick, short |
| **Ease** | B | takes the edge off a minor ache for a while — flavor, not a heal | negligible, out of combat |

**Hard rule:** these never touch combat. `Settle` cannot pacify an enemy mid-fight;
`Ease` is not a heal; `Mend` never restores weapon/armor stats. They are texture —
the warmth of ordinary life — and their power ceiling is *exactly nothing tactical*,
by design and by lore (they're surface-binding, temporary, free).

---

## 8. Casting model

- **Universal & cantrip-tier.** Every player character knows a few from the start
  (origin-flavored, per `Lore/07_Characters.md` — a Sill River-Child opens with
  river-charms; a Catacomb Apostate with bloom-light ones). More are learned from NPCs
  for free or trivially (a grandmother, a festival, a village elder).
- **Cast = a gesture / hum / pinch.** Short cast time, near-zero cost (no mana; the
  "cost" is a moment and a little focus). No cooldown, or a trivial one. Cannot be
  sigiled (per the architecture doc).
- **Cultural variants are cosmetic** (per §2 / Phase-8 material grammar): same property
  output, different flavor text, gesture, and component by region/faction.
- **Diag (per CLAUDE.md observability):** every cast emits a `category=magic` record —
  `kind=CharmCast`, `charmId`, `operation` (bind/unbind), `properties[]`,
  `thinningMultiplier`, `fizzled` (bool). A bug report of "charms feel weak" then
  starts with `diag_query category=magic kind=CharmCast` to read the thinning-scaled
  magnitudes, not with code-grep.

---

## 9. Implementation checklist

1. **Charm definitions** as data (the §2 format) — extend the existing everyday-spell
   table; no per-charm code.
2. **`Operation` + `ThinningSensitivity` tags** read by: the Thinning scaler (§3) and
   (optional) the completion trickle (§4).
3. **Thinning scaler** — a single function `EffectiveDurationAndMagnitude(charm,
   thinningLevel)` applied at cast; fizzle roll above threshold.
4. **Glowmark** needs a short-lived follow-light entity (the one charm that isn't a
   pure cell mod); everything else is property deltas.
5. **Object/comfort charms** (§7) on the separate trivial item/creature-buff surface —
   gated hard out of combat.
6. **NPC Thinning dialogue** hooks (§3) — elders comment as `ThinningLevel` rises.
7. **Diag emission** (§8) on every cast.
8. **Tests** (TDD per CLAUDE.md): (a) each charm writes its declared properties and
   nothing else; (b) Thinning scaling reduces duration/magnitude monotonically and a
   counter-check that `ThinningLevel=0` leaves them at base; (c) object/comfort charms
   emit no combat-relevant effect (adversarial: assert `Settle` on a hostile in combat
   does nothing); (d) a Bind charm's trickle increments and an Unbind's decrements the
   reservoir (if §4 implemented), with a counter-check that a neutral charm does
   neither; (e) diag record fires on cast with correct `operation`/`fizzled`.

---

## 10. Cross-references

- [`EMERGENT_EVERYDAY_MAGIC.md`](EMERGENT_EVERYDAY_MAGIC.md) — the architecture
  (property layer, propagation, emergence philosophy, the original ~10 spells, the
  Frieren framing). **This roster sits on top of it.**
- [`../../Lore/09_Magic.md`](../../Lore/09_Magic.md) — the binding/unbinding cosmology,
  the scale spectrum, the Thinning-as-charm-failure symptom, self-as-Naming.
- [`../../Lore/08_MaterialCulture.md`](../../Lore/08_MaterialCulture.md) — the material
  grammar that drives the cosmetic cultural variants.
- [`../../Lore/06_Plot.md`](../../Lore/06_Plot.md) — the completion-ratio / Thinning
  that the degradation model and the bind/unbind trickle read from.
- `MAGIC-SKILLS-DESIGN.md`, `ALCHEMY_SPELL_SYSTEM.md`, `GROUND_ERUPTION_SPELLS.md` —
  the *deep* magic tiers; everyday charms are explicitly the weak, free, universal
  floor beneath them.

---

## 11. Open questions (for playtest / level-design)

- **Roster size.** This adds 12 cell-charms (→ ~22 total) + 5 object/comfort charms.
  More can be added as new property combinations are identified (per the architecture
  doc's open-Q#5) — but each must claim an empty cell in the §6 matrix.
- **Completion trickle: real or flavor?** §4 — soft-lean real-but-tiny; fall back to
  flavor-only if non-farmable balancing proves fiddly.
- **Region-despair degradation.** §3 — haunting but optional; playtest whether it reads
  as meaningful or just punishing.
- **Glowmark as the only entity-charm** — acceptable special case, or refactor to a
  generic "follow-light" component reusable by lanterns/fireflies?
- **Discoverability of Thinning-failure.** Tune how early/obvious the charm-fading is —
  it should be a *gentle* dread, noticed before it's named.
